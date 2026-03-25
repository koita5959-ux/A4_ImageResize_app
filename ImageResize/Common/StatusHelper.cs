using System.Drawing;
using System.Windows.Forms;

namespace DesktopKit.Common
{
    /// <summary>
    /// ステータスバーへのメッセージ表示を支援するヘルパークラス。
    /// 成功・エラー・情報の種類に応じた表示を行う。
    /// </summary>
    public static class StatusHelper
    {
        /// <summary>
        /// 情報メッセージをステータスバーに表示する。
        /// </summary>
        /// <param name="label">対象のToolStripStatusLabel</param>
        /// <param name="message">表示メッセージ</param>
        public static void ShowInfo(ToolStripStatusLabel label, string message)
        {
            label.Text = message;
            label.ForeColor = SystemColors.ControlText;
        }

        /// <summary>
        /// 成功メッセージをステータスバーに表示する（緑色）。
        /// </summary>
        /// <param name="label">対象のToolStripStatusLabel</param>
        /// <param name="message">表示メッセージ</param>
        public static void ShowSuccess(ToolStripStatusLabel label, string message)
        {
            label.Text = message;
            label.ForeColor = Color.Green;
        }

        /// <summary>
        /// エラーメッセージをステータスバーに表示する（赤色）。
        /// </summary>
        /// <param name="label">対象のToolStripStatusLabel</param>
        /// <param name="message">表示メッセージ</param>
        public static void ShowError(ToolStripStatusLabel label, string message)
        {
            label.Text = message;
            label.ForeColor = Color.Red;
        }
    }
}
