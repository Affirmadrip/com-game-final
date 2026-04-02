using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GalactaJumperMo.Classes
{
    public enum SettingsAction { None, Back }

    public class SettingsScreen : Screen
    {
        private readonly SpriteFont _titleFont;
        private readonly SpriteFont _menuFont;
        private readonly Texture2D  _pixel;

        // ── Setting item types ────────────────────────────────────────────────
        private enum SettingType { Toggle, Slider, Back }

        private struct SettingItem
        {
            public string      Label;
            public SettingType Type;
            public bool        BoolValue;
            public float       FloatValue;   // 0-1 for sliders
        }

        private readonly SettingItem[] _items;
        private int _selectedIndex = 0;

        private readonly float[] _itemReveal;
        private readonly float[] _itemSlide;

        private KeyboardState _prevKb;
        private MouseState    _prevMs;

        private float _time      = 0f;
        private float _fadeIn    = 0f;
        private bool  _confirmed = false;
        private float _exitFade  = 0f;

        // Public accessors for game systems to read
        public bool  SoundEnabled   => _items[0].BoolValue;
        public bool  MusicEnabled   => _items[1].BoolValue;
        public float MasterVolume   => _items[2].FloatValue;
        public float SfxVolume      => _items[3].FloatValue;

        public SettingsAction PendingAction { get; private set; } = SettingsAction.None;

        public SettingsScreen(
            SpriteFont titleFont,
            SpriteFont menuFont,
            Texture2D pixel,
            bool soundEnabled = true,
            bool musicEnabled = true,
            float masterVolume = 0.8f,
            float sfxVolume = 0.8f)
        {
            _titleFont = titleFont;
            _menuFont  = menuFont;
            _pixel     = pixel;
            _prevKb    = Keyboard.GetState();
            _prevMs    = Mouse.GetState();

            _items = new SettingItem[]
            {
                new SettingItem { Label = "Sound Effects",  Type = SettingType.Toggle, BoolValue  = soundEnabled, FloatValue = 0f },
                new SettingItem { Label = "Music",          Type = SettingType.Toggle, BoolValue  = musicEnabled, FloatValue = 0f },
                new SettingItem { Label = "Master Volume",  Type = SettingType.Slider, BoolValue  = false, FloatValue = masterVolume },
                new SettingItem { Label = "SFX Volume",     Type = SettingType.Slider, BoolValue  = false, FloatValue = sfxVolume },
                new SettingItem { Label = "Back",           Type = SettingType.Back,   BoolValue  = false, FloatValue = 0f },
            };

            _itemReveal = new float[_items.Length];
            _itemSlide  = new float[_items.Length];
        }

        // ── Item height (sliders are taller to fit the bar) ───────────────────
        private float ItemHeight(int i) => _items[i].Type == SettingType.Slider ? 76f : 54f;

        private float ItemY(int i, int sh)
        {
            float totalH = 0f;
            for (int j = 0; j < _items.Length; j++) totalH += ItemHeight(j);
            float y0 = sh - totalH - 90f;
            float y  = y0;
            for (int j = 0; j < i; j++) y += ItemHeight(j);
            return y;
        }

        public override void Update(GameTime gt, int sw, int sh)
        {
            float dt = (float)gt.ElapsedGameTime.TotalSeconds;
            _time   += dt;
            _fadeIn  = Math.Min(1f, _fadeIn + dt * 1.2f);

            for (int i = 0; i < _items.Length; i++)
            {
                if (_time > 0.35f + i * 0.12f)
                    _itemReveal[i] = Math.Min(1f, _itemReveal[i] + dt * 2.8f);
            }

            if (_confirmed)
            {
                _exitFade = Math.Min(1f, _exitFade + dt * 3.5f);
                if (_exitFade >= 1f) PendingAction = SettingsAction.Back;
                return;
            }

            for (int i = 0; i < _items.Length; i++)
                _itemSlide[i] = MathHelper.Lerp(_itemSlide[i],
                    i == _selectedIndex ? 18f : 0f, dt * 9f);

            var kb = Keyboard.GetState();
            var ms = Mouse.GetState();

            // Navigation
            if (Pressed(Keys.Down, kb) || Pressed(Keys.S, kb)) _selectedIndex = (_selectedIndex + 1) % _items.Length;
            if (Pressed(Keys.Up,   kb) || Pressed(Keys.W, kb)) _selectedIndex = (_selectedIndex - 1 + _items.Length) % _items.Length;

            // Mouse hover — check each item's rectangle
            for (int i = 0; i < _items.Length; i++)
            {
                float y  = ItemY(i, sh);
                float x  = 80f + _itemSlide[i];
                var   sz = _menuFont.MeasureString(ItemLabel(i));
                var rect = new Rectangle((int)x - 20, (int)y - 6, (int)sz.X + 40, (int)sz.Y + 12);
                if (rect.Contains(ms.Position)) { _selectedIndex = i; break; }
            }

            // Interact with selected item
            ref var item = ref _items[_selectedIndex];

            if (item.Type == SettingType.Toggle)
            {
                if (Pressed(Keys.Enter, kb) || Pressed(Keys.Space, kb) || Pressed(Keys.Left, kb) || Pressed(Keys.Right, kb) || Pressed(Keys.A, kb) || Pressed(Keys.D, kb))
                    item.BoolValue = !item.BoolValue;
            }
            else if (item.Type == SettingType.Slider)
            {
                if (kb.IsKeyDown(Keys.Left) || kb.IsKeyDown(Keys.A))  item.FloatValue = Math.Max(0f, item.FloatValue - dt * 0.6f);
                if (kb.IsKeyDown(Keys.Right) || kb.IsKeyDown(Keys.D)) item.FloatValue = Math.Min(1f, item.FloatValue + dt * 0.6f);
            }
            else if (item.Type == SettingType.Back)
            {
                bool confirm = Pressed(Keys.Enter, kb) || Pressed(Keys.Space, kb) ||
                               Pressed(Keys.Escape, kb) ||
                               (ms.LeftButton == ButtonState.Pressed &&
                                _prevMs.LeftButton == ButtonState.Released);
                if (confirm) _confirmed = true;
            }

            if (Pressed(Keys.Escape, kb)) _confirmed = true;

            _prevKb = kb;
            _prevMs = ms;
        }

        private bool Pressed(Keys k, KeyboardState cur) => cur.IsKeyDown(k) && !_prevKb.IsKeyDown(k);

        private string ItemLabel(int i)
        {
            var item = _items[i];
            return item.Type switch
            {
                SettingType.Toggle => item.Label + "   " + (item.BoolValue ? "ON" : "OFF"),
                SettingType.Slider => item.Label,
                _                  => item.Label,
            };
        }

        public override void Draw(SpriteBatch sb, int sw, int sh)
        {
            DrawBg(sb, _pixel, sw, sh);
            DrawVignette(sb, _pixel, sw, sh);

            byte fa = (byte)(_fadeIn * 255f);
            DrawTitle(sb, fa);
            DrawItems(sb, sw, sh, fa);

            if (_exitFade > 0f)
                sb.Draw(_pixel, new Rectangle(0, 0, sw, sh),
                    new Color((byte)0, (byte)0, (byte)0, (byte)(_exitFade * 255f)));
        }

        private void DrawTitle(SpriteBatch sb, byte fa)
        {
            float sc = (float)Math.Sin(_time * 0.75f) * 0.010f + 1f;
            sb.DrawString(_titleFont, "SETTINGS", new Vector2(72f, 58f),
                new Color(TextWhite.R, TextWhite.G, TextWhite.B, fa),
                0f, Vector2.Zero, sc, SpriteEffects.None, 0f);

            float divY = 58f + _titleFont.LineSpacing * sc + 10f;
            sb.Draw(_pixel, new Rectangle(72, (int)divY, 120, 1),
                new Color((byte)255, (byte)255, (byte)255, (byte)(fa * 0.20f)));
        }

        private void DrawItems(SpriteBatch sb, int sw, int sh, byte fa)
        {
            for (int i = 0; i < _items.Length; i++)
            {
                float rev = _itemReveal[i];
                if (rev <= 0.01f) continue;

                bool  selected = i == _selectedIndex;
                float x        = 80f + _itemSlide[i];
                float y        = ItemY(i, sh);
                byte  alpha    = (byte)(fa * rev);

                switch (_items[i].Type)
                {
                    case SettingType.Toggle: DrawToggle(sb, i, x, y, selected, alpha); break;
                    case SettingType.Slider: DrawSlider(sb, i, x, y, selected, alpha); break;
                    case SettingType.Back:   DrawBack(sb, x, y, selected, alpha);      break;
                }
            }
        }

        private void DrawToggle(SpriteBatch sb, int i, float x, float y, bool selected, byte alpha)
        {
            string label = _items[i].Label;
            bool   on    = _items[i].BoolValue;

            // Label
            Color labelCol = selected
                ? new Color(TextWhite.R, TextWhite.G, TextWhite.B, alpha)
                : new Color(TextDim.R,   TextDim.G,   TextDim.B,   alpha);
            sb.DrawString(_menuFont, label, new Vector2(x, y), labelCol);

            // Tick box  [ 0 ] / [   ] drawn to the right of the label
            float labelW = _menuFont.MeasureString(label).X;
            float boxX   = x + labelW + 24f;
            float boxY   = y + 2f;
            int   boxS   = 14;

            byte ba = (byte)(alpha * 0.55f);
            // outer border (1px)
            sb.Draw(_pixel, new Rectangle((int)boxX,          (int)boxY,          boxS, 1),  new Color(TextDim.R, TextDim.G, TextDim.B, ba));
            sb.Draw(_pixel, new Rectangle((int)boxX,          (int)boxY + boxS-1, boxS, 1),  new Color(TextDim.R, TextDim.G, TextDim.B, ba));
            sb.Draw(_pixel, new Rectangle((int)boxX,          (int)boxY,          1, boxS),  new Color(TextDim.R, TextDim.G, TextDim.B, ba));
            sb.Draw(_pixel, new Rectangle((int)boxX + boxS-1, (int)boxY,          1, boxS),  new Color(TextDim.R, TextDim.G, TextDim.B, ba));
            // fill when ON
            if (on)
            {
                byte fa2 = selected ? alpha : (byte)(alpha * 0.70f);
                sb.Draw(_pixel,
                    new Rectangle((int)boxX + 3, (int)boxY + 3, boxS - 6, boxS - 6),
                    new Color(TextWhite.R, TextWhite.G, TextWhite.B, fa2));
            }

            // ON / OFF text
            string valText = on ? "ON" : "OFF";
            float  valX    = boxX + boxS + 10f;
            byte   va      = (byte)(alpha * (on ? 0.90f : 0.40f));
            sb.DrawString(_menuFont, valText, new Vector2(valX, y),
                new Color(TextWhite.R, TextWhite.G, TextWhite.B, va));

            // Underline the whole row when selected
            if (selected)
            {
                float fullW = _menuFont.MeasureString(valText).X + valX - x;
                sb.Draw(_pixel,
                    new Rectangle((int)x, (int)(y + _menuFont.LineSpacing + 1), (int)fullW, 1),
                    new Color(TextWhite.R, TextWhite.G, TextWhite.B, alpha));
            }
        }

        private void DrawSlider(SpriteBatch sb, int i, float x, float y, bool selected, byte alpha)
        {
            string label   = _items[i].Label;
            float  value   = _items[i].FloatValue;
            int    pct     = (int)(value * 100f);

            // Label
            Color labelCol = selected
                ? new Color(TextWhite.R, TextWhite.G, TextWhite.B, alpha)
                : new Color(TextDim.R,   TextDim.G,   TextDim.B,   alpha);
            sb.DrawString(_menuFont, label, new Vector2(x, y), labelCol);

            // Percentage text
            string pctText = pct + "%";
            float  pctX    = x + _menuFont.MeasureString(label).X + 24f;
            byte   pa      = (byte)(alpha * 0.60f);
            sb.DrawString(_menuFont, pctText, new Vector2(pctX, y),
                new Color(TextDim.R, TextDim.G, TextDim.B, pa));

            // Slider bar — drawn on the line below the label
            float barX = x;
            float barY = y + _menuFont.LineSpacing + 6f;
            float barW = 200f;
            int   barH = 2;
            int   fillW = (int)(barW * value);

            // Track (dim)
            byte ta = (byte)(alpha * 0.25f);
            sb.Draw(_pixel, new Rectangle((int)barX, (int)barY, (int)barW, barH),
                new Color(TextWhite.R, TextWhite.G, TextWhite.B, ta));

            // Fill (bright)
            if (fillW > 0)
            {
                byte fa2 = selected ? alpha : (byte)(alpha * 0.60f);
                sb.Draw(_pixel, new Rectangle((int)barX, (int)barY, fillW, barH),
                    new Color(TextWhite.R, TextWhite.G, TextWhite.B, fa2));
            }

            // Thumb (small square at fill boundary)
            int thumbS = 6;
            int thumbX = (int)(barX + fillW - thumbS / 2);
            int thumbY = (int)(barY - thumbS / 2 + barH / 2);
            sb.Draw(_pixel, new Rectangle(thumbX, thumbY, thumbS, thumbS),
                new Color(TextWhite.R, TextWhite.G, TextWhite.B, selected ? alpha : (byte)(alpha * 0.60f)));
        }

        private void DrawBack(SpriteBatch sb, float x, float y, bool selected, byte alpha)
        {
            Color col = selected
                ? new Color(TextWhite.R, TextWhite.G, TextWhite.B, alpha)
                : new Color(TextDim.R,   TextDim.G,   TextDim.B,   alpha);

            sb.DrawString(_menuFont, "Back", new Vector2(x, y), col);

            if (selected)
            {
                var sz = _menuFont.MeasureString("Back");
                sb.Draw(_pixel,
                    new Rectangle((int)x, (int)(y + sz.Y + 1), (int)sz.X, 1),
                    new Color(TextWhite.R, TextWhite.G, TextWhite.B, alpha));
            }
        }
    }
}
