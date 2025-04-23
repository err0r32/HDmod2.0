using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using Barotrauma;
using Microsoft.Xna.Framework.Graphics;

namespace BarotraumaHD
{
    /// <summary>
    /// Система возврата к оригинальным текстурам игры при отсутствии HD-версий
    /// </summary>
    public sealed class HDVanillaFallback : IDisposable
    {
        private readonly ConcurrentDictionary<string, Texture2D> _vanillaTextures = new ConcurrentDictionary<string, Texture2D>();
        private readonly HDTextureLoader _loader;
        private bool _disposed;

        public HDVanillaFallback(HDTextureLoader loader)
        {
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));
        }

        /// <summary>
        /// Получает оригинальную текстуру игры с кэшированием
        /// </summary>
        public async Task<Texture2D> GetVanillaTextureAsync(string hdTexturePath)
        {
            if (_disposed) 
                throw new ObjectDisposedException(nameof(HDVanillaFallback));

            if (string.IsNullOrEmpty(hdTexturePath))
                return null;

            try
            {
                // Проверка кэша
                if (_vanillaTextures.TryGetValue(hdTexturePath, out var cachedTexture))
                    return cachedTexture;

                // Получение пути к оригинальной текстуре
                var vanillaPath = GetVanillaTexturePath(hdTexturePath);
                if (string.IsNullOrEmpty(vanillaPath))
                    return null;

                // Асинхронная загрузка
                var texture = await _loader.LoadAsync(vanillaPath, cache: false);
                if (texture == null)
                    return null;

                // Кэширование
                _vanillaTextures[hdTexturePath] = texture;
                return texture;
            }
            catch (Exception ex)
            {
                HDMod.Error($"Failed to load vanilla texture for {hdTexturePath}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Преобразует путь HD-текстуры в путь оригинальной текстуры
        /// </summary>
        public string GetVanillaTexturePath(string hdTexturePath)
        {
            if (string.IsNullOrEmpty(hdTexturePath))
                return null;

            try
            {
                // Базовый пример преобразования:
                // "Content/HD/Items/tool.png" -> "Content/Items/tool.png"
                var pathParts = hdTexturePath.Split(Path.DirectorySeparatorChar);
                if (pathParts.Length < 2 || !pathParts[0].Equals("Content"))
                    return null;

                // Удаляем "HD" из пути
                var vanillaParts = pathParts.Where(p => !p.Equals("HD", StringComparison.OrdinalIgnoreCase)).ToArray();
                return Path.Combine(vanillaParts);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Очищает кэш оригинальных текстур
        /// </summary>
        public void ClearCache()
        {
            foreach (var texture in _vanillaTextures.Values)
            {
                try
                {
                    texture?.Dispose();
                }
                catch (Exception ex)
                {
                    HDMod.Error($"Error disposing vanilla texture: {ex.Message}");
                }
            }
            _vanillaTextures.Clear();
        }

        public void Dispose()
        {
            if (_disposed) 
                return;

            ClearCache();
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        ~HDVanillaFallback()
        {
            Dispose();
        }
    }
}
/*
var loader = new HDTextureLoader(cache);
var fallback = new HDVanillaFallback(loader);

// Получение оригинальной текстуры
var vanillaTexture = await fallback.GetVanillaTextureAsync("Content/HD/Items/tool.png");

// Очистка
fallback.Dispose();
*/