using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace GalactaJumperMo.Classes
{
    public class Enemy
    {
        public Vector2 Position;
        private Vector2 velocity;
        private float speed = 75f; 
        private float gravity = 500f;
        private int direction = 1;

        // Hitbox 
        public Rectangle Bounds => new Rectangle((int)Position.X + 6, (int)Position.Y + 6, 20, 24);

        private int animFrame;
        private float animTimer;
        private float frameDuration = 0.15f;

        public Enemy(Vector2 startPos)
        {
            Position = startPos;
        }

        public void Update(GameTime gameTime, Stage stage)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            velocity.X = speed * direction;
            velocity.Y += gravity * dt;

            Vector2 oldPosition = Position;
            Position.X += velocity.X * dt;
            Position.Y += velocity.Y * dt;

            bool isOnGround = false;

            // Collision 
            foreach (Rectangle platform in stage.Platforms)
            {
                if (Bounds.Intersects(platform))
                {
                    float footBottom = oldPosition.Y + 30;

                    if (velocity.Y > 0 && footBottom <= platform.Top + 15)
                    {
                        Position.Y = platform.Top - 30;
                        velocity.Y = 0;
                        isOnGround = true;
                    }
                    // flip
                    else
                    {
                        direction *= -1;
                        Position.X = oldPosition.X;
                    }
                }
            }

            // Patrol Logic
            if (isOnGround)
            {
                float checkX = direction > 0 ? Bounds.Right + 5 : Bounds.Left - 5;
                Vector2 checkPoint = new Vector2(checkX, Bounds.Bottom + 5);

                bool holeAhead = true;
                foreach (Rectangle platform in stage.Platforms)
                {
                    if (platform.Contains(checkPoint))
                    {
                        holeAhead = false;
                        break;
                    }
                }

                if (holeAhead)
                {
                    direction *= -1; 
                    Position.X = oldPosition.X;
                }
            }

            //  Animation 6 frames
            animTimer += dt;
            if (animTimer >= frameDuration)
            {
                animTimer = 0;
                animFrame = (animFrame + 1) % 6;
            }
        }

        public void Draw(SpriteBatch sb, Texture2D texture)
        {
            Color tint = Color.White;

            int frameWidth = texture.Width / 6;
            int frameHeight = texture.Height;

            Rectangle sourceRect = new Rectangle(animFrame * frameWidth, 0, frameWidth, frameHeight);

            Vector2 origin = new Vector2(frameWidth / 2f, frameHeight / 2f);

            Vector2 drawPos = new Vector2(Position.X + 16, Position.Y + 12);

            SpriteEffects flip = direction < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            float scale = 1.0f;

            sb.Draw(texture, drawPos, sourceRect, tint, 0f, origin, scale, flip, 0f);
        }
    }
}