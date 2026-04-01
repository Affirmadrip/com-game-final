using Microsoft.Xna.Framework;

namespace GalactaJumperMo.Classes
{
    public class DeathZone
    {
        public Rectangle Bounds { get; private set; }
        public string ZoneTitle { get; private set; }

        public DeathZone(Rectangle bounds, string zoneTitle)
        {
            Bounds = bounds;
            ZoneTitle = zoneTitle ?? "You died";
        }

        /// Check if the player intersects with this death zone
        public bool CheckCollision(Rectangle playerBounds)
        {
            return Bounds.Intersects(playerBounds);
        }
    }
}
