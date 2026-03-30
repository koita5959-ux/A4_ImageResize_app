using DesktopKit.ImageResize.Conditions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using SLImage = SixLabors.ImageSharp.Image;

namespace DesktopKit.ImageResize
{
    /// <summary>
    /// 画像のリサイズ処理結果
    /// </summary>
    public record ProcessResult(
        string SourcePath,      // 元ファイルパス
        string OutputPath,      // 出力先パス（成功時）
        bool Success,           // 成功/失敗
        bool Skipped,           // スキップされたか（拡大禁止等）
        string Message          // 結果メッセージ
    );

    /// <summary>
    /// ImageSharpを使用した画像リサイズ処理
    /// </summary>
    public class ImageProcessor
    {
        /// <summary>
        /// 画像ファイルをリサイズして出力フォルダに保存する。
        /// </summary>
        /// <param name="fileInfo">対象ファイルの情報</param>
        /// <param name="condition">リサイズ条件</param>
        /// <param name="quality">品質設定</param>
        /// <param name="outputFolderPath">出力先フォルダパス</param>
        /// <returns>処理結果</returns>
        public ProcessResult Process(
            ImageFileInfo fileInfo,
            ResizeCondition condition,
            QualitySettings quality,
            string outputFolderPath)
        {
            var outputPath = Path.Combine(outputFolderPath, fileInfo.FileName);

            try
            {
                // リサイズ後サイズを算出
                var newSize = condition.Calculate(fileInfo.Width, fileInfo.Height);
                if (newSize == null)
                {
                    return new ProcessResult(
                        fileInfo.FullPath, outputPath, true, true,
                        $"スキップ: {fileInfo.FileName}（拡大またはサイズ変更なし）");
                }

                var (newWidth, newHeight) = newSize.Value;

                // 画像を読み込み、リサイズ、保存
                using var image = SLImage.Load(fileInfo.FullPath);
                image.Mutate(x => x.Resize(newWidth, newHeight, KnownResamplers.Lanczos3));

                // 形式に応じたエンコーダーで保存
                switch (fileInfo.Format)
                {
                    case "JPEG":
                        image.Save(outputPath, new JpegEncoder { Quality = quality.Quality });
                        break;
                    case "PNG":
                        image.Save(outputPath, new PngEncoder());
                        break;
                    case "WebP":
                        image.Save(outputPath, new WebpEncoder { Quality = quality.Quality });
                        break;
                    default:
                        return new ProcessResult(
                            fileInfo.FullPath, outputPath, false, false,
                            $"エラー: {fileInfo.FileName}（未対応の形式: {fileInfo.Format}）");
                }

                return new ProcessResult(
                    fileInfo.FullPath, outputPath, true, false,
                    $"成功: {fileInfo.FileName} ({newWidth}×{newHeight})");
            }
            catch (Exception ex)
            {
                return new ProcessResult(
                    fileInfo.FullPath, outputPath, false, false,
                    $"エラー: {fileInfo.FileName}（{ex.Message}）");
            }
        }
    }
}
