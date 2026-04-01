using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GalactaJumperMo.Classes
{
    /// <summary>
    /// Represents a simple red spring block that bounces the player.
    /// </summary>
    public class Spring
    {
        public Vector2 Position { get; private set; }
        public Rectangle Bounds { get; private set; }

        private const int SpringWidth = 16;
        private const int SpringHeight = 16;
        private const float BounceForceY = -430f;
        private const float BounceForceX = 220f;

        private float compressionAmount = 0f;
        private const float RecoverySpeed = 8f;
        private float retriggerTimer = 0f;
        private const float RetriggerDelay = 0.15f;

        public Spring(Vector2 position, Rectangle tileSource = default)
        {
            Position = position;

            Bounds = new Rectangle(
                (int)position.X,
                (int)position.Y,
                SpringWidth,
                SpringHeight
            );
        }

        /// Check if the player touches the spring from any side.
        public bool CheckPlayerContact(Rectangle playerBounds)
        {
            if (retriggerTimer > 0f) return false;

            Rectangle triggerBounds = new Rectangle(
                Bounds.X - 2,
                Bounds.Y - 2,
                Bounds.Width + 4,
                Bounds.Height + 4
            );

            return triggerBounds.Intersects(playerBounds);
        }

        /// Trigger the spring bounce animation
        public void Trigger()
        {
            compressionAmount = 1f;
            retriggerTimer = RetriggerDelay;
        }

        /// Get directional bounce force based on current facing.
        public Vector2 GetBounceForce(bool facingLeft)
        {
            return new Vector2(facingLeft ? -BounceForceX : BounceForceX, BounceForceY);
        }

        /// Update spring animation
        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (retriggerTimer > 0f)
            {
                retriggerTimer -= dt;
                if (retriggerTimer < 0f) retriggerTimer = 0f;
            }

            if (compressionAmount > 0f)
            {
                compressionAmount -= RecoverySpeed * dt;
                if (compressionAmount < 0f) compressionAmount = 0f;
            }
        }

        /// Draw a red spring block with simple compression.
        public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
        {
            if (pixel != null)
            {
                int height = Math.Max(6, (int)(SpringHeight * (1f - compressionAmount * 0.35f)));
                int y = Bounds.Y + (SpringHeight - height);
                var drawRect = new Rectangle(Bounds.X, y, SpringWidth, height);
                spriteBatch.Draw(pixel, drawRect, Color.Red);
            }
        }
    }
}