using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using GalactaJumperMo.Classes;
using Microsoft.Xna.Framework.Audio;

namespace GalactaJumperMo;

public class Game1 : Game
{
    private const float WorldZoom = 2.25f;

    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    // ── Game state ────────────────────────────────────────────────────────────
    private enum GameState { MainMenu, Playing, Paused, GameOver, Tutorial, Settings }
    private GameState _gameState = GameState.MainMenu;
    private GameState _preSettingsState = GameState.MainMenu;

    // ── Screens ───────────────────────────────────────────────────────────────
    private MainMenuScreen _mainMenu;
    private TutorialScreen _tutorial;
    private SettingsScreen _settings;
    private PauseScreen _pause;
    private GameOverScreen _gameOver;

    private SpriteFont _titleFont;
    private SpriteFont _menuFont;

    // ── In-game ───────────────────────────────────────────────────────────────
    private Stage stage;
    private Player player;
    private List<Enemy> enemies;
    private List<EnemyLizard> lizards;
    private List<EnemyBat> bats;

    // ตัวแปรสำหรับดาวและ UI
    private Texture2D starTexture;
    private List<Star> stars;
    private int totalEnemiesForDrop;
    private int remainingStarsToDrop;
    private Random dropRng = new Random();
    private int collectedStarsCount = 0; // ตัวนับจำนวนดาวที่เก็บได้

    private Texture2D pixel;
    private Texture2D tilemap;
    private Texture2D playerTexture;
    private Texture2D ghostTexture;
    private Texture2D lizardWalkTex;
    private Texture2D lizardTongueTex;
    private Texture2D batIdleTex;
    private Texture2D batAtkTex;
    private SpriteFont font;

    private int maxHealth = 3;
    private int currentHealth = 3;
    private Texture2D heartFull;
    private Texture2D heartEmpty;

    // SFX
    SoundEffect sfxHurt;
    SoundEffect sfxJump;
    SoundEffect sfxDash;
    SoundEffect sfxStar; // เพิ่มตัวแปรเสียงเก็บดาว

    private float shakeTimer = 0f;
    private const float shakeDuration = 0.35f;
    private const float shakeStrength = 5f;
    private Vector2 shakeOffset = Vector2.Zero;
    private Random shakeRng = new Random();

