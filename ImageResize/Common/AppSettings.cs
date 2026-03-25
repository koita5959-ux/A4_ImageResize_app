using System.IO;
using System.Text.Json;

namespace DesktopKit.Common
{
    /// <summary>
    /// JSON設定ファイルの読み書きを担うクラス。
    /// %AppData%/DesktopKit/ に設定ファイルを保存する。
    /// </summary>
    public class AppSettings
    {
        private static readonly string SettingsDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DesktopKit");

        private readonly string _filePath;
        private Dictionary<string, string> _settings;

        /// <summary>
        /// 指定されたコンポーネント名の設定ファイルを管理するインスタンスを生成する。
        /// </summary>
        /// <param name="componentName">コンポーネント名（ファイル名に使用される）</param>
        public AppSettings(string componentName)
        {
            _filePath = Path.Combine(SettingsDir, $"{componentName}.json");
            _settings = new Dictionary<string, string>();
            Load();
        }

        /// <summary>
        /// 設定値を取得する。キーが存在しない場合はデフォルト値を返す。
        /// </summary>
        /// <param name="key">設定キー</param>
        /// <param name="defaultValue">デフォルト値</param>
        /// <returns>設定値</returns>
        public string Get(string key, string defaultValue = "")
        {
            return _settings.TryGetValue(key, out var value) ? value : defaultValue;
        }

        /// <summary>
        /// 設定値を保存する。
        /// </summary>
        /// <param name="key">設定キー</param>
        /// <param name="value">設定値</param>
        public void Set(string key, string value)
        {
            _settings[key] = value;
            Save();
        }

        /// <summary>
        /// 設定ファイルをJSONから読み込む。
        /// </summary>
        private void Load()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    var json = File.ReadAllText(_filePath);
                    _settings = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                                ?? new Dictionary<string, string>();
                }
            }
            catch
            {
                _settings = new Dictionary<string, string>();
            }
        }

        /// <summary>
        /// 設定ファイルをJSONに書き出す。
        /// </summary>
        private void Save()
        {
            try
            {
                Directory.CreateDirectory(SettingsDir);
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(_settings, options);
                File.WriteAllText(_filePath, json);
            }
            catch
            {
                // 書き込み失敗時は静かに無視（設定の永続化は必須ではない）
            }
        }
    }
}
