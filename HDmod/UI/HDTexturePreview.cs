using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Barotrauma;

namespace BarotraumaHD
{
    /// <summary>
    /// Компонент для сравнения оригинальных и HD текстур с возможностью переключения
    /// </summary>
    public class HDTexturePreview : GUIComponent
    {
        private Texture2D _originalTexture;
        private Texture2D _hdTexture;
        private bool _showHd = true;
        private float _zoom = 1.0f;
        private Rectangle _drawArea;
        private Point _textureSize;
        private float _switchCooldown;

        public float SwitchInterval { get; set; } = 2.0f;
        public bool AutoSwitch { get; set; } = true;
        public Color BorderColor { get; set; } = Color.White;
        public int BorderThickness { get; set; } = 2;

        public HDTexturePreview(RectTransform rectT, Texture2D original, Texture2D hd) 
            : base(rectT)
        {
            _originalTexture = original ?? throw new ArgumentNullException(nameof(original));
            _hdTexture = hd ?? throw new ArgumentNullException(nameof(hd));
            
            UpdateTextureSize();
            RecalculateDrawArea();
        }

        private void UpdateTextureSize()
        {
            var currentTexture = _showHd ? _hdTexture : _originalTexture;
            _textureSize = new Point(currentTexture.Width, currentTexture.Height);
        }

        private void RecalculateDrawArea()
        {
            // Центрируем текстуру с учетом масштаба
            var scaledWidth = (int)(_textureSize.X * _zoom);
            var scaledHeight = (int)(_textureSize.Y * _zoom);
            
            _drawArea = new Rectangle(
                Rect.Center.X - scaledWidth / 2,
                Rect.Center.Y - scaledHeight / 2,
                scaledWidth,
                scaledHeight);
        }

        public void SetZoom(float zoomLevel)
        {
            _zoom = MathHelper.Clamp(zoomLevel, 0.1f, 5.0f);
            RecalculateDrawArea();
        }

        public void ToggleTexture()
        {
            _showHd = !_showHd;
            UpdateTextureSize();
            RecalculateDrawArea();
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            if (AutoSwitch)
            {
                _switchCooldown -= deltaTime;
                if (_switchCooldown <= 0)
                {
                    ToggleTexture();
                    _switchCooldown = SwitchInterval;
                }
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!Visible) return;

            // Рисуем текстуру
            var texture = _showHd ? _hdTexture : _originalTexture;
            spriteBatch.Draw(
                texture,
                _drawArea,
                null,
                Color.White,
                0f,
                Vector2.Zero,
                SpriteEffects.None,
                0.1f);

            // Рамка
            if (BorderThickness > 0)
            {
                GUI.DrawRectangle(
                    spriteBatch,
                    new Rectangle(
                        _drawArea.X - BorderThickness,
                        _drawArea.Y - BorderThickness,
                        _drawArea.Width + BorderThickness * 2,
                        _drawArea.Height + BorderThickness * 2),
                    BorderColor,
                    isFilled: false);
            }

            // Подпись
            string label = _showHd ? "HD VERSION" : "ORIGINAL";
            var labelPos = new Vector2(_drawArea.X, _drawArea.Y - 20);
            GUI.Font.DrawString(
                spriteBatch,
                label,
                labelPos,
                _showHd ? Color.Lime : Color.Yellow,
                0f,
                Vector2.Zero,
                1.0f,
                SpriteEffects.None,
                0f);

            base.Draw(spriteBatch);
        }

        protected override void Dispose(bool disposing)
        {
            // Не освобождаем текстуры - они управляются извне
            base.Dispose(disposing);
        }
    }
}