<!-- SPDX-License-Identifier: GPL-3.0-only -->

# explorer-convert-images Architecture

🇫🇷 [Version française](ARCHITECTURE.fr.md)

This document is the technical contract of the project: what each program
does, what they say to each other, and what must change together. The
vision and the principles are in `PROJECT_CONTEXT.md`.

## Overview

```
 File Explorer                        (explorer.exe — nothing of ours runs in it)
      │  right-click → "Convert images…"
      │  COM: starts the handler, gives it the whole selection in one call
      ▼
 explorer-convert-images-verb.exe     the menu handler (Rust)
      │  writes the selected paths to a text file
      │  starts: ExplorerConvertImages.exe --input-list FILE
      ▼
 ExplorerConvertImages.exe            the window (C#, WinUI 3)
      │  starts, as often as needed:
      │    convert-images-engine.exe detect  --input-list FILE …
      │    convert-images-engine.exe convert --input-list FILE …
      │  reads one JSON event per line on its standard output
      ▼
 convert-images-engine.exe            the engine (Rust): detects and converts
```

Three separate programs, on purpose:

* **Nothing runs inside File Explorer.** The handler is an *out-of-process*
  COM server: File Explorer talks to a program of ours, it does not load a
  DLL of ours. A bug on our side cannot crash or slow down `explorer.exe`.
* **The window never decodes an image.** All the work on images is done by
  the engine, in its own process: the window stays responsive, a corrupted
  file can at worst make the engine report an error, and cancelling is
  simply stopping that process.
* **The engine knows nothing of Windows' interface.** It is a command-line
  program with a small, stable contract (below), the same code as the Linux
  version.

Everything is installed **per user** (files under `%LOCALAPPDATA%`, registry
under `HKEY_CURRENT_USER`): no administrator rights, at installation or
afterwards.

---

# Project layout

```
engine/                       convert-images-engine
  src/main.rs, cli.rs         command line, JSON events
  src/formats.rs              format detection
  src/convert.rs              conversion
  src/raw.rs, libraw.rs       RAW decoding (rawloader/imagepipe, then LibRaw)
  vendor/libraw-sys/          LibRaw and its bindings
  UPSTREAM.md                 where this copy comes from, what differs
shell-verb/                   explorer-convert-images-verb
  src/main.rs                 command line (-Embedding, --register, --unregister)
  src/plan.rs                 the registry entries, as data
  src/registry.rs             writes and removes them        (Windows only)
  src/com.rs                  the COM object and its server  (Windows only)
  src/launch.rs               selection file, start of the window
  src/extensions.rs           file types that get the menu entry
  src/labels.rs               text of the menu entry, per language
  examples/selftest.rs        end-to-end check on a real Windows
  examples/simulate_explorer.rs  invokes the handler on given paths
app/
  ConvertImages.Core/         everything of the window but the window:
    Selection.cs                arguments, selection, destination folder
    Engine/                     running the engine, its events, its messages
    Localization/               texts (Msg.cs) and their translation
  ConvertImages.Core.Tests/   tests of the above; generates the .pot
  ExplorerConvertImages/      the WinUI 3 application
    App.xaml(.cs)               startup, standard look of the controls
    MainWindow.cs               the window, built in code
po/                           fr.po, de.po, es.po, explorer-convert-images.pot
installer/installer.nsi       NSIS installer
```

---

# The menu handler (`explorer-convert-images-verb`)

## Registry entries

Written by `explorer-convert-images-verb.exe --register` (run by the
installer), removed by `--unregister` (run by the uninstaller). They are
described once, as data, in `plan.rs`; all are under
`HKEY_CURRENT_USER\Software\Classes`:

```
CLSID\{3062A5F8-D1CE-49DC-8ADE-8B1BC53CF1BA}
    (Default)                 = explorer-convert-images context-menu handler
    LocalServer32\(Default)   = "<install dir>\explorer-convert-images-verb.exe"

<parent>\shell\ExplorerConvertImages
    (Default)                 = Convert images…          (in the language of Windows)
    Icon                      = "<install dir>\ExplorerConvertImages.exe",0
    MultiSelectModel          = Player
    NeverDefault              = (empty)
    command\DelegateExecute   = {3062A5F8-D1CE-49DC-8ADE-8B1BC53CF1BA}
```

`<parent>` is, in turn:

