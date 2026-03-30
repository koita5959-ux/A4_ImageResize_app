[Setup]
AppId=DesktopKit_ImageResize
AppName=DesktopKit ImageResize
AppVersion=1.02
AppPublisher=便利アプリシリーズ
DefaultDirName={autopf}\DesktopKit\ImageResize
DefaultGroupName=DesktopKit
DisableProgramGroupPage=yes
OutputDir=installer
OutputBaseFilename=ImageResize_Setup_v1.02
Compression=lzma
SolidCompression=yes
UninstallDisplayName=DesktopKit ImageResize
SetupIconFile=_works\ImageResize.ico
UninstallDisplayIcon={app}\ImageResize.ico
PrivilegesRequired=admin
CloseApplications=no
CloseApplicationsFilter=DesktopKit.ImageResize.exe

; カスタムページで全て制御するため、標準ページを無効化
DisableWelcomePage=yes
DisableDirPage=yes
DisableReadyPage=yes
DisableFinishedPage=yes

[Languages]
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"

[Files]
Source: "publish\DesktopKit.ImageResize.exe"; DestDir: "{app}"; DestName: "ImageResize.exe"; Flags: ignoreversion
Source: "_works\ImageResize.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "ご利用にあたって.txt"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\ImageResize — 画像一括リサイズ"; Filename: "{app}\ImageResize.exe"; IconFilename: "{app}\ImageResize.ico"
Name: "{userdesktop}\ImageResize"; Filename: "{app}\ImageResize.exe"; IconFilename: "{app}\ImageResize.ico"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "デスクトップにショートカットを作成"; GroupDescription: "追加オプション:"

[UninstallDelete]
Type: filesandordirs; Name: "{userappdata}\DesktopKit"
Type: filesandordirs; Name: "{app}"

[Code]
const
  OPTIMAL_RUNTIME_VERSION = '8.0.24';
  RUNTIME_REG_KEY = 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App';
  UNINSTALL_REG_KEY = 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\DesktopKit_ImageResize_is1';
  COLOR_RED = $004040FF;
  COLOR_GREEN = $00008200;
  COLOR_ORANGE = $000080FF;
  COLOR_DESC = $00808080;
  COLOR_DESC_DISABLED = $00C0C0C0;

var
  CustomPage: TWizardPage;
  LblSectionTitle: TLabel;
  LblRuntimeStatus: TLabel;
  LblAppStatus: TLabel;
  RadioInstallBoth: TNewRadioButton;
  RadioInstallApp: TNewRadioButton;
  RadioUninstall: TNewRadioButton;
  LblDescBoth: TLabel;
  LblDescApp: TLabel;
  LblDescUninstall: TLabel;
  DetectedRuntimeStatus: Integer;
  DetectedRuntimeVersion: String;
  DetectedAppStatus: Integer;
  DetectedAppVersion: String;
  SelectedAction: String;

// ========== プロセス検出 ==========

function IsProcessRunning(FileName: String): Boolean;
var
  ResultCode: Integer;
