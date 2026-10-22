# Changelog

All notable changes to dispoSS are recorded here.

## [0.1.0] - 2026-10-22

### Added

- Native Windows screenshot utility for Windows 10 (1903+) and Windows 11.
- Clipboard-first capture flow with region and full-screen modes.
- Configurable countdown before the desktop is frozen.
- Preview toast with **Copy**, **Save**, and **Discard**.
- Configurable preview timeout that pauses while the preview is hovered.
- Optional clipboard auto-clear that only clears the app's own clipboard entry.
- System tray menu, single-instance activation, and close-to-tray behavior.
- Configurable global hotkey using `Ctrl` plus one non-modifier key. Default:
  `Ctrl+Alt+S`.
- Retro sunset theme with bundled Pixel Operator 8 and 04b_30 fonts.
- About page with photo, bio, social links, and SupportKori link.
- Saved captures default to `dispoSS_YYYY-MM-DD-HH-mm-ss.png`.
- Per-user installer with no administrator requirement.
- Start with Windows option.

### Packaging

- Installer output: `dist\dispoSS-Setup.exe`.
- Install size is small and framework-dependent on .NET Framework 4.8, which is
  included with supported Windows versions.
- Current installer is unsigned. Windows SmartScreen may warn until the
  installer is code-signed.

### Known limitations

- Region selection is limited to the monitor where the selection starts.
- No annotation, scrolling capture, OCR, or screenshot history.
- No automatic updater.
