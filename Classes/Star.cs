using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GalactaJumperMo.Classes
{
    public class Star
    {
        public Texture2D Texture;
        public Vector2 Position;
        public bool IsCollected;
        
        public float Scale = 0.05f; 

        public Rectangle Bounds
        {
            get
            {
                return new Rectangle(
                    (int)Position.X, 
                    (int)Position.Y, 
                    (int)(Texture.Width * Scale), 
                    (int)(Texture.Height * Scale)
                );
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
                spriteBatch.Draw(
                    Texture, 
                    Position, 
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