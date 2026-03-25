using System.Windows.Forms;

namespace DesktopKit.Common
{
    /// <summary>
    /// ファイル・フォルダ選択ダイアログのラッパークラス。
    /// 各コンポーネントで共通して使うダイアログ呼び出しを簡潔にする。
    /// </summary>
    public static class FileDialogHelper
    {
        /// <summary>
        /// フォルダ選択ダイアログを表示し、選択されたフォルダパスを返す。
        /// </summary>
        /// <param name="description">ダイアログの説明文</param>
        /// <param name="initialPath">初期表示パス（省略可）</param>
        /// <returns>選択されたフォルダパス。キャンセル時はnull</returns>
        public static string? SelectFolder(string description = "フォルダを選択してください", string? initialPath = null)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = description,
                UseDescriptionForTitle = true
            };

            if (!string.IsNullOrEmpty(initialPath) && Directory.Exists(initialPath))
            {
                dialog.InitialDirectory = initialPath;
            }

            return dialog.ShowDialog() == DialogResult.OK ? dialog.SelectedPath : null;
        }

        /// <summary>
        /// ファイル選択ダイアログを表示し、選択されたファイルパスを返す。
        /// </summary>
        /// <param name="title">ダイアログのタイトル</param>
        /// <param name="filter">ファイルフィルタ（例: "テキストファイル|*.txt"）</param>
        /// <param name="initialDir">初期表示ディレクトリ（省略可）</param>
        /// <returns>選択されたファイルパス。キャンセル時はnull</returns>
        public static string? SelectFile(string title = "ファイルを選択してください", string filter = "すべてのファイル|*.*", string? initialDir = null)
        {
            using var dialog = new OpenFileDialog
            {
                Title = title,
                Filter = filter
            };

            if (!string.IsNullOrEmpty(initialDir) && Directory.Exists(initialDir))
            {
                dialog.InitialDirectory = initialDir;
            }

            return dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : null;
        }

        /// <summary>
        /// 複数ファイル選択ダイアログを表示し、選択されたファイルパスの配列を返す。
        /// </summary>
        /// <param name="title">ダイアログのタイトル</param>
        /// <param name="filter">ファイルフィルタ</param>
        /// <param name="initialDir">初期表示ディレクトリ（省略可）</param>
        /// <returns>選択されたファイルパスの配列。キャンセル時はnull</returns>
        public static string[]? SelectFiles(string title = "ファイルを選択してください", string filter = "すべてのファイル|*.*", string? initialDir = null)
        {
            using var dialog = new OpenFileDialog
            {
                Title = title,
                Filter = filter,
                Multiselect = true
            };

            if (!string.IsNullOrEmpty(initialDir) && Directory.Exists(initialDir))
            {
                dialog.InitialDirectory = initialDir;
            }

            return dialog.ShowDialog() == DialogResult.OK ? dialog.FileNames : null;
        }

        /// <summary>
        /// 保存先ファイル選択ダイアログを表示し、選択されたファイルパスを返す。
        /// </summary>
        /// <param name="title">ダイアログのタイトル</param>
        /// <param name="filter">ファイルフィルタ</param>
        /// <param name="defaultFileName">デフォルトファイル名</param>
        /// <param name="initialDir">初期表示ディレクトリ（省略可）</param>
        /// <returns>選択されたファイルパス。キャンセル時はnull</returns>
        public static string? SaveFile(string title = "保存先を選択してください", string filter = "すべてのファイル|*.*", string defaultFileName = "", string? initialDir = null)
        {
            using var dialog = new SaveFileDialog
            {
                Title = title,
                Filter = filter,
                FileName = defaultFileName
            };

            if (!string.IsNullOrEmpty(initialDir) && Directory.Exists(initialDir))
            {
                dialog.InitialDirectory = initialDir;
            }

            return dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : null;
        }
    }
}
