#define MyAppName "Divvun Installer"
#define MyAppPublisher "Universitetet i Tromsø - Norges arktiske universitet"
#define MyAppURL "http://divvun.no"
#define MyAppExeName "DivvunInstaller.exe"
#define PahkatSvcExe "pahkat-service.exe"

[Setup]
AppId={{4CF2F367-82A8-5E60-8334-34619CBA8347}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={pf}\Divvun Installer
DisableProgramGroupPage=yes
OutputBaseFilename=install
Compression=lzma
SolidCompression=yes
AppMutex=DivvunInstaller
SignedUninstaller=yes
SignTool=signtool
MinVersion=6.3.9200                 

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "armenian"; MessagesFile: "compiler:Languages\Armenian.isl"
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"
Name: "catalan"; MessagesFile: "compiler:Languages\Catalan.isl"
Name: "corsican"; MessagesFile: "compiler:Languages\Corsican.isl"
Name: "czech"; MessagesFile: "compiler:Languages\Czech.isl"
Name: "danish"; MessagesFile: "compiler:Languages\Danish.isl"
Name: "dutch"; MessagesFile: "compiler:Languages\Dutch.isl"
Name: "finnish"; MessagesFile: "compiler:Languages\Finnish.isl"
Name: "french"; MessagesFile: "compiler:Languages\French.isl"
Name: "german"; MessagesFile: "compiler:Languages\German.isl"
Name: "hebrew"; MessagesFile: "compiler:Languages\Hebrew.isl"
Name: "icelandic"; MessagesFile: "compiler:Languages\Icelandic.isl"
Name: "italian"; MessagesFile: "compiler:Languages\Italian.isl"
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"
Name: "norwegian"; MessagesFile: "compiler:Languages\Norwegian.isl"
Name: "polish"; MessagesFile: "compiler:Languages\Polish.isl"
Name: "portuguese"; MessagesFile: "compiler:Languages\Portuguese.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "slovenian"; MessagesFile: "compiler:Languages\Slovenian.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"
Name: "ukrainian"; MessagesFile: "compiler:Languages\Ukrainian.isl"

[Types]
Name: "full"; Description: "Install Divvun Installer (Recommended)"
Name: "noui"; Description: "Only install Pahkat Service (Recommended for sysadmins only)"

[Components]
Name: "divvuninst"; Description: "Divvun Installer"; Types: full
Name: "pahkatd"; Description: "Pahkat Service"; Types: full noui; Flags: fixed

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: ".\{#PahkatSvcExe}"; DestDir: "{app}"; Components: pahkatd
Source: ".\Divvun.Installer\bin\x86\Release\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs uninsrestartdelete; Components: divvuninst

[Run]
Filename: "{app}\{#PahkatSvcExe}"; Parameters: "service install"; StatusMsg: "Installing service..."; Flags: runhidden; Components: pahkatd
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall runasoriginaluser

[UninstallRun]
Filename: "{app}\{#PahkatSvcExe}"; Parameters: "service stop"; Flags: runhidden; StatusMsg: "Stopping service..."; Components: pahkatd
Filename: "{app}\{#PahkatSvcExe}"; Parameters: "service uninstall"; Flags: runhidden; StatusMsg: "Uninstalling service..."; Components: pahkatd

[Icons]
Name: "{commonprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Components: divvuninst
Name: "{commonstartup}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Parameters: "-s"; Components: divvuninst
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon; Components: divvuninst

[Code]
function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode: Integer;
begin
    // Stop the service
    ExtractTemporaryFile('{#PahkatSvcExe}');
    Exec(ExpandConstant('{tmp}\{#PahkatSvcExe}'), 'service stop', '', SW_HIDE, ewWaitUntilTerminated, ResultCode)
end;