using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace GalactaJumperMo.Classes
{
    public class TileInstance
    {
        public Rectangle Source;
        public Rectangle Destination;
        public bool IsSolid;

        public TileInstance(Rectangle source, Rectangle destination, bool isSolid)
        {
            Source = source;
            Destination = destination;
            IsSolid = isSolid;
        }
    }

    public class Stage
    {
        public List<Rectangle> Platforms = new List<Rectangle>();
        public List<Rectangle> StaticPlatforms = new List<Rectangle>();
        public List<Rectangle> HazardRects = new List<Rectangle>();

        public List<TileInstance> SolidTiles = new List<TileInstance>();
        public List<TileInstance> DecorationTiles = new List<TileInstance>();

        public List<Vector2> EnemySpawns = new List<Vector2>();
        public List<Vector2> LizardSpawns = new List<Vector2>();
        public List<Vector2> BatSpawns = new List<Vector2>();
        public List<Vector2> StarSpawns = new List<Vector2>();

        public List<MovingPlatform> MovingPlatforms = new List<MovingPlatform>();

        public Vector2 PlayerSpawn;
        public int VoidY;
        public int TileSize = 16;
        public int StageWidthPixels;

        private int _tilesPerRow;
        private int _mapWidth = 0;
        private int _mapHeight = 0;

        public Stage(int tilesPerRow)
        {
            _tilesPerRow = tilesPerRow;

            string baseDir = AppContext.BaseDirectory;

            string[] candidates = new[]
            {
                Path.Combine(baseDir, "com-game-final.tmx"),
                Path.Combine(baseDir, "Content", "Maps", "com-game-final.tmx"),
                Path.Combine(baseDir, "Maps", "com-game-final.tmx")
            };

            string tmxPath = candidates.FirstOrDefault(File.Exists);

            if (tmxPath == null)
                throw new FileNotFoundException("Could not find com-game-final.tmx.");

            LoadFromTmx(tmxPath);
            AddMapBounds();
            SyncPlatforms();
            SetDefaultPlayerSpawn();
        }

        public void SyncPlatforms()
        {
            Platforms.Clear();
            Platforms.AddRange(StaticPlatforms);

            foreach (MovingPlatform mp in MovingPlatforms)
                Platforms.Add(mp.Bounds);
        }

        private void LoadFromTmx(string tmxPath)
        {
            XDocument doc = XDocument.Load(tmxPath);
            XElement map = doc.Element("map");

            if (map == null)
                throw new Exception("Invalid TMX: missing <map> root.");

            _mapWidth = ParseInt(map.Attribute("width")?.Value, 0);
            _mapHeight = ParseInt(map.Attribute("height")?.Value, 0);
            TileSize = ParseInt(map.Attribute("tilewidth")?.Value, 16);

            StageWidthPixels = _mapWidth * TileSize;
            VoidY = _mapHeight * TileSize;

            foreach (XElement layer in map.Elements("layer"))
            {
                string name = (layer.Attribute("name")?.Value ?? "").Trim().ToLowerInvariant();
                XElement dataElement = layer.Element("data");
                if (dataElement == null) continue;

                string encoding = dataElement.Attribute("encoding")?.Value ?? "";
                if (encoding != "csv")
                    throw new Exception("TMX layer data must use CSV encoding.");

                int[] gids = ParseCsv(dataElement.Value);

                if (name == "collisions")
                {
                    LoadCollisionLayer(gids);
                }
                else if (name == "hazards")
                {
                    LoadHazardLayer(gids);
                }
                else if (name == "fg")
                {
                    LoadVisualLayer(gids, SolidTiles, false);
                }
            }

            foreach (XElement objectGroup in map.Elements("objectgroup"))
            {
                string name = (objectGroup.Attribute("name")?.Value ?? "").Trim().ToLowerInvariant();

                if (name == "moving_platforms")
                    LoadMovingPlatforms(objectGroup);
            }
        }

        private void LoadCollisionLayer(int[] gids)
        {
            for (int y = 0; y < _mapHeight; y++)
            {
                for (int x = 0; x < _mapWidth; x++)
                {
                    int gid = gids[y * _mapWidth + x];
                    if (gid == 0) continue;

                    Rectangle dest = new Rectangle(
                        x * TileSize,
                        y * TileSize,
                        TileSize,
                        TileSize
                    );

                    StaticPlatforms.Add(dest);
                }
            }
        }

        private void LoadHazardLayer(int[] gids)
        {
            for (int y = 0; y < _mapHeight; y++)
            {
                for (int x = 0; x < _mapWidth; x++)
                {
                    int gid = gids[y * _mapWidth + x];
                    if (gid == 0) continue;

                    Rectangle dest = new Rectangle(
                        x * TileSize,
                        y * TileSize,
                        TileSize,
                        TileSize
                    );

                    HazardRects.Add(dest);
                }
            }
        }

        private void LoadVisualLayer(int[] gids, List<TileInstance> target, bool isSolid)
        {
            for (int y = 0; y < _mapHeight; y++)
            {
                for (int x = 0; x < _mapWidth; x++)
                {
                    int gid = gids[y * _mapWidth + x];
                    if (gid == 0) continue;

                    Rectangle src = GidToSourceRect(gid);
                    Rectangle dest = new Rectangle(
                        x * TileSize,
                        y * TileSize,
                        TileSize,
                        TileSize
                    );

                    target.Add(new TileInstance(src, dest, isSolid));
                }
            }
        }

        private void LoadMovingPlatforms(XElement objectGroup)
        {
            foreach (XElement obj in objectGroup.Elements("object"))
            {
                float x = ParseFloat(obj.Attribute("x")?.Value, 0f);
                float y = ParseFloat(obj.Attribute("y")?.Value, 0f);
                int width = (int)MathF.Round(ParseFloat(obj.Attribute("width")?.Value, 48f));
                int height = (int)MathF.Round(ParseFloat(obj.Attribute("height")?.Value, 16f));

                string axis = "horizontal";
                float range = 96f;
                float speed = 60f;

                XElement props = obj.Element("properties");
                if (props != null)
                {
                    foreach (XElement p in props.Elements("property"))
                    {
                        string propName = p.Attribute("name")?.Value ?? "";
                        string propValue = p.Attribute("value")?.Value ?? "";

                        if (propName == "axis")
                            axis = propValue;
                        else if (propName == "range")
                            range = ParseFloat(propValue, 96f);
                        else if (propName == "speed")
                            speed = ParseFloat(propValue, 60f);
                    }
                }

                MovingPlatforms.Add(
                    new MovingPlatform(
                        new Vector2(x, y),
                        width,
                        height,
                        axis,
                        range,
                        speed
                    )
                );
            }
        }

        private void AddMapBounds()
        {
            int wallHeight = (_mapHeight * TileSize) + 2000;

            StaticPlatforms.Add(new Rectangle(-32, 0, 32, wallHeight));
            StaticPlatforms.Add(new Rectangle(StageWidthPixels, 0, 32, wallHeight));
        }

        private void SetDefaultPlayerSpawn()
        {
            PlayerSpawn = new Vector2(48, 384);

            int spawnX = 3 * TileSize;
            Rectangle? bestGround = null;

            foreach (Rectangle rect in StaticPlatforms)
            {
                bool overlapsSpawnX =
                    spawnX >= rect.Left - 2 &&
                    spawnX <= rect.Right + 2;

                if (!overlapsSpawnX)
                    continue;

                if (bestGround == null || rect.Top < bestGround.Value.Top)
                    bestGround = rect;
            }

            if (bestGround.HasValue)
            {
                PlayerSpawn = new Vector2(
                    spawnX,
                    bestGround.Value.Top - 32
                );
            }
        }

        private Rectangle GidToSourceRect(int gid)
        {
            int cleanGid = gid & 0x1FFFFFFF;
            int local = cleanGid - 1;

            if (local < 0) local = 0;

            int sx = (local % _tilesPerRow) * TileSize;
            int sy = (local / _tilesPerRow) * TileSize;

            return new Rectangle(sx, sy, TileSize, TileSize);
        }

        private static int[] ParseCsv(string csv)
        {
            return csv
                .Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.Parse(s.Trim(), CultureInfo.InvariantCulture))
                .ToArray();
        }

        private static int ParseInt(string text, int fallback)
        {
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
                ? value
                : fallback;
        }

        private static float ParseFloat(string text, float fallback)
        {
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value)
                ? value
                : fallback;
        }
    }
}