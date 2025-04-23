HDUIBuilder.csusing System;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Barotrauma;
using Barotrauma.Items.Components;

namespace BarotraumaHD
{
    /// <summary>
    /// Фабрика UI-элементов для HD мода
    /// </summary>
    public static class HDUIBuilder
    {
        private static readonly Color DefaultBackgroundColor = new Color(30, 30, 40, 200);
        private static readonly Color DefaultTextColor = Color.White;
        private static readonly Color DefaultButtonColor = new Color(70, 70, 90, 255);
        private static readonly Color DefaultButtonHoverColor = new Color(100, 100, 120, 255);

        /// <summary>
        /// Создает окно настроек HD мода
        /// </summary>
        public static GUIFrame CreateSettingsWindow(out GUIButton closeButton)
        {
            var frame = new GUIFrame(new RectTransform(new Vector2(0.4f, 0.6f), GUI.Canvas, Anchor.Center))
            {
                Color = DefaultBackgroundColor,
                Padding = new Vector4(20f, 20f, 20f, 20f)
            };

            var title = new GUITextBlock(new RectTransform(new Vector2(1f, 0.1f), frame.RectTransform),
                TextManager.Get("HDModSettings"), font: GUI.LargeFont)
            {
                TextAlignment = Alignment.Center,
                TextColor = DefaultTextColor
            };

            var contentContainer = new GUILayoutGroup(new RectTransform(new Vector2(1f, 0.8f), frame.RectTransform))
            {
                RelativeSpacing = 0.02f,
                Stretch = true
            };

            closeButton = new GUIButton(new RectTransform(new Vector2(0.3f, 0.1f), frame.RectTransform, Anchor.BottomCenter),
                TextManager.Get("Close"))
            {
                Color = DefaultButtonColor,
                HoverColor = DefaultButtonHoverColor
            };

            return frame;
        }

        /// <summary>
        /// Создает переключатель с текстовой меткой
        /// </summary>
        public static GUITickBox CreateToggle(RectTransform parent, string labelText, bool initialState, Action<bool> onValueChanged)
        {
            var container = new GUILayoutGroup(new RectTransform(new Vector2(1f, 0.08f), parent), isHorizontal: true)
            {
                Stretch = true,
                RelativeSpacing = 0.05f
            };

            var tickBox = new GUITickBox(new RectTransform(new Vector2(0.8f, 1f), container.RectTransform), labelText)
            {
                Selected = initialState,
                TextColor = DefaultTextColor,
                OnSelected = (GUITickBox box) => 
                {
                    onValueChanged?.Invoke(box.Selected);
                    return true;
                }
            };

            return tickBox;
        }

        /// <summary>
        /// Создает слайдер с текстовой меткой
        /// </summary>
        public static GUIScrollBar CreateSlider(RectTransform parent, string labelText, float min, float max, float initialValue, Action<float> onValueChanged)
        {
            var container = new GUILayoutGroup(new RectTransform(new Vector2(1f, 0.1f), parent))
            {
                Stretch = true
            };

            var label = new GUITextBlock(new RectTransform(new Vector2(1f, 0.5f), container.RectTransform), labelText)
            {
                TextColor = DefaultTextColor
            };

            var slider = new GUIScrollBar(new RectTransform(new Vector2(1f, 0.5f), container.RectTransform), barSize: 0.1f)
            {
                MinValue = min,
                MaxValue = max,
                Step = 0.01f,
                BarScroll = initialValue,
                OnMoved = (GUIScrollBar scrollBar, float scrollValue) =>
                {
                    onValueChanged?.Invoke(scrollValue);
                    return true;
                }
            };

            return slider;
        }