* `SystemFileAssociations\.<ext>` for each image extension known to the
  engine (`extensions.rs`) — the entry then appears whatever program the
  file type is associated with, without touching that association;
* `Directory` — right-click on a folder;
* `Directory\Background` — right-click on the background of an open folder.

What each value is for:

* **`DelegateExecute`** makes the entry a *COM* verb: File Explorer creates
  the object of that class and hands it the selection, instead of starting
  one program per selected file as a plain command line would.
* **`LocalServer32`** makes that object live in our own program, started on
  demand, rather than in a DLL loaded by File Explorer.
* **`MultiSelectModel = Player`** keeps the entry available whatever the
  number of selected items. A plain command-line entry disappears beyond 15
  selected items (100 at best); a COM verb in this mode has no limit.
* **`NeverDefault`** guarantees that the entry never becomes what a
  double-click does, even for a file type no program is associated with.

Uninstalling deletes exactly the `ExplorerConvertImages` keys and the class
key, then removes the parent keys it may have created **only if they are
left completely empty**: entries of other programs are never touched.

## The COM object and its lifecycle

`com.rs` follows the scheme of Microsoft's "ExecuteCommandVerb" sample:

1. File Explorer asks COM for the class; COM starts
   `explorer-convert-images-verb.exe -Embedding`.
2. `serve()` registers the class factory (`CoRegisterClassObject`, local
   server, single use) and pumps messages on a single-threaded apartment.
3. File Explorer creates the object and calls
   `IObjectWithSelection::SetSelection` (the whole selection, as an
   `IShellItemArray`), then `IExecuteCommand::Execute`. For the background
   of a folder there is no selection: the folder comes through
   `IExecuteCommand::SetDirectory`.
4. `Execute` only queues the command and **returns at once** — File Explorer
   is never kept waiting. The message loop then reads the paths of the
   selection, writes the selection file and starts the window.
5. File Explorer releases the object; the program exits.

Timeouts make sure the program never lingers: 30 seconds if it is started
but never called, 10 seconds after the command if the object is never
released.

Failures (the window cannot be started, for instance) are appended to
`explorer-convert-images-verb.log` in the temporary folder of the user: the
program has neither window nor console.

## Passing the selection

A Windows command line is limited to about 32,000 characters — a few
hundred paths. The selection therefore always travels in a **file**: one
path per line, UTF-8, in the temporary folder of the user. The handler
writes it and passes `--input-list FILE` to the window; the window writes
its own (complete, in a known format) for the engine, and deletes both when
it closes.

## What Windows 11 changes

The entry is a classic context-menu verb: on Windows 11 it appears under
**Show more options**. Appearing in the first-level menu of Windows 11
requires a different mechanism (an `IExplorerCommand` handler declared by a
package with an identity, and signed); the present design keeps the door
open — the handler would stay the same program — but does not do it.

---

# The window

`ExplorerConvertImages` is a WinUI 3 application (Windows App SDK),
**unpackaged** and **self-contained**: its build output is a plain folder
holding the program, the .NET runtime and the Windows App SDK runtime.
Nothing needs to be present on the user's machine beforehand.

It is split in two projects:

* **`ConvertImages.Core`** — plain .NET, no Windows-specific dependency:
  reading the arguments and the selection, choosing the default destination
  folder, running the engine and reading its events, translating. It builds
  and is tested on Linux as well as on Windows.
* **`ExplorerConvertImages`** — the window itself (`MainWindow.cs`, built in
  code) and the application class. `App.xaml` only brings in the standard
  look of the controls. This project builds on Windows only.

Rule: **anything that can be decided without a window goes to `Core`**, so
that it is tested everywhere; `MainWindow.cs` only shows and asks.

Behaviour worth knowing:

* Started with nothing to convert (the Start menu shortcut), the window
  explains how to use the menu entry and offers to choose a folder.
* The default destination folder is the deepest folder containing the whole
  selection (`Selection.CommonParentDirectory`).
* The engine is looked for next to the application
  (`convert-images-engine.exe`); the `CONVERT_IMAGES_ENGINE` environment
  variable gives another path (development and tests).
* Closing the window during a conversion asks for confirmation; confirming
  stops the engine. Files already converted stay on disk.
* An error nobody handled is appended to `explorer-convert-images-app.log`
  in the temporary folder of the user before the application stops.

---

# The engine (`convert-images-engine`)

