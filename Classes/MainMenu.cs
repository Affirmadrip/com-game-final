using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GalactaJumperMo.Classes
{
    public enum MenuAction { None, NewGame, Continue, Tutorial, Settings, Exit }

    public class MainMenuScreen : Screen
    {
        private readonly SpriteFont _titleFont;
        private readonly SpriteFont _menuFont;
        private readonly Texture2D  _pixel;

        private readonly List<string>     _items   = new();
        private readonly List<MenuAction> _actions = new();
        private int _selectedIndex = 0;

        private readonly float[] _itemSlide;
        private readonly float[] _itemReveal;

        private KeyboardState _prevKb;
        private MouseState    _prevMs;

        private float _time    = 0f;
        private float _fadeIn  = 0f;
        private bool  _confirmed = false;
        private float _exitFade  = 0f;


        private struct Mote { public Vector2 Pos; public float Vy, Vx, Alpha, Size; }
        private readonly List<Mote> _motes = new();
        private readonly Random     _rng   = new();
        private bool _motesSeeded = false;

        public MenuAction PendingAction { get; private set; } = MenuAction.None;

        public MainMenuScreen(SpriteFont titleFont, SpriteFont menuFont,
                              Texture2D pixel, bool hasSaveData)
        {
            _titleFont = titleFont;
            _menuFont  = menuFont;
            _pixel     = pixel;

            if (hasSaveData)
            {
                _items.Add("Continue"); _actions.Add(MenuAction.Continue);
            }
            _items.Add("New Game"); _actions.Add(MenuAction.NewGame);
            _items.Add("Tutorial"); _actions.Add(MenuAction.Tutorial);
            _items.Add("Settings"); _actions.Add(MenuAction.Settings);
            _items.Add("Exit");     _actions.Add(MenuAction.Exit);

            _itemSlide  = new float[_items.Count];
            _itemReveal = new float[_items.Count];
            _prevKb     = Keyboard.GetState();
            _prevMs     = Mouse.GetState();
        }

        public override void Update(GameTime gt, int sw, int sh)
        {
            float dt = (float)gt.ElapsedGameTime.TotalSeconds;
            _time   += dt;
            _fadeIn  = Math.Min(1f, _fadeIn + dt * 1.2f);

            if (!_motesSeeded) { SeedMotes(sw, sh); _motesSeeded = true; }

            for (int i = 0; i < _items.Count; i++)
            {
                float delay = 0.35f + i * 0.14f;
                if (_time > delay)
                    _itemReveal[i] = Math.Min(1f, _itemReveal[i] + dt * 2.8f);
            }

            UpdateMotes(dt, sw, sh);

            if (_confirmed)
            {
                _exitFade = Math.Min(1f, _exitFade + dt * 3.5f);
                if (_exitFade >= 1f) PendingAction = _actions[_selectedIndex];
                return;
            }

            for (int i = 0; i < _items.Count; i++)
                _itemSlide[i] = MathHelper.Lerp(_itemSlide[i],
                    i == _selectedIndex ? 18f : 0f, dt * 9f);

            var kb = Keyboard.GetState();
            var ms = Mouse.GetState();

            if (Pressed(Keys.Down, kb) || Pressed(Keys.S, kb)) _selectedIndex = (_selectedIndex + 1) % _items.Count;
            if (Pressed(Keys.Up,   kb) || Pressed(Keys.W, kb)) _selectedIndex = (_selectedIndex - 1 + _items.Count) % _items.Count;

            float lineH = 56f;
            float mx0   = 80f;
            float my0   = sh - _items.Count * lineH - 90f;
            for (int i = 0; i < _items.Count; i++)
            {
                var sz   = _menuFont.MeasureString(_items[i]);
                var rect = new Rectangle((int)mx0 - 28, (int)(my0 + i * lineH) - 6,
                                         (int)sz.X + 64, (int)sz.Y + 12);
                if (rect.Contains(ms.Position)) { _selectedIndex = i; break; }
            }

            bool confirm = Pressed(Keys.Enter, kb) || Pressed(Keys.Space, kb) ||
                           (ms.LeftButton == ButtonState.Pressed &&
                            _prevMs.LeftButton == ButtonState.Released);
            _prevKb = kb;
            _prevMs = ms;

            if (confirm) _confirmed = true;
        }

        private bool Pressed(Keys k, KeyboardState cur) => cur.IsKeyDown(k) && !_prevKb.IsKeyDown(k);

        private void SeedMotes(int sw, int sh)
        {
            for (int i = 0; i < 60; i++) _motes.Add(MakeMote(sw, sh, scatter: true));
        }

        private Mote MakeMote(int sw, int sh, bool scatter) => new Mote
        {
            Pos   = new Vector2((float)(_rng.NextDouble() * sw),
                                scatter ? (float)(_rng.NextDouble() * sh) : sh + 4f),
            Vy    = (float)(_rng.NextDouble() * 14f + 4f),
            Vx    = (float)(_rng.NextDouble() * 14f - 7f),
            Alpha = (float)(_rng.NextDouble() * 0.18 + 0.03),
            Size  = (float)(_rng.NextDouble() * 1.8f + 0.5f)
        };

        private void UpdateMotes(float dt, int sw, int sh)
        {
            for (int i = _motes.Count - 1; i >= 0; i--)
            {
                var m = _motes[i];
                m.Pos.Y -= m.Vy * dt;
                m.Pos.X += m.Vx * dt;
                _motes[i] = m;
                if (m.Pos.Y < -8 || m.Pos.X < -16 || m.Pos.X > sw + 16)
                {
                    _motes.RemoveAt(i);
                    _motes.Add(MakeMote(sw, sh, scatter: false));
                }
            }
        }

        public override void Draw(SpriteBatch sb, int sw, int sh)
        {
            sb.Draw(_pixel, new Rectangle(0, 0, sw, sh), BgBase);
            DrawVignette(sb, sw, sh);
            DrawMotes(sb);

            byte fa = (byte)(_fadeIn * 255f);
            DrawTitle(sb, fa);
            DrawMenuItems(sb, sh, fa);
            DrawFooter(sb, sw, sh, fa);

            if (_exitFade > 0f)
                sb.Draw(_pixel, new Rectangle(0, 0, sw, sh),
                    new Color((byte)0, (byte)0, (byte)0, (byte)(_exitFade * 255f)));
        }

        private void DrawVignette(SpriteBatch sb, int sw, int sh)
        {
            int rim   = Math.Min(sw, sh) / 4;
            int steps = 20;
            int layer = Math.Max(1, rim / steps);
            for (int i = 0; i < steps; i++)
            {
                float t = 1f - (float)i / steps;
                byte  a = (byte)(t * t * 200);
                Color c = new Color((byte)0, (byte)0, (byte)0, a);
                int   p = i * layer;
                sb.Draw(_pixel, new Rectangle(0,           p,               sw, layer), c);
                sb.Draw(_pixel, new Rectangle(0,           sh - p - layer,  sw, layer), c);
                sb.Draw(_pixel, new Rectangle(p,           0,       layer,  sh),        c);
                sb.Draw(_pixel, new Rectangle(sw - p - layer, 0,   layer,  sh),        c);
            }
        }

        private void DrawMotes(SpriteBatch sb)
        {
            foreach (var m in _motes)
            {
                int  sz = Math.Max(1, (int)m.Size);
                byte a  = (byte)(m.Alpha * _fadeIn * 255f);
                sb.Draw(_pixel, new Rectangle((int)m.Pos.X, (int)m.Pos.Y, sz, sz),
                    new Color((byte)255, (byte)255, (byte)255, a));
            }
        }

        private void DrawTitle(SpriteBatch sb, byte fa)
        {
            const string LINE1 = "GALACTA";
            const string LINE2 = "JUMPER : MO";

            float x  = 72f;
            float y  = 58f;
            float sc = (float)Math.Sin(_time * 0.75f) * 0.010f + 1f;
            float ls = _titleFont.LineSpacing * sc;

            Color tc = new Color(TextWhite.R, TextWhite.G, TextWhite.B, fa);
            sb.DrawString(_titleFont, LINE1, new Vector2(x, y),      tc, 0f, Vector2.Zero, sc, SpriteEffects.None, 0f);
            sb.DrawString(_titleFont, LINE2, new Vector2(x, y + ls), tc, 0f, Vector2.Zero, sc, SpriteEffects.None, 0f);

            float divY = y + ls * 2f + 10f;
            sb.Draw(_pixel, new Rectangle((int)x, (int)divY, 120, 1),
                new Color((byte)255, (byte)255, (byte)255, (byte)(fa * 0.25f)));

            float tagPulse = (float)(Math.Sin(_time * 1.1f) * 0.06 + 0.40);
            byte  ta       = (byte)(fa * tagPulse);
            sb.DrawString(_menuFont, "a solo journey",
                new Vector2(x + 2f, divY + 10f),
                new Color(TextDim.R, TextDim.G, TextDim.B, ta));
        }

        private void DrawMenuItems(SpriteBatch sb, int sh, byte fa)
        {
            const float LINE_H = 56f;
            float x0 = 80f;
            float y0 = sh - _items.Count * LINE_H - 90f;

            for (int i = 0; i < _items.Count; i++)
            {
                float rev = _itemReveal[i];
                if (rev <= 0.01f) continue;

                bool  selected = i == _selectedIndex;
                float x = x0 + _itemSlide[i];
                float y = y0 + i * LINE_H;
                byte  alpha = (byte)(fa * rev);

                Color col = selected
                    ? new Color(TextWhite.R, TextWhite.G, TextWhite.B, alpha)
                    : new Color(TextDim.R,   TextDim.G,   TextDim.B,   alpha);

                sb.DrawString(_menuFont, _items[i], new Vector2(x, y), col);

                if (selected)
                {
                    var sz = _menuFont.MeasureString(_items[i]);
                    sb.Draw(_pixel,
                        new Rectangle((int)x, (int)(y + sz.Y + 1), (int)sz.X, 1),
                        new Color(TextWhite.R, TextWhite.G, TextWhite.B, alpha));
                }
            }
        }

        private void DrawFooter(SpriteBatch sb, int sw, int sh, byte fa)
        {
            const string HINT = "W/S / arrows / mouse to navigate   enter to select";
            var sz = _menuFont.MeasureString(HINT);
            sb.DrawString(_menuFont, HINT,
                new Vector2((sw - sz.X) * 0.5f, sh - 28f),
                new Color(TextDim.R, TextDim.G, TextDim.B, (byte)(fa * 0.18f)));
        }
    }
}
