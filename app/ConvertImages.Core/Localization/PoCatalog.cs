// SPDX-License-Identifier: GPL-3.0-only

using System.Text;

namespace ConvertImages.Core.Localization;

/// <summary>
/// One gettext catalog (a <c>.po</c> file) read as text: the translations of
/// one language. Only what the catalogs of this project use is supported:
/// plain and plural entries, multi-line strings, the usual escapes, comments,
/// and the <c>fuzzy</c> flag (a fuzzy translation is ignored, as gettext does).
/// </summary>
public sealed class PoCatalog
{
    private readonly Dictionary<string, string[]> _translations = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _plurals = new(StringComparer.Ordinal);
    private bool _pluralFromTwo;

    private PoCatalog(string language) => Language = language;

    /// <summary>Language code of the catalog ("fr", "de"…).</summary>
    public string Language { get; }

    /// <summary>The msgids of the catalog (header excluded).</summary>
    public IReadOnlyCollection<string> MessageIds => _translations.Keys;

    /// <summary>The msgid_plural of each plural entry, by msgid.</summary>
    public IReadOnlyDictionary<string, string> PluralIds => _plurals;

    /// <summary>All the forms recorded for <paramref name="msgid"/> (empty if unknown).</summary>
    public IReadOnlyList<string> Forms(string msgid) =>
        _translations.TryGetValue(msgid, out var forms) ? forms : [];

    /// <summary>Translation of <paramref name="msgid"/>, or null when there is none.</summary>
    public string? Find(string msgid) => Pick(msgid, 0);

    /// <summary>Translation of a plural entry for the quantity <paramref name="n"/>.</summary>
    public string? FindPlural(string msgid, long n) => Pick(msgid, PluralIndex(n));

    /// <summary>
    /// Index of the plural form for <paramref name="n"/>. The catalogs of this
    /// project have two forms, with one of two rules: plural from 2 (French:
    /// "0 fichier") or plural for everything but 1 (English, German, Spanish).
    /// </summary>
    public int PluralIndex(long n) => _pluralFromTwo ? (n > 1 ? 1 : 0) : (n != 1 ? 1 : 0);

    private string? Pick(string msgid, int index)
    {
        if (!_translations.TryGetValue(msgid, out var forms) || index >= forms.Length)
        {
            return null;
        }
        return forms[index].Length == 0 ? null : forms[index];
    }

    /// <summary>Parses the text of a <c>.po</c> file.</summary>
    public static PoCatalog Parse(string language, string poText)
    {
        var catalog = new PoCatalog(language);
        var entry = new Entry();

        foreach (var rawLine in poText.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0)
            {
                catalog.Add(entry);
                entry = new Entry();
            }
            else if (line.StartsWith("#,", StringComparison.Ordinal))
            {
                entry.Fuzzy |= line.Contains("fuzzy", StringComparison.Ordinal);
            }
            else if (line[0] == '#')
            {
                // Translator comment, source reference or obsolete entry.
            }
            else if (line.StartsWith("msgid_plural ", StringComparison.Ordinal))
            {
                entry.Begin(Field.IdPlural, line["msgid_plural ".Length..]);
            }
            else if (line.StartsWith("msgid ", StringComparison.Ordinal))
            {
                entry.Begin(Field.Id, line["msgid ".Length..]);
            }
            else if (line.StartsWith("msgstr[", StringComparison.Ordinal))
            {
                var close = line.IndexOf(']');
                if (close > 0 && int.TryParse(line.AsSpan("msgstr[".Length, close - "msgstr[".Length), out var index))
                {
                    entry.BeginForm(index, line[(close + 1)..]);
                }
            }
            else if (line.StartsWith("msgstr ", StringComparison.Ordinal))
            {
                entry.BeginForm(0, line["msgstr ".Length..]);
            }
            else if (line[0] == '"')
            {
                entry.Continue(line);
            }
        }
        catalog.Add(entry);
        return catalog;
    }

    private void Add(Entry entry)
    {
        if (entry.Id is null)
        {
            return;
        }
        var id = entry.Id.ToString();
        var forms = entry.Forms.Select(f => f?.ToString() ?? "").ToArray();
        if (id.Length == 0)
        {
            // The header: only the plural rule matters to us.
            var header = forms.Length > 0 ? forms[0] : "";
            _pluralFromTwo = header.Replace(" ", "").Contains("plural=(n>1)", StringComparison.Ordinal);
            return;
        }
        if (entry.Fuzzy)
        {
            forms = forms.Select(_ => "").ToArray();
        }
        _translations[id] = forms;
        if (entry.IdPlural is not null)
        {
            _plurals[id] = entry.IdPlural.ToString();
        }
    }

    private enum Field
    {
        None,
        Id,
        IdPlural,
        Form,
    }

    /// <summary>The entry being read.</summary>
    private sealed class Entry
    {
        private Field _current = Field.None;
        private int _currentForm;

        public StringBuilder? Id { get; private set; }

        public StringBuilder? IdPlural { get; private set; }

        public List<StringBuilder?> Forms { get; } = [];

        public bool Fuzzy { get; set; }

        public void Begin(Field field, string quoted)
        {
            _current = field;
            var text = new StringBuilder(Unquote(quoted));
            if (field == Field.Id)
            {
                Id = text;
            }
            else
            {
                IdPlural = text;
            }
        }

        public void BeginForm(int index, string quoted)
        {
            _current = Field.Form;
            _currentForm = index;
            while (Forms.Count <= index)
            {
                Forms.Add(null);
            }
            Forms[index] = new StringBuilder(Unquote(quoted));
        }

        public void Continue(string quoted)
        {
            var target = _current switch
            {
                Field.Id => Id,
                Field.IdPlural => IdPlural,
                Field.Form => Forms[_currentForm],
                _ => null,
            };
            target?.Append(Unquote(quoted));
        }
    }

    /// <summary>Content of a quoted .po string, escapes resolved.</summary>
    private static string Unquote(string quoted)
    {
        var text = quoted.Trim();
        if (text.Length < 2 || text[0] != '"' || text[^1] != '"')
        {
            return "";
        }
        var result = new StringBuilder(text.Length);
        for (var i = 1; i < text.Length - 1; i++)
        {
            var c = text[i];
            if (c == '\\' && i + 1 < text.Length - 1)
            {
                i++;
                result.Append(text[i] switch
                {
                    'n' => '\n',
                    't' => '\t',
                    'r' => '\r',
                    _ => text[i],
                });
            }
            else
            {
                result.Append(c);
            }
        }
        return result.ToString();
    }
}
