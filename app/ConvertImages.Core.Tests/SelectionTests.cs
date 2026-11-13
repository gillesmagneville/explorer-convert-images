// SPDX-License-Identifier: GPL-3.0-only

using System.Text;

namespace ConvertImages.Core.Tests;

internal static class SelectionTests
{
    [Test]
    private static void Arguments_split_paths_and_the_list_option()
    {
        var parsed = AppArguments.Parse(["a.jpg", "--input-list", @"C:\Temp\list.txt", @"D:\photos dir", ""]);
        Check.Sequence(["a.jpg", @"D:\photos dir"], parsed.Paths);
        Check.Equal(@"C:\Temp\list.txt", parsed.InputListFile);

        var none = AppArguments.Parse([]);
        Check.Equal(0, none.Paths.Count);
        Check.Equal<string?>(null, none.InputListFile);

        // The option without its value is not an option: nothing is lost.
        Check.Sequence(["--input-list"], AppArguments.Parse(["--input-list"]).Paths);
    }

    [Test]
    private static void List_accepts_bom_crlf_and_blank_lines()
    {
        var paths = Selection.ParseList("\uFEFFC:\\Photos d'été\\a b.cr2\r\n\r\n   \r\nD:\\x\\ü.nef\n/tmp/plain.jpg");
        Check.Sequence([@"C:\Photos d'été\a b.cr2", @"D:\x\ü.nef", "/tmp/plain.jpg"], paths);
        Check.Sequence([" a.jpg"], Selection.ParseList(" a.jpg\n"), "a leading space belongs to the name");
        Check.Equal(0, Selection.ParseList("").Count);
    }

    [Test]
    private static void List_file_round_trip_is_utf8_without_bom()
    {
        string[] paths = [@"C:\Photos d'été\château.jpg", @"\\nas\photos\ünïcode.tif"];
        var file = Selection.WriteListFile(paths);
        try
        {
            var bytes = File.ReadAllBytes(file);
            Check.True(bytes.Length > 3 && !(bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF), "no byte order mark expected");
            Check.Sequence(paths, Selection.ParseList(Encoding.UTF8.GetString(bytes)));
            Check.Sequence(
                ["first.png", .. paths],
                Selection.Gather(new AppArguments(["first.png"], file)),
                "command-line paths come first, then the list");
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Test]
    private static void Gather_treats_an_unreadable_list_as_empty()
    {
        var missing = Path.Combine(Path.GetTempPath(), "eci-no-such-list.txt");
        Check.Sequence(["a.jpg"], Selection.Gather(new AppArguments(["a.jpg"], missing)));
    }

    // -------------------------------------------------- destination folder

    /// <summary>Folders are the paths without a file extension, in these tests.</summary>
    private static bool LooksLikeFolder(string path) => !Path.HasExtension(path.Replace('\\', '/'));

    private static string Common(params string[] paths) =>
        Selection.CommonParentDirectory(paths, LooksLikeFolder, fallback: "FALLBACK");

    [Test]
    private static void Destination_is_the_folder_of_the_selected_files()
    {
        Check.Equal(@"C:\Photos\2026", Common(@"C:\Photos\2026\a.cr2", @"C:\Photos\2026\b.jpg"));
        Check.Equal(@"C:\Photos", Common(@"C:\Photos\2026\a.cr2", @"C:\Photos\2025\b.jpg"));
        Check.Equal(@"C:\", Common(@"C:\a.jpg"));
        Check.Equal(@"C:\", Common(@"C:\Photos\a.jpg", @"C:\Music\b.jpg"));
    }

    [Test]
    private static void Destination_of_a_selected_folder_is_that_folder()
    {
        Check.Equal(@"D:\Shoot", Common(@"D:\Shoot"));
        Check.Equal(@"D:\Shoot", Common(@"D:\Shoot", @"D:\Shoot\extra.png"));
        Check.Equal(@"D:\", Common(@"D:\Shoot", @"D:\Other"));
    }

    [Test]
    private static void Destination_ignores_letter_case_and_slash_style_on_windows_paths()
    {
        Check.Equal(@"C:\Photos\Été", Common(@"C:\Photos\Été\a.jpg", @"c:/photos/été/b.jpg"));
    }

    [Test]
    private static void Destination_falls_back_to_the_first_folder_across_drives()
    {
        Check.Equal(@"C:\Photos", Common(@"C:\Photos\a.jpg", @"D:\Photos\b.jpg"));
        Check.Equal(@"\\nas\photos\2026", Common(@"\\nas\photos\2026\a.jpg", @"C:\b.jpg"));
    }

    [Test]
    private static void Destination_handles_network_shares()
    {
        Check.Equal(@"\\nas\photos\2026", Common(@"\\nas\photos\2026\a.jpg", @"\\NAS\Photos\2026\b.jpg"));
        Check.Equal(@"\\nas\photos\", Common(@"\\nas\photos\a.jpg"));
        Check.Equal(@"\\nas\photos\", Common(@"\\nas\photos\a.jpg", @"\\nas\music\b.jpg"),
            "different shares are different roots: the first folder is used");
    }

    [Test]
    private static void Destination_handles_unix_paths_and_an_empty_selection()
    {
        Check.Equal("/home/zoé/photos", Common("/home/zoé/photos/a.jpg", "/home/zoé/photos/sub/b.jpg"));
        Check.Equal("/", Common("/a.jpg", "/b/c.jpg"));
        Check.Equal("FALLBACK", Common());
    }
}
