using DesktopKit.Common;
using System.Windows.Forms;
using System.Drawing;

namespace DesktopKit.ImageResize
{
    /// <summary>
    /// ImageResize（画像一括リサイズ）のメインフォーム。
    /// </summary>
    public class MainForm : BaseForm
    {
        private Button btnSelectFolder = null!;
        private TextBox txtFolderPath = null!;
        private CheckBox chkJpg = null!;
        private CheckBox chkPng = null!;
        private CheckBox chkBmp = null!;
        private Label lblScale = null!;
        private NumericUpDown nudScale = null!;
        private Label lblPercent = null!;
        private DataGridView dgvFiles = null!;
        private Button btnExecute = null!;

        /// <summary>
        /// MainFormのコンストラクタ。
        /// </summary>
        public MainForm()
        {
            ComponentName = "ImageResize";
            InitializeControls();
        }

        private void InitializeControls()
        {
            // --- 上部パネル: フォルダ選択 + 対象形式 + 倍率 ---
            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 100,
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
                Size = new Size(620, 23),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            var lblFormat = new Label
            {
                Text = "対象形式:",
                Location = new Point(10, 45),
                AutoSize = true
            };

            chkJpg = new CheckBox
            {
                Text = ".jpg",
                Location = new Point(90, 43),
                AutoSize = true,
                Checked = true
            };

            chkPng = new CheckBox
            {
                Text = ".png",
                Location = new Point(155, 43),
                AutoSize = true,
                Checked = true
            };

            chkBmp = new CheckBox
            {
                Text = ".bmp",
                Location = new Point(220, 43),
                AutoSize = true,
                Checked = true
            };

            lblScale = new Label
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

            lblPercent = new Label
            {
                Text = "％",
                Location = new Point(185, 73),
                AutoSize = true
            };

            topPanel.Controls.AddRange(new Control[]
            {
                btnSelectFolder, txtFolderPath,
                lblFormat, chkJpg, chkPng, chkBmp,
                lblScale, nudScale, lblPercent
            });

            // --- 中央: DataGridView ---
            dgvFiles = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgvFiles.Columns.Add("FileName", "ファイル名");
            dgvFiles.Columns.Add("Size", "サイズ(KB)");
            dgvFiles.Columns.Add("Resolution", "解像度");
            dgvFiles.Columns.Add("EstimatedSize", "推定後サイズ");

            // --- 下部パネル: 実行ボタン ---
            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 45,
                Padding = new Padding(10, 5, 10, 5)
            };

            btnExecute = new Button
            {
                Text = "実行",
                Location = new Point(10, 8),
                Size = new Size(100, 28)
            };

            bottomPanel.Controls.Add(btnExecute);

            // --- フォームに追加 ---
            Controls.Add(dgvFiles);
            Controls.Add(topPanel);
            Controls.Add(bottomPanel);
        }
    }
}
