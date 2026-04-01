using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GalactaJumperMo.Classes
{
    public enum ConveyorDirection
    {
        Left,
        Right
    }

    /// Represents a conveyor belt that pushes the player in a specified direction.
    /// Parsed from the Conveyor entity layer in LDTK with Direction field.
    public class Conveyor
    {
        public Vector2 Position { get; private set; }
        public Rectangle Bounds { get; private set; }
        public Rectangle CollisionBounds { get; private set; }
        public Rectangle TileSource { get; private set; }
        public ConveyorDirection Direction { get; private set; }
        
        private const float ConveyorSpeed = 80f; // Pixels per second push force
        private const int HorizontalCollisionPadding = 0; // Keep collision exactly on the conveyor size.
        
        public Conveyor(Vector2 position, int width, int height, ConveyorDirection direction, Rectangle tileSource = default)
        {
            Position = position;
            Direction = direction;
            TileSource = tileSource.IsEmpty ? new Rectangle(0, 0, Math.Max(1, width), Math.Max(1, height)) : tileSource;
            
            Bounds = new Rectangle(
                (int)position.X,
                (int)position.Y,
                width,
                height
            );

            int collisionWidth = Math.Max(1, Bounds.Width + HorizontalCollisionPadding * 2);
            CollisionBounds = new Rectangle(
                Bounds.X - HorizontalCollisionPadding,
                Bounds.Y,
                collisionWidth,
                Bounds.Height
            );
        }
        
        /// Check if player is standing on the conveyor
        public bool IsPlayerOnConveyor(Rectangle playerBounds)
        {
            // Match ground collision width to avoid 1px states where player is grounded but not pushed.
            Rectangle playerFeet = new Rectangle(
                playerBounds.X,
                playerBounds.Bottom - 2,
                playerBounds.Width,
                4
            );
            
            Rectangle conveyorTop = new Rectangle(
                CollisionBounds.X,
                CollisionBounds.Y - 2,
                CollisionBounds.Width,
                4
            );
            
            return playerFeet.Intersects(conveyorTop);
        }
        
        /// Get the push velocity to apply to the player
        public Vector2 GetPushVelocity(float deltaTime)
        {
            float pushX = Direction == ConveyorDirection.Right ? ConveyorSpeed : -ConveyorSpeed;
            return new Vector2(pushX * deltaTime, 0f);
        }
        
        /// Draw the conveyor
        public void Draw(SpriteBatch spriteBatch, Texture2D tileset)
        {
            if (tileset != null)
            {
                // Draw the conveyor using the tile source
                // For now, just draw a simple representation
                spriteBatch.Draw(
                    tileset,
                    Bounds,
                    TileSource,
                    Color.White,
                    0f,
                    Vector2.Zero,
                    SpriteEffects.None,
                    0f
                );
            }
        }
    }
}