        /// <summary>
        /// Создает панель с информацией о производительности
        /// </summary>
        public static GUIFrame CreatePerformancePanel(RectTransform parent)
        {
            var frame = new GUIFrame(new RectTransform(new Vector2(1f, 0.2f), parent))
            {
                Color = new Color(20, 20, 30, 150),
                Padding = new Vector4(10f, 10f, 10f, 10f)
            };

            var layout = new GUILayoutGroup(new RectTransform(new Vector2(1f, 1f), frame.RectTransform))
            {
                RelativeSpacing = 0.05f
            };

            new GUITextBlock(new RectTransform(new Vector2(1f, 0.3f), layout.RectTransform), 
                "HD Mod Performance", font: GUI.SubHeadingFont)
            {
                TextAlignment = Alignment.Center,
                TextColor = Color.LightBlue
            };

            return frame;
        }

        /// <summary>
        /// Создает кнопку с иконкой
        /// </summary>
        public static GUIButton CreateIconButton(RectTransform parent, string iconStyle, Action onClick)
        {
            var button = new GUIButton(new RectTransform(new Vector2(0.1f, 0.9f), parent), style: iconStyle)
            {
                Color = DefaultButtonColor,
                HoverColor = DefaultButtonHoverColor,
                OnClicked = (btn, userdata) =>
                {
                    onClick?.Invoke();
                    return true;
                }
            };

            return button;
        }

        /// <summary>
        /// Создает элемент для выбора цвета
        /// </summary>
        public static GUILayoutGroup CreateColorPicker(RectTransform parent, string label, Color initialColor, Action<Color> onColorChanged)
        {
            var container = new GUILayoutGroup(new RectTransform(new Vector2(1f, 0.15f), parent), isHorizontal: true)
            {
                RelativeSpacing = 0.05f,
                Stretch = true
            };

            new GUITextBlock(new RectTransform(new Vector2(0.4f, 1f), container.RectTransform), label)
            {
                TextColor = DefaultTextColor
            };

            var colorPreview = new GUIFrame(new RectTransform(new Vector2(0.2f, 1f), container.RectTransform))
            {
                Color = initialColor
            };

            var slidersContainer = new GUILayoutGroup(new RectTransform(new Vector2(0.4f, 1f), container.RectTransform))
            {
                RelativeSpacing = 0.1f
            };

            CreateColorSlider(slidersContainer, "R", initialColor.R, (value) => 
            {
                var newColor = new Color((int)value, initialColor.G, initialColor.B, initialColor.A);
                colorPreview.Color = newColor;
                onColorChanged?.Invoke(newColor);
            });

            CreateColorSlider(slidersContainer, "G", initialColor.G, (value) => 
            {
                var newColor = new Color(initialColor.R, (int)value, initialColor.B, initialColor.A);
                colorPreview.Color = newColor;
                onColorChanged?.Invoke(newColor);
            });

            CreateColorSlider(slidersContainer, "B", initialColor.B, (value) => 
            {
                var newColor = new Color(initialColor.R, initialColor.G, (int)value, initialColor.A);
                colorPreview.Color = newColor;
                onColorChanged?.Invoke(newColor);
            });

            return container;
        }

        private static void CreateColorSlider(RectTransform parent, string channel, byte initialValue, Action<int> onValueChanged)
        {
            var container = new GUILayoutGroup(new RectTransform(new Vector2(1f, 0.3f), parent), isHorizontal: true)
            {
                RelativeSpacing = 0.1f
            };

            new GUITextBlock(new RectTransform(new Vector2(0.2f, 1f), container.RectTransform), channel)
            {
                TextColor = DefaultTextColor
            };

            var slider = new GUIScrollBar(new RectTransform(new Vector2(0.6f, 1f), container.RectTransform), barSize: 0.1f)
            {
                MinValue = 0,
                MaxValue = 255,
                Step = 1,
                BarScroll = initialValue,
                OnMoved = (GUIScrollBar scrollBar, float scrollValue) =>
                {
                    onValueChanged?.Invoke((int)scrollValue);
                    return true;
                }
            };

            new GUITextBlock(new RectTransform(new Vector2(0.2f, 1f), container.RectTransform), initialValue.ToString())
            {
                TextColor = DefaultTextColor,
                UserData = slider // Для обновления текста значения
            };
        }
    }
}