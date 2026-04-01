using Microsoft.Xna.Framework;

namespace GalactaJumperMo.Classes
{
    public class EnemySpawnData
    {
        public Vector2 Position { get; set; }
        public MonsterType Type { get; set; }

        public EnemySpawnData(Vector2 position, MonsterType type)
        {
            Position = position;
            Type = type;
        }
    }
}
