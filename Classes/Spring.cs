using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GalactaJumperMo.Classes
{
    public class Spring
    {
        public Vector2 Position { get; private set; }
        public Rectangle Bounds { get; private set; }

        private readonly Rectangle _baseTileSource;
        private int _currentFrame = 0;
        private float _animTimer = 0f;
        private bool _isAnimating = false;

        private const int SpringWidth = 16;
        private const int SpringHeight = 16;
        private const float BounceForceY = -430f;
        private const float BounceForceX = 220f;

        private const float FrameDuration = 0.06f; // speed of spring animation

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

            _baseTileSource = tileSource == default
                ? new Rectangle(80, 144, 16, 16)
                : tileSource;
        }

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

        public void Trigger()
        {
            _isAnimating = true;
            _currentFrame = 0;
            _animTimer = 0f;
            retriggerTimer = RetriggerDelay;
        }

        public Vector2 GetBounceForce(bool facingLeft)
        {
            return new Vector2(facingLeft ? -BounceForceX : BounceForceX, BounceForceY);
        }

        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (retriggerTimer > 0f)
            {
                retriggerTimer -= dt;
                if (retriggerTimer < 0f) retriggerTimer = 0f;
            }

            if (_isAnimating)
            {
                _animTimer += dt;

                if (_animTimer >= FrameDuration)
                {
                    _animTimer = 0f;
                    _currentFrame++;

                    if (_currentFrame >= 3)
                    {
                        _currentFrame = 0;
                        _isAnimating = false;
                    }
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D tileset)
        {
            if (tileset == null) return;

            Rectangle currentSource = new Rectangle(
                _baseTileSource.X + (_currentFrame * _baseTileSource.Width),
                _baseTileSource.Y,
                _baseTileSource.Width,
                _baseTileSource.Height
            );

            spriteBatch.Draw(
                tileset,
                Bounds,
                currentSource,
                Color.White
            );
        }
    }
}