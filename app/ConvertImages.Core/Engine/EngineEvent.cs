// SPDX-License-Identifier: GPL-3.0-only

using System.Text.Json;

namespace ConvertImages.Core.Engine;

/// <summary>
/// One line of the engine's standard output. The engine reports everything
/// as JSON objects, one per line, with an <c>event</c> field (see
/// ARCHITECTURE.md, "Command-line interface").
/// </summary>
public abstract record EngineEvent;

/// <summary><c>detect</c>: a file and the format recognized for it.</summary>
public sealed record FileDetected(string Path, string Format, bool IsRaw) : EngineEvent;

/// <summary><c>detect</c>: final count, per format label.</summary>
public sealed record DetectSummary(int Total, IReadOnlyDictionary<string, int> Counts) : EngineEvent;

/// <summary><c>convert</c>: the conversion starts, on <c>Total</c> files.</summary>
public sealed record ConvertStarted(int Total) : EngineEvent;

/// <summary><c>convert</c>: one file is done, successfully or not.</summary>
public sealed record FileConverted(
    int Index, int Total, string Input, string? Output, bool Ok, string? Message) : EngineEvent;

/// <summary><c>convert</c>: final count.</summary>
public sealed record ConvertSummary(int Total, int Ok, int Errors, string OutputDir) : EngineEvent;

/// <summary>The engine could not even start its work.</summary>
public sealed record FatalError(string Message) : EngineEvent;

public static class EngineEventParser
{
    /// <summary>
    /// Reads one output line. Returns null for anything that is not an event
    /// this version knows: such a line is simply ignored.
    /// </summary>
    public static EngineEvent? Parse(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return null;
        }
        try
        {
            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }
            return Text(root, "event") switch
            {
                "file" => new FileDetected(
                    Text(root, "path") ?? "", Text(root, "format") ?? "", Flag(root, "is_raw")),
                "start" => new ConvertStarted(Number(root, "total")),
                "file_done" => new FileConverted(
                    Number(root, "index"),
                    Number(root, "total"),
                    Text(root, "input") ?? "",
                    Text(root, "output"),
                    Text(root, "status") == "ok",
                    Text(root, "message")),
                // `detect` and `convert` both end with a "summary": the one of
                // `detect` carries the per-format counts.
                "summary" when root.TryGetProperty("counts", out var counts) =>
                    new DetectSummary(Number(root, "total"), Counts(counts)),
                "summary" => new ConvertSummary(
                    Number(root, "total"),
                    Number(root, "ok"),
                    Number(root, "errors"),
                    Text(root, "output_dir") ?? ""),
                "fatal_error" => new FatalError(Text(root, "message") ?? ""),
                _ => null,
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? Text(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static int Number(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.TryGetInt32(out var number) ? number : 0;

    private static bool Flag(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.True;

    private static Dictionary<string, int> Counts(JsonElement counts)
    {
        var result = new Dictionary<string, int>(StringComparer.Ordinal);
        if (counts.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in counts.EnumerateObject())
            {
                if (property.Value.TryGetInt32(out var count))
                {
                    result[property.Name] = count;
                }
            }
        }
        return result;
    }
}
