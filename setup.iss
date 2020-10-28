#define MyAppName "Divvun Manager"
#define MyAppPublisher "Universitetet i Tromsø - Norges arktiske universitet"
#define MyAppURL "http://divvun.no"
#define MyAppExeName "DivvunManager.exe"

#define DivvunInstallerUuid "{{4CF2F367-82A8-5E60-8334-34619CBA8347}"
#define PahkatServiceUuid "{{6B3A048B-BB81-4865-86CA-61A0DF038CFE}"

[Setup]
AppId={#DivvunInstallerUuid}
AppName={#MyAppName}
AppVersion={#Version}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={commonpf}\Divvun Manager
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

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "Divvun.Installer\bin\x86\Release\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs uninsrestartdelete
Source: "pahkat-service-setup.exe"; DestDir: "{app}"; Flags: deleteafterinstall dontcopy

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall runasoriginaluser skipifsilent

[Icons]
Name: "{commonprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}";
Name: "{commonstartup}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Parameters: "-s";
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon;

[Code]
function GetUninstallString: String;
var
  sUnInstPath: String;
  sUnInstPathWow64: String;
  sUnInstallString: String;
begin
  sUnInstPath := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#DivvunInstallerUuid}_is1';
  sUnInstPathWow64 := 'Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\{#DivvunInstallerUuid}_is1';
  sUnInstallString := '';
  if not RegQueryStringValue(HKLM, sUnInstPath, 'UninstallString', sUnInstallString) then
    RegQueryStringValue(HKLM, sUnInstPathWow64, 'UninstallString', sUnInstallString);
  Result := sUnInstallString;
end;

function GetPahkatServiceUninstallString: String;
var
  sUnInstPath: String;
  sUnInstPathWow64: String;
  sUnInstallString: String;
begin
  sUnInstPath := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#PahkatServiceUuid}_is1';
  sUnInstPathWow64 := 'Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\{#PahkatServiceUuid}_is1';
  sUnInstallString := '';
  if not RegQueryStringValue(HKLM, sUnInstPath, 'UninstallString', sUnInstallString) then
    RegQueryStringValue(HKLM, sUnInstPathWow64, 'UninstallString', sUnInstallString);
  Result := sUnInstallString;
end;

function UninstallDivvunInstallerV1: String;
var
  sUnInstPath: String;
  sUnInstPathWow64: String;
  majorVersion: Cardinal;    
  iResultCode: Integer;
  sUnInstallString: string;
begin
  sUnInstPath := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#DivvunInstallerUuid}_is1';
  sUnInstPathWow64 := 'Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\{#DivvunInstallerUuid}_is1';
  if RegValueExists(HKEY_LOCAL_MACHINE, sUnInstPath, 'MajorVersion') then
    RegQueryDWordValue(HKEY_LOCAL_MACHINE, sUnInstPath, 'MajorVersion', majorVersion);
  if RegValueExists(HKEY_LOCAL_MACHINE, sUnInstPathWow64, 'MajorVersion') then
    RegQueryDWordValue(HKEY_LOCAL_MACHINE, sUnInstPath, 'MajorVersion', majorVersion);
  if majorVersion = 1 then
    sUnInstallString := GetUninstallString();
    sUnInstallString := RemoveQuotes(sUnInstallString);
    Exec('taskkill', '/F /IM DivvunInstaller.exe', '', SW_HIDE, ewWaitUntilTerminated, iResultCode);
    Sleep(250);
    Exec(ExpandConstant(sUnInstallString), '/VERYSILENT /SP- /SUPPRESSMSGBOXES /NORESTART', '', SW_HIDE, ewWaitUntilTerminated, iResultCode);  
    Sleep(250);
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var                       
  V: Integer;
  uninstString: string;    
  iResultCode: Integer;
begin
  if CurUninstallStep = usUninstall then
  begin
    uninstString := GetPahkatServiceUninstallString();
    if uninstString <> '' then
    begin
      V := MsgBox(ExpandConstant('Pahkat Service was also detected. Do you want to uninstall it? (Recommended)'), mbInformation, MB_YESNO);
      if V = IDYES then                          
        Exec(ExpandConstant(uninstString), '/VERYSILENT /SP- /SUPPRESSMSGBOXES /NORESTART', '', SW_HIDE, ewWaitUntilTerminated, iResultCode);
    end;
  end;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode: Integer;
begin
    // Uninstall Divvun Installer v1 if it exists
    UninstallDivvunInstallerV1();             
    
    // Run embedded Pahkat Service installer
    ExtractTemporaryFile('pahkat-service-setup.exe');
    Exec(ExpandConstant('{tmp}\pahkat-service-setup.exe'), '/VERYSILENT /SP- /SUPPRESSMSGBOXES /NORESTART', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;