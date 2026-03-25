using System.Drawing;
using System.Windows.Forms;

namespace DesktopKit.Common
{
    /// <summary>
    /// 全コンポーネント共通の基底フォームクラス。
    /// 統一されたウィンドウサイズ、フォント、ステータスバー、タイトル形式を提供する。
    /// </summary>
    public class BaseForm : Form
    {
        /// <summary>
        /// ステータスバーコントロール
        /// </summary>
        protected StatusStrip StatusBar { get; private set; } = null!;

        /// <summary>
        /// ステータスバーのテキスト表示用ラベル
        /// </summary>
        protected ToolStripStatusLabel StatusLabel { get; private set; } = null!;

        /// <summary>
        /// コンポーネント名を設定するプロパティ。
        /// 設定するとタイトルバーが「DesktopKit — [コンポーネント名]」に更新される。
        /// </summary>
        public string ComponentName
        {
            get => _componentName;
            set
            {
                _componentName = value;
                Text = $"DesktopKit — {value}";
            }
        }
        private string _componentName = string.Empty;

        /// <summary>
        /// BaseFormのコンストラクタ。共通UIを初期化する。
        /// </summary>
        public BaseForm()
        {
            InitializeBaseComponents();
        }

        /// <summary>
        /// 共通UIコンポーネントを初期化する。
        /// </summary>
        private void InitializeBaseComponents()
        {
            // ウィンドウサイズ
            ClientSize = new Size(800, 600);
            MinimumSize = new Size(640, 480);

            // フォント（メイリオ 9pt）
            Font = new Font("メイリオ", 9f, FontStyle.Regular, GraphicsUnit.Point);

            // ステータスバー
            StatusBar = new StatusStrip();
            StatusLabel = new ToolStripStatusLabel
            {
                Text = "準備完了",
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft
            };
            StatusBar.Items.Add(StatusLabel);
            Controls.Add(StatusBar);

            // フォームの基本設定
            StartPosition = FormStartPosition.CenterScreen;
        }
    }
}
