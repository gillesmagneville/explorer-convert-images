<!-- SPDX-License-Identifier: GPL-3.0-only -->

# PROJECT_CONTEXT.md

🇫🇷 [Version française](PROJECT_CONTEXT.fr.md)

# explorer-convert-images

**explorer-convert-images** is an open-source add-on for Windows File
Explorer that converts images — including camera RAW files — between common
formats, straight from the context menu.

It is the Windows counterpart of
[nautilus-convert-images](https://github.com/gillesmagneville/nautilus-convert-images),
which does the same in GNOME Files on Linux. The two projects share their
goal, their principles, their conversion engine and their wording; each has
the integration and the window that belong to its system.

Its goal is to make image conversion as simple as a right-click, without ever
sacrificing image quality or forcing the user to open a terminal or an image
editor.

explorer-convert-images is aimed at:

* amateur and semi-professional photographers
* Windows users in general
* people who receive RAW or TIFF files and just need a JPEG (or the other
  way round) without knowing anything about photography
* people who prepare workstations for others

It is designed to work **entirely locally**: no account, no online service,
no telemetry.

> **Note on this document:** PROJECT_CONTEXT.md defines *why* the project
> exists and *what it values*. It does not redefine the exact contracts (the
> engine's JSON schema, the registry entries, what must change together) —
> those live once, canonically, in `ARCHITECTURE.md`, so that the two
> documents cannot silently drift apart.

---

# Vision

Converting a batch of photos on Windows usually means installing and
learning a full application, or trusting an online service with one's files,
for a task that should be trivial.

explorer-convert-images aims to answer one simple question:

> **"I selected some photos in my file manager, I want them in another
> format, without losing anything — how?"**

Rather than replacing advanced RAW processing tools, it only takes care of
format conversion, with no editing, exactly as the camera itself would when
exporting a JPEG from its own screen.

---

# Project philosophy

The first five principles are those of nautilus-convert-images; the last
two belong to the Windows version.

## 1. Never lose more than the format requires

PNG, TIFF, BMP and WebP (lossless mode) are re-encoded with no loss of
information whatsoever. JPEG remains by nature a lossy format: that cannot
be worked around, and the tool never claims otherwise. The default quality
(100) minimizes that loss as much as the format allows.

## 2. Never crop, never resize

No step of the pipeline crops or reduces the resolution of an image. A RAW
image is decoded at the exact resolution of the sensor's active area.

## 3. Read-only on source files

explorer-convert-images never modifies or deletes the original file. It
reads, converts, and writes a new file elsewhere (or under a new name).

## 4. Fit into the existing workflow, don't create a new one

No new application to learn: the entry point is the context menu of File
Explorer, which the user already knows. The window only opens on demand, for
this one task, and looks like the rest of Windows.

## 5. Fast

Decoding and encoding use native code (Rust, with C++ LibRaw as a fallback
for RAW files) and take advantage of every available core for batch
conversions.

## 6. A guest in File Explorer, never a risk for it

Nothing of the project is loaded into File Explorer. The menu entry starts
separate programs: whatever goes wrong on our side, the user's file manager
keeps working.

## 7. Leave the system as it was found

Installed for the current user only, without administrator rights. The
uninstaller removes everything the installer wrote — files and registry —
and nothing that belongs to another program.

---

# Project scope

explorer-convert-images focuses on:

* image format detection (standard and RAW)
* format conversion, without editing
* integration into the context menu of File Explorer
* management of the destination folder (including creating a new folder) and
  of file-name collisions

explorer-convert-images is **not**:

* a RAW editor (no exposure adjustment, manual white balance, curves or
  cropping)
* a tool for renaming or organizing a photo library
* an online conversion service or a background program
* a thumbnail or preview provider for File Explorer

---

# Target platforms

Windows 10 (version 1809 or later) and Windows 11, 64-bit (x64).

Windows on ARM is not a target of its own: the x64 programs are expected to
run there through the emulation built into Windows 11, but this has not
been verified.

---

# User experience

Using explorer-convert-images must stay simple:

1. Select files and/or folders in File Explorer.
2. Right-click → **Convert images…** ("Convertir les images…" in French,
   "Convertir imágenes…" in Spanish, "Bilder konvertieren …" in German: the
   language follows that of Windows). On Windows 11: under **Show more
   options**.
3. Choose the destination format and the output folder.
4. Convert.

No prior configuration step, no account, no network connection required.

---

# Formats

**Detected source formats**: JPEG, PNG, TIFF, BMP, GIF, WebP, and a wide
range of camera RAW formats (Canon, Nikon, Sony, Adobe DNG, Olympus,
Fujifilm, Panasonic, Pentax, Samsung, Sigma, Hasselblad, Leica, GoPro...).
The destination formats and their guarantees (lossless or not) are those of
the engine, defined in the `ARCHITECTURE.md` of nautilus-convert-images.

---

# Reliability and transparency

explorer-convert-images never claims that a conversion is lossless when it
is not (the case of JPEG). When a limit exists, it is documented and
displayed, not hidden.

The same goes for the project itself: what has been verified and what has
not is written down (see the status note of the README and the CHANGELOG),
and what can be checked by a test is checked by a test rather than by a
promise.

---

# One engine, two projects

The conversion engine is developed in nautilus-convert-images; this
repository holds a copy, adapted so that it builds and runs on Windows. The
choice of a copy over a shared dependency keeps each project complete and
independent, at the price of carrying engine fixes over by hand. To keep
that price low:

* the copy stays as close as possible to its source, differences being
  confined to what Windows requires and listed in `engine/UPSTREAM.md`;
* behaviour of the engine is not changed here: a fix or a feature belongs
  upstream first.

---

# Extensibility

Adding a new destination format must only touch a small number of
well-identified points: the engine, and the list of formats of the window —
not the menu handler. Adding a new source format by its extension touches
the table of the engine and the list of file types that get the menu entry
(a test makes sure the two agree). The exact contract (what to add where) is
defined in `ARCHITECTURE.md`. As the engine is developed in
nautilus-convert-images, the engine part of such a change is made there
first (see "One engine, two projects").

---

# Performance goals

* Detection: near-instant, even on several thousand files
* Conversion: parallelized across all available cores
* A selection of any size, handed over in one go
* No resident background program: the menu handler runs for the instant of
  a click, the window and the engine for the time of a conversion
* No telemetry, no dependency on an online service

---

# Code principles

* Stable Rust only — no nightly features
* `unsafe` Rust is confined to the calls into LibRaw (engine) and into
  Windows (menu handler), each with its justification
* The window holds as little logic as possible: what can be decided without
  a window lives in a library that is tested on every system
* A dependency is only added if it brings a clear net benefit — see
  "Dependency philosophy" in `ARCHITECTURE.md`
* What is written in two places is guarded by a test — see "What must
  agree" in `ARCHITECTURE.md`

---

# Future vision

Future versions might include:

* a code-signed installer, which would remove the SmartScreen warning
* an entry in the first-level context menu of Windows 11
* whatever the engine gains upstream (new formats, options)
* additional interface languages

None of these is required for the tool to be useful.

---

# Non-goals

explorer-convert-images will never try to become:

* a full RAW editor
* a photo library manager
* a general-purpose batch processing tool (resizing, watermarking,
  filters...)

It must remain a focused, predictable format conversion tool.

---

# Project values

explorer-convert-images values:

* fidelity to the source file
* clarity (saying when a format is inherently limited, rather than hiding
  it)
* speed
* ease of integration (a context menu, not a new application to learn)
* respect for the user's system
* transparency about how the tool works and what its limits are

Above all:

**A conversion must never surprise the user — neither through an
unannounced loss of quality, nor through cropping, nor through a reduced
resolution.**
