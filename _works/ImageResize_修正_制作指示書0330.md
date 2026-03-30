# ImageResize 修正制作指示書

**対象アプリ：** A4_ImageResize_app  
**対象ファイル：** `ImageResize/MainForm.cs`（主）、必要に応じ関連クラス  
**修正件数：** 3件  
**配置先：** `A4_ImageResize_app/_works/`

---

## 把握確認（ClaudeCodeへ）

作業開始前に以下を出力すること。

1. MainForm.cs の入力フィールド（フォルダ選択枠・出力フォルダ名枠・出力先枠）の現在の `Width`・`Anchor`・`Padding` 設定
2. リスト表示に使っているコントロールの種類（ListView / ListBox / DataGridView 等）とカラム構成
3. ファイル収集時の `SearchOption` の現在値
4. 階層表示に使っているコントロールの種類と、現在のファイル名表示方式

---

## 修正 1：入力フィールドの右余白確保

### 対象コントロール
- フォルダ選択枠（TextBox）
- 出力フォルダ名枠（TextBox）
- 出力先枠（TextBox）

### 修正内容
各 TextBox の右端とフォームまたは親コンテナの右端との間に **8px 以上の余白** を確保する。

### 実装方針
```
// Anchor を Left, Top, Right に設定している場合：
// フォームの右マージンを考慮して Width か Right プロパティを調整する
// 例：フォーム幅 800px、右端ボタンまでの余白を確保するなら
textBoxFolder.Width = this.ClientSize.Width - textBoxFolder.Left - 8;

// または Designer 側で右側に Padding を加える
// Panel や TableLayoutPanel を使っている場合は Padding.Right = 8 を設定
```

### 確認基準
- リサイズ時も右余白が維持されること（Anchor: Right が設定されていること）
- 3つの入力フィールドで余白量が統一されていること

---

## 修正 2：リスト右端の空白をファイル名で埋める

### 前提確認
ListView を使用している場合の手順。ListBox の場合は後述。

### ListView の場合
ファイル名カラムの幅を動的にリサイズし、グレー（未使用領域）が出ないようにする。

```csharp
// ファイルリストへの追加・更新後に呼び出す
private void AdjustFileNameColumnWidth()
{
    if (listViewFiles.Columns.Count == 0) return;

    // ファイル名カラム（index 0）をコンテンツに合わせてリサイズ
    listViewFiles.Columns[0].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);

    // ただし、リスト全体幅より小さい場合はリスト幅いっぱいに広げる
    int otherColumnsWidth = 0;
    for (int i = 1; i < listViewFiles.Columns.Count; i++)
        otherColumnsWidth += listViewFiles.Columns[i].Width;

    int scrollBarWidth = SystemInformation.VerticalScrollBarWidth;
    int availableWidth = listViewFiles.ClientSize.Width - otherColumnsWidth - scrollBarWidth;

    if (listViewFiles.Columns[0].Width < availableWidth)
        listViewFiles.Columns[0].Width = availableWidth;
}
```

呼び出しタイミング：ファイルリスト更新後、およびリストコントロールのリサイズイベント時。

```csharp
listViewFiles.Resize += (s, e) => AdjustFileNameColumnWidth();
```

### ListBox の場合
ListBox は横スクロールが出る場合があるため、`HorizontalScrollbar = false` かつ `DrawMode = DrawMode.OwnerDrawFixed` でカスタム描画するか、ListView への切り替えを検討する（要確認・相談）。

### 確認基準
- ファイルを読み込んだ後、リスト右端にグレー空白が出ないこと
- ウィンドウリサイズ後も同様であること

---

## 修正 3：サブフォルダ再帰検索 ＋ 階層表示スタイル

### 現状の問題
指定フォルダの直下ファイルのみを取得している（`SearchOption.TopDirectoryOnly`）。

### 修正後の動作
指定フォルダ配下の**全階層のファイル**を取得し、以下のスタイルで表示する。

### 階層表示スタイル仕様（参照画像：階層表示.JPG）

