# dispoSS - Approved Behavior

This file is the source of truth for behavior the user has already approved.
Read it before changing UI, motion, capture flow, or packaging.

- Do not redesign or rewrite approved areas just because another approach looks cleaner.
- Make the smallest change that satisfies the new request.
- If a request conflicts with something below, call it out and ask before changing it.
- If an approved behavior changes, update this file in the same commit.

Last approved: 2026-10-23

## Product constraints

- Product name is **dispoSS**; the published executable is `dispoSS.exe`.
- Internal namespaces and the `src/DisposableScreenshots` folder keep the old name for stability. Do not treat those as user-facing branding.
- Native Windows app built with C# / WPF on **.NET Framework 4.8**. Do not move to a bundled runtime, Electron, or a web UI.
- Targets **Windows 10 1903+** and **Windows 11**.
- Per-user install, **no administrator rights**.
- Keep the app and installer **under 15 MB** combined. Current installer is about **3.88 MB**; the Store MSIX is about **2.8 MB**.
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
- About opens as a panel that slides over the capture view inside the same window.
- The About panel shows the supplied photo, name, bio, social links under the photo, and a `SUPPORT` link.
- `SAVE` applies settings. `CANCEL` or `Esc` returns without saving.
- Keep the sunset retro visual style: pixel font, navy/coral/gold palette, rounded frame, uppercase labels, hard offset shadows.
- Keep the window title-bar buttons in the top-right: minimize, settings, close.

## Approved typography

- Body and control text use **Pixel Operator 8**.
- The Pixel Operator 8 fonts are embedded at `src/DisposableScreenshots/Assets/Fonts/PixelOperator8-Regular.ttf`
  and `PixelOperator8-Bold.ttf`.
  The pack URI is `pack://application:,,,/Assets/Fonts/#Pixel Operator 8`.
- Secondary text stays at **10 px or larger** so the pixel glyphs remain legible. Small helper captions directly under a control (settings explainers, `FIND ME`) may use **9 px**; do not shrink other secondary text below 10 px.
- Display headings use **04b_30** (font family name `04b 30`):
  - main hero `dispoSS?`, rendered as a small-caps logo: `D` and `SS` at 42 px, `ISPO` at 30 px, coral `?`
  - settings `SETTINGS?`
  - preview `PREVIEW?`
- Display headings render with `TextRenderingMode="Aliased"` for crisp pixel edges.
- The 04b_30 font is embedded at `src/DisposableScreenshots/Assets/Fonts/04B_30__.TTF`.
  The pack URI must keep the space in the family name:
  `pack://application:,,,/Assets/Fonts/#04b 30`.
- Do not replace these fonts or re-enable anti-aliasing on the display headings without approval.
- The window title bar and body copy stay in Pixel Operator 8.

## Approved settings

The settings panel contains:

- Countdown delay
- Automatic clipboard copy
- Preview visibility and timeout
- Clipboard auto-clear interval
- Start with Windows
- Capture hotkey (`Ctrl` + one non-modifier key)

Settings are saved per user at `%APPDATA%\dispoSS\settings.json`.
Dev mode uses `%APPDATA%\dispoSS-dev\settings.json`. Existing settings from the old
`Snipt` or `DisposableScreenshots` folders are read once and migrated on the next save.

## Approved capture flow

1. Start a capture from a button, the tray menu, or the global hotkey.
2. Wait for the configured countdown.
3. Freeze the desktop **after** the countdown.
4. Select a region, or capture the full screen.
5. Copy to the clipboard and optionally show the preview.
6. In the preview, choose `COPY`, `SAVE`, or `DISCARD`.

Behavior details:

- Global hotkey is configurable to **Ctrl + one non-modifier key**; default is **Ctrl+Alt+S**.
- Region capture is limited to the monitor where selection starts.
- Full-screen capture spans the entire virtual desktop.
- `Esc` or right-click cancels selection.
- Pressing the configured hotkey again during the countdown or selection cancels.
- The preview timeout pauses while the preview is hovered.
- Clipboard auto-clear only clears the image if the app's marker is still on the clipboard.
- The app is per-monitor DPI aware (`PerMonitorV2`).

## Approved packaging

- Build with `.\publish.cmd` into `dist\win-x64`.
- Build the installer with `.\installer.cmd`; it requires Inno Setup 6.
- Installer output: `dist\dispoSS-Setup.exe`.
- Default install location: `%LOCALAPPDATA%\Programs\dispoSS`.
- Installer adds Start Menu and desktop shortcuts.
- Setup closes a running copy before installing.
- The installer is unsigned. SmartScreen may warn on another machine; that is accepted for now.
- Startup entry uses the `--tray` flag.

### Microsoft Store package

- Build the Store package with `.\package.cmd` into `dist\store\dispoSS_1.0.0.0_x64.msix`.
- The Store package is an x64, full-trust MSIX built with `MakeAppx` from NuGet
  `Microsoft.Windows.SDK.BuildTools` version `10.0.26100.9169`. Do not add a Visual Studio
  Windows Application Packaging Project.
