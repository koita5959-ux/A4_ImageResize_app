# ImageResize Phase 2 制作指示書

**DesktopKit A-4 — 画像一括リサイズ 機能実装**

作成：C#開発プロジェクト
日付：2026年3月30日
実装：ClaudeCode
品質確認：ユーザー（西村）

---

## 前提

- Phase 1（共通基盤）およびPhase 1.5（UI骨格）は完了済み
- MainForm.csにUI骨格が配置されている状態からの作業
- 本制作指示書は「ImageResize_制作仕様書_v2_0.md」に基づく
- 制作仕様書は `_works/ImageResize_制作仕様書_v2_0.md` に配置済み
- アイコンは `_works/ImageResize.ico` に配置済み

## フォルダ構成（現状）

```
A4_ImageResize_app/
├── ImageResize/
│   ├── Common/
│   │   ├── AppSettings.cs
│   │   ├── BaseForm.cs
│   │   ├── DesktopKit.Common.csproj
│   │   ├── FileDialogHelper.cs
│   │   └── StatusHelper.cs
│   ├── MainForm.cs
│   ├── Program.cs
│   ├── DesktopKit.ImageResize.csproj
│   └── Directory.Build.props
├── _works/
│   ├── ImageResize_制作仕様書_v2_0.md
│   ├── ImageResize.ai
│   ├── ImageResize.ico
│   └── README.md
├── publish/
├── build.bat
└── .gitignore
```

---

## Phase 2A：UI更新 + ファイル読み込み + 一覧表示

### 目的

フォルダを指定し、条件を設定し、対象ファイル一覧がアラート付きで表示されるところまでを実装する。リサイズ処理自体は実装しない。

### 作業内容

#### 1. ImageSharpパッケージの追加

DesktopKit.ImageResize.csproj に以下のNuGetパッケージを追加する：

```xml
<PackageReference Include="SixLabors.ImageSharp" Version="3.*" />
```

#### 2. MainForm.cs の UI更新

Phase 1.5 で配置済みのUI骨格を、制作仕様書v2.0に合わせて更新する。

**変更点：**

| 項目 | Phase 1.5 | Phase 2A |
|------|-----------|----------|
| 形式チェックボックス | ☑ .jpg ☑ .png ☑ .bmp | ☑ .jpg/.jpeg ☑ .png ☑ .webp |
| 品質パラメータ | なし | 「JPEG/WebP品質:」Label + NumericUpDown（初期値95、範囲1〜100）+ Label「％」 |
| DataGridView列 | 4列（ファイル名/サイズ/解像度/推定後） | 6列（☑/ファイル名/形式/サイズ/解像度/推定後サイズ） |
| 出力設定 | なし | 「出力フォルダ名:」TextBox + 「出力先:」TextBox + 「参照」Button |

**コントロール配置（上から順に）：**

1. Button「フォルダを選択」+ TextBox（読み取り専用）— フォルダパス表示
2. Label「対象形式:」+ CheckBox × 3（「.jpg/.jpeg」「.png」「.webp」、すべて初期ON）
3. Label「リサイズ倍率:」+ NumericUpDown（初期値50、範囲1〜100）+ Label「％」
4. Label「JPEG/WebP品質:」+ NumericUpDown（初期値95、範囲1〜100）+ Label「％」
5. DataGridView — 6列構成（後述）
6. Label「出力フォルダ名:」+ TextBox（編集可、初期値は空）
7. Label「出力先:」+ TextBox（読み取り専用）+ Button「参照」
8. Button「実行」— 下部（Phase 2Aでは**グレーアウト**）

**品質NumericUpDownのグレーアウト制御：**
- フォルダ読み込み後、対象ファイルにJPEG/WebPが1件もない場合 → グレーアウト
- JPEG/WebPが1件でもある場合 → 有効
- グレーアウト時、品質ラベルの下に注釈「※対象にJPEG/WebPファイルがありません」をLabel（ForeColor = Gray、Font = メイリオ 8pt）で表示

#### 3. DataGridView の列定義

