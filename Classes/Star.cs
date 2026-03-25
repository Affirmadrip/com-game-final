using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GalactaJumperMo.Classes
{
    public class Star
    {
        public Texture2D Texture;
        public Vector2 Position;
        public bool IsCollected;

        public Rectangle Bounds
        {
            get
            {
                return new Rectangle((int)Position.X, (int)Position.Y, Texture.Width, Texture.Height);
            }
        }

        public Star(Texture2D texture, Vector2 position)
        {
            Texture = texture;
            Position = position;
            IsCollected = false;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (!IsCollected)
            {
                spriteBatch.Draw(Texture, Position, Color.White);
            }
        }
    }
}