// SPDX-License-Identifier: GPL-3.0-only

using System.Reflection;
using System.Text;
using ConvertImages.Core.Localization;

namespace ConvertImages.Core.Tests;

/// <summary>
/// The translation template (<c>po/explorer-convert-images.pot</c>),
/// generated from <see cref="Msg"/>: the starting point of a new language,
/// and what <c>msgmerge</c> updates the existing catalogs from.
/// </summary>
internal static class PotFile
{
    public static string DefaultPath => Repo.File_("po", "explorer-convert-images.pot");

    /// <summary>One text to translate, as declared in <see cref="Msg"/>.</summary>
    public sealed record Entry(string Id, string? Plural, string? Comment);

    /// <summary>The texts of <see cref="Msg"/>, in declaration order.</summary>
    public static List<Entry> Entries()
    {
        var entries = new List<Entry>();
        var fields = typeof(Msg).GetFields(BindingFlags.Public | BindingFlags.Static)
            .OrderBy(field => field.MetadataToken);
        foreach (var field in fields)
        {
            var comment = field.GetCustomAttribute<TranslatorCommentAttribute>()?.Text;
            if (field.IsLiteral && field.FieldType == typeof(string))
            {
                entries.Add(new Entry((string)field.GetRawConstantValue()!, null, comment));
            }
            else if (field.FieldType == typeof(PluralMsg))
            {
                var msg = (PluralMsg)field.GetValue(null)!;
                entries.Add(new Entry(msg.Singular, msg.Plural, comment));
            }
        }
        return entries;
    }

    public static string Generate()
    {
        var pot = new StringBuilder();
        pot.Append(
            """
            # Translation template for explorer-convert-images.
            # Copyright (C) Gilles MAGNEVILLE
            # This file is distributed under the same license as the explorer-convert-images package.
            #
            # Generated from app/ConvertImages.Core/Localization/Msg.cs — do not edit by hand:
            #   dotnet run -c Release --project app/ConvertImages.Core.Tests -- --write-pot
            #
            msgid ""
            msgstr ""
            "Project-Id-Version: explorer-convert-images\n"
            "Report-Msgid-Bugs-To: https://github.com/gillesmagneville/explorer-convert-images/issues\n"
            "Language: \n"
            "MIME-Version: 1.0\n"
            "Content-Type: text/plain; charset=UTF-8\n"
            "Content-Transfer-Encoding: 8bit\n"
            "Plural-Forms: nplurals=INTEGER; plural=EXPRESSION;\n"

            """);
        foreach (var entry in Entries())
        {
            pot.Append('\n');
            if (entry.Comment is not null)
            {
                pot.Append("#. TRANSLATORS: ").Append(entry.Comment).Append('\n');
            }
            if (entry.Id.Contains('{'))
            {
                pot.Append("#, python-brace-format\n");
            }
            pot.Append("msgid ").Append(Quote(entry.Id)).Append('\n');
            if (entry.Plural is null)
            {
                pot.Append("msgstr \"\"\n");
            }
            else
            {
                pot.Append("msgid_plural ").Append(Quote(entry.Plural)).Append('\n');
                pot.Append("msgstr[0] \"\"\nmsgstr[1] \"\"\n");
            }
        }
        return pot.ToString();
    }

    private static string Quote(string text) =>
        "\"" + text.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\t", "\\t") + "\"";
}