    private float timeLeft = 120f;
    private Matrix cameraTransform;
    private KeyboardState previousKeyboard;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        SetFullscreenMode();
        _graphics.ApplyChanges();
    }

    private void SetFullscreenMode()
    {
        DisplayMode d = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
        _graphics.HardwareModeSwitch = false;
        _graphics.IsFullScreen = true;
        _graphics.PreferredBackBufferWidth = d.Width;
        _graphics.PreferredBackBufferHeight = d.Height;
        _graphics.ApplyChanges();
    }

    protected override void Initialize()
    {
        cameraTransform = Matrix.Identity;
        base.Initialize();
    }

    protected override void LoadContent()
    {
        heartFull = Content.Load<Texture2D>("UI/heart_full");
        heartEmpty = Content.Load<Texture2D>("UI/heart_empty");

        _spriteBatch = new SpriteBatch(GraphicsDevice);

        pixel = new Texture2D(GraphicsDevice, 1, 1);
        pixel.SetData(new[] { Color.White });

        font = Content.Load<SpriteFont>("Fonts/GameFont");
        tilemap = Content.Load<Texture2D>("Stage/monochrome_tilemap_transparent_packed");
        playerTexture = Content.Load<Texture2D>("Player/mo_sprites");

        // โหลดเสียง
        sfxHurt = Content.Load<SoundEffect>("Audio/hurt");
        sfxJump = Content.Load<SoundEffect>("Audio/jump");
        sfxDash = Content.Load<SoundEffect>("Audio/dash");
        sfxStar = Content.Load<SoundEffect>("Audio/starcorrect"); // โหลดเสียงเก็บดาว

        // โหลดรูปดาว
        starTexture = Content.Load<Texture2D>("Star/starcoin");

        ghostTexture = Content.Load<Texture2D>("Enemies/ghost/ghost_sprites");
        lizardWalkTex = Content.Load<Texture2D>("Enemies/lizard/lizard_walk");
        lizardTongueTex = Content.Load<Texture2D>("Enemies/lizard/lizard_tongue");
        batIdleTex = Content.Load<Texture2D>("Enemies/bat/bat_idle");
        batAtkTex = Content.Load<Texture2D>("Enemies/bat/bat_atk");
        _titleFont = Content.Load<SpriteFont>("Fonts/TitleFont");
        _menuFont = Content.Load<SpriteFont>("Fonts/MenuFont");

        _mainMenu = new MainMenuScreen(_titleFont, _menuFont, pixel, hasSaveData: false);
        _tutorial = new TutorialScreen(_titleFont, _menuFont, pixel);
        _settings = new SettingsScreen(_titleFont, _menuFont, pixel);
        _pause = new PauseScreen(_titleFont, _menuFont, pixel);
    }

    public void Pause()
    {
        if (_gameState != GameState.Playing) return;
        _pause.OnOpen();
        _gameState = GameState.Paused;
    }

    private void Resume()
    {
        _gameState = GameState.Playing;
    }

    private void GoToMainMenu()
    {
        _mainMenu = new MainMenuScreen(_titleFont, _menuFont, pixel, hasSaveData: false);
        _gameState = GameState.MainMenu;
    }

    private void TriggerGameOver(string reason)
    {
        _gameOver = new GameOverScreen(_titleFont, _menuFont, pixel, reason);
        _gameState = GameState.GameOver;
    }

    private void OpenSettings()
    {
        _preSettingsState = _gameState;
        _settings = new SettingsScreen(_titleFont, _menuFont, pixel);
        _gameState = GameState.Settings;
    }

    // เมธอดจัดการการดรอปดาวเมื่อศัตรูตาย
    private void HandleEnemyDeath(Vector2 deathPosition)
    {
        if (totalEnemiesForDrop <= 0) return;

        double dropChance = (double)remainingStarsToDrop / totalEnemiesForDrop;

        if (dropRng.NextDouble() < dropChance)
        {
            stars.Add(new Star(starTexture, deathPosition));
            remainingStarsToDrop--;
        }

        totalEnemiesForDrop--;
    }

    protected override void Update(GameTime gameTime)
    {
        int sw = GraphicsDevice.Viewport.Width;
        int sh = GraphicsDevice.Viewport.Height;
        var keyboard = Keyboard.GetState();

        switch (_gameState)
        {
            case GameState.MainMenu:
                _mainMenu.Update(gameTime, sw, sh);
                switch (_mainMenu.PendingAction)
                {
                    case MenuAction.NewGame:
                        BuildStageAndActors();
                        _gameState = GameState.Playing;
                        break;
                    case MenuAction.Tutorial:
                        _tutorial = new TutorialScreen(_titleFont, _menuFont, pixel);
                        _gameState = GameState.Tutorial;
                        break;
                    case MenuAction.Settings:
                        OpenSettings();
                        break;
                    case MenuAction.Exit:
                        Exit();
                        break;
                }
                break;

            case GameState.Tutorial:
                _tutorial.Update(gameTime, sw, sh);
                if (_tutorial.PendingAction == TutorialAction.Back)
                    GoToMainMenu();
                break;

            case GameState.Settings:
                _settings.Update(gameTime, sw, sh);
                if (_settings.PendingAction == SettingsAction.Back)
                {
                    if (_preSettingsState == GameState.MainMenu)
                    {
                        GoToMainMenu();
                    }
                    else
                    {
                        if (_preSettingsState == GameState.Paused)
                            _pause.OnOpen();
                        _gameState = _preSettingsState;
                    }
                }
                break;

            case GameState.Playing:
                UpdatePlaying(gameTime, keyboard, sw);
                break;

            case GameState.Paused:
                _pause.Update(gameTime, sw, sh);
                switch (_pause.PendingAction)
                {
                    case PauseAction.Resume: Resume(); break;
                    case PauseAction.Settings: OpenSettings(); break;
                    case PauseAction.MainMenu: GoToMainMenu(); break;
                    case PauseAction.Exit: Exit(); break;
                }
                break;

            case GameState.GameOver:
                _gameOver.Update(gameTime, sw, sh);
                switch (_gameOver.PendingAction)
                {
                    case GameOverAction.Retry:
                        BuildStageAndActors();
                        _gameState = GameState.Playing;
                        break;
                    case GameOverAction.MainMenu:
                        GoToMainMenu();
                        break;
                    case GameOverAction.Exit:
                        Exit();
                        break;
                }
                break;
        }

        previousKeyboard = keyboard;
        base.Update(gameTime);
    }

    private void UpdatePlaying(GameTime gameTime, KeyboardState keyboard, int sw)
    {
        if (keyboard.IsKeyDown(Keys.Escape) && !previousKeyboard.IsKeyDown(Keys.Escape))
        {
            Pause();
            return;
        }

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        foreach (var mp in stage.MovingPlatforms)
             mp.Update(gameTime);

        stage.SyncPlatforms();

        player.Update(gameTime, stage);

        foreach (var mp in stage.MovingPlatforms)
        {
            Rectangle feet = new Rectangle(
                player.Bounds.X + 4,
                player.Bounds.Bottom - 2,
                player.Bounds.Width - 8,
                4
        );

        if (feet.Intersects(mp.Bounds))
        {
        player.MoveBy(mp.Delta);
        break;
        }
    }

        if (player.JustJumped) sfxJump.Play();
        if (player.JustDashed) sfxDash.Play();

        // เช็คการเก็บดาวและบวกจำนวน
        foreach (var star in stars)
        {
            if (!star.IsCollected && player.Bounds.Intersects(star.Bounds))
            {
                star.IsCollected = true;
                collectedStarsCount++;
                sfxStar.Play(); // เล่นเสียงตอนเก็บดาว
            }
        }

        foreach (Rectangle spike in stage.HazardRects)
        {
            if (player.Bounds.Intersects(spike) && !player.IsInvincible)
            {
                currentHealth--;
                sfxHurt.Play();

                player.TriggerIncinvincible();

                player.ApplyKnockback(
                    new Vector2(player.FacingLeft ? 220f : -220f, -260f)
                );

                if (currentHealth <= 0)
                {
                    TriggerGameOver("You died to spikes");
                    return;
                }

                break;
            }
        }

        //ghost
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = enemies[i];
            enemy.Update(gameTime, stage);

            if (player.Bounds.Intersects(enemy.Bounds))
            {
                if (player.getDashingState)
                {
                    if (!enemy.IsPhaseOut)
                    {
                        HandleEnemyDeath(enemy.Position);
                        enemies.RemoveAt(i);
                        player.OnEnemyContact();
                        continue;
                    }
                }
                else if (!player.IsInvincible)
                {
                    currentHealth--;
                    sfxHurt.Play();
                    player.TriggerIncinvincible();
                    shakeTimer = shakeDuration;

                    if (currentHealth <= 0)
                    {
                        TriggerGameOver("You were defeated.");
                        return;
                    }
                }
            }
        }
        //lizard
        for (int i = lizards.Count - 1; i >= 0; i--)
        {
            var liz = lizards[i];
            liz.Update(gameTime, stage, player.Position);

            if (player.Bounds.Intersects(liz.Bounds) || liz.IsHittingPlayer(player.Bounds))
            {
                if (player.getDashingState)
                {
                    HandleEnemyDeath(liz.Position);
                    lizards.RemoveAt(i);
                    player.OnEnemyContact();
                    continue;
                }
                else if (!player.IsInvincible)
                {
                    currentHealth--;
                    sfxHurt.Play();
                    player.TriggerIncinvincible();
                    shakeTimer = shakeDuration;

                    if (currentHealth <= 0)
                    {
                        TriggerGameOver("Defeated by a lizard!");
                        return;
                    }
                }
            }
        }
        //bat
        for (int i = bats.Count - 1; i >= 0; i--)
        {
            var bat = bats[i];
            bat.Update(gameTime, player.Position);

            if (player.Bounds.Intersects(bat.Bounds))
            {
                if (player.getDashingState)
                {
                    HandleEnemyDeath(bat.Position);
                    bats.RemoveAt(i);
                    player.OnEnemyContact();
                    continue;
                }
                else if (!player.IsInvincible)
                {
                    currentHealth--;
                    sfxHurt.Play();
                    player.TriggerIncinvincible();
                    shakeTimer = shakeDuration;

                    if (currentHealth <= 0)
                    {
                        TriggerGameOver("Defeated by a bat!");
                        return;
                    }
                }
            }
        }

        if (shakeTimer > 0f)
        {
            shakeTimer -= dt;
            float curStr = shakeStrength * (shakeTimer / shakeDuration);
            shakeOffset = new Vector2(
                (float)(shakeRng.NextDouble() * 2 - 1) * curStr,
                (float)(shakeRng.NextDouble() * 2 - 1) * curStr
            );
        }
        else
        {
            shakeOffset = Vector2.Zero;
        }

        timeLeft -= dt;
        if (timeLeft <= 0f)
        {
            timeLeft = 0f;
            TriggerGameOver("Time's up.");
            return;
        }

        if (player.Position.Y > stage.VoidY)
        {
            TriggerGameOver("You fell into the void.");
            return;
        }

        float viewportWidth = GraphicsDevice.Viewport.Width / WorldZoom;
        float maxCameraX = Math.Max(0, stage.StageWidthPixels - viewportWidth);
        float cameraX = Math.Clamp(player.Position.X - 250f, 0, maxCameraX);
        cameraTransform = Matrix.CreateTranslation(-cameraX + shakeOffset.X, shakeOffset.Y, 0)
                * Matrix.CreateScale(WorldZoom, WorldZoom, 1f);
    }

    protected override void Draw(GameTime gameTime)
    {
        int sw = GraphicsDevice.Viewport.Width;
        int sh = GraphicsDevice.Viewport.Height;

        switch (_gameState)
        {
            case GameState.MainMenu:
                GraphicsDevice.Clear(new Color(4, 6, 16));
                _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                _mainMenu.Draw(_spriteBatch, sw, sh);
                _spriteBatch.End();
                break;

            case GameState.Tutorial:
                GraphicsDevice.Clear(new Color(4, 6, 16));
                _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                _tutorial.Draw(_spriteBatch, sw, sh);
                _spriteBatch.End();
                break;

            case GameState.Settings:
                GraphicsDevice.Clear(new Color(4, 6, 16));
                _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                _settings.Draw(_spriteBatch, sw, sh);
                _spriteBatch.End();
                break;

            case GameState.GameOver:
                GraphicsDevice.Clear(new Color(4, 6, 16));
                _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                _gameOver.Draw(_spriteBatch, sw, sh);
                _spriteBatch.End();
                break;

            case GameState.Playing:
            case GameState.Paused:
                DrawGameWorld();
                if (_gameState == GameState.Paused)
                {
                    _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                    _pause.Draw(_spriteBatch, sw, sh);
                    _spriteBatch.End();
                }
                break;
        }

        base.Draw(gameTime);
    }

    private void DrawGameWorld()
    {
        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin(transformMatrix: cameraTransform, samplerState: SamplerState.PointClamp);

        foreach (TileInstance tile in stage.SolidTiles)
            _spriteBatch.Draw(tilemap, tile.Destination, tile.Source, Color.White);
        foreach (TileInstance tile in stage.DecorationTiles)
            _spriteBatch.Draw(tilemap, tile.Destination, tile.Source, Color.White);
        foreach (var mp in stage.MovingPlatforms)
            mp.Draw(_spriteBatch, pixel);
        //ghost
        foreach (Enemy enemy in enemies)
            enemy.Draw(_spriteBatch, ghostTexture);
        //lizard
        foreach (var liz in lizards)
            liz.Draw(_spriteBatch, lizardWalkTex, lizardTongueTex);
        //bat
        foreach (var bat in bats)
            bat.Draw(_spriteBatch, batIdleTex, batAtkTex);

        // วาดดาว
        foreach (var star in stars)
        {
            star.Draw(_spriteBatch);
        }

        if (player.Visible)
        {
            Vector2 drawPos = new Vector2(player.Bounds.Center.X, player.Bounds.Center.Y);
            Vector2 origin = new Vector2(16, 16);

            if (player.getDashingState)
            {
                float angle = (float)Math.Atan2(player.getDashDirection.Y, player.getDashDirection.X);
                _spriteBatch.Draw(playerTexture, drawPos, player.SourceRect, Color.White, angle, origin, 1f, SpriteEffects.None, 0f);
            }
            else
            {
                SpriteEffects flip = player.FacingLeft ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                _spriteBatch.Draw(playerTexture, drawPos, player.SourceRect, Color.White, 0f, origin, 1f, flip, 0f);
            }
        }
        _spriteBatch.End();

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        // วาดเวลา
        Color timerColor = timeLeft < 10 ? Color.Red : Color.White;
        _spriteBatch.DrawString(font, $"Time: {(int)timeLeft}", new Vector2(20, 20), timerColor);

        // วาดหัวใจ
        int heartSize = 80;
        int heartPadding = 6;
        for (int i = 0; i < maxHealth; i++)
        {
            Texture2D heart = i < currentHealth ? heartFull : heartEmpty;
            _spriteBatch.Draw(heart, new Rectangle(20 + i * (heartSize + heartPadding), 55, heartSize, heartSize), Color.White);
        }

        // ------------------------------------------------------------------
        // --- วาด UI จำนวนดาวที่มุมขวาบน แบบบังคับขนาด ---
        // ------------------------------------------------------------------
        int screenWidth = GraphicsDevice.Viewport.Width;
        int uiStarSize = 60; // ขนาดคงที่ 60x60 พิกเซล
        Vector2 starUiPos = new Vector2(screenWidth - 160, 20);

        // บังคับสเกลภาพให้พอดีกับกรอบ Rectangle ที่เราตั้งไว้
        _spriteBatch.Draw(starTexture, new Rectangle((int)starUiPos.X, (int)starUiPos.Y, uiStarSize, uiStarSize), Color.White);

        string starText = $"x {collectedStarsCount}";
        Vector2 textPos = new Vector2(starUiPos.X + uiStarSize + 10, starUiPos.Y + (uiStarSize / 2) - 15);
        _spriteBatch.DrawString(font, starText, textPos, Color.Yellow);
        // ------------------------------------------------------------------

        _spriteBatch.End();
    }

    private void BuildStageAndActors()
    {
        stage = new Stage(tilemap.Width / 16);

        player = new Player();
        player.Position = stage.PlayerSpawn;
        player.ResetVelocity();

        enemies = new List<Enemy>();
        foreach (Vector2 spawn in stage.EnemySpawns)
            enemies.Add(new Enemy(spawn));

        lizards = new List<EnemyLizard>();
        foreach (Vector2 spawn in stage.LizardSpawns)
            lizards.Add(new EnemyLizard(spawn));

        bats = new List<EnemyBat>();
        foreach (Vector2 spawn in stage.BatSpawns)
            bats.Add(new EnemyBat(spawn));

        // สร้างดาวบนแมพ
        stars = new List<Star>();
        foreach (Vector2 spawn in stage.StarSpawns)
            stars.Add(new Star(starTexture, spawn));

        // รีเซ็ตตัวแปรการดรอปและจำนวนดาว
        totalEnemiesForDrop = enemies.Count + lizards.Count + bats.Count;
        remainingStarsToDrop = Math.Min(2, totalEnemiesForDrop);
        collectedStarsCount = 0;

        timeLeft = 120f;
        currentHealth = maxHealth;

        UpdateCameraImmediate();
    }

    private void UpdateCameraImmediate()
    {
        float viewportWidth = GraphicsDevice.Viewport.Width / WorldZoom;
        float maxCameraX = Math.Max(0, stage.StageWidthPixels - viewportWidth);
        float cameraX = Math.Clamp(player.Position.X - 250f, 0, maxCameraX);

        cameraTransform = Matrix.CreateTranslation(-cameraX, 0, 0)
                        * Matrix.CreateScale(WorldZoom, WorldZoom, 1f);
    }
}