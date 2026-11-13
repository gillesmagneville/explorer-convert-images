// SPDX-License-Identifier: GPL-3.0-only

using System.Globalization;
using System.Reflection;
using System.Text;

namespace ConvertImages.Core.Localization;

/// <summary>
/// Translates the texts of <see cref="Msg"/> into the language of the user.
/// The source texts are in English; without a catalog for the language (or
/// without a translation for a text), English is shown.
/// </summary>
public sealed class Translator
{
    /// <summary>Languages that have a catalog, English (the source) aside.</summary>
    public static readonly IReadOnlyList<string> TranslatedLanguages = ["fr", "de", "es"];

    /// <summary>
    /// Environment variable that forces the language ("fr", "de", "es", "en"),
    /// for development and tests.
    /// </summary>
    public const string LanguageVariable = "CONVERT_IMAGES_LANGUAGE";

    private readonly PoCatalog? _catalog;

    public Translator(PoCatalog? catalog) => _catalog = catalog;

    /// <summary>Language actually used: a catalog language, or "en".</summary>
    public string Language => _catalog?.Language ?? "en";

    /// <summary>
    /// Translator for a language code or culture name ("fr", "fr-CA", "de_DE"…).
    /// A regional variant uses its base language; anything else gives English.
    /// </summary>
    public static Translator ForLanguage(string? language)
    {
        var code = (language ?? "").Split('-', '_')[0].Trim().ToLowerInvariant();
        return new Translator(TranslatedLanguages.Contains(code) ? LoadCatalog(code) : null);
    }

    /// <summary>Translator for the display language of the current user.</summary>
    public static Translator ForCurrentUser()
    {
        var forced = Environment.GetEnvironmentVariable(LanguageVariable);
        return ForLanguage(string.IsNullOrWhiteSpace(forced)
            ? CultureInfo.CurrentUICulture.TwoLetterISOLanguageName
            : forced);
    }

    /// <summary>Reads the catalog of <paramref name="code"/> embedded in this library.</summary>
    public static PoCatalog? LoadCatalog(string code)
    {
        var assembly = typeof(Translator).GetTypeInfo().Assembly;
        using var stream = assembly.GetManifestResourceStream($"po/{code}.po");
        if (stream is null)
        {
            return null;
        }
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return PoCatalog.Parse(code, reader.ReadToEnd());
    }

    /// <summary>Translation of <paramref name="msgid"/>.</summary>
    public string T(string msgid) => _catalog?.Find(msgid) ?? msgid;

    /// <summary>
    /// Translation of <paramref name="msgid"/> with its <c>{name}</c>
    /// placeholders filled in.
    /// </summary>
    public string T(string msgid, params (string Name, object? Value)[] fields) =>
        Format(T(msgid), fields);

    /// <summary>
    /// Translation of a text that depends on a quantity, with its
    /// placeholders filled in.
    /// </summary>
    public string TN(PluralMsg msg, long n, params (string Name, object? Value)[] fields)
    {
        var text = _catalog?.FindPlural(msg.Singular, n) ?? (n == 1 ? msg.Singular : msg.Plural);
        return Format(text, fields);
    }

    /// <summary>
    /// Replaces each <c>{name}</c> of <paramref name="template"/> by the value
    /// of the field of that name. An unknown placeholder is left as it is.
    /// </summary>
    public static string Format(string template, params (string Name, object? Value)[] fields)
    {
        if (fields.Length == 0 || !template.Contains('{'))
        {
            return template;
        }
        var result = new StringBuilder(template.Length + 32);
        var position = 0;
        while (position < template.Length)
        {
            var open = template.IndexOf('{', position);
            var close = open < 0 ? -1 : template.IndexOf('}', open + 1);
            if (close < 0)
            {
                break;
            }
            result.Append(template, position, open - position);
            var name = template.AsSpan(open + 1, close - open - 1);
            var known = false;
            foreach (var (fieldName, value) in fields)
            {
                if (name.SequenceEqual(fieldName))
                {
                    result.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
                    known = true;
                    break;
                }
            }
            if (!known)
            {
                result.Append(template, open, close - open + 1);
            }
            position = close + 1;
        }
        result.Append(template, position, template.Length - position);
        return result.ToString();
    }
}
