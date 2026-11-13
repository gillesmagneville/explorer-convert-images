// SPDX-License-Identifier: GPL-3.0-only

using System.Text;
using System.Text.RegularExpressions;
using ConvertImages.Core.Localization;

namespace ConvertImages.Core.Engine;

/// <summary>
/// Translates what the engine reports. The engine always writes in English;
/// its messages are recognized here from the patterns of
/// <see cref="Msg.EngineMessageTemplates"/> and rebuilt in the language of
/// the user. The variable parts (a detail coming from a library, a file
/// name…) are kept as they are; a message that matches no pattern is shown
/// unchanged.
/// </summary>
public sealed partial class EngineMessageTranslator
{
    private readonly Translator _translator;
    private readonly List<(Regex Pattern, string Template)> _patterns = [];

    public EngineMessageTranslator(Translator translator)
    {
        _translator = translator;
        foreach (var template in Msg.EngineMessageTemplates)
        {
            _patterns.Add((PatternFor(template), template));
        }
    }

    /// <summary>Translation of an error message of the engine.</summary>
    public string Translate(string? message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return message ?? "";
        }
        foreach (var (pattern, template) in _patterns)
        {
            var match = pattern.Match(message);
            if (!match.Success)
            {
                continue;
            }
            // A captured part may itself be a known message (the two errors
            // joined by "LibRaw fallback"): translate it the same way.
            var fields = pattern.GetGroupNames()
                .Where(name => !int.TryParse(name, out _))
                .Select(name => (name, (object?)Translate(match.Groups[name].Value)))
                .ToArray();
            return Translator.Format(_translator.T(template), fields);
        }
        return message;
    }

    /// <summary>
    /// Translation of a format label of the engine: only "Unknown" and
    /// "Generic RAW" are words; the others are proper names ("RAW Canon (CR2)").
    /// </summary>
    public string TranslateFormatLabel(string label) => label switch
    {
        Msg.LabelUnknown => _translator.T(Msg.LabelUnknown),
        Msg.LabelGenericRaw => _translator.T(Msg.LabelGenericRaw),
        _ => label,
    };

    /// <summary>
    /// The pattern recognizing the messages built from <paramref name="template"/>:
    /// its literal parts as they are, each <c>{name}</c> matching any text.
    /// </summary>
    private static Regex PatternFor(string template)
    {
        var pattern = new StringBuilder("^");
        var position = 0;
        foreach (Match placeholder in Placeholder().Matches(template))
        {
            pattern.Append(Regex.Escape(template[position..placeholder.Index]));
            pattern.Append("(?<").Append(placeholder.Groups[1].Value).Append(">.*?)");
            position = placeholder.Index + placeholder.Length;
        }
        pattern.Append(Regex.Escape(template[position..])).Append('$');
        return new Regex(pattern.ToString(), RegexOptions.Singleline | RegexOptions.CultureInvariant);
    }

    [GeneratedRegex(@"\{(\w+)\}")]
    private static partial Regex Placeholder();
}
