// SPDX-License-Identifier: GPL-3.0-only

using System.Text.RegularExpressions;
using ConvertImages.Core.Engine;
using ConvertImages.Core.Localization;

namespace ConvertImages.Core.Tests;

internal static partial class EngineTests
{
    [GeneratedRegex(@"\{\w+\}")]
    private static partial Regex Placeholder();

    // ------------------------------------------------------------ events

    [Test]
    private static void Parser_reads_the_detect_events()
    {
        var file = EngineEventParser.Parse(
            """{"event":"file","format":"RAW Sony (ARW)","is_raw":true,"path":"C:\\Photos d'été\\a b.arw"}""");
        Check.Equal<EngineEvent?>(new FileDetected(@"C:\Photos d'été\a b.arw", "RAW Sony (ARW)", true), file);

        var summary = EngineEventParser.Parse(
            """{"counts":{"JPEG":2,"RAW Sony (ARW)":1},"event":"summary","total":3}""") as DetectSummary;
        Check.True(summary is not null, "a summary with counts is a DetectSummary");
        Check.Equal(3, summary!.Total);
        Check.Equal(2, summary.Counts["JPEG"]);
        Check.Equal(1, summary.Counts["RAW Sony (ARW)"]);
    }

    [Test]
    private static void Parser_reads_the_convert_events()
    {
        Check.Equal<EngineEvent?>(new ConvertStarted(7), EngineEventParser.Parse("""{"event":"start","total":7}"""));

        Check.Equal<EngineEvent?>(
            new FileConverted(5, 7, @"Z:\in\château.jpg", @"Z:\out\château.png", true, null),
            EngineEventParser.Parse(
                """{"event":"file_done","index":5,"input":"Z:\\in\\château.jpg","output":"Z:\\out\\château.png","status":"ok","total":7}"""));

        Check.Equal<EngineEvent?>(
            new FileConverted(7, 7, "fake.arw", null, false, "decoding failed: nope"),
            EngineEventParser.Parse(
                """{"event":"file_done","index":7,"input":"fake.arw","message":"decoding failed: nope","status":"error","total":7}"""));

        Check.Equal<EngineEvent?>(
            new ConvertSummary(7, 6, 1, @"Z:\out"),
            EngineEventParser.Parse("""{"errors":1,"event":"summary","ok":6,"output_dir":"Z:\\out","total":7}"""));

        Check.Equal<EngineEvent?>(
            new FatalError("could not create the output folder: denied"),
            EngineEventParser.Parse("""{"event":"fatal_error","message":"could not create the output folder: denied"}"""));
    }

    [Test]
    private static void Parser_ignores_what_it_does_not_know()
    {
        foreach (var line in new[] { null, "", "   ", "not json", "[1,2]", "42", """{"event":"from_the_future"}""", """{"no_event":true}""", "{" })
        {
            Check.Equal<EngineEvent?>(null, EngineEventParser.Parse(line), $"line: {line}");
        }
    }

    // ---------------------------------------------------------- messages

    [Test]
    private static void Messages_are_translated_with_their_details_kept()
    {
        var french = new EngineMessageTranslator(Translator.ForLanguage("fr"));
        var catalog = Translator.LoadCatalog("fr")!;

        Check.Equal(catalog.Find(Msg.EngineSkipped), french.Translate("skipped (the destination file already exists)"));
        Check.Equal(
            Translator.Format(catalog.Find(Msg.EngineEncodingFailed)!, ("format", "PNG"), ("detail", "disk full")),
            french.Translate("PNG encoding failed: disk full"));
        Check.Equal(
            Translator.Format(catalog.Find(Msg.EngineInternalError)!, ("name", "rawloader")),
            french.Translate("rawloader: internal decoder error (corrupted file?)"));
        Check.Equal(
            Translator.Format(catalog.Find(Msg.EngineCannotReadList)!, ("detail", "os error 2")),
            french.Translate("could not read the input list: os error 2"));
    }

    [Test]
    private static void Messages_joined_by_the_fallback_are_both_translated()
    {
        var french = new EngineMessageTranslator(Translator.ForLanguage("fr"));
        var catalog = Translator.LoadCatalog("fr")!;

        // The two decoders failed: each half is itself a known message, and
        // braces in a detail must come through untouched.
        const string message =
            "cannot open RAW file: RawLoaderError: \"x {y}\" ; LibRaw fallback: LibRaw: Input/output error";
        var expected = Translator.Format(
            catalog.Find(Msg.EngineFallback)!,
            ("primary", Translator.Format(catalog.Find(Msg.EngineCannotOpenRaw)!, ("detail", "RawLoaderError: \"x {y}\""))),
            ("fallback", "LibRaw: Input/output error"));
        Check.Equal(expected, french.Translate(message));
        Check.True(french.Translate(message).Contains("\"x {y}\""), "the detail was altered");
    }

