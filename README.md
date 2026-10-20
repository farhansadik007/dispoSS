# Disposable Screenshots

A tiny native Windows screenshot utility for quick, throwaway captures.

Press **Ctrl+Alt+S**, wait for the countdown, drag a region, and the image is copied to the clipboard. A small preview lets you save it or discard it. Nothing is written to disk unless you explicitly click **Save**.

Built with **C# / WPF on .NET 10** — no Electron, no browser, no background cloud service.

## Features

- Global hotkey: **Ctrl+Alt+S**
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
- No history, no temp screenshot files, no automatic uploads

## Requirements

- Windows 10 (1903+) or Windows 11
- For development: .NET SDK **10.0.402** or newer
- Published builds are self-contained and do not need .NET installed

## Commands

From the repository root:

```powershell
.\build.cmd      # Debug build
.\dev.cmd        # dotnet watch with hot reload and dev settings
.\publish.cmd    # Self-contained single-file build in dist\win-x64
.\installer.cmd  # Build the Windows installer in dist
```

`dotnet watch` hot-reloads C# changes. XAML changes trigger a rebuild and app restart rather than a live visual refresh.

The published executable is:

```text
dist\win-x64\DisposableScreenshots.exe
```

## Install

Run the setup program:

```text
dist\DisposableScreenshots-Setup.exe
```

It installs per user, so no administrator rights are required. The default location is:

```text
%LOCALAPPDATA%\Programs\Disposable Screenshots
```

The installer adds a Start Menu shortcut and can optionally add a desktop shortcut. After installation the app starts from the Start Menu and lives in the system tray. To remove it, use **Settings > Apps > Installed apps**, or run the uninstaller in the install folder.

`installer.cmd` rebuilds the setup program. It requires Inno Setup 6:

```powershell
winget install JRSoftware.InnoSetup
.\installer.cmd
```

The installer is unsigned, so Windows SmartScreen may show a warning on first launch. Choose **More info > Run anyway** if you built it yourself. A code-signing certificate would remove that warning for distribution.

## Usage

1. Start the app. It lives in the system tray.
2. Press **Ctrl+Alt+S** or use the tray menu.
3. Wait for the countdown.
4. Drag to select a region, or choose **Capture full screen** from the tray.
5. Use the preview toast to copy, save, or discard.
6. Press **Esc** or right-click to cancel the selection.
7. Press the global hotkey again during the countdown or selection to cancel.

Double-clicking the tray icon starts a region capture.

### Testing without a mouse

The app accepts two command-line arguments for smoke testing:

```powershell
.\src\DisposableScreenshots\bin\Debug\net10.0-windows\DisposableScreenshots.exe --capture
.\src\DisposableScreenshots\bin\Debug\net10.0-windows\DisposableScreenshots.exe --capture-full
```

These still use the configured countdown. They are intended for development and automation, not as a general CLI interface.

## Settings

Settings are stored in:

```text
%APPDATA%\DisposableScreenshots\settings.json
```

Dev mode uses a separate file:

```text
%APPDATA%\DisposableScreenshots-dev\settings.json
```

Logs are written to:

```text
%LOCALAPPDATA%\DisposableScreenshots\app.log
```

The settings window controls:

- Countdown delay
- Automatic clipboard copy
- Preview visibility and timeout
- Clipboard auto-clear interval
- Start with Windows

The global hotkey is fixed to `Ctrl+Alt+S` in this build. Rebindable hotkeys are the next obvious addition.

## How it works

The capture flow is deliberately simple:

1. The countdown runs before touching the screen, so menus and hover states can settle.
2. A GDI snapshot of the virtual desktop is taken once.
3. A transparent, topmost overlay is created per monitor using that frozen snapshot.
4. The selection rectangle is mapped from overlay DIPs back to physical pixels.
5. The frozen image is cropped and optionally copied to the clipboard.
6. The preview window holds the image in memory until it closes or you save it.

Important implementation files:

| File | Responsibility |
| --- | --- |
| `src/DisposableScreenshots/App.xaml.cs` | Startup, tray, hotkey, single instance, settings application |
| `Services/CaptureCoordinator.cs` | Countdown, snapshot, overlays, preview orchestration |
| `Services/ScreenCaptureService.cs` | GDI desktop snapshot and pixel cropping |
| `Services/ScreenService.cs` | Monitor enumeration and DPI-aware bounds |
| `Services/HotkeyService.cs` | `RegisterHotKey` + hidden `HwndSource` |
| `Services/ClipboardService.cs` | Clipboard image data and conditional auto-clear |
| `Services/TrayService.cs` | Tray icon and menu |
| `Views/SelectionCanvas.cs` | Dark mask, selection rectangle, size badge |
| `Views/CaptureOverlayWindow.xaml.cs` | Per-monitor overlay positioning and DPI mapping |
| `Views/PreviewWindow.xaml.cs` | Copy/save/discard toast |
| `Views/SettingsWindow.xaml.cs` | Settings UI |

## Design notes

- The desktop is frozen **after** the countdown. The countdown itself is not part of the screenshot.
- Region selection is intentionally limited to one monitor at a time in this version. Full-screen capture spans all monitors.
- The app is single-instance. Launching a second copy shows a short message and exits.
- The app is per-monitor DPI aware (`PerMonitorV2`).
- The clipboard auto-clear timer only clears the clipboard if the custom marker data is still present. If you copied something else in the meantime, it leaves your clipboard alone.
- WPF-UI supplies the dark theme and control styling. The overlay, countdown, and preview are custom-drawn/positioned for tight control over capture behavior.

## Known limitations / next up

- The global hotkey is not editable from the UI yet.
- Region selection does not span multiple monitors.
- No annotation, scrolling capture, OCR, or history.
- The installer is unsigned. Windows SmartScreen may warn when it is downloaded or launched on another machine.
- Real mouse/keyboard interaction must be tested on an unlocked desktop. The app cannot receive local input while Windows is showing the lock screen.
- `dotnet watch` restarts on XAML changes instead of doing true live XAML reload.

## Privacy

Disposable Screenshots does not contain networking code. Captures remain in memory and, if you choose, on the clipboard or in a file you explicitly save.
