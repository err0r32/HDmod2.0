using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using Barotrauma;

namespace BarotraumaHD
{
    /// <summary>
    /// Кэш текстур с LRU-алгоритмом вытеснения и асинхронной загрузкой
    /// </summary>
    public sealed class HDTextureCache : IDisposable
    {
        #region Структуры данных
        
        private class CacheEntry
        {
            public Texture2D Texture;
            public DateTime LastAccessTime;
            public long SizeBytes;
            public bool IsPermanent;
        }

        #endregion

        #region Настройки
        
        /// <summary> Максимальный размер кэша в мегабайтах </summary>
        public const int DEFAULT_MAX_CACHE_SIZE_MB = 2048; // 2GB
        private long _maxCacheSizeBytes = DEFAULT_MAX_CACHE_SIZE_MB * 1024L * 1024L;
        
        /// <summary> Текущий размер кэша в байтах </summary>
        public long CurrentCacheSize { get; private set; }
        
        /// <summary> Количество элементов в кэше </summary>
        public int Count => _lruCache.Count;

        #endregion

        #region Поля
        
        private readonly Dictionary<string, CacheEntry> _lruCache = new Dictionary<string, CacheEntry>();
        private readonly object _cacheLock = new object();
        private bool _isDisposed;

        #endregion

        #region Публичные методы

        /// <summary>
        /// Устанавливает максимальный размер кэша
        /// </summary>
        /// <param name="maxSizeMB">Размер в мегабайтах</param>
        public void SetMaxSize(int maxSizeMB)
        {
            lock (_cacheLock)
            {
                _maxCacheSizeBytes = maxSizeMB * 1024L * 1024L;
                EnforceCacheSizeLimit();
            }
        }

        /// <summary>
        /// Добавляет текстуру в кэш
        /// </summary>
        /// <param name="texturePath">Путь к текстуре</param>
        /// <param name="texture">Объект текстуры</param>
        /// <param name="isPermanent">Не вытеснять из кэша</param>
        public void AddTexture(string texturePath, Texture2D texture, bool isPermanent = false)
        {
            if (string.IsNullOrEmpty(texturePath))
                throw new ArgumentNullException(nameof(texturePath));
            if (texture == null)
                throw new ArgumentNullException(nameof(texture));
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(HDTextureCache));

            lock (_cacheLock)
            {
                if (_lruCache.ContainsKey(texturePath))
                {
                    HDMod.Log($"Текстура уже в кэше: {texturePath}");
                    return;
                }

                var entry = new CacheEntry
                {
                    Texture = texture,
                    LastAccessTime = DateTime.Now,
                    SizeBytes = CalculateTextureSize(texture),
                    IsPermanent = isPermanent
                };

                _lruCache.Add(texturePath, entry);
                CurrentCacheSize += entry.SizeBytes;

                HDMod.Log($"Текстура добавлена в кэш: {texturePath} ({entry.SizeBytes / 1024} KB)");
                EnforceCacheSizeLimit();
            }
        }

        /// <summary>
        /// Асинхронно загружает и кэширует текстуру
        /// </summary>
        public async Task<Texture2D> LoadTextureAsync(string texturePath, bool isPermanent = false)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(HDTextureCache));

            // Проверка кэша в синхронном режиме
            if (TryGetTexture(texturePath, out var cachedTexture))
                return cachedTexture;

            // Асинхронная загрузка
            return await Task.Run(() =>
            {
                try
                {
                    var texture = TextureLoader.FromFile(texturePath);
                    AddTexture(texturePath, texture, isPermanent);
                    return texture;
                }
                catch (Exception ex)
                {
                    HDMod.Error($"Ошибка загрузки текстуры {texturePath}: {ex.Message}");
                    throw;
                }
            });
        }

        /// <summary>
        /// Пытается получить текстуру из кэша
        /// </summary>
        public bool TryGetTexture(string texturePath, out Texture2D texture)
        {
            texture = null;
            if (_isDisposed || string.IsNullOrEmpty(texturePath))
                return false;

            lock (_cacheLock)
            {
                if (_lruCache.TryGetValue(texturePath, out var entry))
                {
                    entry.LastAccessTime = DateTime.Now;
                    texture = entry.Texture;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Очищает кэш, освобождая ресурсы
        /// </summary>
        /// <param name="force">Очистить даже постоянные текстуры</param>
        public void Clear(bool force = false)
        {
            lock (_cacheLock)
            {
                foreach (var entry in _lruCache.Values)
                {
                    if (force || !entry.IsPermanent)
                    {
                        entry.Texture?.Dispose();
                        HDMod.Log($"Текстура выгружена: {entry.Texture?.Name}");
                    }
                }

                _lruCache.Clear();
                CurrentCacheSize = 0;
            }
        }

        #endregion

        #region Приватные методы

        private long CalculateTextureSize(Texture2D texture)
		{
			try
			{
				return texture?.Width * texture?.Height * 4L ?? 0L;
			}
			catch
			{
				return 0L;
			}
		}

        private void EnforceCacheSizeLimit()
        {
            while (CurrentCacheSize > _maxCacheSizeBytes && _lruCache.Count > 0)
            {
                // Находим наименее используемую непостоянную текстуру
                var oldest = _lruCache
                    .Where(x => !x.Value.IsPermanent)
                    .OrderBy(x => x.Value.LastAccessTime)
                    .FirstOrDefault();

                if (oldest.Value == null) break;

                CurrentCacheSize -= oldest.Value.SizeBytes;
                oldest.Value.Texture?.Dispose();
                _lruCache.Remove(oldest.Key);

                HDMod.Log($"Текстура вытеснена из кэша: {oldest.Key} (Размер: {CurrentCacheSize / (1024 * 1024)} MB)");
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_isDisposed) return;

            lock (_cacheLock)
            {
                Clear(true);
                _isDisposed = true;
            }

            HDMod.Log("Кэш текстур освобожден");
        }

        #endregion
    }
}