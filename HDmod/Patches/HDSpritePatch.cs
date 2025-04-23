using System;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using Barotrauma;

namespace BarotraumaHD
{
    /// <summary>
    /// Патчи для замены оригинальных спрайтов на HD версии
    /// </summary>
    [HarmonyPatch]
    public static class HDSpritePatch
    {
        private static HDTextureManager _textureManager;

        public static void Initialize(HDTextureManager textureManager)
        {
            _textureManager = textureManager ?? throw new ArgumentNullException(nameof(textureManager));
            HDMod.Log("Sprite patches initialized");
        }

        [HarmonyPatch(typeof(Sprite), nameof(Sprite.Load))]
        [HarmonyPrefix]
        private static bool LoadPrefix(ref string file, bool preloadOnly, ref Sprite __result)
        {
            if (!_textureManager.Enabled || string.IsNullOrEmpty(file))
                return true;

            try
            {
                var hdTexture = _textureManager.GetTextureAsync(file, preloadOnly).Result;
                if (hdTexture != null)
                {
                    __result = CreateHDSprite(file, hdTexture);
                    return false; // Пропускаем оригинальный метод
                }
            }
            catch (Exception ex)
            {
                HDMod.Error($"Failed to patch sprite {file}: {ex.Message}");
            }

            return true;
        }

        private static Sprite CreateHDSprite(string file, Texture2D texture)
        {
            // Сохраняем оригинальные параметры спрайта
            var originalSprite = new Sprite(file);
            
            // Создаем HD спрайт с теми же параметрами, но новой текстурой
            return new Sprite(
                file: file,
                texture: texture,
                sourceRectangle: originalSprite.SourceRect,
                offset: originalSprite.Offset,
                newSpriteSheetSize: originalSprite.size
            )
            {
                Depth = originalSprite.Depth,
                Smoothing = originalSprite.Smooth
            };
        }

        [HarmonyPatch(typeof(Sprite), nameof(Sprite.Remove))]
        [HarmonyPostfix]
        private static void RemovePostfix(Sprite __instance)
        {
            // Дополнительная очистка ресурсов при удалении спрайта
            if (__instance?.Texture != null)
            {
                // Помечаем текстуру как кандидат на выгрузку из кэша
                _textureManager.MarkTextureForCleanup(__instance.Texture);
            }
        }
    }
}

/*
Дополнение для HDTextureManager.cs (необходимые изменения):
{
    // ... существующий код ...

    public void MarkTextureForCleanup(Texture2D texture)
    {
        if (texture == null) return;
        
        // Логика пометки текстуры для последующей очистки
        _cache.MarkForCleanup(texture.Name);
    }
}

Дополнение для HDTextureCache.cs
public class HDTextureCache : IDisposable
{
    // ... существующий код ...

    public void MarkForCleanup(string texturePath)
    {
        lock (_cacheLock)
        {
            if (_lruCache.TryGetValue(texturePath, out var entry))
            {
                entry.LastAccessTime = DateTime.MinValue; // Помечаем как старую
                EnforceCacheSizeLimit();
            }
        }
    }
}

Для активации патчей добавьте в HDMain.cs
public void Initialize()
{
    // ... инициализация других компонентов ...
    
    var textureManager = new HDTextureManager(cache, loader);
    HDSpritePatch.Initialize(textureManager);
    new Harmony("com.barotrauma.hdmod").PatchAll();
}
*/