using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GalactaJumperMo.Classes
{
    public enum PauseAction { None, Resume, Settings, MainMenu, Exit }

    public class PauseScreen : Screen
    {
        private readonly SpriteFont _titleFont;
        private readonly SpriteFont _menuFont;
        private readonly Texture2D  _pixel;

        private readonly List<string>      _items   = new();
        private readonly List<PauseAction> _actions = new();
        private int _selectedIndex = 0;

        private readonly float[] _itemSlide;
        private readonly float[] _itemReveal;

        private KeyboardState _prevKb;
        private MouseState    _prevMs;

        private float _time      = 0f;
        private float _fadeIn    = 0f;
        private float _exitFade  = 0f;
        private bool  _doExit    = false;
        private PauseAction _exitAction = PauseAction.None;

        // ── Confirmation state ────────────────────────────────────────────────
        private bool        _confirming   = false;
        private PauseAction _confirmTarget = PauseAction.None;
        private int         _confirmIndex  = 1;   // 0 = Yes, 1 = No (default No)

        public PauseAction PendingAction { get; private set; } = PauseAction.None;

        public PauseScreen(SpriteFont titleFont, SpriteFont menuFont, Texture2D pixel)
        {
            _titleFont = titleFont;
            _menuFont  = menuFont;
            _pixel     = pixel;

            _items.Add("Resume");    _actions.Add(PauseAction.Resume);
            _items.Add("Settings");  _actions.Add(PauseAction.Settings);
            _items.Add("Main Menu"); _actions.Add(PauseAction.MainMenu);
            _items.Add("Exit");      _actions.Add(PauseAction.Exit);

            _itemSlide  = new float[_items.Count];
            _itemReveal = new float[_items.Count];
        }

        // Call this every time the pause screen is opened.
        // Captures the current keyboard state so held keys (e.g. ESC) are not
        // re-fired on the very first Update frame.
        public void OnOpen()
        {
            _time          = 0f;
            _fadeIn        = 0f;
            _exitFade      = 0f;
            _doExit        = false;
            _confirming    = false;
            _selectedIndex = 0;
            PendingAction  = PauseAction.None;
            _prevKb        = Keyboard.GetState();   // ← key-bleed fix
            _prevMs        = Mouse.GetState();
            for (int i = 0; i < _items.Count; i++) { _itemSlide[i] = 0f; _itemReveal[i] = 0f; }
        }

        public override void Update(GameTime gt, int sw, int sh)
        {
            float dt = (float)gt.ElapsedGameTime.TotalSeconds;
            _time   += dt;
            _fadeIn  = Math.Min(1f, _fadeIn + dt * 3.0f);

            // Animate item reveal
            for (int i = 0; i < _items.Count; i++)
            {
                float delay = 0.05f + i * 0.08f;
                if (_time > delay)
                    _itemReveal[i] = Math.Min(1f, _itemReveal[i] + dt * 4f);
            }

            // Flash-out before firing the confirmed action
            if (_doExit)
            {
                _exitFade = Math.Min(1f, _exitFade + dt * 5f);
                if (_exitFade >= 1f) PendingAction = _exitAction;
                return;
            }

            var kb = Keyboard.GetState();
            var ms = Mouse.GetState();

            if (_confirming)
            {
                UpdateConfirm(kb, ms, sw, sh);
            }
            else
            {
                UpdateMenu(kb, ms, sw, sh, dt);
            }

            _prevKb = kb;
            _prevMs = ms;
        }

        private void UpdateMenu(KeyboardState kb, MouseState ms, int sw, int sh, float dt)
        {
            if (Pressed(Keys.Escape, kb)) { FireAction(PauseAction.Resume); return; }

            if (Pressed(Keys.Down, kb)) _selectedIndex = (_selectedIndex + 1) % _items.Count;
            if (Pressed(Keys.Up,   kb)) _selectedIndex = (_selectedIndex - 1 + _items.Count) % _items.Count;

            for (int i = 0; i < _items.Count; i++)
                _itemSlide[i] = MathHelper.Lerp(_itemSlide[i],
                    i == _selectedIndex ? 18f : 0f, dt * 9f);

            // Mouse hover
            float lineH = 52f;
            float x0    = 80f;
            float y0    = sh - _items.Count * lineH - 90f;
            for (int i = 0; i < _items.Count; i++)
            {
                var sz   = _menuFont.MeasureString(_items[i]);
                var rect = new Rectangle((int)x0 - 20, (int)(y0 + i * lineH) - 6,
                                         (int)sz.X + 40, (int)sz.Y + 12);
                if (rect.Contains(ms.Position)) { _selectedIndex = i; break; }
            }

            bool confirm = Pressed(Keys.Enter, kb) || Pressed(Keys.Space, kb) ||
                           (ms.LeftButton == ButtonState.Pressed &&
                            _prevMs.LeftButton == ButtonState.Released);
            if (!confirm) return;

            var action = _actions[_selectedIndex];
            if (action == PauseAction.MainMenu || action == PauseAction.Exit)
            {
                // Require confirmation before destructive actions
                _confirming    = true;
                _confirmTarget = action;
                _confirmIndex  = 1;   // default to No
            }
            else
            {
                FireAction(action);
            }
        }

        private void UpdateConfirm(KeyboardState kb, MouseState ms, int sw, int sh)
        {
            if (Pressed(Keys.Escape, kb)) { _confirming = false; return; }
            if (Pressed(Keys.Up,    kb) || Pressed(Keys.Left,  kb))
                _confirmIndex = Math.Max(0, _confirmIndex - 1);
            if (Pressed(Keys.Down,  kb) || Pressed(Keys.Right, kb))
                _confirmIndex = Math.Min(1, _confirmIndex + 1);

            float lineH = 52f;
            float x0 = 80f;
            float y0 = sh - 3 * lineH - 90f;
            string[] opts = { "Yes", "No" };

            for (int i = 0; i < 2; i++)
            {
                float y = y0 + lineH + i * lineH;
                var sz = _menuFont.MeasureString(opts[i]);
                var rect = new Rectangle((int)x0 - 20, (int)y - 6,
                                         (int)sz.X + 40, (int)sz.Y + 12);
                if (rect.Contains(ms.Position))
                {
                    _confirmIndex = i;
                    break;
                }
            }

            bool confirm = Pressed(Keys.Enter, kb) || Pressed(Keys.Space, kb) ||
                           (ms.LeftButton == ButtonState.Pressed &&
                            _prevMs.LeftButton == ButtonState.Released);
            if (!confirm) return;

            if (_confirmIndex == 0)   // Yes
                FireAction(_confirmTarget);
            else                       // No
                _confirming = false;
        }

        private void FireAction(PauseAction action)
        {
            if (action == PauseAction.Resume || action == PauseAction.Settings)
            {
                PendingAction = action;
            }
            else
            {
                _doExit    = true;
                _exitAction = action;
            }
        }

        private bool Pressed(Keys k, KeyboardState cur) => cur.IsKeyDown(k) && !_prevKb.IsKeyDown(k);

        // ═══════════════════════════════════════════════════════════════════════
        // DRAW
        // ═══════════════════════════════════════════════════════════════════════
        public override void Draw(SpriteBatch sb, int sw, int sh)
        {
            // Dark overlay on top of the game world
            byte oa = (byte)(_fadeIn * 185f);
            sb.Draw(_pixel, new Rectangle(0, 0, sw, sh),
                new Color((byte)4, (byte)6, (byte)16, oa));

            DrawVignette(sb, _pixel, sw, sh, (byte)(_fadeIn * 120));

            byte fa = (byte)(_fadeIn * 255f);
            DrawTitle(sb, fa);

            if (_confirming)
                DrawConfirm(sb, sh, fa);
            else
                DrawItems(sb, sh, fa);

            // Flash-out overlay
            if (_exitFade > 0f)
                sb.Draw(_pixel, new Rectangle(0, 0, sw, sh),
                    new Color((byte)0, (byte)0, (byte)0, (byte)(_exitFade * 255f)));
        }

        private void DrawTitle(SpriteBatch sb, byte fa)
        {
            float sc = (float)Math.Sin(_time * 0.75f) * 0.008f + 1f;
            sb.DrawString(_titleFont, "PAUSED", new Vector2(72f, 58f),
                new Color(TextWhite.R, TextWhite.G, TextWhite.B, fa),
                0f, Vector2.Zero, sc, SpriteEffects.None, 0f);

            float divY = 58f + _titleFont.LineSpacing * sc + 10f;
            sb.Draw(_pixel, new Rectangle(72, (int)divY, 120, 1),
                new Color((byte)255, (byte)255, (byte)255, (byte)(fa * 0.20f)));
        }

        private void DrawItems(SpriteBatch sb, int sh, byte fa)
        {
            const float LINE_H = 52f;
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

        private void DrawConfirm(SpriteBatch sb, int sh, byte fa)
        {
            const float LINE_H = 52f;
            float x0 = 80f;
            float y0 = sh - 3 * LINE_H - 90f;

            // Question
            string question = _confirmTarget == PauseAction.Exit
                ? "Exit the game?" : "Return to main menu?";
            byte qa = (byte)(fa * 0.65f);
            sb.DrawString(_menuFont, question, new Vector2(x0, y0),
                new Color(TextDim.R, TextDim.G, TextDim.B, qa));

            // Yes / No
            string[] opts = { "Yes", "No" };
            for (int i = 0; i < 2; i++)
            {
                float y   = y0 + LINE_H + i * LINE_H;
                bool  sel = i == _confirmIndex;
                byte  a   = fa;
                Color col = sel
                    ? new Color(TextWhite.R, TextWhite.G, TextWhite.B, a)
                    : new Color(TextDim.R,   TextDim.G,   TextDim.B,   a);

                sb.DrawString(_menuFont, opts[i], new Vector2(x0, y), col);

                if (sel)
                {
                    var sz = _menuFont.MeasureString(opts[i]);
                    sb.Draw(_pixel,
                        new Rectangle((int)x0, (int)(y + sz.Y + 1), (int)sz.X, 1),
                        new Color(TextWhite.R, TextWhite.G, TextWhite.B, a));
                }
            }
        }
    }
}
