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

        // Safe but not too easy
        private const int MAX_STEP_UP_TILES = 4;
        private const int MAX_FLOAT_HEIGHT_TILES = 5;
        private const int MIN_GAP_TILES = 2;
        private const int MAX_GAP_TILES = 5;

        // Solid themes
        private readonly List<Rectangle> purpleTiles = new List<Rectangle>();
        private readonly List<Rectangle> blueTiles = new List<Rectangle>();
        private readonly List<Rectangle> metalTiles = new List<Rectangle>();
        private readonly List<Rectangle> panelTiles = new List<Rectangle>();

        // Decoration tiles
        private readonly List<Rectangle> smallTopDecorTiles = new List<Rectangle>();
        private readonly List<Rectangle> rootedDecorTiles = new List<Rectangle>();
        private readonly List<Rectangle> undersideDecorTiles = new List<Rectangle>();
        private readonly List<Rectangle> pillarTiles = new List<Rectangle>();

        public Stage()
        {
            BuildTilePools();
            GenerateStage();
        }

        private Rectangle TileRect(int row, int col)
        {
            return new Rectangle(col * TileSize, row * TileSize, TileSize, TileSize);
        }

        private void BuildTilePools()
        {
            // Purple pool
            for (int row = 0; row <= 3; row++)
            {
                for (int col = 7; col <= 11; col++)
                {
                    purpleTiles.Add(TileRect(row, col));
                }
            }
            purpleTiles.Add(TileRect(4, 10));
            purpleTiles.Add(TileRect(4, 11));

            // Blue pool
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

            // Other solid-looking blocks
            metalTiles.Add(TileRect(17, 10));
            metalTiles.Add(TileRect(18, 10));
            metalTiles.Add(TileRect(19, 10));

            panelTiles.Add(TileRect(17, 13));
            panelTiles.Add(TileRect(18, 13));
            panelTiles.Add(TileRect(19, 13));

            // Top decorations
            smallTopDecorTiles.Add(TileRect(0, 15));
            smallTopDecorTiles.Add(TileRect(0, 16));
            smallTopDecorTiles.Add(TileRect(0, 17));
            smallTopDecorTiles.Add(TileRect(1, 15));
            smallTopDecorTiles.Add(TileRect(1, 16));
            smallTopDecorTiles.Add(TileRect(1, 17));
            smallTopDecorTiles.Add(TileRect(2, 15));

            // Rooted decorations
            rootedDecorTiles.Add(TileRect(0, 13));
            rootedDecorTiles.Add(TileRect(0, 14));
            rootedDecorTiles.Add(TileRect(1, 13));
            rootedDecorTiles.Add(TileRect(1, 14));
            rootedDecorTiles.Add(TileRect(2, 13));
            rootedDecorTiles.Add(TileRect(2, 14));

            // Underside / hanging decorations
            undersideDecorTiles.Add(TileRect(0, 18));
            undersideDecorTiles.Add(TileRect(0, 19));
            undersideDecorTiles.Add(TileRect(1, 18));
            undersideDecorTiles.Add(TileRect(1, 19));

            // Thin pillar / cable / chain-like visuals only
            pillarTiles.Add(TileRect(0, 12));
            pillarTiles.Add(TileRect(1, 12));
            pillarTiles.Add(TileRect(2, 12));
            pillarTiles.Add(TileRect(3, 12));
        }

        private Rectangle RandomFrom(List<Rectangle> pool)
        {
            return pool[rng.Next(pool.Count)];
        }

        private int RandomTheme()
        {
            int roll = rng.Next(100);
            if (roll < 40) return 0; // purple
            if (roll < 65) return 1; // blue
            if (roll < 83) return 2; // metal
            return 3;               // panel
        }

        private Rectangle GetThemeTile(int theme)
        {
            switch (theme)
            {
                case 0: return RandomFrom(purpleTiles);
                case 1: return RandomFrom(blueTiles);
                case 2: return RandomFrom(metalTiles);
                default: return RandomFrom(panelTiles);
            }
        }

        private void GenerateStage()
        {
            int tile = TileSize;
            StageWidthPixels = 260 * tile;
            VoidY = 560;

            int currentGroundY = 416;

            AddInvisibleWall(-32, 0, 32, 2000);

            // Start
            Rectangle startTile = GetThemeTile(0);
            AddBlockArea(0, currentGroundY, 12, 2, startTile);
            AddDecorationsOnTop(0, currentGroundY, 12);
            PlayerSpawn = new Vector2(48, currentGroundY - 32);

            int x = 12 * tile;

            while (x < StageWidthPixels - 28 * tile)
            {
                int widthTiles = rng.Next(6, 11);
                int gapTiles = rng.Next(MIN_GAP_TILES, MAX_GAP_TILES + 1);

                int nextGroundY = currentGroundY + rng.Next(-2, 3) * tile;
                nextGroundY = Clamp(nextGroundY, 352, 432);

                int riseTiles = (currentGroundY - nextGroundY) / tile;
                if (riseTiles > MAX_STEP_UP_TILES)
                    nextGroundY = currentGroundY - MAX_STEP_UP_TILES * tile;

                int theme = RandomTheme();
                Rectangle sectionTile = GetThemeTile(theme);

                int shape = rng.Next(5);

                switch (shape)
                {
                    case 0:
                        BuildFlatSection(x, nextGroundY, widthTiles, sectionTile);
                        break;

                    case 1:
                        BuildBlockStepUpSection(x, nextGroundY, widthTiles, sectionTile);
                        break;

                    case 2:
                        BuildBlockStepDownSection(x, nextGroundY, widthTiles, sectionTile);
                        break;

                    case 3:
                        BuildRaisedMiddleSection(x, nextGroundY, widthTiles, sectionTile);
                        break;

                    default:
                        BuildSplitLevelSection(x, nextGroundY, widthTiles, sectionTile);
                        break;
                }

                AddFloatingPlatforms(x, nextGroundY, widthTiles);

                x += widthTiles * tile;
                x += gapTiles * tile;
                currentGroundY = nextGroundY;
            }

            // End
            Rectangle endTile = GetThemeTile(RandomTheme());
            AddBlockArea(x, currentGroundY, 16, 2, endTile);
            AddDecorationsOnTop(x, currentGroundY, 16);

            Rectangle endUpper1 = GetThemeTile(RandomTheme());
            AddBlockArea(x + 8 * tile, currentGroundY - 3 * tile, 4, 1, endUpper1);
            AddDecorationsOnTop(x + 8 * tile, currentGroundY - 3 * tile, 4);

            Rectangle endUpper2 = GetThemeTile(RandomTheme());
            AddBlockArea(x + 14 * tile, currentGroundY - 5 * tile, 3, 1, endUpper2);
            AddDecorationsOnTop(x + 14 * tile, currentGroundY - 5 * tile, 3);
        }

        private void BuildFlatSection(int startX, int groundY, int widthTiles, Rectangle tileSrc)
        {
            AddBlockArea(startX, groundY, widthTiles, 2, tileSrc);
            AddDecorationsOnTop(startX, groundY, widthTiles);

            if (rng.NextDouble() < 0.20)
                AddUndersideDecor(startX, groundY, widthTiles);
        }

        private void BuildBlockStepUpSection(int startX, int groundY, int widthTiles, Rectangle tileSrc)
        {
            int leftWidth = Math.Max(3, widthTiles / 2);
            int rightWidth = Math.Max(2, widthTiles - leftWidth);
            int highY = groundY - 2 * TileSize;

            AddBlockArea(startX, groundY, leftWidth, 2, tileSrc);
            AddBlockArea(startX + leftWidth * TileSize, highY, rightWidth, 4, tileSrc);

            AddDecorationsOnTop(startX, groundY, leftWidth);
            AddDecorationsOnTop(startX + leftWidth * TileSize, highY, rightWidth);

            if (rng.NextDouble() < 0.70)
            {
                int pillarX = startX + leftWidth * TileSize;
                int pillarHeight = Math.Max(2, (groundY - highY) / TileSize + 1);
                AddDecorativePillar(pillarX, highY + TileSize, pillarHeight, RandomFrom(pillarTiles));
            }
        }

        private void BuildBlockStepDownSection(int startX, int groundY, int widthTiles, Rectangle tileSrc)
        {
            int leftWidth = Math.Max(2, widthTiles / 2);
            int rightWidth = Math.Max(3, widthTiles - leftWidth);
            int highY = groundY - 2 * TileSize;

            AddBlockArea(startX, highY, leftWidth, 4, tileSrc);
            AddBlockArea(startX + leftWidth * TileSize, groundY, rightWidth, 2, tileSrc);

            AddDecorationsOnTop(startX, highY, leftWidth);
            AddDecorationsOnTop(startX + leftWidth * TileSize, groundY, rightWidth);

            if (rng.NextDouble() < 0.70)
            {
                int pillarX = startX + leftWidth * TileSize - TileSize;
                int pillarHeight = Math.Max(2, (groundY - highY) / TileSize + 1);
                AddDecorativePillar(pillarX, highY + TileSize, pillarHeight, RandomFrom(pillarTiles));
            }
        }

        private void BuildRaisedMiddleSection(int startX, int groundY, int widthTiles, Rectangle tileSrc)
        {
            int left = Math.Max(2, widthTiles / 3);
            int middle = Math.Max(2, widthTiles / 3);
            int right = Math.Max(2, widthTiles - left - middle);
            int middleY = groundY - 2 * TileSize;

            AddBlockArea(startX, groundY, left, 2, tileSrc);
            AddBlockArea(startX + left * TileSize, middleY, middle, 4, tileSrc);
            AddBlockArea(startX + (left + middle) * TileSize, groundY, right, 2, tileSrc);

            AddDecorationsOnTop(startX, groundY, left);
            AddDecorationsOnTop(startX + left * TileSize, middleY, middle);
            AddDecorationsOnTop(startX + (left + middle) * TileSize, groundY, right);

            if (rng.NextDouble() < 0.80)
            {
                int pillarX1 = startX + left * TileSize;
                int pillarX2 = startX + (left + middle - 1) * TileSize;
                int pillarHeight = Math.Max(2, (groundY - middleY) / TileSize + 1);

                AddDecorativePillar(pillarX1, middleY + TileSize, pillarHeight, RandomFrom(pillarTiles));
                AddDecorativePillar(pillarX2, middleY + TileSize, pillarHeight, RandomFrom(pillarTiles));
            }
        }

        private void BuildSplitLevelSection(int startX, int groundY, int widthTiles, Rectangle tileSrc)
        {
            int first = Math.Max(2, widthTiles / 3);
            int second = Math.Max(2, widthTiles / 3);
            int third = Math.Max(2, widthTiles - first - second);

            int y1 = groundY;
            int y2 = groundY - TileSize;
            int y3 = groundY - 2 * TileSize;

            AddBlockArea(startX, y1, first, 2, tileSrc);
            AddBlockArea(startX + first * TileSize, y2, second, 3, tileSrc);
            AddBlockArea(startX + (first + second) * TileSize, y3, third, 4, tileSrc);

            AddDecorationsOnTop(startX, y1, first);
            AddDecorationsOnTop(startX + first * TileSize, y2, second);
            AddDecorationsOnTop(startX + (first + second) * TileSize, y3, third);

            if (rng.NextDouble() < 0.65)
            {
                AddDecorativePillar(startX + first * TileSize, y2 + TileSize, 2, RandomFrom(pillarTiles));
                AddDecorativePillar(startX + (first + second) * TileSize, y3 + TileSize, 3, RandomFrom(pillarTiles));
            }
        }

        private void AddFloatingPlatforms(int sectionX, int groundY, int sectionWidthTiles)
        {
            int floatingCount = rng.Next(1, 3);

            for (int i = 0; i < floatingCount; i++)
            {
                int widthTiles = rng.Next(2, 5);
                if (sectionWidthTiles <= widthTiles + 1)
                    continue;

                int px = sectionX + rng.Next(1, sectionWidthTiles - widthTiles) * TileSize;
                int py = groundY - rng.Next(3, MAX_FLOAT_HEIGHT_TILES + 1) * TileSize;

                Rectangle tileSrc = GetThemeTile(RandomTheme());
                AddBlockArea(px, py, widthTiles, 1, tileSrc);
                AddDecorationsOnTop(px, py, widthTiles);

                if (rng.NextDouble() < 0.70)
                {
                    int supportCount = widthTiles >= 4 ? 2 : 1;

                    if (supportCount == 1)
                    {
                        int pillarX = px + widthTiles / 2 * TileSize;
                        int pillarHeight = Math.Max(2, (groundY - py) / TileSize - 1);
                        AddDecorativePillar(pillarX, py + TileSize, pillarHeight, RandomFrom(pillarTiles));
                    }
                    else
                    {
                        int pillarHeight = Math.Max(2, (groundY - py) / TileSize - 1);
                        AddDecorativePillar(px + TileSize, py + TileSize, pillarHeight, RandomFrom(pillarTiles));
                        AddDecorativePillar(px + (widthTiles - 2) * TileSize, py + TileSize, pillarHeight, RandomFrom(pillarTiles));
                    }
                }
            }
        }

        private void AddBlockArea(int x, int y, int widthTiles, int heightTiles, Rectangle src)
        {
            for (int row = 0; row < heightTiles; row++)
            {
                for (int col = 0; col < widthTiles; col++)
                {
                    Rectangle dest = new Rectangle(
                        x + col * TileSize,
                        y + row * TileSize,
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

            int decorCount = Math.Max(1, widthTiles / 5);

            for (int i = 0; i < decorCount; i++)
            {
                int x = startX + rng.Next(0, widthTiles) * TileSize;

                Rectangle src = rng.NextDouble() < 0.72
                    ? RandomFrom(smallTopDecorTiles)
                    : RandomFrom(rootedDecorTiles);

                Rectangle dest = new Rectangle(
                    x,
                    groundY - TileSize,
                    TileSize,
                    TileSize
                );

                DecorationTiles.Add(new TileInstance(src, dest, false));
            }
        }

        private void AddUndersideDecor(int startX, int startY, int widthTiles)
        {
            for (int col = 0; col < widthTiles; col++)
            {
                if (rng.NextDouble() < 0.40)
                {
                    Rectangle src = RandomFrom(undersideDecorTiles);

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

        private void AddDecorativePillar(int x, int startY, int heightTiles, Rectangle pillarSrc)
        {
            for (int i = 0; i < heightTiles; i++)
            {
                Rectangle dest = new Rectangle(
                    x,
                    startY + i * TileSize,
                    TileSize,
                    TileSize
                );

                DecorationTiles.Add(new TileInstance(pillarSrc, dest, false));
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