;
; ASCOM WeatherWatcher2 ObservingConditions Driver Installer
; Local Server EXE Driver
;

[Setup]
AppID={{185dafc9-a1ef-46fb-93fc-e0e8735d9890}}
AppName=ASCOM WeatherWatcher 2 ObservingConditions Driver
AppVerName=ASCOM WeatherWatcher 2 ObservingConditions Driver 1.0.3
AppVersion=1.0.3
VersionInfoVersion=1.0.3

AppPublisher=Chuck Faranda
AppPublisherURL=mailto:ccd@ccdastro.net
AppSupportURL=https://ascomtalk.groups.io/g/Help
AppUpdatesURL=https://ascom-standards.org/

MinVersion=6.1sp1
PrivilegesRequired=admin

DefaultDirName={commoncf}\ASCOM\ObservingConditions\WeatherWatcher2
DisableDirPage=yes
DisableProgramGroupPage=yes

OutputDir=.
OutputBaseFilename=WeatherWatcher2_Setup

Compression=lzma
SolidCompression=yes

WizardImageFile="C:\Program Files (x86)\ASCOM\Developer\Installer Generator\Resources\WizardImage.bmp"
LicenseFile="C:\Program Files (x86)\ASCOM\Developer\Installer Generator\Resources\CreativeCommons.txt"

UninstallFilesDir={commoncf}\ASCOM\Uninstall\ObservingConditions\WeatherWatcher2


[Messages]
WelcomeLabel2=WARNING: Close NINA and all other ASCOM clients before continuing.%n%nSetup must replace and re-register the WeatherWatcher2 local server. Leaving NINA open can prevent a complete update.%n%nIt is recommended that you close all other applications before continuing.

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"


[Dirs]
Name: "{commoncf}\ASCOM\Uninstall\ObservingConditions\WeatherWatcher2"


[Tasks]
Name: source; Description: "Install source files"; Flags: unchecked


[Files]

; Main Local Server EXE
Source: "C:\Users\chuck\source\repos\WeatherWatcher2.ObservingConditions\WeatherWatcher2.ObservingConditions\bin\Release\ASCOM.WeatherWatcher2.exe"; DestDir: "{app}"; Flags: ignoreversion

; Optional readme/help file
Source: "C:\Users\chuck\source\repos\WeatherWatcher2.ObservingConditions\WeatherWatcher2.ObservingConditions\bin\Release\test.txt"; DestDir: "{app}"; Flags: isreadme ignoreversion

; Optional source files
Source: "C:\Users\chuck\source\repos\WeatherWatcher2.ObservingConditions\WeatherWatcher2.ObservingConditions\bin\Release\*"; \
    Excludes: "*.zip,*.exe,*.dll,\bin\*,\obj\*"; \
    DestDir: "{app}\Source\WeatherWatcher2 Driver"; \
    Tasks: source; \
    Flags: recursesubdirs ignoreversion


[Run]

; Register Local Server EXE with Windows + ASCOM
Filename: "{app}\ASCOM.WeatherWatcher2.exe"; \
    Parameters: "/regserver"; \
    Flags: waituntilterminated runhidden


[UninstallRun]

; Unregister Local Server EXE cleanly
Filename: "{app}\ASCOM.WeatherWatcher2.exe"; \
    Parameters: "/unregserver"; \
    RunOnceId: "UnregisterWeatherWatcher2"; \
    Flags: waituntilterminated runhidden


[Code]

const
  REQUIRED_PLATFORM_VERSION = 6.2;


function PlatformVersion(): Double;
var
  PlatVerString: String;
begin
  Result := 0.0;

  try
    if RegQueryStringValue(
      HKEY_LOCAL_MACHINE_32,
      'Software\ASCOM',
      'PlatformVersion',
      PlatVerString) then
    begin
      Result := StrToFloat(PlatVerString);
    end;
  except
    ShowExceptionMessage;
    Result := -1.0;
  end;
end;


function InitializeSetup(): Boolean;
var
  PlatformVersionNumber: Double;
begin
  Result := False;

  PlatformVersionNumber := PlatformVersion();

  if PlatformVersionNumber >= REQUIRED_PLATFORM_VERSION then
  begin
    Result := True;
  end
  else
  begin
    if PlatformVersionNumber = 0.0 then
    begin
      MsgBox(
        'No ASCOM Platform is installed.' + #13#10 +
        'Please install ASCOM Platform ' +
        Format('%3.1f', [REQUIRED_PLATFORM_VERSION]) +
        ' or later from:' + #13#10 +
        'https://www.ascom-standards.org',
        mbCriticalError,
        MB_OK);
    end
    else
    begin
      MsgBox(
        'ASCOM Platform ' +
        Format('%3.1f', [REQUIRED_PLATFORM_VERSION]) +
        ' or later is required.' + #13#10 +
        'Installed version is: ' +
        Format('%3.1f', [PlatformVersionNumber]) + #13#10 +
        'Please install the latest ASCOM Platform.',
        mbCriticalError,
        MB_OK);
    end;
  end;
end;


procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
  UninstallExe: String;
  UninstallRegistry: String;
begin
  if CurStep = ssInstall then
  begin
    UninstallRegistry :=
      ExpandConstant(
        'Software\Microsoft\Windows\CurrentVersion\Uninstall\' +
        '{#SetupSetting("AppId")}' + '_is1');

    if RegQueryStringValue(
      HKLM,
      UninstallRegistry,
      'UninstallString',
      UninstallExe) then
    begin
      MsgBox(
        'Setup will now remove the previous version.',
        mbInformation,
        MB_OK);

      Exec(
        RemoveQuotes(UninstallExe),
        '/SILENT',
        '',
        SW_SHOWNORMAL,
        ewWaitUntilTerminated,
        ResultCode);

      Sleep(1000);
    end;
  end;
end;