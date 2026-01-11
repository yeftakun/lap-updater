#define MyAppName "LapUpdater"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "yeftakun"
#define MyAppExeName "LapUpdater.exe"
#define MyIconFile "icon.ico"

[Setup]
AppId={{B8C4193B-4E0E-4780-B81A-8666C52E1E9D}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}

WizardImageFile=app/shiroko-vert.bmp
WizardSmallImageFile=app/shiroko-miaw.bmp

DisableWelcomePage=no
DisableFinishedPage=no


ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}

OutputBaseFilename={#MyAppName}-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=classic

; Run setup as admin (UAC prompt)
PrivilegesRequired=admin

; Installer / Add-Remove Programs icon
SetupIconFile=app\{#MyIconFile}
UninstallDisplayIcon={app}\{#MyIconFile}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; Flags: unchecked

[Files]
; Bundle everything from the "app" folder (exe, dll, deps/runtime json, icon, etc.)
Source: "app\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
; Use icon.ico for shortcuts (works even if exe itself has no embedded icon yet)
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyIconFile}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon; IconFilename: "{app}\{#MyIconFile}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent

[Code]
var
  ConfirmPage: TWizardPage;
  CbSetupDone: TNewCheckBox;
  CbNoManipulate: TNewCheckBox;

procedure UpdateNextButtonState();
begin
  WizardForm.NextButton.Enabled :=
    Assigned(CbSetupDone) and Assigned(CbNoManipulate) and
    CbSetupDone.Checked and CbNoManipulate.Checked;
end;

procedure CheckBoxClick(Sender: TObject);
begin
  UpdateNextButtonState();
end;

procedure InitializeWizard();
begin
  ConfirmPage :=
    CreateCustomPage(
      wpSelectTasks,
      'Confirmation',
      'Please confirm the statements below before continuing.'
    );

  CbSetupDone := TNewCheckBox.Create(ConfirmPage);
  CbSetupDone.Parent := ConfirmPage.Surface;
  CbSetupDone.Left := ScaleX(0);
  CbSetupDone.Top := ScaleY(8);
  CbSetupDone.Width := ConfirmPage.SurfaceWidth;
  CbSetupDone.Caption := 'I have previously completed the Assetto Corsa Lap Archive setup.';
  CbSetupDone.Checked := False;
  CbSetupDone.OnClick := @CheckBoxClick;

  CbNoManipulate := TNewCheckBox.Create(ConfirmPage);
  CbNoManipulate.Parent := ConfirmPage.Surface;
  CbNoManipulate.Left := ScaleX(0);
  CbNoManipulate.Top := ScaleY(32);
  CbNoManipulate.Width := ConfirmPage.SurfaceWidth;
  CbNoManipulate.Caption := 'I will not manipulate the lap data in `personalbest.ini`.';
  CbNoManipulate.Checked := False;
  CbNoManipulate.OnClick := @CheckBoxClick;
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  if (CurPageID = ConfirmPage.ID) then
    UpdateNextButtonState()
  else
    WizardForm.NextButton.Enabled := True;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;

  if (CurPageID = ConfirmPage.ID) then
  begin
    if not (CbSetupDone.Checked and CbNoManipulate.Checked) then
    begin
      MsgBox('You must check both statements to continue.', mbError, MB_OK);
      Result := False;
    end;
  end;
end;

