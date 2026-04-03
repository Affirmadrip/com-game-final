using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;

namespace GalactaJumperMo.Classes
{
    public class Player
    {
        public Vector2 Position;
        private Vector2 velocity;
        public Vector2 Velocity => velocity;

        // Core Physics - Updated values from new code
        private float speed = 180f;
        private float jumpForce = -330f;
        private float gravity = 1000f;

        private bool isOnGround = false;
        public bool JustJumped { get; private set; }
        public bool JustDashed { get; private set; }

        // Animation management
        private int animRow;
        private int animFrame;
        private float animTimer;
        private float[] frameDurations = { 0.35f, 0.1f, 0.18f, 0.25f, 0.1f, 0.2f };
        private int[] frameCounts = { 2, 6, 3, 2, 5, 1 };
        public bool FacingLeft { get; private set; }
        public Rectangle SourceRect => new Rectangle(animFrame * 32, animRow * 32, 32, 32);

        // Smaller hitbox for better collision (centered in the 32x32 sprite)
        private const int HitboxWidth = 16;
        private const int HitboxHeight = 24;
        private const int HitboxOffsetX = 8;
        private const int HitboxOffsetY = 8;

        public Rectangle Bounds =>
            new Rectangle(
                (int)Position.X + HitboxOffsetX,
                (int)Position.Y + HitboxOffsetY,
                HitboxWidth,
                HitboxHeight
            );

        public Vector2 DrawCenter =>
            isOnWall
                ? new Vector2(Bounds.Center.X - wallSide * HitboxOffsetX, Bounds.Center.Y)
                : new Vector2(Bounds.Center.X, Bounds.Center.Y);

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

        private bool justWallJumped { get; set; }
        private bool isOnWall = false;
        private int wallSide = 0;
        private int touchingWallSide = 0;
        private float touchingWallTimer = 0f;

        // Ability unlock system
        public bool CanWallJump { get; private set; } = false;
        public bool CanDash { get; private set; } = false;

        // Girder platform drop-through
        private bool droppingThroughGirder = false;
        private float dropThroughTimer = 0f;
        private const float DropThroughDuration = 0.3f;

        public void UnlockWallJump()
        {
            CanWallJump = true;
        }

        public void UnlockDash()
        {
            CanDash = true;
        }

