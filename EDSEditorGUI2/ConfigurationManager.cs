using System;
using System.IO;
using System.Text.Json;
using libEDSsharp;
using System.Globalization;

namespace EDSEditorGUI2
{
    public class AppSettingsData
    {
        public ExporterFactory.Exporter CurrentExporter { get; set; } = ExporterFactory.Exporter.CANOPENNODE_V4;
        public string CurrentLanguage { get; set; } = "";
        public string CurrentTheme { get; set; } = "Default";
        public uint WarningMask { get; set; } = 0xFFFF; // default all warnings
    }

    public static class ConfigurationManager
    {
        private static readonly string ConfigPath;
        public static AppSettingsData Settings { get; private set; } = new AppSettingsData();

        static ConfigurationManager()
        {
            string homepath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), ".edseditor");
            if (!Directory.Exists(homepath))
            {
                Directory.CreateDirectory(homepath);
            }
            ConfigPath = Path.Combine(homepath, "config.json");
        }

        public static void Load()
        {
            if (File.Exists(ConfigPath))
            {
                try
                {
                    string json = File.ReadAllText(ConfigPath);
                    var settings = JsonSerializer.Deserialize<AppSettingsData>(json);
                    if (settings != null)
                    {
                        Settings = settings;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load config: {ex}");
                }
            }

            // Automatic System Language Detection Fallback
            if (string.IsNullOrEmpty(Settings.CurrentLanguage))
            {
                var culture = CultureInfo.CurrentUICulture.Name.ToLower();
                if (culture.Contains("zh"))
                {
                    Settings.CurrentLanguage = "zh-CN";
                }
                else
                {
                    Settings.CurrentLanguage = "en-US";
                }
            }
            
            // Sync legacy Warnings mask in libEDSsharp
            Warnings.warning_mask = Settings.WarningMask;
        }

        public static void Save()
        {
            try
            {
                Settings.WarningMask = Warnings.warning_mask;
                string json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save config: {ex}");
            }
        }
    }
}
