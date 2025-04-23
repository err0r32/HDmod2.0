using System;
using System.Linq;
using System.Xml.Linq;
using Barotrauma;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BarotraumaHD
{
    /// <summary>
    /// Класс для создания и управления меню настроек HD-мода
    /// </summary>
    public sealed class HDSettingsMenu : IDisposable
    {
        private readonly HDMod _mod;
        private GUIComponent _menuFrame;
        private GUIListBox _settingsList;
        private bool _disposed;

        /// <summary> Событие при закрытии меню </summary>
        public event Action OnClose;

        public HDSettingsMenu(HDMod mod)
        {
            _mod = mod ?? throw new ArgumentNullException(nameof(mod));
            CreateMenu();
            LoadSettings();
        }

        /// <summary>
        /// Создает основную структуру меню
        /// </summary>
        private void CreateMenu()
        {
            // Создаем фрейм меню
            _menuFrame = new GUIFrame(new RectTransform(new Vector2(0.4f, 0.6f), GUI.Canvas, Anchor.Center)
            {
                MinSize = new Point(600, 500)
            }, style: "GUIBackgroundBlocker");

            // Заголовок меню
            new GUITextBlock(new RectTransform(new Vector2(1.0f, 0.1f), _menuFrame.RectTransform),
                TextManager.Get("HDModSettings"), font: GUI.LargeFont)
            {
                TextAlignment = Alignment.Center,
                Padding = Vector4.Zero
            };

            // Список настроек
            _settingsList = new GUIListBox(new RectTransform(new Vector2(0.95f, 0.75f), _menuFrame.RectTransform, Anchor.Center)
            {
                RelativeOffset = new Vector2(0f, 0.1f)
            });

            // Кнопка закрытия
            var closeButton = new GUIButton(new RectTransform(new Vector2(0.3f, 0.08f), _menuFrame.RectTransform, Anchor.BottomCenter)
            {
                RelativeOffset = new Vector2(0f, -0.02f)
            }, TextManager.Get("Close"))
            {
                OnClicked = (btn, userdata) =>
                {
                    Close();
                    return true;
                }
            };
        }

        /// <summary>
        /// Загружает и отображает текущие настройки
        /// </summary>
        private void LoadSettings()
        {
            _settingsList.ClearChildren();

            // 1. Переключатель HD текстур
            AddToggleSetting(
                "EnableHDTextures",
                _mod.TextureManager.Enabled,
                enabled => _mod.TextureManager.Enabled = enabled);

            // 2. Настройка кэширования
            AddSliderSetting(
                "CacheSizeMB",
                _mod.TextureCache.MaxSizeMB,
                512, 4096,
                value => _mod.TextureCache.SetMaxSize((int)value));

            // 3. Переключатель совместимости с другими модами
            AddToggleSetting(
                "ModCompatibilityMode",
                _mod.CompatibilityMode,
                enabled => _mod.CompatibilityMode = enabled);

            // 4. Настройка прозрачности интерфейса
            AddSliderSetting(
                "UITransparency",
                _mod.UITransparency * 100f,
                0, 100,
                value => _mod.UITransparency = value / 100f);

            // 5. Переключатель отображения FPS
            AddToggleSetting(
                "ShowFPS",
                _mod.ShowFPS,
                enabled => _mod.ShowFPS = enabled);
        }

        /// <summary>
        /// Добавляет переключатель в меню настроек
        /// </summary>
        private void AddToggleSetting(string labelTag, bool defaultValue, Action<bool> onValueChanged)
        {
            var frame = new GUILayoutGroup(new RectTransform(new Vector2(1.0f, 0.12f), _settingsList.Content.RectTransform),
                isHorizontal: true, childAnchor: Anchor.CenterLeft)
            {
                Stretch = true,
                RelativeSpacing = 0.02f
            };

            // Текст настройки
            new GUITextBlock(new RectTransform(new Vector2(0.7f, 1.0f), frame.RectTransform),
                TextManager.Get(labelTag))
            {
                Padding = Vector4.Zero,
                TextAlignment = Alignment.CenterLeft
            };

            // Переключатель
            var toggle = new GUITickBox(new RectTransform(new Vector2(0.3f, 0.6f), frame.RectTransform),
                string.Empty)
            {
                Selected = defaultValue
            };

            toggle.OnSelected = (tickBox) =>
            {
                onValueChanged?.Invoke(tickBox.Selected);
                return true;
            };
        }

        /// <summary>
        /// Добавляет слайдер в меню настроек
        /// </summary>
        private void AddSliderSetting(string labelTag, float defaultValue, float min, float max, Action<float> onValueChanged)
        {
            var frame = new GUIFrame(new RectTransform(new Vector2(1.0f, 0.15f), _settingsList.Content.RectTransform));
            var layout = new GUILayoutGroup(new RectTransform(new Vector2(1.0f, 0.8f), frame.RectTransform, Anchor.Center),
                isHorizontal: false);

            // Текст настройки
            new GUITextBlock(new RectTransform(new Vector2(1.0f, 0.4f), layout.RectTransform),
                TextManager.Get(labelTag))
            {
                Padding = Vector4.Zero
            };

            // Слайдер и значение
            var sliderLayout = new GUILayoutGroup(new RectTransform(new Vector2(1.0f, 0.6f), layout.RectTransform),
                isHorizontal: true);

            var slider = new GUIScrollBar(new RectTransform(new Vector2(0.7f, 1.0f), sliderLayout.RectTransform),
                barSize: 0.1f, style: "GUISlider");
            
            var valueText = new GUITextBlock(new RectTransform(new Vector2(0.3f, 1.0f), sliderLayout.RectTransform),
                defaultValue.ToString("0"));

            slider.Range = new Vector2(min, max);
            slider.BarScroll = defaultValue;

            slider.OnMoved = (scrollBar, scroll) =>
            {
                float value = scrollBar.BarScrollValue;
                valueText.Text = value.ToString("0");
                onValueChanged?.Invoke(value);
                return true;
            };
        }

        /// <summary>
        /// Открывает меню настроек
        /// </summary>
        public void Open()
        {
            _menuFrame.Visible = true;
            GUI.KeyboardDispatcher.Subscriber = _menuFrame;
        }

        /// <summary>
        /// Закрывает меню настроек
        /// </summary>
        public void Close()
        {
            _menuFrame.Visible = false;
            OnClose?.Invoke();
        }

        public void Dispose()
        {
            if (_disposed) return;
            
            _menuFrame?.Parent?.RemoveChild(_menuFrame);
            _menuFrame = null;
            _disposed = true;
        }
    }
}