The engine is a copy of that of nautilus-convert-images, adapted to build
and run on Windows (`engine/UPSTREAM.md`). Format detection, RAW decoding
and conversion are those described in the `ARCHITECTURE.md` of that
project; what follows is the part the window depends on.

## Command-line interface (`cli.rs` / `main.rs`)

```
convert-images-engine formats
convert-images-engine detect   [--recursive] [--input-list <file>] [<paths...>]
convert-images-engine convert  [--recursive] [--input-list <file>]
                                 --output-dir <dir> --format <jpeg|png|tiff|bmp|webp>
                                 [--quality <1-100>] [--on-collision <skip|rename|overwrite>]
                                 [--threads <n>] [<paths...>]
```

`--input-list <file>` adds the paths listed in a file (one per line, UTF-8,
with or without a byte order mark, Unix or Windows line endings) to those
given as arguments.

Each line written to stdout is a complete JSON object (one line = one
event), in UTF-8, so that the window can read as it goes without waiting
for the process to finish.

### `detect` events

```json
{"event": "file", "path": "...", "format": "RAW Canon (CR2)", "is_raw": true}
{"event": "summary", "total": 42, "counts": {"RAW Canon (CR2)": 30, "JPEG": 12}}
```

### `convert` events

```json
{"event": "start", "total": 42}
{"event": "file_done", "index": 1, "total": 42, "input": "...", "output": "...", "status": "ok"}
{"event": "file_done", "index": 2, "total": 42, "input": "...", "status": "error", "message": "..."}
{"event": "summary", "total": 42, "ok": 40, "errors": 2, "output_dir": "..."}
```

### Fatal errors (both commands)

```json
{"event": "fatal_error", "message": "..."}
```

followed by exit code 1: the output folder could not be created, or the
`--input-list` file could not be read.

The `message` fields are **technical messages in English**, not localized:
the window translates them (see "Internationalization"). The format labels
of the `file` event (`"Unknown"`, `"Generic RAW"`…) are also translated on
the window side.

This schema is the contract between the engine and the window
(`EngineEvent.cs` reads it): its shape must not change without updating
this document **and** the code that consumes it. An event or a field the
window does not know is ignored.

### Collision policy (`--on-collision`)

* `skip` — skip the file if the destination already exists.
* `rename` (default) — append ` (1)`, ` (2)`, ... until a free name is
  found.
* `overwrite` — replace the existing file.

---

# Internationalization

Source texts are in English; translations are gettext catalogs,
`po/fr.po`, `po/de.po`, `po/es.po`.

* **`Msg.cs`** (`ConvertImages.Core/Localization`) declares every text of
  the window, as constants. Most are word for word those of the Linux
  version, so that its translations are reused.
* The catalogs are **embedded as text** in `ConvertImages.Core` and parsed
  at startup (`PoCatalog.cs`): there is no compilation step and nothing to
  install. The language is the display language of Windows
  (`CultureInfo.CurrentUICulture`); a regional variant uses its base
  language; anything else gives English.
* **`po/explorer-convert-images.pot`** is generated from `Msg.cs` by the
  test program (`--write-pot`). The tests fail if it is out of date, if a
  text of `Msg.cs` is missing or untranslated in a catalog, if a catalog
  holds a text `Msg.cs` no longer has, or if a translation does not keep
  the `{placeholders}` of its source.

## Engine messages

The engine emits technical error messages **in English**. The window
renders them in the user's language by means of
`Msg.EngineMessageTemplates`: each template is an English message with
`{name}` fields (for example `"cannot open RAW file: {detail}"`), turned
into a regular expression and translated like any other text
(`EngineMessageTranslator.cs`).

* The variable parts are kept as they are and translated recursively if
  they in turn match a template — this is how the combined error
  "`cannot open RAW file: …` ; `LibRaw fallback: …`" is rendered in full.
* An **unknown** message is displayed as is, in English: changing an engine
  message never breaks the window, it just stops being translated.
* **When you change a message in `engine/src/*.rs`**, update the matching
  template in `Msg.cs`, regenerate the `.pot` and translate the new entry.
  The tests check that every template still exists in the Rust code.

## Texts outside the window

* **The menu entry** is a registry value read by File Explorer itself. It
  is written by `--register` in the language Windows has at that moment
  (`labels.rs`), hence the advice to run the installer again after changing
  the language of Windows.
