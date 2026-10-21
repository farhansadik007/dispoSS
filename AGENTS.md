# Disposable Screenshots - Approved Behavior

This file is the source of truth for behavior the user has already approved.
Read it before changing UI, motion, capture flow, or packaging.

- Do not redesign or rewrite approved areas just because another approach looks cleaner.
- Make the smallest change that satisfies the new request.
- If a request conflicts with something below, call it out and ask before changing it.
- If an approved behavior changes, update this file in the same commit.

Last approved: 2026-10-21

## Product constraints

- Native Windows app built with C# / WPF on **.NET Framework 4.8**. Do not move to a bundled runtime, Electron, or a web UI.
- Targets **Windows 10 1903+** and **Windows 11**.
- Per-user install, **no administrator rights**.
- Keep the app and installer **under 15 MB** combined. Current installer is about **2.85 MB**.
- Clipboard-first. Nothing is written to disk unless the user explicitly clicks **Save**.
- No networking, no uploads, no history, no automatic screenshot files.

## Approved button motion

Motion moves the button face diagonally down-right by applying the same offset to X and Y.

Timing and easing, everywhere:

| State | Duration | Easing |
| --- | --- | --- |
| Hover in | 200 ms | `SineEase`, `EaseOut` |
| Press down | 100 ms | `SineEase`, `EaseOut` |
| Release | 220 ms | `SineEase`, `EaseOut` |

### Main window, footer, and preview buttons

| Buttons | Hover | Press | After release |
| --- | --- | --- | --- |
| Capture region / Capture full screen | 2 px | 4 px | Back to 2 px while hovered |
| Footer `SETTINGS` / `HIDE TO TRAY` / `EXIT` | 2 px | 4 px | Back to 2 px while hovered |
| Preview `COPY` / `SAVE` / `DISCARD` | 2 px | 4 px | Back to 2 px while hovered |

These keep **both hover and press movement**.

### Press-only buttons

| Buttons | Hover | Press | After release |
| --- | --- | --- | --- |
| Title-bar minimize / settings / close (main and preview) | 0 px | 3 px | Back to 0 px |
| Settings screen stepper buttons (`-` / `+`) | 0 px | 3 px | Back to 0 px |
| Settings screen `CANCEL` / `SAVE` | 0 px | 4 px | Back to 0 px |

These have **no hover movement**. Press movement is still required.

Do not add opacity, scale, glow, or color-pulse hover effects to press-only buttons.

### Implementation rules

- Button motion is code-driven in `src/DisposableScreenshots/Controls/ButtonMotion.cs`.
- **Do not restore XAML trigger storyboards for this motion.** WPF freezes the template's `TranslateTransform`, which silently prevents those animations from moving the face.
- `ButtonMotion` requires a template element named `Face`; it replaces that element's transform with a per-button instance at `Loaded`.
- Base press offset is **4 px**. `RetroWindowButton` overrides it to **3 px** and sets `HoverMotion=False`.
- `RetroStepButton` inherits the no-hover / 3 px behavior from `RetroWindowButton`.
- Settings `CANCEL` and `SAVE` set `controls:ButtonMotion.HoverMotion="False"` inline because they use the normal button styles.

## Approved layout and navigation

- The app opens a small main window and also lives in the system tray.
- Closing the main window hides it to the tray; it does not exit the app.
- The app is single-instance. Launching it again activates the existing window instead of starting a duplicate.
- Settings opens as a panel that slides over the capture view inside the same window.
- `SAVE` applies settings. `CANCEL` or `Esc` returns without saving.
- Keep the sunset retro visual style: pixel font, navy/coral/gold palette, rounded frame, uppercase labels, hard offset shadows.
- Keep the window title-bar buttons in the top-right: minimize, settings, close.

## Approved settings

The settings panel contains:

- Countdown delay
- Automatic clipboard copy
- Preview visibility and timeout
- Clipboard auto-clear interval
- Start with Windows

Settings are saved per user at `%APPDATA%\DisposableScreenshots\settings.json`.
Dev mode uses `%APPDATA%\DisposableScreenshots-dev\settings.json`.

## Approved capture flow

1. Start a capture from a button, the tray menu, or the global hotkey.
2. Wait for the configured countdown.
3. Freeze the desktop **after** the countdown.
4. Select a region, or capture the full screen.
5. Copy to the clipboard and optionally show the preview.
6. In the preview, choose `COPY`, `SAVE`, or `DISCARD`.

Behavior details:

- Global hotkey is fixed to **Ctrl+Alt+S** in this build.
- Region capture is limited to the monitor where selection starts.
- Full-screen capture spans the entire virtual desktop.
- `Esc` or right-click cancels selection.
- Pressing the hotkey again during the countdown or selection cancels.
- The preview timeout pauses while the preview is hovered.
- Clipboard auto-clear only clears the image if the app's marker is still on the clipboard.
- The app is per-monitor DPI aware (`PerMonitorV2`).

## Approved packaging

- Build with `.\publish.cmd` into `dist\win-x64`.
- Build the installer with `.\installer.cmd`; it requires Inno Setup 6.
- Installer output: `dist\DisposableScreenshots-Setup.exe`.
- Default install location: `%LOCALAPPDATA%\Programs\Disposable Screenshots`.
- Installer adds Start Menu and desktop shortcuts.
- Setup closes a running copy before installing.
- The installer is unsigned. SmartScreen may warn on another machine; that is accepted for now.
- Startup entry uses the `--tray` flag.

## Do not change without explicit approval

- The hover/press split and the exact offsets and timings in the tables above.
- The sunset retro theme and the `Face`-based motion approach.
- .NET Framework 4.8 and the framework-dependent, per-user packaging.
- Clipboard-first behavior and the no-temp-files / no-history / no-network guarantees.
- Tray behavior, single-instance behavior, and close-to-tray.
- The fixed `Ctrl+Alt+S` hotkey.
- The settings fields listed above.

## Open items (not approved or implemented)

These are possible future features. Do not implement them unless the user asks:

- Rebindable global hotkey
- Region selection across multiple monitors
- Annotation, scrolling capture, OCR, or capture history
- Code signing the installer

## Verification before shipping

1. `dotnet build src\DisposableScreenshots\DisposableScreenshots.csproj -c Debug`
2. Rebuild and reinstall with `.\publish.cmd` and `.\installer.cmd`.
3. Confirm the installed executable matches `dist\win-x64\DisposableScreenshots.exe`.
4. Check the motion matrix on the installed build:
   - main capture button: `0 -> 2 -> 4 -> 2`
   - title-bar button: `0 -> 3 -> 0`
   - settings `SAVE`: `0 -> 4 -> 0`
5. Launch the installed app and leave it running.
