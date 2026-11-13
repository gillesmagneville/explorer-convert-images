<!-- SPDX-License-Identifier: GPL-3.0-only -->

# vendor/

🇫🇷 [Version française](README.fr.md)

## libraw-sys

Copy of [`rsraw-sys` 0.1.1](https://crates.io/crates/rsraw-sys) (MIT
license): the Rust bindings to **LibRaw 0.21.3**, whose C++ source code
is included (`libraw-sys/LibRaw/`, with the `LICENSE.LGPL` and
`LICENSE.CDDL` licenses kept) and compiled **statically** into
`convert-images-engine`. No LibRaw library therefore needs to be installed
on the machine, neither to build nor to run.

LibRaw serves as a **fallback decoder** when `rawloader` cannot read a
file (typically a camera missing from its database) — see
`src/raw.rs` and, in nautilus-convert-images, `ARCHITECTURE.md`, section
"RAW decoding".

### Differences from the version published on crates.io

* `build.rs`: the bindings are always taken from the pre-generated file
  `src/bindings.rs`; `bindgen` is no longer called. This avoids requiring
  `libclang` on the build machine. Only a C++ compiler (`g++`, provided
  by `build-essential`) is needed.
* `Cargo.toml`: the build dependency on `bindgen` is removed.

These two changes come from nautilus-convert-images. This repository adds
three, needed to build for Windows (see also `../UPSTREAM.md`):

* `build.rs`: for Windows with the GNU toolchain (MinGW-w64), the C++
  runtime is linked statically; otherwise the program would not start
  without `libstdc++-6.dll` next to it.
* `build.rs`: an edit of `src/bindings.rs` now triggers a rebuild (it was
  silently ignored).
* `src/bindings.rs`: on Windows, `time_t` is 64 bits wide. The bindings
  were generated on Linux, where it is a `long`; on 64-bit Windows a `long`
  is 32 bits wide, and every field located after a `time_t` would have been
  read at the wrong place. With this change the Rust structures have,
  on Windows too, exactly the layout the C++ code uses (checked field by
  field against the MinGW-w64 compiler).

Nothing else is changed.

### Updating LibRaw

Replace the content of `libraw-sys/` with a more recent version of
`rsraw-sys`, re-apply the changes above, then run `cargo test` (the
tests in `src/libraw.rs` decode a synthetic DNG) — natively, and for the
Windows target (see `CONTRIBUTING.md`, "Trying the Windows programs on
Linux, with Wine").
