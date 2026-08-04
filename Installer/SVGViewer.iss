#define AppName "SVGViewer"
#define AppExeName "SVGViewer.exe"
#define AppPublisher "© HN Software development - 2026"
#define AppVersion GetVersionNumbersString("..\src\SVGViewer\bin\Publish\" + AppExeName)

[Setup]
AppId={{F03368A6-429E-4683-910D-F1DB7F54B380}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL=https://hnsoftwaredevelopment.nl/
AppSupportURL=https://hnsoftwaredevelopment.nl/
AppUpdatesURL=https://hnsoftwaredevelopment.nl/
DefaultDirName={autopf}\HnSoftwaredevelopment\SVGViewer
DisableDirPage=yes
DisableProgramGroupPage=no
DisableReadyMemo=yes
DisableFinishedPage=yes
DisableWelcomePage=yes
AllowNoIcons=yes
UsePreviousAppDir=no
OutputDir=..\src\SVGViewer\bin\Installer
OutputBaseFilename=SVGViewerSetup-{#AppVersion}
SetupIconFile=..\src\SVGViewer\Assets\appicon.ico
UninstallDisplayIcon={app}\{#AppExeName}
UninstallDisplayName={#AppName}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
WizardImageFile=Assets\developer-logo-wizard.bmp
WizardSmallImageFile=Assets\app-logo-small.bmp
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=yes
RestartApplications=no
VersionInfoCompany={#AppPublisher}
VersionInfoDescription={#AppName} installer
VersionInfoProductName={#AppName}
VersionInfoProductVersion={#AppVersion}
VersionInfoVersion={#AppVersion}
AppCopyright={#AppPublisher}
VersionInfoCopyright={#AppPublisher}

[Languages]
Name: "dutch"; MessagesFile: "compiler:Languages\Dutch.isl"

[Messages]
SetupAppTitle=SVGViewer installeren of bijwerken
SetupWindowTitle=SVGViewer installeren of bijwerken
ButtonInstall=Installeren
ReadyLabel1=SVGViewer wordt geinstalleerd of bijgewerkt.
ReadyLabel2a=Klik op Installeren om te beginnen.
ReadyLabel2b=Klik op Installeren om te beginnen.

[InstallDelete]
Type: files; Name: "{app}\*.exe"
Type: files; Name: "{app}\*.dll"
Type: files; Name: "{app}\*.pdb"
Type: files; Name: "{app}\*.deps.json"
Type: files; Name: "{app}\*.runtimeconfig.json"

[Files]
Source: "..\src\SVGViewer\bin\Publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\SVGViewer"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\{#AppExeName}"

[Code]
{ --- .NET 8 Desktop Runtime prerequisite ------------------------------------
  The app is framework-dependent, so it needs the .NET 8 Desktop Runtime. If it
  is missing we download the latest 8.0 build from Microsoft (stable aka.ms link)
  and install it silently before copying the app files. }

var
  DownloadPage: TDownloadWizardPage;

function IsDotNetDesktop8Installed(): Boolean;
var
  BaseDir: String;
  FindRec: TFindRec;
begin
  Result := False;
  BaseDir := ExpandConstant('{commonpf}\dotnet\shared\Microsoft.WindowsDesktop.App');
  if not DirExists(BaseDir) then
    Exit;

  if FindFirst(BaseDir + '\8.*', FindRec) then
  begin
    try
      repeat
        if (FindRec.Attributes and FILE_ATTRIBUTE_DIRECTORY) <> 0 then
        begin
          Result := True;
          Break;
        end;
      until not FindNext(FindRec);
    finally
      FindClose(FindRec);
    end;
  end;
end;

function OnDownloadProgress(const Url, FileName: String; const Progress, ProgressMax: Int64): Boolean;
begin
  Result := True;
end;

procedure InitializeWizard;
begin
  DownloadPage := CreateDownloadPage(
    'Vereiste onderdelen',
    'De .NET 8 Desktop Runtime wordt gedownload en ge√Ønstalleerd...',
    @OnDownloadProgress);
end;

function NextButtonClick(CurPageID: Integer): Boolean;
var
  ResultCode: Integer;
begin
  Result := True;

  if (CurPageID = wpReady) and (not IsDotNetDesktop8Installed()) then
  begin
    DownloadPage.Clear;
    DownloadPage.Add(
      'https://aka.ms/dotnet/8.0/windowsdesktop-runtime-win-x64.exe',
      'windowsdesktop-runtime-8-win-x64.exe', '');
    DownloadPage.Show;
    try
      try
        DownloadPage.Download;
      except
        SuppressibleMsgBox(
          'Het downloaden van de .NET 8 Desktop Runtime is mislukt:' + #13#10 +
          AddPeriod(GetExceptionMessage) + #13#10#13#10 +
          'Controleer je internetverbinding en probeer het opnieuw.',
          mbError, MB_OK, IDOK);
        Result := False;
        Exit;
      end;

      if not Exec(ExpandConstant('{tmp}\windowsdesktop-runtime-8-win-x64.exe'),
        '/install /quiet /norestart', '', SW_SHOW, ewWaitUntilTerminated, ResultCode) then
      begin
        SuppressibleMsgBox(
          'Het installeren van de .NET 8 Desktop Runtime is mislukt.',
          mbError, MB_OK, IDOK);
        Result := False;
      end;
    finally
      DownloadPage.Hide;
    end;
  end;
end;
