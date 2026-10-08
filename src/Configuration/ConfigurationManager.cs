using System.Text.Encodings.Web;
using System.Text.Json;

namespace USBSilentCopy.Configuration;

/// <summary>
/// مدیریت بارگذاری، ذخیره‌سازی و بازنشانی تنظیمات config.json
/// </summary>
public class ConfigurationManager
{
    private readonly string _configFilePath;
    private AppConfig _currentConfig;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public AppConfig Current => _currentConfig;

    public event Action<AppConfig>? ConfigChanged;

    public ConfigurationManager(string? configPath = null)
    {
        if (string.IsNullOrWhiteSpace(configPath))
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _configFilePath = Path.Combine(baseDir, "config.json");
        }
        else
        {
            _configFilePath = Path.GetFullPath(configPath);
        }

        _currentConfig = LoadOrCreateDefault();
    }

    public string ConfigFilePath => _configFilePath;

    public AppConfig LoadOrCreateDefault()
    {
        try
        {
            if (File.Exists(_configFilePath))
            {
                var json = File.ReadAllText(_configFilePath);
                var loaded = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions);
                if (loaded != null)
                {
                    var errors = ConfigValidator.Validate(loaded);
                    if (errors.Count == 0)
                    {
                        _currentConfig = loaded;
                        return _currentConfig;
                    }
                }
            }
        }
        catch
        {
            // در صورت بروز هرگونه خطای خواندن یا پارس نامعتبر، تنظیمات پیش‌فرض امن بارگذاری می‌شود
        }

        _currentConfig = new AppConfig();
        Save(_currentConfig);
        return _currentConfig;
    }

    public bool Save(AppConfig config)
    {
        try
        {
            var errors = ConfigValidator.Validate(config);
            if (errors.Count > 0)
            {
                return false;
            }

            var dir = Path.GetDirectoryName(_configFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonSerializer.Serialize(config, JsonOptions);
            File.WriteAllText(_configFilePath, json);
            _currentConfig = config;
            ConfigChanged?.Invoke(_currentConfig);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public AppConfig ResetToDefault()
    {
        var defaultConfig = new AppConfig();
        Save(defaultConfig);
        return defaultConfig;
    }

    /// <summary>
    /// تبدیل مسیر نسبی به مسیر مطلق با مبنای پوشه برنامه
    /// </summary>
    public string ResolveAbsolutePath(string path)
    {
        if (Path.IsPathRooted(path))
            return path;

        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        return Path.GetFullPath(Path.Combine(baseDir, path));
    }
}
