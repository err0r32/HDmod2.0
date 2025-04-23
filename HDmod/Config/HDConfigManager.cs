using System;
using System.IO;
using System.Xml.Linq;
using Barotrauma;

namespace BarotraumaHD
{
    /// <summary>
    /// Менеджер конфигурации мода с поддержкой горячего перезагрузки
    /// </summary>
    public static class HDConfigManager
    {
        private const string CONFIG_FILE = "Config/HDConfig.xml";
        private static string _configPath;
        private static DateTime _lastWriteTime;
        
        // Конфигурационные параметры
        public static bool EnableHDTextures { get; private set; } = true;
        public static bool EnableTextureCache { get; private set; } = true;
        public static int CacheSizeMB { get; private set; } = 2048;
        public static float TextureScale { get; private set; } = 2.0f;
        public static bool ShowDebugInfo { get; private set; } = false;

        /// <summary>
        /// Инициализация менеджера конфигурации
        /// </summary>
        public static void Initialize()
        {
            _configPath = Path.Combine(HDMod.ModDirectory, CONFIG_FILE);
            LoadConfig();
            HDMod.Log("Конфигурационный менеджер инициализирован");
        }

        /// <summary>
        /// Загрузка конфигурации из XML файла
        /// </summary>
        public static void LoadConfig()
        {
            try
            {
                if (!File.Exists(_configPath))
                {
                    CreateDefaultConfig();
                    return;
                }

                var doc = XDocument.Load(_configPath);
                var root = doc.Element("HDConfig");

                EnableHDTextures = root.Element("EnableHDTextures")?.Value.ToBool() ?? true;
                EnableTextureCache = root.Element("EnableTextureCache")?.Value.ToBool() ?? true;
                CacheSizeMB = root.Element("CacheSizeMB")?.Value.ToInt() ?? 2048;
                TextureScale = root.Element("TextureScale")?.Value.ToFloat() ?? 2.0f;
                ShowDebugInfo = root.Element("ShowDebugInfo")?.Value.ToBool() ?? false;

                _lastWriteTime = File.GetLastWriteTime(_configPath);
                HDMod.Log("Конфигурация загружена");
            }
            catch (Exception ex)
            {
                HDMod.Error($"Ошибка загрузки конфигурации: {ex.Message}");
                CreateDefaultConfig();
            }
        }

        /// <summary>
        /// Проверка обновлений конфигурации
        /// </summary>
        public static void CheckForUpdates()
        {
            try
            {
                var currentWriteTime = File.GetLastWriteTime(_configPath);
                if (currentWriteTime > _lastWriteTime)
                {
                    LoadConfig();
                    HDMod.Log("Обнаружены изменения в конфигурации - выполнена перезагрузка");
                }
            }
            catch
            {
                // Игнорируем ошибки проверки
            }
        }

        /// <summary>
        /// Создание конфигурации по умолчанию
        /// </summary>
        private static void CreateDefaultConfig()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_configPath));
                
                var doc = new XDocument(
                    new XElement("HDConfig",
                        new XElement("EnableHDTextures", true),
                        new XElement("EnableTextureCache", true),
                        new XElement("CacheSizeMB", 2048),
                        new XElement("TextureScale", 2.0),
                        new XElement("ShowDebugInfo", false)
                    )
                );

                doc.Save(_configPath);
                _lastWriteTime = File.GetLastWriteTime(_configPath);
                HDMod.Log("Создана конфигурация по умолчанию");
            }
            catch (Exception ex)
            {
                HDMod.Error($"Ошибка создания конфигурации: {ex.Message}");
            }
        }

        /// <summary>
        /// Сохранение текущих настроек
        /// </summary>
        public static void SaveConfig()
        {
            try
            {
                var doc = new XDocument(
                    new XElement("HDConfig",
                        new XElement("EnableHDTextures", EnableHDTextures),
                        new XElement("EnableTextureCache", EnableTextureCache),
                        new XElement("CacheSizeMB", CacheSizeMB),
                        new XElement("TextureScale", TextureScale),
                        new XElement("ShowDebugInfo", ShowDebugInfo)
                    )
                );

                doc.Save(_configPath);
                _lastWriteTime = File.GetLastWriteTime(_configPath);
                HDMod.Log("Конфигурация сохранена");
            }
            catch (Exception ex)
            {
                HDMod.Error($"Ошибка сохранения конфигурации: {ex.Message}");
            }
        }
    }

    public static class ConfigExtensions
    {
        public static bool ToBool(this string value)
        {
            return bool.TryParse(value, out var result) && result;
        }

        public static int ToInt(this string value)
        {
            return int.TryParse(value, out var result) ? result : 0;
        }

        public static float ToFloat(this string value)
        {
            return float.TryParse(value, out var result) ? result : 0f;
        }
    }
}