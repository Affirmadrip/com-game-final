using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

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
        public List<TileInstance> SolidTiles = new List<TileInstance>();
        public List<TileInstance> DecorationTiles = new List<TileInstance>();

        public Vector2 PlayerSpawn;
        public int VoidY;
        public int TileSize = 16;
        public int StageWidthPixels;

        private readonly Random rng = new Random(42);

        private readonly List<Rectangle> purpleTiles = new List<Rectangle>();
        private readonly List<Rectangle> greenTiles = new List<Rectangle>();

        public Stage()
        {
            BuildTilePools();
            GenerateStage();
        }

        private void BuildTilePools()
        {
            // olid small block
            for (int row = 0; row <= 3; row++)
            {
                for (int col = 7; col <= 11; col++)
                {
                    purpleTiles.Add(TileRect(row, col));
                }
            }

            purpleTiles.Add(TileRect(4, 10));
            purpleTiles.Add(TileRect(4, 11));

            // decoration
            for (int row = 0; row <= 1; row++)
            {
                for (int col = 13; col <= 19; col++)
                {
                    greenTiles.Add(TileRect(row, col));
                }
            }

            for (int col = 13; col <= 15; col++)
            {
                greenTiles.Add(TileRect(2, col));
            }
        }

        private Rectangle TileRect(int row, int col)
        {
            return new Rectangle(col * TileSize, row * TileSize, TileSize, TileSize);
        }

        private void GenerateStage()
        {
            int tile = TileSize;

            int totalColumns = 260;
            StageWidthPixels = totalColumns * tile;

            int groundY = 416;
            VoidY = 560;

            AddInvisibleWall(-32, 0, 32, 2000);

            // start
            Rectangle startStyle = purpleTiles[rng.Next(purpleTiles.Count)];
            AddStyledBlockArea(0, groundY, 14, 2, startStyle);
            PlayerSpawn = new Vector2(48, groundY - 32);

            int x = 14 * tile;

            while (x < StageWidthPixels - 20 * tile)
            {
                float progress = (float)x / StageWidthPixels;

                int minGroundLen = progress < 0.33f ? 10 : progress < 0.66f ? 8 : 6;
                int maxGroundLen = progress < 0.33f ? 16 : progress < 0.66f ? 12 : 10;

                int minGap = progress < 0.33f ? 2 : progress < 0.66f ? 3 : 4;
                int maxGap = progress < 0.33f ? 4 : progress < 0.66f ? 5 : 6;

                int groundShift = rng.Next(-1, 2) * tile;
                groundY = Clamp(groundY + groundShift, 368, 432);

                int groundLenTiles = rng.Next(minGroundLen, maxGroundLen + 1);

                Rectangle sectionStyle = purpleTiles[rng.Next(purpleTiles.Count)];
                AddStyledBlockArea(x, groundY, groundLenTiles, 2, sectionStyle);

                AddDecorationsOnTop(x, groundY, groundLenTiles);

                // floating platforms
                int floatingCount = progress < 0.33f ? rng.Next(1, 3) : rng.Next(2, 4);
                for (int i = 0; i < floatingCount; i++)
                {
                    int widthTiles = rng.Next(3, progress < 0.66f ? 7 : 6);
                    int px = x + rng.Next(1, Math.Max(2, groundLenTiles - widthTiles)) * tile;
                    int py = groundY - rng.Next(progress < 0.33f ? 4 : 5, progress < 0.66f ? 8 : 10) * tile;

                    Rectangle platformStyle = purpleTiles[rng.Next(purpleTiles.Count)];
                    AddStyledBlockArea(px, py, widthTiles, 1, platformStyle);
                }

                if (rng.NextDouble() < 0.25)
                {
                    int narrowX = x + rng.Next(2, Math.Max(3, groundLenTiles - 2)) * tile;
                    int narrowY = groundY - rng.Next(4, 7) * tile;

                    Rectangle narrowStyle = purpleTiles[rng.Next(purpleTiles.Count)];
                    AddStyledBlockArea(narrowX, narrowY, 2, 1, narrowStyle);
                }

                if (rng.NextDouble() < 0.18)
                {
                    int islandX = x + rng.Next(2, Math.Max(3, groundLenTiles - 4)) * tile;
                    int islandY = groundY - rng.Next(6, 9) * tile;
                    AddBluePrefab3x3(islandX, islandY, rng.Next(0, 2) == 0 ? 4 : 11, 7);
                }

                x += groundLenTiles * tile;

                int gapTiles = rng.Next(minGap, maxGap + 1);
                x += gapTiles * tile;
            }

            Rectangle endStyle = purpleTiles[rng.Next(purpleTiles.Count)];
            AddStyledBlockArea(x, groundY, 18, 2, endStyle);

            Rectangle endPlatform1 = purpleTiles[rng.Next(purpleTiles.Count)];
            AddStyledBlockArea(x + 10 * TileSize, groundY - 5 * TileSize, 4, 1, endPlatform1);

            Rectangle endPlatform2 = purpleTiles[rng.Next(purpleTiles.Count)];
            AddStyledBlockArea(x + 16 * TileSize, groundY - 8 * TileSize, 3, 1, endPlatform2);
        }

        private void AddStyledBlockArea(int startX, int startY, int widthTiles, int heightTiles, Rectangle sourceStyle)
        {
            for (int row = 0; row < heightTiles; row++)
            {
                for (int col = 0; col < widthTiles; col++)
                {
                    Rectangle dest = new Rectangle(
                        startX + col * TileSize,
                        startY + row * TileSize,
                        TileSize,
                        TileSize
                    );

                    SolidTiles.Add(new TileInstance(sourceStyle, dest, true));
                    Platforms.Add(dest);
                }
            }
        }

        private void AddBluePrefab3x3(int startX, int startY, int sourceStartRow, int sourceStartCol)
        {
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    Rectangle src = TileRect(sourceStartRow + row, sourceStartCol + col);
                    Rectangle dest = new Rectangle(
                        startX + col * TileSize,
                        startY + row * TileSize,
                        TileSize,
                        TileSize
                    );

                    SolidTiles.Add(new TileInstance(src, dest, true));
                    Platforms.Add(dest);
                }
            }
        }

        private void AddDecorationsOnTop(int startX, int groundY, int widthTiles)
        {
            if (greenTiles.Count == 0) return;

            int decorCount = Math.Max(1, widthTiles / 5);

            for (int i = 0; i < decorCount; i++)
            {
                if (rng.NextDouble() < 0.55)
                {
                    int x = startX + rng.Next(1, Math.Max(2, widthTiles - 1)) * TileSize;
                    int y = groundY - TileSize;

                    Rectangle src = greenTiles[rng.Next(greenTiles.Count)];
                    Rectangle dest = new Rectangle(x, y, TileSize, TileSize);

                    DecorationTiles.Add(new TileInstance(src, dest, false));
                }
            }
        }

        private void AddInvisibleWall(int x, int y, int width, int height)
        {
            Platforms.Add(new Rectangle(x, y, width, height));
        }

        private int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}