using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using Barotrauma;

namespace BarotraumaHD
{
    /// <summary>
    /// Загрузчик текстур с поддержкой DDS, асинхронной загрузкой и кэшированием
    /// </summary>
    public sealed class HDTextureLoader : IDisposable
    {
        private readonly HDTextureCache _textureCache;
        private bool _disposed;

        /// <summary>
        /// Инициализирует загрузчик с указанным кэшем
        /// </summary>
        public HDTextureLoader(HDTextureCache textureCache)
        {
            _textureCache = textureCache ?? throw new ArgumentNullException(nameof(textureCache));
        }

        /// <summary>
        /// Асинхронно загружает текстуру с автоматическим определением формата
        /// </summary>
        public async Task<Texture2D> LoadAsync(string texturePath, bool cache = true)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(HDTextureLoader));
            if (string.IsNullOrEmpty(texturePath)) throw new ArgumentNullException(nameof(texturePath));

            // 1. Проверка кэша
            if (_textureCache.TryGetTexture(texturePath, out var cachedTexture))
                return cachedTexture;

            // 2. Определение формата
            var isDds = texturePath.EndsWith(".dds", StringComparison.OrdinalIgnoreCase);

            // 3. Асинхронная загрузка
            var texture = await Task.Run(() => 
            {
                try
                {
                    return isDds ? 
                        LoadDdsTexture(texturePath) : 
                        TextureLoader.FromFile(texturePath);
                }
                catch (Exception ex)
                {
                    HDMod.Error($"Ошибка загрузки текстуры {texturePath}: {ex.Message}");
                    throw;
                }
            });

            // 4. Кэширование
            if (cache && texture != null)
            {
                _textureCache.AddTexture(texturePath, texture);
            }

            return texture;
        }

        /// <summary>
        /// Загружает DDS текстуру с помощью собственного парсера
        /// </summary>
        public Texture2D LoadDdsTexture(string path)
		{
			if (!File.Exists(path)) 
			{
				HDMod.Error($"DDS file not found: {path}");
				return null;
			}

			try
			{
				using var stream = TitleContainer.OpenStream(path);
				return TextureLoader.FromStream(stream);
			}
			catch (Exception ex)
			{
				HDMod.Error($"DDS load error ({path}): {ex.Message}");
				return null;
			}
		}

        /// <summary>
        /// Синхронная загрузка текстуры (использовать только при необходимости)
        /// </summary>
        public Texture2D Load(string texturePath, bool cache = true)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(HDTextureLoader));
            if (string.IsNullOrEmpty(texturePath)) throw new ArgumentNullException(nameof(texturePath));

            if (_textureCache.TryGetTexture(texturePath, out var cachedTexture))
                return cachedTexture;

            Texture2D texture = null;
            var isDds = texturePath.EndsWith(".dds", StringComparison.OrdinalIgnoreCase);

            try
            {
                texture = isDds ? 
                    LoadDdsTexture(texturePath) : 
                    TextureLoader.FromFile(texturePath);

                if (cache && texture != null)
                {
                    _textureCache.AddTexture(texturePath, texture);
                }
            }
            catch (Exception ex)
            {
                HDMod.Error($"Ошибка загрузки {texturePath}: {ex.Message}");
                throw;
            }

            return texture;
        }

        /// <summary>
        /// Проверяет существование файла текстуры
        /// </summary>
        public bool TextureExists(string texturePath)
        {
            if (string.IsNullOrEmpty(texturePath)) return false;
            
            // Проверка HD версии
            if (File.Exists(texturePath)) return true;
            
            // Проверка оригинальной текстуры
            var originalPath = GetOriginalTexturePath(texturePath);
            return File.Exists(originalPath);
        }

        /// <summary>
        /// Возвращает путь к оригинальной текстуре
        /// </summary>
        public string GetOriginalTexturePath(string hdTexturePath)
        {
            // Логика поиска оригинальной текстуры
            var fileName = Path.GetFileName(hdTexturePath);
            return Path.Combine("Content", fileName);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            // Кэш очищается отдельно
        }
    }
}