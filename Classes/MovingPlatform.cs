using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace GalactaJumperMo.Classes
{
    public class MovingPlatform
    {
        public Vector2 StartPosition;
        public Vector2 Position;
        public Vector2 PreviousPosition;
        public Vector2 Delta;

        public int Width;
        public int Height;

        public string Axis;
        public float Range;
        public float Speed;

        private int _direction = 1;

        public Rectangle Bounds =>
            new Rectangle(
                (int)MathF.Round(Position.X),
                (int)MathF.Round(Position.Y),
                Width,
                Height
            );

        public MovingPlatform(Vector2 startPosition, int width, int height,
                              string axis, float range, float speed)
        {
            StartPosition = startPosition;
            Position = startPosition;
            PreviousPosition = startPosition;
            Width = width;
            Height = height;
            Axis = axis ?? "horizontal";
            Range = range;
            Speed = speed;
        }

        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            PreviousPosition = Position;

            if (Axis == "vertical")
            {
                Position.Y += Speed * _direction * dt;

                if (Position.Y > StartPosition.Y + Range)
                {
                    Position.Y = StartPosition.Y + Range;
                    _direction = -1;
                }
                else if (Position.Y < StartPosition.Y)
                {
                    Position.Y = StartPosition.Y;
                    _direction = 1;
                }
            }
            else
            {
                Position.X += Speed * _direction * dt;

                if (Position.X > StartPosition.X + Range)
                {
                    Position.X = StartPosition.X + Range;
                    _direction = -1;
                }
                else if (Position.X < StartPosition.X)
                {
                    Position.X = StartPosition.X;
                    _direction = 1;
                }
            }

            Delta = Position - PreviousPosition;
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D tilemap, Rectangle sourceRect)
        {
            Rectangle dest = new Rectangle(
                (int)MathF.Round(Position.X),
                (int)MathF.Round(Position.Y),
                Width,
                Height
            );

            spriteBatch.Draw(tilemap, dest, sourceRect, Color.White);
        }
    }
}