| # | 列名 | 型 | 幅 | 備考 |
|---|------|----|----|------|
| 0 | （チェック） | DataGridViewCheckBoxColumn | 30 | 初期値true。ヘッダーテキストなし |
| 1 | ファイル名 | DataGridViewTextBoxColumn | 200 | ReadOnly |
| 2 | 形式 | DataGridViewTextBoxColumn | 60 | 「JPEG」「PNG」「WebP」と表示。ReadOnly |
| 3 | サイズ(KB) | DataGridViewTextBoxColumn | 80 | 右揃え、小数点以下なし。ReadOnly |
| 4 | 解像度 | DataGridViewTextBoxColumn | 100 | 「W × H」形式。ReadOnly |
| 5 | 推定後サイズ | DataGridViewTextBoxColumn | 100 | Phase 2Aでは空欄（Phase 2Bで計算）。ReadOnly |

- DataGridView.SelectionMode = FullRowSelect
- DataGridView.AllowUserToAddRows = false
- DataGridView.AllowUserToDeleteRows = false
- DataGridView.RowHeadersVisible = false

#### 4. ImageInfoReader クラスの新規作成

**ファイル：** `ImageResize/ImageInfoReader.cs`

**責務：** 画像ファイルの情報（形式、サイズ、解像度）を読み取る

```csharp
namespace DesktopKit.ImageResize
{
    /// <summary>
    /// 画像ファイルの情報を保持するレコード
    /// </summary>
    public record ImageFileInfo(
        string FileName,        // ファイル名
        string FullPath,        // フルパス
        string Format,          // "JPEG" / "PNG" / "WebP"
        long FileSizeBytes,     // ファイルサイズ（バイト）
        int Width,              // 横幅（ピクセル）
        int Height              // 縦幅（ピクセル）
    );

    /// <summary>
    /// 画像ファイルの情報を読み取るクラス
    /// </summary>
    public static class ImageInfoReader
    {
        /// <summary>
        /// 指定フォルダ内の対象画像ファイルの情報を取得する
        /// サブフォルダは走査しない（直下のみ）
        /// </summary>
        /// <param name="folderPath">対象フォルダのパス</param>
        /// <param name="includeJpeg">JPEGを含むか</param>
        /// <param name="includePng">PNGを含むか</param>
        /// <param name="includeWebp">WebPを含むか</param>
        /// <returns>画像情報のリスト</returns>
        public static List<ImageFileInfo> ReadFolder(
            string folderPath,
            bool includeJpeg,
            bool includePng,
            bool includeWebp)
        {
            // 実装内容：
            // 1. 対象拡張子のリストを構築（チェックボックスの状態に基づく）
            //    JPEG: .jpg, .jpeg
            //    PNG: .png
            //    WebP: .webp
            // 2. Directory.GetFiles でフォルダ直下のファイルを取得
            // 3. 各ファイルについてImageSharpの Image.Identify() で解像度を取得
            //    ※ Image.Load() ではなく Identify() を使うこと（メモリ効率）
            // 4. FileInfo でファイルサイズを取得
            // 5. 拡張子から形式名（"JPEG"/"PNG"/"WebP"）を判定
            // 6. ImageFileInfo のリストとして返す
        }
    }
}
```

**重要：** `Image.Identify()` を使用すること。`Image.Load()` はピクセルデータまで読み込むため、一覧表示の段階ではメモリの無駄遣いになる。

#### 5. FileAlertChecker クラスの新規作成

**ファイル：** `ImageResize/FileAlertChecker.cs`

```csharp
namespace DesktopKit.ImageResize
{
    /// <summary>
    /// ファイルのアラート判定レベル
    /// </summary>
    public enum AlertLevel
    {
        None,       // 正常
        Warning,    // 警告（将来用の予備）
        Danger,     // 危険（巨大ファイル）
        Skip        // 処理対象外（空ファイル）
    }

    /// <summary>
    /// ファイルのアラート判定を行うクラス
    /// </summary>
    public static class FileAlertChecker
    {
        private const long DangerThresholdBytes = 5 * 1024 * 1024; // 5MB

        /// <summary>
        /// ファイル情報からアラートレベルを判定する
        /// </summary>
        public static AlertLevel Check(ImageFileInfo info)
        {
            if (info.FileSizeBytes == 0) return AlertLevel.Skip;
            if (info.FileSizeBytes >= DangerThresholdBytes) return AlertLevel.Danger;
            return AlertLevel.None;
        }
    }
}
```

#### 6. MainForm.cs のイベントハンドラ実装

