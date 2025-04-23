using System;
using System.Linq;
using Barotrauma;
using Microsoft.Xna.Framework;

namespace BarotraumaHD
{
    /// <summary>
    /// Консольные команды для управления текстурами HD-мода
    /// </summary>
    public static class HDTextureCommands
    {
        private static HDTextureManager _textureManager;
        private static HDTextureCache _textureCache;

        /// <summary>
        /// Инициализация команд с зависимостями
        /// </summary>
        public static void Initialize(HDTextureManager manager, HDTextureCache cache)
        {
            _textureManager = manager ?? throw new ArgumentNullException(nameof(manager));
            _textureCache = cache ?? throw new ArgumentNullException(nameof(cache));

            RegisterCommands();
        }

        private static void RegisterCommands()
        {
            // Основные команды управления
            RegisterCommand("hd_texture_reload", "Reload all HD textures", ReloadTextures);
            RegisterCommand("hd_texture_cache", "Manage texture cache", CacheCommand);
            RegisterCommand("hd_texture_stats", "Show texture statistics", TextureStats);
            RegisterCommand("hd_texture_toggle", "Toggle texture replacement", ToggleReplacement);
            
            // Команды диагностики
            RegisterCommand("hd_texture_find", "Find texture by name", FindTexture);
            RegisterCommand("hd_texture_verify", "Verify texture integrity", VerifyTextures);
        }

        private static void RegisterCommand(string name, string description, Action<string[]> action)
        {
            if (DebugConsole.Commands.Any(c => c.Name == name))
                return;

            var command = new DebugConsole.Command(name, description, action);
            DebugConsole.Commands.Add(command);
        }

        #region Command Implementations

        private static void ReloadTextures(string[] args)
        {
            try
            {
                _textureCache.Clear();
                _textureManager.ClearCache();
                DebugConsole.NewMessage("All HD textures reload scheduled", Color.Green);
            }
            catch (Exception ex)
            {
                DebugConsole.NewMessage($"Reload failed: {ex.Message}", Color.Red);
            }
        }

        private static void CacheCommand(string[] args)
        {
            if (args.Length == 0)
            {
                ShowCacheInfo();
                return;
            }

            switch (args[0].ToLower())
            {
                case "clear":
                    _textureCache.Clear();
                    DebugConsole.NewMessage("Texture cache cleared", Color.Yellow);
                    break;
                
                case "size":
                    if (args.Length > 1 && int.TryParse(args[1], out var sizeMB))
                    {
                        _textureCache.SetMaxSize(sizeMB);
                        DebugConsole.NewMessage($"Cache size set to {sizeMB}MB", Color.LightGreen);
                    }
                    break;
                
                default:
                    DebugConsole.NewMessage("Invalid cache command. Usage: hd_texture_cache [clear|size MB]", Color.Red);
                    break;
            }
        }

        private static void ShowCacheInfo()
        {
            var info = $@"Texture Cache Information:
Current size: {_textureCache.CurrentCacheSize / (1024 * 1024)} MB
Textures cached: {_textureCache.Count}
Status: {(_textureManager.Enabled ? "Enabled" : "Disabled")}";

            DebugConsole.NewMessage(info, Color.Cyan);
        }

        private static void TextureStats(string[] args)
        {
            var stats = HDTextureAnalyzer.GetStatistics();
            var message = $@"Texture Statistics:
HD textures loaded: {stats.HDTexturesCount}
Vanilla fallbacks: {stats.VanillaTexturesCount}
Memory usage: {stats.TotalMemoryMB} MB
Average resolution: {stats.AverageWidth}x{stats.AverageHeight}";

            DebugConsole.NewMessage(message, Color.LightBlue);
        }

        private static void ToggleReplacement(string[] args)
        {
            _textureManager.Enabled = !_textureManager.Enabled;
            var status = _textureManager.Enabled ? "ENABLED" : "DISABLED";
            DebugConsole.NewMessage($"HD texture replacement {status}", Color.Yellow);
        }

        private static void FindTexture(string[] args)
        {
            if (args.Length < 1)
            {
                DebugConsole.NewMessage("Usage: hd_texture_find <name_part>", Color.Red);
                return;
            }

            var searchTerm = args[0].ToLower();
            var results = _textureCache.FindTextures(searchTerm);

            if (!results.Any())
            {
                DebugConsole.NewMessage("No matching textures found", Color.Orange);
                return;
            }

            DebugConsole.NewMessage($"Found {results.Count} textures:", Color.LightGreen);
            foreach (var texture in results)
            {
                DebugConsole.NewMessage($"- {texture}", Color.White);
            }
        }

        private static void VerifyTextures(string[] args)
        {
            var corruptTextures = HDTextureVerifier.CheckTextures();
            if (!corruptTextures.Any())
            {
                DebugConsole.NewMessage("All textures verified successfully", Color.Green);
                return;
            }

            DebugConsole.NewMessage($"Found {corruptTextures.Count} issues:", Color.Red);
            foreach (var issue in corruptTextures)
            {
                DebugConsole.NewMessage($"- {issue.TexturePath}: {issue.Error}", Color.Orange);
            }
        }

        #endregion
    }

    /// <summary>
    /// Вспомогательный класс для анализа текстур (заглушка)
    /// </summary>
    public static class HDTextureAnalyzer
    {
        public static TextureStats GetStatistics() => new TextureStats();
    }

    /// <summary>
    /// Вспомогательный класс для проверки текстур (заглушка)
    /// </summary>
    public static class HDTextureVerifier
    {
        public static System.Collections.Generic.List<TextureIssue> CheckTextures() => new System.Collections.Generic.List<TextureIssue>();
    }

    public struct TextureStats
    {
        public int HDTexturesCount = 0;
        public int VanillaTexturesCount = 0;
        public float TotalMemoryMB = 0;
        public int AverageWidth = 0;
        public int AverageHeight = 0;
    }

    public struct TextureIssue
    {
        public string TexturePath;
        public string Error;
    }
}