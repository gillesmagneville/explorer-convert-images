<!-- SPDX-License-Identifier: GPL-3.0-only -->

# Contributing to explorer-convert-images

🇫🇷 [Version française](CONTRIBUTING.fr.md)

Thank you for your interest in this project! This document explains how to
report a problem, suggest an improvement, or contribute code.

By taking part in this project, you agree to follow its
[Code of Conduct](CODE_OF_CONDUCT.md).

To report a **security problem**, please do not open a public issue: follow
the [Security Policy](SECURITY.md).

## Reporting a bug

Open an issue with, as far as you can:

- the version of the program (**Settings > Apps > Installed apps**) and
  your version of Windows (`winver`);
- what you did, what you expected, what happened;
- the text of the log shown in the window, if the problem is a conversion;
- the content of these two files of your temporary folder (type `%TEMP%` in
  the address bar of File Explorer), if they exist:
  `explorer-convert-images-verb.log` (the menu entry) and
  `explorer-convert-images-app.log` (the window);
- if the problem comes from a particular file, the file itself — only if
  you may share it.

A RAW file that the engine cannot read is better reported to
[nautilus-convert-images](https://github.com/gillesmagneville/nautilus-convert-images/issues),
where the engine is developed.

## Suggesting an improvement

Open an issue describing the need rather than the solution. Please read
`PROJECT_CONTEXT.md` first: the project deliberately stays a focused format
converter.

## Contributing code

The project has three programs in two languages (see `ARCHITECTURE.md`):

| Part | Where | Builds on |
|---|---|---|
| Engine and menu handler (Rust) | `engine/`, `shell-verb/` | Linux, for Linux (tests) and for Windows |
| Logic of the window (C#) | `app/ConvertImages.Core/` | Linux and Windows |
| The window itself (C#, WinUI 3) | `app/ExplorerConvertImages/` | Windows only |

You do not need a Windows machine to work on most of it: the GitHub Actions
workflow builds and tests everything, including the window, on each push
and pull request.

### Setting up on Linux (Ubuntu/Debian)

```bash
git clone git@github.com:gillesmagneville/explorer-convert-images.git ~/Projects/explorer-convert-images
cd ~/Projects/explorer-convert-images

# Rust tests, and the C# tests against the real engine
sudo apt-get install build-essential dotnet-sdk-10.0
cargo test
cargo build --release -p convert-images-engine
dotnet run -c Release --project app/ConvertImages.Core.Tests
```

To also build the Windows programs and the installer:

```bash
sudo apt-get install nsis gcc-mingw-w64-x86-64-posix g++-mingw-w64-x86-64-posix
rustup target add x86_64-pc-windows-gnu
cargo build --release --target x86_64-pc-windows-gnu
```

The Windows target needs a Rust installed with `rustup`: the Rust packaged
by distributions does not include it. `rust-toolchain.toml` then selects
the Rust version by itself.

### Setting up on Windows

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download). Then, in
PowerShell, at the root of the repository:

```powershell
dotnet run -c Release --project app/ConvertImages.Core.Tests
dotnet publish app/ExplorerConvertImages -c Release -r win-x64 -p:Platform=x64 -o dist\explorer-convert-images
```

The published folder lacks the two Rust programs. Take them from the
`rust-windows` artifact of a workflow run (or build them on Linux) and copy
them into it; `dist\explorer-convert-images\ExplorerConvertImages.exe` can
then be started, with files or folders as arguments. To get the menu entry
without installing, run `explorer-convert-images-verb.exe --register` from
that folder (`--unregister` removes it).

### Building the installer

On Linux, from a folder holding the published window:

```bash
./build-installer.sh --app FOLDER
```

or from a complete folder (the `explorer-convert-images-folder` artifact of
a workflow run):

```bash
./build-installer.sh --dist FOLDER
```

The installer and its `.sha256` file are written to `dist/`.

### Checking the whole chain on Windows

`selftest` does what File Explorer does on **Convert images…** and checks
that a window of the application comes on the screen, is still there twenty
seconds later, and that the application logged no error. Build it with Rust
on Windows:

```powershell
cargo build -p explorer-convert-images-verb --example selftest
target\debug\examples\selftest.exe
```

Without argument it uses the installed program. `simulate_explorer.exe
PATH...` (built the same way) invokes the installed menu handler on the
paths you give, which is handy to try a large selection.

### Trying the Windows programs on Linux, with Wine

The Windows tests of the two Rust programs run under Wine:

```bash
sudo apt-get install wine
CARGO_TARGET_X86_64_PC_WINDOWS_GNU_RUNNER=wine cargo test --target x86_64-pc-windows-gnu
```

The installer runs under Wine too (it is a 32-bit program: it needs the
`wine32` package). Wine has two limits to know about: it does not let File
Explorer's COM calls cross from one program to another for the two
interfaces the handler uses, and it does not see classes registered for the
current user only. The complete chain — menu, handler, window — can
therefore only be exercised on a real Windows; the window itself does not
run under Wine.

## Adding a new destination format

This is the most common kind of contribution. The exact contract (the
lossless/depth/alpha table, the code points to change) is defined in
`ARCHITECTURE.md`, in the "Conversion" section ("Extension point — adding a
new destination format"). The engine is developed in nautilus-convert-images
(see "Changing the engine" below): the format is added there first, then
carried over to `engine/`. In short:

1. `engine/src/convert.rs` — new `TargetFormat` variant + encoding case.
2. `engine/src/main.rs` — entry in the output of the `formats` subcommand.
3. `app/ConvertImages.Core/Engine/ConversionOptions.cs` — entry in
   `Choices.Formats`, with its text in `Localization/Msg.cs` and its
   translations (see "Translating the interface"); the test that lists the
   formats, in `app/ConvertImages.Core.Tests/EngineTests.cs`, follows.
4. `ARCHITECTURE.md` — update the formats table.

### Rules for any new format

- Never advertise a format as "lossless" if it really is not — see
  "Reliability and transparency" in `PROJECT_CONTEXT.md`. If in doubt, check
  with a pixel-by-pixel comparison (see the "Tests" section of `README.md`
  for the method used so far).
- No cropping or resizing added, even as an option, without discussing it in
  an issue first — it is a founding principle of the project
  (`PROJECT_CONTEXT.md`, principle no. 2).

## Translating the interface

The texts are in `app/ConvertImages.Core/Localization/Msg.cs` (English) and
their translations in `po/*.po` (gettext catalogs, editable with a text
editor or Poedit).

**To correct a translation**, edit the `.po` file, then run the C# tests:
they check that every text is translated and keeps its `{placeholders}`.

**To add or change a text**, edit `Msg.cs`, then regenerate the template
and update each catalog:

```bash
dotnet run -c Release --project app/ConvertImages.Core.Tests -- --write-pot
for po in po/*.po; do msgmerge --update --no-fuzzy-matching "$po" po/explorer-convert-images.pot; done
# translate the new entries in po/*.po, then:
dotnet run -c Release --project app/ConvertImages.Core.Tests
```

(`msgmerge` comes with the `gettext` package; the entries can also be
copied by hand from the `.pot`.)

**To add a language** (Italian, say):

1. Copy `po/explorer-convert-images.pot` to `po/it.po`; fill in its header
   (`Language: it`, `Plural-Forms`) and translate.
2. Add `"it"` to `Translator.TranslatedLanguages` (`Translator.cs`).
3. Add the text of the menu entry to `LABELS` in
   `shell-verb/src/labels.rs`, with the Windows identifier of the language.
4. In `installer/installer.nsi`: add the `MUI_LANGUAGE` line, a line to
   each `LangString` and to `VersionKeys`, and the name of the Start menu
   shortcut to `DeleteShortcuts`.
5. Run `cargo test` and the C# tests: they tell you what is missing.

The catalogs know two plural rules: plural from 2 (French) or plural for
everything but 1 (English, German, Spanish, Italian…). A language with
another rule needs `PoCatalog.PluralIndex` to be extended.

## Changing the engine

The engine is developed in nautilus-convert-images; `engine/` is a copy
(`engine/UPSTREAM.md`). A fix or a new feature of the engine belongs there
first, then is carried over here. Here, only change what Windows requires,
and list it in `engine/UPSTREAM.md`.

If a message of the engine changes, update its template in `Msg.cs` (see
"Engine messages" in `ARCHITECTURE.md`); if an image extension is added,
add it to `shell-verb/src/extensions.rs`. The tests fail until both sides
agree.

## Updating the Windows App SDK or .NET

The Windows App SDK version is the `PackageReference` of
`app/ExplorerConvertImages/ExplorerConvertImages.csproj`; the .NET SDK
version is in `global.json` and in the workflow. Change one thing at a
time, in a pull request of its own: the workflow builds the window and
starts it (`selftest`), which is what tells whether the new version works.

After an update of the Windows App SDK, see whether
`app/ExplorerConvertImages/PublishWinUIResources.targets` is still needed:
it makes up for a file `dotnet publish` leaves out, and its header says how
to tell. A build that prints no "added to the published files" line no
longer needs it.

## Publishing a release

1. Set the version everywhere it is written:
   `tools/set-version.sh X.Y.Z`
2. In `CHANGELOG.md` and `CHANGELOG.fr.md`, turn the `[Unreleased]`
   (`[Non publié]`) section into the section of the release, with a heading
   of this exact form — `## 2026-11-20 — vX.Y.Z` — and open a new, empty
   `[Unreleased]` section above it.
3. Check the notes that will be published:
   `tools/release-notes.sh X.Y.Z`
4. Commit, then tag and push:
   `git tag vX.Y.Z && git push origin main vX.Y.Z`
5. Wait for the workflow. On a tag it creates a **draft** release with the
   installer, its `.sha256` file and the notes (English first, French
   below). Review the draft on the Releases page, try the installer on a
   Windows machine, then publish.

The workflow refuses to create the release if the tag and the `VERSION`
file disagree, or if either changelog lacks the section.

## Code style

- `cargo fmt` before opening a pull request.
- Stable Rust only, no nightly features.
- The window holds as little logic as possible: what can be decided without
  a window goes to `ConvertImages.Core`, with a test.
- Nothing is ever loaded into File Explorer: the menu handler stays an
  out-of-process COM server.
- What must be written in two places gets a test that compares them (see
  "What must agree" in `ARCHITECTURE.md`).
- Any new dependency must be justified (see "Dependency philosophy" in
  `ARCHITECTURE.md`).

## Documentation

If your change alters observable behavior, update `CHANGELOG.md` and
`CHANGELOG.fr.md` in the same pull request, under `[Unreleased]`
(`[Non publié]` in the French file), and `ARCHITECTURE.md` /
`ARCHITECTURE.fr.md` if the technical contract changes.

Every document exists in two languages: English (`README.md`,
`PROJECT_CONTEXT.md`, `ARCHITECTURE.md`, `CONTRIBUTING.md`,
`CODE_OF_CONDUCT.md`, `SECURITY.md`, `CHANGELOG.md`, shown by default on
GitHub) and French (the same names with `.fr.md`). If you change one,
change the other. If you only write one language, say so in the pull
request and the maintainer will translate it.

## Questions

For any question that does not fit the cases above, simply open an issue.

## License

By contributing, you agree that your contributions will be released under
the same license as the project (GPL-3.0-only). The code of conduct is the
one exception: it is the Contributor Covenant 3.0, distributed under CC BY-SA 4.0.
