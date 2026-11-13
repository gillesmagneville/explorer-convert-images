// SPDX-License-Identifier: GPL-3.0-only

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using ConvertImages.Core;
using ConvertImages.Core.Engine;
using ConvertImages.Core.Localization;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;

namespace ExplorerConvertImages;

/// <summary>
/// The single window of the application. Opened on a selection of files and
/// folders, it:
///   1. has the engine detect the format of every image found (subfolders
///      included);
///   2. offers a destination format, a quality (JPEG), what to do when a file
///      already exists, and a destination folder (a new one can be created);
///   3. runs the conversion through the engine and shows its progress live.
/// The images themselves are never touched here: all the work is done by
/// <c>convert-images-engine.exe</c>, a separate process.
///
/// The layout is built in code rather than in XAML: everything about this
/// window — texts, layout, behaviour — is in this one file.
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly Translator _t = Translator.ForCurrentUser();
    private readonly EngineMessageTranslator _engineMessages;
    private readonly EngineRunner _engine = new(EngineRunner.DefaultEnginePath());

    /// <summary>Temporary selection files to delete when the window closes.</summary>
    private readonly List<string> _temporaryFiles = [];

    /// <summary>The file listing the selection, as given to the engine.</summary>
    private string? _listFile;

    private string _outputDir = "";
    private int _detectedCount;
    private EngineRun? _engineRun;
    private bool _conversionInProgress;
    private bool _dialogOpen;
    private bool _closeConfirmed;

    // Text of the log; shown in batches so that a large conversion (one line
    // per file) does not redraw the window thousands of times.
    private readonly StringBuilder _log = new();
    private readonly DispatcherQueueTimer _logTimer;

    private readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap };
    private readonly ProgressBar _detectProgress = new() { IsIndeterminate = true };
    private readonly Button _chooseFolderButton = new() { Visibility = Visibility.Collapsed };
    private readonly Grid _options = new() { ColumnSpacing = 12, RowSpacing = 10, Visibility = Visibility.Collapsed };
    private readonly ComboBox _format = new() { HorizontalAlignment = HorizontalAlignment.Stretch };
    private readonly TextBlock _qualityLabel = new() { VerticalAlignment = VerticalAlignment.Center };
    private readonly Grid _qualityRow = new() { ColumnSpacing = 12 };
    private readonly Slider _quality = new() { Minimum = 1, Maximum = 100, StepFrequency = 1, Value = 100 };
    private readonly TextBlock _qualityValue = new() { VerticalAlignment = VerticalAlignment.Center, MinWidth = 28 };
    private readonly ComboBox _collision = new() { HorizontalAlignment = HorizontalAlignment.Stretch };
    private readonly TextBlock _destination = new()
    {
        VerticalAlignment = VerticalAlignment.Center,
        TextTrimming = TextTrimming.CharacterEllipsis,
    };
    private readonly StackPanel _actions = new()
    {
        Orientation = Orientation.Horizontal,
        HorizontalAlignment = HorizontalAlignment.Right,
        Spacing = 8,
        Visibility = Visibility.Collapsed,
    };
    private readonly Button _cancelButton = new() { Visibility = Visibility.Collapsed };
    private readonly Button _convertButton = new();
    private readonly ProgressBar _convertProgress = new() { Minimum = 0, Maximum = 1, Visibility = Visibility.Collapsed };
    private readonly TextBlock _logText = new()
    {
        TextWrapping = TextWrapping.Wrap,
        IsTextSelectionEnabled = true,
        FontFamily = new FontFamily("Cascadia Mono, Consolas"),
    };
    private readonly ScrollViewer _logScroll = new()
    {
        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        Visibility = Visibility.Collapsed,
    };
    private readonly Button _openDestinationButton = new() { Visibility = Visibility.Collapsed };
    private readonly Button _quitButton = new() { HorizontalAlignment = HorizontalAlignment.Right };

    public MainWindow(AppArguments arguments)
    {
        _engineMessages = new EngineMessageTranslator(_t);

        Title = _t.T(Msg.WindowTitle);
        SystemBackdrop = new MicaBackdrop();
        SetUpWindowFrame();

        Content = BuildLayout();

        _logTimer = DispatcherQueue.CreateTimer();
        _logTimer.Interval = TimeSpan.FromMilliseconds(150);
        _logTimer.IsRepeating = false;
        _logTimer.Tick += (_, _) => ShowLog();

        AppWindow.Closing += OnWindowClosing;
        Closed += (_, _) => CleanUp();

        var paths = Selection.Gather(arguments);
        if (arguments.InputListFile is { } givenList)
        {
            // Written for us by the context-menu handler: ours to delete.
            _temporaryFiles.Add(givenList);
        }
        if (paths.Count == 0)
        {
            ShowNothingToConvert();
        }
        else
        {
            StartOn(paths);
        }
    }

    // ------------------------------------------------------------- window

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);

    private IntPtr Handle => WinRT.Interop.WindowNative.GetWindowHandle(this);

    /// <summary>Initial size (in the scale of the screen), minimum size and icon.</summary>
    private void SetUpWindowFrame()
    {
        // AppWindow sizes are in physical pixels: 96 dots per inch is 100 %.
        var dpi = GetDpiForWindow(Handle);
        var scale = dpi == 0 ? 1.0 : dpi / 96.0;
        AppWindow.Resize(new SizeInt32((int)(620 * scale), (int)(700 * scale)));
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.PreferredMinimumWidth = (int)(460 * scale);
            presenter.PreferredMinimumHeight = (int)(420 * scale);
        }

        var icon = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
        if (File.Exists(icon))
        {
            AppWindow.SetIcon(icon);
        }
    }

    private Grid BuildLayout()
    {
        var root = new Grid { Padding = new Thickness(20), RowSpacing = 12 };
        // status, analysis progress, "choose a folder", options, actions,
        // conversion progress, log (takes the remaining height), bottom bar
        foreach (var height in new[]
        {
            GridLength.Auto, GridLength.Auto, GridLength.Auto, GridLength.Auto, GridLength.Auto,
            GridLength.Auto, new GridLength(1, GridUnitType.Star), GridLength.Auto,
        })
        {
            root.RowDefinitions.Add(new RowDefinition { Height = height });
        }

        _status.Text = _t.T(Msg.Analyzing);
        Place(root, _status, row: 0);
        Place(root, _detectProgress, row: 1);

        _chooseFolderButton.Content = _t.T(Msg.ChooseFolderToConvert);
        _chooseFolderButton.Click += async (_, _) => await ChooseFolderToConvertAsync();
        Place(root, _chooseFolderButton, row: 2);

        BuildOptions();
        Place(root, _options, row: 3);

        _cancelButton.Content = _t.T(Msg.CancelConversion);
        _cancelButton.Click += (_, _) => CancelConversion();
        _convertButton.Content = _t.T(Msg.Convert);
        _convertButton.Click += (_, _) => StartConversion();
        if (TryGetStyle("AccentButtonStyle") is { } accent)
        {
            _convertButton.Style = accent;
        }
        _actions.Children.Add(_cancelButton);
        _actions.Children.Add(_convertButton);
        Place(root, _actions, row: 4);

        Place(root, _convertProgress, row: 5);

        _logScroll.Content = _logText;
        Place(root, _logScroll, row: 6);

        var bottom = new Grid();
        _openDestinationButton.Content = _t.T(Msg.OpenDestination);
        _openDestinationButton.Click += (_, _) => OpenDestination();
        _quitButton.Content = _t.T(Msg.Quit);
        _quitButton.Click += async (_, _) => await RequestCloseAsync();
        bottom.Children.Add(_openDestinationButton);
        bottom.Children.Add(_quitButton);
        Place(root, bottom, row: 7);

        return root;
    }

    /// <summary>The options panel: a label and a control per line.</summary>
    private void BuildOptions()
    {
        _options.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _options.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        for (var row = 0; row < 4; row++)
        {
            _options.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        // Destination format — JPEG first, the default.
        Place(_options, Label(Msg.DestinationFormat), row: 0);
        foreach (var choice in Choices.Formats)
        {
            _format.Items.Add(_t.T(choice.Msgid));
        }
        _format.SelectedIndex = 0;
        _format.SelectionChanged += (_, _) => ShowQualityOnlyForJpeg();
        AutomationProperties.SetName(_format, _t.T(Msg.DestinationFormat));
        Place(_options, _format, row: 0, column: 1);

        // JPEG quality, hidden for the other formats.
        _qualityLabel.Text = _t.T(Msg.JpegQuality);
        Place(_options, _qualityLabel, row: 1);
        _qualityRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        _qualityRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _qualityValue.Text = "100";
        _quality.ValueChanged += (_, e) => _qualityValue.Text = ((int)Math.Round(e.NewValue)).ToString();
        AutomationProperties.SetName(_quality, _t.T(Msg.JpegQuality));
        Place(_qualityRow, _quality, row: 0);
        Place(_qualityRow, _qualityValue, row: 0, column: 1);
        Place(_options, _qualityRow, row: 1, column: 1);

        // What to do when the destination file already exists.
        Place(_options, Label(Msg.IfFileExists), row: 2);
        foreach (var choice in Choices.Collisions)
        {
            _collision.Items.Add(_t.T(choice.Msgid));
        }
        _collision.SelectedIndex = 0;
        AutomationProperties.SetName(_collision, _t.T(Msg.IfFileExists));
        Place(_options, _collision, row: 2, column: 1);

        // Destination folder: its path, "Browse…" and "New folder…".
        Place(_options, Label(Msg.DestinationFolder), row: 3);
        var destinationRow = new Grid { ColumnSpacing = 6 };
        destinationRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        destinationRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        destinationRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var browse = new Button { Content = _t.T(Msg.Browse) };
        browse.Click += async (_, _) => await BrowseDestinationAsync();
        var newFolder = new Button { Content = _t.T(Msg.NewFolderButton) };
        newFolder.Click += async (_, _) => await CreateDestinationFolderAsync();
        Place(destinationRow, _destination, row: 0);
        Place(destinationRow, browse, row: 0, column: 1);
        Place(destinationRow, newFolder, row: 0, column: 2);
        Place(_options, destinationRow, row: 3, column: 1);
    }

    private TextBlock Label(string msgid) =>
        new() { Text = _t.T(msgid), VerticalAlignment = VerticalAlignment.Center };

    private static void Place(Grid grid, FrameworkElement element, int row, int column = 0)
    {
        Grid.SetRow(element, row);
        Grid.SetColumn(element, column);
        grid.Children.Add(element);
    }

    /// <summary>A standard style of the WinUI controls, or null if it cannot be found.</summary>
    private static Style? TryGetStyle(string key)
    {
        try
        {
            return Application.Current.Resources.TryGetValue(key, out var value) ? value as Style : null;
        }
        catch (Exception)
        {
            // Purely cosmetic: whatever the reason, the default style will do.
            return null;
        }
    }

    private static Visibility ShownIf(bool shown) => shown ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Runs <paramref name="action"/> on the thread of the window. The engine
    /// reports its events on a background thread, and only the window's own
    /// thread may touch its controls.
    /// </summary>
    private void OnWindowThread(Action action) => DispatcherQueue.TryEnqueue(() => action());

    // ------------------------------------------------------ what to convert

    /// <summary>Started with nothing: explain, and offer to choose a folder.</summary>
    private void ShowNothingToConvert()
    {
        _detectProgress.Visibility = Visibility.Collapsed;
        _status.Text = _t.T(Msg.NothingProvided) + "\n\n"
            + _t.T(Msg.HowToUse, ("menu", _t.T(Msg.MenuLabel)));
        _chooseFolderButton.Visibility = Visibility.Visible;
    }

    private async Task ChooseFolderToConvertAsync()
    {
        if (await PickFolderAsync() is { } folder)
        {
            _chooseFolderButton.Visibility = Visibility.Collapsed;
            StartOn([folder]);
        }
    }

    /// <summary>Takes <paramref name="paths"/> as the selection and analyzes it.</summary>
    private void StartOn(IReadOnlyList<string> paths)
    {
        _outputDir = Selection.CommonParentDirectory(paths);
        ShowDestination();
        try
        {
            // Always a list file of our own, complete and in a known format.
            _listFile = Selection.WriteListFile(paths);
            _temporaryFiles.Add(_listFile);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            _detectProgress.Visibility = Visibility.Collapsed;
            _status.Text = e.Message;
            return;
        }
        StartDetection();
    }

    // ------------------------------------------------------------ detection

    private void StartDetection()
    {
        _detectedCount = 0;
        _status.Text = _t.T(Msg.Analyzing);
        _detectProgress.Visibility = Visibility.Visible;
        try
        {
            var run = _engine.Start(
                EngineArguments.Detect(_listFile!),
                engineEvent => OnWindowThread(() => OnDetectEvent(engineEvent)));
            _engineRun = run;
            run.Completion.ContinueWith(_ => OnWindowThread(OnDetectionDone), TaskScheduler.Default);
        }
        catch (EngineStartException e)
        {
            AppendLog(_t.T(Msg.LogEngineNotStarted, ("error", e.Message)));
            OnDetectionDone();
        }
    }

    private void OnDetectEvent(EngineEvent engineEvent)
    {
        switch (engineEvent)
        {
            case FileDetected:
                _detectedCount++;
                break;
            case DetectSummary { Total: 0 }:
                _status.Text = _t.T(Msg.NoImageInSelection);
                break;
            case DetectSummary summary:
                var details = string.Join(", ", summary.Counts
                    .OrderBy(count => count.Key, StringComparer.Ordinal)
                    .Select(count => $"{count.Value} {_engineMessages.TranslateFormatLabel(count.Key)}"));
                _status.Text = _t.TN(Msg.FilesDetected, summary.Total, ("n", summary.Total), ("details", details));
                break;
            case FatalError fatal:
                AppendLog(_t.T(Msg.LogFatalError, ("message", _engineMessages.Translate(fatal.Message))));
                break;
        }
    }

    private void OnDetectionDone()
    {
        _engineRun = null;
        _detectProgress.Visibility = Visibility.Collapsed;
        _convertButton.IsEnabled = _detectedCount > 0;
        _options.Visibility = Visibility.Visible;
        _actions.Visibility = Visibility.Visible;
        ShowQualityOnlyForJpeg();
    }

    // -------------------------------------------------------------- options

    private Choice SelectedFormat => Choices.Formats[Math.Max(0, _format.SelectedIndex)];

    private Choice SelectedCollision => Choices.Collisions[Math.Max(0, _collision.SelectedIndex)];

    private void ShowQualityOnlyForJpeg()
    {
        var jpeg = ShownIf(SelectedFormat.Id == "jpeg");
        _qualityLabel.Visibility = jpeg;
        _qualityRow.Visibility = jpeg;
    }

    private void ShowDestination()
    {
        _destination.Text = _outputDir;
        ToolTipService.SetToolTip(_destination, _outputDir);
    }

    private async Task BrowseDestinationAsync()
    {
        if (await PickFolderAsync() is { } folder)
        {
            _outputDir = folder;
            ShowDestination();
        }
    }

    /// <summary>The system folder dialog; null when the user cancels.</summary>
    private async Task<string?> PickFolderAsync()
    {
        try
        {
            var picker = new Windows.Storage.Pickers.FolderPicker
            {
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary,
            };
            // A desktop application must tell the dialog which window owns it.
            WinRT.Interop.InitializeWithWindow.Initialize(picker, Handle);
            picker.FileTypeFilter.Add("*");
            var folder = await picker.PickSingleFolderAsync();
            return folder?.Path;
        }
        catch (COMException)
        {
            return null;
        }
    }

    /// <summary>Asks for a name and creates that folder in the current destination.</summary>
    private async Task CreateDestinationFolderAsync()
    {
        if (_dialogOpen)
        {
            return;
        }
        var parent = _outputDir;
        var name = new TextBox { PlaceholderText = _t.T(Msg.FolderName), Text = _t.T(Msg.DefaultFolderName) };
        var error = new TextBlock { TextWrapping = TextWrapping.Wrap, Visibility = Visibility.Collapsed };
        var panel = new StackPanel { Spacing = 10, MinWidth = 320 };
        panel.Children.Add(new TextBlock
        {
            Text = _t.T(Msg.CreateIn, ("folder", parent)),
            TextWrapping = TextWrapping.Wrap,
        });
        panel.Children.Add(name);
        panel.Children.Add(error);

        var dialog = new ContentDialog
        {
            XamlRoot = Content.XamlRoot,
            Title = _t.T(Msg.NewFolderTitle),
            Content = panel,
            PrimaryButtonText = _t.T(Msg.Create),
            CloseButtonText = _t.T(Msg.Cancel),
            DefaultButton = ContentDialogButton.Primary,
        };
        string? created = null;
        dialog.PrimaryButtonClick += (_, args) =>
        {
            // On a problem: say it and keep the dialog open.
            var problem = TryCreateFolder(parent, name.Text.Trim(), out created);
            if (problem is not null)
            {
                error.Text = problem;
                error.Visibility = Visibility.Visible;
                args.Cancel = true;
            }
        };
        name.Loaded += (_, _) => name.SelectAll();

        _dialogOpen = true;
        try
        {
            await dialog.ShowAsync();
        }
        finally
        {
            _dialogOpen = false;
        }
        if (created is not null)
        {
            _outputDir = created;
            ShowDestination();
        }
    }

    /// <summary>Creates <paramref name="name"/> in <paramref name="parent"/>; returns the problem, if any.</summary>
    private string? TryCreateFolder(string parent, string name, out string? created)
    {
        created = null;
        if (name.Length == 0)
        {
            return _t.T(Msg.NameCannotBeEmpty);
        }
        try
        {
            var path = Path.Combine(parent, name);
            Directory.CreateDirectory(path);
            created = path;
            return null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException
            or NotSupportedException)
        {
            return _t.T(Msg.CouldNotCreateFolder, ("error", e.Message));
        }
    }

    // ----------------------------------------------------------- conversion

    private void StartConversion()
    {
        if (_listFile is null || _conversionInProgress)
        {
            return;
        }
        var options = new ConversionOptions(
            SelectedFormat.Id, (int)Math.Round(_quality.Value), SelectedCollision.Id, _outputDir);

        _log.Clear();
        _logScroll.Visibility = Visibility.Visible;
        AppendLog(_t.T(Msg.LogConverting, ("format", options.Format.ToUpperInvariant()), ("folder", options.OutputDir)));

        EngineRun run;
        try
        {
            run = _engine.Start(
                EngineArguments.Convert(options, _listFile),
                engineEvent => OnWindowThread(() => OnConvertEvent(engineEvent)));
        }
        catch (EngineStartException e)
        {
            AppendLog(_t.T(Msg.LogEngineNotStarted, ("error", e.Message)));
            return;
        }

        _engineRun = run;
        _conversionInProgress = true;
        _convertButton.IsEnabled = false;
        _cancelButton.Visibility = Visibility.Visible;
        _convertProgress.Value = 0;
        _convertProgress.Visibility = Visibility.Visible;
        run.Completion.ContinueWith(_ => OnWindowThread(OnConversionDone), TaskScheduler.Default);
    }

    private void OnConvertEvent(EngineEvent engineEvent)
    {
        switch (engineEvent)
        {
            case FileConverted { Ok: true } file:
                ShowProgress(file);
                AppendLog(_t.T(Msg.LogOk, ("input", file.Input), ("output", file.Output)));
                break;
            case FileConverted file:
                ShowProgress(file);
                AppendLog(_t.T(Msg.LogError, ("input", file.Input), ("message", _engineMessages.Translate(file.Message))));
                break;
            case ConvertSummary summary:
                AppendLog(_t.T(Msg.LogFinished, ("ok", summary.Ok), ("errors", summary.Errors)));
                _status.Text = _t.T(Msg.StatusFinished, ("ok", summary.Ok), ("errors", summary.Errors));
                break;
            case FatalError fatal:
                AppendLog(_t.T(Msg.LogFatalError, ("message", _engineMessages.Translate(fatal.Message))));
                break;
        }
    }

    private void ShowProgress(FileConverted file) =>
        _convertProgress.Value = Math.Min(1.0, file.Index / (double)Math.Max(1, file.Total));

    private void OnConversionDone()
    {
        _engineRun = null;
        _conversionInProgress = false;
        _convertButton.IsEnabled = true;
        _cancelButton.Visibility = Visibility.Collapsed;
        _openDestinationButton.Visibility = Visibility.Visible;
    }

    private void CancelConversion()
    {
        if (_conversionInProgress)
        {
            _engineRun?.Kill();
            AppendLog(_t.T(Msg.LogCancelled));
        }
    }

    private void OpenDestination()
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = _outputDir, UseShellExecute = true })?.Dispose();
        }
        catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException
            or FileNotFoundException)
        {
            AppendLog(e.Message + "\n");
        }
    }

    // ------------------------------------------------------------------ log

    private void AppendLog(string text)
    {
        _log.Append(text);
        _logScroll.Visibility = Visibility.Visible;
        if (!_logTimer.IsRunning)
        {
            _logTimer.Start();
        }
    }

    /// <summary>Shows the text accumulated so far and scrolls to its end.</summary>
    private void ShowLog()
    {
        _logText.Text = _log.ToString();
        _logScroll.UpdateLayout();
        _logScroll.ChangeView(null, _logScroll.ScrollableHeight, null, true);
    }

    // -------------------------------------------------------------- closing

    /// <summary>The close button of the window.</summary>
    private async void OnWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (_conversionInProgress && !_closeConfirmed)
        {
            // Not now: ask first, and close for good if confirmed.
            args.Cancel = true;
            await RequestCloseAsync();
        }
    }

    /// <summary>The "Quit" button, and the close button during a conversion.</summary>
    private async Task RequestCloseAsync()
    {
        if (_conversionInProgress && !await ConfirmQuitAsync())
        {
            return;
        }
        _closeConfirmed = true;
        Close();
    }

    /// <summary>Warns that a conversion is running; true if the user quits anyway.</summary>
    private async Task<bool> ConfirmQuitAsync()
    {
        if (_dialogOpen)
        {
            return false;
        }
        var dialog = new ContentDialog
        {
            XamlRoot = Content.XamlRoot,
            Title = _t.T(Msg.ConversionInProgress),
            Content = new TextBlock { Text = _t.T(Msg.QuitWarning), TextWrapping = TextWrapping.Wrap },
            PrimaryButtonText = _t.T(Msg.QuitAnyway),
            CloseButtonText = _t.T(Msg.Cancel),
            DefaultButton = ContentDialogButton.Close,
        };
        _dialogOpen = true;
        try
        {
            return await dialog.ShowAsync() == ContentDialogResult.Primary;
        }
        finally
        {
            _dialogOpen = false;
        }
    }

    /// <summary>Stops the engine if it is still running and deletes our temporary files.</summary>
    private void CleanUp()
    {
        _engineRun?.Kill();
        foreach (var file in _temporaryFiles)
        {
            try
            {
                File.Delete(file);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // Left in the temporary folder: harmless.
            }
        }
    }
}