**「フォルダを選択」ボタン：**
1. FolderBrowserDialogを開く
2. 選択されたパスをTextBoxに表示
3. ImageInfoReader.ReadFolder() を呼び出す
4. 結果をDataGridViewに表示する
5. 各行にFileAlertCheckerでアラート判定し、行の背景色を設定
   - AlertLevel.Danger → Color.FromArgb(255, 200, 200)（薄い赤）
   - AlertLevel.Skip → Color.FromArgb(220, 220, 220)（薄いグレー）
   - AlertLevel.None → デフォルト
6. JPEG/WebPが1件もなければ品質NumericUpDownをグレーアウト
7. 出力フォルダ名のデフォルト値を生成して設定
   - 形式：「[元フォルダ名]_[倍率]pct_[yyyyMMdd]」
   - 例：「AI作品_50pct_20260330」
8. 出力先TextBoxに元フォルダの親フォルダのパスを設定
9. ステータスバーに「○件の画像ファイルが見つかりました」と表示

**形式チェックボックスのCheckedChanged：**
- フォルダが選択済みの場合、ReadFolderを再実行して一覧を更新する

**倍率NumericUpDownのValueChanged：**
- 出力フォルダ名のデフォルト値を倍率に合わせて更新する
- ただし、利用者がフォルダ名を手動編集済みの場合は更新しない（フラグで管理）

**出力フォルダ名TextBoxのTextChanged：**
- 手動編集フラグをONにする

**「参照」ボタン：**
- FolderBrowserDialogを開く
- 選択されたパスを出力先TextBoxに設定する

### Phase 2A 完了条件

1. フォルダを指定すると対象画像ファイルの一覧が表示されること
2. JPEG、PNG、WebPの3形式が正しく読み取れること
3. 形式チェックボックスのON/OFFでフィルタが機能すること
4. 個別チェックボックスで除外指定ができること
5. 5MB以上のファイルが赤背景で表示されること
6. 0バイトのファイルがグレー背景で表示されること
7. JPEG/WebPがない場合に品質欄がグレーアウトすること
8. 出力フォルダ名がデフォルト生成されること
9. 出力先の「参照」ボタンで別フォルダを選択できること
10. 「実行」ボタンがグレーアウト状態であること
11. ビルドエラー0で通ること

### Phase 2A 完了時の報告

- 追加・変更したファイルの一覧
- ビルド結果
- 指示書との差異がある場合はその理由
- ImageSharpのバージョン（実際にインストールされたもの）

---

## Phase 2B：リサイズ処理 + 出力

### 目的

Phase 2Aで表示された一覧に対し、リサイズ処理を実行し、並列フォルダに複製保存する。

### 作業内容

#### 1. ResizeCondition 基底クラスの作成

**ファイル：** `ImageResize/Conditions/ResizeCondition.cs`

```csharp
namespace DesktopKit.ImageResize.Conditions
{
    /// <summary>
    /// リサイズ条件の基底クラス。
    /// 将来の拡張（WidthCondition, HeightCondition等）に備えた設計。
    /// </summary>
    public abstract class ResizeCondition
    {
        /// <summary>
        /// 条件の表示名
        /// </summary>
        public abstract string DisplayName { get; }

        /// <summary>
        /// リサイズ後のサイズを計算する
        /// </summary>
        /// <param name="originalWidth">元の横幅</param>
        /// <param name="originalHeight">元の縦幅</param>
        /// <returns>リサイズ後の (Width, Height)。拡大になる場合は null を返す</returns>
        public abstract (int Width, int Height)? Calculate(int originalWidth, int originalHeight);
    }
}
```

#### 2. PercentCondition クラスの作成

**ファイル：** `ImageResize/Conditions/PercentCondition.cs`

```csharp
namespace DesktopKit.ImageResize.Conditions
{
    /// <summary>
    /// ％指定によるリサイズ条件
    /// </summary>
    public class PercentCondition : ResizeCondition
    {
        private readonly int _percent;

        public PercentCondition(int percent)
        {
            if (percent < 1 || percent > 100)
                throw new ArgumentOutOfRangeException(nameof(percent), "1〜100の範囲で指定してください");
            _percent = percent;
        }

        public override string DisplayName => $"{_percent}%リサイズ";

        public override (int Width, int Height)? Calculate(int originalWidth, int originalHeight)
        {
            // 100%は実質コピーなのでスキップ（拡大禁止ルールの一環）
            if (_percent >= 100) return null;

            int newWidth = (int)Math.Round(originalWidth * _percent / 100.0);
            int newHeight = (int)Math.Round(originalHeight * _percent / 100.0);

            // 最小1ピクセルを保証
            newWidth = Math.Max(1, newWidth);
            newHeight = Math.Max(1, newHeight);

            return (newWidth, newHeight);
        }
    }
}
```