    [Test]
    private static void Messages_unknown_or_in_english_are_left_alone()
    {
        var french = new EngineMessageTranslator(Translator.ForLanguage("fr"));
        Check.Equal("something the engine never says", french.Translate("something the engine never says"));
        Check.Equal("", french.Translate(null));
        Check.Equal("", french.Translate(""));

        var english = new EngineMessageTranslator(Translator.ForLanguage("en"));
        Check.Equal("PNG encoding failed: disk full", english.Translate("PNG encoding failed: disk full"));
    }

    [Test]
    private static void Messages_format_labels_translate_words_but_not_proper_names()
    {
        var french = new EngineMessageTranslator(Translator.ForLanguage("fr"));
        var catalog = Translator.LoadCatalog("fr")!;
        Check.Equal(catalog.Find(Msg.LabelUnknown), french.TranslateFormatLabel("Unknown"));
        Check.Equal(catalog.Find(Msg.LabelGenericRaw), french.TranslateFormatLabel("Generic RAW"));
        Check.Equal("RAW Sony (ARW)", french.TranslateFormatLabel("RAW Sony (ARW)"));
        Check.Equal("JPEG", french.TranslateFormatLabel("JPEG"));
    }

    /// <summary>
    /// The patterns of the interface must exist in the Rust code: if a message
    /// of the engine is reworded, its pattern here must follow.
    /// </summary>
    [Test]
    private static void Messages_every_template_is_emitted_by_the_engine()
    {
        var sources = string.Join(
            "\n",
            Directory.GetFiles(Repo.File_("engine", "src"), "*.rs").Select(File.ReadAllText));
        Check.True(Msg.EngineMessageTemplates.Count >= 10, "templates list unexpectedly short");
        foreach (var template in Msg.EngineMessageTemplates)
        {
            var longest = Placeholder().Split(template)
                .Where(chunk => chunk.Trim().Length > 0)
                .OrderByDescending(chunk => chunk.Length)
                .First();
            Check.True(sources.Contains(longest), $"pattern with no equivalent in engine/src/*.rs: \"{template}\"");
        }
    }

    // --------------------------------------------------------- arguments

    [Test]
    private static void Arguments_match_the_command_line_of_the_engine()
    {
        Check.Sequence(
            ["detect", "--recursive", "--input-list", @"C:\Temp\list.txt"],
            EngineArguments.Detect(@"C:\Temp\list.txt"));

        Check.Sequence(
            [
                "convert", "--recursive", "--input-list", @"C:\Temp\list.txt",
                "--output-dir", @"D:\Out put", "--format", "tiff", "--quality", "100",
                "--on-collision", "overwrite",
            ],
            EngineArguments.Convert(new ConversionOptions("tiff", 250, "overwrite", @"D:\Out put"), @"C:\Temp\list.txt"));

        Check.True(
            EngineArguments.Convert(new ConversionOptions("jpeg", 0, "rename", "x"), "l").Contains("1"),
            "the quality is clamped to 1..100");
    }

    [Test]
    private static void Choices_ids_are_those_the_engine_accepts_and_jpeg_comes_first()
    {
        Check.Sequence(["jpeg", "png", "tiff", "bmp", "webp"], Choices.Formats.Select(c => c.Id));
        Check.Sequence(["rename", "skip", "overwrite"], Choices.Collisions.Select(c => c.Id));

        // The same lists as the `clap` enums of the engine.
        var convert = File.ReadAllText(Repo.File_("engine", "src", "convert.rs"));
        foreach (var format in Choices.Formats)
        {
            var variant = char.ToUpperInvariant(format.Id[0]) + format.Id[1..];
            Check.True(convert.Contains($"    {variant},"), $"TargetFormat::{variant} not found in convert.rs");
        }
        var cli = File.ReadAllText(Repo.File_("engine", "src", "cli.rs"));
        foreach (var collision in Choices.Collisions)
        {
            var variant = char.ToUpperInvariant(collision.Id[0]) + collision.Id[1..];
            Check.True(cli.Contains($"    {variant},"), $"CollisionPolicy::{variant} not found in cli.rs");
        }
    }

    /// <summary>
    /// The application looks for the engine by file name: the name used here,
    /// the one Cargo builds and the one the installer copies must agree.
    /// </summary>
    [Test]
    private static void Runner_engine_file_name_matches_cargo_and_the_installer()
    {
        var cargo = File.ReadAllText(Repo.File_("engine", "Cargo.toml"));
        var package = Path.GetFileNameWithoutExtension(EngineRunner.EngineFileName);
        Check.True(cargo.Contains($"name = \"{package}\""), $"engine/Cargo.toml does not build {package}");

        var installer = File.ReadAllText(Repo.File_("installer", "installer.nsi"));
        Check.True(
            installer.Contains($"!define ENGINE_EXE \"{EngineRunner.EngineFileName}\""),
            "installer.nsi does not name the engine the same way");
    }

    [Test]
    private static void Runner_reports_an_engine_that_cannot_be_started()
    {
        var runner = new EngineRunner(Path.Combine(Path.GetTempPath(), "no-such-folder", "no-such-engine.exe"));
        Check.Throws<EngineStartException>(() => runner.Start(["formats"], _ => { }), "missing engine");
    }
}
