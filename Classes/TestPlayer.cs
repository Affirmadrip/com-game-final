using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace GalactaJumperMo.Classes
{
    public class TestPlayer
    {
        public Vector2 Position;
        private Vector2 velocity;

        private float speed = 220f;
        private float jumpForce = -430f;
        private float gravity = 1000f;

        private bool isOnGround = false;

        public Rectangle Bounds =>
            new Rectangle((int)Position.X, (int)Position.Y, 32, 32);

        public void Update(GameTime gameTime, Stage stage)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            KeyboardState k = Keyboard.GetState();

            if (k.IsKeyDown(Keys.A))
                velocity.X = -speed;
            else if (k.IsKeyDown(Keys.D))
                velocity.X = speed;
            else
                velocity.X = 0;

            if (k.IsKeyDown(Keys.Space) && isOnGround)
            {
                velocity.Y = jumpForce;
                isOnGround = false;
            }

            Rectangle oldBounds = Bounds;

            // Horizontal move
            Position.X += velocity.X * dt;

            // Vertical move
            velocity.Y += gravity * dt;
            Position.Y += velocity.Y * dt;

            isOnGround = false;
            Rectangle newBounds = Bounds;

            foreach (Rectangle platform in stage.Platforms)
            {
                if (!newBounds.Intersects(platform))
                    continue;

                // Landing from above
                if (velocity.Y >= 0 && oldBounds.Bottom <= platform.Top + 6)
                {
                    Position.Y = platform.Top - Bounds.Height;
                    velocity.Y = 0;
                    isOnGround = true;
                    newBounds = Bounds;
                }
                // Hitting underside
                else if (velocity.Y < 0 && oldBounds.Top >= platform.Bottom - 6)
                {
                    Position.Y = platform.Bottom;
                    velocity.Y = 0;
                    newBounds = Bounds;
                }
            }
        }

        public void ResetVelocity()
        {
            velocity = Vector2.Zero;
            isOnGround = false;
        }
    }
}