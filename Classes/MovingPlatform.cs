using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace GalactaJumperMo.Classes
{
    public class MovingPlatform
    {
        public Vector2 Position;
        public Vector2 PreviousPosition;
        public Vector2 Delta;

        public int Width;
        public int Height;
        public float Speed;

        // Tile source from LDtk entity
        public Rectangle TileSource;

        // TargetNodes: list of waypoints (including start)
        public List<Vector2> TargetNodes;
        private int _currentNodeIndex = 0;
        private int _direction = 1; // 1 = forward, -1 = backward

        // Use TileSource size for bounds (collision)
        public Rectangle Bounds =>
            new Rectangle(
                (int)MathF.Round(Position.X),
                (int)MathF.Round(Position.Y),
                TileSource.Width,
                TileSource.Height
            );

        public MovingPlatform(Vector2 startPosition, int width, int height, float speed, List<Vector2> targetNodes = null, Rectangle? tileSource = null)
        {
            Position = startPosition;
            PreviousPosition = startPosition;
            Width = width;
            Height = height;
            Speed = speed;
            TileSource = tileSource ?? new Rectangle(64, 48, 48, 16);

            // Build node list: start position + all target nodes
            TargetNodes = new List<Vector2> { startPosition };
            if (targetNodes != null && targetNodes.Count > 0)
            {
                TargetNodes.AddRange(targetNodes);
            }
        }

        public void Update(GameTime gameTime)
        {
            if (TargetNodes.Count < 2) return;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            PreviousPosition = Position;

            // Get next target node index
            int nextIndex = _currentNodeIndex + _direction;

            // Handle direction reversal at ends
            if (nextIndex >= TargetNodes.Count)
            {
                _direction = -1;
                nextIndex = _currentNodeIndex + _direction;
            }
            else if (nextIndex < 0)
            {
                _direction = 1;
                nextIndex = _currentNodeIndex + _direction;
            }

            Vector2 target = TargetNodes[nextIndex];
            Vector2 toTarget = target - Position;
            float distance = toTarget.Length();

            if (distance > 0)
            {
                float moveAmount = Speed * dt;

                if (moveAmount >= distance)
                {
                    // Reached the node
                    Position = target;
                    _currentNodeIndex = nextIndex;
                }
                else
                {
                    // Move towards target
                    Vector2 dir = Vector2.Normalize(toTarget);
                    Position += dir * moveAmount;
                }
            }
            else
            {
                _currentNodeIndex = nextIndex;
            }

            Delta = Position - PreviousPosition;
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D tilemap)
        {
            // Use tile source size for drawing, not entity size
            Rectangle dest = new Rectangle(
                (int)MathF.Round(Position.X),
                (int)MathF.Round(Position.Y),
                TileSource.Width,
                TileSource.Height
            );

            spriteBatch.Draw(tilemap, dest, TileSource, Color.White);
        }
    }
}