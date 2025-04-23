using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Barotrauma;
using Microsoft.Xna.Framework.Graphics;

namespace BarotraumaHD
{
    /// <summary>
    /// Менеджер управления HD-текстурами с приоритетной загрузкой и автоматическим масштабированием
    /// </summary>
    public sealed class HDTextureManager : IDisposable
    {
        private readonly HDTextureCache _cache;
        private readonly HDTextureLoader _loader;
        private readonly Dictionary<string, Texture2D> _vanillaFallbacks = new Dictionary<string, Texture2D>();
        private bool _disposed;
        private bool _enabled = true;

        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled != value)
                {
                    _enabled = value;
                    HDMod.Log($"Texture replacement {(_enabled ? "enabled" : "disabled")}");
                }
            }
        }

        public HDTextureManager(HDTextureCache cache, HDTextureLoader loader)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));
        }

        /// <summary>
        /// Основной метод получения текстуры с автоматическим fallback
        /// </summary>
        public async Task<Texture2D> GetTextureAsync(string texturePath, bool preloadOnly = false)
        {
            if (!_enabled || string.IsNullOrEmpty(texturePath))
                return await GetVanillaTextureAsync(texturePath);

            try
            {
                // 1. Попытка загрузки HD текстуры
                if (_loader.TextureExists(texturePath))
                {
                    var hdTexture = await _loader.LoadAsync(texturePath);
                    if (hdTexture != null && !preloadOnly)
                    {
                        return ApplyScaling(hdTexture);
                    }
                    return null; // Для предзагрузки
                }

                // 2. Fallback на оригинальную текстуру
                return await GetVanillaTextureAsync(texturePath, preloadOnly);
            }
            catch (Exception ex)
            {
                HDMod.Error($"Failed to load texture {texturePath}: {ex.Message}");
                return await GetVanillaTextureAsync(texturePath);
            }
        }

        private async Task<Texture2D> GetVanillaTextureAsync(string texturePath, bool preloadOnly = false)
        {
            if (string.IsNullOrEmpty(texturePath)) return null;

            try
            {
                if (_vanillaFallbacks.TryGetValue(texturePath, out var cached))
                    return cached;

                var vanillaPath = _loader.GetOriginalTexturePath(texturePath);
                var texture = await _loader.LoadAsync(vanillaPath, cache: false);

                if (texture != null && !preloadOnly)
                {
                    _vanillaFallbacks[texturePath] = texture;
                    return texture;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Пакетная предзагрузка текстур для уровня
        /// </summary>
        public async Task PreloadTexturesForLocation(string locationIdentifier)
		{
			if (!_enabled || string.IsNullOrEmpty(locationIdentifier)) 
				return;

			try
			{
				var textureList = GetTexturesForLocation(locationIdentifier)?.ToList();
				if (textureList?.Any() != true) return;

				var loadTasks = textureList.Select(path => 
					_loader.LoadAsync(path, cache: true)
				).ToArray();

				await Task.WhenAll(loadTasks);
			}
			catch (Exception ex)
			{
				HDMod.Error($"Preload failed for {locationIdentifier}: {ex.Message}");
			}
		}

        private IEnumerable<string> GetTexturesForLocation(string locationId)
        {
            // Логика определения текстур для локации
            return Directory.EnumerateFiles(Path.Combine("Content/Locations", locationId, "HD"))
                .Where(f => f.EndsWith(".png") || f.EndsWith(".dds"));
        }

        private Texture2D ApplyScaling(Texture2D texture)
        {
            // Автоматическое масштабирование под оригинальные размеры
            return texture; // Заглушка - реальная реализация будет в отдельном классе
        }

        public void ClearCache()
        {
            _cache.Clear();
            _vanillaFallbacks.Clear();
        }

        public void Dispose()
        {
            if (_disposed) return;
            
            ClearCache();
            _loader.Dispose();
            _disposed = true;
        }
    }
}