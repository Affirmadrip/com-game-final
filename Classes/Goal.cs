using Microsoft.Xna.Framework;

namespace GalactaJumperMo.Classes
{
    public class Goal
    {
        public Rectangle Bounds;

        public Goal(Vector2 position)
        {
            Bounds = new Rectangle((int)position.X, (int)position.Y, 32, 32);
        }

        public bool CheckCollision(Rectangle playerBounds)
        {
            return Bounds.Intersects(playerBounds);
        }
    }
}