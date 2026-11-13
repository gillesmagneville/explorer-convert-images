<!-- SPDX-License-Identifier: GPL-3.0-only -->

# Where this engine comes from

🇫🇷 [Version française](UPSTREAM.fr.md)

`engine/` is a copy of the conversion engine of
[nautilus-convert-images](https://github.com/gillesmagneville/nautilus-convert-images),
where it is developed.

| | |
|---|---|
| Copied from | nautilus-convert-images **1.3.5**, commit `3019d43` (2026-11-03) |
| Files | upstream `src/` → `engine/src/`, `vendor/` → `engine/vendor/`, `Cargo.toml` → `engine/Cargo.toml`; `Cargo.lock` is at the root of this repository |

## Differences from upstream

Only what Windows requires. Behaviour on Linux is unchanged: the upstream
tests still pass, unmodified.

* **`src/libraw.rs`** — on Unix, LibRaw opens the file itself from its path,
  as upstream. Elsewhere, the file is read by Rust and handed to LibRaw as a
  memory buffer (`libraw_open_buffer`): LibRaw's path API only understands
  the legacy code page of Windows, so a path with other characters
  (accents, depending on the language of the system) or longer than 260
  characters would fail.
* **`src/cli.rs`, `src/main.rs`** — new `--input-list <file>` option of
  `detect` and `convert`: more paths, one per line, read from a file. A
  Windows command line is limited to about 32,000 characters. An unreadable
  list is reported as a `fatal_error` event (`could not read the input
  list: …`). The description shown by `--help` names this project.
* **`Cargo.toml`** — the package takes its version, edition and license
  from the workspace of this repository. The dependencies and their pinned
  versions are those of upstream.
* **`vendor/libraw-sys`** — three changes, listed in `vendor/README.md`
  (static C++ runtime with MinGW-w64, width of `time_t` on Windows, a
  rebuild trigger).

## Carrying a change over from upstream

1. Compare upstream with this copy, for instance:
   `diff -ru ~/Projects/nautilus-convert-images/src ~/Projects/explorer-convert-images/engine/src`
   — apart from the differences above, the two must be identical.
2. Apply the upstream change to `engine/`, keeping the differences above.
3. If a message of the engine or the list of extensions changed, follow the
   failing tests (`cargo test`, then the C# tests): they name what must
   follow in `shell-verb/src/extensions.rs` and in `Msg.cs`.
4. Update the table at the top of this file (version and commit), in both
   languages.
