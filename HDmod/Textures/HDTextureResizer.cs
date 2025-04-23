using System;
using Microsoft.Xna.Framework.Graphics;
using Barotrauma;

namespace BarotraumaHD
{
    /// <summary>
    /// Система автоматического масштабирования текстур под оригинальные размеры спрайтов
    /// </summary>
    public static class HDTextureResizer
    {
        /// <summary>
        /// Коэффициент увеличения разрешения (2x для HD текстур)
        /// </summary>
        public const float RESOLUTION_SCALE = 2.0f;

        /// <summary>
        /// Масштабирует текстуру под оригинальные размеры спрайта
        /// </summary>
        public static Texture2D ApplyScaling(Texture2D texture, Sprite originalSprite)
        {
            if (texture == null || originalSprite == null) 
                return texture;

            try
            {
                // Рассчитываем целевой размер с сохранением пропорций
                var targetSize = CalculateTargetSize(texture, originalSprite);
                
                // Если размер уже подходящий - возвращаем как есть
                if (texture.Width == targetSize.X && texture.Height == targetSize.Y)
                    return texture;

                // Создаем новую текстуру с правильными размерами
                var scaledTexture = ScaleTexture(texture, targetSize);
                
                // Освобождаем оригинальную текстуру, если она была временной
                if (!texture.IsDisposed)
                    texture.Dispose();

                return scaledTexture;
            }
            catch (Exception ex)
            {
                HDMod.Error($"Texture scaling failed: {ex.Message}");
                return texture;
            }
        }

        /// <summary>
        /// Рассчитывает целевой размер с сохранением пропорций
        /// </summary>
        private static Point CalculateTargetSize(Texture2D texture, Sprite originalSprite)
        {
            // Получаем оригинальный размер спрайта в пикселях
            var originalSize = originalSprite.size;
            
            // Рассчитываем соотношение сторон оригинальной текстуры
            float originalRatio = originalSprite.SourceRect.Width / (float)originalSprite.SourceRect.Height;
            float newRatio = texture.Width / (float)texture.Height;

            // Корректируем размеры для сохранения пропорций
            if (Math.Abs(originalRatio - newRatio) > 0.01f)
            {
                HDMod.Log($"Adjusting texture ratio for {originalSprite.FilePath}");
                if (newRatio > originalRatio)
                {
                    return new Point(
                        (int)(originalSize.Y * newRatio * RESOLUTION_SCALE),
                        (int)(originalSize.Y * RESOLUTION_SCALE)
                    );
                }
                else
                {
                    return new Point(
                        (int)(originalSize.X * RESOLUTION_SCALE),
                        (int)(originalSize.X / newRatio * RESOLUTION_SCALE)
                    );
                }
            }

            // Стандартное масштабирование
            return new Point(
                (int)(originalSize.X * RESOLUTION_SCALE),
                (int)(originalSize.Y * RESOLUTION_SCALE)
            );
        }

        /// <summary>
        /// Масштабирует текстуру с использованием билинейной фильтрации
        /// </summary>
        private static Texture2D ScaleTexture(Texture2D original, Point targetSize)
        {
            // Реализация упрощенного масштабирования через RenderTarget
            var graphicsDevice = GameMain.Instance.GraphicsDevice;
            var renderTarget = new RenderTarget2D(
                graphicsDevice,
                targetSize.X,
                targetSize.Y,
                false,
                SurfaceFormat.Color,
                DepthFormat.None);

            // Сохраняем текущий RenderTarget
            var previousTargets = graphicsDevice.GetRenderTargets();
            
            try
            {
                graphicsDevice.SetRenderTarget(renderTarget);
                graphicsDevice.Clear(Color.Transparent);

                // Простое масштабирование через SpriteBatch
                using (var spriteBatch = new SpriteBatch(graphicsDevice))
                {
                    spriteBatch.Begin(
                        SpriteSortMode.Immediate,
                        BlendState.AlphaBlend,
                        SamplerState.LinearClamp,
                        DepthStencilState.None,
                        RasterizerState.CullNone);
                    
                    spriteBatch.Draw(
                        original,
                        new Rectangle(0, 0, targetSize.X, targetSize.Y),
                        Color.White);
                    
                    spriteBatch.End();
                }

                // Копируем данные в новую текстуру
                var result = new Texture2D(graphicsDevice, targetSize.X, targetSize.Y);
                Color[] data = new Color[targetSize.X * targetSize.Y];
                renderTarget.GetData(data);
                result.SetData(data);

                return result;
            }
            finally
            {
                // Восстанавливаем RenderTarget
                graphicsDevice.SetRenderTargets(previousTargets);
                renderTarget.Dispose();
            }
        }

        /// <summary>
        /// Проверяет, требуется ли масштабирование для текстуры
        /// </summary>
        public static bool NeedsScaling(Texture2D texture, Sprite originalSprite)
        {
            if (texture == null || originalSprite == null)
                return false;

            var targetSize = CalculateTargetSize(texture, originalSprite);
            return texture.Width != targetSize.X || texture.Height != targetSize.Y;
        }
    }
}