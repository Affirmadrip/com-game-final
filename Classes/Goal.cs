using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GalactaJumperMo.Classes
{
    public class Goal
    {
        public Rectangle Bounds;
        public Rectangle TileSource { get; private set; } 

        public Goal(Vector2 position, Rectangle tileSource = default)
        {
            Bounds = new Rectangle((int)position.X, (int)position.Y, 16, 16); 
            TileSource = tileSource;
        }

        public bool CheckCollision(Rectangle playerBounds)
        {
            return Bounds.Intersects(playerBounds);
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D texture)
        {
            if (TileSource != Rectangle.Empty)
            {
                spriteBatch.Draw(texture, Bounds, TileSource, Color.White);
            }
            else
            {
                spriteBatch.Draw(texture, Bounds, Color.White);
            }
        }
    }
}