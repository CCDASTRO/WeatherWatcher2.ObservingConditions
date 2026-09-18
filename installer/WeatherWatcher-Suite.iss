#define WW "..\WeatherWatcher2.ObservingConditions\bin\Release"
#define SM "..\SafetyMonitor\bin\Release"
#define Clsid "{7FCECB35-1C6B-4E48-8F5B-8D4F0E5C1101}"
[Setup]
AppId={{185dafc9-a1ef-46fb-93fc-e0e8735d9890}}
AppName=CCDASTRO WeatherWatcher and SafetyMonitor
AppVersion=1.2.1
AppVerName=WeatherWatcher 1.2.1 and SafetyMonitor 1.3.1 (development)
AppPublisher=CCDASTRO
DefaultDirName={commoncf32}\ASCOM\ObservingConditions\WeatherWatcher2
DisableDirPage=yes
DisableProgramGroupPage=yes
PrivilegesRequired=admin
MinVersion=10.0
OutputDir=..\dist
OutputBaseFilename=CCDASTRO.WeatherWatcher-Safety.Setup-1.2.1-development
Compression=lzma2
SolidCompression=yes
CloseApplications=yes
RestartApplications=no
UninstallFilesDir={commoncf32}\ASCOM\Uninstall\ObservingConditions\WeatherWatcher2
[Files]
Source: "{#WW}\ASCOM.WeatherWatcher2.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#WW}\ASCOM.WeatherWatcher2.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SM}\ASCOM.WeatherWatcher.SafetyMonitor.dll"; DestDir: "{commoncf32}\ASCOM\SafetyMonitor"; Flags: ignoreversion
Source: "..\docs\RainCloud\ASCOM-INTEGRATION.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\docs\weatherwatcher-guide.html"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#WW}\PureHDF.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#WW}\System.Buffers.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#WW}\System.Memory.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#WW}\System.Numerics.Vectors.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#WW}\System.Runtime.CompilerServices.Unsafe.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#WW}\System.Threading.Tasks.Extensions.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\docs\RainCloud\NOAA-CLOUD.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\docs\RainCloud\PUSHOVER.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\docs\RainCloud\THIRD-PARTY-NOTICES.txt"; DestDir: "{app}"; Flags: ignoreversion
[Registry]
; The x86 EXE registers the 32-bit view. Mirror local-server activation for 64-bit clients.
Root: HKLM64; Subkey: "SOFTWARE\Classes\WeatherWatcher2.ObservingConditions"; ValueType: string; ValueData: "WeatherWatcher2 Observing Conditions"; Flags: uninsdeletekey; Check: IsWin64
Root: HKLM64; Subkey: "SOFTWARE\Classes\WeatherWatcher2.ObservingConditions\CLSID"; ValueType: string; ValueData: "{{7FCECB35-1C6B-4E48-8F5B-8D4F0E5C1101}"; Check: IsWin64
Root: HKLM64; Subkey: "SOFTWARE\Classes\CLSID\{{7FCECB35-1C6B-4E48-8F5B-8D4F0E5C1101}"; ValueType: string; ValueData: "WeatherWatcher2 Observing Conditions"; Flags: uninsdeletekey; Check: IsWin64
Root: HKLM64; Subkey: "SOFTWARE\Classes\CLSID\{{7FCECB35-1C6B-4E48-8F5B-8D4F0E5C1101}"; ValueType: string; ValueName: "AppID"; ValueData: "{{7FCECB35-1C6B-4E48-8F5B-8D4F0E5C1101}"; Check: IsWin64
Root: HKLM64; Subkey: "SOFTWARE\Classes\CLSID\{{7FCECB35-1C6B-4E48-8F5B-8D4F0E5C1101}\ProgID"; ValueType: string; ValueData: "WeatherWatcher2.ObservingConditions"; Check: IsWin64
Root: HKLM64; Subkey: "SOFTWARE\Classes\CLSID\{{7FCECB35-1C6B-4E48-8F5B-8D4F0E5C1101}\LocalServer32"; ValueType: string; ValueData: """{app}\ASCOM.WeatherWatcher2.exe"""; Check: IsWin64
Root: HKLM64; Subkey: "SOFTWARE\Classes\AppID\{{7FCECB35-1C6B-4E48-8F5B-8D4F0E5C1101}"; ValueType: string; ValueName: "RunAs"; ValueData: "Interactive User"; Flags: uninsdeletekey; Check: IsWin64
Root: HKLM64; Subkey: "SOFTWARE\ASCOM\ObservingConditions Drivers\WeatherWatcher2.ObservingConditions"; ValueType: string; ValueData: "WeatherWatcher2 Observing Conditions"; Flags: uninsdeletekey; Check: IsWin64
[Run]
Filename: "{app}\weatherwatcher-guide.html"; Description: "Open WeatherWatcher and RainCloud guide"; Flags: postinstall shellexec skipifsilent unchecked runasoriginaluser
[UninstallRun]
Filename: "{app}\ASCOM.WeatherWatcher2.exe"; Parameters: "/unregister"; Flags: runhidden waituntilterminated; RunOnceId: UnregisterWeatherWatcher2
Filename: "{dotnet4032}\regasm.exe"; Parameters: "/unregister ""{commoncf32}\ASCOM\SafetyMonitor\ASCOM.WeatherWatcher.SafetyMonitor.dll"""; Flags: runhidden waituntilterminated; RunOnceId: UnregisterSafety32
Filename: "{dotnet4064}\regasm.exe"; Parameters: "/unregister ""{commoncf32}\ASCOM\SafetyMonitor\ASCOM.WeatherWatcher.SafetyMonitor.dll"""; Flags: runhidden waituntilterminated; Check: IsWin64; RunOnceId: UnregisterSafety64
[Code]
function InitializeSetup(): Boolean;
var Version, Tail: String; Release: Cardinal; Dot, Major, Minor: Integer;
begin
  Result := False;
  if not RegQueryStringValue(HKLM32, 'SOFTWARE\ASCOM', 'PlatformVersion', Version) then begin
    MsgBox('Install ASCOM Platform 7.1 or later first.', mbError, MB_OK); Exit;
  end;
  Dot := Pos('.', Version);
  Major := StrToIntDef(Copy(Version, 1, Dot - 1), 0);
  Tail := Copy(Version, Dot + 1, Length(Version));
  Dot := Pos('.', Tail);
  if Dot > 0 then Tail := Copy(Tail, 1, Dot - 1);
  Minor := StrToIntDef(Tail, 0);
  if (Major < 7) or ((Major = 7) and (Minor < 1)) then begin
    MsgBox('ASCOM Platform 7.1 or later is required.', mbError, MB_OK); Exit;
  end;
  if not RegQueryDWordValue(HKLM32, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release) then Release := 0;
  if Release < 528040 then begin
    MsgBox('Microsoft .NET Framework 4.8 or later is required.', mbError, MB_OK); Exit;
  end;
  Result := True;
