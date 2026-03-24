using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace GalactaJumperMo.Classes
{
    public class Enemy
    {
        public Vector2 Position;
        private Vector2 basePosition;
        private float startX;
        private float speed = 80f;
        private int direction = 1;
        private float patrolRange = 150f;

        // --- Sine Wave ---
        private float sineTimer = 0f;
        private float waveAmplitude = 15f;
        private float waveSpeed = 4f;

        private float alpha = 1.0f;
        private float alphaTimer = 0f;
        private float fadeSpeed = 2f;
        private float minAlpha = 0.2f;

        public bool IsPhaseOut => alpha < 0.5f;

        public bool IsDead = false;

        public Rectangle Bounds => new Rectangle((int)Position.X + 6, (int)Position.Y + 6, 20, 24);

        private int animFrame;
        private float animTimer;
        private float frameDuration = 0.12f;

        public Enemy(Vector2 startPos)
        {
            basePosition = startPos;
            Position = startPos;
            startX = startPos.X;
        }

        public void Update(GameTime gameTime, Stage stage)
        {
            if (IsDead) return; 

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Fade
            alphaTimer += dt * fadeSpeed;
            float lerpFactor = (float)(Math.Cos(alphaTimer) + 1f) / 2f;
            alpha = MathHelper.Lerp(minAlpha, 1.0f, lerpFactor);

            //  Sine Wave
            basePosition.X += speed * direction * dt;

            if (basePosition.X > startX + patrolRange) { direction = -1; basePosition.X = startX + patrolRange; }
            else if (basePosition.X < startX - patrolRange) { direction = 1; basePosition.X = startX - patrolRange; }

            sineTimer += dt * waveSpeed;
            Position.X = basePosition.X;
            Position.Y = basePosition.Y + (float)Math.Sin(sineTimer) * waveAmplitude;

            // 3. ป้องกันการชนพื้น
            foreach (Rectangle platform in stage.Platforms)
            {
                if (Bounds.Intersects(platform))
                {
                    if (Position.Y + 28 > platform.Top && Position.Y < platform.Top)
                        basePosition.Y = platform.Top - 30 - waveAmplitude;
                    else
                    {
                        direction *= -1;
                        basePosition.X += direction * 5;
                    }
                    break;
                }
            }

            // Animation
            animTimer += dt;
            if (animTimer >= frameDuration)
            {
                animTimer = 0;
                animFrame = (animFrame + 1) % 6;
            }
        }

        public void Draw(SpriteBatch sb, Texture2D texture)
        {
            if (IsDead || texture == null) return;

            //  IsPhaseOut 
            Color tintColor = IsPhaseOut ? Color.LightBlue * alpha : Color.White * alpha;

            int frameWidth = texture.Width / 6;
            int frameHeight = texture.Height;
            Rectangle sourceRect = new Rectangle(animFrame * frameWidth, 0, frameWidth, frameHeight);
            Vector2 origin = new Vector2(frameWidth / 2f, frameHeight / 2f);
            Vector2 drawPos = new Vector2(Position.X + 16, Position.Y + 12);

            SpriteEffects flip = direction < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            sb.Draw(texture, drawPos, sourceRect, tintColor, 0f, origin, 1.0f, flip, 0f);
        }
    }
}