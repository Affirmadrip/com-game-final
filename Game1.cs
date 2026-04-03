using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using GalactaJumperMo.Classes;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;

namespace GalactaJumperMo;

public class Game1 : Game
{
    private const float WorldZoom = 3.0f;

    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    // ── Game state ────────────────────────────────────────────────────────────
    private enum GameState { MainMenu, SaveLoad, Playing, Paused, GameOver, Complete, Tutorial, Settings }
    private GameState _gameState = GameState.MainMenu;
    private GameState _preSettingsState = GameState.MainMenu;

    // ── Screens ───────────────────────────────────────────────────────────────
    private MainMenuScreen _mainMenu;
    private SaveLoadScreen _saveLoad;
    private TutorialScreen _tutorial;
    private SettingsScreen _settings;
    private PauseScreen _pause;
    private GameOverScreen _gameOver;
    private CompleteScreen _complete;

    // ── Save system ───────────────────────────────────────────────────────────
    private GameSaveData _currentSave;
    private int _currentCheckpoint = 0;

    private SpriteFont _titleFont;
    private SpriteFont _menuFont;

    private Stage stage;
    private Player player;
    private List<Enemy> enemies;
    private List<EnemyLizard> lizards;
    private List<EnemyBat> bats;

    // variables of star and UI
    private Texture2D starTexture;
    private List<Star> stars;
    private int collectedStarsCount = 0; // collected stars

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
    SoundEffect sfxStar;

    private Song menuBgm;
    private Song stageBgm;
    private Song _currentSong;

    private bool _soundEnabled = true;
    private bool _musicEnabled = true;
    private float _masterVolume = 0.35f;
    private float _sfxVolume = 1f;
    private float shakeTimer = 0f;
    private const float shakeDuration = 0.0f;
    private const float shakeStrength = 0f;
    private Vector2 shakeOffset = Vector2.Zero;
    private Random shakeRng = new Random();

    // Tracks total play time for the current run
    private float elapsedTime = 0f;
    private Matrix cameraTransform;
    private float currentCameraX = 0f;
    private float currentCameraY = 0f;
    private float cameraVelocityX = 0f;
    private float cameraVelocityY = 0f;
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
        tilemap = Content.Load<Texture2D>("Stage/monochrome_tilemap_packed");
        playerTexture = Content.Load<Texture2D>("Player/mo_sprites");

        sfxHurt = Content.Load<SoundEffect>("Audio/hurt");
        sfxJump = Content.Load<SoundEffect>("Audio/jump");
        sfxDash = Content.Load<SoundEffect>("Audio/dash");
        sfxStar = Content.Load<SoundEffect>("Audio/starcorrect"); 

        menuBgm = Content.Load<Song>("Audio/main_menu_bgm");
        stageBgm = Content.Load<Song>("Audio/stage_bgm");

        MediaPlayer.IsRepeating = true;
        PlayMenuBgm();
        ApplyAudioSettings();
        
        starTexture = Content.Load<Texture2D>("Star/starcoin");

        ghostTexture = Content.Load<Texture2D>("Enemies/ghost/ghost_sprites");
        lizardWalkTex = Content.Load<Texture2D>("Enemies/lizard/lizard_walk");
        lizardTongueTex = Content.Load<Texture2D>("Enemies/lizard/lizard_tongue");
        batIdleTex = Content.Load<Texture2D>("Enemies/bat/bat_idle");
        batAtkTex = Content.Load<Texture2D>("Enemies/bat/bat_atk");
        _titleFont = Content.Load<SpriteFont>("Fonts/TitleFont");
        _menuFont = Content.Load<SpriteFont>("Fonts/MenuFont");

