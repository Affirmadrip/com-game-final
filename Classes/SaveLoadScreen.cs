using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GalactaJumperMo.Classes
{
    public enum SaveLoadAction { None, Continue, NewGame, DeleteSave, Back }

    public class SaveLoadScreen : Screen
    {
        private readonly SpriteFont _titleFont;
        private readonly SpriteFont _menuFont;
        private readonly Texture2D _pixel;
        private readonly GameSaveData _saveData;

        private readonly List<string> _items = new();
        private readonly List<SaveLoadAction> _actions = new();
        private int _selectedIndex = 0;

        private KeyboardState _prevKb;
        private MouseState _prevMs;

        private float _time = 0f;
        private float _fadeIn = 0f;
        private bool _confirmed = false;
        private float _exitFade = 0f;

        public SaveLoadAction PendingAction { get; private set; } = SaveLoadAction.None;

        public SaveLoadScreen(SpriteFont titleFont, SpriteFont menuFont, Texture2D pixel)
        {
            _titleFont = titleFont;
            _menuFont = menuFont;
            _pixel = pixel;
            _saveData = GameSaveData.Load();

            if (_saveData != null)
            {
                _items.Add("Continue Game");
                _actions.Add(SaveLoadAction.Continue);
            }

            _items.Add("New Game");
            _actions.Add(SaveLoadAction.NewGame);

            if (_saveData != null)
            {
                _items.Add("Delete Save");
                _actions.Add(SaveLoadAction.DeleteSave);
            }

            _items.Add("Back");
            _actions.Add(SaveLoadAction.Back);

            _prevKb = Keyboard.GetState();
            _prevMs = Mouse.GetState();
        }

        public GameSaveData GetSaveData() => _saveData;

        public override void Update(GameTime gt, int sw, int sh)
        {
            float dt = (float)gt.ElapsedGameTime.TotalSeconds;
            _time += dt;
            _fadeIn = Math.Min(1f, _fadeIn + dt * 1.2f);

            if (_confirmed)
            {
                _exitFade = Math.Min(1f, _exitFade + dt * 3.5f);
                if (_exitFade >= 1f) PendingAction = _actions[_selectedIndex];
                return;
            }

            var kb = Keyboard.GetState();
            var ms = Mouse.GetState();

            if (Pressed(Keys.Down, kb) || Pressed(Keys.S, kb))
                _selectedIndex = (_selectedIndex + 1) % _items.Count;
            if (Pressed(Keys.Up, kb) || Pressed(Keys.W, kb))
                _selectedIndex = (_selectedIndex - 1 + _items.Count) % _items.Count;

            float lineH = 56f;
            float mx0 = sw / 2f - 150f;
            float my0 = GetMenuStartY(sh, lineH);
            for (int i = 0; i < _items.Count; i++)
            {
                var sz = _menuFont.MeasureString(_items[i]);
                var rect = new Rectangle((int)(mx0 - 28), (int)(my0 + i * lineH) - 6,
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

        private float GetMenuStartY(int sh, float lineH)
        {
            float y = sh / 2f - _items.Count * lineH / 2f + 50f;
            if (_saveData != null)
                y += 70f;
            return y;
        }

        public override void Draw(SpriteBatch sb, int sw, int sh)
        {
            sb.Draw(_pixel, new Rectangle(0, 0, sw, sh), BgBase);

            byte fa = (byte)(_fadeIn * 255f);

            // Draw title
            string title = "LOAD GAME";
            Vector2 titleSize = _titleFont.MeasureString(title);
            Vector2 titlePos = new Vector2((sw - titleSize.X) / 2, sh / 4);
            sb.DrawString(_titleFont, title, titlePos, new Color(TextWhite.R, TextWhite.G, TextWhite.B, fa));

            // Draw save info if exists
            if (_saveData != null)
            {
                string saveAt = _saveData.SaveTime == default
                    ? "Unknown"
                    : _saveData.SaveTime.ToString("dd/MM/yyyy HH:mm");
                string saveInfoLine1 = $"Saved: {saveAt}";
                string saveInfoLine2 = $"Checkpoint: {_saveData.CurrentCheckpoint}";

                Vector2 info1Size = _menuFont.MeasureString(saveInfoLine1);
                Vector2 info1Pos = new Vector2((sw - info1Size.X) / 2, titlePos.Y + titleSize.Y + 24);
                sb.DrawString(_menuFont, saveInfoLine1, info1Pos,
                    new Color(TextDim.R, TextDim.G, TextDim.B, (byte)(fa * 0.75f)));

                Vector2 info2Size = _menuFont.MeasureString(saveInfoLine2);
                Vector2 info2Pos = new Vector2((sw - info2Size.X) / 2, info1Pos.Y + info1Size.Y + 6);
                sb.DrawString(_menuFont, saveInfoLine2, info2Pos,
                    new Color(TextDim.R, TextDim.G, TextDim.B, (byte)(fa * 0.75f)));
            }

            // Draw menu items
            float lineH = 56f;
            float x0 = sw / 2f - 150f;
            float y0 = GetMenuStartY(sh, lineH);

            for (int i = 0; i < _items.Count; i++)
            {
                bool selected = i == _selectedIndex;
                float x = x0;
                float y = y0 + i * lineH;

                Color col = selected
                    ? new Color(TextWhite.R, TextWhite.G, TextWhite.B, fa)
                    : new Color(TextDim.R, TextDim.G, TextDim.B, fa);

                sb.DrawString(_menuFont, _items[i], new Vector2(x, y), col);

                if (selected)
                {
                    var sz = _menuFont.MeasureString(_items[i]);
                    sb.Draw(_pixel,
                        new Rectangle((int)x, (int)(y + sz.Y + 1), (int)sz.X, 1),
                        new Color(TextWhite.R, TextWhite.G, TextWhite.B, fa));
                }
            }

            if (_exitFade > 0f)
                sb.Draw(_pixel, new Rectangle(0, 0, sw, sh),
                    new Color((byte)0, (byte)0, (byte)0, (byte)(_exitFade * 255f)));
        }
    }
}