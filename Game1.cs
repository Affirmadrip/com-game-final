using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using GalactaJumperMo.Classes;

namespace GalactaJumperMo;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    private Stage stage;
    private Player player;

    private Texture2D pixel;
    private Texture2D tilemap;
    private SpriteFont font;

    private float timeLeft = 120f;
    private bool isGameOver = false;

    private Matrix cameraTransform;

    private Texture2D playerTexture;


    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        _graphics.PreferredBackBufferWidth = 800;
        _graphics.PreferredBackBufferHeight = 480;
    }

    protected override void Initialize()
    {
        stage = new Stage();

        player = new Player();
        player.Position = stage.PlayerSpawn;

        cameraTransform = Matrix.Identity;

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        pixel = new Texture2D(GraphicsDevice, 1, 1);
        pixel.SetData(new[] { Color.White });

        font = Content.Load<SpriteFont>("Fonts/GameFont");
        tilemap = Content.Load<Texture2D>("Stage/monochrome_tilemap_transparent_packed");
        playerTexture = Content.Load<Texture2D>("Player/mo_sprites");
    }

    protected override void Update(GameTime gameTime)
    {
        KeyboardState keyboard = Keyboard.GetState();

        if (keyboard.IsKeyDown(Keys.Escape))
            Exit();

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (!isGameOver)
        {
            player.Update(gameTime, stage);

            timeLeft -= dt;
            if (timeLeft <= 0f)
            {
                timeLeft = 0f;
                isGameOver = true;
            }

            // Visible platforms only. Everything else below is void.
            if (player.Position.Y > stage.VoidY)
            {
                isGameOver = true;
            }

            float viewportWidth = GraphicsDevice.Viewport.Width;
            float maxCameraX = Math.Max(0, stage.StageWidthPixels - viewportWidth);
            float cameraX = Math.Clamp(player.Position.X - 250f, 0, maxCameraX);

            cameraTransform = Matrix.CreateTranslation(-cameraX, 0, 0);
        }

        if (keyboard.IsKeyDown(Keys.R))
        {
            RestartStage();
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        // WORLD
        _spriteBatch.Begin(transformMatrix: cameraTransform, samplerState: SamplerState.PointClamp);

        // Solid stage tiles
        foreach (TileInstance tile in stage.SolidTiles)
        {
            _spriteBatch.Draw(tilemap, tile.Destination, tile.Source, Color.White);
        }

        // Decorations (no collision)
        foreach (TileInstance tile in stage.DecorationTiles)
        {
            _spriteBatch.Draw(tilemap, tile.Destination, tile.Source, Color.White);
        }

        // Temporary player
        Vector2 drawPos = new Vector2(player.Bounds.Center.X, player.Bounds.Center.Y);
        Vector2 origin = new Vector2(16, 16); // centre of the 32×32 frame

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
        _spriteBatch.End();

        // UI
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        Color timerColor = timeLeft < 10 ? Color.Red : Color.White;
        _spriteBatch.DrawString(font, $"Time: {(int)timeLeft}", new Vector2(20, 20), timerColor);

        if (isGameOver)
        {
            _spriteBatch.DrawString(font, "GAME OVER", new Vector2(20, 60), Color.Red);
            _spriteBatch.DrawString(font, "Press R to Retry", new Vector2(20, 95), Color.White);
        }

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void RestartStage()
    {
        player.Position = stage.PlayerSpawn;
        player.ResetVelocity();

        timeLeft = 120f;
        isGameOver = false;
        cameraTransform = Matrix.Identity;
    }
}