using DesktopKit.Common;
using DesktopKit.ImageResize.Conditions;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;

namespace DesktopKit.ImageResize
{
    /// <summary>
    /// ImageResize（画像一括リサイズ）のメインフォーム。
    /// フォルダ指定 → 形式フィルタ → 一覧表示 → 条件設定 → リサイズ実行 の流れを制御する。
    /// </summary>
    public class MainForm : BaseForm
    {
        // --- フォルダ選択 ---
        private Button btnSelectFolder = null!;
        private TextBox txtFolderPath = null!;

        // --- 形式フィルタ ---
        private CheckBox chkJpg = null!;
        private CheckBox chkPng = null!;
        private CheckBox chkWebp = null!;

        // --- リサイズ倍率 ---
        private NumericUpDown nudScale = null!;

        // --- JPEG/WebP品質 ---
        private NumericUpDown nudQuality = null!;
        private Label lblQualityNote = null!;

        // --- ファイル一覧 ---
        private DataGridView dgvFiles = null!;

        // --- 出力設定 ---
        private Label lblFileCount = null!;
        private TextBox txtOutputName = null!;

        // --- 実行 ---
        private Button btnExecute = null!;

        // --- 内部状態 ---
        private string? _selectedFolderPath;
        private List<ImageFileInfo> _currentFiles = new();
        private List<DisplayEntry> _displayEntries = new();
        private bool _outputNameManuallyEdited;
        private bool _isUpdatingOutputName;
        private int _totalFileCount;

        /// <summary>
        /// MainFormのコンストラクタ。
        /// </summary>
        public MainForm()
        {
            ComponentName = "ImageResize";
            InitializeControls();
            WireEvents();
        }

        /// <summary>
        /// UIコントロールを初期化・配置する。
        /// </summary>
        private void InitializeControls()
        {
            // --- 上部パネル: フォルダ選択 + 対象形式 + 倍率 + 品質 ---
            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 130,
                Padding = new Padding(10, 10, 10, 5)
            };

            btnSelectFolder = new Button
            {
                Text = "フォルダを選択",
                Location = new Point(10, 8),
                Size = new Size(120, 28)
            };

            txtFolderPath = new TextBox
            {
                ReadOnly = true,
                Location = new Point(140, 10),
                Size = new Size(648, 23),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                AllowDrop = true
            };

            var lblFormat = new Label
            {
                Text = "対象形式:",
                Location = new Point(10, 45),
                AutoSize = true
            };

            chkJpg = new CheckBox
            {
                Text = ".jpg/.jpeg",
                Location = new Point(90, 43),
                AutoSize = true,
                Checked = true
            };

            chkPng = new CheckBox
            {
                Text = ".png",
                Location = new Point(185, 43),
                AutoSize = true,
                Checked = true
            };

            chkWebp = new CheckBox
            {
                Text = ".webp",
                Location = new Point(250, 43),
                AutoSize = true,
                Checked = true
            };

            var lblScale = new Label
            {
                Text = "リサイズ倍率:",
                Location = new Point(10, 73),
                AutoSize = true
            };

            nudScale = new NumericUpDown
            {
                Value = 50,
                Minimum = 1,
                Maximum = 100,
                Location = new Point(120, 70),
                Size = new Size(60, 23)
            };

            var lblPercent = new Label
            {
                Text = "％",
                Location = new Point(185, 73),
                AutoSize = true
            };

            var lblQuality = new Label
            {
                Text = "JPEG/WebP品質:",
                Location = new Point(10, 100),
                AutoSize = true
            };

            nudQuality = new NumericUpDown
            {
                Value = 95,
                Minimum = 1,
                Maximum = 100,
                Location = new Point(140, 97),
                Size = new Size(60, 23)
            };

            var lblQualityPercent = new Label
            {
                Text = "％",
                Location = new Point(205, 100),
                AutoSize = true
            };

