// SPDX-License-Identifier: GPL-3.0-only

//! explorer-convert-images-verb — the link between File Explorer's context
//! menu and the graphical application.
//!
//! Usage:
//!   explorer-convert-images-verb.exe -Embedding     started by COM: serve one command
//!   explorer-convert-images-verb.exe --register     add the menu entries (current user)
//!   explorer-convert-images-verb.exe --unregister   remove them
//!   explorer-convert-images-verb.exe PATH...        open the application on these paths
//!
//! The program has no window and no console; failures are appended to
//! `explorer-convert-images-verb.log` in the user's temporary folder.

#![cfg_attr(windows, windows_subsystem = "windows")]

#[cfg(windows)]
fn run() -> i32 {
    use explorer_convert_images_verb::{com, labels, launch, log, registry};
    use windows::Win32::Globalization::GetUserDefaultUILanguage;

    let args: Vec<String> = std::env::args().skip(1).collect();
    let first = args.first().map(|a| a.to_ascii_lowercase());
    let outcome: Result<(), String> = match first.as_deref() {
        Some("-embedding") | Some("/embedding") => com::serve().map_err(|e| e.to_string()),
        Some("--register") => {
            // SAFETY: plain call without arguments.
            let language = unsafe { GetUserDefaultUILanguage() };
            registry::register(labels::menu_label(language)).map_err(|e| e.to_string())
        }
        Some("--unregister") => registry::unregister().map_err(|e| e.to_string()),
        Some(_) => launch::launch_app(&args)
            .map(|_| ())
            .map_err(|e| e.to_string()),
        None => Err("no argument (expected -Embedding, --register, --unregister or paths)".into()),
    };
    match outcome {
        Ok(()) => 0,
        Err(message) => {
            log(&format!("{}: {message}", first.unwrap_or_default()));
            1
        }
    }
}

#[cfg(not(windows))]
fn run() -> i32 {
    eprintln!("explorer-convert-images-verb is a Windows program; build it for a Windows target.");
    1
}

fn main() {
    std::process::exit(run());
}
