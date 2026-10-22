# dispoSS Privacy Policy

Effective: 2026-10-22

dispoSS is a local Windows screenshot tool. It is designed to keep captures on
your machine.

## What the app does

- Captures the screen only when you start a capture.
- Keeps the captured image in memory.
- Copies the image to the Windows clipboard when **Copy to clipboard** is on.
- Writes an image file only when you explicitly choose **Save**.
- Keeps no screenshot history, temp captures, accounts, or cloud copies.

## Network access

The app contains no telemetry, analytics, advertising, upload, or update-checking
code. It does not send captures or usage data anywhere.

The About page contains links to SupportKori and social profiles. Choosing one
opens the URL in your default browser. The app does not send any capture or
usage data with that request; the destination website has its own privacy policy.

## What is stored on disk

Settings are stored per Windows user:

```text
%APPDATA%\dispoSS\settings.json
```

The app writes a small diagnostic log:

```text
%LOCALAPPDATA%\dispoSS\app.log
```

The log can include app lifecycle messages, capture dimensions, settings paths,
and errors. It does not contain screenshot pixels or clipboard contents.

If **Start with Windows** is enabled, Windows keeps the startup entry for the
current user. Uninstall or turn off that setting to remove it.

## Clipboard behavior

When **Clipboard auto-clear** is enabled, the app adds a private marker to the
clipboard entry it created. It clears the clipboard later only if that marker is
still present. If you copied something else in the meantime, the app leaves your
clipboard alone.

## Deleting your data

Uninstall dispoSS from **Settings > Apps > Installed apps**. Your settings and
log may remain after uninstall; remove these folders manually to delete them:

```text
%APPDATA%\dispoSS
%LOCALAPPDATA%\dispoSS
```

Saved screenshots are ordinary files. Delete them wherever you saved them.

## Contact

Support and contact: https://www.supportkori.com/farhansadik007
