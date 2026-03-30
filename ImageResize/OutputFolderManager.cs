namespace DesktopKit.ImageResize
{
    /// <summary>
    /// 出力先フォルダの生成・管理を行うクラス
    /// </summary>
    public static class OutputFolderManager
    {
        /// <summary>
        /// デフォルトの出力フォルダ名を生成する。
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
