using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using GalactaJumperMo.Classes;

namespace GalactaJumperMo;

public class Game1 : Game
{
    private const float WorldZoom = 2.25f;

    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    // ── Game state ────────────────────────────────────────────────────────────
    private enum GameState { MainMenu, Playing, Paused, GameOver, Tutorial, Settings }
    private GameState _gameState      = GameState.MainMenu;
    private GameState _preSettingsState = GameState.MainMenu;

    // ── Screens ───────────────────────────────────────────────────────────────
    private MainMenuScreen _mainMenu;
    private TutorialScreen _tutorial;
    private SettingsScreen _settings;
    private PauseScreen    _pause;
    private GameOverScreen _gameOver;

    private SpriteFont _titleFont;
    private SpriteFont _menuFont;

    // ── In-game ───────────────────────────────────────────────────────────────
    private Stage      stage;
    private Player     player;
    private List<Enemy> enemies;

    private Texture2D  pixel;
    private Texture2D  tilemap;
    private Texture2D  playerTexture;
    private Texture2D  enemyTexture;
    private SpriteFont font;

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
        _graphics.HardwareModeSwitch       = true;
        _graphics.IsFullScreen             = true;
        _graphics.PreferredBackBufferWidth  = d.Width;
        _graphics.PreferredBackBufferHeight = d.Height;
    }

    protected override void Initialize()
    {
        cameraTransform = Matrix.Identity;
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        pixel = new Texture2D(GraphicsDevice, 1, 1);
        pixel.SetData(new[] { Color.White });

        font          = Content.Load<SpriteFont>("Fonts/GameFont");
        tilemap       = Content.Load<Texture2D>("Stage/monochrome_tilemap_transparent_packed");
        playerTexture = Content.Load<Texture2D>("Player/mo_sprites");
        enemyTexture  = Content.Load<Texture2D>("Enemies/ghost_sprites");
        _titleFont    = Content.Load<SpriteFont>("Fonts/TitleFont");
        _menuFont     = Content.Load<SpriteFont>("Fonts/MenuFont");

        _mainMenu = new MainMenuScreen(_titleFont, _menuFont, pixel, hasSaveData: false);
        _tutorial = new TutorialScreen(_titleFont, _menuFont, pixel);
        _settings = new SettingsScreen(_titleFont, _menuFont, pixel);
        _pause    = new PauseScreen(_titleFont, _menuFont, pixel);
    }

    // ── Public pause API ──────────────────────────────────────────────────────
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
        _gameOver  = new GameOverScreen(_titleFont, _menuFont, pixel, reason);
        _gameState = GameState.GameOver;
    }

    private void OpenSettings()
    {
        _preSettingsState = _gameState;
        _settings = new SettingsScreen(_titleFont, _menuFont, pixel);
        _gameState = GameState.Settings;
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
                    case PauseAction.Resume:   Resume(); break;
                    case PauseAction.Settings: OpenSettings(); break;
                    case PauseAction.MainMenu: GoToMainMenu(); break;
                    case PauseAction.Exit:     Exit(); break;
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

        player.Update(gameTime, stage);

        foreach (Enemy enemy in enemies)
        {
            enemy.Update(gameTime, stage);
            if (player.Bounds.Intersects(enemy.Bounds))
            {
                TriggerGameOver("You were defeated.");
                return;
            }
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
        float maxCameraX    = Math.Max(0, stage.StageWidthPixels - viewportWidth);
        float cameraX       = Math.Clamp(player.Position.X - 250f, 0, maxCameraX);
        cameraTransform     = Matrix.CreateTranslation(-cameraX, 0, 0)
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

        foreach (Enemy enemy in enemies)
            enemy.Draw(_spriteBatch, enemyTexture);

        Vector2 drawPos = new Vector2(player.Bounds.Center.X, player.Bounds.Center.Y);
        Vector2 origin  = new Vector2(16, 16);

        if (player.getDashingState)
        {
            float angle = (float)Math.Atan2(player.getDashDirection.Y, player.getDashDirection.X);
            _spriteBatch.Draw(playerTexture, drawPos, player.SourceRect,
                Color.White, angle, origin, 1f, SpriteEffects.None, 0f);
        }
        else
        {
            SpriteEffects flip = player.FacingLeft ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            _spriteBatch.Draw(playerTexture, drawPos, player.SourceRect,
                Color.White, 0f, origin, 1f, flip, 0f);
        }

        _spriteBatch.End();

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        Color timerColor = timeLeft < 10 ? Color.Red : Color.White;
        _spriteBatch.DrawString(font, $"Time: {(int)timeLeft}", new Vector2(20, 20), timerColor);
        _spriteBatch.End();
    }

    private void BuildStageAndActors()
    {
        stage  = new Stage();
        player = new Player { Position = stage.PlayerSpawn };

        timeLeft        = 120f;
        cameraTransform = Matrix.Identity;

        enemies = new List<Enemy>();
        foreach (Vector2 spawn in stage.EnemySpawns)
            enemies.Add(new Enemy(spawn));
    }
}
