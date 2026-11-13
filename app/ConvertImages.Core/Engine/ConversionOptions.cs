// SPDX-License-Identifier: GPL-3.0-only

using System.Globalization;
using ConvertImages.Core.Localization;

namespace ConvertImages.Core.Engine;

/// <summary>A choice offered in a list: the value given to the engine and its text.</summary>
public sealed record Choice(string Id, string Msgid);

/// <summary>The lists of the options panel, in display order (the first one is the default).</summary>
public static class Choices
{
    /// <summary>Destination formats — JPEG first, the default.</summary>
    public static readonly IReadOnlyList<Choice> Formats =
    [
        new("jpeg", Msg.FormatJpeg),
        new("png", Msg.FormatPng),
        new("tiff", Msg.FormatTiff),
        new("bmp", Msg.FormatBmp),
        new("webp", Msg.FormatWebp),
    ];

    /// <summary>What to do when the destination file already exists.</summary>
    public static readonly IReadOnlyList<Choice> Collisions =
    [
        new("rename", Msg.CollisionRename),
        new("skip", Msg.CollisionSkip),
        new("overwrite", Msg.CollisionOverwrite),
    ];
}

/// <summary>What the user chose for a conversion.</summary>
public sealed record ConversionOptions(string Format, int Quality, string OnCollision, string OutputDir);

/// <summary>Builds the command lines of the engine.</summary>
public static class EngineArguments
{
    /// <summary>
    /// <c>detect</c> on the paths listed in <paramref name="inputList"/> (one
    /// per line), subfolders included.
    /// </summary>
    public static List<string> Detect(string inputList) =>
        ["detect", "--recursive", "--input-list", inputList];

    /// <summary><c>convert</c> on the same paths, with the chosen options.</summary>
    public static List<string> Convert(ConversionOptions options, string inputList) =>
    [
        "convert",
        "--recursive",
        "--input-list", inputList,
        "--output-dir", options.OutputDir,
        "--format", options.Format,
        "--quality", Math.Clamp(options.Quality, 1, 100).ToString(CultureInfo.InvariantCulture),
        "--on-collision", options.OnCollision,
    ];
}
