using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Diagnostics;

namespace EZDUploader.Core.Configuration
{
    public static class ConfigurationManager
    {
        private const string CONFIG_FILE = "appsettings.json";

        public static void SaveSettings(ApiSettings settings)
        {
            try
            {
                // Tworzymy kopię ustawień do zapisu
                var settingsToSave = new ApiSettings
                {
                    BaseUrl = settings.BaseUrl,
                    ApplicationToken = settings.ApplicationToken,
                    Login = settings.Login,
                    Password = EncryptString(settings.Password),
                    AuthType = settings.AuthType
                };

                var configPath = GetConfigPath();
                var json = JsonSerializer.Serialize(settingsToSave, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(configPath, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Błąd podczas zapisywania konfiguracji: {ex.Message}");
                throw;
            }
        }

        public static ApiSettings LoadSettings()
        {
            try
            {
                var configPath = GetConfigPath();
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    return JsonSerializer.Deserialize<ApiSettings>(json, options);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Błąd podczas wczytywania ustawień: {ex.Message}");
            }
            return new ApiSettings();
        }

        private static string EncryptString(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            try
            {
                byte[] data = System.Text.Encoding.UTF8.GetBytes(text);
                return Convert.ToBase64String(data);
            }
            catch
            {
                return text;
            }
        }

        private static string DecryptString(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            try
            {
                byte[] data = Convert.FromBase64String(text);
                return System.Text.Encoding.UTF8.GetString(data);
            }
            catch
            {
                return text;
            }
        }

        private static string GetConfigPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CONFIG_FILE);
        }
    }
}