            lblQualityNote = new Label
            {
                Text = "※対象にJPEG/WebPファイルがありません",
                Location = new Point(240, 100),
                AutoSize = true,
                ForeColor = Color.Gray,
                Font = new Font("メイリオ", 8f),
                Visible = false
            };

            topPanel.Controls.AddRange(new Control[]
            {
                btnSelectFolder, txtFolderPath,
                lblFormat, chkJpg, chkPng, chkWebp,
                lblScale, nudScale, lblPercent,
                lblQuality, nudQuality, lblQualityPercent, lblQualityNote
            });

            // --- 中央: DataGridView ---
            dgvFiles = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false
            };

            var colCheck = new DataGridViewCheckBoxColumn
            {
                Name = "Check",
                HeaderText = "",
                Width = 30
            };
            var colFileName = new DataGridViewTextBoxColumn
            {
                Name = "FileName",
                HeaderText = "ファイル名",
                MinimumWidth = 200,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                ReadOnly = true
            };
            var colFormat = new DataGridViewTextBoxColumn
            {
                Name = "Format",
                HeaderText = "形式",
                Width = 60,
                ReadOnly = true
            };
            var colSize = new DataGridViewTextBoxColumn
            {
                Name = "Size",
                HeaderText = "サイズ(KB)",
                Width = 80,
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight }
            };
            var colResolution = new DataGridViewTextBoxColumn
            {
                Name = "Resolution",
                HeaderText = "解像度",
                Width = 100,
                ReadOnly = true
            };
            var colEstimated = new DataGridViewTextBoxColumn
            {
                Name = "EstimatedSize",
                HeaderText = "推定後サイズ",
                Width = 100,
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight }
            };

            dgvFiles.Columns.AddRange(new DataGridViewColumn[]
            {
                colCheck, colFileName, colFormat, colSize, colResolution, colEstimated
            });

            // --- 下部パネル: ファイル件数 + 出力設定 + 実行ボタン ---
            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 90,
                Padding = new Padding(10, 5, 10, 5)
            };

            lblFileCount = new Label
            {
                Text = "",
                Location = new Point(10, 5),
                AutoSize = true,
                Font = new Font("メイリオ", 9f)
            };

            var lblOutputName = new Label
            {
                Text = "出力フォルダ名:",
                Location = new Point(10, 30),
                AutoSize = true
            };

            txtOutputName = new TextBox
            {
                Location = new Point(130, 27),
                Size = new Size(658, 23),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            btnExecute = new Button
            {
                Text = "実行",
                Location = new Point(10, 57),
                Size = new Size(100, 28),
                Enabled = false
            };

            bottomPanel.Controls.AddRange(new Control[]
            {
                lblFileCount,
                lblOutputName, txtOutputName,
                btnExecute
            });

            // --- フォームに追加（DockStyle.Fill は最後に追加） ---
            Controls.Add(dgvFiles);
            Controls.Add(topPanel);
            Controls.Add(bottomPanel);
        }

        /// <summary>
        /// イベントハンドラを接続する。
        /// </summary>
        private void WireEvents()
        {
            btnSelectFolder.Click += BtnSelectFolder_Click;
            txtFolderPath.DragEnter += TxtFolderPath_DragEnter;
            txtFolderPath.DragDrop += TxtFolderPath_DragDrop;
            chkJpg.CheckedChanged += FormatFilter_Changed;
            chkPng.CheckedChanged += FormatFilter_Changed;
            chkWebp.CheckedChanged += FormatFilter_Changed;
            nudScale.ValueChanged += NudScale_ValueChanged;
            nudQuality.ValueChanged += NudQuality_ValueChanged;
            txtOutputName.TextChanged += TxtOutputName_TextChanged;
            btnExecute.Click += BtnExecute_Click;
            dgvFiles.CellValueChanged += DgvFiles_CellValueChanged;
            dgvFiles.CurrentCellDirtyStateChanged += DgvFiles_CurrentCellDirtyStateChanged;
            dgvFiles.CellPainting += DgvFiles_CellPainting;
        }

        /// <summary>
        /// 「フォルダを選択」ボタンのクリックイベント。
        /// </summary>
        private void BtnSelectFolder_Click(object? sender, EventArgs e)
        {
            var path = FileDialogHelper.SelectFolder("画像フォルダを選択してください");
            if (path == null) return;

            ApplySelectedFolder(path);
        }

        /// <summary>
        /// ドラッグ中のカーソルがTextBox上に入った時のイベント。
        /// </summary>
        private void TxtFolderPath_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                var paths = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (paths?.Length > 0 && Directory.Exists(paths[0]))
                {
                    e.Effect = DragDropEffects.Copy;
                    return;
                }
            }
            e.Effect = DragDropEffects.None;
        }

        /// <summary>
        /// フォルダがドロップされた時のイベント。
        /// </summary>
        private void TxtFolderPath_DragDrop(object? sender, DragEventArgs e)
        {
            var paths = e.Data?.GetData(DataFormats.FileDrop) as string[];
            if (paths?.Length > 0 && Directory.Exists(paths[0]))
            {
                ApplySelectedFolder(paths[0]);
            }
        }

        /// <summary>
        /// 選択されたフォルダを適用する共通処理。
        /// </summary>
        private void ApplySelectedFolder(string path)
        {
            _selectedFolderPath = path;
            txtFolderPath.Text = path;
            _outputNameManuallyEdited = false;

            LoadFileList();
            UpdateOutputDefaults();
        }

        /// <summary>
        /// 形式チェックボックスの変更イベント。
        /// </summary>
        private void FormatFilter_Changed(object? sender, EventArgs e)
        {
            if (_selectedFolderPath != null)
            {
                LoadFileList();
                UpdateExecuteButtonState();
            }
        }

        /// <summary>
        /// 倍率NumericUpDownの値変更イベント。
        /// </summary>
        private void NudScale_ValueChanged(object? sender, EventArgs e)
        {
            if (!_outputNameManuallyEdited && _selectedFolderPath != null)
            {
                UpdateOutputFolderName();
            }
            UpdateEstimatedSizes();
        }

        /// <summary>
        /// 品質NumericUpDownの値変更イベント。
        /// </summary>
        private void NudQuality_ValueChanged(object? sender, EventArgs e)
        {
            UpdateEstimatedSizes();
        }

        /// <summary>
        /// 出力フォルダ名TextBoxのテキスト変更イベント。
        /// </summary>
        private void TxtOutputName_TextChanged(object? sender, EventArgs e)
        {
            if (!_isUpdatingOutputName)
            {
                _outputNameManuallyEdited = true;
            }
        }

        /// <summary>
        /// DataGridViewのチェックボックス変更を即時コミットする。
        /// </summary>
        private void DgvFiles_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
        {
            if (dgvFiles.IsCurrentCellDirty && dgvFiles.CurrentCell is DataGridViewCheckBoxCell)
            {
                dgvFiles.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        /// <summary>
        /// DataGridViewのセル値変更イベント。
        /// フォルダ行のチェック変更時は配下エントリを連動させる。
        /// </summary>
        private void DgvFiles_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 0 && e.RowIndex >= 0 && e.RowIndex < _displayEntries.Count)
            {
                var entry = _displayEntries[e.RowIndex];
                if (entry.IsFolder)
                {
                    var folderChecked = dgvFiles.Rows[e.RowIndex].Cells["Check"].Value is true;
                    var folderDepth = entry.Depth;

                    // 次の行から走査し、このフォルダ配下（depth > folderDepth）を連動
                    for (int i = e.RowIndex + 1; i < dgvFiles.Rows.Count && i < _displayEntries.Count; i++)
                    {
                        var child = _displayEntries[i];
                        if (child.IsFolder && child.Depth <= folderDepth) break;
                        if (!child.IsFolder && child.Depth <= folderDepth) break;
                        dgvFiles.Rows[i].Cells["Check"].Value = folderChecked;
                    }
                }

                UpdateExecuteButtonState();
            }
        }

        /// <summary>
        /// 「実行」ボタンのクリックイベント。非同期でリサイズ処理を実行する。
        /// </summary>
        private async void BtnExecute_Click(object? sender, EventArgs e)
        {
            var checkedFiles = GetCheckedFiles();
            if (checkedFiles.Count == 0) return;

            // サブフォルダが存在するか判定
            bool hasSubFolders = _displayEntries.Any(
                entry => entry.IsFolder && entry.Depth > 0);

            bool preserveStructure = false;
            if (hasSubFolders)
            {
                var structureResult = MessageBox.Show(
                    "複数のフォルダが含まれています。\n\n" +
                    "「はい」→ フォルダ構造を維持して出力\n" +
                    "「いいえ」→ すべてのファイルを1つのフォルダにまとめて出力",
                    "フォルダ構造の選択",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (structureResult == DialogResult.Cancel) return;
                preserveStructure = structureResult == DialogResult.Yes;
            }

            // 出力先フォルダをダイアログで選択（デフォルト：元フォルダの親 + 出力フォルダ名）
            var defaultParentDir = _selectedFolderPath != null
                ? Path.GetDirectoryName(_selectedFolderPath) ?? _selectedFolderPath
                : "";
            var defaultFullPath = Path.Combine(defaultParentDir, txtOutputName.Text);

            var outputPath = FileDialogHelper.SelectFolder(
                "出力先フォルダを選択してください", defaultFullPath);
            if (outputPath == null) return;

            // 確認ダイアログ
            var result = MessageBox.Show(
                $"{checkedFiles.Count}件の画像をリサイズします。よろしいですか？\n\n" +
                $"出力先: {outputPath}\n" +
                $"フォルダ構造: {(hasSubFolders ? (preserveStructure ? "維持" : "まとめる") : "単一フォルダ")}",
                "確認",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Question);

            if (result != DialogResult.OK) return;

            // UI無効化
            SetProcessingState(true);

            try
            {
                OutputFolderManager.EnsureFolder(outputPath);

                var condition = new PercentCondition((int)nudScale.Value);
                var quality = new QualitySettings((int)nudQuality.Value);
                var processor = new ImageProcessor();

                int successCount = 0, skipCount = 0, failCount = 0;
                int total = checkedFiles.Count;

                for (int i = 0; i < total; i++)
                {
                    var file = checkedFiles[i];
                    StatusHelper.ShowInfo(StatusLabel, $"処理中... [{i + 1}/{total}] {file.FileName}");

                    // フォルダ構造維持時は相対パスを保持して出力
                    string fileOutputFolder;
                    if (preserveStructure && _selectedFolderPath != null)
                    {
                        var fileDir = Path.GetDirectoryName(file.FullPath) ?? "";
                        var relativePath = Path.GetRelativePath(_selectedFolderPath, fileDir);
                        fileOutputFolder = Path.Combine(outputPath, relativePath);
                        OutputFolderManager.EnsureFolder(fileOutputFolder);
                    }
                    else
                    {
                        fileOutputFolder = outputPath;
                    }

                    var processResult = await Task.Run(() =>
                        processor.Process(file, condition, quality, fileOutputFolder));

                    if (processResult.Skipped) skipCount++;
                    else if (processResult.Success) successCount++;
                    else failCount++;
                }

                // 結果表示
                StatusHelper.ShowSuccess(StatusLabel,
                    $"完了：成功 {successCount}件 / スキップ {skipCount}件 / 失敗 {failCount}件");

                // 出力フォルダをエクスプローラーで開く
                Process.Start("explorer.exe", outputPath);
            }
            catch (Exception ex)
            {
                StatusHelper.ShowError(StatusLabel, $"エラー: {ex.Message}");
                MessageBox.Show(
                    $"処理中にエラーが発生しました。\n\n{ex.Message}",
                    "エラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                SetProcessingState(false);
            }
        }

        /// <summary>
        /// 対象ファイル一覧を読み込み、DataGridViewに階層表示する。
        /// </summary>
        private void LoadFileList()
        {
            dgvFiles.Rows.Clear();
            _currentFiles.Clear();
            _displayEntries.Clear();

            if (_selectedFolderPath == null) return;

            _displayEntries = ImageInfoReader.ReadFolderRecursive(
                _selectedFolderPath,
                chkJpg.Checked,
                chkPng.Checked,
                chkWebp.Checked);

            bool hasJpegOrWebp = false;
            int fileCount = 0;

            foreach (var entry in _displayEntries)
            {
                if (entry.IsFolder)
                {
                    // フォルダ行：チェックボックスあり、サイズ等は空欄
                    int rowIndex = dgvFiles.Rows.Add(
                        true,
                        entry.DisplayName,
                        "",
                        "",
                        "",
                        ""
                    );
                    dgvFiles.Rows[rowIndex].DefaultCellStyle.BackColor = Color.FromArgb(232, 232, 232);
                    dgvFiles.Rows[rowIndex].Tag = entry;
                }
                else
                {
                    var file = entry.FileInfo!;
                    _currentFiles.Add(file);
                    fileCount++;

                    var sizeKb = file.FileSizeBytes > 0
                        ? (file.FileSizeBytes / 1024.0).ToString("N0")
                        : "0";
                    var resolution = file.Width > 0 && file.Height > 0
                        ? $"{file.Width} × {file.Height}"
                        : "";

                    int rowIndex = dgvFiles.Rows.Add(
                        true,
                        entry.DisplayName,
                        file.Format,
                        sizeKb,
                        resolution,
                        ""
                    );

                    dgvFiles.Rows[rowIndex].Tag = entry;

                    // アラート色付き表示（フォルダ行のグレーより優先されるアラートのみ）
                    var alert = FileAlertChecker.Check(file);
                    var row = dgvFiles.Rows[rowIndex];
                    switch (alert)
                    {
                        case AlertLevel.Danger:
                            row.DefaultCellStyle.BackColor = Color.FromArgb(255, 200, 200);
                            break;
                        case AlertLevel.Skip:
                            row.DefaultCellStyle.BackColor = Color.FromArgb(220, 220, 220);
                            break;
                    }

                    if (file.Format == "JPEG" || file.Format == "WebP")
                    {
                        hasJpegOrWebp = true;
                    }
                }
            }

            // 品質パラメータのグレーアウト制御
            nudQuality.Enabled = hasJpegOrWebp;
            lblQualityNote.Visible = !hasJpegOrWebp;

            StatusHelper.ShowInfo(StatusLabel, $"{fileCount}件の画像ファイルが見つかりました");
            _totalFileCount = fileCount;

            UpdateEstimatedSizes();
            UpdateExecuteButtonState();
        }

        /// <summary>
        /// ファイル名セルにインデントを付けて描画する。
        /// </summary>
        private void DgvFiles_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != dgvFiles.Columns["FileName"]!.Index)
                return;

            var entry = dgvFiles.Rows[e.RowIndex].Tag as DisplayEntry;
            if (entry == null) return;

            int indent = entry.IsFolder ? entry.Depth * 16 : (entry.Depth) * 16;

            e.Handled = true;

            // 背景を塗る
            using (var bgBrush = new SolidBrush(e.CellStyle!.BackColor))
            {
                e.Graphics!.FillRectangle(bgBrush, e.CellBounds);
            }

            // テキストを描画（インデント付き）
            var textRect = new Rectangle(
                e.CellBounds.X + indent + 4,
                e.CellBounds.Y,
                e.CellBounds.Width - indent - 4,
                e.CellBounds.Height);

            var text = e.Value?.ToString() ?? "";
            TextRenderer.DrawText(
                e.Graphics!,
                text,
                e.CellStyle.Font,
                textRect,
                e.CellStyle.ForeColor,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);

            // グリッド線を描画
            using (var pen = new Pen(dgvFiles.GridColor))
            {
                e.Graphics.DrawLine(pen,
                    e.CellBounds.Right - 1, e.CellBounds.Top,
                    e.CellBounds.Right - 1, e.CellBounds.Bottom - 1);
                e.Graphics.DrawLine(pen,
                    e.CellBounds.Left, e.CellBounds.Bottom - 1,
                    e.CellBounds.Right - 1, e.CellBounds.Bottom - 1);
            }
        }

        /// <summary>
        /// 推定後サイズ列を更新する。
        /// </summary>
        private void UpdateEstimatedSizes()
        {
            if (_displayEntries.Count == 0 || dgvFiles.Rows.Count == 0) return;

            int percent = (int)nudScale.Value;
            int quality = (int)nudQuality.Value;
            double scaleRatio = percent / 100.0;
            double areaRatio = scaleRatio * scaleRatio;

            for (int i = 0; i < dgvFiles.Rows.Count && i < _displayEntries.Count; i++)
            {
                var entry = _displayEntries[i];
                if (entry.IsFolder)
                {
                    dgvFiles.Rows[i].Cells["EstimatedSize"].Value = "";
                    continue;
                }

                var file = entry.FileInfo!;
                if (file.FileSizeBytes == 0 || percent >= 100)
                {
                    dgvFiles.Rows[i].Cells["EstimatedSize"].Value = "";
                    continue;
                }

                double estimated = file.FileSizeBytes * areaRatio;

                if (QualitySettings.IsApplicable(file.Format))
                {
                    estimated *= quality / 100.0;
                }

                var estimatedKb = (estimated / 1024.0).ToString("N0");
                dgvFiles.Rows[i].Cells["EstimatedSize"].Value = estimatedKb;
            }
        }

        /// <summary>
        /// 実行ボタンの有効/無効を更新する。
        /// </summary>
        private void UpdateExecuteButtonState()
        {
            var checkedCount = GetCheckedFiles().Count;
            btnExecute.Enabled = _selectedFolderPath != null && checkedCount > 0;

            if (_totalFileCount > 0)
                lblFileCount.Text = $"{_totalFileCount}件の画像ファイルが見つかりました — {checkedCount}件選択中";
            else
                lblFileCount.Text = "";
        }

        /// <summary>
        /// チェックONのファイル一覧を取得する（フォルダ行はスキップ）。
        /// </summary>
        private List<ImageFileInfo> GetCheckedFiles()
        {
            var checkedFiles = new List<ImageFileInfo>();

            for (int i = 0; i < dgvFiles.Rows.Count && i < _displayEntries.Count; i++)
            {
                var entry = _displayEntries[i];
                if (entry.IsFolder) continue;

                var cellValue = dgvFiles.Rows[i].Cells["Check"].Value;
                if (cellValue is true && entry.FileInfo != null)
                {
                    checkedFiles.Add(entry.FileInfo);
                }
            }

            return checkedFiles;
        }

        /// <summary>
        /// 処理中のUI状態を切り替える。
        /// </summary>
        private void SetProcessingState(bool processing)
        {
            btnExecute.Enabled = !processing;
            btnSelectFolder.Enabled = !processing;
            nudScale.Enabled = !processing;
            nudQuality.Enabled = !processing;
            chkJpg.Enabled = !processing;
            chkPng.Enabled = !processing;
            chkWebp.Enabled = !processing;
        }

        /// <summary>
        /// 出力先のデフォルト値を設定する。
        /// </summary>
        private void UpdateOutputDefaults()
        {
            if (_selectedFolderPath == null) return;

            UpdateOutputFolderName();
        }

        /// <summary>
        /// 出力フォルダ名のデフォルト値を生成してTextBoxに設定する。
        /// </summary>
        private void UpdateOutputFolderName()
        {
            if (_selectedFolderPath == null) return;

            var trimmedPath = _selectedFolderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var folderName = Path.GetFileName(trimmedPath);
            var defaultName = OutputFolderManager.GenerateDefaultName(
                folderName ?? "", (int)nudScale.Value);

            _isUpdatingOutputName = true;
            txtOutputName.Text = defaultName;
            _isUpdatingOutputName = false;
        }
    }
}
