// SPDX-License-Identifier: GPL-3.0-only

//! Checks the whole chain on a real Windows, the way File Explorer uses it:
//! menu handler → application window.
//!
//!   selftest.exe
//!       On a machine where explorer-convert-images is installed: invokes the
//!       installed menu handler on a sample folder, exactly as a right-click →
//!       "Convert images…" would, and checks that the window opens and stays
//!       up. The window is then closed.
//!
//!   selftest.exe --dist DIR
//!       For the continuous integration, on the freshly built folder DIR
//!       (needs administrator rights, which a CI machine has): first starts
//!       the window directly, then goes through DIR's menu handler, registered
//!       for the duration of the test.
//!
//! Exit code 0 when every check passed; otherwise the failed step is printed.
//!
//! Why administrator rights with --dist: a program run as administrator does
//! not see the handlers registered for the current user only (a Windows
//! security rule), and a CI job runs as administrator. The handler is
//! therefore registered machine-wide for the test, then removed. File
//! Explorer itself never runs as administrator and uses the per-user
//! registration made by the installer.

#[cfg(windows)]
mod windows_only {
    use std::path::{Path, PathBuf};
    use std::process::Command;
    use std::time::{Duration, Instant};

    use explorer_convert_images_verb::com::CLSID;
    use explorer_convert_images_verb::plan;
    use windows::core::{Interface, HSTRING};
    use windows::Win32::System::Com::{
        CoCreateInstance, CoInitializeEx, CoUninitialize, CLSCTX_LOCAL_SERVER,
        COINIT_APARTMENTTHREADED,
    };
    use windows::Win32::UI::Shell::Common::ITEMIDLIST;
    use windows::Win32::UI::Shell::{
        IExecuteCommand, ILCreateFromPathW, ILFree, IObjectWithSelection, IsUserAnAdmin,
        SHCreateShellItemArrayFromIDLists,
    };

    /// How long the window must stay up to be considered started.
    const STAY_UP: Duration = Duration::from_secs(12);

    /// How long the menu handler has to start the application.
    const START_TIMEOUT: Duration = Duration::from_secs(40);

    /// A one-pixel, 24-bit BMP image.
    const PIXEL_BMP: [u8; 58] = [
        66, 77, 58, 0, 0, 0, 0, 0, 0, 0, 54, 0, 0, 0, 40, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 0,
        24, 0, 0, 0, 0, 0, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 255, 255,
        255, 0,
    ];

    fn machine_class_key() -> String {
        format!(r"HKLM\Software\Classes\CLSID\{}", plan::clsid_braced())
    }

    /// Runs a command-line tool and returns what it printed.
    fn output_of(program: &str, args: &[&str]) -> Result<String, String> {
        let output = Command::new(program)
            .args(args)
            .output()
            .map_err(|e| format!("could not run {program}: {e}"))?;
        Ok(String::from_utf8_lossy(&output.stdout).into_owned()
            + &String::from_utf8_lossy(&output.stderr))
    }

    fn app_is_running() -> bool {
        let filter = format!("IMAGENAME eq {}", plan::APP_EXE);
        output_of("tasklist", &["/FI", &filter, "/NH"])
            .map(|out| out.to_lowercase().contains(&plan::APP_EXE.to_lowercase()))
            .unwrap_or(false)
    }

    fn close_app() {
        let _ = output_of("taskkill", &["/F", "/IM", plan::APP_EXE]);
    }

    /// The application appends the errors nobody handled to this file.
    fn crash_log() -> PathBuf {
        std::env::temp_dir().join("explorer-convert-images-app.log")
    }

    fn check_no_crash_logged() -> Result<(), String> {
        match std::fs::read_to_string(crash_log()) {
            Ok(content) if !content.trim().is_empty() => Err(format!(
                "the application logged an unhandled error:\n{content}"
            )),
            _ => Ok(()),
        }
    }

    /// Waits for the window to appear (within `START_TIMEOUT`), then checks
    /// that it is still there after `STAY_UP` and that nothing was logged.
    fn expect_window_up(started_by: &str) -> Result<(), String> {
        let start = Instant::now();
        while !app_is_running() {
            if start.elapsed() > START_TIMEOUT {
                return Err(format!(
                    "{started_by}: {} did not start within {} s (see explorer-convert-images-verb.log \
                     in the temporary folder)",
                    plan::APP_EXE,
                    START_TIMEOUT.as_secs()
                ));
            }
            std::thread::sleep(Duration::from_millis(500));
        }
        std::thread::sleep(STAY_UP);
        if !app_is_running() {
            let logged = check_no_crash_logged().err().unwrap_or_default();
            return Err(format!(
                "{started_by}: the window opened, then stopped by itself. {logged}"
            ));
        }
        check_no_crash_logged()
    }

