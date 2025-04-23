using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Barotrauma;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BarotraumaHD
{
    /// <summary>
    /// Инструменты отладки и диагностики для HD-мода
    /// </summary>
    public static class HDDebugUtils
    {
        private static readonly StringBuilder _logBuilder = new StringBuilder();
        private static readonly Dictionary<string, Texture2D> _debugTextures = new Dictionary<string, Texture2D>();
        private static bool _showTextureInfo;
        private static bool _showMemoryStats;
        private static bool _showTextureCache;
        private static float _lastUpdateTime;
        private static int _framesCount;
        private static int _fps;

        /// <summary>
        /// Включение/отключение отображения информации о текстурах
        /// </summary>
        public static bool ShowTextureInfo
        {
            get => _showTextureInfo;
            set
            {
                if (_showTextureInfo != value)
                {
                    _showTextureInfo = value;
                    HDMod.Log($"Texture info display {(_showTextureInfo ? "enabled" : "disabled")}");
                }
            }
        }

        /// <summary>
        /// Включение/отключение отображения статистики памяти
        /// </summary>
        public static bool ShowMemoryStats
        {
            get => _showMemoryStats;
            set
            {
                if (_showMemoryStats != value)
                {
                    _showMemoryStats = value;
                    HDMod.Log($"Memory stats display {(_showMemoryStats ? "enabled" : "disabled")}");
                }
            }
        }

        /// <summary>
        /// Включение/отключение отображения кэша текстур
        /// </summary>
        public static bool ShowTextureCache
        {
            get => _showTextureCache;
            set
            {
                if (_showTextureCache != value)
                {
                    _showTextureCache = value;
                    HDMod.Log($"Texture cache display {(_showTextureCache ? "enabled" : "disabled")}");
                }
            }
        }

        /// <summary>
        /// Обновление счетчика FPS
        /// </summary>
        public static void UpdateFpsCounter(float deltaTime)
        {
            _framesCount++;
            _lastUpdateTime += deltaTime;

            if (_lastUpdateTime >= 1.0f)
            {
                _fps = _framesCount;
                _framesCount = 0;
                _lastUpdateTime = 0;
            }
        }

        /// <summary>
        /// Отрисовка диагностической информации
        /// </summary>
        public static void DrawDiagnostics(SpriteBatch spriteBatch, SpriteFont font)
        {
            if (!ShowTextureInfo && !ShowMemoryStats && !ShowTextureCache) return;

            _logBuilder.Clear();
            var position = new Vector2(10, 30);

            if (ShowMemoryStats)
            {
                AppendMemoryStats();
            }

            if (ShowTextureInfo)
            {
                AppendTextureInfo();
            }

            if (ShowTextureCache)
            {
                AppendTextureCacheInfo();
            }

            spriteBatch.DrawString(font, _logBuilder.ToString(), position, Color.White);

            // FPS всегда отображается в правом верхнем углу
            DrawFpsCounter(spriteBatch, font);
        }

        private static void AppendMemoryStats()
        {
            _logBuilder.AppendLine("=== Memory Statistics ===");
            _logBuilder.AppendLine($"Allocated Textures: {_debugTextures.Count}");
            _logBuilder.AppendLine();
        }

        private static void AppendTextureInfo()
        {
            _logBuilder.AppendLine("=== Texture Information ===");
            foreach (var kvp in _debugTextures.Take(5))
            {
                _logBuilder.AppendLine($"{Path.GetFileName(kvp.Key)}: {kvp.Value.Width}x{kvp.Value.Height}");
            }
            _logBuilder.AppendLine();
        }

        private static void AppendTextureCacheInfo()
        {
            if (HDServiceLocator.TryGet<HDTextureCache>(out var cache))
            {
                _logBuilder.AppendLine("=== Texture Cache ===");
                _logBuilder.AppendLine($"Cached Textures: {cache.Count}");
                _logBuilder.AppendLine();
            }
        }

        private static void DrawFpsCounter(SpriteBatch spriteBatch, SpriteFont font)
        {
            var fpsText = $"FPS: {_fps}";
            var textSize = font.MeasureString(fpsText);
            var position = new Vector2(GameMain.GraphicsWidth - textSize.X - 10, 10);

            spriteBatch.DrawString(
                font,
                fpsText,
                position,
                _fps > 30 ? Color.LimeGreen : Color.Red);
        }

        /// <summary>
        /// Регистрация текстуры для отладки
        /// </summary>
        public static void RegisterDebugTexture(string name, Texture2D texture)
        {
            if (_debugTextures.ContainsKey(name))
            {
                _debugTextures[name] = texture;
            }
            else
            {
                _debugTextures.Add(name, texture);
            }
        }

        /// <summary>
        /// Создание чекпоинта для поиска утечек памяти
        /// </summary>
        public static void CreateMemoryCheckpoint(string checkpointName)
        {
            // Реализация сравнения состояния памяти
            HDMod.Log($"Memory checkpoint created: {checkpointName}");
        }

        /// <summary>
        /// Очистка всех зарегистрированных текстур
        /// </summary>
        public static void ClearDebugTextures()
        {
            foreach (var texture in _debugTextures.Values)
            {
                texture?.Dispose();
            }
            _debugTextures.Clear();
        }

        /// <summary>
        /// Сохранение лога в файл
        /// </summary>
        public static void SaveLogToFile()
        {
            try
            {
                File.WriteAllText(
                    Path.Combine(HDMod.ModDirectory, "debug_log.txt"),
                    _logBuilder.ToString());
            }
            catch (Exception ex)
            {
                HDMod.Error($"Failed to save debug log: {ex.Message}");
            }
        }
    }
}

/*
// В основном цикле обновления
HDDebugUtils.UpdateFpsCounter(deltaTime);

// При загрузке текстуры
HDDebugUtils.RegisterDebugTexture("WeaponHD", weaponTexture);

// Для отрисовки диагностики
HDDebugUtils.DrawDiagnostics(spriteBatch, GUI.SmallFont);

// Создание чекпоинта
HDDebugUtils.CreateMemoryCheckpoint("AfterLevelLoad");

// В HDMain.cs
public void Update(float deltaTime)
{
    HDDebugUtils.UpdateFpsCounter(deltaTime);
    
    if (DebugMode)
    {
        // Дополнительная диагностика
    }
}
*/