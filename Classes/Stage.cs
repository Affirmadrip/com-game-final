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
        private readonly List<Rectangle> blueTiles = new List<Rectangle>();

        private readonly List<Rectangle> smallTopDecorTiles = new List<Rectangle>();
        private readonly List<Rectangle> rootedDecorTiles = new List<Rectangle>();
        private readonly List<Rectangle> undersideDecorTiles = new List<Rectangle>();

        public Stage()
        {
            BuildTilePools();
            GenerateStage();
        }

        private void BuildTilePools()
        {
            for (int row = 0; row <= 3; row++)
            {
                for (int col = 7; col <= 11; col++)
                {
                    purpleTiles.Add(TileRect(row, col));
                }
            }

            purpleTiles.Add(TileRect(4, 10));
            purpleTiles.Add(TileRect(4, 11));

            blueTiles.Add(TileRect(4, 7));
            blueTiles.Add(TileRect(4, 8));
            blueTiles.Add(TileRect(4, 9));

            blueTiles.Add(TileRect(11, 7));
            blueTiles.Add(TileRect(11, 8));
            blueTiles.Add(TileRect(11, 9));
            blueTiles.Add(TileRect(12, 7));
            blueTiles.Add(TileRect(12, 8));
            blueTiles.Add(TileRect(12, 9));
            blueTiles.Add(TileRect(13, 7));
            blueTiles.Add(TileRect(13, 8));
            blueTiles.Add(TileRect(13, 9));

            smallTopDecorTiles.Add(TileRect(0, 15));
            smallTopDecorTiles.Add(TileRect(0, 16));
            smallTopDecorTiles.Add(TileRect(0, 17));
            smallTopDecorTiles.Add(TileRect(0, 18));
            smallTopDecorTiles.Add(TileRect(0, 19));
            smallTopDecorTiles.Add(TileRect(1, 15));
            smallTopDecorTiles.Add(TileRect(1, 16));
            smallTopDecorTiles.Add(TileRect(1, 17));
            smallTopDecorTiles.Add(TileRect(1, 18));
            smallTopDecorTiles.Add(TileRect(1, 19));
            smallTopDecorTiles.Add(TileRect(2, 15));

            rootedDecorTiles.Add(TileRect(0, 13));
            rootedDecorTiles.Add(TileRect(0, 14));
            rootedDecorTiles.Add(TileRect(1, 13));
            rootedDecorTiles.Add(TileRect(1, 14));
            rootedDecorTiles.Add(TileRect(2, 13));
            rootedDecorTiles.Add(TileRect(2, 14));

            undersideDecorTiles.Add(TileRect(0, 18));
            undersideDecorTiles.Add(TileRect(0, 19));
            undersideDecorTiles.Add(TileRect(1, 18));
            undersideDecorTiles.Add(TileRect(1, 19));
        }

        private Rectangle TileRect(int row, int col)
        {
            return new Rectangle(col * TileSize, row * TileSize, TileSize, TileSize);
        }

        private Rectangle RandomPurple()
        {
            return purpleTiles[rng.Next(purpleTiles.Count)];
        }

        private Rectangle RandomBlue()
        {
            return blueTiles[rng.Next(blueTiles.Count)];
        }

        private Rectangle RandomSmallTopDecor()
        {
            return smallTopDecorTiles[rng.Next(smallTopDecorTiles.Count)];
        }

        private Rectangle RandomRootedDecor()
        {
            return rootedDecorTiles[rng.Next(rootedDecorTiles.Count)];
        }

        private Rectangle RandomUndersideDecor()
        {
            return undersideDecorTiles[rng.Next(undersideDecorTiles.Count)];
        }

        private Rectangle RandomSolidTile()
        {
            return rng.NextDouble() < 0.65 ? RandomPurple() : RandomBlue();
        }

        private void GenerateStage()
        {
            int tile = TileSize;

            int totalColumns = 260;
            StageWidthPixels = totalColumns * tile;

            int groundY = 416;
            VoidY = 560;

            AddInvisibleWall(-32, 0, 32, 2000);

            Rectangle startStyle = RandomPurple();
            AddStyledBlockArea(0, groundY, 14, 2, startStyle);
            AddDecorationsOnTop(0, groundY, 14);
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

                Rectangle sectionStyle = RandomSolidTile();
                AddStyledBlockArea(x, groundY, groundLenTiles, 2, sectionStyle);
                AddDecorationsOnTop(x, groundY, groundLenTiles);

                if (rng.NextDouble() < 0.25)
                {
                    AddUndersideDecor(x, groundY, groundLenTiles);
                }

                int floatingCount = progress < 0.33f ? rng.Next(1, 3) : rng.Next(2, 4);

                for (int i = 0; i < floatingCount; i++)
                {
                    int widthTiles = rng.Next(3, progress < 0.66f ? 7 : 6);
                    int px = x + rng.Next(1, Math.Max(2, groundLenTiles - widthTiles + 1)) * tile;
                    int py = groundY - rng.Next(progress < 0.33f ? 4 : 5, progress < 0.66f ? 8 : 10) * tile;

                    double typeRoll = rng.NextDouble();

                    if (typeRoll < 0.50)
                    {
                        Rectangle platformStyle = RandomSolidTile();
                        AddStyledBlockArea(px, py, widthTiles, 1, platformStyle);
                        AddDecorationsOnTop(px, py, widthTiles);

                        if (rng.NextDouble() < 0.35)
                        {
                            AddUndersideDecor(px, py, widthTiles);
                        }
                    }
                    else if (typeRoll < 0.80)
                    {
                        Rectangle platformStyle = RandomSolidTile();
                        AddStyledBlockArea(px, py, widthTiles, 2, platformStyle);

                        if (rng.NextDouble() < 0.30)
                        {
                            AddUndersideDecor(px, py, widthTiles);
                        }
                    }
                    else
                    {
                        AddBluePrefab3x3(px, py, rng.Next(0, 2) == 0 ? 4 : 11, 7);
                    }
                }

                if (rng.NextDouble() < 0.25)
                {
                    int narrowX = x + rng.Next(2, Math.Max(3, groundLenTiles - 1)) * tile;
                    int narrowY = groundY - rng.Next(4, 7) * tile;

                    Rectangle narrowStyle = RandomSolidTile();
                    AddStyledBlockArea(narrowX, narrowY, 2, 1, narrowStyle);
                    AddDecorationsOnTop(narrowX, narrowY, 2);
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

            Rectangle endStyle = RandomSolidTile();
            AddStyledBlockArea(x, groundY, 18, 2, endStyle);
            AddDecorationsOnTop(x, groundY, 18);

            Rectangle endPlatform1 = RandomSolidTile();
            AddStyledBlockArea(x + 10 * TileSize, groundY - 5 * TileSize, 4, 1, endPlatform1);
            AddDecorationsOnTop(x + 10 * TileSize, groundY - 5 * TileSize, 4);

            Rectangle endPlatform2 = RandomSolidTile();
            AddStyledBlockArea(x + 16 * TileSize, groundY - 8 * TileSize, 3, 1, endPlatform2);
            AddDecorationsOnTop(x + 16 * TileSize, groundY - 8 * TileSize, 3);
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
            if (widthTiles < 2) return;
            if (smallTopDecorTiles.Count == 0 && rootedDecorTiles.Count == 0) return;

            int decorCount = Math.Max(1, widthTiles / 5);

            for (int i = 0; i < decorCount; i++)
            {
                int x = startX + rng.Next(0, widthTiles) * TileSize;

                if (rng.NextDouble() < 0.7)
                {
                    Rectangle src = RandomSmallTopDecor();
                    Rectangle dest = new Rectangle(x, groundY - TileSize, TileSize, TileSize);
                    DecorationTiles.Add(new TileInstance(src, dest, false));
                }
                else
                {
                    Rectangle src = RandomRootedDecor();
                    Rectangle dest = new Rectangle(x, groundY - TileSize, TileSize, TileSize);
                    DecorationTiles.Add(new TileInstance(src, dest, false));
                }
            }
        }

        private void AddUndersideDecor(int startX, int startY, int widthTiles)
        {
            if (undersideDecorTiles.Count == 0) return;

            for (int col = 0; col < widthTiles; col++)
            {
                if (rng.NextDouble() < 0.45)
                {
                    Rectangle src = RandomUndersideDecor();

                    Rectangle dest = new Rectangle(
                        startX + col * TileSize,
                        startY + TileSize,
                        TileSize,
                        TileSize
                    );

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