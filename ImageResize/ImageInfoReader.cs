using SixLabors.ImageSharp;
using SLImage = SixLabors.ImageSharp.Image;

namespace DesktopKit.ImageResize
{
    /// <summary>
    /// 画像ファイルの情報を保持するレコード
    /// </summary>
    public record ImageFileInfo(
        string FileName,        // ファイル名
        string FullPath,        // フルパス
        string Format,          // "JPEG" / "PNG" / "WebP"
        long FileSizeBytes,     // ファイルサイズ（バイト）
        int Width,              // 横幅（ピクセル）
        int Height              // 縦幅（ピクセル）
    );

    /// <summary>
    /// 階層表示用のエントリ（フォルダ行またはファイル行）
    /// </summary>
    public class DisplayEntry
    {
        public int Depth { get; set; }
        public bool IsFolder { get; set; }
        public string DisplayName { get; set; } = "";
        public string FullPath { get; set; } = "";
        public ImageFileInfo? FileInfo { get; set; }
    }

    /// <summary>
    /// 画像ファイルの情報を読み取るクラス
    /// </summary>
    public static class ImageInfoReader
    {
        private static readonly Dictionary<string, string> ExtensionToFormat = new(StringComparer.OrdinalIgnoreCase)
        {
            { ".jpg", "JPEG" },
            { ".jpeg", "JPEG" },
            { ".png", "PNG" },
            { ".webp", "WebP" }
        };

        /// <summary>
        /// 指定フォルダ内の対象画像ファイルの情報を取得する。
        /// サブフォルダは走査しない（直下のみ）。
        /// </summary>
        /// <param name="folderPath">対象フォルダのパス</param>
        /// <param name="includeJpeg">JPEGを含むか</param>
        /// <param name="includePng">PNGを含むか</param>
        /// <param name="includeWebp">WebPを含むか</param>
        /// <returns>画像情報のリスト</returns>
        public static List<ImageFileInfo> ReadFolder(
            string folderPath,
            bool includeJpeg,
            bool includePng,
            bool includeWebp)
        {
            var targetExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (includeJpeg) { targetExtensions.Add(".jpg"); targetExtensions.Add(".jpeg"); }
            if (includePng) { targetExtensions.Add(".png"); }
            if (includeWebp) { targetExtensions.Add(".webp"); }

            var results = new List<ImageFileInfo>();

            if (targetExtensions.Count == 0 || !Directory.Exists(folderPath))
                return results;

            var files = Directory.GetFiles(folderPath)
                .Where(f => targetExtensions.Contains(Path.GetExtension(f)))
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase);

            foreach (var filePath in files)
            {
                try
                {
                    var fileInfo = new FileInfo(filePath);
                    var ext = Path.GetExtension(filePath);
                    var format = ExtensionToFormat[ext];

                    int width = 0, height = 0;
                    if (fileInfo.Length > 0)
                    {
                        var imageInfo = SLImage.Identify(filePath);
                        if (imageInfo != null)
                        {
                            width = imageInfo.Width;
                            height = imageInfo.Height;
                        }
                    }

                    results.Add(new ImageFileInfo(
                        FileName: fileInfo.Name,
                        FullPath: filePath,
                        Format: format,
                        FileSizeBytes: fileInfo.Length,
                        Width: width,
                        Height: height
                    ));
                }
                catch
                {
                    // 読み取れないファイルはスキップ
                }
            }

            return results;
        }

        /// <summary>
        /// 指定フォルダ配下の全階層を再帰的に走査し、
        /// フォルダ行・ファイル行を含む表示用エントリのリストを返す。
        /// </summary>
        public static List<DisplayEntry> ReadFolderRecursive(
            string folderPath,
            bool includeJpeg,
            bool includePng,
            bool includeWebp)
        {
            var targetExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (includeJpeg) { targetExtensions.Add(".jpg"); targetExtensions.Add(".jpeg"); }
            if (includePng) { targetExtensions.Add(".png"); }
            if (includeWebp) { targetExtensions.Add(".webp"); }

            var entries = new List<DisplayEntry>();
            if (targetExtensions.Count == 0 || !Directory.Exists(folderPath))
                return entries;

            CollectRecursive(folderPath, targetExtensions, 0, entries);
            return entries;
        }

        private static void CollectRecursive(
            string folderPath,
            HashSet<string> targetExtensions,
            int depth,
            List<DisplayEntry> entries)
        {
            // フォルダ行を追加
            entries.Add(new DisplayEntry
            {
                Depth = depth,
                IsFolder = true,
                DisplayName = Path.GetFileName(folderPath) + "/",
                FullPath = folderPath
            });

            // 直下ファイルを追加
            var files = Directory.GetFiles(folderPath)
                .Where(f => targetExtensions.Contains(Path.GetExtension(f)))
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase);

            foreach (var filePath in files)
            {
                try
                {
                    var fileInfo = new FileInfo(filePath);
                    var ext = Path.GetExtension(filePath);
                    var format = ExtensionToFormat[ext];

                    int width = 0, height = 0;
                    if (fileInfo.Length > 0)
                    {
                        var imageInfo = SLImage.Identify(filePath);
                        if (imageInfo != null)
                        {
                            width = imageInfo.Width;
                            height = imageInfo.Height;
                        }
                    }

                    var imgInfo = new ImageFileInfo(
                        FileName: fileInfo.Name,
                        FullPath: filePath,
                        Format: format,
                        FileSizeBytes: fileInfo.Length,
                        Width: width,
                        Height: height
                    );

                    entries.Add(new DisplayEntry
                    {
                        Depth = depth + 1,
                        IsFolder = false,
                        DisplayName = fileInfo.Name,
                        FullPath = filePath,
                        FileInfo = imgInfo
                    });
                }
                catch
                {
                    // 読み取れないファイルはスキップ
                }
            }

            // サブフォルダを再帰処理
            foreach (var dir in Directory.GetDirectories(folderPath).OrderBy(d => d, StringComparer.OrdinalIgnoreCase))
            {
                CollectRecursive(dir, targetExtensions, depth + 1, entries);
            }
        }
    }
}
