#define MyAppName "QBTag"
#ifndef MyAppVersion
  #define MyAppVersion "0.0.0"
#endif
#define MyAppPublisher "Abanaki"
#define MyAppExeName "QBTag.exe"
#define BuildDir "..\QBTag\bin\Release\net40"

[Setup]
AppId={{F3B2C83A-26FF-45F0-A812-B58AC802494F}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableDirPage=no
DisableProgramGroupPage=yes
OutputDir=output
OutputBaseFilename=QBTag-Setup-{#MyAppVersion}
SetupIconFile=..\QBTag\app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x86compatible
MinVersion=6.1sp1

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "installqodbc"; Description: "Install QODBC Driver for QuickBooks (required for ODBC queries)"; GroupDescription: "Prerequisites:"; Check: not IsQODBCInstalled
Name: "installqbfc"; Description: "Install QuickBooks SDK 12.0 (required for QuickBooks connection)"; GroupDescription: "Prerequisites:"; Check: not IsQBFC12Installed

[Files]
; .NET Framework 4.0 offline installer (bundled, silent install if needed)
Source: "..\installer\prereqs\dotNetFx40_Full_x86_x64.exe"; DestDir: "{tmp}"; Flags: ignoreversion deleteafterinstall; Check: not IsDotNet40Installed

; QODBC installer (bundled, optional, skipifsourcedoesntexist so builds work without it)
Source: "..\installer\prereqs\qodbc.exe"; DestDir: "{tmp}"; Flags: ignoreversion deleteafterinstall skipifsourcedoesntexist; Tasks: installqodbc

; QBFC12 SDK installer (bundled, optional)
Source: "..\installer\prereqs\qbsdk120.exe"; DestDir: "{tmp}"; Flags: ignoreversion deleteafterinstall skipifsourcedoesntexist; Tasks: installqbfc

; Crystal Reports Runtime (required, installs CR engine, log4net, etc. to GAC)
Source: "..\installer\prereqs\CRRedist2008_x86.msi"; DestDir: "{tmp}"; Flags: ignoreversion deleteafterinstall skipifsourcedoesntexist; Check: not IsCRRuntimeInstalled

; Main application
Source: "{#BuildDir}\QBTag.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildDir}\QBTag.exe.config"; DestDir: "{app}"; Flags: ignoreversion

; Application DLLs
Source: "{#BuildDir}\Interop.QBFC12Lib.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildDir}\Logs.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildDir}\QBHelpers.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildDir}\QBSalesOrder.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildDir}\QuickBooksDAL.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildDir}\QuickBooksHandler.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildDir}\QuickBooksModel.dll"; DestDir: "{app}"; Flags: ignoreversion

; Crystal Reports DLLs loaded from GAC (installed by CRRedist2008_x86.msi)

; Report templates
Source: "{#BuildDir}\tag.rpt"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildDir}\TagWithQRCodes.rpt"; DestDir: "{app}"; Flags: ignoreversion

; Config (don't overwrite user config on upgrade)
Source: "{#BuildDir}\Product.xml"; DestDir: "{app}"; Flags: onlyifdoesntexist
Source: "{#BuildDir}\QBTag.exe.manifest"; DestDir: "{app}"; Flags: ignoreversion

[Dirs]
Name: "{localappdata}\QBTag\Log"
; Machine-wide QuickBooks login (qb-user.txt, qb-password.dat encrypted with DPAPI); users may update it
Name: "{commonappdata}\QBTag"; Permissions: users-modify

[UninstallDelete]
; The stored QuickBooks login does not outlive the program (upgrades do not run the uninstaller).
Type: files; Name: "{commonappdata}\QBTag\qb-password.dat"
Type: files; Name: "{commonappdata}\QBTag\qb-user.txt"
Type: dirifempty; Name: "{commonappdata}\QBTag"

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
; Install .NET Framework 4.0 silently if needed
Filename: "{tmp}\dotNetFx40_Full_x86_x64.exe"; Parameters: "/q /norestart"; StatusMsg: "Installing .NET Framework 4.0 (this may take a few minutes)..."; Flags: waituntilterminated; Check: not IsDotNet40Installed

; Install Crystal Reports Runtime silently if needed
Filename: "msiexec.exe"; Parameters: "/i ""{tmp}\CRRedist2008_x86.msi"" /qn /norestart"; StatusMsg: "Installing Crystal Reports Runtime (this may take a few minutes)..."; Flags: waituntilterminated skipifdoesntexist; Check: not IsCRRuntimeInstalled

; Install QBFC12 SDK if selected
Filename: "{tmp}\qbsdk120.exe"; Parameters: "/s /v""/qn"""; StatusMsg: "Installing QuickBooks SDK 12.0 (this may take a few minutes)..."; Flags: waituntilterminated skipifdoesntexist; Tasks: installqbfc

; Install QODBC if selected
Filename: "{tmp}\qodbc.exe"; StatusMsg: "Installing QODBC Driver for QuickBooks..."; Flags: waituntilterminated skipifdoesntexist; Tasks: installqodbc

; Create database if it doesn't exist at the chosen path
Filename: "{app}\{#MyAppExeName}"; Parameters: "--init-db ""{code:GetDbPath}"""; StatusMsg: "Initializing tag database..."; Flags: runhidden waituntilterminated

; Set the database path in app config
Filename: "{app}\{#MyAppExeName}"; Parameters: "--set-db ""{code:GetDbPath}"""; StatusMsg: "Configuring database path..."; Flags: runhidden waituntilterminated

; Store the QuickBooks login (user + password, encrypted for this computer) if one was entered.
; The password goes through a file in Setup's private temp folder, never on the command line;
; the file is deleted as soon as QBTag has read it.
Filename: "{app}\{#MyAppExeName}"; Parameters: "{code:QbLoginParams}"; StatusMsg: "Storing the QuickBooks login..."; Flags: runhidden waituntilterminated; Check: QbLoginWanted; AfterInstall: DeleteQbLoginFile

; Launch app
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent

[Code]
var
  QbPage: TInputQueryWizardPage;
  QbNoPassword: TNewCheckBox;
  DbPage: TWizardPage;
  DbPathEdit: TNewEdit;
  DbBrowseBtn: TNewButton;
  DbNewBtn: TNewButton;

procedure DbBrowseClick(Sender: TObject);
var
  FileName: String;
begin
  FileName := DbPathEdit.Text;
  if GetOpenFileName('Select existing database file', FileName, '', 'Access Database (*.mdb)|*.mdb', 'mdb') then
    DbPathEdit.Text := FileName;
end;

procedure DbNewClick(Sender: TObject);
var
  Path: String;
begin
  Path := ExtractFilePath(DbPathEdit.Text);
  if BrowseForFolder('Select folder for new database:', Path, False) then
    DbPathEdit.Text := Path + '\usman.mdb';
end;

procedure InitializeWizard;
var
  Lbl: TNewStaticText;
begin
  DbPage := CreateCustomPage(wpSelectDir,
    'Tag Database Location',
    'Select an existing database or choose where to create a new one.');

  Lbl := TNewStaticText.Create(DbPage);
  Lbl.Parent := DbPage.Surface;
  Lbl.Caption := 'Enter the full path to an existing usman.mdb file, or browse to select one.' + #13#10 +
    'To create a new database, click "New..." and pick a folder.' + #13#10 +
    'Supports local and network (UNC) paths.';
  Lbl.Top := 0;
  Lbl.Left := 0;
  Lbl.Width := DbPage.SurfaceWidth;
  Lbl.AutoSize := True;
  Lbl.WordWrap := True;

  DbPathEdit := TNewEdit.Create(DbPage);
  DbPathEdit.Parent := DbPage.Surface;
  DbPathEdit.Top := Lbl.Top + Lbl.Height + 16;
  DbPathEdit.Left := 0;
  DbPathEdit.Width := DbPage.SurfaceWidth - 170;
  DbPathEdit.Text := ExpandConstant('{localappdata}\QBTag\usman.mdb');

  DbBrowseBtn := TNewButton.Create(DbPage);
  DbBrowseBtn.Parent := DbPage.Surface;
  DbBrowseBtn.Top := DbPathEdit.Top - 1;
  DbBrowseBtn.Left := DbPathEdit.Left + DbPathEdit.Width + 8;
  DbBrowseBtn.Width := 72;
  DbBrowseBtn.Height := DbPathEdit.Height + 2;
  DbBrowseBtn.Caption := 'Browse...';
  DbBrowseBtn.OnClick := @DbBrowseClick;

  DbNewBtn := TNewButton.Create(DbPage);
  DbNewBtn.Parent := DbPage.Surface;
  DbNewBtn.Top := DbPathEdit.Top - 1;
  DbNewBtn.Left := DbBrowseBtn.Left + DbBrowseBtn.Width + 4;
  DbNewBtn.Width := 72;
  DbNewBtn.Height := DbPathEdit.Height + 2;
  DbNewBtn.Caption := 'New...';
  DbNewBtn.OnClick := @DbNewClick;

  QbPage := CreateInputQueryPage(DbPage.ID,
    'QuickBooks Login',
    'The QuickBooks user and password QBTag connects with.',
    'QBTag reads sales orders from QuickBooks through QODBC and logs in as this QuickBooks user. ' +
    'The password is stored encrypted on this computer (Windows DPAPI), never in a plain file. ' +
    'Leave the password empty to set it later: QBTag asks for it on first start, or run QBTag --set-qb-password.');
  QbPage.Add('QuickBooks user:', False);
  QbPage.Add('QuickBooks password:', True);
  QbPage.Values[0] := 'Admin';

  QbNoPassword := TNewCheckBox.Create(QbPage);
  QbNoPassword.Parent := QbPage.Surface;
  QbNoPassword.Top := QbPage.Edits[1].Top + QbPage.Edits[1].Height + 12;
  QbNoPassword.Left := QbPage.Edits[1].Left;
  QbNoPassword.Width := QbPage.SurfaceWidth;
  QbNoPassword.Caption := 'This QuickBooks user has no password';
end;

function QbUserName(): String;
begin
  // No double quotes: the name is passed as one quoted command-line argument.
  Result := Trim(QbPage.Values[0]);
  StringChangeEx(Result, '"', '', True);
  if Result = '' then
    Result := 'Admin';
end;

function QbLoginWanted(): Boolean;
begin
  Result := QbNoPassword.Checked or (QbPage.Values[1] <> '') or (CompareText(QbUserName(), 'Admin') <> 0);
end;

function QbLoginParams(Param: String): String;
var
  PwFile: String;
  Lines: TArrayOfString;
begin
  Result := '--set-qb-password --user "' + QbUserName() + '"';
  if QbNoPassword.Checked then
    Result := Result + ' --none'
  else
  begin
    PwFile := ExpandConstant('{tmp}\qb-login.txt');
    SetArrayLength(Lines, 1);
    Lines[0] := QbPage.Values[1];
    SaveStringsToUTF8File(PwFile, Lines, False);
    Result := Result + ' --file "' + PwFile + '"';
  end;
end;

// Runs right after QBTag has read the file ([Run] AfterInstall), so the password
// is not left on disk while the final page is shown.
procedure DeleteQbLoginFile;
begin
  DeleteFile(ExpandConstant('{tmp}\qb-login.txt'));
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssDone then
    DeleteQbLoginFile;
end;

function GetDbPath(Param: String): String;
begin
  Result := DbPathEdit.Text;
end;

function IsDotNet40Installed(): Boolean;
var
  Release: Cardinal;
  Success: Boolean;
begin
  Success := RegQueryDWordValue(HKLM,
    'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full',
    'Release', Release);
  if not Success then
    Success := RegValueExists(HKLM,
      'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full',
      'Install');
  Result := Success;
end;

function IsQBFC12Installed(): Boolean;
begin
  Result := FileExists(ExpandConstant('{commonpf32}\Intuit\QuickBooks\qbfc12.dll'));
end;

function IsQODBCInstalled(): Boolean;
begin
  Result := RegValueExists(HKLM,
    'SOFTWARE\ODBC\ODBCINST.INI\QuickBooks Data',
    'Driver');
  if not Result then
    Result := RegValueExists(HKLM,
      'SOFTWARE\WOW6432Node\ODBC\ODBCINST.INI\QuickBooks Data',
      'Driver');
end;

function IsCRRuntimeInstalled(): Boolean;
begin
  Result := RegValueExists(HKLM,
    'SOFTWARE\WOW6432Node\SAP BusinessObjects\Crystal Reports for .NET Framework 2.0',
    'InstallDir');
  if not Result then
    Result := RegValueExists(HKLM,
      'SOFTWARE\SAP BusinessObjects\Crystal Reports for .NET Framework 2.0',
      'InstallDir');
  if not Result then
    Result := FileExists(ExpandConstant('{commonpf32}\SAP BusinessObjects\Crystal Reports for .NET Framework 2.0\Common\SAP BusinessObjects Enterprise XI 4.0\win32_x86\crpe32.dll'));
end;
