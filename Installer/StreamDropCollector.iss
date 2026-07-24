; Inno Setup script for Stream Drop Collector.
;
; Build locally:
;   dotnet publish ..\UI\UI.csproj -c Release -r win-x64 -p:SelfContained=true -o ..\publish\self-contained\
;   ISCC.exe StreamDropCollector.iss
;
; CI overrides the version via /DMyAppVersion=x.y.z (see .github/workflows/release.yml).
#ifndef MyAppVersion
  #define MyAppVersion "1.0.4"
#endif

#define MyAppName "Stream Drop Collector"
#define MyAppPublisher "Scuttle"
#define MyAppURL "https://github.com/Scuttle-ZapAccess/StreamDropCollector"
#define MyAppExeName "Stream Drop Collector.exe"
#define MyPublishDir "..\publish\self-contained"

[Setup]
; Fixed GUID - keep stable across versions so upgrades/uninstall detection work correctly.
AppId={{CD0BB7D3-E565-46D6-83B6-49AF20817C33}
AppName={#MyAppName}
; Without this, Inno's default Programs & Features display name is "AppName version AppVersion" -
; the version already shows inside the app itself (bottom of the sidebar), no need to duplicate it
; here too.
AppVerName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
VersionInfoVersion={#MyAppVersion}
; The app defines this same mutex (App.xaml.cs) to detect a second instance of itself. Reusing it
; here makes Setup/Uninstall refuse to proceed while the app is running instead of silently
; failing to replace/delete a locked exe and leaving stale shortcuts/registry entries behind.
AppMutex=Global\StreamDropCollector_Instance
; Per-user install only, always under a folder the user owns. This app (like the existing zip
; releases) relies on WebView2's default behavior of writing its browser profile in a folder
; right next to its own exe - that fails with Access Denied if the exe lives under Program Files,
; which only an elevated/all-users install would do. So this intentionally never offers that
; choice, unlike a typical Inno "install for all users" setup.
DefaultDirName={localappdata}\Programs\Stream Drop Collector
DefaultGroupName=Stream Drop Collector
PrivilegesRequired=lowest
DisableProgramGroupPage=yes
DisableWelcomePage=no
WizardStyle=modern
WizardImageFile=Assets\WizardImage.bmp
WizardSmallImageFile=Assets\WizardSmallImage.bmp
SetupIconFile=..\UI\Assets\logo.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2
SolidCompression=yes
OutputDir=..\publish\installer
OutputBaseFilename=StreamDropCollectorSetup-{#MyAppVersion}
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "{#MyPublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Stream Drop Collector"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall Stream Drop Collector"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Stream Drop Collector"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch Stream Drop Collector"; Flags: nowait postinstall skipifsilent
