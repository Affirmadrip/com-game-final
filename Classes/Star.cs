using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GalactaJumperMo.Classes
{
    public class Star
    {
        public Texture2D Texture;
        public Vector2 Position;
        public bool IsCollected;
        public int Checkpoint { get; private set; }

        private readonly bool _usesTileSource;
        private readonly Rectangle _tileSource;
        private float _animationTimer = 0f;
        private float _bobOffset = 0f;

        public float Scale = 0.05f;

        public Rectangle Bounds
        {
            get
            {
                if (_usesTileSource)
                {
                    return new Rectangle(
                        (int)Position.X,
                        (int)Position.Y,
                        _tileSource.Width,
                        _tileSource.Height
                    );
                }

                return new Rectangle(
                    (int)Position.X, 
                    (int)Position.Y, 
                    Texture == null ? 0 : (int)(Texture.Width * Scale),
                    Texture == null ? 0 : (int)(Texture.Height * Scale)
                );
            }
        }

        public Star(Texture2D texture, Vector2 position, int checkpoint = 0)
        {
            Texture = texture;
            Position = position;
            Checkpoint = checkpoint;
            IsCollected = false;
            _usesTileSource = false;
            _tileSource = Rectangle.Empty;
        }

        public Star(Vector2 position, int checkpoint, Rectangle tileSource)
        {
            Texture = null;
            Position = position;
            Checkpoint = checkpoint;
            IsCollected = false;
            _usesTileSource = true;
            _tileSource = tileSource;
        }

        public void Update(GameTime gameTime)
        {
            if (IsCollected) return;

            _animationTimer += (float)gameTime.ElapsedGameTime.TotalSeconds * 3f;
            _bobOffset = (float)System.Math.Sin(_animationTimer) * 3f;
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D tileset = null)
        {
            if (!IsCollected)
            {
                Vector2 drawPos = new Vector2(Position.X, Position.Y + _bobOffset);

                if (_usesTileSource && tileset != null)
                {
                    spriteBatch.Draw(
                        tileset,
                        new Rectangle((int)drawPos.X, (int)drawPos.Y, _tileSource.Width, _tileSource.Height),
                        _tileSource,
                        Color.White
                    );
                }
                else if (Texture != null)
                {
                    spriteBatch.Draw(
                        Texture,
                        drawPos,
                        null,
                        Color.White,
                        0f,
                        Vector2.Zero,
                        Scale,
                        SpriteEffects.None,
                        0f
                    );
                }
            }
        }
    }
}