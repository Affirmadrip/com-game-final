using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GalactaJumperMo.Classes
{
    public enum CompleteAction { None, MainMenu, Exit }

    public class CompleteScreen : Screen
    {
        private readonly SpriteFont _titleFont;
        private readonly SpriteFont _menuFont;
        private readonly Texture2D _pixel;

        private readonly string _sideQuestText;
        private readonly string _timeText;

        private readonly List<string> _items = new();
        private readonly List<CompleteAction> _actions = new();
        private int _selectedIndex = 0;

        private readonly float[] _itemSlide;
        private readonly float[] _itemReveal;

        private KeyboardState _prevKb;
        private MouseState _prevMs;

        private float _time = 0f;
        private float _fadeIn = 0f;
        private bool _confirmed = false;
        private float _exitFade = 0f;

        public CompleteAction PendingAction { get; private set; } = CompleteAction.None;

        public CompleteScreen(
            SpriteFont titleFont,
            SpriteFont menuFont,
            Texture2D pixel,
            string sideQuestText,
            string timeText)
        {
            _titleFont = titleFont;
            _menuFont = menuFont;
            _pixel = pixel;

            _sideQuestText = sideQuestText ?? "";
            _timeText = timeText ?? "";

            _items.Add("Main Menu"); _actions.Add(CompleteAction.MainMenu);
            _items.Add("Exit");      _actions.Add(CompleteAction.Exit);

            _itemSlide = new float[_items.Count];
            _itemReveal = new float[_items.Count];

            _prevKb = Keyboard.GetState();
            _prevMs = Mouse.GetState();
        }

        public override void Update(GameTime gt, int sw, int sh)
        {
            float dt = (float)gt.ElapsedGameTime.TotalSeconds;
            _time += dt;
            _fadeIn = Math.Min(1f, _fadeIn + dt * 1.2f);

            for (int i = 0; i < _items.Count; i++)
            {
                float delay = 0.75f + i * 0.14f;
                if (_time > delay)
                    _itemReveal[i] = Math.Min(1f, _itemReveal[i] + dt * 2.8f);
            }

            if (_confirmed)
            {
                _exitFade = Math.Min(1f, _exitFade + dt * 3.5f);
                if (_exitFade >= 1f)
                    PendingAction = _actions[_selectedIndex];
                return;
            }

            for (int i = 0; i < _items.Count; i++)
            {
                _itemSlide[i] = MathHelper.Lerp(
                    _itemSlide[i],
                    i == _selectedIndex ? 18f : 0f,
                    dt * 9f
                );
            }

            var kb = Keyboard.GetState();
            var ms = Mouse.GetState();

            if (Pressed(Keys.Down, kb) || Pressed(Keys.S, kb))
                _selectedIndex = (_selectedIndex + 1) % _items.Count;
            if (Pressed(Keys.Up, kb) || Pressed(Keys.W, kb))
                _selectedIndex = (_selectedIndex - 1 + _items.Count) % _items.Count;

            float lineH = 56f;
            float x0 = sw * 0.5f - 90f;
            float y0 = sh * 0.70f;

            for (int i = 0; i < _items.Count; i++)
            {
                var sz = _menuFont.MeasureString(_items[i]);
                var rect = new Rectangle(
                    (int)x0 - 20,
                    (int)(y0 + i * lineH) - 6,
                    (int)sz.X + 40,
                    (int)sz.Y + 12
                );

                if (rect.Contains(ms.Position))
                {
                    _selectedIndex = i;
                    break;
                }
            }

            bool confirm =
                Pressed(Keys.Enter, kb) ||
                Pressed(Keys.Space, kb) ||
                (ms.LeftButton == ButtonState.Pressed &&
                 _prevMs.LeftButton == ButtonState.Released);

            _prevKb = kb;
            _prevMs = ms;

            if (confirm)
                _confirmed = true;
        }

        private bool Pressed(Keys k, KeyboardState cur)
        {
            return cur.IsKeyDown(k) && !_prevKb.IsKeyDown(k);
        }

        public override void Draw(SpriteBatch sb, int sw, int sh)
        {
            DrawBg(sb, _pixel, sw, sh);
            DrawVignette(sb, _pixel, sw, sh);

            byte fa = (byte)(_fadeIn * 255f);

            DrawTitle(sb, sw, sh, fa);
            DrawSummary(sb, sw, sh, fa);
            DrawMenuItems(sb, sw, sh, fa);

            if (_exitFade > 0f)
            {
                sb.Draw(
                    _pixel,
                    new Rectangle(0, 0, sw, sh),
                    new Color((byte)0, (byte)0, (byte)0, (byte)(_exitFade * 255f))
                );
            }
        }

        private void DrawTitle(SpriteBatch sb, int sw, int sh, byte fa)
        {
            const string TITLE = "COMPLETE";
            var tsz = _titleFont.MeasureString(TITLE);

            float tx = (sw - tsz.X) * 0.5f;
            float ty = sh * 0.17f;

            Color tc = new Color(TextWhite.R, TextWhite.G, TextWhite.B, fa);
            sb.DrawString(_titleFont, TITLE, new Vector2(tx, ty), tc);

            float lineX = (sw - 120f) * 0.5f;
            float lineY = ty + tsz.Y + 18f;

            sb.Draw(
                _pixel,
                new Rectangle((int)lineX, (int)lineY, 120, 1),
                new Color((byte)255, (byte)255, (byte)255, (byte)(fa * 0.20f))
            );
        }

        private void DrawSummary(SpriteBatch sb, int sw, int sh, byte fa)
        {
            float centerX = sw * 0.5f;
            float startY = sh * 0.38f;
            float gap = 42f;

            byte dimA = (byte)(fa * 0.80f);
            Color labelCol = new Color(TextDim.R, TextDim.G, TextDim.B, dimA);
            Color valueCol = new Color(TextWhite.R, TextWhite.G, TextWhite.B, fa);

            string sideLabel = "Side Quest";
            string timeLabel = "Time Taken";

            Vector2 sideLabelSize = _menuFont.MeasureString(sideLabel);
            Vector2 sideTextSize = _menuFont.MeasureString(_sideQuestText);
            Vector2 timeLabelSize = _menuFont.MeasureString(timeLabel);
            Vector2 timeTextSize = _menuFont.MeasureString(_timeText);

            sb.DrawString(
                _menuFont,
                sideLabel,
                new Vector2(centerX - sideLabelSize.X * 0.5f, startY),
                labelCol
            );

            sb.DrawString(
                _menuFont,
                _sideQuestText,
                new Vector2(centerX - sideTextSize.X * 0.5f, startY + 12f + sideLabelSize.Y),
                valueCol
            );

            sb.DrawString(
                _menuFont,
                timeLabel,
                new Vector2(centerX - timeLabelSize.X * 0.5f, startY + gap + sideLabelSize.Y + 18f),
                labelCol
            );

            sb.DrawString(
                _menuFont,
                _timeText,
                new Vector2(centerX - timeTextSize.X * 0.5f, startY + gap + sideLabelSize.Y + 30f + timeLabelSize.Y),
                valueCol
            );
        }

        private void DrawMenuItems(SpriteBatch sb, int sw, int sh, byte fa)
        {
            const float LINE_H = 56f;
            float x0 = sw * 0.5f - 70f;
            float y0 = sh * 0.70f;

            for (int i = 0; i < _items.Count; i++)
            {
                float rev = _itemReveal[i];
                if (rev <= 0.01f) continue;

                bool selected = i == _selectedIndex;
                float x = x0 + _itemSlide[i];
                float y = y0 + i * LINE_H;
                byte alpha = (byte)(fa * rev);

                Color col = selected
                    ? new Color(TextWhite.R, TextWhite.G, TextWhite.B, alpha)
                    : new Color(TextDim.R, TextDim.G, TextDim.B, alpha);

                sb.DrawString(_menuFont, _items[i], new Vector2(x, y), col);

                if (selected)
                {
                    var sz = _menuFont.MeasureString(_items[i]);
                    sb.Draw(
                        _pixel,
                        new Rectangle((int)x, (int)(y + sz.Y + 1), (int)sz.X, 1),
                        new Color(TextWhite.R, TextWhite.G, TextWhite.B, alpha)
                    );
                }
            }
        }
    }
}