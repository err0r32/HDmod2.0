using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Barotrauma;

namespace BarotraumaHD
{
    /// <summary>
    /// Компонент выбора цвета с поддержкой RGBA/HSV и предустановками
    /// </summary>
    public class HDColorPicker : GUIComponent
    {
        #region Константы
        private const int PICKER_SIZE = 200;
        private const int HUE_BAR_WIDTH = 30;
        private const int SLIDER_HEIGHT = 20;
        private const int PRESET_COUNT = 8;
        #endregion

        #region Поля
        private Texture2D _colorPickerTexture;
        private Texture2D _hueBarTexture;
        private Rectangle _pickerArea;
        private Rectangle _hueBarArea;
        private Rectangle _alphaSliderArea;
        private Rectangle[] _presetAreas = new Rectangle[PRESET_COUNT];
        
        private Color _currentColor = Color.White;
        private float _hue = 0f;
        private float _saturation = 1f;
        private float _value = 1f;
        private float _alpha = 1f;
        
        private bool _isDraggingPicker;
        private bool _isDraggingHue;
        private bool _isDraggingAlpha;
        #endregion

        #region События
        public event Action<Color> OnColorChanged;
        #endregion

        #region Свойства
        public Color CurrentColor
        {
            get => _currentColor;
            set
            {
                if (_currentColor != value)
                {
                    _currentColor = value;
                    ColorToHSV(_currentColor, out _hue, out _saturation, out _value);
                    _alpha = _currentColor.A / 255f;
                    UpdateTextures();
                    OnColorChanged?.Invoke(_currentColor);
                }
            }
        }
        #endregion

        public HDColorPicker(RectTransform rectT) : base(rectT)
        {
            CreateTextures();
            CalculateAreas();
            
            // Инициализация предустановленных цветов
            for (int i = 0; i < PRESET_COUNT; i++)
            {
                _presetAreas[i] = new Rectangle(
                    (int)rectT.Rect.X + 10 + i * 30,
                    (int)rectT.Rect.Y + 250,
                    25, 25);
            }
        }

        #region Основные методы
        private void CreateTextures()
        {
            _colorPickerTexture = CreateColorPickerTexture();
            _hueBarTexture = CreateHueBarTexture();
        }

        private void CalculateAreas()
        {
            var rect = RectTransform.Rect;
            _pickerArea = new Rectangle(
                (int)rect.X + 10,
                (int)rect.Y + 10,
                PICKER_SIZE,
                PICKER_SIZE);
            
            _hueBarArea = new Rectangle(
                _pickerArea.Right + 10,
                _pickerArea.Top,
                HUE_BAR_WIDTH,
                PICKER_SIZE);
            
            _alphaSliderArea = new Rectangle(
                _pickerArea.Left,
                _pickerArea.Bottom + 10,
                PICKER_SIZE,
                SLIDER_HEIGHT);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!Visible) return;

            // Отрисовка основного цветового поля
            spriteBatch.Draw(_colorPickerTexture, _pickerArea, Color.White);
            
            // Отрисовка ползунка насыщенности/яркости
            DrawPickerCursor(spriteBatch);
            
            // Отрисовка полосы выбора оттенка
            spriteBatch.Draw(_hueBarTexture, _hueBarArea, Color.White);
            
            // Отрисовка ползунка оттенка
            DrawHueCursor(spriteBatch);
            
            // Отрисовка слайдера прозрачности
            DrawAlphaSlider(spriteBatch);
            
            // Отрисовка предустановленных цветов
            DrawPresets(spriteBatch);
            
            // Отрисовка текущего цвета
            DrawCurrentColor(spriteBatch);
        }

