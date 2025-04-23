using System;
using System.IO;
using System.Linq;
using Barotrauma;
using HarmonyLib;
using Microsoft.Xna.Framework;

namespace BarotraumaHD
{
    /// <summary>
    /// Главный класс HD-мода, реализует IAssemblyPlugin для интеграции с игрой
    /// </summary>
    public sealed class HDMod : IAssemblyPlugin, IDisposable
    {
        #region Константы
        /// <summary>Имя мода для идентификации</summary>
        public const string MOD_NAME = "[HD] Texture Overhaul";
        
        /// <summary>Минимальная поддерживаемая версия игры</summary>
        public const string MIN_GAME_VERSION = "1.8.7.0";
        #endregion

        #region Публичные свойства
        /// <summary>Активна ли замена текстур</summary>
        public static bool IsEnabled { get; private set; } = true;
        
        /// <summary>Путь к папке мода</summary>
        public static string ModDirectory { get; private set; }
        
        /// <summary>Режим отладки (логирование в консоль)</summary>
        public static bool DebugMode { get; set; }
        #endregion

        #region Приватные поля
        private Harmony _harmony;
        private static HDMod _instance;
        private bool _initialized;
        #endregion

        /// <summary>
        /// Конструктор, вызывается игрой при загрузке мода
        /// </summary>
        public HDMod()
        {
            if (_instance != null)
            {
                throw new InvalidOperationException("Мод уже инициализирован!");
            }
            _instance = this;
        }

        /// <summary>
        /// Основной метод инициализации
        /// </summary>
        public void Initialize()
        {
            if (_initialized) return;

            try
            {
                // 1. Настройка путей
                InitializePaths();
                
                // 2. Загрузка конфигурации
                LoadConfiguration();
                
                // 3. Инициализация Harmony
                _harmony = new Harmony("com.barotrauma.hdmod");
                
                // 4. Настройка систем
                SetupSubsystems();
                
                // 5. Запуск сервисов
                StartServices();

                _initialized = true;
                Log("Мод успешно инициализирован");
            }
            catch (Exception ex)
            {
                Error($"Ошибка инициализации: {ex.Message}");
                Dispose();
            }
        }

        #region Основные методы
        /// <summary>
        /// Инициализация путей и проверка файлов
        /// </summary>
        private void InitializePaths()
        {
            ModDirectory = ContentPackageManager.EnabledPackages.All
                .FirstOrDefault(p => p.Name.Contains(MOD_NAME))?.Dir 
                ?? throw new DirectoryNotFoundException("Папка мода не найдена");

            // Создаем необходимые подкаталоги
            Directory.CreateDirectory(Path.Combine(ModDirectory, "Cache"));
            Directory.CreateDirectory(Path.Combine(ModDirectory, "Logs"));
        }

        /// <summary>
        /// Загрузка конфигурации
        /// </summary>
        private void LoadConfiguration()
        {
            try
            {
                var configPath = Path.Combine(ModDirectory, "Config", "HDConfig.xml");
                if (File.Exists(configPath))
                {
                    // TODO: Реализовать загрузку конфига
                    DebugMode = false;
                    Log("Конфигурация загружена");
                }
            }
            catch (Exception ex)
            {
                Error($"Ошибка загрузки конфигурации: {ex.Message}");
            }
        }

        /// <summary>
        /// Настройка подсистем мода
        /// </summary>
        private void SetupSubsystems()
        {
            try
            {
                // TODO: Инициализация TextureManager, CommandSystem и других компонентов
                
                Log("Подсистемы инициализированы");
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка настройки подсистем: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Запуск фоновых сервисов
        /// </summary>
        private void StartServices()
        {
            try
            {
                // TODO: Запуск мониторинга памяти, автообновления и других сервисов
                
                Log("Сервисы запущены");
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка запуска сервисов: {ex.Message}", ex);
            }
        }
        #endregion

        #region Служебные методы
        /// <summary>
        /// Логирование сообщений
        /// </summary>
        public static void Log(string message)
        {
            DebugConsole.NewMessage($"[HD] {message}", Color.Cyan);
            WriteToLog(message);
        }

        /// <summary>
        /// Логирование ошибок
        /// </summary>
        public static void Error(string message)
        {
            DebugConsole.NewMessage($"[HD] ОШИБКА: {message}", Color.Red);
            WriteToLog($"ERROR: {message}");
        }

        /// <summary>
        /// Запись в лог-файл
        /// </summary>
        private static void WriteToLog(string message)
        {
            try
            {
                File.AppendAllText(
                    Path.Combine(ModDirectory, "Logs", "HDMod.log"),
                    $"[{DateTime.Now:HH:mm:ss}] {message}\n");
            }
            catch { /* Игнорируем ошибки записи */ }
        }
        #endregion

        /// <summary>
        /// Очистка ресурсов при выгрузке мода
        /// </summary>
        public void Dispose()
        {
            try
            {
                if (!_initialized) return;

                // 1. Остановка сервисов
                // TODO: Реализовать остановку сервисов

                // 2. Удаление Harmony-патчей
                _harmony?.UnpatchAll();
                _harmony = null;

                // 3. Очистка кэшей
                // TODO: Очистка кэша текстур

                Log("Мод выгружен");
            }
            catch (Exception ex)
            {
                DebugConsole.NewMessage($"[HD] КРИТИЧЕСКАЯ ОШИБКА при выгрузке: {ex.Message}", Color.DarkRed);
            }
            finally
            {
                _instance = null;
                _initialized = false;
            }
        }

        // Методы, требуемые IAssemblyPlugin
        public void OnLoadCompleted() { }
        public void PreInitPatching() { }
    }
}