# Microsoft Store submission kit

This folder contains the MSIX packaging files and the Store listing assets for **dispoSS**.
The GitHub/Inno installer remains the primary direct-download channel.

## Package facts

| Field | Value |
| --- | --- |
| Store product name | `dispoSS` |
| Package identity name | `PaperCraneLabs.DispoSS` |
| Publisher | `CN=E029A42B-BCDE-43A9-9E99-03026508BB24` |
| Publisher display name | `PaperCrane Labs` |
| Package version | `1.0.0.0` |
| App version | `1.0.0` |
| Architecture | `x64` |
| Minimum OS | Windows 10 version 1903 (10.0.18362.0) |
| Maximum tested | Windows 11 24H2 (10.0.26100.0) |
| Capability | `runFullTrust` |
| Startup task | `dispoSSStartup`, `uap10:Parameters="--tray"` |

Final package:

```text
dist\store\dispoSS_1.0.0.0_x64.msix
```

Current size and hash (recompute after any rebuild):

```text
2,805,653 bytes
SHA-256 1805B0E614CAD05DC1BB5AA70C9CECF76A034AF1422CB247A5852A91E3724AB4
```

The final package is intentionally **unsigned**. Microsoft re-signs Store submissions.
Do not upload the test-signed package from `store/test-package.ps1`.

The package includes unplated `Square44x44Logo.targetsize-*_altform-unplated.png` and
`..._altform-lightunplated.png` assets plus `resources.pri`. MakePri must index the package
so Windows uses the unplated assets; without the PRI, Windows ignores them and shrinks the
logo onto a square `BackgroundColor` plate. The square assets are generated from the
full-bleed `store/listing/StoreListingLogo-fullbleed.png` artwork with a 15% rounded-corner
mask, so the taskbar icon fills the full slot while keeping rounded edges. This logo treatment
is approved as-is; do not regenerate or alter it.

## Build

```powershell
.\package.cmd
```

This publishes the x64 release build, restores the MSIX build tools, stages the payload,
and writes the final MSIX to `dist\store\dispoSS_1.0.0.0_x64.msix`.

The MSIX tooling comes from NuGet `Microsoft.Windows.SDK.BuildTools` version
`10.0.26100.9169`. Visual Studio and the Windows Application Packaging Project are not
required.

## Local QA and the reserved identity

The reserved Store identity (`PaperCraneLabs.DispoSS`) cannot be activated locally. Windows
returns `0x80270254` even after a clean install because the name is protected by the Store
reservation. Local QA therefore uses the test identity `PaperCraneLabs.DispoSSTest`.

Do **not** change the identity in `store/Package.appxmanifest`. The test workflow copies the
staging folder and rewrites only the copied manifest.

```powershell
# Build a test-signed package and install it (admin once, to trust the test certificate).
.\store\test-package.ps1 `
    -PfxPath C:\path\to\dispoSS-test.pfx `
    -PfxPassword 'your-test-password' `
    -Install
```

The test certificate must use the same publisher subject:

```text
CN=E029A42B-BCDE-43A9-9E99-03026508BB24
```

A one-time test certificate setup looks like this:

```powershell
$password = ConvertTo-SecureString 'dispoSS-test-password' -Force -AsPlainText
$cert = New-SelfSignedCertificate `
    -Type CodeSigningCert `
    -Subject 'CN=E029A42B-BCDE-43A9-9E99-03026508BB24' `
    -CertStoreLocation 'Cert:\CurrentUser\My' `
    -KeyExportPolicy Exportable
Export-PfxCertificate -Cert $cert -FilePath .\dispoSS-test.pfx -Password $password
Export-Certificate -Cert $cert -FilePath .\dispoSS-test.cer

# Run these two in an elevated PowerShell session.
Import-Certificate -FilePath .\dispoSS-test.cer -CertStoreLocation 'Cert:\LocalMachine\Root'
Import-Certificate -FilePath .\dispoSS-test.cer -CertStoreLocation 'Cert:\LocalMachine\TrustedPeople'
```

`Add-AppxPackage` requires the test certificate in both `LocalMachine\Root` and
`LocalMachine\TrustedPeople`. Trusting it only in the current user store is not enough.

If the same version is already installed with different content, remove it first:

```powershell
Get-AppxPackage -Name PaperCraneLabs.DispoSSTest | Remove-AppxPackage
```

## Partner Center listing

### Product name

```text
dispoSS
```

### Short description

```text
Clipboard-first screenshots for Windows. Press a hotkey, pick a region, and the image is copied. Nothing is saved unless you choose Save.
```

### Description

```text
dispoSS is a tiny, clipboard-first screenshot tool for Windows.

Press the hotkey (Ctrl+Alt+S by default), wait for the countdown, and drag the area you want. The screenshot lands on your clipboard automatically. A small preview lets you copy, save, or discard it.

WHY DISPOSS?

- Clipboard first: nothing is written to disk unless you click Save.
- No history, no accounts, and no network access.
- A discreet countdown and a preview that stays out of the way.
- Full-screen capture across all monitors.
- A configurable Ctrl + key hotkey.
- Optional Start with Windows.
- Per-user install. No administrator rights.

The main window hides while you capture, so it does not get in the shot. The preview closes itself after a few seconds and pauses while you hover. You can turn the preview off entirely in Settings.

