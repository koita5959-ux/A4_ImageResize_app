namespace DesktopKit.ImageResize
{
    /// <summary>
    /// ファイルのアラート判定レベル
    /// </summary>
    public enum AlertLevel
    {
        /// <summary>正常</summary>
        None,
        /// <summary>警告（将来用の予備）</summary>
        Warning,
        /// <summary>危険（巨大ファイル）</summary>
        Danger,
        /// <summary>処理対象外（空ファイル）</summary>
        Skip
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