        public override void Update(float deltaTime)
        {
            if (!Visible) return;

            var mousePos = PlayerInput.MousePosition;
            bool leftClicked = PlayerInput.LeftButtonHeld();

            // Обработка перетаскивания в основном поле цвета
            if (_pickerArea.Contains(mousePos) && leftClicked)
            {
                _isDraggingPicker = true;
                UpdateFromPickerPosition(mousePos);
            }
            
            // Обработка перетаскивания на полосе оттенка
            if (_hueBarArea.Contains(mousePos) && leftClicked)
            {
                _isDraggingHue = true;
                UpdateHueFromPosition(mousePos);
            }
            
            // Обработка перетаскивания слайдера прозрачности
            if (_alphaSliderArea.Contains(mousePos) && leftClicked)
            {
                _isDraggingAlpha = true;
                UpdateAlphaFromPosition(mousePos);
            }
            
            // Обработка выбора предустановленного цвета
            for (int i = 0; i < PRESET_COUNT; i++)
            {
                if (_presetAreas[i].Contains(mousePos) && PlayerInput.LeftButtonClicked())
                {
                    CurrentColor = GetPresetColor(i);
                    break;
                }
            }

            // Сброс состояния перетаскивания
            if (!leftClicked)
            {
                _isDraggingPicker = false;
                _isDraggingHue = false;
                _isDraggingAlpha = false;
            }
        }
        #endregion

        #region Методы обновления состояния
        private void UpdateFromPickerPosition(Point mousePos)
        {
            float x = MathHelper.Clamp((mousePos.X - _pickerArea.X) / (float)_pickerArea.Width, 0f, 1f);
            float y = MathHelper.Clamp((mousePos.Y - _pickerArea.Y) / (float)_pickerArea.Height, 0f, 1f);
            
            _saturation = x;
            _value = 1f - y;
            UpdateCurrentColorFromHSV();
        }

        private void UpdateHueFromPosition(Point mousePos)
        {
            _hue = MathHelper.Clamp((mousePos.Y - _hueBarArea.Y) / (float)_hueBarArea.Height, 0f, 1f);
            UpdateCurrentColorFromHSV();
            UpdateTextures();
        }

        private void UpdateAlphaFromPosition(Point mousePos)
        {
            _alpha = MathHelper.Clamp((mousePos.X - _alphaSliderArea.X) / (float)_alphaSliderArea.Width, 0f, 1f);
            UpdateCurrentColorFromHSV();
        }

        private void UpdateCurrentColorFromHSV()
        {
            Color newColor = ColorFromHSV(_hue, _saturation, _value);
            newColor.A = (byte)(_alpha * 255);
            
            if (_currentColor != newColor)
            {
                _currentColor = newColor;
                OnColorChanged?.Invoke(_currentColor);
            }
        }
        #endregion

        #region Вспомогательные методы отрисовки
        private void DrawPickerCursor(SpriteBatch spriteBatch)
        {
            int x = _pickerArea.X + (int)(_saturation * _pickerArea.Width);
            int y = _pickerArea.Y + (int)((1f - _value) * _pickerArea.Height);
            
            GUI.DrawRectangle(spriteBatch, 
                new Rectangle(x - 5, y - 5, 10, 10), 
                Color.Black, 
                isFilled: false, 
                depth: 0.01f);
        }

        private void DrawHueCursor(SpriteBatch spriteBatch)
        {
            int y = _hueBarArea.Y + (int)(_hue * _hueBarArea.Height);
            
            GUI.DrawRectangle(spriteBatch, 
                new Rectangle(_hueBarArea.X - 3, y - 3, _hueBarArea.Width + 6, 6), 
                Color.Black, 
                isFilled: false, 
                depth: 0.01f);
        }

        private void DrawAlphaSlider(SpriteBatch spriteBatch)
        {
            // Фоновый градиент
            for (int x = 0; x < _alphaSliderArea.Width; x++)
            {
                float pos = x / (float)_alphaSliderArea.Width;
                var color = new Color(_currentColor, pos);
                GUI.DrawRectangle(spriteBatch, 
                    new Rectangle(_alphaSliderArea.X + x, _alphaSliderArea.Y, 1, _alphaSliderArea.Height), 
                    color, 
                    isFilled: true);
            }
            
            // Ползунок
            int sliderX = _alphaSliderArea.X + (int)(_alpha * _alphaSliderArea.Width);
            GUI.DrawRectangle(spriteBatch, 
                new Rectangle(sliderX - 2, _alphaSliderArea.Y - 2, 4, _alphaSliderArea.Height + 4), 
                Color.Black, 
                isFilled: false);
        }

