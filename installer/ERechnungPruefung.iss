; Inno-Setup-Skript für "E-Rechnung Prüfung"
; Wird vom GitHub-Workflow .github/workflows/build-setup.yml aufgerufen:
;   ISCC.exe /DAppVersion=1.2.3 /DSourceDir=..\publish /DOutputDir=..\setup-output installer\ERechnungPruefung.iss

#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif
#ifndef SourceDir
  #define SourceDir "..\publish"
#endif
#ifndef OutputDir
  #define OutputDir "..\setup-output"
#endif

#define AppName "E-Rechnung Prüfung"
#define AppExeName "ERechnungPruefung.exe"
#define AppPublisher "mdrollo"
#define AppUrl "https://github.com/mdrollo/einvoice"

[Setup]
; Feste AppId – NICHT ändern, sonst funktionieren Updates/Deinstallation nicht mehr
AppId={{6B8E3F2A-4C1D-4E7B-9A5F-2D3C8E1B7A40}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppUrl}
AppSupportURL={#AppUrl}
AppUpdatesURL={#AppUrl}/releases
DefaultDirName={autopf}\ERechnungPruefung
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
LicenseFile=..\LICENSE
OutputDir={#OutputDir}
OutputBaseFilename=ERechnungPruefung-Setup-{#AppVersion}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
; Installation pro Benutzer oder für alle Benutzer wählbar
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
UninstallDisplayIcon={app}\{#AppExeName}
UninstallDisplayName={#AppName}
VersionInfoVersion={#AppVersion}
VersionInfoProductName={#AppName}
MinVersion=10.0.17763

[Languages]
Name: "german"; MessagesFile: "compiler:Languages\German.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Registry]
; Eintrag für "Öffnen mit" im Explorer (keine Standard-Zuordnung, nur Angebot)
Root: HKA; Subkey: "Software\Classes\Applications\{#AppExeName}\shell\open\command"; ValueType: string; ValueData: """{app}\{#AppExeName}"" ""%1"""; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\Applications\{#AppExeName}\SupportedTypes"; ValueType: string; ValueName: ".xml"; ValueData: ""
Root: HKA; Subkey: "Software\Classes\Applications\{#AppExeName}\SupportedTypes"; ValueType: string; ValueName: ".pdf"; ValueData: ""
Root: HKA; Subkey: "Software\Classes\.xml\OpenWithList\{#AppExeName}"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\.pdf\OpenWithList\{#AppExeName}"; Flags: uninsdeletekey

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
