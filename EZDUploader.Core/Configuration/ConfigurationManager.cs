using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Diagnostics;
using EZDUploader.Core.Models;

namespace EZDUploader.Core.Configuration
{
    public static class ConfigurationManager
    {
        private const string CONFIG_FILE = "appsettings.json";
        private const string DOCUMENT_TYPES_FILE = "documentTypes.json";

        public static void SaveSettings(ApiSettings settings)
        {
            try
            {
                var settingsToSave = new ApiSettings
                {
                    BaseUrl = settings.BaseUrl,
                    ApplicationToken = settings.ApplicationToken,
                    Login = settings.Login,
                    Password = settings.AuthType == AuthenticationType.LoginPassword ?
                              EncryptString(settings.Password) :
                              string.Empty,
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
                    var settings = JsonSerializer.Deserialize<ApiSettings>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (settings.AuthType == AuthenticationType.LoginPassword && !string.IsNullOrEmpty(settings.Password))
                    {
                        settings.Password = DecryptString(settings.Password);
                    }

                    // Załaduj typy dokumentów z osobnego pliku
                    settings.DocumentTypes = LoadDocumentTypes();

                    return settings;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Błąd podczas wczytywania ustawień: {ex.Message}");
            }
            return new ApiSettings();
        }

        public static List<DocumentType> LoadDocumentTypes()
        {
            try
            {
                var docTypesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DOCUMENT_TYPES_FILE);
                if (File.Exists(docTypesPath))
                {
                    var json = File.ReadAllText(docTypesPath);
                    var docTypesContainer = JsonSerializer.Deserialize<DocumentTypesContainer>(json);
                    return docTypesContainer?.DocumentTypes ?? GetDefaultDocumentTypes();
                }
                else
                {
                    // Jeśli plik nie istnieje, utwórz go z domyślnymi typami
                    var defaultTypes = GetDefaultDocumentTypes();
                    SaveDocumentTypes(defaultTypes);
                    return defaultTypes;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Błąd podczas wczytywania typów dokumentów: {ex.Message}");
                return GetDefaultDocumentTypes();
            }
        }

        public static void SaveDocumentTypes(List<DocumentType> documentTypes)
        {
            try
            {
                var docTypesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DOCUMENT_TYPES_FILE);
                var container = new DocumentTypesContainer { DocumentTypes = documentTypes };
                var json = JsonSerializer.Serialize(container, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(docTypesPath, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Błąd podczas zapisywania typów dokumentów: {ex.Message}");
                throw;
            }
        }

        private static List<DocumentType> GetDefaultDocumentTypes()
        {
            return new List<DocumentType>
            {
                new() { Id = 1, Name = "Pismo" },
                new() { Id = 2, Name = "Notatka" },
                new() { Id = 3, Name = "Wniosek" },
                new() { Id = 4, Name = "Decyzja" },
                new() { Id = 5, Name = "Opinia" },
                new() { Id = 6, Name = "Zaświadczenie" },
                new() { Id = 7, Name = "Inny" }
            };
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

    internal class DocumentTypesContainer
    {
        public List<DocumentType> DocumentTypes { get; set; } = new();
    }
}