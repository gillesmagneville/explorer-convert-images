// SPDX-License-Identifier: GPL-3.0-only

//! File extensions on which the "Convert images…" entry is offered.

/// Extensions (lowercase, without the dot) known to the engine: standard
/// formats first, then RAW formats. Must stay identical to the tables of
/// `engine/src/formats.rs` — the test below fails otherwise.
pub const IMAGE_EXTENSIONS: &[&str] = &[
    // Standard formats
    "jpg", "jpeg", "jpe", "jfif", "png", "tif", "tiff", "bmp", "gif", "webp",
    // RAW formats
    "cr2", "cr3", "crw", "nef", "nrw", "arw", "srf", "sr2", "dng", "orf", "raf", "rw2", "pef",
    "srw", "x3f", "3fr", "fff", "erf", "mrw", "mos", "dcr", "kdc", "k25", "iiq", "raw", "rwl",
    "gpr",
];

#[cfg(test)]
mod tests {
    use super::*;
    use std::collections::BTreeSet;

    /// Extracts the extensions of the two tables of `formats.rs`: the string
    /// literals on the left of each `=>` between `fn raw_extension_label` and
    /// `pub fn has_known_image_extension`.
    fn engine_extensions() -> BTreeSet<String> {
        let path = concat!(env!("CARGO_MANIFEST_DIR"), "/../engine/src/formats.rs");
        let source = std::fs::read_to_string(path).expect("engine/src/formats.rs must be readable");
        let start = source
            .find("fn raw_extension_label")
            .expect("raw table not found");
        let end = source
            .find("pub fn has_known_image_extension")
            .expect("end marker not found");
        let mut found = BTreeSet::new();
        for line in source[start..end].lines() {
            let Some((patterns, _)) = line.split_once("=>") else {
                continue;
            };
            for (i, piece) in patterns.split('"').enumerate() {
                if i % 2 == 1 {
                    found.insert(piece.to_string());
                }
            }
        }
        found
    }

    #[test]
    fn list_matches_the_engine_tables() {
        let ours: BTreeSet<String> = IMAGE_EXTENSIONS.iter().map(|e| e.to_string()).collect();
        assert_eq!(
            ours.len(),
            IMAGE_EXTENSIONS.len(),
            "duplicate extension in the list"
        );
        assert_eq!(ours, engine_extensions());
    }

    #[test]
    fn extensions_are_lowercase_without_dot() {
        for ext in IMAGE_EXTENSIONS {
            assert!(!ext.is_empty() && !ext.contains('.'), "{ext}");
            assert_eq!(*ext, ext.to_lowercase(), "{ext}");
        }
    }
}