#### 3. QualitySettings クラスの作成

**ファイル：** `ImageResize/QualitySettings.cs`

```csharp
namespace DesktopKit.ImageResize
{
    /// <summary>
    /// JPEG/WebP保存時の品質設定を管理するクラス
    /// </summary>
    public class QualitySettings
    {
        /// <summary>
        /// 品質値（1〜100）
        /// </summary>
        public int Quality { get; }

        public QualitySettings(int quality)
        {
            if (quality < 1 || quality > 100)
                throw new ArgumentOutOfRangeException(nameof(quality), "1〜100の範囲で指定してください");
            Quality = quality;
        }

        /// <summary>
        /// 指定された形式に品質パラメータが適用可能かを判定する
        /// </summary>
        public static bool IsApplicable(string format)
        {
            return format == "JPEG" || format == "WebP";
        }
    }
}
```

#### 4. ImageProcessor クラスの作成

**ファイル：** `ImageResize/ImageProcessor.cs`

```csharp
namespace DesktopKit.ImageResize
{
    /// <summary>
    /// 画像のリサイズ処理結果
    /// </summary>
    public record ProcessResult(
        string SourcePath,      // 元ファイルパス
        string OutputPath,      // 出力先パス（成功時）
        bool Success,           // 成功/失敗
        bool Skipped,           // スキップされたか（拡大禁止等）
        string Message          // 結果メッセージ
    );

    /// <summary>
    /// ImageSharpを使用した画像リサイズ処理
    /// </summary>
    public class ImageProcessor
    {
        // 実装内容：
        // 
        // public ProcessResult Process(
        //     ImageFileInfo fileInfo,
        //     ResizeCondition condition,
        //     QualitySettings quality,
        //     string outputFolderPath)
        //
        // 処理フロー：
        // 1. condition.Calculate() でリサイズ後サイズを算出
        // 2. null が返った場合はスキップ（Skipped = true）
        // 3. Image.Load() で画像を読み込む
        // 4. image.Mutate(x => x.Resize(newWidth, newHeight, KnownResamplers.Lanczos3))
        // 5. 形式に応じたエンコーダーで保存：
        //    - JPEG: new JpegEncoder { Quality = quality.Quality }
        //    - PNG:  new PngEncoder()（品質パラメータなし）
        //    - WebP: new WebpEncoder { Quality = quality.Quality }
        // 6. 出力パスは outputFolderPath + 元のファイル名（拡張子も同じ）
        // 7. ProcessResult を返す
        //
        // 例外処理：
        // - 読み込み失敗 → Success = false, Message にエラー内容
        // - 書き込み失敗 → Success = false, Message にエラー内容
        // - 例外は呼び出し元に伝播させない（ProcessResultで返す）
    }
}
```

**リサンプリング：** `KnownResamplers.Lanczos3` を使用すること（高品質リサイズ）。

#### 5. OutputFolderManager クラスの作成

**ファイル：** `ImageResize/OutputFolderManager.cs`

```csharp
namespace DesktopKit.ImageResize
{
    /// <summary>
    /// 出力先フォルダの生成・管理を行うクラス
    /// </summary>
    public static class OutputFolderManager
    {
        /// <summary>
        /// デフォルトの出力フォルダ名を生成する
        /// 形式：[元フォルダ名]_[倍率]pct_[yyyyMMdd]
        /// </summary>
        public static string GenerateDefaultName(string sourceFolderName, int percent)
        {
            string date = DateTime.Now.ToString("yyyyMMdd");
            return $"{sourceFolderName}_{percent}pct_{date}";
        }

        /// <summary>
        /// 出力先のフルパスを構築する
        /// </summary>
        /// <param name="parentPath">出力先の親フォルダパス</param>
        /// <param name="folderName">出力フォルダ名</param>
        /// <returns>出力先のフルパス</returns>
        public static string BuildOutputPath(string parentPath, string folderName)
        {
            return Path.Combine(parentPath, folderName);
        }

        /// <summary>
        /// 出力フォルダを作成する。既に存在する場合はそのまま使用する。
        /// </summary>
        /// <returns>作成（または既存）のフォルダパス</returns>
        public static string EnsureFolder(string fullPath)
        {
            Directory.CreateDirectory(fullPath);
            return fullPath;
        }
    }
}
```

