// SPDX-License-Identifier: GPL-3.0-only

using ConvertImages.Core;
using Microsoft.UI.Xaml;

namespace ExplorerConvertImages;

/// <summary>
/// Entry point of the application. It is started by the context-menu handler
/// (<c>explorer-convert-images-verb.exe</c>) with <c>--input-list FILE</c>,
/// FILE listing the selected files and folders; it can also be started by
/// hand, with paths as arguments or with nothing at all.
/// </summary>
public partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();
        UnhandledException += (_, e) => CrashLog.Write(e.Exception);
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        // The first element is the path of the program itself.
        var arguments = AppArguments.Parse(Environment.GetCommandLineArgs().Skip(1).ToArray());
        _window = new MainWindow(arguments);
        _window.Activate();
    }
}

/// <summary>
/// Last-resort trace: an error nobody handled is appended to
/// <c>explorer-convert-images-app.log</c> in the temporary folder of the
/// user before the application stops, so that a crash can be reported with
/// something to go on.
/// </summary>
internal static class CrashLog
{
    public static void Write(Exception exception)
    {
        try
        {
            var file = Path.Combine(Path.GetTempPath(), "explorer-convert-images-app.log");
            File.AppendAllText(file, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {exception}{Environment.NewLine}");
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Nothing more can be done.
        }
    }
}
