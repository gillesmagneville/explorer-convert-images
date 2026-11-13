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
* **Everything you have at hand** — RAW from many cameras, JPEG, PNG, TIFF,
  BMP, GIF, WebP, even mixed in a single folder: the format of each file is
  detected, even when the extension is missing or misleading.
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
> sign of malware. Click **More info**, then **Run anyway** to proceed.

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

## Usage

Right-click on a selection (files and/or folders), on a folder, or on the
background of an open folder → **Convert images…**.

* **On Windows 11**, the entry is in the classic menu: right-click, then
  **Show more options** (or Shift + right-click, or Shift+F10).
* The **Convert images** shortcut of the Start menu opens the same window
  with nothing selected; it then offers to choose a folder to convert.

The window shows what was found (subfolders included), lets you choose the
destination format, the JPEG quality, what to do when a file already exists
and the destination folder, then converts and logs each file.

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

## Formats

| | Formats |
|---|---|
| **Sources** | RAW (recognized extensions: CR2/CR3, NEF/NRW, ARW/SRF/SR2, DNG, ORF, RAF, RW2, PEF, SRW, X3F, 3FR, RWL, GPR…), JPEG, PNG, TIFF, BMP, GIF, WebP |
| **Destinations** | JPEG (adjustable quality, 100 by default), PNG, TIFF (uncompressed), BMP, lossless WebP (VP8L) |

PNG, TIFF, BMP and WebP are **genuinely lossless** (alpha channel and 16-bit
included). JPEG remains a lossy format by nature — a limit of the format
itself, not of this tool. Detection is done by extension, then by content
analysis, with an actual decoding attempt as a last resort. RAW files are
decoded at full resolution (active sensor area).

The engine is the one of nautilus-convert-images: what it reads, and how
well, is described in the
["RAW compatibility"](https://github.com/gillesmagneville/nautilus-convert-images#raw-compatibility)
section of that project. `engine/UPSTREAM.md` says which version this
repository holds and what was changed for Windows.

## How it is made

Three programs, installed in the same folder (see `ARCHITECTURE.md`):

| Program | Role | Language |
|---|---|---|
| `explorer-convert-images-verb.exe` | Receives the selection from File Explorer's menu and opens the window on it | Rust |
| `ExplorerConvertImages.exe` | The window (WinUI 3, the native Windows 11 look) | C# |
| `convert-images-engine.exe` | Detects the formats and converts; usable on its own from a command line | Rust |

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

Each run of the workflow also keeps, as the `explorer-convert-images-folder`
artifact, the complete folder that the installer installs (the window and
the two Rust programs); `./build-installer.sh --dist FOLDER` turns such a
folder into an installer. Details and variants: `CONTRIBUTING.md`.

### Publishing a release

Set the version (`tools/set-version.sh X.Y.Z`), give the release its section
in `CHANGELOG.md` and `CHANGELOG.fr.md`, commit, then push a tag `vX.Y.Z`.
The workflow builds the installer and prepares a **draft** release holding
it and its `.sha256` file, with the notes taken from the two changelogs
(English first, French below). Nothing is public until you publish the
draft. Step by step: `CONTRIBUTING.md`.

## Tests

```bash
# Rust: the engine and the platform-independent part of the menu handler
cargo test

# C#: the logic of the window, against the real engine when it is built
cargo build --release -p convert-images-engine
dotnet run -c Release --project app/ConvertImages.Core.Tests
```

Both run on Linux and on Windows. The workflow also runs, on a real
Windows: the Windows-only tests of the menu handler (its COM object), the
C# tests against the Windows engine, and `selftest`, which starts the built
window and then goes through the whole chain — menu handler, then window —
the way File Explorer does. `selftest` is a development tool (see
`CONTRIBUTING.md`): started without argument on a Windows where the program
is installed, it checks the installed chain the same way.

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
