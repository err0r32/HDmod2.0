using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Barotrauma;
using Barotrauma.Lights;
using Barotrauma.Sounds;

namespace BarotraumaHD
{
    /// <summary>
    /// Harmony-патчи для модификации UI системы игры
    /// </summary>
    [HarmonyPatch]
    public static class HDUIPatch
    {
        #region Переменные и вспомогательные методы
        
        private static bool _initialized;
        private static FieldInfo _spriteBatchField;
        private static FieldInfo _graphicsDeviceField;
        
        /// <summary>
        /// Инициализация reflection-доступа к приватным полям
        /// </summary>
        private static void InitializeReflection()
        {
            if (_initialized) return;
            
            try
            {
                _spriteBatchField = typeof(GUIStyle).GetField("spriteBatch", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                
                _graphicsDeviceField = typeof(SpriteBatch).GetField("graphicsDevice", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                
                _initialized = true;
            }
            catch (Exception ex)
            {
                HDMod.Error($"Reflection initialization failed: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Основные патчи
        
        /// <summary>
        /// Патч для добавления поддержки масштабирования UI
        /// </summary>
        [HarmonyPatch(typeof(GUI), nameof(GUI.Draw))]
        [HarmonyPrefix]
        private static bool DrawPrefix(SpriteBatch spriteBatch, Sprite sprite, Vector2 pos, Color? color = null)
        {
            if (!HDMod.TextureScalingEnabled) return true;
            
            try
            {
                InitializeReflection();
                
                var scale = HDMod.UIScale;
                var modifiedPos = pos * scale;
                var modifiedScale = sprite.Scale * scale;
                
                spriteBatch.Draw(
                    sprite.Texture,
                    modifiedPos,
                    sprite.SourceRect,
                    color ?? Color.White,
                    sprite.Rotation,
                    sprite.Origin,
                    modifiedScale,
                    sprite.SpriteEffects,
                    sprite.Depth);
                
                return false;
            }
            catch (Exception ex)
            {
                HDMod.Error($"UI Draw patch failed: {ex.Message}");
                return true;
            }
        }
        
        /// <summary>
        /// Патч для обработки измененного масштаба при расчете UI
        /// </summary>
        [HarmonyPatch(typeof(GUI), nameof(GUI.CalculateDimensionsFromString))]
        [HarmonyPostfix]
        private static void CalculateDimensionsPostfix(ref Vector2 __result)
        {
            if (HDMod.TextureScalingEnabled)
            {
                __result *= HDMod.UIScale;
            }
        }
        
        #endregion
        
        #region Патчи для совместимости
        
        /// <summary>
        /// Патч для корректировки позиции курсора
        /// </summary>
        [HarmonyPatch(typeof(PlayerInput), nameof(PlayerInput.MousePosition), MethodType.Getter)]
        [HarmonyPostfix]
        private static void MousePositionPostfix(ref Vector2 __result)
        {
            if (HDMod.TextureScalingEnabled)
            {
                __result /= HDMod.UIScale;
            }
        }
        
        /// <summary>
        /// Патч для обработки кликов в измененном UI
        /// </summary>
        [HarmonyPatch(typeof(GUI), nameof(GUI.MouseOn))]
        [HarmonyPrefix]
        private static bool MouseOnPrefix(Rectangle rect, ref bool __result)
        {
            if (!HDMod.TextureScalingEnabled) return true;
            
            var mousePos = PlayerInput.MousePosition;
            var scaledRect = new Rectangle(
                (int)(rect.X * HDMod.UIScale),
                (int)(rect.Y * HDMod.UIScale),
                (int)(rect.Width * HDMod.UIScale),
                (int)(rect.Height * HDMod.UIScale));
            
            __result = scaledRect.Contains(mousePos);
            return false;
        }
        
        #endregion
        
        #region Патчи производительности
        
        /// <summary>
        /// Оптимизация частых вызовов отрисовки
        /// </summary>
        [HarmonyPatch(typeof(SpriteBatch), nameof(SpriteBatch.Draw))]
        [HarmonyPrefix]
        private static bool SpriteBatchDrawPrefix(SpriteBatch __instance, Texture2D texture)
        {
            if (!HDMod.OptimizeDrawing) return true;
            
            try
            {
                // Пропускаем нулевые текстуры
                if (texture == null || texture.IsDisposed)
                    return false;
                
                // Проверка на повторную отрисовку того же спрайта
                if (__instance == HDUIManager.LastSpriteBatch)
                {
                    if (texture == HDUIManager.LastDrawnTexture && 
                        HDUIManager.FrameCount == HDUIManager.LastDrawFrame)
                        return false;
                }
                
                HDUIManager.LastSpriteBatch = __instance;
                HDUIManager.LastDrawnTexture = texture;
                HDUIManager.LastDrawFrame = HDUIManager.FrameCount;
                
                return true;
            }
            catch (Exception ex)
            {
                HDMod.Error($"Draw optimization failed: {ex.Message}");
                return true;
            }
        }
        
        #endregion
        
        #region Вспомогательные классы
        
        private static class HDUIManager
        {
            public static SpriteBatch LastSpriteBatch;
            public static Texture2D LastDrawnTexture;
            public static int LastDrawFrame;
            public static int FrameCount;
            
            public static void Update()
            {
                FrameCount++;
            }
        }
        
        #endregion
    }
}

/*
Добавить в HDMod.cs:
public static float UIScale = 1.0f;
public static bool TextureScalingEnabled = true;
public static bool OptimizeDrawing = true;

Инициализировать патчи в основном классе:
var harmony = new Harmony("com.barotrauma.hdui");
harmony.PatchAll(typeof(HDUIPatch));
*/