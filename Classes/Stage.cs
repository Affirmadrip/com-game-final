using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Globalization;
using System.Collections.Generic;
using System.IO;
using System.Linq; 
using System.Text.Json;
using LDtk;

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

    public class LDtkTile
    {
        public Point Px;
        public Point Src;
        public int F;
    }

    public class Stage
    {
        public List<Rectangle> Platforms = new List<Rectangle>();
        public List<Rectangle> StaticPlatforms = new List<Rectangle>();
        public List<Rectangle> WallJumpPlatforms = new List<Rectangle>();
        public List<Rectangle> GirderPlatforms = new List<Rectangle>();
        public List<Rectangle> HazardRects = new List<Rectangle>();

        public List<TileInstance> SolidTiles = new List<TileInstance>();
        public List<TileInstance> DecorationTiles = new List<TileInstance>();

        public List<Vector2> EnemySpawns = new List<Vector2>();
        public List<Vector2> LizardSpawns = new List<Vector2>();
        public List<Vector2> BatSpawns = new List<Vector2>();
        public List<Vector2> StarSpawns = new List<Vector2>();
        public List<(Vector2 position, int checkpoint, Rectangle tileSource)> StarSpawnData = new List<(Vector2, int, Rectangle)>();

        public List<MovingPlatform> MovingPlatforms = new List<MovingPlatform>();
        public List<Spike> Spikes = new List<Spike>();
        
        // New LDTK entity systems
        public List<DeathZone> DeathZones = new List<DeathZone>();
        public List<Objective> Objectives = new List<Objective>();
        public List<EnemySpawnData> EnemySpawnDataList = new List<EnemySpawnData>();
        public List<Spring> Springs = new List<Spring>();
        public List<Conveyor> Conveyors = new List<Conveyor>();
        public List<TitleDisplay> Titles = new List<TitleDisplay>();

        public Vector2 PlayerSpawn;
        public int VoidY;
        public int TileSize = 16;
        public int StageWidthPixels;
        public int StageHeightPixels;

        private LDtkFile _ldtkFile;
        private LDtkWorld _world;
        private LDtkLevel[] _levels;
        private string _ldtkPath;
        private Texture2D _tileset;
        private List<LDtkTile> _allTiles = new List<LDtkTile>();
        private readonly Dictionary<string, Rectangle> _entityDefaultTileSources = new Dictionary<string, Rectangle>(StringComparer.OrdinalIgnoreCase);
        private int _movingPlatformsStartIndex = -1;
        private int _lastMovingPlatformCount = -1;
        private bool _platformLayoutInitialized = false;

        public Color BgColor { get; private set; } = new Color(105, 106, 121); // #696A79

        public Stage(ContentManager content, SpriteBatch spriteBatch, Texture2D tileset)
        {
            _tileset = tileset;

            string baseDir = AppContext.BaseDirectory;
            string currentDir = Directory.GetCurrentDirectory();
            string[] candidates = new[]
            {
                Path.Combine(baseDir, "Content", "Maps", "World_tutorial.ldtk"),
                Path.Combine(baseDir, "Maps", "World_tutorial.ldtk"),
                Path.Combine(currentDir, "Content", "Maps", "World_tutorial.ldtk"),
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "Content", "Maps", "World_tutorial.ldtk")),
            };

            string ldtkPath = candidates.FirstOrDefault(File.Exists);
            if (ldtkPath == null)
                throw new FileNotFoundException($"Could not find World_tutorial.ldtk. Checked: {string.Join(" | ", candidates)}");

            _ldtkPath = ldtkPath;

            _ldtkFile = LDtkFile.FromFile(ldtkPath);
            _world = _ldtkFile.LoadWorld(Guid.Parse("21a8a391-21a0-11f1-bf11-5587492c64e3"));
            _levels = _world.Levels;
            LoadEntityDefaultTileSources();

            LoadFromLDtk();
            AddMapBounds();
            SyncPlatforms();
            SetDefaultPlayerSpawn();
        }

        public void SyncPlatforms()
        {
            bool needsFullRebuild = !_platformLayoutInitialized || _lastMovingPlatformCount != MovingPlatforms.Count;

            if (needsFullRebuild)
            {
                Platforms.Clear();
                Platforms.AddRange(StaticPlatforms);
                Platforms.AddRange(WallJumpPlatforms);
                foreach (Conveyor conveyor in Conveyors)
                    Platforms.Add(conveyor.CollisionBounds);

                _movingPlatformsStartIndex = Platforms.Count;
                foreach (MovingPlatform mp in MovingPlatforms)
                    Platforms.Add(mp.Bounds);

                _lastMovingPlatformCount = MovingPlatforms.Count;
                _platformLayoutInitialized = true;
                return;
            }

            for (int i = 0; i < MovingPlatforms.Count; i++)
                Platforms[_movingPlatformsStartIndex + i] = MovingPlatforms[i].Bounds;
        }

        public void Draw(SpriteBatch spriteBatch, Rectangle? worldCullBounds = null)
        {
            bool useCull = worldCullBounds.HasValue;
            int cullLeft = 0;
            int cullTop = 0;
            int cullRight = 0;
            int cullBottom = 0;

            if (useCull)
            {
                Rectangle cull = worldCullBounds.Value;
                int margin = TileSize * 2;
                cullLeft = cull.Left - margin;
                cullTop = cull.Top - margin;
                cullRight = cull.Right + margin;
                cullBottom = cull.Bottom + margin;
            }

            foreach (var tile in _allTiles)
            {
                if (useCull)
                {
                    int tileLeft = tile.Px.X;
                    int tileTop = tile.Px.Y;
                    int tileRight = tileLeft + TileSize;
                    int tileBottom = tileTop + TileSize;

                    if (tileRight < cullLeft || tileLeft > cullRight || tileBottom < cullTop || tileTop > cullBottom)
                        continue;
                }

                SpriteEffects effect = SpriteEffects.None;
                if ((tile.F & 1) != 0) effect |= SpriteEffects.FlipHorizontally;
                if ((tile.F & 2) != 0) effect |= SpriteEffects.FlipVertically;

                spriteBatch.Draw(
                    _tileset,
                    new Rectangle(tile.Px.X, tile.Px.Y, TileSize, TileSize),
                    new Rectangle(tile.Src.X, tile.Src.Y, TileSize, TileSize),
                    Color.White,
                    0f,
                    Vector2.Zero,
                    effect,
                    0f
                );
            }
        }

        private void LoadFromLDtk()
        {
            int totalWidth = 0;
            int maxHeight = 0;

            foreach (LDtkLevel level in _levels)
            {
                int levelRight = (int)level.WorldX + level.PxWid;
                int levelBottom = (int)level.WorldY + level.PxHei;

                if (levelRight > totalWidth) totalWidth = levelRight;
                if (levelBottom > maxHeight) maxHeight = levelBottom;

                int levelOffsetX = (int)level.WorldX;
                int levelOffsetY = (int)level.WorldY;

                // Load layers in reverse order (bottom to top for proper drawing)
                var layers = level.LayerInstances.Reverse().ToArray();
                
                foreach (var layer in layers)
                {
                    // Load auto-generated tiles from ANY layer that has them
                    if (layer.AutoLayerTiles != null && layer.AutoLayerTiles.Length > 0)
                    {
                        foreach (var tile in layer.AutoLayerTiles)
                        {
                            _allTiles.Add(new LDtkTile
                            {
                                Px = new Point(tile.Px.X + levelOffsetX, tile.Px.Y + levelOffsetY),
                                Src = tile.Src,
                                F = tile.F
                            });
                        }
                    }

                    // Load manual grid tiles
                    if (layer.GridTiles != null && layer.GridTiles.Length > 0)
                    {
                        foreach (var tile in layer.GridTiles)
                        {
                            _allTiles.Add(new LDtkTile
                            {
                                Px = new Point(tile.Px.X + levelOffsetX, tile.Px.Y + levelOffsetY),
                                Src = tile.Src,
                                F = tile.F
                            });
                        }
                    }

                    // Load collision from Collisions layer
                    if (layer._Identifier == "Collisions")
                    {
                        LoadCollisionLayer(layer, level);
                    }

                    // Load Spike entities
                    if (layer._Identifier == "Spikes" && layer.EntityInstances != null)
                    {
                        foreach (var entity in layer.EntityInstances)
                        {
                            if (entity._Identifier == "Spike")
                            {
                                // entity.Px is pivot position, need to calculate top-left
                                // Default pivot is (0.5, 0.5) for Spike
                                Vector2 pos = new Vector2(
                                    entity.Px.X + levelOffsetX - entity.Width / 2,
                                    entity.Px.Y + levelOffsetY - entity.Height / 2
                                );

                                // Use default spike tile (48, 144) from tileset
                                Rectangle tileSource = new Rectangle(48, 144, 16, 16);
                                if (entity._Tile != null)
                                {
                                    tileSource = new Rectangle(
                                        entity._Tile.X,
                                        entity._Tile.Y,
                                        entity._Tile.W,
                                        entity._Tile.H
                                    );
                                }

                                Spikes.Add(new Spike(pos, entity.Width, entity.Height, tileSource));
                            }
                        }
                    }

                    // Load MovingPlatform entities with TargetNodes
                    if (layer._Identifier == "MovingPlatform" && layer.EntityInstances != null)
                    {
                        foreach (var entity in layer.EntityInstances)
                        {
                            if (entity._Identifier == "MovingPlatform")
                            {
                                // Get tile source first to know actual size
                                Rectangle tileSource = new Rectangle(64, 48, 48, 16);
                                if (entity._Tile != null)
                                {
                                    tileSource = new Rectangle(
                                        entity._Tile.X,
                                        entity._Tile.Y,
                                        entity._Tile.W,
                                        entity._Tile.H
                                    );
                                }

                                // Use tile size for pivot offset (not entity size)
                                Vector2 startPos = new Vector2(
                                    entity.Px.X + levelOffsetX - tileSource.Width / 2,
                                    entity.Px.Y + levelOffsetY - tileSource.Height / 2
                                );

                                List<Vector2> targetNodes = new List<Vector2>();

                                // Parse TargetNodes field
                                foreach (var field in entity.FieldInstances)
                                {
                                    if (field._Identifier == "TargetNodes" && field._Value is System.Text.Json.JsonElement pointsArray)
                                    {
                                        if (pointsArray.ValueKind == System.Text.Json.JsonValueKind.Array)
                                        {
                                            foreach (var point in pointsArray.EnumerateArray())
                                            {
                                                if (point.ValueKind == System.Text.Json.JsonValueKind.Object)
                                                {
                                                    int cx = point.GetProperty("cx").GetInt32();
                                                    int cy = point.GetProperty("cy").GetInt32();
                                                    // Use tile size for node position offset
                                                    Vector2 nodePos = new Vector2(
                                                        levelOffsetX + cx * TileSize + TileSize / 2 - tileSource.Width / 2,
                                                        levelOffsetY + cy * TileSize + TileSize / 2 - tileSource.Height / 2
                                                    );
                                                    targetNodes.Add(nodePos);
                                                }
                                            }
                                        }
                                    }
                                }

                                MovingPlatforms.Add(new MovingPlatform(
                                    startPos,
                                    tileSource.Width,
                                    tileSource.Height,
                                    60f,
                                    targetNodes,
                                    tileSource
                                ));
                           }
                       }
                   }

                   // Load DeathZone entities with ZoneTitle
                   if (layer._Identifier == "DeathZone" && layer.EntityInstances != null)
                   {
                       foreach (var entity in layer.EntityInstances)
                       {
                           if (entity._Identifier == "DeathZone")
                           {
                               // DeathZone uses pivot (0, 0) and is resizable
                               Rectangle bounds = new Rectangle(
                                   entity.Px.X + levelOffsetX,
                                   entity.Px.Y + levelOffsetY,
                                   entity.Width,
                                   entity.Height
                               );

                               string zoneTitle = "You died";
                               foreach (var field in entity.FieldInstances)
                               {
                                   if (field._Identifier == "ZoneTitle" && field._Value is System.Text.Json.JsonElement jsonValue && jsonValue.ValueKind != System.Text.Json.JsonValueKind.Null)
                                   {
                                       zoneTitle = jsonValue.ToString().Trim('"');
                                   }
                               }

                               DeathZones.Add(new DeathZone(bounds, zoneTitle));
                           }
                       }
                   }

                   // Load skill/objective entities from any entity layer.
                   if (layer.EntityInstances != null)
                   {
                       foreach (var entity in layer.EntityInstances)
                       {
                           if (entity._Identifier == "Skills" || entity._Identifier == "Skill" || entity._Identifier == "Objective")
                           {
                               // Skills uses pivot (0, 0)
                               Vector2 position = new Vector2(
                                   entity.Px.X + levelOffsetX + entity.Width / 2,
                                   entity.Px.Y + levelOffsetY + entity.Height / 2
                               );

                               string skill = "";
                               int checkpoint = 0;
                               foreach (var field in entity.FieldInstances)
                               {
                                   if (field._Identifier == "Skill" && field._Value is System.Text.Json.JsonElement skillValue && skillValue.ValueKind != System.Text.Json.JsonValueKind.Null)
                                   {
                                       skill = skillValue.ToString().Trim('"');
                                   }
                                   if (field._Identifier == "Checkpoint" && field._Value is System.Text.Json.JsonElement checkpointValue && checkpointValue.ValueKind != System.Text.Json.JsonValueKind.Null)
                                   {
                                       checkpoint = ParseCheckpointValue(checkpointValue);
                                   }
                               }

                               // Get tile source for rendering
                               Rectangle tileSource = new Rectangle(32, 80, 16, 16); // Default
                               if (entity._Tile != null)
                               {
                                   tileSource = new Rectangle(
                                       entity._Tile.X,
                                       entity._Tile.Y,
                                       entity._Tile.W,
                                       entity._Tile.H
                                   );
                               }

                               Objectives.Add(new Objective(position, skill, checkpoint, tileSource));
                           }
                       }
                   }

                   // Load Enemy entities with MonsterType
                   if (layer._Identifier == "Enemy" && layer.EntityInstances != null)
                   {
                       foreach (var entity in layer.EntityInstances)
                       {
                           if (entity._Identifier == "Enemy")
                           {
                               // Enemy uses pivot (0, 0)
                               Vector2 position = new Vector2(
                                   entity.Px.X + levelOffsetX,
                                   entity.Px.Y + levelOffsetY
                               );

                               MonsterType monsterType = MonsterType.Ghost; // Default
                               foreach (var field in entity.FieldInstances)
                               {
                                   if (field._Identifier == "MonsterType" && field._Value is System.Text.Json.JsonElement jsonValue && jsonValue.ValueKind != System.Text.Json.JsonValueKind.Null)
                                   {
                                       string typeStr = jsonValue.ToString().Trim('"');
                                       if (Enum.TryParse<MonsterType>(typeStr, true, out MonsterType parsed))
                                       {
                                           monsterType = parsed;
                                       }
                                   }
                               }

                               EnemySpawnDataList.Add(new EnemySpawnData(position, monsterType));
                           }
                       }
                   }

                   // Load Spring entities
                   if (layer._Identifier == "Spring" && layer.EntityInstances != null)
                   {
                       foreach (var entity in layer.EntityInstances)
                       {
                           if (entity._Identifier == "Spring")
                           {
                               // Springs in this project use top-left pivot (0,0).
                               Vector2 position = new Vector2(
                                   entity.Px.X + levelOffsetX,
                                   entity.Px.Y + levelOffsetY
                               );

                               Rectangle tileSource = new Rectangle(80, 144, 16, 16); // Default spring tile
                               if (entity._Tile != null)
                               {
                                   tileSource = new Rectangle(
                                       entity._Tile.X,
                                       entity._Tile.Y,
                                       entity._Tile.W,
                                       entity._Tile.H
                                   );
                               }

                               Springs.Add(new Spring(position, tileSource));
                           }
                       }
                   }

                   // Load Conveyor entities from any entity layer.
                   if (layer.EntityInstances != null)
                   {
                       foreach (var entity in layer.EntityInstances)
                       {
                           if (entity._Identifier == "Conveyor")
                           {
                               // Conveyor uses pivot (0, 0)
                               Vector2 position = new Vector2(
                                   entity.Px.X + levelOffsetX,
                                   entity.Px.Y + levelOffsetY
                               );

                               ConveyorDirection direction = ConveyorDirection.Right; // Default
                               foreach (var field in entity.FieldInstances)
                               {
                                   if (field._Identifier == "Direction" && field._Value is System.Text.Json.JsonElement jsonValue && jsonValue.ValueKind != System.Text.Json.JsonValueKind.Null)
                                   {
                                       string dirStr = jsonValue.ToString().Trim('"');
                                       if (Enum.TryParse<ConveyorDirection>(dirStr, true, out ConveyorDirection parsed))
                                       {
                                           direction = parsed;
                                       }
                                   }
                               }

                               Rectangle tileSource = Rectangle.Empty;
                               if (entity._Tile != null)
                               {
                                   tileSource = new Rectangle(
                                       entity._Tile.X,
                                       entity._Tile.Y,
                                       entity._Tile.W,
                                       entity._Tile.H
                                   );
                               }
                               else if (!TryGetEntityDefaultTileSource("Conveyor", out tileSource))
                               {
                                   tileSource = new Rectangle(0, 0, Math.Max(1, entity.Width), Math.Max(1, entity.Height));
                               }

                               int conveyorWidth = Math.Max(entity.Width, tileSource.Width);
                               int conveyorHeight = Math.Max(entity.Height, tileSource.Height);
                               Conveyors.Add(new Conveyor(position, conveyorWidth, conveyorHeight, direction, tileSource));
                           }
                       }
                   }

                   // Load Titles entities
                   if (layer._Identifier == "Titles" && layer.EntityInstances != null)
                   {
                       foreach (var entity in layer.EntityInstances)
                       {
                           if (entity._Identifier == "Title" || entity._Identifier == "Titles")
                           {
                               // Title uses pivot (0.5, 0.5) - center
                               Vector2 position = new Vector2(
                                   entity.Px.X + levelOffsetX,
                                   entity.Px.Y + levelOffsetY
                               );

                               string title = "";
                               foreach (var field in entity.FieldInstances)
                               {
                                   if (field._Identifier == "Title" && field._Value is System.Text.Json.JsonElement jsonValue && jsonValue.ValueKind != System.Text.Json.JsonValueKind.Null)
                                   {
                                       title = jsonValue.ToString().Trim('"');
                                   }
                               }

                               Titles.Add(new TitleDisplay(position, title));
                           }
                       }
                   }

                   // Load Star entities with checkpoint support from any entity layer.
                   if (layer.EntityInstances != null)
                   {
                       foreach (var entity in layer.EntityInstances)
                       {
                           if (entity._Identifier == "Star")
                           {
                               // Star uses top-left position in this project.
                               Vector2 position = new Vector2(
                                   entity.Px.X + levelOffsetX,
                                   entity.Px.Y + levelOffsetY
                               );

                               int checkpoint = 0;
                               bool hasExplicitCheckpoint = false;
                               foreach (var field in entity.FieldInstances)
                               {
                                   if (field._Identifier == "Checkpoint" && field._Value is System.Text.Json.JsonElement checkpointValue && checkpointValue.ValueKind != System.Text.Json.JsonValueKind.Null)
                                   {
                                       checkpoint = ParseCheckpointValue(checkpointValue);
                                       hasExplicitCheckpoint = true;
                                   }
                               }

                               // If no checkpoint field is defined, use star order as checkpoint id.
                               if (!hasExplicitCheckpoint)
                               {
                                   checkpoint = StarSpawnData.Count + 1;
                               }

                               Rectangle tileSource = new Rectangle(32, 0, 16, 16);
                               if (entity._Tile != null)
                               {
                                   tileSource = new Rectangle(
                                       entity._Tile.X,
                                       entity._Tile.Y,
                                       entity._Tile.W,
                                       entity._Tile.H
                                   );
                               }

                               StarSpawnData.Add((position, checkpoint, tileSource));
                               StarSpawns.Add(position); // Keep for backward compatibility
                           }
                       }
                   }
               }
           }

           StageWidthPixels = totalWidth;
            StageHeightPixels = maxHeight;
            VoidY = maxHeight + 200;
        }

        private void LoadCollisionLayer(LayerInstance layer, LDtkLevel level)
        {
            int gridSize = layer._GridSize;
            int levelOffsetX = (int)level.WorldX;
            int levelOffsetY = (int)level.WorldY;

            if (layer.IntGridCsv != null)
            {
                int width = layer._CWid;

                for (int i = 0; i < layer.IntGridCsv.Length; i++)
                {
                    int value = layer.IntGridCsv[i];
                    if (value == 0) continue;

                    int x = i % width;
                    int y = i / width;

                    Rectangle dest = new Rectangle(
                        levelOffsetX + (x * gridSize),
                        levelOffsetY + (y * gridSize),
                        gridSize,
                        gridSize
                    );

                    // value 1 = walls (normal collision)
                    // value 2 = wall_jump (wall jump enabled)
                    // value 3 = girder (one-way platform - can jump through from below)
                    // value 4 = brick (solid collision like walls)
                    // value 7 = machineblock (solid collision like walls)
                    if (value == 1 || value == 4 || value == 5 || value == 7)
                    {
                        StaticPlatforms.Add(dest);
                    }
                    else if (value == 2)
                    {
                        WallJumpPlatforms.Add(dest);
                    }
                    else if (value == 3)
                    {
                        GirderPlatforms.Add(dest);
                    }
                }
            }
        }

        private static int ParseCheckpointValue(System.Text.Json.JsonElement checkpointValue)
        {
            if (checkpointValue.ValueKind == System.Text.Json.JsonValueKind.Number &&
                checkpointValue.TryGetInt32(out int numericValue))
            {
                return numericValue;
            }

            if (checkpointValue.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                string raw = checkpointValue.GetString();
                if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedValue))
                {
                    return parsedValue;
                }
            }

            return 0;
        }

        private void LoadEntityDefaultTileSources()
        {
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(_ldtkPath));
                if (!doc.RootElement.TryGetProperty("defs", out JsonElement defs) ||
                    !defs.TryGetProperty("entities", out JsonElement entities) ||
                    entities.ValueKind != JsonValueKind.Array)
                {
                    return;
                }

                foreach (JsonElement entityDef in entities.EnumerateArray())
                {
                    if (!entityDef.TryGetProperty("identifier", out JsonElement identifierEl))
                        continue;

                    string identifier = identifierEl.GetString();
                    if (string.IsNullOrWhiteSpace(identifier))
                        continue;

                    if (!entityDef.TryGetProperty("tileRect", out JsonElement tileRectEl) ||
                        tileRectEl.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    if (TryParseTileRect(tileRectEl, out Rectangle tileRect))
                    {
                        _entityDefaultTileSources[identifier] = tileRect;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load entity tile defaults: {ex.Message}");
            }
        }

        private bool TryGetEntityDefaultTileSource(string entityIdentifier, out Rectangle tileSource)
        {
            return _entityDefaultTileSources.TryGetValue(entityIdentifier, out tileSource);
        }

        private static bool TryParseTileRect(JsonElement tileRectEl, out Rectangle tileRect)
        {
            tileRect = Rectangle.Empty;

            if (!tileRectEl.TryGetProperty("x", out JsonElement xEl) ||
                !tileRectEl.TryGetProperty("y", out JsonElement yEl) ||
                !tileRectEl.TryGetProperty("w", out JsonElement wEl) ||
                !tileRectEl.TryGetProperty("h", out JsonElement hEl))
            {
                return false;
            }

            if (!xEl.TryGetInt32(out int x) ||
                !yEl.TryGetInt32(out int y) ||
                !wEl.TryGetInt32(out int w) ||
                !hEl.TryGetInt32(out int h))
            {
                return false;
            }

            if (w <= 0 || h <= 0)
                return false;

            tileRect = new Rectangle(x, y, w, h);
            return true;
        }

        private void AddMapBounds()
        {
            int wallHeight = StageHeightPixels + 2000;

            StaticPlatforms.Add(new Rectangle(-32, 0, 32, wallHeight));
            StaticPlatforms.Add(new Rectangle(StageWidthPixels, 0, 32, wallHeight));
        }

        private void SetDefaultPlayerSpawn()
        {
            // Default fallback position
            PlayerSpawn = new Vector2(100, 300);

            // Try to find PlayerStart entity from LDtk
            foreach (LDtkLevel level in _levels)
            {
                int levelOffsetX = (int)level.WorldX;
                int levelOffsetY = (int)level.WorldY;

                foreach (var layer in level.LayerInstances)
                {
                    if (layer._Identifier == "PlayerStart" && layer.EntityInstances != null)
                    {
                        foreach (var entity in layer.EntityInstances)
                        {
                            if (entity._Identifier == "PlayerStart")
                            {
                                // Use exact world position from LDtk PlayerStart.
                                PlayerSpawn = new Vector2(
                                    entity.Px.X + levelOffsetX,
                                    entity.Px.Y + levelOffsetY
                                );
                                System.Diagnostics.Debug.WriteLine($"Found PlayerStart at: {PlayerSpawn}");
                                return;
                            }
                        }
                    }
                }
            }

            // Fallback to TOC world coordinates when PlayerStart entity isn't in loaded layer instances.
            if (TrySetPlayerSpawnFromToc())
                return;

            System.Diagnostics.Debug.WriteLine($"PlayerStart not found, using default: {PlayerSpawn}");
        }

        private bool TrySetPlayerSpawnFromToc()
        {
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(_ldtkPath));
                if (!doc.RootElement.TryGetProperty("toc", out JsonElement toc) || toc.ValueKind != JsonValueKind.Array)
                    return false;

                foreach (JsonElement entry in toc.EnumerateArray())
                {
                    if (!entry.TryGetProperty("identifier", out JsonElement idEl) || idEl.GetString() != "PlayerStart")
                        continue;

                    if (!entry.TryGetProperty("instancesData", out JsonElement instances) || instances.ValueKind != JsonValueKind.Array)
                        continue;

                    foreach (JsonElement inst in instances.EnumerateArray())
                    {
                        if (!inst.TryGetProperty("worldX", out JsonElement xEl) || !inst.TryGetProperty("worldY", out JsonElement yEl))
                            continue;

                        float worldX = (float)xEl.GetDouble();
                        float worldY = (float)yEl.GetDouble();
                        PlayerSpawn = new Vector2(worldX, worldY);
                        System.Diagnostics.Debug.WriteLine($"Found PlayerStart from TOC at: {PlayerSpawn}");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to read PlayerStart from TOC: {ex.Message}");
            }

            return false;
        }
    }
}