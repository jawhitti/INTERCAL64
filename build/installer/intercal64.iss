[Setup]
AppName=INTERCAL-64
AppVersion={#AppVersion}
AppPublisher=jawhitti
AppPublisherURL=https://github.com/jawhitti/INTERCAL64
DefaultDirName={localappdata}\intercal64
DefaultGroupName=INTERCAL-64
OutputBaseFilename=intercal64-{#AppVersion}-win-x64-setup
Compression=lzma2
SolidCompression=yes
PrivilegesRequired=lowest
ChangesEnvironment=yes
SetupIconFile=compiler:SetupClassicIcon.ico
UninstallDisplayIcon={app}\bin\churn.exe

[Files]
Source: "{#DistDir}\bin\*"; DestDir: "{app}\bin"; Flags: ignoreversion recursesubdirs
Source: "{#DistDir}\lib\*"; DestDir: "{app}\lib"; Flags: ignoreversion recursesubdirs
Source: "{#DistDir}\samples\*"; DestDir: "{app}\samples"; Flags: ignoreversion recursesubdirs
Source: "{#DistDir}\*.vsix"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#DistDir}\*.md"; DestDir: "{app}"; Flags: ignoreversion

[Registry]
Root: HKCU; Subkey: "Environment"; ValueType: expandsz; ValueName: "Path"; ValueData: "{olddata};{app}\bin"; Check: NeedsAddPath('{app}\bin')

[Run]
Filename: "cmd.exe"; Parameters: "/c copy ""{app}\lib\intercal64.runtime.dll"" ""{app}\bin\"" >nul 2>&1"; Flags: runhidden
Filename: "cmd.exe"; Parameters: "/c copy ""{app}\lib\syslib64.dll"" ""{app}\bin\"" >nul 2>&1"; Flags: runhidden
Filename: "code"; Parameters: "--install-extension ""{app}\intercal64-{#AppVersion}.vsix"""; Flags: runhidden nowait; Check: VsCodeExists; StatusMsg: "Installing VS Code extension..."

[Code]
function VsCodeExists(): boolean;
var
  ResultCode: integer;
begin
  Result := Exec('cmd.exe', '/c where code >nul 2>&1', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) and (ResultCode = 0);
end;

function NeedsAddPath(Param: string): boolean;
var
  OrigPath: string;
begin
  if not RegQueryStringValue(HKEY_CURRENT_USER, 'Environment', 'Path', OrigPath)
  then begin
    Result := True;
    exit;
  end;
  Result := Pos(';' + Param + ';', ';' + OrigPath + ';') = 0;
end;
