<!-- SPDX-License-Identifier: GPL-3.0-only -->

# explorer-convert-images

🇫🇷 [Version française](README.fr.md)

**Convert your photos with a right-click, straight from Windows File
Explorer — RAW included, at full resolution, with no cropping or resizing.**

[![Latest release](https://img.shields.io/github/v/release/gillesmagneville/explorer-convert-images)](https://github.com/gillesmagneville/explorer-convert-images/releases/latest)
[![License GPL-3.0](https://img.shields.io/badge/license-GPL--3.0-blue)](LICENSE)

Select files or whole folders in File Explorer, right-click →
**Convert images…**, choose the format and the destination folder:
that's all.

explorer-convert-images is the Windows counterpart of
[nautilus-convert-images](https://github.com/gillesmagneville/nautilus-convert-images)
(the same tool for GNOME Files, on Linux): same conversion engine, same
texts, behind a native Windows window.

> **Status: version 1.0.0.** Tried by hand on Windows 11 — installation,
> menu entry (on files, folders and the background of a folder),
> conversions, uninstallation — and checked on a real Windows by the GitHub
> Actions workflow of this repository, which builds the three programs and
> the installer on every change. **Not tried yet**: Windows 10 and Windows
> on ARM. See [CHANGELOG.md](CHANGELOG.md).

> **Note:** the user interface is available in **English, French, Spanish
> and German** (see [Languages](#languages)). The other documents
> (`PROJECT_CONTEXT.md` for the vision and principles, `ARCHITECTURE.md`
> for the technical contract, `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`,
> `SECURITY.md`, `CHANGELOG.md`) exist in **English and French**: each one
> has a link to switch language at the top, and the French files end in
> `.fr.md`.

## Why use it?

* **No detour** — no application to launch, no command line, no import:
  the conversion is in the context menu of your files.
* **Everything you have at hand** — RAW from many cameras (see "RAW
  compatibility"), JPEG, PNG, TIFF, BMP, GIF, WebP, even mixed in a single
  folder: the format of each file is detected, even when the extension is
  missing or misleading.
* **Your quality, intact** — full resolution, never any cropping or
  resizing; PNG, TIFF, BMP and WebP are truly lossless (transparency and
  16-bit depth preserved). For a RAW, this applies to the developed image,
  not to the original file: keep it.
* **Fast** — Rust engine, all CPU cores work in parallel on batches.
* **Any number of files** — the whole selection arrives in one go, in a
  single window, whatever its size.
* **In control** — you see what was detected before starting, choose the
  destination folder (creating one is possible) and what happens if a name
  already exists (skip, rename, overwrite), then follow the progress live.
* **Harmless for File Explorer** — nothing is loaded into `explorer.exe`:
  the menu entry starts separate programs.
* **Light on your system and free** — installed for your user account
  only, without administrator rights; nothing else to install; GPL-3.0
  license.

| You have | You want | What to do |
|---|---|---|
| A folder of RAW files after a shoot | JPEGs to share | select it, choose JPEG (the default) |
| RAW files to pass to another program | A lossless image | choose TIFF or PNG (16-bit), and keep the RAW |
| A folder mixing CR2, TIFF and PNG | A single format everywhere | select everything, choose the format |
| Large BMP or PNG files | Smaller files, still lossless | choose lossless WebP |

## Installation

Requires a 64-bit Windows 10 (version 1809 or later) or Windows 11.

Download `explorer-convert-images-X.Y.Z-setup.exe` (`X.Y.Z` = version
number) from the
[releases](https://github.com/gillesmagneville/explorer-convert-images/releases/latest)
page and run it. It installs the program for your user account, in
`%LOCALAPPDATA%\Programs\explorer-convert-images`, and asks for no
administrator rights.

> **Note:** since the installer isn't code-signed, Windows will very likely
> show a blue **"Windows protected your PC"** (SmartScreen) warning on first
> run. This is expected for a small, unsigned open-source project — not a
> sign of malware. Click **More info**, then **Run anyway** to proceed. For
> the same reason, your browser may warn about the download; the check
> below tells you whether the file is the one that was published.

To check your download, each installer is published with its own `.sha256`
file. In PowerShell, in the folder of the download:

```powershell
(Get-FileHash .\explorer-convert-images-X.Y.Z-setup.exe -Algorithm SHA256).Hash
Get-Content .\explorer-convert-images-X.Y.Z-setup.exe.sha256
```

The two fingerprints must be the same (letter case aside).

To uninstall: **Settings > Apps > Installed apps**, then
`explorer-convert-images` > **Uninstall**. The menu entry and every file of
the program are removed.

### What the installer does

* It copies the three programs (see "How it is made") into
  `%LOCALAPPDATA%\Programs\explorer-convert-images`, adds a **Convert
  images** shortcut to the Start menu, and registers the program in
  **Settings > Apps** so that it can be uninstalled from there.
* It adds the menu entry to the registry, **for the current user only**
  (`HKEY_CURRENT_USER`): on image files, on folders and on the background of
  a folder. The file types keep the program they are associated with: the
  entry never becomes what a double-click does.
* Installing a newer version over an older one is an upgrade: the previous
  version is removed first, menu entry included.
* The uninstaller removes every file and registry entry of the program, and
  only those: the entries of other programs are never touched.
* Nothing else needs to be present on your machine: the .NET and Windows
  App SDK runtimes come with the program, LibRaw is built into the engine,
  and the C and C++ runtimes are linked statically.

## Usage

Right-click on a selection (files and/or folders), on a folder, or on the
background of an open folder → **Convert images…**.

* **On Windows 11**, the entry is in the classic menu: right-click, then
  **Show more options** (or Shift + right-click, or Shift+F10). Appearing
  in the first-level menu of Windows 11 requires a signed package, which
  the project does not provide yet (see `PROJECT_CONTEXT.md`). Windows 10
  only has the classic menu, so the entry is in the menu you get right
  away.
* The **Convert images** shortcut of the Start menu opens the same window
  with nothing selected; it then offers to choose a folder to convert.

The window shows what was found (subfolders included), lets you choose the
destination format, the JPEG quality, what to do when a file already exists
and the destination folder, then converts and logs each file. By default the
destination folder is the deepest folder that contains the whole selection.
Closing the window during a conversion asks for confirmation; the files
already converted stay where they are.

The window can also be started by hand, outside File Explorer, with files
and/or folders as arguments:

```powershell
& "$env:LOCALAPPDATA\Programs\explorer-convert-images\ExplorerConvertImages.exe" `
    "$HOME\Pictures\img001.cr2" "$HOME\Pictures\img002.jpg"
```

## Languages

The interface — the context-menu entry, the window, labels and error
messages, the installer — is available in **English, French, Spanish and
German**. The language is that of Windows (your display language); for any
other language, the interface is shown in English.

* The text of the **menu entry** is written when the program is installed,
  in the language Windows has at that moment. After changing the language
  of Windows, run the installer again so that the entry follows.
* To try the window in another language without changing Windows, set the
  `CONVERT_IMAGES_LANGUAGE` environment variable (`fr`, `de`, `es`, `en`)
  before starting `ExplorerConvertImages.exe`.
* The Spanish and German translations have not yet been reviewed by native
  speakers: corrections are welcome (see `CONTRIBUTING.md`, which also
  explains how to add a language).
* Error messages produced by the engine itself are translated; text coming
  back from third-party libraries (for example the detail of a `rawloader`
  or LibRaw error) stays in English.
* The command-line engine (`convert-images-engine.exe`, its help and its
  messages) is in English.

## Formats

| | Formats |
|---|---|
| **Sources** | RAW (recognized extensions: CR2/CR3, NEF/NRW, ARW/SRF/SR2, DNG, ORF, RAF, RW2, PEF, SRW, X3F, 3FR, RWL, GPR…; see "RAW compatibility"), JPEG, PNG, TIFF, BMP, GIF, WebP |
| **Destinations** | JPEG (adjustable quality, 100 by default), PNG, TIFF (uncompressed), BMP, lossless WebP (VP8L) |

PNG, TIFF, BMP and WebP are **genuinely lossless**, verified by
pixel-by-pixel comparison when the engine was validated in
nautilus-convert-images (alpha channel and 16-bit included). JPEG remains a
lossy format by nature — a limit of the format itself, not of this tool,
and never presented as anything else. Detection is done by
extension, then by content analysis, with an actual decoding attempt as a
last resort. RAW files are decoded at full resolution (active sensor area).

The engine, the menu handler and the window are decoupled (see
`ARCHITECTURE.md`): the engine (`convert-images-engine.exe`) is a standalone
command-line program, usable independently of File Explorer or of the
window (see "Command line").

The engine is the one of nautilus-convert-images, adapted to run on
Windows: `engine/UPSTREAM.md` says which version this repository holds and
what was changed.

## RAW compatibility

Recognizing a file as RAW (by its extension) does not guarantee that it can
be decoded: it depends on the camera.

* **A conversion, not a development** — for a RAW input file, the result
  is the camera's raw data as it is: the camera's white balance and a
  conversion to sRGB, with no retouching (no lens correction — vignetting,
  distortion —, no sharpening or noise reduction, no creative exposure or
  contrast adjustment). It is therefore neither the rendering of the
  camera's JPEG (more contrasty, corners corrected, sometimes cropped to
  16:9 whereas the converted image covers the whole sensor), nor that of a
  RAW developer such as darktable or RawTherapee, which add their own
  default adjustments. For a finished result, convert to 16-bit TIFF or
  PNG and retouch in such a program.
* **Decoding order** — `rawloader` first; if it does not know the camera,
  automatic fallback to [LibRaw](https://www.libraw.org/), built into the
  program. If both fail, the error is shown for that file and the batch
  carries on with the next one.
* **Tested with real files** — only Sony ARW files, including those from
  the RX100 VI (absent from `rawloader`'s database, hence decoded by the
  fallback), checked by the author with nautilus-convert-images, which has
  the same engine. Canon, Nikon, Fujifilm, Olympus, Panasonic, Pentax,
  Sigma, DNG… are supported according to what `rawloader` and LibRaw
  announce, but have not been verified by the author; color fidelity may
  vary from one camera to another.
* **Limits of the LibRaw fallback** — it is built without the optional
  libraries: it reads neither lossy-compressed (JPEG) DNG files nor
  floating-point compressed (deflate) DNG files, nor Sigma Foveon files
  (X3F; `rawloader` reads some of them). Canon's CR3 does not exist in
  `rawloader`: it therefore depends entirely on the fallback, and has not
  been tested.
* **Two decoders, two renderings** — the main decoder (`imagepipe`) applies
  a light base tone curve to go from the sensor's linear signal to a
  natural look; the LibRaw fallback does not, and its images tend to look
  darker. A batch that mixes cameras handled by one and by the other can
  therefore show slightly different brightness.
* **Accented or very long paths** — on Windows, the engine hands the file to
  LibRaw from memory, so that a path with accents or longer than 260
  characters is read like any other.
* **A file refused, or with doubtful colors?** Report it through the
  [issues](https://github.com/gillesmagneville/explorer-convert-images/issues),
  stating the camera model.

## Command line

The engine can also be used on its own, from a command line. It writes one
JSON object per line on its standard output (detection, then progress of
the conversion), which is how the window follows it:

```powershell
$engine = "$env:LOCALAPPDATA\Programs\explorer-convert-images\convert-images-engine.exe"
& $engine formats
& $engine detect --recursive "$HOME\Pictures"
& $engine convert --recursive "$HOME\Pictures" `
    --output-dir "$HOME\Pictures\converted" --format png
```

`--format` is `jpeg`, `png`, `tiff`, `bmp` or `webp`; `--quality` (1–100)
applies to JPEG; `--on-collision` is `skip`, `rename` (the default) or
`overwrite`; `--threads` limits the number of cores used; `--input-list
FILE` adds the paths listed in a file, one per line. `& $engine --help`
shows everything. The exact shape of the events is described in
`ARCHITECTURE.md`.

## How it is made

Three programs, installed in the same folder (see `ARCHITECTURE.md`):

| Program | Role | Language |
|---|---|---|
| `explorer-convert-images-verb.exe` | Receives the selection from File Explorer's menu and opens the window on it | Rust |
| `ExplorerConvertImages.exe` | The window (WinUI 3, the native Windows 11 look) | C# |
| `convert-images-engine.exe` | Detects the formats and converts; usable on its own from a command line | Rust |

The menu entry is a COM verb served by the menu handler as a separate
program, started on demand: File Explorer only hands it the selection (all
of it, in one call, whatever its size, through a file) and is never kept
waiting. If something goes wrong, the programs write what happened to
`explorer-convert-images-verb.log` and `explorer-convert-images-app.log` in
the temporary folder of your user (type `%TEMP%` in File Explorer's address
bar): please attach them to a bug report.

## Getting set up

To get the source:

```bash
git clone https://github.com/gillesmagneville/explorer-convert-images.git
cd explorer-convert-images
```

What you need depends on what you want to do (details, with the commands:
`CONTRIBUTING.md`):

* **Tests of the engine and of the menu handler** — on Linux: Rust (with
  `rustup`: `rust-toolchain.toml` selects the version) and a C/C++ compiler,
  needed to build the LibRaw built into the engine. The first build is
  longer (a few minutes); the next ones are incremental.
* **Tests of the logic of the window** — the .NET 10 SDK, on Linux or on
  Windows.
* **The window itself** — Windows and the .NET 10 SDK.
* **The Windows-only tests of the menu handler, and `selftest`** — Windows
  and Rust.
* **The installer** — Linux, with `nsis` and the MinGW-w64 compilers.

## Building

**The simplest way is to let GitHub build everything.** Each push runs the
workflow `.github/workflows/build.yml`, which tests the three programs,
builds them and produces the installer. Download it from the **Actions**
tab of the repository: open the run, then **Artifacts > installer**.

To build by hand, two machines are involved, because the window can only be
built on Windows while the rest is built on Linux:

```bash
# On Linux (Ubuntu/Debian): the two Rust programs and the installer
sudo apt-get install nsis gcc-mingw-w64-x86-64-posix g++-mingw-w64-x86-64-posix
rustup target add x86_64-pc-windows-gnu
./build-installer.sh --app FOLDER_OF_THE_PUBLISHED_WINDOW
```

```powershell
# On Windows (.NET 10 SDK): the window, as a folder
dotnet publish app/ExplorerConvertImages -c Release -r win-x64 -p:Platform=x64 -o FOLDER
```

`build-installer.sh` builds the two Rust programs, gathers them with the
window in `dist/explorer-convert-images/`, then builds the installer with
NSIS; the result is `dist/explorer-convert-images-X.Y.Z-setup.exe` and its
`.sha256` file. `--no-rust-build` reuses the Rust programs already built.
Each run of the workflow also keeps, as the `explorer-convert-images-folder`
artifact, the complete folder that the installer installs (the window and
the two Rust programs); `./build-installer.sh --dist FOLDER` turns such a
folder into an installer, which is what the workflow does. The script checks
that everything the installer needs is present, and stops with a message if
not. Details and variants: `CONTRIBUTING.md`.

### Publishing a release

Set the version (`tools/set-version.sh X.Y.Z`), give the release its section
in `CHANGELOG.md` and `CHANGELOG.fr.md`, commit, then push a tag `vX.Y.Z`.
The workflow builds the installer and prepares a **draft** release holding
it and its `.sha256` file, with the notes taken from the two changelogs
(English first, French below). It refuses to do so if the tag and the
`VERSION` file disagree, or if a changelog lacks the section. Nothing is
public until you publish the draft. Step by step: `CONTRIBUTING.md`.

## Tests

The engine is the one of nautilus-convert-images, with its unit tests
unchanged. There, it was validated by pixel-by-pixel comparison
(PNG/TIFF/BMP/WebP bit-identical to the source, including with an alpha
channel and 16-bit depth; JPEG with a minimal, expected difference at
quality 100). The LibRaw fallback is covered by unit tests on a synthetic
DNG.

```bash
# Rust: the engine and the platform-independent part of the menu handler
cargo test

# C#: the logic of the window, against the real engine when it is built
cargo build --release -p convert-images-engine
dotnet run -c Release --project app/ConvertImages.Core.Tests
```

Both run on Linux and on Windows. The tests also check what is written in
two places (version, program names, file extensions of the menu entry,
texts of the window and of the engine) and the translations: complete
catalogs, placeholders identical to the source text. Tests that need a
missing tool or a built engine are skipped, not failed.

The workflow also runs, on a real Windows: the Windows-only tests of the
menu handler (its COM object), the C# tests against the Windows engine, and
`selftest`, which starts the built window and then goes through the whole
chain — menu handler, then window — the way File Explorer does. `selftest`
is a development tool (see `CONTRIBUTING.md`): started without argument on a
Windows where the program is installed, it checks the installed chain the
same way. The Windows programs are also tried under Wine on Linux
(`CONTRIBUTING.md`).

## Toolchain note

* **Rust** — `rust-toolchain.toml` pins the version used by those who have
  `rustup` (the workflow does). The engine keeps, unchanged, the dependency
  versions pinned by nautilus-convert-images (see the "Toolchain note" of
  its README), so that the two copies of the engine stay easy to compare.
  The menu handler uses the `windows` crate, which needs Rust 1.82 or
  later.
* **Windows target** — the Rust programs are built for
  `x86_64-pc-windows-gnu` (MinGW-w64), from Linux: the build script of the
  bundled LibRaw does not support the Microsoft compiler. The C and C++
  runtimes are linked statically: the programs need no extra DLL.
* **.NET** — .NET 10 SDK (`global.json`); the Windows App SDK version is
  pinned in `app/ExplorerConvertImages/ExplorerConvertImages.csproj`.

## Layout

```
explorer-convert-images/
├── engine/                      convert-images-engine (Rust): copy of the
│   │                            nautilus-convert-images engine, see UPSTREAM.md
│   ├── src/
│   └── vendor/libraw-sys/       LibRaw, compiled into the engine
├── shell-verb/                  explorer-convert-images-verb (Rust)
│   ├── src/                     registry entries, COM object, launch of the window
│   └── examples/                selftest and simulate_explorer (test tools)
├── app/
│   ├── ConvertImages.Core/      what the window does, without the window (C#)
│   ├── ConvertImages.Core.Tests/ its tests, and the .pot generator
│   └── ExplorerConvertImages/   the WinUI 3 window (C#)
├── po/                          translations (fr, de, es) and their template
├── installer/installer.nsi      the NSIS installer
├── tools/                       set-version.sh, release-notes.sh, make_icon.py
├── build-installer.sh           builds the installer, on Linux
├── .github/workflows/build.yml  tests, builds and packages everything
├── VERSION                      the version of the project
├── Cargo.toml, Cargo.lock       Rust workspace (engine + shell-verb)
└── README, ARCHITECTURE, PROJECT_CONTEXT, CONTRIBUTING, CHANGELOG,
    CODE_OF_CONDUCT, SECURITY    (each in English and in French)
```

## Security

To report a vulnerability, please do not open a public issue: follow the [Security Policy](SECURITY.md).

## License

GPL-3.0-only. See [LICENSE](LICENSE).

LibRaw, built into the engine, keeps its own licenses: see `LICENSE.LGPL`
and `LICENSE.CDDL` in `engine/vendor/libraw-sys/LibRaw/`.

The code of conduct ([CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md)) is the
Contributor Covenant 3.0, adapted for this project; it is distributed under
its own license, [CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/).

explorer-convert-images is developed by Gilles MAGNEVILLE. Gilles is not a
professional developer — this project was built with the help of Claude.ai.
