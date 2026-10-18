# Minit Browser

**A quieter Windows browser prototype.** Native dark/light themes, a custom window frame, translucent edges and configurable sleeping tabs — built with C# / .NET 10 WinForms and Microsoft Edge WebView2.

![Minit Browser preview](preview.png)

## Download v0.3

Get **Minit-v0.3-Windows-x64.zip** from [Releases](https://github.com/mustafakula10/minit-browser/releases). Extract the entire folder and run `MinitBrowser.exe`. A separate .NET installation is not required. [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/) must be installed. The app is unsigned; this is an experimental preview, not a production browser replacement.

Lower RAM usage than Chrome has **not** been benchmarked. The goal is to reduce background-tab load; the displayed working-set sum is approximate and may double-count shared memory.

## Features

- Custom rounded frame, original M logo, window drag, resize, minimize and maximize/restore.
- Translucent outer rim, separate from the opaque website content. Toggle it under **Ayarlar → Görünüm**. This is a tinted transparent edge, not an OS acrylic blur effect.
- Dark/light themes, start-page search and quick links.
- Tab close buttons, middle-click close and **Bellekte tut** (keep in memory).
- Background tabs unload after 30–3600 seconds (default 120). Active, pinned, audio-playing and downloading tabs are protected.
- DuckDuckGo, Google or Bing, default zoom, download-folder selection and session URL restoration.
- Optional local history, bookmark management and a RAM indicator.
- Turkish interface and persistent settings.

Sleeping tabs reload when selected. Unsaved forms, scroll position and per-tab navigation history are not preserved. Keep form/media tabs in memory when needed.

## Build and test

On Windows x64 with the .NET 10 SDK:

```powershell
dotnet restore MinitBrowser.csproj
dotnet build MinitBrowser.csproj -c Release
dotnet run --project MinitBrowser.csproj -c Release
```

Run built-in checks on an interactive Windows desktop with WebView2 installed:

```powershell
dotnet run --project MinitBrowser.csproj -c Release -- --self-test
```

The test uses a separate profile and writes `self-test.txt` plus preview images next to the executable. A normal user profile is not used. `--visual-test` opens a separate temporary profile for manual UI checks.

For a portable distribution, run `./build.ps1`. It creates `publish/`, copies the bundled third-party license notices, and optionally tests the published executable with `./build.ps1 -Test`.

## Validation and limits

The published v0.3 executable passed [these checks](self-test.txt): actual HTML rendering/JavaScript, configured tab expiry and protection, renderer recreation, session-address storage, borderless frame/icon, resize hit zones, rim visibility toggle, maximize/restore, settings UI save and persistence, selected search engine, zoom, disabled sleeping, corrupt-settings recovery and history deduplication. Build completed with zero warnings and errors. Home layouts and all four settings pages were visually inspected.

Actual mouse-drag interaction and desktop-compositor transparency were not independently verified by screenshot automation: the native capture helper timed out. Window geometry and visibility were checked programmatically. Downloads, permission prompts and broad external-site compatibility still need manual end-to-end testing. No comprehensive browser security audit or performance comparison has been performed.

Missing features: extensions, private mode, sync, a cookie-management panel, password-manager UI and automatic Minit updates. WebView2 certificate checks and security defaults are not disabled.

## Local data

Settings, bookmarks, optional history (up to 500 distinct URLs), session addresses and WebView2 data are stored under `%LOCALAPPDATA%\MinitBrowser`. Disabling history does not delete older records; history deletion does not clear cookies or signed-in sessions.

The first normal launch can copy preferences/bookmarks/history/session-address files from the earlier Minti prototype if matching Minit files do not exist. Old files are preserved. WebView2 cookies and signed-in sessions are not migrated.

Minit implements no user account or cloud synchronization. Websites, search engines and WebView2 have their own networking behavior; this is not an offline or telemetry-free claim.

## Status and rights

An AI-assisted learning prototype, version 0.3. Source is published for review; no open-source license has been selected for the Minit application. Third-party components retain their own terms; included license and notice files cover WebView2, .NET and Windows Forms.

## Türkçe

**Minit**, Windows için sekme yönetimine odaklanan bir tarayıcı prototipidir. Klasik Windows başlığı yerine özel pencere düğmeleri, logo ve yarı saydam dış kenarlar kullanır. **Ayarlar** bölümünden tema, arama motoru, uyku süresi, yakınlaştırma, indirme klasörü ve geçmiş kaydı değiştirilebilir. Eklenti/gizli mod/eşitleme yoktur; Chrome ile RAM kıyaslaması yapılmamıştır.
