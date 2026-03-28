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
Filename: "{code:GetVsCodePath}"; Parameters: "--install-extension ""{app}\intercal64-v{#AppVersion}.vsix"""; Flags: runhidden nowait; Check: VsCodeFound; StatusMsg: "Installing VS Code extension..."

[Code]
var
  VsCodePathCache: string;

function FindVsCode(): string;
var
  ResultCode: integer;
  Paths: array of string;
  I: integer;
begin
  Result := '';
  // Check known install locations first (full paths work reliably in installer)
  SetArrayLength(Paths, 4);
  SetArrayLength(Paths, 8);
  Paths[0] := ExpandConstant('{localappdata}') + '\Programs\Microsoft VS Code\bin\code.cmd';
  Paths[1] := ExpandConstant('{pf}') + '\Microsoft VS Code\bin\code.cmd';
  Paths[2] := ExpandConstant('{localappdata}') + '\Programs\Microsoft VS Code\bin\new_code.cmd';
  Paths[3] := ExpandConstant('{pf}') + '\Microsoft VS Code\bin\new_code.cmd';
  // VS Code Insiders
  Paths[4] := ExpandConstant('{localappdata}') + '\Programs\Microsoft VS Code Insiders\bin\code-insiders.cmd';
  Paths[5] := ExpandConstant('{pf}') + '\Microsoft VS Code Insiders\bin\code-insiders.cmd';
  // Scoop / Chocolatey
  Paths[6] := ExpandConstant('{userappdata}') + '\scoop\apps\vscode\current\bin\code.cmd';
  Paths[7] := 'C:\tools\vscode\bin\code.cmd';
  for I := 0 to GetArrayLength(Paths) - 1 do
  begin
    if FileExists(Paths[I]) then
    begin
      Result := Paths[I];
      exit;
    end;
  end;
end;

function VsCodeFound(): boolean;
begin
  if VsCodePathCache = '' then
    VsCodePathCache := FindVsCode();
  Result := VsCodePathCache <> '';
end;

function GetVsCodePath(Param: string): string;
begin
  if VsCodePathCache = '' then
    VsCodePathCache := FindVsCode();
  Result := VsCodePathCache;
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
