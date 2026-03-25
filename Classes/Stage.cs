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

        public List<Vector2> EnemySpawns = new List<Vector2>();
        public List<Vector2> LizardSpawns = new List<Vector2>();
        public List<Vector2> BatSpawns = new List<Vector2>();

        public Vector2 PlayerSpawn;
        public int VoidY;
        public int TileSize = 16;
        public int StageWidthPixels;

        private readonly Random rng = new Random(42);

        // difficulty tuning
        private const int MAX_STEP_UP_TILES = 4;
        private const int NORMAL_GAP_MIN = 3;
        private const int NORMAL_GAP_MAX = 5;

        // island bridge tuning
        private const int ISLAND_GAP_TOTAL_MIN = 8;
        private const int ISLAND_GAP_TOTAL_MAX = 9;
        private const int MAX_JUMP_TO_ISLAND_TILES = 4;
        private const int MAX_ISLAND_HEIGHT_ABOVE_GROUND = 5;

        private readonly List<Rectangle> purpleTiles = new List<Rectangle>();
        private readonly List<Rectangle> blueChunkTiles = new List<Rectangle>();

        private readonly List<Rectangle> smallTopDecorTiles = new List<Rectangle>();
        private readonly List<Rectangle> rootedDecorTiles = new List<Rectangle>();
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
            // regular ground
            for (int row = 0; row <= 3; row++)
            {
                for (int col = 7; col <= 11; col++)
                {
                    purpleTiles.Add(TileRect(row, col));
                }
            }
            purpleTiles.Add(TileRect(4, 10));
            purpleTiles.Add(TileRect(4, 11));

            // island-style floating chunks
            blueChunkTiles.Add(TileRect(4, 7));
            blueChunkTiles.Add(TileRect(4, 8));
            blueChunkTiles.Add(TileRect(4, 9));

            blueChunkTiles.Add(TileRect(11, 7));
            blueChunkTiles.Add(TileRect(11, 8));
            blueChunkTiles.Add(TileRect(11, 9));
            blueChunkTiles.Add(TileRect(12, 7));
            blueChunkTiles.Add(TileRect(12, 8));
            blueChunkTiles.Add(TileRect(12, 9));
            blueChunkTiles.Add(TileRect(13, 7));
            blueChunkTiles.Add(TileRect(13, 8));
            blueChunkTiles.Add(TileRect(13, 9));

            // top decorations
            smallTopDecorTiles.Add(TileRect(0, 15));
            smallTopDecorTiles.Add(TileRect(0, 16));
            smallTopDecorTiles.Add(TileRect(0, 17));
            smallTopDecorTiles.Add(TileRect(1, 15));
            smallTopDecorTiles.Add(TileRect(1, 16));
            smallTopDecorTiles.Add(TileRect(1, 17));
            smallTopDecorTiles.Add(TileRect(2, 15));

            rootedDecorTiles.Add(TileRect(0, 13));
            rootedDecorTiles.Add(TileRect(0, 14));
            rootedDecorTiles.Add(TileRect(1, 13));
            rootedDecorTiles.Add(TileRect(1, 14));
            rootedDecorTiles.Add(TileRect(2, 13));
            rootedDecorTiles.Add(TileRect(2, 14));

            // support pillars
            pillarTiles.Add(TileRect(0, 12));
            pillarTiles.Add(TileRect(1, 12));
            pillarTiles.Add(TileRect(2, 12));
            pillarTiles.Add(TileRect(3, 12));
        }

        private Rectangle RandomFrom(List<Rectangle> pool)
        {
            return pool[rng.Next(pool.Count)];
        }

        private Rectangle RandomGroundTile()
        {
            return RandomFrom(purpleTiles);
        }

        private Rectangle RandomIslandTile()
        {
            return RandomFrom(blueChunkTiles);
        }

        private void GenerateStage()
        {
            int tile = TileSize;
            StageWidthPixels = 260 * tile;
            VoidY = 560;

            int currentGroundY = 416;
            AddInvisibleWall(-32, 0, 32, 2000);

            Rectangle startTile = RandomGroundTile();
            AddBlockArea(0, currentGroundY, 10, 2, startTile);
            AddDecorationsOnTop(0, currentGroundY, 10);
            PlayerSpawn = new Vector2(48, currentGroundY - 32);

            int x = 10 * tile;

            while (x < StageWidthPixels - 30 * tile)
            {
                int widthTiles = rng.Next(4, 8);

                int nextGroundY = currentGroundY + rng.Next(-2, 3) * tile;
                nextGroundY = Clamp(nextGroundY, 352, 432);

                int riseTiles = (currentGroundY - nextGroundY) / tile;
                if (riseTiles > MAX_STEP_UP_TILES)
                    nextGroundY = currentGroundY - MAX_STEP_UP_TILES * tile;

                Rectangle sectionTile = RandomGroundTile();

                int shapeRoll = rng.Next(100);
                if (shapeRoll < 45)
                    BuildFlatSection(x, nextGroundY, widthTiles, sectionTile);
                else if (shapeRoll < 70)
                    BuildStepUpSection(x, nextGroundY, widthTiles, sectionTile);
                else if (shapeRoll < 88)
                    BuildStepDownSection(x, nextGroundY, widthTiles, sectionTile);
                else
                    BuildRaisedMiddleSection(x, nextGroundY, widthTiles, sectionTile);
                
                TryAddEnemyOnPlatform(x, nextGroundY, widthTiles);
                AddUpperMiniPlatforms(x, nextGroundY, widthTiles);

                bool useIslandBridge = rng.NextDouble() < 0.65;

                if (useIslandBridge)
                {
                    int totalGapTiles = rng.Next(ISLAND_GAP_TOTAL_MIN, ISLAND_GAP_TOTAL_MAX + 1);
                    int islandWidthTiles = rng.Next(3, 5); // 3-4 blocks wide

                    int leftJump = rng.Next(3, MAX_JUMP_TO_ISLAND_TILES + 1);
                    int rightJump = totalGapTiles - leftJump - islandWidthTiles;

                    if (rightJump < 2)
                    {
                        rightJump = 2;
                        leftJump = totalGapTiles - islandWidthTiles - rightJump;
                    }

                    if (leftJump < 2)
                    {
                        leftJump = 2;
                        rightJump = totalGapTiles - islandWidthTiles - leftJump;
                    }

                    int islandY = nextGroundY - rng.Next(1, MAX_ISLAND_HEIGHT_ABOVE_GROUND + 1) * tile;
                    int islandX = x + widthTiles * tile + leftJump * tile;

                    // standalone island
                    Rectangle islandTile = RandomIslandTile();
                    int islandHeightTiles = rng.Next(1, 3); // 1 or 2 tall
                    AddBlockArea(islandX, islandY, islandWidthTiles, islandHeightTiles, islandTile);
                    AddDecorationsOnTop(islandX, islandY, islandWidthTiles);

                    // some islands get a pillar into the void
                    if (rng.NextDouble() < 0.35)
                    {
                        int pillarStartY = islandY + islandHeightTiles * tile;
                        int pillarHeightTiles = Math.Max(2, (VoidY - pillarStartY) / tile);
                        int pillarX = islandX + (islandWidthTiles / 2) * tile;
                        AddSolidPillar(pillarX, pillarStartY, pillarHeightTiles, RandomFrom(pillarTiles));
                    }

                    x += widthTiles * tile;
                    x += totalGapTiles * tile;
                }
                else
                {
                    int gapTiles = rng.Next(NORMAL_GAP_MIN, NORMAL_GAP_MAX + 1);
                    x += widthTiles * tile;
                    x += gapTiles * tile;
                }

                currentGroundY = nextGroundY;
            }

            Rectangle endTile = RandomGroundTile();
            AddBlockArea(x, currentGroundY, 14, 2, endTile);
            AddDecorationsOnTop(x, currentGroundY, 14);
        }

        private void BuildFlatSection(int startX, int groundY, int widthTiles, Rectangle tileSrc)
        {
            AddBlockArea(startX, groundY, widthTiles, 2, tileSrc);
            AddDecorationsOnTop(startX, groundY, widthTiles);

            if (rng.NextDouble() < 0.20)
            {
                int pillarX = startX + rng.Next(1, Math.Max(2, widthTiles - 1)) * TileSize;
                int pillarStartY = groundY + 2 * TileSize;
                int pillarHeightTiles = Math.Max(2, (VoidY - pillarStartY) / TileSize);
                AddSolidPillar(pillarX, pillarStartY, pillarHeightTiles, RandomFrom(pillarTiles));
            }
        }

        private void BuildStepUpSection(int startX, int groundY, int widthTiles, Rectangle tileSrc)
        {
            int leftWidth = Math.Max(3, widthTiles / 2);
            int rightWidth = Math.Max(2, widthTiles - leftWidth);
            int highY = groundY - 2 * TileSize;

            AddBlockArea(startX, groundY, leftWidth, 2, tileSrc);
            AddBlockArea(startX + leftWidth * TileSize, highY, rightWidth, 4, tileSrc);

            AddDecorationsOnTop(startX, groundY, leftWidth);
            AddDecorationsOnTop(startX + leftWidth * TileSize, highY, rightWidth);

            if (rng.NextDouble() < 0.30)
            {
                int pillarX = startX + leftWidth * TileSize + (rightWidth / 2) * TileSize;
                int pillarStartY = highY + 4 * TileSize;
                int pillarHeightTiles = Math.Max(2, (VoidY - pillarStartY) / TileSize);
                AddSolidPillar(pillarX, pillarStartY, pillarHeightTiles, RandomFrom(pillarTiles));
            }
        }

        private void BuildStepDownSection(int startX, int groundY, int widthTiles, Rectangle tileSrc)
        {
            int leftWidth = Math.Max(2, widthTiles / 2);
            int rightWidth = Math.Max(3, widthTiles - leftWidth);
            int highY = groundY - 2 * TileSize;

            AddBlockArea(startX, highY, leftWidth, 4, tileSrc);
            AddBlockArea(startX + leftWidth * TileSize, groundY, rightWidth, 2, tileSrc);

            AddDecorationsOnTop(startX, highY, leftWidth);
            AddDecorationsOnTop(startX + leftWidth * TileSize, groundY, rightWidth);

            if (rng.NextDouble() < 0.30)
            {
                int pillarX = startX + (leftWidth / 2) * TileSize;
                int pillarStartY = highY + 4 * TileSize;
                int pillarHeightTiles = Math.Max(2, (VoidY - pillarStartY) / TileSize);
                AddSolidPillar(pillarX, pillarStartY, pillarHeightTiles, RandomFrom(pillarTiles));
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

            if (rng.NextDouble() < 0.35)
            {
                int pillarX = startX + left * TileSize + (middle / 2) * TileSize;
                int pillarStartY = middleY + 4 * TileSize;
                int pillarHeightTiles = Math.Max(2, (VoidY - pillarStartY) / TileSize);
                AddSolidPillar(pillarX, pillarStartY, pillarHeightTiles, RandomFrom(pillarTiles));
            }
        }

        private void AddUpperMiniPlatforms(int sectionX, int groundY, int sectionWidthTiles)
        {
            if (sectionWidthTiles < 4) return;

            if (rng.NextDouble() > 0.25) return;

            int miniCount = rng.Next(1, 3); // 1 or 2 mini platforms

            for (int i = 0; i < miniCount; i++)
            {
                int widthTiles = rng.Next(2, 5); // 2-4 blocks wide

                if (sectionWidthTiles <= widthTiles + 1)
                    continue;

                int px = sectionX + rng.Next(1, sectionWidthTiles - widthTiles) * TileSize;

                // reachable height
                int py = groundY - rng.Next(4, 6) * TileSize;

                Rectangle tileSrc = RandomIslandTile();
                AddBlockArea(px, py, widthTiles, 1, tileSrc);
                AddDecorationsOnTop(px, py, widthTiles);
            }
        }        

        private void TryAddEnemyOnPlatform(int startX, int groundY, int widthTiles)
        {
            if (widthTiles < 5) return;

            if (rng.NextDouble() > 0.35) return;

            // keep enemy away from edges
            int minOffset = 2;
            int maxOffset = widthTiles - 3;
            if (maxOffset < minOffset) return;

            int tileOffset = rng.Next(minOffset, maxOffset + 1);

            float enemyX = startX + tileOffset * TileSize;
            float enemyY = groundY - 30; // fits the enemy landing logic, and hitbox

            double roll = rng.NextDouble();
            if (roll < 0.30) 
            {
                BatSpawns.Add(new Vector2(enemyX, enemyY - 50));
            }
            else if (roll < 0.65) 
            {
                LizardSpawns.Add(new Vector2(enemyX, enemyY));
            }
            else 
            {
                EnemySpawns.Add(new Vector2(enemyX, enemyY));
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

        private void AddSolidPillar(int x, int startY, int heightTiles, Rectangle pillarSrc)
        {
            for (int i = 0; i < heightTiles; i++)
            {
                Rectangle dest = new Rectangle(
                    x,
                    startY + i * TileSize,
                    TileSize,
                    TileSize
                );

                SolidTiles.Add(new TileInstance(pillarSrc, dest, true));
                Platforms.Add(dest);
            }
        }

        private void AddDecorationsOnTop(int startX, int groundY, int widthTiles)
        {
            if (widthTiles < 2) return;

            int decorCount = Math.Max(1, widthTiles / 5);

            for (int i = 0; i < decorCount; i++)
            {
                int x = startX + rng.Next(0, widthTiles) * TileSize;

                Rectangle src = rng.NextDouble() < 0.75
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