// SPDX-License-Identifier: GPL-3.0-only

using System.Text;

namespace ConvertImages.Core;

/// <summary>What the application was started with.</summary>
/// <param name="Paths">Files and folders given directly on the command line.</param>
/// <param name="InputListFile">
/// File listing more of them, one per line (<c>--input-list</c>): this is how
/// the context-menu handler passes a selection of any size.
/// </param>
public sealed record AppArguments(IReadOnlyList<string> Paths, string? InputListFile)
{
    public const string InputListOption = "--input-list";

    public static AppArguments Parse(IReadOnlyList<string> arguments)
    {
        var paths = new List<string>();
        string? inputList = null;
        for (var i = 0; i < arguments.Count; i++)
        {
            if (arguments[i] == InputListOption && i + 1 < arguments.Count)
            {
                inputList = arguments[++i];
            }
            else if (arguments[i].Length > 0)
            {
                paths.Add(arguments[i]);
            }
        }
        return new AppArguments(paths, inputList);
    }
}

/// <summary>The files and folders to convert, and the list file given to the engine.</summary>
public static class Selection
{
    /// <summary>
    /// Parses the content of a list file: one path per line, UTF-8, with or
    /// without a byte order mark, with Unix or Windows line endings. Empty
    /// lines are ignored. Same rules as <c>--input-list</c> in the engine.
    /// </summary>
    public static List<string> ParseList(string content)
    {
        var paths = new List<string>();
        foreach (var rawLine in content.TrimStart('\uFEFF').Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            if (!string.IsNullOrWhiteSpace(line))
            {
                paths.Add(line);
            }
        }
        return paths;
    }

    /// <summary>
    /// All the paths the application was started with: those of the command
    /// line, then those of the list file. An unreadable list counts as empty.
    /// </summary>
    public static List<string> Gather(AppArguments arguments)
    {
        var paths = new List<string>(arguments.Paths);
        if (arguments.InputListFile is { } file)
        {
            try
            {
                paths.AddRange(ParseList(File.ReadAllText(file, Encoding.UTF8)));
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // Nothing to add.
            }
        }
        return paths;
    }

    /// <summary>
    /// Writes <paramref name="paths"/> to a new list file in the temporary
    /// folder of the user and returns its path. The engine always receives the
    /// selection this way: a Windows command line is limited to about 32,000
    /// characters, far too few for a large selection.
    /// </summary>
    public static string WriteListFile(IEnumerable<string> paths)
    {
        var file = Path.Combine(
            Path.GetTempPath(),
            $"explorer-convert-images-selection-{Environment.ProcessId}-{Guid.NewGuid():N}.txt");
        // UTF-8 without byte order mark, one path per line.
        File.WriteAllLines(file, paths, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        return file;
    }

    /// <summary>
    /// Folder proposed as destination: the deepest folder containing the whole
    /// selection (a selected folder counts as itself, a selected file as the
    /// folder it is in). When nothing is common — files on different drives —
    /// the folder of the first item is used.
    /// </summary>
    /// <param name="paths">The selected files and folders.</param>
    /// <param name="isDirectory">Tells whether a path is a folder (the file system, by default).</param>
    /// <param name="fallback">Returned when the selection is empty.</param>
    public static string CommonParentDirectory(
        IReadOnlyList<string> paths, Func<string, bool>? isDirectory = null, string? fallback = null)
    {
        isDirectory ??= Directory.Exists;
        var folders = new List<SplitPath>();
        foreach (var path in paths)
        {
            var split = SplitPath.Parse(path);
            folders.Add(isDirectory(path) ? split : split.Parent());
        }
        if (folders.Count == 0)
        {
            return fallback ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        var first = folders[0];
        var common = first.Parts.Count;
        foreach (var folder in folders.Skip(1))
        {
            if (!string.Equals(folder.Root, first.Root, first.Comparison))
            {
                return first.ToString();
            }
            var shared = 0;
            while (shared < common && shared < folder.Parts.Count
                && string.Equals(folder.Parts[shared], first.Parts[shared], first.Comparison))
            {
                shared++;
            }
            common = shared;
        }
        return new SplitPath(first.Root, first.Parts.Take(common).ToList(), first.Separator, first.Comparison)
            .ToString();
    }

    /// <summary>
    /// A path cut into its root (<c>C:\</c>, <c>\\server\share\</c> or <c>/</c>)
    /// and its components. Done by hand rather than with
    /// <see cref="Path"/> so that Windows paths are understood the same way on
    /// any system (the tests also run on Linux).
    /// </summary>
    private sealed record SplitPath(
        string Root, IReadOnlyList<string> Parts, char Separator, StringComparison Comparison)
    {
        public static SplitPath Parse(string path)
        {
            // \\server\share\… (also written with forward slashes)
            if (path.Length > 2 && IsSlash(path[0]) && IsSlash(path[1]))
            {
                var parts = Split(path[2..], windows: true);
                var rootParts = parts.Take(2).ToList();
                return new SplitPath(
                    @"\\" + string.Join('\\', rootParts) + @"\",
                    parts.Skip(2).ToList(), '\\', StringComparison.OrdinalIgnoreCase);
            }
            // C:\…
            if (path.Length >= 2 && char.IsAsciiLetter(path[0]) && path[1] == ':')
            {
                return new SplitPath(
                    path[..2] + @"\", Split(path[2..], windows: true), '\\', StringComparison.OrdinalIgnoreCase);
            }
            // /… (Unix)
            return new SplitPath(
                path.StartsWith('/') ? "/" : "", Split(path, windows: false), '/', StringComparison.Ordinal);
        }

        public SplitPath Parent() =>
            Parts.Count == 0 ? this : this with { Parts = Parts.Take(Parts.Count - 1).ToList() };

        public override string ToString() => Root + string.Join(Separator, Parts);

        private static bool IsSlash(char c) => c is '\\' or '/';

        private static List<string> Split(string text, bool windows) =>
            text.Split(windows ? new[] { '\\', '/' } : new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).ToList();
    }
}