    /// What File Explorer does on "Convert images…": obtains the handler from
    /// COM, gives it the selection, executes the command.
    fn invoke_menu_handler(folder: &Path) -> Result<(), String> {
        // SAFETY: straightforward COM client sequence; the item identifier
        // list is freed after use.
        unsafe {
            CoInitializeEx(None, COINIT_APARTMENTTHREADED)
                .ok()
                .map_err(|e| format!("COM initialization: {e}"))?;
            let result = (|| {
                let id = ILCreateFromPathW(&HSTRING::from(folder.as_os_str()));
                if id.is_null() {
                    return Err(format!("the shell does not know {}", folder.display()));
                }
                let ids = [id as *const ITEMIDLIST];
                let selection = SHCreateShellItemArrayFromIDLists(&ids);
                ILFree(Some(id));
                let selection = selection.map_err(|e| format!("building the selection: {e}"))?;

                let command: IExecuteCommand = CoCreateInstance(&CLSID, None, CLSCTX_LOCAL_SERVER)
                    .map_err(|e| {
                        format!("obtaining the menu handler from COM (is it registered?): {e}")
                    })?;
                command
                    .cast::<IObjectWithSelection>()
                    .map_err(|e| format!("the handler does not accept a selection: {e}"))?
                    .SetSelection(&selection)
                    .map_err(|e| format!("passing the selection: {e}"))?;
                command
                    .Execute()
                    .map_err(|e| format!("executing the command: {e}"))
            })();
            CoUninitialize();
            result
        }
    }

    pub fn run() -> Result<(), String> {
        let args: Vec<String> = std::env::args().skip(1).collect();
        let dist = match args.as_slice() {
            [] => None,
            // An absolute path in its usual form (C:\…), fit for the registry.
            [flag, dir] if flag == "--dist" => {
                Some(std::path::absolute(dir).map_err(|e| format!("{dir}: {e}"))?)
            }
            _ => return Err("usage: selftest [--dist DIR]".to_string()),
        };

        if app_is_running() {
            return Err(format!(
                "{} is already running: close it first",
                plan::APP_EXE
            ));
        }
        let _ = std::fs::remove_file(crash_log());

        // Something to convert: a folder with one tiny image.
        let folder = std::env::temp_dir().join(format!("eci-selftest-{}", std::process::id()));
        std::fs::create_dir_all(&folder).map_err(|e| format!("{}: {e}", folder.display()))?;
        std::fs::write(folder.join("pixel.bmp"), PIXEL_BMP).map_err(|e| e.to_string())?;

        let outcome = (|| {
            if let Some(dist) = &dist {
                // 1. The window, started directly.
                let app = dist.join(plan::APP_EXE);
                Command::new(&app)
                    .arg(&folder)
                    .spawn()
                    .map_err(|e| format!("could not start {}: {e}", app.display()))?;
                let direct = expect_window_up("started directly");
                close_app();
                direct?;
                println!("ok: the window starts and stays up");

                // 2. The handler of DIR, registered machine-wide for the test.
                // SAFETY: plain call without arguments.
                if !unsafe { IsUserAnAdmin() }.as_bool() {
                    return Err(
                        "--dist needs administrator rights (see the top of selftest.rs)".into(),
                    );
                }
                let server = format!("\"{}\"", dist.join(plan::VERB_EXE).display());
                let key = format!(r"{}\LocalServer32", machine_class_key());
                let added = output_of("reg", &["add", &key, "/ve", "/d", &server, "/f"])?;
                println!("registered for the test: {}", added.trim());
            }

            // 3. The whole chain: menu handler → application window.
            let through_handler = invoke_menu_handler(&folder)
                .and_then(|()| expect_window_up("through the menu handler"));
            close_app();
            through_handler?;
            println!("ok: the menu handler opens the window on the selection");
            Ok(())
        })();

        if dist.is_some() {
            let _ = output_of("reg", &["delete", &machine_class_key(), "/f"]);
        }
        let _ = std::fs::remove_dir_all(&folder);
        outcome
    }
}

#[cfg(windows)]
fn main() {
    match windows_only::run() {
        Ok(()) => println!("All checks passed."),
        Err(message) => {
            eprintln!("FAILED: {message}");
            std::process::exit(1);
        }
    }
}

#[cfg(not(windows))]
fn main() {
    eprintln!("selftest is a Windows program; build it for a Windows target.");
}
