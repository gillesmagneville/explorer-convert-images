<!-- SPDX-License-Identifier: GPL-3.0-only -->

# Changelog

🇫🇷 [Version française](CHANGELOG.fr.md)

All notable changes to this project are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/).

Each release has a section whose heading is its date and its version, in
this exact form: `## 2026-11-20 — v0.1.0`. The release notes published on
GitHub are taken from it (see "Publishing a release" in `CONTRIBUTING.md`).

## [Unreleased]

### Changed

- Documentation brought up to the level of nautilus-convert-images (English
  and French): the README now holds the "RAW compatibility" section in full,
  a table of typical uses, what the installer does, the command-line use of
  the engine, how to get set up and what the tests cover;
  `ARCHITECTURE.md` describes the engine (format detection, RAW decoding,
  conversion); `CONTRIBUTING.md` explains how to add a destination format;
  `PROJECT_CONTEXT.md` gains an "Extensibility" section.

## 2026-11-13 — v1.0.0

First release of the project: the Windows counterpart of
nautilus-convert-images, as three programs, an installer and a build
workflow.

### Added

- **The menu handler** (`explorer-convert-images-verb.exe`, Rust): adds
  **Convert images…** to the context menu of File Explorer — on the image
  types the engine knows, on folders and on the background of a folder —
  and opens the window on the selection. It is an out-of-process COM
  handler: nothing is loaded into File Explorer, the whole selection
  arrives in one call whatever its size, and everything is registered for
  the current user only.
- **The window** (`ExplorerConvertImages.exe`, C#, WinUI 3): what was
  detected, destination format (JPEG by default), JPEG quality, what to do
  when a file exists, destination folder (with creation of a new folder),
  live progress and log, confirmation before quitting during a conversion.
  Started without anything to convert, it explains how to use the menu
  entry and offers to choose a folder.
- **The engine** (`convert-images-engine.exe`, Rust): the engine of
  nautilus-convert-images 1.3.5, adapted to Windows. New `--input-list`
  option of `detect` and `convert`, to receive a selection of any size
  through a file.
- **Four languages**: English, French, Spanish and German, for the menu
  entry, the window and the installer. The translations of the Linux
  version are reused.
- **The installer** (NSIS): per user, without administrator rights; it
  removes the previous version on upgrade, and the uninstaller removes
  every file and registry entry of the program.
- **A GitHub Actions workflow** that tests the three programs, builds them
  (the Rust programs on Linux, the window on Windows), checks them together
  on a real Windows, and produces the installer with its `.sha256` file; on
  a version tag, it prepares a draft release.
- **Tests**: of the engine, of the menu handler (registry entries, COM
  object), of the logic of the window against the real engine, of the
  translations — and tests that compare what is written in two places
  (version, program names, file extensions, texts).

### Verified so far, and not

- Verified on Linux: the Rust and C# tests; the two Rust programs built for
  Windows and tested under Wine (including the engine on files with
  accented names, the COM object, and the registration and removal of the
  menu entries); the installer under Wine (installation, upgrade, refusal
  of a folder that is not ours, uninstallation).
- Verified on a real Windows, by the build workflow: the C# tests against
  the Windows engine; the Windows-only tests of the menu handler; the build
  of the window; and a start-up check — the window opens and stays open,
  started directly and through the menu handler (the COM object) the way
  File Explorer does it.
- Verified by hand on Windows 11: the installer; the menu entry used from
  File Explorer itself (on one image, on several, on a folder and on the
  background of a folder); the conversions; the uninstallation.
- **Not verified**: Windows 10, and Windows on ARM (where the programs run
  through the emulation built into Windows 11). The Spanish and German
  translations have not been reviewed by native speakers.

### Known limitations

- On Windows 11, the entry is in the classic menu (**Show more options**,
  or Shift + right-click). Appearing in the first-level menu requires a
  signed package with an identity, which the project does not provide yet.
- The installer is not code-signed: Windows or the browser may show a
  warning about an unknown publisher when it is downloaded or started.
