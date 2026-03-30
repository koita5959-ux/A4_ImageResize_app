namespace DesktopKit.ImageResize.Conditions
{
    /// <summary>
    /// ％指定によるリサイズ条件
    /// </summary>
    public class PercentCondition : ResizeCondition
    {
        private readonly int _percent;

        /// <summary>
        /// PercentConditionのコンストラクタ。
        /// </summary>
        /// <param name="percent">リサイズ倍率（1〜100）</param>
        public PercentCondition(int percent)
        {
            if (percent < 1 || percent > 100)
                throw new ArgumentOutOfRangeException(nameof(percent), "1〜100の範囲で指定してください");
            _percent = percent;
        }

        /// <summary>
        /// 条件の表示名
        /// </summary>
        public override string DisplayName => $"{_percent}%リサイズ";

        /// <summary>
        /// リサイズ後のサイズを計算する。
        /// 100%は実質コピーなのでスキップ（拡大禁止ルールの一環）。
        /// </summary>
        public override (int Width, int Height)? Calculate(int originalWidth, int originalHeight)
        {
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