* **The installer** has its own texts (`LangString` in `installer.nsi`), in
  the same four languages.

---

# The installer

`installer/installer.nsi` (NSIS), built on Linux by `build-installer.sh`.

* Per user: `RequestExecutionLevel user`, files in
  `%LOCALAPPDATA%\Programs\explorer-convert-images`, uninstall information
  under `HKEY_CURRENT_USER`.
* The installation folder is not asked for. Because that folder is deleted
  as a whole by an upgrade and by the uninstaller, the installer refuses a
  folder (given with `/D=`) that holds anything but a previous installation
  of ours.
* An upgrade first removes the menu entries with the handler of the
  *previous* version (the list of file types may differ), then all its
  files.
* The registration of the menu entries is done by the handler itself
  (`--register`), not by the installer script: the registry entries have a
  single description, `plan.rs`.

---

# What must agree

Several things are written in more than one place because they belong to
different tools. Each pair is guarded by a test, so that a change in one
place fails loudly until the other follows.

| What | Where | Guarded by |
|---|---|---|
| Version of the project | `VERSION`, `Cargo.toml`, `Cargo.lock` (set all three with `tools/set-version.sh`) | `plan.rs` tests |
| File names of the three programs | `plan.rs`, `installer.nsi`, the application `.csproj`, `EngineRunner.cs`, `engine/Cargo.toml` | `plan.rs` and `EngineTests.cs` tests |
| Image extensions | `engine/src/formats.rs`, `shell-verb/src/extensions.rs` | `extensions.rs` tests |
| Text of the menu entry | `po/*.po`, `labels.rs`, `installer.nsi`, `Msg.MenuLabel` | `labels.rs` and `LocalizationTests.cs` tests |
| Format and collision identifiers | `engine/src/convert.rs`, `cli.rs`, `ConversionOptions.cs` | `EngineTests.cs` tests |
| Engine messages | `engine/src/*.rs`, `Msg.cs` | `EngineTests.cs` tests |
| Texts and translations | `Msg.cs`, `po/*.po`, the `.pot` | `LocalizationTests.cs` tests |

---

# Build and continuous integration

`.github/workflows/build.yml` runs three jobs in sequence:

1. **`rust`** (Linux) — `cargo test`, then cross-compilation of the engine
   and the handler for `x86_64-pc-windows-gnu`, and a check that neither
   needs a MinGW DLL.
2. **`app`** (Windows) — the C# tests against the Windows engine; the
   Windows-only tests of the handler (`cargo test`, with the Microsoft
   toolchain); `dotnet publish` of the window; then `selftest --dist`, which
   checks the content of the published folder, starts the built window and
   goes through the handler like File Explorer. Each time, a window of the
   application must be on the screen and still be there twenty seconds
   later. If a step fails, the errors Windows recorded are printed.
3. **`installer`** (Linux) — NSIS, the `.sha256` file and, on a tag
   `vX.Y.Z`, a draft release.

The published folder needs a file `dotnet publish` does not put there by
itself: `ExplorerConvertImages.pri`, the resource index through which the
window finds the look declared in `App.xaml`. The Windows App SDK only
publishes it for MSIX packages
([issue 6720](https://github.com/microsoft/WindowsAppSDK/issues/6720));
without it nothing is reported at build time, and the window stops by itself
some ten seconds after starting. `PublishWinUIResources.targets`, imported
by the project of the window, adds the file to the published folder and
makes the publish fail if it is missing; `selftest` and `build-installer.sh`
check for it again.

Why the Rust programs are built on Linux for Windows: the build script of
the bundled LibRaw does not support the Microsoft compiler, and the GNU
toolchain produces the same programs on a developer's Linux machine and in
the CI. They are linked statically (`.cargo/config.toml`,
`vendor/libraw-sys/build.rs`) and need no DLL beyond those of Windows.

---

# Dependency philosophy

* **The engine's dependencies are those of nautilus-convert-images**, at
  the same pinned versions: the two copies must stay easy to compare.
* **The handler** depends on the `windows` crate only (Microsoft's Rust
  bindings), and only in a Windows build.
* **`ConvertImages.Core`** has no dependency beyond .NET itself; its tests
  use no test framework. Nothing is downloaded to build or test it.
* **The window** depends on the Windows App SDK only, at a pinned version.
* Any new dependency must be justified by a clear net benefit.

# Toolchain note

See the "Toolchain note" section of `README.md`.
