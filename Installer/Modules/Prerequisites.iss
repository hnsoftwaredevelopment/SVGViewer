[Code]
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
          Exit;
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
    '.NET Desktop Runtime {#DotNetDesktopRuntimeVersion} wordt gedownload en geïnstalleerd...',
    @OnDownloadProgress);
  DownloadPage.ShowBaseNameInsteadOfUrl := True;
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
      '{#DotNetDesktopRuntimeUrl}',
      '{#DotNetDesktopRuntimeFileName}',
      '{#DotNetDesktopRuntimeSha256}');
    DownloadPage.Show;
    try
      try
        DownloadPage.Download;
      except
        SuppressibleMsgBox(
          'Het downloaden van de .NET Desktop Runtime is mislukt:' + #13#10 +
          AddPeriod(GetExceptionMessage) + #13#10#13#10 +
          'Controleer je internetverbinding en probeer het opnieuw.',
          mbError, MB_OK, IDOK);
        Result := False;
        Exit;
      end;

      if not Exec(
        ExpandConstant('{tmp}\{#DotNetDesktopRuntimeFileName}'),
        '/install /quiet /norestart', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
      begin
        SuppressibleMsgBox('Het installeren van de .NET Desktop Runtime kon niet worden gestart.', mbError, MB_OK, IDOK);
        Result := False;
      end
      else if (ResultCode <> 0) and (ResultCode <> 3010) then
      begin
        SuppressibleMsgBox(
          'Het installeren van de .NET Desktop Runtime is mislukt (exitcode ' + IntToStr(ResultCode) + ').',
          mbError, MB_OK, IDOK);
        Result := False;
      end;
    finally
      DownloadPage.Hide;
    end;
  end;
end;
