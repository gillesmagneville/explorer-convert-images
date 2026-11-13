// SPDX-License-Identifier: GPL-3.0-only

namespace ConvertImages.Core.Localization;

/// <summary>A note for translators, written to the .pot template.</summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class TranslatorCommentAttribute(string text) : Attribute
{
    public string Text { get; } = text;
}

/// <summary>A text with a singular and a plural form.</summary>
public sealed record PluralMsg(string Singular, string Plural);

/// <summary>
/// Every text shown to the user, in English: the msgids of the catalogs in
/// <c>po/</c>. A text is translated when it is displayed
/// (<see cref="Translator"/>). The tests check that each of these texts is
/// translated in every catalog and that the catalogs hold nothing else; they
/// also regenerate <c>po/explorer-convert-images.pot</c> from this class.
///
/// Most of them are, on purpose, word for word those of the Linux version
/// (nautilus-convert-images), so that its translations can be reused.
/// </summary>
public static class Msg
{
    // --- Context menu (written to the registry by the handler) ---

    public const string MenuLabel = "Convert images…";

    // --- Main window ---

    public const string WindowTitle = "Convert images";
    public const string Analyzing = "Analyzing the selected files…";
    public const string NoImageInSelection = "No recognized image file in the selection.";
    public static readonly PluralMsg FilesDetected =
        new("{n} file detected: {details}", "{n} files detected: {details}");

    public const string DestinationFormat = "Destination format:";
    public const string JpegQuality = "JPEG quality:";
    public const string IfFileExists = "If the file already exists:";
    public const string DestinationFolder = "Destination folder:";
    public const string Browse = "Browse…";
    public const string NewFolderButton = "New folder…";
    public const string CancelConversion = "Cancel conversion";
    public const string Convert = "Convert";
    public const string OpenDestination = "Open the destination folder";
    public const string Quit = "Quit";

    // --- Window opened without anything to convert ---

    public const string NothingProvided = "No file or folder provided.";

    [TranslatorComment("{menu} is the name of the context-menu entry (\"Convert images…\"). "
        + "\"Show more options\" must be the exact wording of Windows 11 in your language.")]
    public const string HowToUse =
        "In File Explorer, select images or folders, right-click, then choose “{menu}” "
        + "(on Windows 11: under “Show more options”).";

    public const string ChooseFolderToConvert = "Choose a folder to convert…";

    // --- Destination formats ---

    public const string FormatJpeg = "JPEG (lossy, adjustable quality)";
    public const string FormatPng = "PNG (lossless)";
    public const string FormatTiff = "Uncompressed TIFF (lossless)";
    public const string FormatBmp = "BMP (lossless)";
    public const string FormatWebp = "Lossless WebP";

    // --- What to do when the destination file exists ---

    public const string CollisionRename = "Rename the new file";
    public const string CollisionSkip = "Skip the file";
    public const string CollisionOverwrite = "Replace the existing file";

    // --- Format labels emitted by the engine that are not proper names ---

    public const string LabelUnknown = "Unknown";
    public const string LabelGenericRaw = "Generic RAW";

    // --- "New folder" dialog ---

    public const string NewFolderTitle = "New folder";
    public const string CreateIn = "Create in: {folder}";
    public const string FolderName = "Folder name";

    [TranslatorComment("default name proposed for the new destination folder "
        + "(a single lowercase word, e.g. \"converted\").")]
    public const string DefaultFolderName = "converted";

    public const string Cancel = "Cancel";
    public const string Create = "Create";
    public const string NameCannotBeEmpty = "The name cannot be empty.";
    public const string CouldNotCreateFolder = "Could not create the folder: {error}";

    // --- Conversion log ---

    public const string LogConverting = "Converting to {format} → {folder}\n";
    public const string LogOk = "[OK] {input} → {output}\n";
    public const string LogError = "[ERROR] {input}: {message}\n";
    public const string LogFatalError = "[FATAL ERROR] {message}\n";
    public const string LogFinished = "\nFinished — succeeded: {ok}, errors: {errors}.\n";
    public const string LogCancelled = "\nConversion cancelled by the user.\n";
    public const string LogEngineNotStarted = "[ERROR] could not start the engine: {error}\n";
    public const string StatusFinished = "Conversion finished — succeeded: {ok}, errors: {errors}.";

    // --- Quitting during a conversion ---

    public const string ConversionInProgress = "Conversion in progress";
    public const string QuitWarning =
        "Quitting now will interrupt the conversion in progress. Files already "
        + "converted stay on disk; the remaining ones will not be processed.";
    public const string QuitAnyway = "Quit anyway";

    // --- Engine error messages ---
    // Contract: the engine emits them in English; the interface translates
    // them (see EngineMessageTranslator). Each text is both a recognition
    // pattern and a msgid. Keep them in sync with engine/src/*.rs: the tests
    // check that each one still exists there.

    [TranslatorComment("{primary} is the error of the main RAW decoder and {fallback} "
        + "the error of LibRaw, the backup decoder tried afterwards.")]
    public const string EngineFallback = "{primary} ; LibRaw fallback: {fallback}";

    public const string EngineCannotOpenRaw = "cannot open RAW file: {detail}";
    public const string EngineRawDecodingFailed = "RAW decoding failed: {detail}";
    public const string EngineDecodingFailed = "decoding failed: {detail}";
    public const string EngineCannotCreateFile = "could not create the output file: {detail}";
    public const string EngineCannotCreateFolder = "could not create the output folder: {detail}";
    public const string EngineCannotReadList = "could not read the input list: {detail}";

    [TranslatorComment("{format} is the target format name (JPEG, PNG, TIFF, BMP, WebP).")]
    public const string EngineEncodingFailed = "{format} encoding failed: {detail}";

    public const string EngineInternalError = "{name}: internal decoder error (corrupted file?)";
    public const string EngineSkipped = "skipped (the destination file already exists)";
    public const string EngineLibRawInit = "LibRaw: initialization failed";
    public const string EngineInvalidPath = "invalid path (null byte)";
    public const string EngineBufferSize = "inconsistent {bits}-bit RAW buffer size";
    public const string EngineUnexpectedImage =
        "LibRaw: unexpected developed image (type {kind}, {colors} channels, {depth} bits)";
    public const string EngineImageSize =
        "LibRaw: inconsistent image size ({size} bytes, {expected} expected)";

    /// <summary>
    /// The engine messages, in the order they are tried: the first pattern
    /// that matches wins, so the most specific ones come first.
    /// </summary>
    public static readonly IReadOnlyList<string> EngineMessageTemplates =
    [
        EngineFallback,
        EngineCannotOpenRaw,
        EngineRawDecodingFailed,
        EngineDecodingFailed,
        EngineCannotCreateFile,
        EngineCannotCreateFolder,
        EngineCannotReadList,
        EngineEncodingFailed,
        EngineInternalError,
        EngineSkipped,
        EngineLibRawInit,
        EngineInvalidPath,
        EngineBufferSize,
        EngineUnexpectedImage,
        EngineImageSize,
    ];
}