dispoSS is built with C# and WPF on .NET Framework 4.8. It is a native Windows app, not a web wrapper.

Privacy: dispoSS does not collect, upload, or store your screenshots. Captures remain in memory until you copy or save them. See the privacy policy for details.
```

### Features

```text
Clipboard-first capture
Region or full-screen capture
Countdown before the screen is frozen
Preview with Copy, Save, and Discard
Configurable Ctrl + key hotkey
Optional Start with Windows
No screenshot history or temp files
No networking, uploads, or accounts
Per-user install without administrator rights
Sunset retro pixel-art interface
```

### What's new in this version

```text
First Microsoft Store release.

Clipboard-first region and full-screen capture, a configurable Ctrl + key hotkey, countdown, preview with Copy / Save / Discard, optional Start with Windows, and the dispoSS retro interface.
```

### Store listing URLs

```text
Privacy policy:
https://github.com/farhansadik007/dispoSS/blob/main/PRIVACY.md

Support:
https://github.com/farhansadik007/dispoSS/issues

Website:
https://github.com/farhansadik007/dispoSS

License terms:
https://github.com/farhansadik007/dispoSS/blob/main/LICENSE
```

### Listing metadata

```text
Category: Utilities & tools
Secondary category: Productivity
Pricing: Free
Markets: All available markets
Language: English (United States)
Copyright: © 2026 PaperCrane Labs
Developed by: PaperCrane Labs
System requirements: Windows 10 version 1903 or later; Windows 11; x64; .NET Framework 4.8
```

### Age rating

Answer the questionnaire as a utility with no objectionable content. Expected result:
**Everyone / 3+** (PEGI 3, ESRB E).

The app contains no violence, sexual content, nudity, profanity, drugs, alcohol, tobacco,
gambling, user-generated content, advertising, in-app purchases, or online interactions.
It does contain links to the developer's public social pages and support page; those pages
are not part of the app's content and are not age-restricted.

### Certification notes

```text
dispoSS is a full-trust Win32/WPF desktop utility packaged as MSIX.

- It has no networking code and does not upload captures.
- Captures stay in memory and are placed on the clipboard. A file is written only when the
  user clicks Save in the preview.
- It does not keep screenshot history or automatic screenshot files.
- It is single-instance. Launching it again activates the existing window.
- The optional Start with Windows setting uses the declared StartupTask
  (TaskId: dispoSSStartup). The task passes --tray so login starts in the tray.
- The default capture hotkey is Ctrl+Alt+S. It is configurable to Ctrl + one non-modifier key.
- There are no test credentials and no sign-in.
- The package is unsigned for Store submission; Microsoft re-signs it.
```

## Store assets

Upload these files from `store/listing/`:

| Asset | File |
| --- | --- |
| 300×300 Store logo | `StoreListingLogo.png` |
| Screenshot 1 | `Screenshot-1-Main.png` |
| Screenshot 2 | `Screenshot-2-Settings.png` |
| Screenshot 3 | `Screenshot-3-Preview.png` |
| Screenshot 4 | `Screenshot-4-About.png` |

All screenshots are 1920×1080 PNG. The raw window captures (`_raw-*.png`) are local-only
and are ignored by git. `compose.ps1` rebuilds the final screenshots when the UI changes.
`StoreListingLogo-fullbleed.png` is the full-bleed source for the MSIX square assets; do not
upload it to the Store listing.

The 300×300 `StoreListingLogo.png` is an exact copy of the app icon asset
(`store/Assets/Square310x310Logo.png`), which is also 300×300. The manifest slot keeps the
`Square310x310Logo.png` file name; Windows scales the 300×300 artwork into that tile slot.
Windows paints the declared `BackgroundColor="#0B1B2B"` behind the transparent corners.

## Submission checklist

1. Partner Center → **Apps and games** → **dispoSS** → **New submission**.
2. **Packages** → upload `dist\store\dispoSS_1.0.0.0_x64.msix`.
3. **Store listing** → English (United States) → paste the name, short description, description, features, and what's new.
4. Upload the 300×300 logo and the four screenshots.
5. **Properties** → category, privacy policy URL, support URL, website, copyright, system requirements.
6. **Age rating** → complete the questionnaire; expect Everyone / 3+.
7. **Pricing and availability** → Free; Windows 10 1903+; x64.
8. **Submission options** → paste the certification notes.
9. Submit a **package flight** first if you want a smaller audience. After the flight is
   verified, submit the full Store listing.
10. After approval, add the Store link to `README.md` while keeping the GitHub/Inno installer.

## Why the Store package fixes SmartScreen

The Store re-signs the MSIX with a Microsoft-trusted certificate. Users who install from the
Store do not see the "Unknown publisher" SmartScreen warning that the unsigned Inno installer
can trigger. Direct downloads from GitHub still use the existing Inno installer.

## Notes

- The Store package targets Windows 10 1903+ because `uap10:Parameters` is supported there.
- The GitHub/Inno installer remains available and still uses the registry Run entry for
  Start with Windows.
- The packaged app uses the WinRT `StartupTask` API and reconciles the task on startup when
  `StartWithWindows` is true.
- `dist\store\stage`, test packages, certificates, and raw listing captures are build/local
  artifacts and are not committed.
