# dispoSS

A tiny native Windows screenshot utility for quick, throwaway captures.

Press **Ctrl+Alt+S**, wait for the countdown, drag a region, and the image is copied to the clipboard. A small preview lets you save it or discard it. Nothing is written to disk unless you explicitly click **Save**.

Built with **C# / WPF on .NET Framework 4.8** - no Electron, no browser, and no background cloud service.

## Download

Download the latest Windows installer from GitHub Releases:

- [Latest release](https://github.com/farhansadik007/dispoSS/releases/latest)
- [Direct installer download](https://github.com/farhansadik007/dispoSS/releases/latest/download/dispoSS-Setup.exe)

The release page includes the SHA-256 checksum. The installer is about 3.88 MB, installs per user, and does not require administrator rights.

The latest GitHub release is **v0.1.0**; the Store build is **v1.0.0** and is in submission. The GitHub installer is unsigned, so Windows SmartScreen may show an "Unknown publisher" warning on first launch.

## Microsoft Store

dispoSS is being submitted to the Microsoft Store as an x64 MSIX. Store installs are signed by Microsoft and avoid the unsigned-publisher SmartScreen warning. The Store listing is not live yet; this section will get the link after certification. The GitHub installer remains available.

## Features

- Visible main window with **Capture region**, **Capture full screen**, and **Settings** buttons
- Global hotkey configurable as **Ctrl + one non-modifier key**; default is **Ctrl+Alt+S**
- Countdown timer before the screen is frozen (default 3 seconds)
- Region capture on the monitor where you start the capture
- Full-screen capture across the entire virtual desktop
- Clipboard-first output
- Preview toast with **Copy**, **Save**, and **Discard**
- Optional preview timeout that pauses while hovered
- Optional clipboard auto-clear that only clears the image if it is still ours
- System tray menu with capture, delay, settings, and exit
- Optional "start with Windows" entry
- Settings persisted per user
- About page with photo, bio, social links, and a SupportKori link
- No screenshot history, no temp screenshot files, no automatic uploads

## Requirements

- Windows 10 (1903+) or Windows 11
- .NET Framework **4.8** or newer (included with supported Windows 10 and Windows 11 versions)
- For development: .NET SDK **10.0.402** or newer

## Install from the release

Run `dispoSS-Setup.exe` from the [latest release](https://github.com/farhansadik007/dispoSS/releases/latest).

It installs per user, so no administrator rights are required. The default location is:

```text
%LOCALAPPDATA%\Programs\dispoSS
```

The installer adds Start Menu and desktop shortcuts. If the app is already running, setup closes it and continues. After installation the app opens its main window and also lives in the system tray. To remove it, use **Settings > Apps > Installed apps**, or run the uninstaller in the install folder.

## Verify the download

Each release includes `SHA256SUMS.txt`. Compare it with the downloaded installer:

```powershell
Get-FileHash .\dispoSS-Setup.exe -Algorithm SHA256
```

A checksum confirms the file matches the published release, but it is not a substitute for code signing or SmartScreen reputation.

## Usage

1. Start the app. It opens a small main window and also lives in the system tray.
2. Click **Capture region** or **Capture full screen**, press the configured hotkey, or use the tray menu.
3. Wait for the countdown.
4. Drag to select a region, or let the full-screen capture finish.
5. Use the preview toast to copy, save, or discard.
6. Press **Esc** or right-click to cancel the selection.
7. Press the global hotkey again during the countdown or selection to cancel.

Saving a capture uses this filename by default:

```text
dispoSS_YYYY-MM-DD-HH-mm-ss.png
```

Closing the main window leaves the app running in the system tray. Double-click the tray icon, choose **Open dispoSS** from its menu, or launch the desktop/Start Menu shortcut again to bring the window back. Launching the shortcut while the app is running activates the existing window instead of starting a duplicate.

The title bar contains minimize, settings, and close buttons. The footer contains **SETTINGS**, **HIDE TO TRAY**, and **EXIT**. The About panel opens over the capture view and includes the supplied photo, bio, social links, and a support link.

## Settings

Open the main window and click **Settings** at the bottom of the window, or use **Settings** in the tray menu.

Settings slides in over the capture view inside the same window. **Save** applies the changes; **Cancel** or **Esc** returns without saving.

The in-window settings panel controls:

- Countdown delay
- Automatic clipboard copy
- Preview visibility and timeout
- Clipboard auto-clear interval
- Start with Windows
- Capture hotkey: **Ctrl + one non-modifier key**

The default hotkey is `Ctrl+Alt+S`. Choose **Capture hotkey** in Settings and press a new `Ctrl + key` combination. The selection is saved and re-registered immediately.

Settings are stored in:

```text
%APPDATA%\dispoSS\settings.json
```

Dev mode uses a separate file:

```text
%APPDATA%\dispoSS-dev\settings.json
```

Logs are written to:

```text
%LOCALAPPDATA%\dispoSS\app.log
```

## Commands

From the repository root:

```powershell
.\build.cmd      # Debug build
.\dev.cmd        # dotnet watch with hot reload and dev settings
.\publish.cmd    # Framework-dependent release build in dist\win-x64
.\installer.cmd  # Build the Windows installer in dist
.\package.cmd    # Build the x64 Microsoft Store MSIX in dist\store
```

`dotnet watch` hot-reloads C# changes. XAML changes trigger a rebuild and app restart rather than a live visual refresh.

The published executable is:

```text
dist\win-x64\dispoSS.exe
```

## Build the installer

`installer.cmd` rebuilds the setup program. It requires Inno Setup 6:

```powershell
winget install JRSoftware.InnoSetup
.\installer.cmd
```

The installer output is:

```text
dist\dispoSS-Setup.exe
```

Build and verify the installer on a clean Windows 10/11 machine before publishing a release. Test as a standard user, with SmartScreen enabled, and without developer tools installed.

## Build the Microsoft Store package

`package.cmd` publishes the x64 release build, stages the MSIX payload, and writes:

```text
dist\store\dispoSS_1.0.0.0_x64.msix
```

The MSIX is built with `MakeAppx` from NuGet `Microsoft.Windows.SDK.BuildTools`; Visual Studio and a Windows Application Packaging Project are not required. The final package is intentionally unsigned for Store submission because Microsoft re-signs Store packages.

Local packaged QA uses the test identity `PaperCraneLabs.DispoSSTest`; the reserved Store identity cannot be activated locally. See [`store/SUBMISSION.md`](store/SUBMISSION.md) and [`store/test-package.ps1`](store/test-package.ps1) for the test-signing workflow, listing copy, and certification notes.

## Testing without a mouse

The app accepts two command-line arguments for smoke testing:

```powershell
.\src\DisposableScreenshots\bin\Debug\net48\dispoSS.exe --capture
.\src\DisposableScreenshots\bin\Debug\net48\dispoSS.exe --capture-full
```

These still use the configured countdown. They are intended for development and automation, not as a general CLI interface.

`--tray` starts the app without showing the main window. The Windows startup entry uses this flag.

## How it works

The capture flow is deliberately simple:

1. The countdown runs before touching the screen, so menus and hover states can settle.
2. A GDI snapshot of the virtual desktop is taken once.
3. An opaque, topmost overlay is created per monitor using that frozen snapshot. The snapshot is a separate layer from the selection canvas, so moving the mouse does not repaint the full screenshot.
4. The selection rectangle is mapped from overlay DIPs back to physical pixels.
5. The frozen image is cropped and optionally copied to the clipboard.
6. The preview window holds the image in memory until it closes or you save it.

Important implementation files:

| File | Responsibility |
| --- | --- |
| `src/DisposableScreenshots/App.xaml.cs` | Startup, tray, hotkey, single instance, settings application |
| `Views/MainWindow.xaml.cs` | Capture view, in-window settings transition, window actions |
| `Controls/SettingsPanel.xaml.cs` | In-window settings panel and draft settings |
| `Themes/SunsetTheme.xaml` | Sunset palette, pixel controls, rounded frame, preview/countdown, and tray menu styles |
| `Services/CaptureCoordinator.cs` | Countdown, snapshot, overlays, preview orchestration |
| `Services/ScreenCaptureService.cs` | GDI desktop snapshot and pixel cropping |
| `Services/ScreenService.cs` | Monitor enumeration and DPI-aware bounds |
| `Services/HotkeyService.cs` | `RegisterHotKey` + hidden `HwndSource` |
| `Services/ClipboardService.cs` | Clipboard image data and conditional auto-clear |
| `Services/TrayService.cs` | Tray icon and menu |
| `Views/SelectionCanvas.cs` | Dark mask, selection rectangle, size badge |
| `Views/CaptureOverlayWindow.xaml.cs` | Per-monitor overlay positioning and DPI mapping |
| `Views/PreviewWindow.xaml.cs` | Copy/save/discard toast |

## Design notes

- The desktop is frozen **after** the countdown. The countdown itself is not part of the screenshot.
- Region selection is intentionally limited to one monitor at a time in this version. Full-screen capture spans all monitors.
- The app is single-instance. Launching a second copy activates the existing window and exits.
- The app is per-monitor DPI aware (`PerMonitorV2`).
- The clipboard auto-clear timer only clears the clipboard if the custom marker data is still present. If you copied something else in the meantime, it leaves your clipboard alone.
- The app targets .NET Framework 4.8 and uses the WPF that ships with supported Windows versions, keeping the distribution small without a bundled runtime. The main window, in-window settings panel, preview toast, countdown card, and tray menu use the custom sunset retro theme; the selection overlay stays custom-drawn and positioned for tight control over capture behavior.
- Body text uses the bundled Pixel Operator 8 font. Display headings use the bundled 04b_30 font, rendered aliased for crisp pixel edges.

## Privacy

dispoSS does not contain networking code. Captures remain in memory and, if you choose, on the clipboard or in a file you explicitly save. Settings and a small diagnostic log are written locally; the log does not contain screenshot pixels or clipboard contents.

Read the full [privacy policy](PRIVACY.md).

## Distribution

GitHub Releases is the primary direct-download channel. The Microsoft Store MSIX is in submission; the Store link will be added here after approval. A **winget** package is planned after the first stable release. The app itself does not check for updates; use the release page or the Store listing to install a newer version.

## Known limitations

- The GitHub installer is unsigned. Windows SmartScreen may warn when it is downloaded or launched on another machine. The Store MSIX is re-signed by Microsoft.
- Region selection does not span multiple monitors.
- No annotation, scrolling capture, OCR, or screenshot history.
- No automatic updater.
- Real mouse/keyboard interaction must be tested on an unlocked desktop. The app cannot receive local input while Windows is showing the lock screen.
- `dotnet watch` restarts on XAML changes instead of doing true live XAML reload.

## Credits

- **Pixel Operator 8** by Jayvee Enaguas (HarvettFonts), used for body and control text (CC0 1.0, public domain).
- **04b_30** by 04 / Yuji Oshimoto, used for display headings ([DaFont](https://www.dafont.com/04b-30.font)).
- Social icons derived from [Simple Icons](https://simpleicons.org/) (CC0 1.0); brand logos remain trademarks of their owners.

See [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) for details.

## License

MIT License. See [LICENSE](LICENSE). Third-party components and assets are covered by their own terms.

## Support

If dispoSS is useful to you, you can support the project here:

https://www.supportkori.com/farhansadik007
