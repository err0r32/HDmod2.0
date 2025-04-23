using System;
using System.Collections.Generic;
using System.Linq;
using Barotrauma;

namespace BarotraumaHD
{
    /// <summary>
    /// Система регистрации и управления консольными командами HD-мода
    /// </summary>
    public static class HDCommandRegistry
    {
        private static readonly Dictionary<string, HDCommand> _commands = new Dictionary<string, HDCommand>(StringComparer.OrdinalIgnoreCase);
        private static bool _initialized;

        /// <summary>
        /// Инициализирует все команды мода
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;

            RegisterCoreCommands();
            RegisterTextureCommands();
            RegisterDebugCommands();

            _initialized = true;
            HDMod.Log($"Command system initialized ({_commands.Count} commands registered)");
        }

        #region Регистрация команд

        /// <summary>
        /// Регистрирует новую команду
        /// </summary>
        public static void RegisterCommand(string name, string description, Action<string[]> action, params string[] aliases)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Command name cannot be empty");

            var command = new HDCommand(name, description, action, aliases);

            lock (_commands)
            {
                if (_commands.ContainsKey(command.Name))
                {
                    HDMod.Error($"Command '{command.Name}' is already registered");
                    return;
                }

                _commands.Add(command.Name, command);

                foreach (var alias in command.Aliases)
                {
                    if (!_commands.ContainsKey(alias))
                    {
                        _commands.Add(alias, command);
                    }
                }
            }
        }

        private static void RegisterCoreCommands()
        {
            RegisterCommand("hd_status", 
                "Показывает статус HD-мода", 
                args => {
                    var status = $"HD Mod v{HDMod.VERSION}\n" +
                                $"Textures loaded: {HDTextureManager.Instance.LoadedCount}\n" +
                                $"Cache size: {HDTextureCache.Instance.CurrentSizeMB}MB";
                    DebugConsole.NewMessage(status, Color.Cyan);
                },
                "hd_stats");

            RegisterCommand("hd_toggle",
                "Включает/выключает замену текстур",
                args => {
                    HDTextureManager.Instance.Enabled = !HDTextureManager.Instance.Enabled;
                    DebugConsole.NewMessage($"Texture replacement {(HDTextureManager.Instance.Enabled ? "enabled" : "disabled")}");
                },
                "hd_on", "hd_off");
        }

        private static void RegisterTextureCommands()
        {
            RegisterCommand("hd_reload",
                "Перезагружает все HD-текстуры",
                async args => {
                    DebugConsole.NewMessage("Reloading HD textures...", Color.Yellow);
                    await HDTextureManager.Instance.ReloadAllTextures();
                    DebugConsole.NewMessage("Textures reloaded!", Color.LightGreen);
                });

            RegisterCommand("hd_preload",
                "Предзагружает текстуры для текущей локации",
                async args => {
                    var location = GameMain.GameSession?.Level?.LevelData?.Name;
                    if (string.IsNullOrEmpty(location))
                    {
                        DebugConsole.NewMessage("No active location found", Color.Red);
                        return;
                    }
                    DebugConsole.NewMessage($"Preloading textures for {location}...", Color.Yellow);
                    await HDTextureManager.Instance.PreloadLocationTextures(location);
                });
        }

        private static void RegisterDebugCommands()
        {
            RegisterCommand("hd_debug",
                "Включает/выключает режим отладки",
                args => {
                    HDMod.DebugMode = !HDMod.DebugMode;
                    DebugConsole.NewMessage($"Debug mode {(HDMod.DebugMode ? "enabled" : "disabled")}");
                });

            RegisterCommand("hd_cache",
                "Управление кэшем текстур",
                args => {
                    if (args.Length < 1)
                    {
                        DebugConsole.NewMessage("Usage: hd_cache [clear|stats|size MB]", Color.White);
                        return;
                    }

                    switch (args[0].ToLower())
                    {
                        case "clear":
                            HDTextureCache.Instance.Clear();
                            DebugConsole.NewMessage("Texture cache cleared", Color.LightGreen);
                            break;
                        case "stats":
                            var stats = $"Cached textures: {HDTextureCache.Instance.Count}\n" +
                                       $"Memory usage: {HDTextureCache.Instance.CurrentSizeMB}MB";
                            DebugConsole.NewMessage(stats, Color.Cyan);
                            break;
                        case "size":
                            if (args.Length > 1 && int.TryParse(args[1], out var mb))
                            {
                                HDTextureCache.Instance.SetMaxSizeMB(mb);
                                DebugConsole.NewMessage($"Cache size limit set to {mb}MB", Color.LightGreen);
                            }
                            break;
                    }
                });
        }

        #endregion

        #region Выполнение команд

        /// <summary>
        /// Выполняет команду по имени
        /// </summary>
        public static bool ExecuteCommand(string commandName, string[] args)
        {
            if (!_initialized)
                Initialize();

            lock (_commands)
            {
                if (_commands.TryGetValue(commandName, out var command))
                {
                    try
                    {
                        command.Execute(args);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        HDMod.Error($"Command '{commandName}' failed: {ex.Message}");
                        DebugConsole.NewMessage($"Error executing command: {ex.Message}", Color.Red);
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Получает все зарегистрированные команды (без алиасов)
        /// </summary>
        public static IEnumerable<HDCommand> GetRegisteredCommands()
        {
            lock (_commands)
            {
                return _commands.Values
                    .Distinct()
                    .OrderBy(c => c.Name);
            }
        }

        #endregion

        /// <summary>
        /// Очищает все зарегистрированные команды
        /// </summary>
        public static void Clear()
        {
            lock (_commands)
            {
                _commands.Clear();
                _initialized = false;
            }
        }

        /// <summary>
        /// Представляет зарегистрированную команду
        /// </summary>
        private class HDCommand
        {
            public string Name { get; }
            public string Description { get; }
            public string[] Aliases { get; }
            private readonly Action<string[]> _action;

            public HDCommand(string name, string description, Action<string[]> action, params string[] aliases)
            {
                Name = name.ToLowerInvariant();
                Description = description;
                _action = action ?? throw new ArgumentNullException(nameof(action));
                Aliases = aliases.Select(a => a.ToLowerInvariant()).ToArray();
            }

            public void Execute(string[] args)
            {
                _action.Invoke(args);
            }
        }
    }
}