        private void DrawPresets(SpriteBatch spriteBatch)
        {
            for (int i = 0; i < PRESET_COUNT; i++)
            {
                GUI.DrawRectangle(spriteBatch, _presetAreas[i], GetPresetColor(i), isFilled: true);
                GUI.DrawRectangle(spriteBatch, _presetAreas[i], Color.Black, isFilled: false);
            }
        }

        private void DrawCurrentColor(SpriteBatch spriteBatch)
        {
            var rect = new Rectangle(
                _alphaSliderArea.X,
                _alphaSliderArea.Y + 30,
                100,
                50);
            
            GUI.DrawRectangle(spriteBatch, rect, _currentColor, isFilled: true);
            GUI.DrawRectangle(spriteBatch, rect, Color.Black, isFilled: false);
            
            // Отображение значений RGBA
            string text = $"R: {_currentColor.R}\nG: {_currentColor.G}\nB: {_currentColor.B}\nA: {_currentColor.A}";
            GUI.Font.DrawString(spriteBatch, rect.X + 110, rect.Y, text, Color.Black, 0f, Vector2.Zero, 0.8f);
        }
        #endregion

        #region Методы создания текстур
        private Texture2D CreateColorPickerTexture()
        {
            var texture = new Texture2D(GameMain.Instance.GraphicsDevice, PICKER_SIZE, PICKER_SIZE);
            Color[] data = new Color[PICKER_SIZE * PICKER_SIZE];

            for (int y = 0; y < PICKER_SIZE; y++)
            {
                float value = 1f - (y / (float)PICKER_SIZE);
                
                for (int x = 0; x < PICKER_SIZE; x++)
                {
                    float saturation = x / (float)PICKER_SIZE;
                    data[y * PICKER_SIZE + x] = ColorFromHSV(_hue, saturation, value);
                }
            }

            texture.SetData(data);
            return texture;
        }

        private Texture2D CreateHueBarTexture()
        {
            var texture = new Texture2D(GameMain.Instance.GraphicsDevice, 1, PICKER_SIZE);
            Color[] data = new Color[PICKER_SIZE];

            for (int y = 0; y < PICKER_SIZE; y++)
            {
                float hue = y / (float)PICKER_SIZE;
                data[y] = ColorFromHSV(hue, 1f, 1f);
            }

            texture.SetData(data);
            return texture;
        }

        private void UpdateTextures()
        {
            _colorPickerTexture?.Dispose();
            _colorPickerTexture = CreateColorPickerTexture();
        }
        #endregion

        #region Вспомогательные методы
        private Color GetPresetColor(int index)
        {
            // Стандартные предустановленные цвета
            return index switch
            {
                0 => Color.Red,
                1 => Color.Green,
                2 => Color.Blue,
                3 => Color.Yellow,
                4 => Color.Cyan,
                5 => Color.Magenta,
                6 => Color.White,
                7 => Color.Black,
                _ => Color.White
            };
        }

        private static Color ColorFromHSV(float hue, float saturation, float value)
        {
            int hi = (int)(hue * 6) % 6;
            float f = hue * 6 - hi;
            float p = value * (1 - saturation);
            float q = value * (1 - f * saturation);
            float t = value * (1 - (1 - f) * saturation);

            return hi switch
            {
                0 => new Color(value, t, p),
                1 => new Color(q, value, p),
                2 => new Color(p, value, t),
                3 => new Color(p, q, value),
                4 => new Color(t, p, value),
                _ => new Color(value, p, q)
            };
        }

        private static void ColorToHSV(Color color, out float hue, out float saturation, out float value)
        {
            float r = color.R / 255f;
            float g = color.G / 255f;
            float b = color.B / 255f;

            float max = Math.Max(r, Math.Max(g, b));
            float min = Math.Min(r, Math.Min(g, b));
            float delta = max - min;

            hue = 0f;
            if (delta != 0f)
            {
                if (max == r) hue = (g - b) / delta % 6f;
                else if (max == g) hue = (b - r) / delta + 2f;
                else hue = (r - g) / delta + 4f;
                
                hue /= 6f;
                if (hue < 0f) hue += 1f;
            }

            saturation = max == 0f ? 0f : delta / max;
            value = max;
        }
        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _colorPickerTexture?.Dispose();
                _hueBarTexture?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}