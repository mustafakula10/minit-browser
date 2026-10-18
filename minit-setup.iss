; Minit installer source, MIT licensed. Build with Inno Setup 6.7 or later.
#ifndef PayloadDir
  #define PayloadDir "publish"
#endif
#ifndef SetupOutputDir
  #define SetupOutputDir "dist"
#endif

[Setup]
AppId={{80AC89A8-6DAD-4338-95A9-879EA97D5E64}
AppName=Minit
AppVersion=0.3.0
AppPublisher=Mustafa Kula
AppPublisherURL=https://github.com/mustafakula10/minit-browser
AppSupportURL=https://github.com/mustafakula10/minit-browser/issues
AppUpdatesURL=https://github.com/mustafakula10/minit-browser/releases
DefaultDirName={localappdata}\Programs\Minit
DefaultGroupName=Minit
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.19041
DisableProgramGroupPage=yes
DisableWelcomePage=no
WizardStyle=modern
SetupIconFile=Assets\minit.ico
UninstallDisplayIcon={app}\MinitBrowser.exe
OutputDir={#SetupOutputDir}
OutputBaseFilename=Minit-v0.3-Windows-x64-Setup
Compression=lzma2
SolidCompression=yes
CloseApplications=yes
RestartApplications=no
UninstallDisplayName=Minit
VersionInfoDescription=Minit Windows installer
VersionInfoVersion=0.3.0.0

[Languages]
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[CustomMessages]
turkish.DesktopShortcut=Masaüstü kısayolu oluştur
english.DesktopShortcut=Create a desktop shortcut
turkish.StartMinit=Minit'i başlat
english.StartMinit=Launch Minit
turkish.MissingWebView=Microsoft Edge WebView2 Runtime bulunamadı. Önce https://developer.microsoft.com/microsoft-edge/webview2/ adresinden Evergreen Runtime kurup bu kurulumu yeniden çalıştırın.
english.MissingWebView=Microsoft Edge WebView2 Runtime was not found. Install Evergreen Runtime from https://developer.microsoft.com/microsoft-edge/webview2/ and run this setup again.

[Tasks]
Name: "desktopicon"; Description: "{cm:DesktopShortcut}"; Flags: unchecked

[Files]
Source: "{#PayloadDir}\MinitBrowser.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\WEBVIEW2-LICENSE.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\WEBVIEW2-NOTICE.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\DOTNET-LICENSE.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\DOTNET-THIRD-PARTY-NOTICES.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\WINFORMS-LICENSE.txt"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Minit"; Filename: "{app}\MinitBrowser.exe"; WorkingDir: "{app}"
Name: "{group}\Minit Kaldır - Uninstall"; Filename: "{uninstallexe}"
Name: "{userdesktop}\Minit"; Filename: "{app}\MinitBrowser.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\MinitBrowser.exe"; Description: "{cm:StartMinit}"; Flags: nowait postinstall skipifsilent

[Code]
function HasWebView2: Boolean;
var
  Version: String;
  Key: String;
begin
  Key := 'Software\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}';
  Result := (RegQueryStringValue(HKLM32, Key, 'pv', Version) and (Version <> '') and (Version <> '0.0.0.0')) or
            (RegQueryStringValue(HKCU32, Key, 'pv', Version) and (Version <> '') and (Version <> '0.0.0.0'));
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  Result := '';
  if not HasWebView2 then
    Result := CustomMessage('MissingWebView');
end;

// User profile files are intentionally preserved when uninstalling.