end;
function PrepareToInstall(var NeedsRestart: Boolean): String;
var Locator, Service, Processes: Variant;
begin
  Result := '';
  try
    Locator := CreateOleObject('WbemScripting.SWbemLocator');
    Service := Locator.ConnectServer('.', 'root\CIMV2');
    Processes := Service.ExecQuery('SELECT ProcessId FROM Win32_Process WHERE Name="ASCOM.WeatherWatcher2.exe" OR Name="NINA.exe"');
    if Processes.Count > 0 then Result := 'Close NINA and WeatherWatcher before installing both drivers. Also close other ASCOM clients using SafetyMonitor.';
  except
    Result := 'Unable to check running clients. Close all ASCOM clients and retry setup.';
  end;
end;
procedure RunChecked(Path, Args: String);
var Code: Integer;
begin
  if not Exec(Path, Args, '', SW_HIDE, ewWaitUntilTerminated, Code) then RaiseException('Could not start registration: ' + Path);
  if Code <> 0 then RaiseException('Driver registration failed: ' + Path + ' (exit ' + IntToStr(Code) + ')');
end;
procedure CurStepChanged(CurStep: TSetupStep);
var Args, Registered: String;
begin
  if CurStep = ssPostInstall then begin
    RunChecked(ExpandConstant('{app}\ASCOM.WeatherWatcher2.exe'), '/register');
    Args := '/codebase "' + ExpandConstant('{commoncf32}\ASCOM\SafetyMonitor\ASCOM.WeatherWatcher.SafetyMonitor.dll') + '"';
    RunChecked(ExpandConstant('{dotnet4032}\regasm.exe'), Args);
    if IsWin64 then RunChecked(ExpandConstant('{dotnet4064}\regasm.exe'), Args);
    if not RegQueryStringValue(HKLM32, 'SOFTWARE\Classes\CLSID\{#Clsid}\LocalServer32', '', Registered) then RaiseException('WeatherWatcher registration missing.');
    if Pos(Lowercase(ExpandConstant('{app}\ASCOM.WeatherWatcher2.exe')), Lowercase(Registered)) = 0 then RaiseException('WeatherWatcher registration points to another installation.');
  end;
end;
