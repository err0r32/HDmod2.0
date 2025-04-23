using System;
using System.Collections.Generic;
using System.Linq;
using Barotrauma;

namespace BarotraumaHD
{
    /// <summary>
    /// Система обработки совместимости с другими модами
    /// </summary>
    public static class HDModCompatibility
    {
        private static readonly Dictionary<string, int> _modPriorityMap = new Dictionary<string, int>();
        private static readonly HashSet<string> _ignoredMods = new HashSet<string>();
        private static bool _initialized;

        /// <summary>
        /// Инициализация системы совместимости
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;

            // Стандартные приоритеты
            _modPriorityMap["barotrauma.hd"] = 50; // Базовый приоритет HD мода
            _modPriorityMap["barotrauma.vanilla"] = 0; // Оригинальные текстуры
            
            // Загрузка пользовательских настроек
            LoadCompatibilityConfig();
            
            _initialized = true;
            HDMod.Log("Система совместимости инициализирована");
        }

        /// <summary>
        /// Загружает конфигурацию совместимости из файла
        /// </summary>
        private static void LoadCompatibilityConfig()
        {
            try
            {
                string configPath = Path.Combine(HDMod.ModDirectory, "Config", "HDCompatibility.xml");
                if (!File.Exists(configPath)) return;

                // TODO: Реализовать парсинг XML конфига
                // Пример структуры:
                /*
                <Compatibility>
                    <Mod id="neurotrauma" priority="100" />
                    <Mod id="barotraumatic" priority="90" />
                    <Ignore id="some.mod.id" />
                </Compatibility>
                */
            }
            catch (Exception ex)
            {
                HDMod.Error($"Ошибка загрузки конфига совместимости: {ex.Message}");
            }
        }

        /// <summary>
        /// Определяет приоритетный мод для загрузки текстуры
        /// </summary>
        public static string GetPriorityTextureMod(string texturePath)
        {
            if (!_initialized) Initialize();

            // 1. Проверяем игнорируемые моды
            foreach (var modId in _ignoredMods)
            {
                if (texturePath.Contains(modId, StringComparison.OrdinalIgnoreCase))
                    return null;
            }

            // 2. Ищем мод с максимальным приоритетом
            var matchingMods = _modPriorityMap
                .Where(kv => texturePath.Contains(kv.Key, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(kv => kv.Value)
                .ToList();

            return matchingMods.Count > 0 ? matchingMods[0].Key : null;
        }

        /// <summary>
        /// Регистрирует приоритет мода
        /// </summary>
        public static void RegisterModPriority(string modId, int priority)
        {
            _modPriorityMap[modId.ToLowerInvariant()] = priority;
            HDMod.Log($"Установлен приоритет {priority} для мода {modId}");
        }

        /// <summary>
        /// Добавляет мод в игнор-лист
        /// </summary>
        public static void IgnoreMod(string modId)
        {
            _ignoredMods.Add(modId.ToLowerInvariant());
            HDMod.Log($"Мод {modId} добавлен в игнор-лист");
        }

        /// <summary>
        /// Проверяет, заменяет ли другой мод эту текстуру
        /// </summary>
        public static bool IsOverriddenByOtherMod(string texturePath)
        {
            if (!_initialized) Initialize();

            var priorityMod = GetPriorityTextureMod(texturePath);
            return priorityMod != null && !priorityMod.Equals("barotrauma.hd");
        }

        /// <summary>
        /// Получает список конфликтующих модов для текстуры
        /// </summary>
        public static List<string> GetConflictingMods(string texturePath)
        {
            if (!_initialized) Initialize();

            return _modPriorityMap
                .Where(kv => texturePath.Contains(kv.Key, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(kv => kv.Value)
                .Select(kv => kv.Key)
                .ToList();
        }

        /// <summary>
        /// Сбрасывает настройки совместимости
        /// </summary>
        public static void Reset()
        {
            _modPriorityMap.Clear();
            _ignoredMods.Clear();
            _initialized = false;
            HDMod.Log("Настройки совместимости сброшены");
        }
    }
}