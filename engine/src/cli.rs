use clap::{Parser, Subcommand, ValueEnum};
use std::path::{Path, PathBuf};

use crate::convert::TargetFormat;
use crate::formats;

#[derive(Parser)]
#[command(
    name = "convert-images-engine",
    version,
    about = "Image conversion engine (RAW, JPEG, PNG, TIFF, BMP, WebP) of explorer-convert-images"
)]
pub struct Cli {
    #[command(subcommand)]
    pub command: Command,
}

#[derive(Subcommand)]
pub enum Command {
    /// Detects the format of the image files found in the given paths.
    /// Emits one JSON line per file on stdout, plus a final summary.
    Detect {
        /// Files and/or folders to analyze.
        paths: Vec<PathBuf>,
        /// Search subfolders.
        #[arg(short, long)]
        recursive: bool,
        /// File listing more files and/or folders to analyze, one per line
        /// (UTF-8). Avoids the length limit of the Windows command line
        /// (about 32,000 characters) with a very large selection.
        #[arg(long, value_name = "FILE")]
        input_list: Option<PathBuf>,
    },

    /// Converts the image files found to the target format.
    /// Emits one JSON progress line per file on stdout.
    Convert {
        /// Files and/or folders to convert.
        paths: Vec<PathBuf>,
        /// Search subfolders.
        #[arg(short, long)]
        recursive: bool,
        /// File listing more files and/or folders to convert, one per line
        /// (UTF-8). See `detect --input-list`.
        #[arg(long, value_name = "FILE")]
        input_list: Option<PathBuf>,
        /// Destination folder (created if needed).
        #[arg(short, long)]
        output_dir: PathBuf,
        /// Target format.
        #[arg(short, long, value_enum)]
        format: TargetFormat,
        /// JPEG quality (1-100), ignored for other formats.
        #[arg(long, default_value_t = 100)]
        quality: u8,
        /// What to do when the destination file already exists.
        #[arg(long, value_enum, default_value = "rename")]
        on_collision: CollisionPolicy,
        /// Number of threads (default: all available cores).
        #[arg(long)]
        threads: Option<usize>,
    },

    /// Lists the supported source and target formats (for the GUI).
    Formats,
}

#[derive(Debug, Clone, Copy, PartialEq, Eq, ValueEnum)]
pub enum CollisionPolicy {
    Skip,
    Rename,
    Overwrite,
}

/// Parses the content of an `--input-list` file: one path per line, UTF-8,
/// with or without a byte order mark, with Unix or Windows line endings.
/// Empty lines are ignored. A path is otherwise taken as it is (no trimming
/// of spaces: a file name may start with one).
pub fn parse_input_list(content: &str) -> Vec<PathBuf> {
    content
        .trim_start_matches('\u{feff}')
        .lines()
        .map(|line| line.trim_end_matches('\r'))
        .filter(|line| !line.trim().is_empty())
        .map(PathBuf::from)
        .collect()
}

/// Returns the paths given on the command line followed by those of the
/// `--input-list` file, if any.
pub fn gather_paths(
    mut paths: Vec<PathBuf>,
    input_list: Option<&Path>,
) -> Result<Vec<PathBuf>, String> {
    if let Some(list) = input_list {
        let bytes =
            std::fs::read(list).map_err(|e| format!("could not read the input list: {e}"))?;
        paths.extend(parse_input_list(&String::from_utf8_lossy(&bytes)));
    }
    Ok(paths)
}

/// Lists, recursively or not, the plausible image files found in a list
/// of paths (files and/or folders).
pub fn enumerate_images(paths: &[PathBuf], recursive: bool) -> Vec<PathBuf> {
    let mut out = Vec::new();
    for p in paths {
        collect_from_path(p, recursive, &mut out);
    }
    out
}

fn collect_from_path(path: &Path, recursive: bool, out: &mut Vec<PathBuf>) {
    if path.is_file() {
        // A file selected explicitly is always taken into account, even if its
        // extension is not recognized (detection by content will decide).
        out.push(path.to_path_buf());
        return;
    }
    if path.is_dir() {
        let walker = if recursive {
            walkdir::WalkDir::new(path)
        } else {
            walkdir::WalkDir::new(path).max_depth(1)
        };
        for entry in walker.into_iter().filter_map(|e| e.ok()) {
            let p = entry.path();
            if p.is_file() && (p == path || formats::has_known_image_extension(p)) {
                if p != path {
                    out.push(p.to_path_buf());
                }
            }
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn input_list_accepts_bom_crlf_and_blank_lines() {
        let content = "\u{feff}C:\\Photos d'été\\a b.cr2\r\n\r\n  \r\nD:\\x\\ü.nef\n/tmp/plain.jpg";
        let paths = parse_input_list(content);
        assert_eq!(
            paths,
            vec![
                PathBuf::from("C:\\Photos d'été\\a b.cr2"),
                PathBuf::from("D:\\x\\ü.nef"),
                PathBuf::from("/tmp/plain.jpg"),
            ]
        );
    }

    #[test]
    fn input_list_keeps_leading_spaces_of_a_name() {
        assert_eq!(parse_input_list(" a.jpg\n"), vec![PathBuf::from(" a.jpg")]);
    }

    #[test]
    fn gather_paths_appends_the_list_to_the_arguments() {
        let dir = std::env::temp_dir().join(format!("cie-list-{}", std::process::id()));
        std::fs::create_dir_all(&dir).unwrap();
        let list = dir.join("selection.txt");
        std::fs::write(&list, "b.png\r\nc.tif\r\n").unwrap();
        let paths = gather_paths(vec![PathBuf::from("a.jpg")], Some(&list)).unwrap();
        assert_eq!(
            paths,
            vec![
                PathBuf::from("a.jpg"),
                PathBuf::from("b.png"),
                PathBuf::from("c.tif")
            ]
        );
        let _ = std::fs::remove_dir_all(&dir);
    }

    #[test]
    fn gather_paths_reports_an_unreadable_list() {
        let missing = std::env::temp_dir().join("cie-list-that-does-not-exist.txt");
        let err = gather_paths(vec![], Some(&missing)).unwrap_err();
        assert!(err.starts_with("could not read the input list: "), "{err}");
    }
}
