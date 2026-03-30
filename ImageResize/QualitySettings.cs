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

        /// <summary>
        /// QualitySettingsのコンストラクタ。
        /// </summary>
        /// <param name="quality">品質値（1〜100）</param>
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
