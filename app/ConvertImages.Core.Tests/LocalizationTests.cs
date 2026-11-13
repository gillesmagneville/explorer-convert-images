// SPDX-License-Identifier: GPL-3.0-only

using System.Text.RegularExpressions;
using ConvertImages.Core.Localization;

namespace ConvertImages.Core.Tests;

internal static partial class LocalizationTests
{
    [GeneratedRegex(@"\{(\w+)\}")]
    private static partial Regex Placeholder();

    private static SortedSet<string> Placeholders(string text) =>
        new(Placeholder().Matches(text).Select(m => m.Groups[1].Value), StringComparer.Ordinal);

    private static PoCatalog Catalog(string language) =>
        Translator.LoadCatalog(language) ?? throw new TestFailure($"no embedded catalog for {language}");

    // ------------------------------------------------------------ parser

    [Test]
    private static void Parser_reads_plain_plural_multiline_and_escaped_entries()
    {
        const string po = """
            # A comment.
            msgid ""
            msgstr ""
            "Language: xx\n"
            "Plural-Forms: nplurals=2; plural=(n != 1);\n"

            #: some/source.py:12
            msgid "Quit"
            msgstr "Quitter"

            #, python-brace-format
            msgid "{n} file"
            msgid_plural "{n} files"
            msgstr[0] "{n} fichier"
            msgstr[1] "{n} fichiers"

            msgid ""
            "A long text "
            "on two lines.\n"
            msgstr ""
            "Un long texte "
            "sur deux lignes.\n"

            msgid "say \"hi\"\tnow \\ then"
            msgstr "dire \"salut\"\tmaintenant \\ puis"

            msgid "not translated"
            msgstr ""
            """;
        var catalog = PoCatalog.Parse("xx", po);

        Check.Equal("Quitter", catalog.Find("Quit"));
        Check.Equal("{n} fichier", catalog.FindPlural("{n} file", 1));
        Check.Equal("{n} fichiers", catalog.FindPlural("{n} file", 2));
        Check.Equal("{n} fichiers", catalog.FindPlural("{n} file", 0), "plural=(n != 1): 0 is plural");
        Check.Equal("{n} files", catalog.PluralIds["{n} file"]);
        Check.Equal("Un long texte sur deux lignes.\n", catalog.Find("A long text on two lines.\n"));
        Check.Equal("dire \"salut\"\tmaintenant \\ puis", catalog.Find("say \"hi\"\tnow \\ then"));
        Check.Equal<string?>(null, catalog.Find("not translated"), "an empty msgstr is no translation");
        Check.Equal<string?>(null, catalog.Find("never heard of"));
        Check.Equal(5, catalog.MessageIds.Count, "the header is not an entry");
    }

    [Test]
    private static void Parser_applies_the_french_plural_rule()
    {
        const string po = """
            msgid ""
            msgstr ""
            "Plural-Forms: nplurals=2; plural=(n > 1);\n"

            msgid "{n} file"
            msgid_plural "{n} files"
            msgstr[0] "{n} fichier"
            msgstr[1] "{n} fichiers"
            """;
        var catalog = PoCatalog.Parse("fr", po);
        Check.Equal("{n} fichier", catalog.FindPlural("{n} file", 0), "French: 0 is singular");
        Check.Equal("{n} fichier", catalog.FindPlural("{n} file", 1));
        Check.Equal("{n} fichiers", catalog.FindPlural("{n} file", 2));
    }

    [Test]
    private static void Parser_ignores_fuzzy_translations_and_windows_line_endings()
    {
        var po = "msgid \"a\"\r\nmsgstr \"A\"\r\n\r\n#, fuzzy\r\nmsgid \"b\"\r\nmsgstr \"B?\"\r\n";
        var catalog = PoCatalog.Parse("xx", po);
        Check.Equal("A", catalog.Find("a"));
        Check.Equal<string?>(null, catalog.Find("b"), "a fuzzy translation must not be used");
    }

    // ---------------------------------------------------------- catalogs

    [Test]
    private static void Catalogs_translate_every_text_and_nothing_else()
    {
        var expected = new SortedSet<string>(PotFile.Entries().Select(e => e.Id), StringComparer.Ordinal);
        Check.True(expected.Count >= 60, $"only {expected.Count} texts found in Msg");

        foreach (var language in Translator.TranslatedLanguages)
        {
            var catalog = Catalog(language);
            var actual = new SortedSet<string>(catalog.MessageIds, StringComparer.Ordinal);
            Check.Sequence(expected.Except(actual), [], $"{language}.po: texts of Msg.cs missing from the catalog");
            Check.Sequence(actual.Except(expected), [], $"{language}.po: entries that Msg.cs no longer has");

            foreach (var entry in PotFile.Entries())
            {
                var forms = catalog.Forms(entry.Id);
                Check.Equal(entry.Plural is null ? 1 : 2, forms.Count, $"{language}.po: number of forms of \"{entry.Id}\"");
                Check.True(forms.All(f => f.Length > 0), $"{language}.po: \"{entry.Id}\" is not translated");
                if (entry.Plural is not null)
                {
                    Check.Equal(entry.Plural, catalog.PluralIds[entry.Id], $"{language}.po: msgid_plural of \"{entry.Id}\"");
                }
            }
        }
    }