        public void Update(GameTime gameTime, Stage stage)
        {
            justWallJumped = false;
            JustJumped = false;
            JustDashed = false;
            bool wasOnWall = isOnWall;
            isOnWall = false;
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            touchingWallTimer -= dt;
            KeyboardState k = Keyboard.GetState();
            KeyboardState currentKeyboard = Keyboard.GetState();

            if (dashCooldownTimer > 0)
                dashCooldownTimer -= dt;

            // Update drop-through timer
            if (dropThroughTimer > 0)
            {
                dropThroughTimer -= dt;
                if (dropThroughTimer <= 0)
                    droppingThroughGirder = false;
            }

            // Check for drop-through input (S key while on ground)
            bool sJustPressed = currentKeyboard.IsKeyDown(Keys.S) && !prevKeyboard.IsKeyDown(Keys.S);
            if (sJustPressed && isOnGround)
            {
                droppingThroughGirder = true;
                dropThroughTimer = DropThroughDuration;
            }

            // Dash ability (only if unlocked)
            bool shiftJustPressed = currentKeyboard.IsKeyDown(Keys.LeftShift) && !prevKeyboard.IsKeyDown(Keys.LeftShift);
            if (shiftJustPressed && !isDashing && !isOnWall && dashCooldownTimer <= 0f && CanDash)
            {
                float dx = 0f, dy = 0f;
                if (k.IsKeyDown(Keys.W)) dy = -1f;
                if (k.IsKeyDown(Keys.S)) dy = +1f;
                if (k.IsKeyDown(Keys.A)) dx = -1f;
                if (k.IsKeyDown(Keys.D)) dx = +1f;
                if (dx == 0 && dy == 0)
                    dx = FacingLeft ? -1f : 1f;
                dashDirection = Vector2.Normalize(new Vector2(dx, dy));
                isDashing = true;
                JustDashed = true;
                dashTimer = dashDuration;
                dashCooldownTimer = dashCooldown;
            }

            if (!isDashing && !wasOnWall)
            {
                float targetSpeed = 0f;
                if (k.IsKeyDown(Keys.A))
                    targetSpeed = -speed;
                else if (k.IsKeyDown(Keys.D))
                    targetSpeed = speed;
                velocity.X = targetSpeed;

                if (k.IsKeyDown(Keys.Space) && isOnGround)
                {
                    velocity.Y = jumpForce;
                    isOnGround = false;
                    JustJumped = true;
                }
            }
            else if (!isDashing && wasOnWall)
            {
                velocity.X = wallSide * 40f;
            }

            bool spaceJustPressed = currentKeyboard.IsKeyDown(Keys.Space) && !prevKeyboard.IsKeyDown(Keys.Space);
            if (spaceJustPressed && touchingWallTimer > 0f && !wasOnWall && !isOnGround && CanWallJump)
            {
                isOnWall = true;
                wallSide = touchingWallSide;
                velocity = Vector2.Zero;
            }
            if (wasOnWall && spaceJustPressed)
            {
                isOnWall = false;
                velocity.X = -wallSide * speed;
                velocity.Y = jumpForce;
                dashCooldownTimer = 0f;
                justWallJumped = true;
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

            if (!isDashing && !wasOnWall)
                velocity.Y += gravity * dt;
            else if (wasOnWall && !justWallJumped)
            {
                if (velocity.Y < 0) velocity.Y = 0f;
                velocity.Y = 0f;
            }

            isOnGround = false;

            float moveY = velocity.Y * dt;
            float moveX = velocity.X * dt;

            // Vertical collision with drop-through support
            if (moveY != 0)
            {
                float newY = Position.Y + moveY;
                int boxY = (velocity.Y > 0) ? (int)MathF.Ceiling(newY) : (int)MathF.Floor(newY);

                Rectangle testBounds = new Rectangle(
                    (int)MathF.Round(Position.X) + HitboxOffsetX,
                    boxY + HitboxOffsetY,
                    HitboxWidth,
                    HitboxHeight
                );

                // Check girder platforms first (one-way platforms)
                if (velocity.Y > 0 && !droppingThroughGirder)
                {
                    foreach (Rectangle girder in stage.GirderPlatforms)
                    {
                        if (testBounds.Intersects(girder))
                        {
                            if (Position.Y + HitboxOffsetY + HitboxHeight <= girder.Top + 4)
                            {
                                newY = girder.Top - HitboxHeight - HitboxOffsetY;
                                isOnGround = true;
                                velocity.Y = 0;
                                break;
                            }
                        }
                    }
                }

                // Check regular platforms
                foreach (Rectangle platform in stage.Platforms)
                {
                    if (testBounds.Intersects(platform))
                    {
                        if (velocity.Y > 0)
                        {
                            newY = platform.Top - HitboxHeight - HitboxOffsetY;
                            isOnGround = true;
                            isOnWall = false;
                            touchingWallTimer = 0f;
                        }
                        else if (velocity.Y < 0)
                            newY = platform.Bottom - HitboxOffsetY;
                        velocity.Y = 0;
                        break;
                    }
                }
                Position.Y = newY;
            }

            // Horizontal collision with wall jump platform detection
            if (moveX != 0)
            {
                float newX = Position.X + moveX;
                int boxX = (velocity.X > 0) ? (int)MathF.Ceiling(newX) : (int)MathF.Floor(newX);

                Rectangle testBounds = new Rectangle(
                    boxX + HitboxOffsetX,
                    (int)MathF.Round(Position.Y) + HitboxOffsetY,
                    HitboxWidth,
                    HitboxHeight
                );

                Rectangle? collidedPlatform = null;

                foreach (Rectangle platform in stage.Platforms)
                {
                    if (testBounds.Intersects(platform))
                    {
                        collidedPlatform = platform;
                        break;
                    }
                }

                if (collidedPlatform.HasValue)
                {
                    Rectangle platform = collidedPlatform.Value;

                    bool isWallJumpPlatform = false;
                    foreach (Rectangle wjp in stage.WallJumpPlatforms)
                    {
                        if (wjp == platform || wjp.Intersects(testBounds))
                        {
                            isWallJumpPlatform = true;
                            break;
                        }
                    }

                    if (velocity.X > 0)
                    {
                        newX = platform.Left - HitboxWidth - HitboxOffsetX;
                        if (!justWallJumped && isWallJumpPlatform && CanWallJump)
                        {
                            touchingWallSide = 1;
                            touchingWallTimer = 0.15f;
                            if (wasOnWall) { isOnWall = true; wallSide = 1; }
                        }
                    }
                    else if (velocity.X < 0)
                    {
                        newX = platform.Right - HitboxOffsetX;
                        if (!justWallJumped && isWallJumpPlatform && CanWallJump)
                        {
                            touchingWallSide = -1;
                            touchingWallTimer = 0.15f;
                            if (wasOnWall) { isOnWall = true; wallSide = -1; }
                        }
                    }
                    velocity.X = 0;
                    if (isDashing)
                    {
                        isDashing = false;
                        dashTimer = 0f;
                        velocity.Y = 0f;
                    }
                }
                Position.X = newX;
            }

            // Animation updates
            int newRow;
            if (isDashing)
                newRow = 4;
            else if (!isOnGround && velocity.Y < 0)
                newRow = 2;
            else if (!isOnGround && velocity.Y > 80f)
                newRow = 3;
            else if (isOnGround && (velocity.X > 10f || velocity.X < -10f))
                newRow = 1;
            else if (isOnGround && velocity.X == 0)
                newRow = 0;
            else if (isOnWall)
                newRow = 5;
            else
                newRow = animRow;

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