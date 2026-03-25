using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace GalactaJumperMo.Classes
{
    public class EnemyBat
    {
        public Vector2 Position;
        private Vector2 startPosition;
        private float speed = 80f;
        private int direction = 1;

        // Sine Wave Movement
        private float waveTimer = 0f;
        private float waveFrequency = 4f;
        private float waveAmplitude = 15f;

        private enum BatState { Idle, Attacking }
        private BatState currentState = BatState.Idle;

        // Hitbox
        public Rectangle Bounds => new Rectangle((int)Position.X - 8, (int)Position.Y - 8, 16, 16);

        // Animation Settings
        private int animFrame;
        private float animTimer;
        private float idleFrameDuration = 0.08f;
        private float attackFrameDuration = 0.06f;

        private const int IDLE_MAX_FRAMES = 9;
        private const int ATK_MAX_FRAMES = 8;

        private float attackCooldown = 0f;
        private const float MAX_ATTACK_COOLDOWN = 1.5f;
        private Vector2 attackTarget;

        public EnemyBat(Vector2 startPos)
        {
            Position = startPos;
            startPosition = startPos;
        }

        public void Update(GameTime gameTime, Vector2 playerPos)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (currentState == BatState.Idle)
                UpdateIdle(dt, playerPos);
            else
                UpdateAttacking(dt, playerPos);

            // --- Animation Logic ---
            int currentMaxFrames = (currentState == BatState.Idle) ? IDLE_MAX_FRAMES : ATK_MAX_FRAMES;
            float currentFrameDuration = (currentState == BatState.Idle) ? idleFrameDuration : attackFrameDuration;

            animTimer += dt;
            if (animTimer >= currentFrameDuration)
            {
                animTimer = 0;
                animFrame++;

                if (animFrame >= currentMaxFrames)
                {
                    animFrame = 0;

                    if (currentState == BatState.Attacking)
                    {
                        attackCooldown = MAX_ATTACK_COOLDOWN;
                        ChangeState(BatState.Idle);
                    }
                }
            }
        }

        private void UpdateIdle(float dt, Vector2 playerPos)

            Position.X += speed * direction * dt;

            if (direction > 0 && Position.X > startPosition.X + 100) direction = -1;
            else if (direction < 0 && Position.X < startPosition.X - 100) direction = 1;

            waveTimer += dt * waveFrequency;
            Position.Y = startPosition.Y + (float)Math.Sin(waveTimer) * waveAmplitude;

            attackCooldown -= dt;
            float distToPlayer = Vector2.Distance(Position, playerPos);
            if (distToPlayer < 120f && attackCooldown <= 0)
            {
                attackTarget = playerPos;
                ChangeState(BatState.Attacking);
            }
        }

        private void UpdateAttacking(float dt, Vector2 playerPos)
        {
            if (Position != attackTarget)
            {
                Vector2 dir = Vector2.Normalize(attackTarget - Position);
                Position += dir * speed * 2.5f * dt;
            }
        }

        private void ChangeState(BatState newState)
        {
            currentState = newState;
            animFrame = 0;
            animTimer = 0;

            if (newState == BatState.Idle) startPosition = Position;
        }

        public void Draw(SpriteBatch sb, Texture2D idleTex, Texture2D atkTex)
        {
            Texture2D currentTex = (currentState == BatState.Idle) ? idleTex : atkTex;
            int currentMaxFrames = (currentState == BatState.Idle) ? IDLE_MAX_FRAMES : ATK_MAX_FRAMES;

            if (currentTex == null || currentMaxFrames <= 0) return;

            int frameWidth = currentTex.Width / currentMaxFrames;
            int frameHeight = currentTex.Height;

            int safeFrame = animFrame % currentMaxFrames;

            Rectangle sourceRect = new Rectangle(safeFrame * frameWidth, 0, frameWidth, frameHeight);
            Vector2 origin = new Vector2(frameWidth / 2f, frameHeight / 2f);

            SpriteEffects flip = SpriteEffects.None;
            if (currentState == BatState.Idle)
            {
                flip = direction < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            }
            else
            {
                flip = (attackTarget.X < Position.X) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            }

            sb.Draw(currentTex, Position, sourceRect, Color.White, 0f, origin, 1.0f, flip, 0f);
        }

        public bool IsHittingPlayer(Rectangle playerBounds)
        {
            return Bounds.Intersects(playerBounds);
        }
    }
}