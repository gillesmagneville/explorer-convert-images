// SPDX-License-Identifier: GPL-3.0-only

using ConvertImages.Core.Engine;
using ConvertImages.Core.Localization;

namespace ConvertImages.Core.Tests;

/// <summary>
/// Runs the real engine through <see cref="EngineRunner"/>, on real files.
/// The engine is looked for where Cargo builds it; set CONVERT_IMAGES_ENGINE
/// to use another one. Without an engine the tests are skipped — unless
/// CONVERT_IMAGES_REQUIRE_ENGINE=1 (the continuous integration sets it, so
/// that these tests can never be skipped silently there).
/// </summary>
internal static class EngineIntegrationTests
{
    private static string EnginePath()
    {
        var candidates = new List<string>();
        if (Environment.GetEnvironmentVariable(EngineRunner.EngineVariable) is { Length: > 0 } overridden)
        {
            candidates.Add(overridden);
        }
        candidates.Add(Repo.File_("target", "x86_64-pc-windows-gnu", "release", "convert-images-engine.exe"));
        candidates.Add(Repo.File_("target", "release", "convert-images-engine.exe"));
        candidates.Add(Repo.File_("target", "release", "convert-images-engine"));

        // A Windows program cannot be started on another system.
        var usable = candidates.Where(File.Exists)
            .Where(path => OperatingSystem.IsWindows() || !path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));
        if (usable.FirstOrDefault() is { } found)
        {
            return found;
        }
        if (Environment.GetEnvironmentVariable("CONVERT_IMAGES_REQUIRE_ENGINE") == "1")
        {
            throw new TestFailure("the engine is required but was not found in: " + string.Join(", ", candidates));
        }
        throw new TestSkipped("engine not built (cargo build --release -p convert-images-engine)");
    }

    /// <summary>A 4×4 uncompressed 24-bit BMP image.</summary>
    private static byte[] TinyBmp(byte shade)
    {
        const int width = 4, height = 4, pixelBytes = width * height * 3;
        var bmp = new byte[54 + pixelBytes];
        bmp[0] = (byte)'B';
        bmp[1] = (byte)'M';
        BitConverter.TryWriteBytes(bmp.AsSpan(2), bmp.Length);
        BitConverter.TryWriteBytes(bmp.AsSpan(10), 54);
        BitConverter.TryWriteBytes(bmp.AsSpan(14), 40);
        BitConverter.TryWriteBytes(bmp.AsSpan(18), width);
        BitConverter.TryWriteBytes(bmp.AsSpan(22), height);
        BitConverter.TryWriteBytes(bmp.AsSpan(26), (short)1);
        BitConverter.TryWriteBytes(bmp.AsSpan(28), (short)24);
        BitConverter.TryWriteBytes(bmp.AsSpan(34), pixelBytes);
        Array.Fill(bmp, shade, 54, pixelBytes);
        return bmp;
    }

    private sealed class Workspace : IDisposable
    {
        public Workspace()
        {
            // Accents and spaces on purpose: this is what real folders look like.
            Root = Path.Combine(Path.GetTempPath(), $"eci tests été {Environment.ProcessId}-{Guid.NewGuid():N}");
            Photos = Path.Combine(Root, "Photos d'été");
            Directory.CreateDirectory(Path.Combine(Photos, "sous dossier"));
            File.WriteAllBytes(Path.Combine(Photos, "château.bmp"), TinyBmp(200));
            File.WriteAllBytes(Path.Combine(Photos, "sous dossier", "ünïcode.bmp"), TinyBmp(40));
            File.WriteAllText(Path.Combine(Photos, "notes.txt"), "not an image");
        }

        public string Root { get; }

        public string Photos { get; }

        public void Dispose()
        {
            try
            {
                Directory.Delete(Root, recursive: true);
            }
            catch (IOException)
            {
                // Best effort.
            }
        }
    }

    private static (List<EngineEvent> Events, int ExitCode) Run(IReadOnlyList<string> arguments)
    {
        var events = new List<EngineEvent>();
        var run = new EngineRunner(EnginePath()).Start(arguments, e =>
        {
            lock (events)
            {
                events.Add(e);
            }
        });
        Check.True(run.Completion.Wait(TimeSpan.FromMinutes(2)), "the engine did not finish in two minutes");
        return (events, run.Completion.Result);
    }

    [Test]
    private static void Engine_detects_then_converts_a_folder_given_through_a_list_file()
    {
        EnginePath();
        using var workspace = new Workspace();
        var list = Selection.WriteListFile([workspace.Photos]);
        try
        {
            // Detection, subfolders included; the text file is not an image.
            var (detected, detectCode) = Run(EngineArguments.Detect(list));
            Check.Equal(0, detectCode, "exit code of detect");
            var files = detected.OfType<FileDetected>().ToList();
            Check.Sequence(
                ["château.bmp", "ünïcode.bmp"],
                files.Select(f => Path.GetFileName(f.Path)).Order(StringComparer.Ordinal),
                "detected files (names must come back intact)");
            Check.True(files.All(f => f.Format == "BMP" && !f.IsRaw), "both are plain BMP images");
            var summary = detected.OfType<DetectSummary>().Single();
            Check.Equal(2, summary.Total);
            Check.Equal(2, summary.Counts["BMP"]);

            // Conversion to PNG, into a folder that does not exist yet.
            var output = Path.Combine(workspace.Root, "résultat png");
            var (converted, convertCode) = Run(
                EngineArguments.Convert(new ConversionOptions("png", 100, "rename", output), list));
            Check.Equal(0, convertCode, "exit code of convert");
            Check.Equal(2, converted.OfType<ConvertStarted>().Single().Total);
            var done = converted.OfType<FileConverted>().ToList();
            Check.Equal(2, done.Count);
            Check.True(done.All(f => f.Ok && f.Total == 2), "both conversions succeed");
            Check.Sequence([1, 2], done.Select(f => f.Index).Order());
            foreach (var file in done)
            {
                Check.True(File.Exists(file.Output), $"missing output file {file.Output}");
            }
            Check.True(File.Exists(Path.Combine(output, "château.png")), "château.png not written");
            Check.True(File.Exists(Path.Combine(output, "ünïcode.png")), "ünïcode.png not written");
            var total = converted.OfType<ConvertSummary>().Single();
            Check.Equal((2, 2, 0), (total.Total, total.Ok, total.Errors));

            // Same again: "rename" keeps the first results and adds new names.
            Run(EngineArguments.Convert(new ConversionOptions("png", 100, "rename", output), list));
            Check.True(File.Exists(Path.Combine(output, "château (1).png")), "the second run must rename");
        }
        finally
        {
            File.Delete(list);
        }
    }

    [Test]
    private static void Engine_reports_a_missing_list_as_a_fatal_error_that_gets_translated()
    {
        EnginePath();
        var missing = Path.Combine(Path.GetTempPath(), $"eci-no-such-list-{Guid.NewGuid():N}.txt");
        var (events, exitCode) = Run(EngineArguments.Detect(missing));
        Check.Equal(1, exitCode, "exit code");
        var fatal = events.OfType<FatalError>().Single();
        Check.True(fatal.Message.StartsWith("could not read the input list: ", StringComparison.Ordinal), fatal.Message);

        var french = new EngineMessageTranslator(Translator.ForLanguage("fr")).Translate(fatal.Message);
        Check.True(french != fatal.Message, $"not translated: {french}");
    }

    [Test]
    private static void Engine_can_be_stopped()
    {
        EnginePath();
        using var workspace = new Workspace();
        var list = Selection.WriteListFile([workspace.Photos]);
        try
        {
            var run = new EngineRunner(EnginePath()).Start(EngineArguments.Detect(list), _ => { });
            run.Kill();
            Check.True(run.Completion.Wait(TimeSpan.FromSeconds(30)), "a killed engine must complete");
            run.Kill(); // Killing twice, or after the end, is harmless.
        }
        finally
        {
            File.Delete(list);
        }
    }
}
