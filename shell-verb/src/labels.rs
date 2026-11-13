// SPDX-License-Identifier: GPL-3.0-only

//! Text of the context-menu entry, in the languages of the application.
//!
//! The entry is a static registry value read by File Explorer itself: it is
//! written once, at installation time, in the display language of the user
//! who installs (English when that language is not one of ours).

/// (primary language identifier, ISO code, label). The translations must stay
/// identical to those of `po/*.po` for the msgid `MENU_LABEL_MSGID` — the test
/// below fails otherwise.
const LABELS: &[(u16, &str, &str)] = &[
    (0x09, "en", "Convert images…"),
    (0x0c, "fr", "Convertir les images…"),
    (0x07, "de", "Bilder konvertieren …"),
    (0x0a, "es", "Convertir imágenes…"),
];

#[cfg(test)]
const MENU_LABEL_MSGID: &str = "Convert images…";

/// Label for a Windows language identifier (LANGID, as returned by
/// `GetUserDefaultUILanguage`). Its low 10 bits are the primary language.
pub fn menu_label(lang_id: u16) -> &'static str {
    let primary = lang_id & 0x03ff;
    LABELS
        .iter()
        .find(|(id, _, _)| *id == primary)
        .map_or(LABELS[0].2, |(_, _, label)| label)
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn known_languages_and_fallback() {
        assert_eq!(menu_label(0x040c), "Convertir les images…"); // fr-FR
        assert_eq!(menu_label(0x0c0c), "Convertir les images…"); // fr-CA
        assert_eq!(menu_label(0x0407), "Bilder konvertieren …"); // de-DE
        assert_eq!(menu_label(0x0c0a), "Convertir imágenes…"); // es-ES
        assert_eq!(menu_label(0x0409), "Convert images…"); // en-US
        assert_eq!(menu_label(0x0411), "Convert images…"); // ja-JP -> English
    }

    /// Returns the msgstr of `msgid` in a .po file (single-line entries only,
    /// which is the case of the menu label).
    fn po_translation(lang: &str, msgid: &str) -> String {
        let path = format!("{}/../po/{lang}.po", env!("CARGO_MANIFEST_DIR"));
        // Whatever the line endings Git checked the file out with.
        let content = std::fs::read_to_string(&path)
            .unwrap_or_else(|e| panic!("{path}: {e}"))
            .replace("\r\n", "\n");
        let needle = format!("msgid \"{msgid}\"\nmsgstr \"");
        let start = content
            .find(&needle)
            .unwrap_or_else(|| panic!("{msgid} not in {path}"))
            + needle.len();
        let end = content[start..].find('"').unwrap() + start;
        content[start..end].to_string()
    }

    /// The last page of the installer quotes the menu entry, in the same
    /// languages: it must name it as it really appears.
    #[test]
    fn labels_are_those_quoted_by_the_installer() {
        let path = concat!(env!("CARGO_MANIFEST_DIR"), "/../installer/installer.nsi");
        let installer = std::fs::read_to_string(path).unwrap_or_else(|e| panic!("{path}: {e}"));
        for (_, lang, label) in LABELS {
            assert!(
                installer.contains(label),
                "installer.nsi does not quote the {lang} label"
            );
        }
    }

    #[test]
    fn labels_match_the_po_catalogs() {
        for (_, lang, label) in LABELS {
            if *lang == "en" {
                assert_eq!(*label, MENU_LABEL_MSGID);
            } else {
                assert_eq!(*label, po_translation(lang, MENU_LABEL_MSGID), "{lang}");
            }
        }
    }
}