    [Test]
    private static void Catalogs_keep_the_placeholders_and_the_surrounding_line_breaks()
    {
        foreach (var language in Translator.TranslatedLanguages)
        {
            var catalog = Catalog(language);
            foreach (var entry in PotFile.Entries())
            {
                var forms = catalog.Forms(entry.Id);
                for (var i = 0; i < forms.Count; i++)
                {
                    var source = i == 0 ? entry.Id : entry.Plural!;
                    var what = $"{language}.po, \"{entry.Id}\" (form {i})";
                    Check.Sequence(Placeholders(source), Placeholders(forms[i]), $"{what}: placeholders differ");
                    Check.Equal(source.StartsWith('\n'), forms[i].StartsWith('\n'), $"{what}: leading line break");
                    Check.Equal(source.EndsWith('\n'), forms[i].EndsWith('\n'), $"{what}: trailing line break");
                }
            }
        }
    }

    [Test]
    private static void Template_file_is_up_to_date_with_the_code()
    {
        Check.True(File.Exists(PotFile.DefaultPath), $"missing {PotFile.DefaultPath}");
        var onDisk = File.ReadAllText(PotFile.DefaultPath).Replace("\r\n", "\n");
        Check.True(
            onDisk == PotFile.Generate().Replace("\r\n", "\n"),
            "po/explorer-convert-images.pot is out of date: run\n"
            + "      dotnet run -c Release --project app/ConvertImages.Core.Tests -- --write-pot");
    }

    // -------------------------------------------------------- translator

    [Test]
    private static void Translator_uses_the_catalog_of_the_language()
    {
        Check.Equal("Quitter", Translator.ForLanguage("fr").T(Msg.Quit));
        Check.Equal("Beenden", Translator.ForLanguage("de").T(Msg.Quit));
        Check.Equal("Salir", Translator.ForLanguage("es").T(Msg.Quit));
        Check.Equal("fr", Translator.ForLanguage("fr").Language);
    }

    [Test]
    private static void Translator_uses_the_base_language_of_a_regional_variant()
    {
        Check.Equal("Quitter", Translator.ForLanguage("fr-CA").T(Msg.Quit));
        Check.Equal("Beenden", Translator.ForLanguage("de_AT").T(Msg.Quit));
        Check.Equal("Salir", Translator.ForLanguage("ES-mx").T(Msg.Quit));
    }

    [Test]
    private static void Translator_falls_back_to_english()
    {
        foreach (var language in new[] { "en", "en-US", "ja", "pt-BR", "", null })
        {
            var translator = Translator.ForLanguage(language);
            Check.Equal("en", translator.Language, $"language \"{language}\"");
            Check.Equal("Quit", translator.T(Msg.Quit));
            Check.Equal("3 files detected: 3 PNG",
                translator.TN(Msg.FilesDetected, 3, ("n", 3), ("details", "3 PNG")));
            Check.Equal("1 file detected: 1 PNG",
                translator.TN(Msg.FilesDetected, 1, ("n", 1), ("details", "1 PNG")));
        }
    }

    [Test]
    private static void Translator_picks_the_plural_form_of_each_language()
    {
        string Detected(string language, int n) =>
            Translator.ForLanguage(language).TN(Msg.FilesDetected, n, ("n", n), ("details", "x"));

        Check.Equal("0 fichier détecté : x", Detected("fr", 0));
        Check.Equal("1 fichier détecté : x", Detected("fr", 1));
        Check.Equal("2 fichiers détectés : x", Detected("fr", 2));
        Check.True(Detected("de", 0) == Detected("de", 2).Replace("2", "0"), "German: 0 is plural");
        Check.True(Detected("es", 1) != Detected("es", 2).Replace("2", "1"), "Spanish: 1 is singular");
    }

    [Test]
    private static void Translator_forced_language_variable_wins()
    {
        var before = Environment.GetEnvironmentVariable(Translator.LanguageVariable);
        try
        {
            Environment.SetEnvironmentVariable(Translator.LanguageVariable, "de");
            Check.Equal("de", Translator.ForCurrentUser().Language);
            Environment.SetEnvironmentVariable(Translator.LanguageVariable, "en");
            Check.Equal("en", Translator.ForCurrentUser().Language);
        }
        finally
        {
            Environment.SetEnvironmentVariable(Translator.LanguageVariable, before);
        }
    }

    [Test]
    private static void Format_fills_the_known_placeholders_only()
    {
        Check.Equal("a → b", Translator.Format("{input} → {output}", ("input", "a"), ("output", "b")));
        Check.Equal("12 of 40", Translator.Format("{n} of {total}", ("n", 12), ("total", 40)));
        Check.Equal("{unknown} stays, x goes", Translator.Format("{unknown} stays, {known} goes", ("known", "x")));
        Check.Equal("no field {here}", Translator.Format("no field {here}"));
        Check.Equal("an open { brace", Translator.Format("an open { brace", ("brace", "x")));
        // A value is never scanned again: braces in a file name are harmless.
        Check.Equal("[OK] {output}.jpg → x", Translator.Format("[OK] {input} → {output}", ("input", "{output}.jpg"), ("output", "x")));
    }

    [Test]
    private static void Menu_label_is_the_same_text_as_the_context_menu_handler_uses()
    {
        // shell-verb/src/labels.rs checks its labels against the catalogs for
        // this very msgid: renaming it here must fail there too.
        var labels = File.ReadAllText(Repo.File_("shell-verb", "src", "labels.rs"));
        Check.True(labels.Contains($"\"{Msg.MenuLabel}\""), "Msg.MenuLabel is not the msgid used by labels.rs");
    }
}
