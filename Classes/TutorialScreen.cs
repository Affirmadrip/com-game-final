using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GalactaJumperMo.Classes
{
    public enum TutorialAction { None, Back }

    public class TutorialScreen : Screen
    {
        private readonly SpriteFont _titleFont;
        private readonly SpriteFont _menuFont;
        private readonly Texture2D  _pixel;

        private KeyboardState _prevKb;
        private MouseState    _prevMs;

        private float _time   = 0f;
        private float _fadeIn = 0f;
        private float _backSlide  = 0f;
        private float _backReveal = 0f;

        public TutorialAction PendingAction { get; private set; } = TutorialAction.None;

        private static readonly (string head, string[] lines)[] Sections =
        {
            ("OBJECTIVE", new[]
            {
                "Collect all Stars before time runs out.",
                "They are scattered across the world — or can be taken by force.",
            }),
            ("MOVEMENT", new[]
            {
                "Speed builds the longer you run.",
                "Momentum is your greatest strength — and your greatest risk.",
            }),
            ("DASH", new[]
            {
                "Press Left Shift to dash in any direction.",
                "A direct hit deals damage to enemies.",
                "Stamina limits how often you can dash. Use it wisely.",
            }),
            ("THE REST...", new[]
            {
                "Some things are better discovered yourself.",
            }),
            ("CONTROLS", new[]
            {
                "Move         Arrow Keys / A  D",
                "Jump         W  /  Up Arrow",
                "Dash         Left Shift  +  Direction",
                "Pause        ESC",
            }),
        };

        public TutorialScreen(SpriteFont titleFont, SpriteFont menuFont, Texture2D pixel)
        {
            _titleFont = titleFont;
            _menuFont  = menuFont;
            _pixel     = pixel;
            _prevKb    = Keyboard.GetState();   // key-bleed fix
            _prevMs    = Mouse.GetState();
        }

        public override void Update(GameTime gt, int sw, int sh)
        {
            float dt = (float)gt.ElapsedGameTime.TotalSeconds;
            _time   += dt;
            _fadeIn  = Math.Min(1f, _fadeIn + dt * 1.2f);

            if (_time > 0.3f)
                _backReveal = Math.Min(1f, _backReveal + dt * 2.8f);

            _backSlide = MathHelper.Lerp(_backSlide, IsBackHovered(sw, sh) ? 18f : 0f, dt * 9f);

            var kb = Keyboard.GetState();
            var ms = Mouse.GetState();

            bool confirm = Pressed(Keys.Enter, kb) || Pressed(Keys.Space, kb) ||
                           Pressed(Keys.Escape, kb) ||
                           (ms.LeftButton == ButtonState.Pressed &&
                            _prevMs.LeftButton == ButtonState.Released && IsBackHovered(sw, sh));
            _prevKb = kb;
            _prevMs = ms;

            if (confirm) PendingAction = TutorialAction.Back;
        }

        private bool Pressed(Keys k, KeyboardState cur) => cur.IsKeyDown(k) && !_prevKb.IsKeyDown(k);

        private bool IsBackHovered(int sw, int sh)
        {
            var ms  = Mouse.GetState();
            var sz  = _menuFont.MeasureString("Back");
            var rect = new Rectangle(56, sh - 90 - 6, (int)sz.X + 40, (int)sz.Y + 12);
            return rect.Contains(ms.Position);
        }

        public override void Draw(SpriteBatch sb, int sw, int sh)
        {
            DrawBg(sb, _pixel, sw, sh);
            DrawVignette(sb, _pixel, sw, sh);

            byte fa = (byte)(_fadeIn * 255f);

            DrawTitle(sb, fa);
            DrawContent(sb, sw, sh, fa);
            DrawBack(sb, sh, fa);
        }

        private void DrawTitle(SpriteBatch sb, byte fa)
        {
            float sc = (float)Math.Sin(_time * 0.75f) * 0.010f + 1f;
            Color tc = new Color(TextWhite.R, TextWhite.G, TextWhite.B, fa);
            sb.DrawString(_titleFont, "HOW TO PLAY",
                new Vector2(72f, 58f), tc, 0f, Vector2.Zero, sc, SpriteEffects.None, 0f);

            float divY = 58f + _titleFont.LineSpacing * sc + 10f;
            sb.Draw(_pixel, new Rectangle(72, (int)divY, 120, 1),
                new Color((byte)255, (byte)255, (byte)255, (byte)(fa * 0.20f)));
        }

        private void DrawContent(SpriteBatch sb, int sw, int sh, byte fa)
        {
            float sc     = (float)Math.Sin(_time * 0.75f) * 0.010f + 1f;
            float startY = 58f + _titleFont.LineSpacing * sc + 36f;
            float col1X  = 72f;
            float col2X  = sw * 0.48f;
            float y      = startY;
            float lineH  = _menuFont.LineSpacing + 4f;

            int half = Sections.Length / 2 + 1;

            for (int s = 0; s < Sections.Length; s++)
            {
                bool  isRight = s >= half;
                float cx      = isRight ? col2X : col1X;
                if (isRight && s == half) y = startY;

                byte ha = (byte)(fa * 0.85f);
                sb.DrawString(_menuFont, Sections[s].head,
                    new Vector2(cx, y),
                    new Color(TextWhite.R, TextWhite.G, TextWhite.B, ha));
                y += lineH + 2f;

                foreach (var line in Sections[s].lines)
                {
                    byte la = (byte)(fa * 0.50f);
                    sb.DrawString(_menuFont, line,
                        new Vector2(cx + 12f, y),
                        new Color(TextDim.R, TextDim.G, TextDim.B, la));
                    y += lineH;
                }
                y += lineH * 0.6f;
            }
        }

        private void DrawBack(SpriteBatch sb, int sh, byte fa)
        {
            if (_backReveal <= 0.01f) return;

            float x     = 80f + _backSlide;
            float y     = sh - 90f;
            byte  alpha = (byte)(fa * _backReveal);
            bool  hover = IsBackHovered(0, sh) || _backSlide > 1f;

            Color col = hover
                ? new Color(TextWhite.R, TextWhite.G, TextWhite.B, alpha)
                : new Color(TextDim.R,   TextDim.G,   TextDim.B,   alpha);

            sb.DrawString(_menuFont, "Back", new Vector2(x, y), col);

            if (hover)
            {
                var sz = _menuFont.MeasureString("Back");
                sb.Draw(_pixel,
                    new Rectangle((int)x, (int)(y + sz.Y + 1), (int)sz.X, 1),
                    new Color(TextWhite.R, TextWhite.G, TextWhite.B, alpha));
            }
        }
    }
}
