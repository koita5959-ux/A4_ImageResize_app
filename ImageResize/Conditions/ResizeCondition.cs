namespace DesktopKit.ImageResize.Conditions
{
    /// <summary>
    /// リサイズ条件の基底クラス。
    /// 将来の拡張（WidthCondition, HeightCondition等）に備えた設計。
    /// </summary>
    public abstract class ResizeCondition
    {
        /// <summary>
        /// 条件の表示名
        /// </summary>
        public abstract string DisplayName { get; }

        /// <summary>
        /// リサイズ後のサイズを計算する
        /// </summary>
        /// <param name="originalWidth">元の横幅</param>
        /// <param name="originalHeight">元の縦幅</param>
        /// <returns>リサイズ後の (Width, Height)。拡大になる場合は null を返す</returns>
        public abstract (int Width, int Height)? Calculate(int originalWidth, int originalHeight);
    }
}
