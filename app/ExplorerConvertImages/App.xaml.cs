// SPDX-License-Identifier: GPL-3.0-only

using ConvertImages.Core;
using Microsoft.UI.Xaml;

namespace ExplorerConvertImages;

/// <summary>
/// Entry point of the application. It is started by the context-menu handler
/// (<c>explorer-convert-images-verb.exe</c>) with <c>--input-list FILE</c>,
/// FILE listing the selected files and folders; it can also be started by
/// hand, with paths as arguments or with nothing at all.
/// </summary>
public partial class App : Application
{
    private Window? _window;

    public App()
    {
        // Before anything else, so that an error at start-up leaves a trace
        // too. The two events cover different places: the first one the code
        // called by the window system, the second one everything else.
        UnhandledException += (_, e) => CrashLog.Write(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => CrashLog.Write(
            e.ExceptionObject as Exception ?? new InvalidOperationException(e.ExceptionObject?.ToString()));

        // Start-up runs inside a call made by the window system, and an error
        // there does not always reach the events above: hence the explicit
        // traces, here and in OnLaunched.
        try
        {
            InitializeComponent();
        }
        catch (Exception exception)
        {
            CrashLog.Write(exception);
            throw;
        }
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        try
        {
            // The first element is the path of the program itself.
            var arguments = AppArguments.Parse(Environment.GetCommandLineArgs().Skip(1).ToArray());
            _window = new MainWindow(arguments);
            _window.Activate();
        }
        catch (Exception exception)
        {
            CrashLog.Write(exception);
            throw;
        }
    }
}

/// <summary>
/// Last-resort trace: an error nobody handled is appended to
/// <c>explorer-convert-images-app.log</c> in the temporary folder of the
/// user before the application stops, so that a crash can be reported with
/// something to go on.
/// </summary>
internal static class CrashLog
{
    private static Exception? _lastWritten;

    public static void Write(Exception exception)
    {
        // The same error can come by two ways (an explicit trace, then one of
        // the "unhandled" events): once is enough.
        if (ReferenceEquals(exception, _lastWritten))
        {
            return;
        }
        _lastWritten = exception;

        try
        {
            var file = Path.Combine(Path.GetTempPath(), "explorer-convert-images-app.log");
            File.AppendAllText(file, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {exception}{Environment.NewLine}");
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Nothing more can be done.
        }
    }
}
