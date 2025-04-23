using System;
using Microsoft.Xna.Framework;

namespace BarotraumaHD
{
    /// <summary>
    /// Централизованное хранилище констант и настроек HD-мода
    /// </summary>
    public static class HDConstants
    {
        #region Версии и идентификаторы
        /// <summary>Имя мода для отображения в интерфейсе</summary>
        public const string ModName = "[HD] Texture Overhaul";
        
        /// <summary>Текущая версия мода (SemVer)</summary>
        public const string ModVersion = "2.0.0";
        
        /// <summary>Минимальная требуемая версия игры</summary>
        public const string MinGameVersion = "1.8.7.0";
        
        /// <summary>ID мода в Steam Workshop (0 если не опубликован)</summary>
        public const ulong WorkshopID = 0;
        #endregion

        #region Пути и файлы
        /// <summary>Относительный путь к папке с HD-текстурами</summary>
        public const string TexturesFolder = "Content/HDTextures";
        
        /// <summary>Имя файла конфигурации</summary>
        public const string ConfigFile = "Config/HDConfig.xml";
        
        /// <summary>Имя файла с горячими клавишами</summary>
        public const string KeyBindingsFile = "Config/HDKeybinds.xml";
        
        /// <summary>Имя файла локализации</summary>
        public const string LocalizationFile = "Localization/HDTranslations.xml";
        
        /// <summary>Имя лог-файла</summary>
        public const string LogFile = "Logs/HDMod.log";
        #endregion

        #region Настройки текстур
        /// <summary>Множитель разрешения HD-текстур (2.0 = 2x от оригинала)</summary>
        public const float DefaultTextureScale = 2.0f;
        
        /// <summary>Максимальный размер кэша текстур в MB</summary>
        public const int MaxTextureCacheMB = 2048;
        
        /// <summary>Поддерживаемые форматы текстур (в порядке приоритета)</summary>
        public static readonly string[] SupportedTextureFormats = { ".dds", ".png", ".jpg" };
        #endregion

        #region Цвета интерфейса
        /// <summary>Цвет информационных сообщений</summary>
        public static readonly Color InfoColor = new Color(100, 200, 255);
        
        /// <summary>Цвет предупреждений</summary>
        public static readonly Color WarningColor = Color.Orange;
        
        /// <summary>Цвет ошибок</summary>
        public static readonly Color ErrorColor = new Color(255, 50, 50);
        
        /// <summary>Цвет успешных операций</summary>
        public static readonly Color SuccessColor = Color.LightGreen;
        
        /// <summary>Цвет FPS-счетчика при нормальной производительности</summary>
        public static readonly Color GoodFpsColor = Color.Lime;
        
        /// <summary>Цвет FPS-счетчика при низкой производительности</summary>
        public static readonly Color BadFpsColor = Color.Red;
        #endregion

        #region Ключевые настройки
        /// <summary>Включить ли замену текстур по умолчанию</summary>
        public const bool DefaultTextureReplacementEnabled = true;
        
        /// <summary>Включить ли режим отладки по умолчанию</summary>
        public const bool DefaultDebugMode = false;
        
        /// <summary>Порог FPS для предупреждения о низкой производительности</summary>
        public const int LowFpsThreshold = 30;
        
        /// <summary>Интервал автосохранения настроек (в секундах)</summary>
        public const int SettingsAutoSaveInterval = 300;
        #endregion

        #region Горячие клавиши (умолчания)
        /// <summary>Горячая клавиша включения/выключения мода</summary>
        public const Microsoft.Xna.Framework.Input.Keys ToggleModKey = Microsoft.Xna.Framework.Input.Keys.F10;
        
        /// <summary>Горячая клавиша открытия меню</summary>
        public const Microsoft.Xna.Framework.Input.Keys OpenMenuKey = Microsoft.Xna.Framework.Input.Keys.F11;
        
        /// <summary>Горячая клавиша перезагрузки текстур</summary>
        public const Microsoft.Xna.Framework.Input.Keys ReloadTexturesKey = Microsoft.Xna.Framework.Input.Keys.F12;
        #endregion

        #region Системные константы
        /// <summary>Максимальное количество попыток повторной загрузки текстуры</summary>
        public const int MaxTextureLoadAttempts = 3;
        
        /// <summary>Задержка между попытками загрузки (в мс)</summary>
        public const int TextureLoadRetryDelay = 1000;
        
        /// <summary>Таймаут асинхронных операций (в мс)</summary>
        public const int AsyncOperationTimeout = 5000;
        #endregion

        #region Методы
        /// <summary>
        /// Проверяет, поддерживается ли формат текстуры
        /// </summary>
        public static bool IsTextureFormatSupported(string extension)
        {
            if (string.IsNullOrEmpty(extension)) return false;
            
            foreach (var format in SupportedTextureFormats)
            {
                if (extension.Equals(format, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Возвращает путь к папке мода
        /// </summary>
        public static string GetModDirectory()
        {
            return Path.Combine("Mods", ModName);
        }
        #endregion
    }
}