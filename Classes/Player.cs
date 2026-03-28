using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;

namespace GalactaJumperMo.Classes
{


    public class Player
    {
        public Vector2 Position;
        private Vector2 velocity;

        private float speed = 320f;
        private float jumpForce = -430f;
        private float gravity = 1000f;

        private bool isOnGround = false;
        public bool JustJumped { get; private set; }
        public bool JustDashed { get; private set; }

        // animation management
        private int animRow;
        private int animFrame;
        private float animTimer;
        private float[] frameDurations = { 0.35f, 0.1f, 0.18f, 0.25f, 0.1f };
        private int[] frameCounts = { 2, 6, 3, 2, 5 };
        public bool FacingLeft { get; private set; }
        public Rectangle SourceRect => new Rectangle(animFrame * 32, animRow * 32, 32, 32);
        public Rectangle Bounds =>
            new Rectangle((int)Position.X, (int)Position.Y, 32, 32);

        // Health System
        public bool IsInvincible => invincibilityTimer > 0f;
        public bool Visible = true;
        private float invincibilityTimer = 0f;
        private const float InvincibilityDuration = 1.5f;
        private float flickerTimer = 0f;

        // 8-directional dashing
        public bool getDashingState => isDashing;
        public Vector2 getDashDirection => dashDirection;
        private KeyboardState prevKeyboard;
        private bool isDashing;
        private float dashTimer;
        private float dashCooldownTimer;
        private float dashDuration = 0.3f;
        private float dashSpeed = 550f;
        private float dashCooldown = 1.5f;
        private Vector2 dashDirection;

 
        public void Update(GameTime gameTime, Stage stage)
        {
            JustJumped = false;
            JustDashed = false;
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            KeyboardState k = Keyboard.GetState();

            KeyboardState currentKeyboard = Keyboard.GetState();
            if (dashCooldownTimer > 0)
            {
                dashCooldownTimer -= dt;
            }

            bool shiftJustPressed = currentKeyboard.IsKeyDown(Keys.LeftShift) && !prevKeyboard.IsKeyDown(Keys.LeftShift);
            if (shiftJustPressed && !isDashing && dashCooldownTimer <= 0f)
            {
                float dx = 0f, dy = 0f;
                if (k.IsKeyDown(Keys.W)) dy = -1f;
                if (k.IsKeyDown(Keys.S)) dy = +1f;
                if (k.IsKeyDown(Keys.A)) dx = -1f;
                if (k.IsKeyDown(Keys.D)) dx = +1f;

                if (dx == 0 && dy == 0)
                {
                    dx = FacingLeft ? -1f : 1f;
                }

                dashDirection = Vector2.Normalize(new Vector2(dx, dy));
                isDashing = true;
                JustDashed = true;
                dashTimer = dashDuration;
                dashCooldownTimer = dashCooldown;
            }

            if (!isDashing)
            {
                float acceleration = 900f;
                float targetSpeed = 0f;
                if (k.IsKeyDown(Keys.A)) {
                    targetSpeed = -speed;
                }
                else if (k.IsKeyDown(Keys.D))
                {
                    targetSpeed = speed;
                } 
                if (Math.Abs(targetSpeed - velocity.X) <= acceleration * dt) {
                    velocity.X = targetSpeed;
                }
                else if (isOnGround)
                {
                    velocity.X = (velocity.X + Math.Sign(targetSpeed - velocity.X) * acceleration * dt) * 0.9f;
                }
                else {
                    velocity.X = velocity.X + Math.Sign(targetSpeed - velocity.X) * acceleration * dt;
                }

                if (k.IsKeyDown(Keys.Space) && isOnGround)
                {
                    velocity.Y = jumpForce;
                    isOnGround = false;
                    JustJumped = true;
                }
            }

            prevKeyboard = currentKeyboard;

            if (isDashing)
            {
                dashTimer -= dt;
                velocity = dashDirection * dashSpeed;

                if (dashTimer <= 0f)
                {
                    isDashing = false;
                    velocity = Vector2.Zero;
                }
            }

            // gravity
            if (!isDashing)
                velocity.Y += gravity * dt;

            isOnGround = false;

            // horizontal collision
            Position.X += velocity.X * dt;
            Rectangle boundsX = Bounds;

            foreach (Rectangle platform in stage.Platforms)
            {
                if (!boundsX.Intersects(platform))
                    continue;

                if (velocity.X > 0) // moving right
                {
                    Position.X = platform.Left - Bounds.Width;
                }
                else if (velocity.X < 0) // moving left
                {
                    Position.X = platform.Right;
                }

                velocity.X = 0;
                boundsX = Bounds;
            }

            // vertical collision
            Position.Y += velocity.Y * dt;
            Rectangle boundsY = Bounds;

            foreach (Rectangle platform in stage.Platforms)
            {
                if (!boundsY.Intersects(platform))
                    continue;

                if (velocity.Y > 0) // falling
                {
                    Position.Y = platform.Top - Bounds.Height;
                    velocity.Y = 0;
                    isOnGround = true;
                }
                else if (velocity.Y < 0) // jumping upward
                {
                    Position.Y = platform.Bottom;
                    velocity.Y = 0;
                }

                boundsY = Bounds;
            }

            // animation updates
            int newRow;
            if (isDashing)
            {
                newRow = 4;
            }
            else if (!isOnGround && velocity.Y < 0)
            {
                newRow = 2;
            }
            else if (!isOnGround && velocity.Y > 80f)
            {
                newRow = 3;
            }
            else if (isOnGround && (velocity.X > 10f || velocity.X < -10f))
            {
                newRow = 1;
            }
            else if (isOnGround && velocity.X == 0)
            {
                newRow = 0;
            }
            else
            {
                newRow = animRow;
            }

            if (newRow != animRow)
            {
                animRow = newRow;
                animFrame = 0;
                animTimer = 0f;
            }

            animTimer += dt;
            if (animTimer >= frameDurations[animRow])
            {
                animTimer = 0f;
                animFrame = (animFrame + 1) % frameCounts[animRow];
            }

            if (velocity.X < 0)
                FacingLeft = true;
            else if (velocity.X > 0)
                FacingLeft = false;

            if (invincibilityTimer > 0f)
            {
                invincibilityTimer -= dt;
                flickerTimer += dt;
                Visible = (int)(flickerTimer / 0.1f) % 2 == 0;
            }
            else
            {
                Visible = true;
            }
        }

        public void TriggerIncinvincible()
        {
            invincibilityTimer = InvincibilityDuration;
            flickerTimer = 0f;
        }

        public void MoveBy(Vector2 delta)
        {
            Position += delta;
        }

        public void ApplyKnockback(Vector2 force)
        {
            velocity = force;
            isOnGround = false;
            isDashing = false;
        }
        public void ResetVelocity()
        {
            velocity = Vector2.Zero;
            isOnGround = false;
        }

        public void OnEnemyContact()
        {
            velocity.Y = jumpForce * 0.8f;
            isDashing = false;
            dashTimer = 0f;
            dashCooldownTimer = 0f;
        }
    }
}