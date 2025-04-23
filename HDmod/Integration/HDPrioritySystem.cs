using System;
using System.Collections.Generic;
using System.Linq;
using Barotrauma;

namespace BarotraumaHD
{
    /// <summary>
    /// Система управления приоритетами загрузки текстур для модов
    /// </summary>
    public sealed class HDPrioritySystem
    {
        private readonly Dictionary<string, int> _modPriorities = new Dictionary<string, int>();
        private readonly Dictionary<string, string> _forcedOverrides = new Dictionary<string, string>();
        private readonly HDTextureManager _textureManager;

        public HDPrioritySystem(HDTextureManager textureManager)
        {
            _textureManager = textureManager ?? throw new ArgumentNullException(nameof(textureManager));
        }

        /// <summary>
        /// Регистрирует мод и устанавливает его приоритет
        /// </summary>
        /// <param name="modId">Идентификатор мода</param>
        /// <param name="priority">Приоритет (чем выше, тем приоритетнее)</param>
        public void RegisterMod(string modId, int priority)
        {
            if (string.IsNullOrWhiteSpace(modId))
                throw new ArgumentException("Mod ID cannot be empty", nameof(modId));

            _modPriorities[modId] = priority;
            HDMod.Log($"Registered mod '{modId}' with priority {priority}");
        }

        /// <summary>
        /// Устанавливает принудительное переопределение текстуры
        /// </summary>
        public void SetOverride(string texturePath, string modId)
        {
            if (!_modPriorities.ContainsKey(modId))
                throw new InvalidOperationException($"Mod '{modId}' is not registered");

            _forcedOverrides[texturePath] = modId;
            HDMod.Log($"Texture override set: {texturePath} -> {modId}");
        }

        /// <summary>
        /// Получает приоритетный путь к текстуре с учетом всех модов
        /// </summary>
        public string GetPriorityTexturePath(string originalPath)
        {
            // 1. Проверка принудительных переопределений
            if (_forcedOverrides.TryGetValue(originalPath, out var modId))
            {
                return GetModTexturePath(originalPath, modId);
            }

            // 2. Автоматический выбор по приоритетам
            var availableMods = FindAvailableModsForTexture(originalPath);
            if (availableMods.Count == 0) return originalPath;

            var highestPriorityMod = availableMods
                .OrderByDescending(m => _modPriorities[m])
                .First();

            return GetModTexturePath(originalPath, highestPriorityMod);
        }

        /// <summary>
        /// Находит все моды, содержащие указанную текстуру
        /// </summary>
        private List<string> FindAvailableModsForTexture(string texturePath)
        {
            var availableMods = new List<string>();
            foreach (var modId in _modPriorities.Keys)
            {
                var modPath = GetModTexturePath(texturePath, modId);
                if (_textureManager.TextureExists(modPath))
                {
                    availableMods.Add(modId);
                }
            }
            return availableMods;
        }

        /// <summary>
        /// Формирует путь к текстуре в папке мода
        /// </summary>
        private string GetModTexturePath(string originalPath, string modId)
        {
            var fileName = Path.GetFileName(originalPath);
            return Path.Combine("Mods", modId, "HD", fileName);
        }

        /// <summary>
        /// Очищает все принудительные переопределения
        /// </summary>
        public void ClearOverrides()
        {
            _forcedOverrides.Clear();
            HDMod.Log("Cleared all texture overrides");
        }

        /// <summary>
        /// Обновляет приоритеты на основе конфигурации
        /// </summary>
        public void UpdateFromConfig(Dictionary<string, int> configPriorities)
        {
            foreach (var kvp in configPriorities)
            {
                RegisterMod(kvp.Key, kvp.Value);
            }
        }
    }
}
/*
// Инициализация
var textureManager = new HDTextureManager(...);
var prioritySystem = new HDPrioritySystem(textureManager);

// Регистрация модов
prioritySystem.RegisterMod("AwesomeMod", 100);
prioritySystem.RegisterMod("BasicHD", 50);

// Установка переопределения
prioritySystem.SetOverride("Content/Items/tool.png", "AwesomeMod");

// Получение приоритетного пути
var bestTexturePath = prioritySystem.GetPriorityTexturePath("Content/Items/tool.png");

// В конфигурации:

<TexturePriorities>
  <Mod name="AwesomeMod" priority="100"/>
  <Mod name="BasicHD" priority="50"/>
  <Override texture="Content/Items/tool.png" mod="AwesomeMod"/>
</TexturePriorities>
*/