        bool hasSaveData = GameSaveData.SaveExists();
        _mainMenu = new MainMenuScreen(_titleFont, _menuFont, pixel, hasSaveData: hasSaveData);
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
        PlayStageBgm();
    }

    private void GoToMainMenu()
    {
        bool hasSaveData = GameSaveData.SaveExists();
        _mainMenu = new MainMenuScreen(_titleFont, _menuFont, pixel, hasSaveData: hasSaveData);
        _gameState = GameState.MainMenu;
        PlayMenuBgm();
    }

    private void TriggerGameOver(string reason)
    {
        _gameOver = new GameOverScreen(_titleFont, _menuFont, pixel, reason);
        _gameState = GameState.GameOver;
    }

    private void TriggerComplete()
    {
        int totalStars = stage?.StarSpawnData.Count ?? 0;
        int collectedStars = _currentSave?.CollectedStarCheckpoints?.Count ?? 0;

        string sideQuestText = $"Collected {collectedStars} / {totalStars} stars";

        TimeSpan played = TimeSpan.FromSeconds(elapsedTime);
        string timeText = played.ToString(@"mm\:ss");

        _complete = new CompleteScreen(_titleFont, _menuFont, pixel, sideQuestText, timeText);
        _gameState = GameState.Complete;
    }
    private void OpenSettings()
    {
        _preSettingsState = _gameState;
        _settings = new SettingsScreen(
            _titleFont,
            _menuFont,
            pixel,
            _soundEnabled,
            _musicEnabled,
            _masterVolume,
            _sfxVolume
        );
        _gameState = GameState.Settings;
    }

    private void PlayMenuBgm()
    {
        if (_currentSong == menuBgm) return;

        _currentSong = menuBgm;

        if (_musicEnabled && menuBgm != null)
        {
            MediaPlayer.Play(menuBgm);
            MediaPlayer.IsRepeating = true;
            MediaPlayer.Volume = MathHelper.Clamp(_masterVolume, 0f, 1f);
        }
        else
        {
            MediaPlayer.Stop();
        }
    }

    private void PlayStageBgm()
    {
        if (_currentSong == stageBgm) return;

        _currentSong = stageBgm;

        if (_musicEnabled && stageBgm != null)
        {
            MediaPlayer.Play(stageBgm);
            MediaPlayer.IsRepeating = true;
            MediaPlayer.Volume = MathHelper.Clamp(_masterVolume, 0f, 1f);
        }
        else
        {
            MediaPlayer.Stop();
        }
    }

    private void ApplyAudioSettings()
    {
        MediaPlayer.Volume = _musicEnabled ? MathHelper.Clamp(_masterVolume, 0f, 1f) : 0f;
    }

    private void PlaySfx(SoundEffect sfx)
    {
        if (sfx == null) return;
        if (!_soundEnabled) return;

        float volume = MathHelper.Clamp(_masterVolume * _sfxVolume * 1.5f, 0f, 1f);
        sfx.Play(volume, 0f, 0f);
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
                    case MenuAction.Continue:
                        _saveLoad = new SaveLoadScreen(_titleFont, _menuFont, pixel);
                        _gameState = GameState.SaveLoad;
                        break;
                    case MenuAction.NewGame:
                        _currentSave = new GameSaveData();
                        BuildStageAndActors(loadFromSave: false);
                        _gameState = GameState.Playing;
                        PlayStageBgm();
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

            case GameState.SaveLoad:
                _saveLoad.Update(gameTime, sw, sh);
                switch (_saveLoad.PendingAction)
                {
                    case SaveLoadAction.Continue:
                        _currentSave = _saveLoad.GetSaveData();
                        BuildStageAndActors(loadFromSave: true);
                        _gameState = GameState.Playing;
                        PlayStageBgm();
                        break;
                    case SaveLoadAction.NewGame:
                        _currentSave = new GameSaveData();
                        BuildStageAndActors(loadFromSave: false);
                        _gameState = GameState.Playing;
                        PlayStageBgm();
                        break;
                    case SaveLoadAction.DeleteSave:
                        GameSaveData.DeleteSave();
                        GoToMainMenu();
                        break;
                    case SaveLoadAction.Back:
                        GoToMainMenu();
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

                _soundEnabled = _settings.SoundEnabled;
                _musicEnabled = _settings.MusicEnabled;
                _masterVolume = _settings.MasterVolume;
                _sfxVolume = _settings.SfxVolume;

                ApplyAudioSettings();

                if (_preSettingsState == GameState.MainMenu)
                    PlayMenuBgm();
                else
                    PlayStageBgm();

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
                        BuildStageAndActors(retryFromCheckpoint: true);
                        _gameState = GameState.Playing;
                        PlayStageBgm();
                        break;
                    case GameOverAction.MainMenu:
                        GoToMainMenu();
                        break;
                    case GameOverAction.Exit:
                        Exit();
                        break;
                }
                break;
            case GameState.Complete:
                _complete.Update(gameTime, sw, sh);
                switch (_complete.PendingAction)
                {
                    case CompleteAction.MainMenu:
                        GoToMainMenu();
                        break;
                    case CompleteAction.Exit:
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
        elapsedTime += dt;

        foreach (var mp in stage.MovingPlatforms)
             mp.Update(gameTime);

        stage.SyncPlatforms();

        player.Update(gameTime, stage);

        // Goal
        foreach (var goal in stage.Goals)
        {
            if (goal.CheckCollision(player.Bounds))
            {
                TriggerComplete();
                return;
            }
        }

        foreach (var mp in stage.MovingPlatforms)
        {
            Rectangle platformFeet = new Rectangle(
                player.Bounds.X + 4,
                player.Bounds.Bottom - 2,
                player.Bounds.Width - 8,
                4
        );

        if (platformFeet.Intersects(mp.Bounds))
        {
        player.MoveBy(mp.Delta);
        break;
        }
    }

        if (player.JustJumped) PlaySfx(sfxJump);
        if (player.JustDashed) PlaySfx(sfxDash);

        foreach (var star in stars)
        {
            star.Update(gameTime);

            if (!star.IsCollected && player.Bounds.Intersects(star.Bounds))
            {
                star.IsCollected = true;
                collectedStarsCount++;
                
                // Update checkpoint and save
                if (star.Checkpoint > _currentCheckpoint)
                {
                    _currentCheckpoint = star.Checkpoint;
                }
                
                // Track collected star
                _currentSave ??= new GameSaveData();
                _currentSave.CollectedStarCheckpoints ??= new List<int>();
                if (star.Checkpoint >= 0 && !_currentSave.CollectedStarCheckpoints.Contains(star.Checkpoint))
                {
                    _currentSave.CollectedStarCheckpoints.Add(star.Checkpoint);
                }

                SaveGame();
                
                PlaySfx(sfxStar);
            }
        }
        Rectangle feet = new Rectangle(
            player.Bounds.X + 6,
            player.Bounds.Bottom - 4,
            player.Bounds.Width - 12,
            6
        );

        // Spike collision (using Spike entities)
        foreach (Spike spike in stage.Spikes)
        {
            if (player.Bounds.Intersects(spike.Hitbox) && !player.IsInvincible)
            {
                currentHealth--;
                PlaySfx(sfxHurt);
                player.TriggerIncinvincible();
                shakeTimer = shakeDuration;

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

        float cullDistanceSq = 1000f * 1000f;

        // ── Ghost ────────────────────────────────────────────────────────
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = enemies[i];
            if (Vector2.DistanceSquared(player.Position, enemy.Position) > cullDistanceSq)
                continue;

            enemy.Update(gameTime, stage);

            if (player.Bounds.Intersects(enemy.Bounds))
            {
                if (player.getDashingState)
                {
                    if (!enemy.IsPhaseOut)
                    {
                        enemies[i] = enemies[enemies.Count - 1];
                        enemies.RemoveAt(enemies.Count - 1);

                        player.OnEnemyContact();
                        continue;
                    }
                }
                else if (!player.IsInvincible)
                {
                    currentHealth--;
                    PlaySfx(sfxHurt);
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

        // ── Lizard ───────────────────────────────────────────────────────
        for (int i = lizards.Count - 1; i >= 0; i--)
        {
            var liz = lizards[i];
            if (Vector2.DistanceSquared(player.Position, liz.Position) > cullDistanceSq)
                continue;

            liz.Update(gameTime, stage, player.Position);

            if (player.Bounds.Intersects(liz.Bounds) || liz.IsHittingPlayer(player.Bounds))
            {
                if (player.getDashingState)
                {
                    lizards[i] = lizards[lizards.Count - 1];
                    lizards.RemoveAt(lizards.Count - 1);

                    player.OnEnemyContact();
                    continue;
                }
                else if (!player.IsInvincible)
                {
                    currentHealth--;
                    PlaySfx(sfxHurt);
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

        // ── Bat ──────────────────────────────────────────────────────────
        for (int i = bats.Count - 1; i >= 0; i--)
        {
            var bat = bats[i];
            if (Vector2.DistanceSquared(player.Position, bat.Position) > cullDistanceSq)
                continue;

            bat.Update(gameTime, player.Position);

            if (player.Bounds.Intersects(bat.Bounds))
            {
                if (player.getDashingState)
                {
                    bats[i] = bats[bats.Count - 1];
                    bats.RemoveAt(bats.Count - 1);

                    player.OnEnemyContact();
                    continue;
                }
                else if (!player.IsInvincible)
                {
                    currentHealth--;
                    PlaySfx(sfxHurt);
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

        // Death zone collision detection
        foreach (var deathZone in stage.DeathZones)
        {
            if (deathZone.CheckCollision(player.Bounds))
            {
                TriggerGameOver(deathZone.ZoneTitle);
                return;
            }
        }

        // Objective/Skill collection and update
        foreach (var objective in stage.Objectives)
        {
            objective.Update(gameTime);
            
            if (objective.CheckCollision(player.Bounds))
            {
                objective.IsCollected = true;
                _currentSave ??= new GameSaveData();
                _currentSave.CollectedObjectiveCheckpoints ??= new List<int>();
                _currentSave.CollectedObjectiveKeys ??= new List<string>();

                if (objective.Checkpoint > 0 && !_currentSave.CollectedObjectiveCheckpoints.Contains(objective.Checkpoint))
                {
                    _currentSave.CollectedObjectiveCheckpoints.Add(objective.Checkpoint);
                }

                string objectiveKey = GetObjectiveSaveKey(objective);
                if (!string.IsNullOrEmpty(objectiveKey) && !_currentSave.CollectedObjectiveKeys.Contains(objectiveKey))
                {
                    _currentSave.CollectedObjectiveKeys.Add(objectiveKey);
                }
                
                // Handle skill unlocking based on the Skill field
                if (objective.Skill.Equals("wall_jump", StringComparison.OrdinalIgnoreCase))
                {
                    player.UnlockWallJump();
                    _currentSave.HasWallJump = true;
                    System.Diagnostics.Debug.WriteLine("Wall jump unlocked!");
                }
                else if (objective.Skill.Equals("dash", StringComparison.OrdinalIgnoreCase))
                {
                    player.UnlockDash();
                    _currentSave.HasDash = true;
                    System.Diagnostics.Debug.WriteLine("Dash unlocked!");
                }
                
                SaveGame(); // Save when collecting objectives/skills

                PlaySfx(sfxStar);
            }
        }

        // Spring collision and bounce
        foreach (var spring in stage.Springs)
        {
            spring.Update(gameTime);
            
            if (spring.CheckPlayerContact(player.Bounds))
            {
                spring.Trigger();
                player.ApplyKnockback(spring.GetBounceForce(player.FacingLeft));
                PlaySfx(sfxJump);
            }
        }

        // Conveyor belt push
        foreach (var conveyor in stage.Conveyors)
        {
            if (conveyor.IsPlayerOnConveyor(player.Bounds))
            {
                player.MoveBy(conveyor.GetPushVelocity(dt));
            }
        }

        // Update title displays
        foreach (var title in stage.Titles)
        {
            title.Update(gameTime);
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


        if (player.Position.Y > stage.VoidY)
        {
            TriggerGameOver("You fell into the void.");
            return;
        }

        float viewportWidth = GraphicsDevice.Viewport.Width / WorldZoom;
        float viewportHeight = GraphicsDevice.Viewport.Height / WorldZoom;

        // Center camera on player with vertical offset (show more above player)
        float cameraYOffset = -60f; // Negative = camera looks higher
        float targetCameraX = player.Position.X + 16 - viewportWidth / 2;
        float targetCameraY = player.Position.Y + 16 - viewportHeight / 2 + cameraYOffset;

        // Clamp to stage bounds
        // X: prevent showing outside left/right
        float minCameraX = 0;
        float maxCameraX = Math.Max(0, stage.StageWidthPixels - viewportWidth);
        // Y: allow camera to go above stage (follow player upward), but not below stage bottom
        float minCameraY = float.MinValue; // No upper limit - camera follows player up
        float maxCameraY = Math.Max(0, stage.StageHeightPixels - viewportHeight);

        // If stage is smaller than viewport, center the stage
        if (stage.StageWidthPixels < viewportWidth)
        {
            targetCameraX = -(viewportWidth - stage.StageWidthPixels) / 2;
            minCameraX = targetCameraX;
            maxCameraX = targetCameraX;
        }
        if (stage.StageHeightPixels < viewportHeight)
        {
            targetCameraY = -(viewportHeight - stage.StageHeightPixels) / 2;
            minCameraY = targetCameraY;
            maxCameraY = targetCameraY;
        }

        targetCameraX = Math.Clamp(targetCameraX, minCameraX, maxCameraX);
        targetCameraY = Math.Clamp(targetCameraY, minCameraY, maxCameraY);

        // SmoothDamp for buttery smooth camera
        float smoothTime = 0.15f;
        currentCameraX = SmoothDamp(currentCameraX, targetCameraX, ref cameraVelocityX, smoothTime, dt);
        currentCameraY = SmoothDamp(currentCameraY, targetCameraY, ref cameraVelocityY, smoothTime, dt);

        // Round for pixel-perfect rendering
        float cameraX = MathF.Round(currentCameraX);
        float cameraY = MathF.Round(currentCameraY);

        cameraTransform = Matrix.CreateTranslation(-cameraX + shakeOffset.X, -cameraY + shakeOffset.Y, 0)
                * Matrix.CreateScale(WorldZoom, WorldZoom, 1f);
    }

    // SmoothDamp - smooth interpolation like Unity's
    private static float SmoothDamp(float current, float target, ref float velocity, float smoothTime, float dt)
    {
        smoothTime = Math.Max(0.0001f, smoothTime);
        float omega = 2f / smoothTime;
        float x = omega * dt;
        float exp = 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);
        float change = current - target;
        float temp = (velocity + omega * change) * dt;
        velocity = (velocity - omega * temp) * exp;
        float result = target + (change + temp) * exp;
        
        // Prevent overshooting
        if ((target - current > 0f) == (result > target))
        {
            result = target;
            velocity = 0f;
        }
        return result;
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

            case GameState.SaveLoad:
                GraphicsDevice.Clear(new Color(4, 6, 16));
                _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                _saveLoad.Draw(_spriteBatch, sw, sh);
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

            case GameState.Complete:
                GraphicsDevice.Clear(new Color(4, 6, 16));
                _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                _complete.Draw(_spriteBatch, sw, sh);
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
        GraphicsDevice.Clear(stage.BgColor);

        _spriteBatch.Begin(transformMatrix: cameraTransform, samplerState: SamplerState.PointClamp);

        // Draw only tiles near the camera to reduce per-frame draw calls.
        int viewportWidth = GraphicsDevice.Viewport.Width;
        int viewportHeight = GraphicsDevice.Viewport.Height;
        int worldViewWidth = (int)MathF.Ceiling(viewportWidth / WorldZoom);
        int worldViewHeight = (int)MathF.Ceiling(viewportHeight / WorldZoom);
        Rectangle worldCullBounds = new Rectangle(
            (int)MathF.Floor(currentCameraX),
            (int)MathF.Floor(currentCameraY),
            Math.Max(1, worldViewWidth),
            Math.Max(1, worldViewHeight)
        );

        // Draw LDtk prerendered levels
        stage.Draw(_spriteBatch, worldCullBounds);

        foreach (var mp in stage.MovingPlatforms)
            mp.Draw(_spriteBatch, tilemap);

        // Draw spikes
        foreach (var spike in stage.Spikes)
            spike.Draw(_spriteBatch, tilemap);

        // Draw objectives/skills
        foreach (var objective in stage.Objectives)
            objective.Draw(_spriteBatch, tilemap);

        // Draw springs
        foreach (var spring in stage.Springs)
            spring.Draw(_spriteBatch, tilemap);

        // Draw conveyors
        foreach (var conveyor in stage.Conveyors)
            conveyor.Draw(_spriteBatch, tilemap);

        // Draw titles
        foreach (var title in stage.Titles)
            title.Draw(_spriteBatch, _menuFont);

        // Draw Goal
        foreach (var goal in stage.Goals)
            goal.Draw(_spriteBatch, tilemap);

        // moving platform debug
        // foreach (var mp in stage.MovingPlatforms)
        //     _spriteBatch.Draw(pixel, mp.Bounds, Color.Blue * 0.4f);

        Rectangle cameraCullBounds = new Rectangle(
            (int)MathF.Floor(currentCameraX) - 100,
            (int)MathF.Floor(currentCameraY) - 100,
             (int)(GraphicsDevice.Viewport.Width / WorldZoom) + 200,
             (int)(GraphicsDevice.Viewport.Height / WorldZoom) + 200
        );

        // ghost
        foreach (Enemy enemy in enemies)
        {
            if (cameraCullBounds.Intersects(enemy.Bounds))
                enemy.Draw(_spriteBatch, ghostTexture);
        }

        // lizard
        foreach (var liz in lizards)
        {
            if (cameraCullBounds.Intersects(liz.Bounds))
                liz.Draw(_spriteBatch, lizardWalkTex, lizardTongueTex);
        }

        // bat
        foreach (var bat in bats)
        {
            if (cameraCullBounds.Intersects(bat.Bounds))
                bat.Draw(_spriteBatch, batIdleTex, batAtkTex);
        }

        // draw star
        foreach (var star in stars)
        {
            star.Draw(_spriteBatch, starTexture);
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

        TimeSpan played = TimeSpan.FromSeconds(elapsedTime);
        string timeText = $"Time: {played:mm\\:ss}";
        _spriteBatch.DrawString(font, timeText, new Vector2(20, 20), Color.White);

        // draw hearts
        int heartSize = 80;
        int heartPadding = 6;
        for (int i = 0; i < maxHealth; i++)
        {
            Texture2D heart = i < currentHealth ? heartFull : heartEmpty;
            _spriteBatch.Draw(heart, new Rectangle(20 + i * (heartSize + heartPadding), 55, heartSize, heartSize), Color.White);
        }


        _spriteBatch.End();
    }

    private void BuildStageAndActors(bool loadFromSave = false, bool retryFromCheckpoint = false)
    {
        stage = new Stage(Content, _spriteBatch, tilemap);

        player = new Player();
        player.Position = stage.PlayerSpawn;

        bool shouldUseSaveData = (loadFromSave || retryFromCheckpoint) && _currentSave != null;

        // Load player abilities from save when continuing or retrying.
        if (shouldUseSaveData)
        {
            _currentSave.CollectedObjectiveCheckpoints ??= new List<int>();
            _currentSave.CollectedStarCheckpoints ??= new List<int>();
            _currentSave.CollectedObjectiveKeys ??= new List<string>();

            if (_currentSave.HasWallJump) player.UnlockWallJump();
            if (_currentSave.HasDash) player.UnlockDash();

            if (loadFromSave)
                _currentCheckpoint = _currentSave.CurrentCheckpoint;

            collectedStarsCount = _currentSave.CollectedStars;
        }
        else if (!retryFromCheckpoint)
        {
            _currentCheckpoint = 0;
            collectedStarsCount = 0;
        }

        enemies = new List<Enemy>();
        lizards = new List<EnemyLizard>();
        bats = new List<EnemyBat>();
       
            // Legacy enemy spawns (keep for backward compatibility)
            foreach (Vector2 spawn in stage.EnemySpawns)
                enemies.Add(new Enemy(spawn));

            foreach (Vector2 spawn in stage.LizardSpawns)
                lizards.Add(new EnemyLizard(spawn));

            foreach (Vector2 spawn in stage.BatSpawns)
                bats.Add(new EnemyBat(spawn));

            // New LDTK-based enemy spawning with MonsterType
            foreach (var spawnData in stage.EnemySpawnDataList)
            {
                switch (spawnData.Type)
                {
                    case MonsterType.Ghost:
                        enemies.Add(new Enemy(spawnData.Position));
                        break;
                    case MonsterType.Lizard:
                        lizards.Add(new EnemyLizard(spawnData.Position));
                        break;
                    case MonsterType.Bat:
                        bats.Add(new EnemyBat(spawnData.Position));
                        break;
                }
            }

        // Load stars with checkpoint data
        stars = new List<Star>();
        foreach (var (position, checkpoint, tileSource) in stage.StarSpawnData)
        {
            // Skip stars that were already collected in this save.
            if (shouldUseSaveData && _currentSave != null && _currentSave.CollectedStarCheckpoints.Contains(checkpoint))
            {
                continue;
            }

            var star = new Star(position, checkpoint, tileSource);
            stars.Add(star);
        }

        // Also handle legacy star spawns
        foreach (Vector2 spawn in stage.StarSpawns)
        {
            if (!stage.StarSpawnData.Any(s => s.position == spawn))
                stars.Add(new Star(starTexture, spawn));
        }

        // Remove objectives already collected in this save.
        if (shouldUseSaveData && _currentSave != null)
        {
            stage.Objectives.RemoveAll(objective =>
            {
                string objectiveKey = GetObjectiveSaveKey(objective);
                bool collectedByKey = !string.IsNullOrEmpty(objectiveKey)
                    && _currentSave.CollectedObjectiveKeys.Contains(objectiveKey);

                bool collectedBySkill =
                    (objective.Skill.Equals("wall_jump", StringComparison.OrdinalIgnoreCase) && _currentSave.HasWallJump) ||
                    (objective.Skill.Equals("dash", StringComparison.OrdinalIgnoreCase) && _currentSave.HasDash);

                return collectedByKey || collectedBySkill;
            });
        }
        // New game starts at PlayerStart, loading/ retry starts from checkpoint.
        if (loadFromSave && _currentSave != null)
            player.Position = ResolveCheckpointSpawn(_currentCheckpoint);
        else if (retryFromCheckpoint)
        {
            bool hasReachedCheckpoint = _currentSave?.CollectedStarCheckpoints != null
                && _currentSave.CollectedStarCheckpoints.Contains(_currentCheckpoint);

            player.Position = hasReachedCheckpoint
                ? ResolveCheckpointSpawn(_currentCheckpoint)
                : stage.PlayerSpawn;
        }
        else
            player.Position = stage.PlayerSpawn;

        player.ResetVelocity();

        if ((loadFromSave || retryFromCheckpoint) && _currentSave != null)
        {
            elapsedTime = _currentSave.ElapsedTime;
        }
        else
        {
            elapsedTime = 0f;
        }

        currentHealth = maxHealth;

        UpdateCameraImmediate();
    }

    private void SaveGame()
    {
        _currentSave ??= new GameSaveData();
        _currentSave.CollectedObjectiveCheckpoints ??= new List<int>();
        _currentSave.CollectedStarCheckpoints ??= new List<int>();
        _currentSave.CollectedObjectiveKeys ??= new List<string>();

        _currentSave.CurrentCheckpoint = _currentCheckpoint;
        _currentSave.CollectedStars = collectedStarsCount;
        _currentSave.HasWallJump = player?.CanWallJump ?? false;
        _currentSave.HasDash = player?.CanDash ?? false;
        _currentSave.ElapsedTime = elapsedTime;
        _currentSave.Save();
    }

    private static string GetObjectiveSaveKey(Objective objective)
    {
        if (objective == null)
            return string.Empty;

        string skill = objective.Skill?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrEmpty(skill))
            return $"skill:{skill}";

        if (objective.Checkpoint > 0)
            return $"checkpoint:{objective.Checkpoint}";

        return $"pos:{(int)objective.Position.X}:{(int)objective.Position.Y}";
    }

    private Vector2 ResolveCheckpointSpawn(int checkpoint)
    {
        foreach (var (position, checkpointId, _) in stage.StarSpawnData)
        {
            if (checkpointId == checkpoint)
            {
                return new Vector2(position.X - 8f, position.Y - 16f);
            }
        }

        return stage.PlayerSpawn;
    }

    private void UpdateCameraImmediate()
    {
        float viewportWidth = GraphicsDevice.Viewport.Width / WorldZoom;
        float viewportHeight = GraphicsDevice.Viewport.Height / WorldZoom;

        // Center camera on player with vertical offset (show more above player)
        float cameraYOffset = -60f;
        float cameraX = player.Position.X + 16 - viewportWidth / 2;
        float cameraY = player.Position.Y + 16 - viewportHeight / 2 + cameraYOffset;

        // Clamp to stage bounds
        // X: prevent showing outside left/right
        float minCameraX = 0;
        float maxCameraX = Math.Max(0, stage.StageWidthPixels - viewportWidth);
        // Y: allow camera to go above stage (follow player upward), but not below stage bottom
        float minCameraY = float.MinValue;
        float maxCameraY = Math.Max(0, stage.StageHeightPixels - viewportHeight);

        // If stage is smaller than viewport, center the stage
        if (stage.StageWidthPixels < viewportWidth)
        {
            cameraX = -(viewportWidth - stage.StageWidthPixels) / 2;
            minCameraX = cameraX;
            maxCameraX = cameraX;
        }
        if (stage.StageHeightPixels < viewportHeight)
        {
            cameraY = -(viewportHeight - stage.StageHeightPixels) / 2;
            minCameraY = cameraY;
            maxCameraY = cameraY;
        }

        cameraX = Math.Clamp(cameraX, minCameraX, maxCameraX);
        cameraY = Math.Clamp(cameraY, minCameraY, maxCameraY);

        cameraX = MathF.Round(cameraX);
        cameraY = MathF.Round(cameraY);

        currentCameraX = cameraX;
        currentCameraY = cameraY;
        cameraVelocityX = 0f;
        cameraVelocityY = 0f;

        cameraTransform = Matrix.CreateTranslation(-cameraX, -cameraY, 0)
                        * Matrix.CreateScale(WorldZoom, WorldZoom, 1f);
    }
}
