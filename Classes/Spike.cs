using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace GalactaJumperMo.Classes
{
    public class Spike
    {
        public Vector2 Position;
        public int Width;
        public int Height;
        public Rectangle TileSource;

        public Rectangle Bounds => new Rectangle(
            (int)Position.X,
            (int)Position.Y,
            Width,
            Height
        );

        // Smaller hitbox for better collision feel
        public Rectangle Hitbox => new Rectangle(
            (int)Position.X + 2,
            (int)Position.Y + 4,
            Width - 4,
            Height - 4
        );

        public Spike(Vector2 position, int width = 16, int height = 16, Rectangle tileSource = default)
        {
            Position = position;
            Width = width;
            Height = height;
            TileSource = tileSource == default ? new Rectangle(48, 144, 16, 16) : tileSource;
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D tilemap)
        {
            Rectangle dest = new Rectangle(
                (int)MathF.Round(Position.X),
                (int)MathF.Round(Position.Y),
                Width,
                Height
            );

            spriteBatch.Draw(tilemap, dest, TileSource, Color.White);
        }
    }
}
