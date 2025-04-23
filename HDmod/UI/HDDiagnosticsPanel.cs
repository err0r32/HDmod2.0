using System;
using System.Linq;
using System.Text;
using Barotrauma;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BarotraumaHD
{
    /// <summary>
    /// Панель диагностики для отображения статистики работы HD-мода
    /// </summary>
    public class HDDiagnosticsPanel : IDisposable
    {
        private readonly HDTextureCache _textureCache;
        private readonly HDTextureManager _textureManager;
        private GUIComponent _panel;
        private GUITextBlock _statsText;
        private bool _isVisible;
        private float _updateTimer;
        private const float UPDATE_INTERVAL = 1.0f; // Обновление раз в секунду

        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (_isVisible != value)
                {
                    _isVisible = value;
                    _panel.Visible = _isVisible;
                    if (_isVisible) UpdateStats();
                }
            }
        }

        public HDDiagnosticsPanel(HDTextureCache cache, HDTextureManager manager)
        {
            _textureCache = cache ?? throw new ArgumentNullException(nameof(cache));
            _textureManager = manager ?? throw new ArgumentNullException(nameof(manager));

            CreatePanel();
        }

        private void CreatePanel()
        {
            _panel = new GUIFrame(new RectTransform(new Vector2(0.3f, 0.4f), GUI.Canvas, Anchor.TopRight)
            {
                MinSize = new Point(300, 400)
            }, style: "GUIBackgroundBlocker");

            var header = new GUITextBlock(new RectTransform(new Vector2(1.0f, 0.07f), _panel.RectTransform),
                "HD Mod Diagnostics", font: GUI.LargeFont);

            var closeButton = new GUIButton(new RectTransform(new Vector2(0.1f, 0.9f), header.RectTransform, Anchor.TopRight),
                "X", style: "GUIButtonClose")
            {
                OnClicked = (btn, userdata) => { IsVisible = false; return true; }
            };

            _statsText = new GUITextBlock(new RectTransform(new Vector2(0.95f, 0.85f), _panel.RectTransform, Anchor.Center),
                "", font: GUI.SmallFont)
            {
                TextColor = Color.White,
                TextAlignment = Alignment.TopLeft,
                Wrap = true
            };

            var toggleButton = new GUIButton(new RectTransform(new Vector2(0.2f, 0.05f), GUI.Canvas, Anchor.BottomRight),
                "HD Stats", style: "GUIButtonSmall")
            {
                OnClicked = (btn, userdata) => { IsVisible = !IsVisible; return true; }
            };

            _panel.Visible = false;
        }

        public void Update(float deltaTime)
        {
            if (!_isVisible) return;

            _updateTimer += deltaTime;
            if (_updateTimer >= UPDATE_INTERVAL)
            {
                _updateTimer = 0;
                UpdateStats();
            }
        }

        private void UpdateStats()
        {
            var sb = new StringBuilder();
            
            // Основная информация
            sb.AppendLine($"HD Mod v{HDMod.Version}");
            sb.AppendLine($"Status: {(_textureManager.Enabled ? "Enabled" : "Disabled")}");
            sb.AppendLine();

            // Статистика текстур
            sb.AppendLine("[Texture Cache]");
            sb.AppendLine($"Textures loaded: {_textureCache.Count}");
            sb.AppendLine($"Memory usage: {CalculateTextureMemoryMB()} MB");
            sb.AppendLine($"Cache hits: {_textureCache.CacheHits}");
            sb.AppendLine($"Cache misses: {_textureCache.CacheMisses}");
            sb.AppendLine();

            // Системная информация
            sb.AppendLine("[System]");
            sb.AppendLine($"FPS: {1.0f / GameMain.DeltaTime:0}");
            sb.AppendLine($"GC Memory: {GC.GetTotalMemory(false) / 1024 / 1024} MB");

            _statsText.Text = sb.ToString();
        }

        private float CalculateTextureMemoryMB()
        {
            return _textureCache.Sum(t => t.Width * t.Height * 4) / 1024f / 1024f;
        }

        public void Toggle()
        {
            IsVisible = !IsVisible;
        }

        public void Dispose()
        {
            _panel?.Parent?.RemoveChild(_panel);
            _panel = null;
        }
    }
}