| 行種別 | 表示内容 | 背景色 | インデント |
|---|---|---|---|
| ルートフォルダ行 | `フォルダ名/` | グレー（`#E8E8E8` 相当） | なし（0px） |
| 直下ファイル行 | `ファイル名` | 白 | 1段（16px） |
| サブフォルダ行 | `サブフォルダ名/` | グレー | 1段（16px） |
| サブフォルダ内ファイル | `ファイル名` | 白 | 2段（32px） |
| 孫フォルダ行 | `孫フォルダ名/` | グレー | 2段（32px） |
| 孫フォルダ内ファイル | `ファイル名` | 白 | 3段（48px） |

- インデント単位：**16px × 深さ**
- フォルダ行：チェックボックスあり、サイズ列・更新日時列は空欄
- ファイル行：チェックボックスあり、サイズ・更新日時を表示

### 実装方針

**ファイル収集ロジックの変更（FileAlertChecker.cs または MainForm.cs）**

```csharp
// 変更前
var files = Directory.GetFiles(folderPath, "*", SearchOption.TopDirectoryOnly);

// 変更後：再帰収集 + 階層情報を保持
public class FileEntry
{
    public string FullPath { get; set; }
    public string FolderPath { get; set; }   // 親フォルダのフルパス
    public int Depth { get; set; }            // 階層の深さ（ルート=0）
    public bool IsFolder { get; set; }        // フォルダ行フラグ
    public string DisplayName { get; set; }  // 表示名（ファイル名 or フォルダ名/）
}

private List<FileEntry> CollectEntriesRecursive(string rootPath, int depth = 0)
{
    var entries = new List<FileEntry>();

    // フォルダ自身の行を追加
    entries.Add(new FileEntry
    {
        FullPath = rootPath,
        Depth = depth,
        IsFolder = true,
        DisplayName = Path.GetFileName(rootPath) + "/"
    });

    // 直下ファイルを追加
    foreach (var file in Directory.GetFiles(rootPath).OrderBy(f => f))
    {
        entries.Add(new FileEntry
        {
            FullPath = file,
            FolderPath = rootPath,
            Depth = depth,
            IsFolder = false,
            DisplayName = Path.GetFileName(file)
        });
    }

    // サブフォルダを再帰処理
    foreach (var dir in Directory.GetDirectories(rootPath).OrderBy(d => d))
    {
        entries.AddRange(CollectEntriesRecursive(dir, depth + 1));
    }

    return entries;
}
```

**ListView への描画（OwnerDraw または SubItem 設定）**

```csharp
private void PopulateListView(List<FileEntry> entries)
{
    listViewFiles.Items.Clear();
    foreach (var entry in entries)
    {
        var item = new ListViewItem(entry.DisplayName);
        item.IndentCount = entry.Depth; // ListView の IndentCount を使用
        item.Tag = entry;

        if (entry.IsFolder)
        {
            item.BackColor = Color.FromArgb(232, 232, 232); // グレー背景
            item.SubItems.Add(""); // サイズ列
            item.SubItems.Add(""); // 更新日時列
        }
        else
        {
            var fi = new FileInfo(entry.FullPath);
            item.SubItems.Add((fi.Length / 1024).ToString()); // KB
            item.SubItems.Add(fi.LastWriteTime.ToString("MM/dd HH:mm"));
        }

        listViewFiles.Items.Add(item);
    }

    AdjustFileNameColumnWidth(); // 修正2と連動
}
```

> **注意：** `IndentCount` はファイルアイコン付きの ListView（SmallImageList設定時）でのみ有効。アイコンなしの場合は `OwnerDrawFixed` + `DrawItem` イベントで左パディングを手動描画する必要がある。現在の実装を確認の上、適切な方法を選択すること。差異が生じた場合は理由を報告すること。

### 確認基準
- フォルダ選択後、全階層のファイルが収集されること
- フォルダ行がグレー背景で表示されること
- 階層に応じてインデントが段階的に付くこと
- チェックボックスのON/OFFが動作すること

---

## ビルド確認

修正完了後、以下を実行してエラーがないことを確認すること。

```bash
cd A4_ImageResize_app
build.bat
```

エラーが出た場合は内容を報告し、修正の上で再ビルドすること。

---

## 差異報告ルール

指示書と異なる実装を行った場合は、必ず以下の形式で報告すること。

```
【差異報告】
修正番号：（1 / 2 / 3）
指示内容：（指示書の内容）
実装内容：（実際に実装した内容）
理由：（技術的な理由）
```