#### 6. MainForm.cs の「実行」ボタン実装

**「実行」ボタンの有効化条件：**
- フォルダが選択済み
- 対象ファイルが1件以上ある（チェックONのファイルが1件以上）

**「実行」ボタンのクリック処理：**

1. 確認ダイアログを表示
   - 「[N]件の画像をリサイズします。よろしいですか？」
   - 出力先フォルダのパスも表示する
2. 出力フォルダを作成（OutputFolderManager.EnsureFolder）
3. チェックONの各ファイルに対してImageProcessor.Process()を実行
4. 処理中はProgressBarまたはステータスバーで進捗を表示
   - 「処理中... [N/M]」
5. 全件完了後、ステータスバーに結果を表示
   - 「完了：成功 N件 / スキップ N件 / 失敗 N件」
6. Process.Start("explorer.exe", outputFolderPath) で出力フォルダを開く

**UIスレッドのブロック防止：**
- リサイズ処理はasync/awaitまたはBackgroundWorkerで非同期実行すること
- 処理中は「実行」ボタンとフォルダ選択を無効化すること

#### 7. DataGridViewの「推定後サイズ」列の実装

Phase 2Aでは空欄にしていた「推定後サイズ」列を実装する：
- 倍率・品質の値が変更されたタイミングで再計算する
- 推定ロジック：元サイズ × (倍率/100)^2 をベースに、形式ごとの圧縮率を加味する
  - JPEG/WebP：品質パラメータを反映した概算
  - PNG：リサイズ後のピクセル数に基づく概算
- あくまで概算であり、正確である必要はない。目安として表示する

### Phase 2B で追加・変更するファイル

| ファイル | 操作 |
|---------|------|
| ImageResize/Conditions/ResizeCondition.cs | 新規作成 |
| ImageResize/Conditions/PercentCondition.cs | 新規作成 |
| ImageResize/QualitySettings.cs | 新規作成 |
| ImageResize/ImageProcessor.cs | 新規作成 |
| ImageResize/OutputFolderManager.cs | 新規作成 |
| ImageResize/MainForm.cs | 実行ボタンの有効化・イベントハンドラ実装 |

### Phase 2B 完了条件

1. JPEG、PNG、WebPの3形式がすべてリサイズできること
2. 元ファイルが変更されないこと（複製保存の確認）
3. 出力フォルダが元フォルダと並列に作成されること
4. 出力フォルダ名が利用者の指定通りであること
5. JPEG/WebP品質パラメータが保存画像に反映されていること
6. 100%指定でスキップされること
7. 処理完了後にエクスプローラーで出力フォルダが開くこと
8. 処理中にUIがフリーズしないこと（非同期処理の確認）
9. 確認ダイアログが表示されること
10. ステータスバーに処理結果が表示されること
11. ビルドエラー0で通ること

### Phase 2B 完了時の報告

- 追加・変更したファイルの一覧
- ビルド結果
- 指示書との差異がある場合はその理由
- テスト用の画像で実際にリサイズした結果（形式ごとのファイルサイズ比較）

---

## ClaudeCodeへの指示事項

### 把握レポート

各Phaseの開始前に、この制作指示書の該当セクションを読み、把握レポートを出力すること。「把握しました」で進ませない。具体的に何を作るか、どのクラスを作るか、どのような動作になるかを自分の言葉で説明すること。

### 差異の報告

制作指示書と異なる実装をした場合、その理由を必ず報告すること。技術的に妥当な判断であれば承認する。理由なき差異は修正を求める。

### 品質基準

- ビルドエラー0が前提
- UIの文字列はすべて日本語（ボタン、メニュー、メッセージ、ステータスバー）
- 例外処理を適切に行い、利用者にわかりやすいエラーメッセージを表示すること
- コードにはクラス・メソッドレベルのXMLコメントを付与すること

### 制作仕様書の参照

制作仕様書（`_works/ImageResize_制作仕様書_v2_0.md`）を必ず読み、本制作指示書と合わせて理解すること。指示書に記載のない判断が必要な場合は、仕様書の設計方針に従うこと。