- Store identity: package name `PaperCraneLabs.DispoSS`, publisher
  `CN=E029A42B-BCDE-43A9-9E99-03026508BB24`, publisher display name `PaperCrane Labs`.
- Keep the package version and app version at `1.0.0.0` / `1.0.0` for this release.
- The final MSIX stays **unsigned** for Store submission; Microsoft re-signs it. Do not upload
  a test-signed package.
- Local packaged QA uses the test identity `PaperCraneLabs.DispoSSTest` because the reserved
  Store identity cannot be activated locally (`0x80270254`). Build/install the test package
  with `store\test-package.ps1`. Do not change the final manifest identity for local QA.
- The MSIX ships `Square44x44Logo.targetsize-*_altform-unplated.png` and
  `Square44x44Logo.targetsize-*_altform-lightunplated.png` variants so the taskbar, Alt+Tab,
  and the lower-right corner of Start use the full logo. Without them Windows shrinks the logo
  and draws it on a square plate using `BackgroundColor`. Keep the base `Square44x44Logo.png`
  for the manifest reference.
- The MSIX square logo treatment is approved as-is: the base square logos and every
  unplated/light-unplated target size come from the full-bleed
  `store\listing\StoreListingLogo-fullbleed.png` artwork with a **15% rounded-corner mask** and
  **8× supersampling**. The package must include `resources.pri` generated by MakePri so
  Windows uses those unplated assets. Do not regenerate, re-crop, or change the corner radius
  without explicit approval.
- The packaged Start with Windows setting uses the declared `uap5:StartupTask`
  (`TaskId=dispoSSStartup`) with `uap10:Parameters="--tray"` and the WinRT `StartupTask` API.
  The unpackaged Inno build keeps using the per-user registry Run entry.
- The Store package targets Windows 10 1903+ and Windows 11, x64, per-user, with no
  administrator rights.
- Store listing assets live under `store\listing`. Submission copy, URLs, age rating, and
  certification notes live in `store\SUBMISSION.md`.
- The Store listing app icon is exactly 300×300, downscaled from
  `store\Assets\Square310x310Logo.png`. The package asset stays 310×310 for its manifest tile
  slot. Do not upload the 310×310 package asset as the listing logo.
- Keep the existing GitHub/Inno installer as the direct-download channel alongside the Store.

## Do not change without explicit approval

- The hover/press split and the exact offsets and timings in the tables above.
- The **dispoSS** name and the sunset retro theme.
- The `Face`-based motion approach and the Pixel Operator 8 / 04b_30 typography split.
- .NET Framework 4.8 and the framework-dependent, per-user packaging.
- Clipboard-first behavior and the no-temp-files / no-history / no-network guarantees.
- Tray behavior, single-instance behavior, and close-to-tray.
- The configurable Ctrl+key hotkey and its default `Ctrl+Alt+S`.
- The settings fields listed above.
- The About content: supplied photo, name, bio, social links under the photo, and the SupportKori link.
- The Store package identity, publisher, x64 full-trust MSIX format, and unsigned-for-submission workflow.
- The MSIX square logo treatment: full-bleed Store artwork, 15% rounded corners, and the
  `resources.pri` unplated assets. Do not regenerate or alter the logo.
- The 300×300 Store listing logo derived from `Square310x310Logo.png`; keep it at 300×300.
- The packaged `StartupTask` + `uap10:Parameters="--tray"` approach and the test-identity local QA workflow.

## Open items (not approved or implemented)

These are possible future features. Do not implement them unless the user asks:

- Region selection across multiple monitors
- Annotation, scrolling capture, OCR, or capture history
- Code signing the installer

## Verification before shipping

1. `dotnet build src\DisposableScreenshots\DisposableScreenshots.csproj -c Debug`
2. Rebuild and reinstall with `.\publish.cmd` and `.\installer.cmd`.
3. Confirm the installed executable matches `dist\win-x64\dispoSS.exe`.
4. Check the motion matrix on the installed build:
   - main capture button: `0 -> 2 -> 4 -> 2`
   - title-bar button: `0 -> 3 -> 0`
   - settings `SAVE`: `0 -> 4 -> 0`
5. Launch the installed app and leave it running.

For a Store package release:

1. Run `.\package.cmd`.
2. Confirm `dist\store\dispoSS_1.0.0.0_x64.msix` has the expected identity, version, x64
   architecture, `runFullTrust`, and `uap10:Parameters="--tray"`.
3. For local packaged QA, build and install the test package with
   `.\store\test-package.ps1 -PfxPath <pfx> -PfxPassword <password> -Install -Force`.
4. Smoke test activation, `--tray`, single-instance, region and full-screen capture,
   clipboard copy, preview `COPY` / `SAVE` / `DISCARD`, save naming, settings, About, and
   startup-task reconciliation under the test identity.
5. Run the Windows App Certification Kit when it is available, then submit the final MSIX.
6. After Store approval, add the Store link to `README.md` while keeping the GitHub/Inno
   installer.
