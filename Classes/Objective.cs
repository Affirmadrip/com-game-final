using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GalactaJumperMo.Classes
{

    public class Objective
    {
        public Vector2 Position { get; private set; }
        public string Skill { get; private set; }
        public int Checkpoint { get; private set; }
        public bool IsCollected { get; set; }
        public Rectangle Bounds { get; private set; }
        public Rectangle TileSource { get; private set; }

        private const int ObjectiveSize = 16;
        private float animationTimer = 0f;
        private float bobOffset = 0f;

        public Objective(Vector2 position, string skill, int checkpoint = 0, Rectangle tileSource = default)
        {
            Position = position;
            Skill = skill ?? "";
            Checkpoint = checkpoint;
            IsCollected = false;
            TileSource = tileSource.IsEmpty ? new Rectangle(32, 80, 16, 16) : tileSource;
            
            // Create hitbox centered on position
            Bounds = new Rectangle(
                (int)position.X - ObjectiveSize / 2,
                (int)position.Y - ObjectiveSize / 2,
                ObjectiveSize,
                ObjectiveSize
            );
        }

        /// Check if the player has collected this objective
        public bool CheckCollision(Rectangle playerBounds)
        {
            return !IsCollected && Bounds.Intersects(playerBounds);
        }

        public void Update(GameTime gameTime)
        {
            if (!IsCollected)
            {
                animationTimer += (float)gameTime.ElapsedGameTime.TotalSeconds * 3f;
                bobOffset = (float)System.Math.Sin(animationTimer) * 3f;
            }
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D tileset)
        {
            if (!IsCollected && tileset != null)
            {
                Vector2 drawPos = new Vector2(Position.X, Position.Y + bobOffset);
                spriteBatch.Draw(
                    tileset,
                    drawPos,
                    TileSource,
                    Color.White * 0.9f,
                    0f,
                    Vector2.Zero,
                    1f,
                    SpriteEffects.None,
                    0f
                );
            }
        }
    }
}
