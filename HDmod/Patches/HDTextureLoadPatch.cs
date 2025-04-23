using System;
using System.IO;
using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using Barotrauma;

namespace BarotraumaHD.Patches
{
    /// <summary>
    /// Harmony-патч для перехвата загрузки текстур
    /// </summary>
    [HarmonyPatch(typeof(TextureLoader))]
    [HarmonyPatch(nameof(TextureLoader.FromFile))]
    public static class HDTextureLoadPatch
    {
        private static bool Prefix(ref string file, ref bool compress, ref Texture2D __result)
        {
            try
            {
                // 1. Получаем экземпляр менеджера текстур
                if (!HDServiceLocator.TryGet<HDTextureManager>(out var textureManager) || !textureManager.Enabled)
                {
                    return true; // Продолжить оригинальный метод
                }

                // 2. Проверяем, нужно ли обрабатывать этот путь
                if (!ShouldInterceptTextureLoad(file))
                {
                    return true;
                }

                // 3. Пытаемся загрузить HD-версию
                var hdTexturePath = GetHDTexturePath(file);
                var texture = textureManager.GetTextureAsync(hdTexturePath).Result;

                if (texture != null)
                {
                    __result = texture;
                    HDMod.Log($"Texture redirected: {Path.GetFileName(file)} -> {Path.GetFileName(hdTexturePath)}");
                    return false; // Пропустить оригинальный метод
                }

                return true;
            }
            catch (Exception ex)
            {
                HDMod.Error($"Texture load patch failed: {ex.Message}");
                return true;
            }
        }

        private static bool ShouldInterceptTextureLoad(string filePath)
        {
            // Игнорируем UI и специальные текстуры
            if (filePath.Contains("GUI/", StringComparison.OrdinalIgnoreCase) ||
                filePath.Contains("Effects/", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Обрабатываем только текстуры из Content
            return filePath.StartsWith("Content/", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetHDTexturePath(string originalPath)
        {
            // Преобразуем путь к HD-версии
            var fileName = Path.GetFileName(originalPath);
            var relativePath = originalPath
                .Replace("Content/", "", StringComparison.OrdinalIgnoreCase)
                .Replace(fileName, "", StringComparison.OrdinalIgnoreCase);

            return Path.Combine(HDMod.ModDirectory, relativePath, fileName);
        }

        /// <summary>
        /// Регистрация патча
        /// </summary>
        public static void Register(Harmony harmony)
        {
            try
            {
                var original = typeof(TextureLoader).GetMethod(nameof(TextureLoader.FromFile), new[] { typeof(string), typeof(bool) });
                var prefix = typeof(HDTextureLoadPatch).GetMethod(nameof(Prefix));
                harmony.Patch(original, new HarmonyMethod(prefix));
            }
            catch (Exception ex)
            {
                HDMod.Error($"Failed to register texture load patch: {ex.Message}");
            }
        }
    }
}

/*
Для использования добавьте в инициализацию мода:
HDTextureLoadPatch.Register(yourHarmonyInstance);
*/