begin
  Result := False;
  if Exec(ExpandConstant('{cmd}'), '/C tasklist /FI "IMAGENAME eq ' + FileName + '" /NH 2>nul | find /I "' + FileName + '" >nul',
    '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
    Result := (ResultCode = 0);
end;

function WaitForProcessExit(FileName: String): Boolean;
var
  ResultCode: Integer;
  I: Integer;
begin
  Result := False;
  for I := 1 to 5 do
  begin
    Sleep(1000);
    if not IsProcessRunning(FileName) then
    begin
      Result := True;
      Exit;
    end;
    Exec('taskkill', '/IM ' + FileName, '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  end;
  MsgBox('ImageResize を終了できませんでした。' + #13#10 +
    'タスクマネージャーから手動で終了してください。', mbError, MB_OK);
end;

// ========== 初期化 ==========

function InitializeSetup(): Boolean;
var
  ResultCode: Integer;
begin
  Result := True;
  SelectedAction := '';

  if IsProcessRunning('ImageResize.exe') then
  begin
    if MsgBox('ImageResize が実行中です。終了してセットアップを続けますか？',
      mbConfirmation, MB_OKCANCEL) = IDOK then
    begin
      Exec('taskkill', '/IM ImageResize.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
      if not WaitForProcessExit('ImageResize.exe') then
      begin
        Result := False;
        Exit;
      end;
    end
    else
      Result := False;
  end;
end;

// ========== 環境検出 ==========

procedure DetectEnvironment();
var
  Names: TArrayOfString;
  I: Integer;
  Value: Cardinal;
begin
  DetectedRuntimeStatus := 0;
  DetectedRuntimeVersion := '';

  if RegGetValueNames(HKLM, RUNTIME_REG_KEY, Names) then
  begin
    for I := 0 to GetArrayLength(Names) - 1 do
    begin
      if RegQueryDWordValue(HKLM, RUNTIME_REG_KEY, Names[I], Value) and (Value = 1) then
      begin
        if Names[I] = OPTIMAL_RUNTIME_VERSION then
        begin
          DetectedRuntimeStatus := 1;
          DetectedRuntimeVersion := Names[I];
          Break;
        end
        else
        begin
          DetectedRuntimeStatus := 2;
          if (DetectedRuntimeVersion = '') or (CompareStr(Names[I], DetectedRuntimeVersion) > 0) then
            DetectedRuntimeVersion := Names[I];
        end;
      end;
    end;
  end;

  DetectedAppStatus := 0;
  DetectedAppVersion := '';

  if RegQueryStringValue(HKLM, UNINSTALL_REG_KEY, 'DisplayVersion', DetectedAppVersion) then
  begin
    if DetectedAppVersion = '' then
      DetectedAppVersion := '(不明)';
    DetectedAppStatus := 1;
  end;
end;

// ========== 検出結果をUIに反映 ==========

procedure ApplyDetectionResults();
begin
  case DetectedRuntimeStatus of
    0:
      begin
        LblRuntimeStatus.Caption := '.NET Desktop Runtime：インストールされていません';
        LblRuntimeStatus.Font.Color := COLOR_RED;
      end;
    1:
      begin
        LblRuntimeStatus.Caption := '.NET Desktop Runtime v' + DetectedRuntimeVersion + '：インストール済み';
        LblRuntimeStatus.Font.Color := COLOR_GREEN;
      end;
    2:
      begin
        LblRuntimeStatus.Caption := '.NET Desktop Runtime v' + DetectedRuntimeVersion + '：インストール済み（推奨 v' + OPTIMAL_RUNTIME_VERSION + ' と異なります）';
        LblRuntimeStatus.Font.Color := COLOR_ORANGE;
      end;
  end;

  case DetectedAppStatus of
    0:
      begin
        LblAppStatus.Caption := 'ImageResize：インストールされていません';
        LblAppStatus.Font.Color := COLOR_RED;
      end;
    1:
      begin
        LblAppStatus.Caption := 'ImageResize v' + DetectedAppVersion + '：インストール済み';
        LblAppStatus.Font.Color := COLOR_GREEN;
      end;
  end;

  RadioInstallBoth.Enabled := True;
  LblDescBoth.Font.Color := COLOR_DESC;

  RadioInstallApp.Enabled := True;
  LblDescApp.Font.Color := COLOR_DESC;

  RadioUninstall.Enabled := (DetectedAppStatus = 1);
  if RadioUninstall.Enabled then
    LblDescUninstall.Font.Color := COLOR_DESC
  else
    LblDescUninstall.Font.Color := COLOR_DESC_DISABLED;

  RadioInstallBoth.Checked := False;
  RadioInstallApp.Checked := False;
  RadioUninstall.Checked := False;

  if DetectedRuntimeStatus = 0 then
    RadioInstallBoth.Checked := True
  else
    RadioInstallApp.Checked := True;
end;

// ========== ウィザードUI構築 ==========

procedure InitializeWizard();
var
  PageWidth: Integer;
  Y: Integer;
  Bevel: TBevel;
begin
  DetectEnvironment();

  CustomPage := CreateCustomPage(wpWelcome,
    'ImageResize セットアップ',
    'お使いの環境を確認し、操作を選択してください。');

  PageWidth := CustomPage.SurfaceWidth;
  Y := 8;

  LblSectionTitle := TLabel.Create(WizardForm);
  LblSectionTitle.Parent := CustomPage.Surface;
  LblSectionTitle.Caption := '環境チェック結果';
  LblSectionTitle.Font.Size := 10;
  LblSectionTitle.Font.Style := [fsBold];
  LblSectionTitle.Left := 0;
  LblSectionTitle.Top := Y;
  LblSectionTitle.Width := PageWidth;
  Y := Y + 28;

  LblRuntimeStatus := TLabel.Create(WizardForm);
  LblRuntimeStatus.Parent := CustomPage.Surface;
  LblRuntimeStatus.Font.Size := 9;
  LblRuntimeStatus.Left := 12;
  LblRuntimeStatus.Top := Y;
  LblRuntimeStatus.Width := PageWidth - 12;
  Y := Y + 22;

  LblAppStatus := TLabel.Create(WizardForm);
  LblAppStatus.Parent := CustomPage.Surface;
  LblAppStatus.Font.Size := 9;
  LblAppStatus.Left := 12;
  LblAppStatus.Top := Y;
  LblAppStatus.Width := PageWidth - 12;
  Y := Y + 30;

  Bevel := TBevel.Create(WizardForm);
  Bevel.Parent := CustomPage.Surface;
  Bevel.Shape := bsTopLine;
  Bevel.Left := 0;
  Bevel.Top := Y;
  Bevel.Width := PageWidth;
  Bevel.Height := 2;
  Y := Y + 12;

  RadioInstallBoth := TNewRadioButton.Create(WizardForm);
  RadioInstallBoth.Parent := CustomPage.Surface;
  RadioInstallBoth.Caption := '標準インストール';
  RadioInstallBoth.Font.Size := 9;
  RadioInstallBoth.Font.Style := [fsBold];
  RadioInstallBoth.Left := 0;
  RadioInstallBoth.Top := Y;
  RadioInstallBoth.Width := PageWidth;
  RadioInstallBoth.Height := 20;
  Y := Y + 20;

  LblDescBoth := TLabel.Create(WizardForm);
  LblDescBoth.Parent := CustomPage.Surface;
  LblDescBoth.Caption := '.NET Desktop Runtime と ImageResize をインストールします。';
  LblDescBoth.Font.Size := 8;
  LblDescBoth.Left := 20;
  LblDescBoth.Top := Y;
  LblDescBoth.Width := PageWidth - 20;
  Y := Y + 28;

  RadioInstallApp := TNewRadioButton.Create(WizardForm);
  RadioInstallApp.Parent := CustomPage.Surface;
  RadioInstallApp.Caption := 'ImageResize のみインストール';
  RadioInstallApp.Font.Size := 9;
  RadioInstallApp.Font.Style := [fsBold];
  RadioInstallApp.Left := 0;
  RadioInstallApp.Top := Y;
  RadioInstallApp.Width := PageWidth;
  RadioInstallApp.Height := 20;
  Y := Y + 20;

  LblDescApp := TLabel.Create(WizardForm);
  LblDescApp.Parent := CustomPage.Surface;
  LblDescApp.Caption := 'ImageResize 本体のみをインストール（上書き更新）します。ランタイムは別途必要です。';
  LblDescApp.Font.Size := 8;
  LblDescApp.Left := 20;
  LblDescApp.Top := Y;
  LblDescApp.Width := PageWidth - 20;
  Y := Y + 28;

  RadioUninstall := TNewRadioButton.Create(WizardForm);
  RadioUninstall.Parent := CustomPage.Surface;
  RadioUninstall.Caption := 'アンインストール';
  RadioUninstall.Font.Size := 9;
  RadioUninstall.Font.Style := [fsBold];
  RadioUninstall.Left := 0;
  RadioUninstall.Top := Y;
  RadioUninstall.Width := PageWidth;
  RadioUninstall.Height := 20;
  Y := Y + 20;

  LblDescUninstall := TLabel.Create(WizardForm);
  LblDescUninstall.Parent := CustomPage.Surface;
  LblDescUninstall.Caption := 'ImageResize をアンインストールします。';
  LblDescUninstall.Font.Size := 8;
  LblDescUninstall.Left := 20;
  LblDescUninstall.Top := Y;
  LblDescUninstall.Width := PageWidth - 20;

  WizardForm.NextButton.Caption := '実行';
  WizardForm.BackButton.Visible := False;

  ApplyDetectionResults();
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  if CurPageID = CustomPage.ID then
  begin
    WizardForm.NextButton.Caption := '実行';
    WizardForm.BackButton.Visible := False;
  end;
end;

// ========== ランタイムインストーラー実行 ==========

function InstallRuntime(): Boolean;
var
  RuntimeInstaller: String;
  ResultCode: Integer;
begin
  Result := False;

  RuntimeInstaller := ExpandConstant('{src}\windowsdesktop-runtime-' + OPTIMAL_RUNTIME_VERSION + '-win-x64.exe');

  if not FileExists(RuntimeInstaller) then
  begin
    MsgBox('.NET Desktop Runtime のインストーラーが見つかりません。' + #13#10 +
      '以下のファイルをセットアップと同じフォルダに配置してください：' + #13#10#13#10 +
      'windowsdesktop-runtime-' + OPTIMAL_RUNTIME_VERSION + '-win-x64.exe',
      mbError, MB_OK);
    Exit;
  end;

  MsgBox('.NET Desktop Runtime のインストールを開始します。' + #13#10 +
    'ランタイムのインストーラーが起動しますので、指示に従ってインストールしてください。',
    mbInformation, MB_OK);

  if Exec(RuntimeInstaller, '/install /passive /norestart', '', SW_SHOWNORMAL, ewWaitUntilTerminated, ResultCode) then
  begin
    if ResultCode = 0 then
    begin
      Result := True;
      DetectEnvironment();
      ApplyDetectionResults();
    end
    else if ResultCode = 1602 then
      MsgBox('ランタイムのインストールがキャンセルされました。', mbInformation, MB_OK)
    else if ResultCode = 1638 then
    begin
      Result := True;
      MsgBox('より新しいバージョンのランタイムが既にインストールされています。', mbInformation, MB_OK);
    end
    else
      MsgBox('ランタイムのインストールに失敗しました。（エラーコード: ' + IntToStr(ResultCode) + '）', mbError, MB_OK);
  end
  else
    MsgBox('ランタイムインストーラーの起動に失敗しました。', mbError, MB_OK);
end;

// ========== アンインストール処理 ==========

function RunUninstall(): Boolean;
var
  UninstallString: String;
  ResultCode: Integer;
begin
  Result := False;

  if not RegQueryStringValue(HKLM, UNINSTALL_REG_KEY, 'UninstallString', UninstallString) then
  begin
    MsgBox('アンインストール情報が見つかりません。' + #13#10 +
      '手動でアンインストールする場合は、以下のフォルダを削除してください：' + #13#10 +
      ExpandConstant('{autopf}\DesktopKit\ImageResize'),
      mbError, MB_OK);
    Exit;
  end;

  if MsgBox('ImageResize をアンインストールします。よろしいですか？',
    mbConfirmation, MB_OKCANCEL) = IDCANCEL then
    Exit;

  if IsProcessRunning('ImageResize.exe') then
  begin
    Exec('taskkill', '/IM ImageResize.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    if not WaitForProcessExit('ImageResize.exe') then
    begin
      Result := False;
      Exit;
    end;
  end;

  if Exec(RemoveQuotes(UninstallString), '/SILENT', '', SW_SHOWNORMAL, ewWaitUntilTerminated, ResultCode) then
  begin
    Result := True;
    MsgBox('アンインストールが完了しました。', mbInformation, MB_OK);
  end
  else
    MsgBox('アンインストールに失敗しました。', mbError, MB_OK);
end;

// ========== 「実行」ボタン押下時の処理 ==========

function NextButtonClick(CurPageID: Integer): Boolean;
var
  RuntimeResult: Boolean;
begin
  Result := True;

  if CurPageID = CustomPage.ID then
  begin
    if RadioInstallBoth.Checked then
    begin
      SelectedAction := 'both';
      if DetectedRuntimeStatus <> 1 then
      begin
        RuntimeResult := InstallRuntime();
        if not RuntimeResult then
        begin
          if MsgBox('ランタイムのインストールが完了しませんでした。' + #13#10 +
            'ImageResize のインストールを続けますか？',
            mbConfirmation, MB_YESNO) = IDNO then
          begin
            Result := False;
            Exit;
          end;
        end;
      end;
      Result := True;
    end
    else if RadioInstallApp.Checked then
    begin
      SelectedAction := 'app';
      Result := True;
    end
    else if RadioUninstall.Checked then
    begin
      SelectedAction := 'uninstall';
      if RunUninstall() then
      begin
        WizardForm.Tag := 1;
        WizardForm.Close;
      end;
      Result := False;
    end;
  end;
end;

// ========== インストール完了後にアプリを起動 ==========

procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
begin
  if CurStep = ssPostInstall then
  begin
    if (SelectedAction = 'both') or (SelectedAction = 'app') then
    begin
      MsgBox('ImageResize のインストールが完了しました。', mbInformation, MB_OK);
      Exec(ExpandConstant('{app}\ImageResize.exe'), '', '', SW_SHOWNORMAL, ewNoWait, ResultCode);
    end;
  end;
end;

procedure CancelButtonClick(CurPageID: Integer; var Cancel, Confirm: Boolean);
begin
  if WizardForm.Tag = 1 then
    Confirm := False;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  ResultCode: Integer;
begin
  if CurUninstallStep = usUninstall then
  begin
    if IsProcessRunning('ImageResize.exe') then
    begin
      Exec('taskkill', '/IM ImageResize.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
      WaitForProcessExit('ImageResize.exe');
    end;
  end;
end;
