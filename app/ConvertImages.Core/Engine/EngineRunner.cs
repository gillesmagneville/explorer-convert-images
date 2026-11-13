// SPDX-License-Identifier: GPL-3.0-only

using System.Diagnostics;
using System.Text;

namespace ConvertImages.Core.Engine;

/// <summary>The engine program could not be started.</summary>
public sealed class EngineStartException(string message, Exception inner) : Exception(message, inner);

/// <summary>
/// Runs <c>convert-images-engine</c> as a separate process and reports its
/// events as they arrive. All the detection and conversion work happens in
/// that process: the interface never decodes an image itself.
/// </summary>
public sealed class EngineRunner
{
    /// <summary>File name of the engine, installed next to the application.</summary>
    public const string EngineFileName = "convert-images-engine.exe";

    /// <summary>
    /// Environment variable giving another path for the engine (development
    /// and tests only).
    /// </summary>
    public const string EngineVariable = "CONVERT_IMAGES_ENGINE";

    private readonly string _enginePath;

    public EngineRunner(string enginePath) => _enginePath = enginePath;

    /// <summary>Path of the engine: next to the application, unless overridden.</summary>
    public static string DefaultEnginePath()
    {
        var overridden = Environment.GetEnvironmentVariable(EngineVariable);
        return string.IsNullOrWhiteSpace(overridden)
            ? Path.Combine(AppContext.BaseDirectory, EngineFileName)
            : overridden;
    }

    /// <summary>
    /// Starts the engine with <paramref name="arguments"/>.
    /// <paramref name="onEvent"/> is called for each event, in order, on a
    /// background thread: the caller switches to its own thread if it needs to.
    /// </summary>
    /// <exception cref="EngineStartException">The program could not be started.</exception>
    public EngineRun Start(IReadOnlyList<string> arguments, Action<EngineEvent> onEvent)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _enginePath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            // The engine writes UTF-8, whatever the code page of the system.
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        foreach (var argument in arguments)
        {
            // ArgumentList quotes each argument as needed (spaces, quotes…).
            startInfo.ArgumentList.Add(argument);
        }

        var process = new Process { StartInfo = startInfo };
        try
        {
            process.Start();
        }
        catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException
            or FileNotFoundException or DirectoryNotFoundException)
        {
            process.Dispose();
            throw new EngineStartException(e.Message, e);
        }
        return new EngineRun(process, onEvent);
    }
}

/// <summary>A running (or finished) engine process.</summary>
public sealed class EngineRun
{
    private readonly Process _process;

    internal EngineRun(Process process, Action<EngineEvent> onEvent)
    {
        _process = process;
        Completion = Task.Run(() => PumpAsync(onEvent));
    }

    /// <summary>
    /// Completes when the engine has exited and all its events have been
    /// reported; the result is its exit code (0: no fatal error).
    /// </summary>
    public Task<int> Completion { get; }

    /// <summary>
    /// Stops the engine at once. The files already written stay on disk; the
    /// one being written, if any, is left incomplete.
    /// </summary>
    public void Kill()
    {
        try
        {
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
            }
        }
        catch (Exception e) when (e is InvalidOperationException or System.ComponentModel.Win32Exception
            or NotSupportedException)
        {
            // Already gone.
        }
    }

    private async Task<int> PumpAsync(Action<EngineEvent> onEvent)
    {
        try
        {
            // The engine reports nothing useful on its error stream, but an
            // unread pipe would end up blocking it: drain it.
            var errors = _process.StandardError.ReadToEndAsync();

            while (await _process.StandardOutput.ReadLineAsync().ConfigureAwait(false) is { } line)
            {
                if (EngineEventParser.Parse(line) is { } engineEvent)
                {
                    onEvent(engineEvent);
                }
            }
            await errors.ConfigureAwait(false);
            await _process.WaitForExitAsync().ConfigureAwait(false);
            return _process.ExitCode;
        }
        finally
        {
            _process.Dispose();
        }
    }
}
