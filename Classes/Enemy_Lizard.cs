using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace GalactaJumperMo.Classes
{
    public class EnemyLizard
    {
        public Vector2 Position;
        private Vector2 velocity;
        private float speed = 50f;
        private float gravity = 500f;
        private int direction = 1;

        private enum LizardState { Walking, Attacking }
        private LizardState currentState = LizardState.Walking;

        // Hitbox body
        public Rectangle Bounds => direction > 0
            ? new Rectangle((int)Position.X + 0, (int)Position.Y - 12, 6, 12)
            : new Rectangle((int)Position.X + 20, (int)Position.Y - 12, 6, 12);

        // Hitbox tongue
        public Rectangle TongueBounds => direction > 0
            ? new Rectangle((int)Position.X + 0, (int)Position.Y - 14, 20, 8)
            : new Rectangle((int)Position.X - 0, (int)Position.Y - 14, 20, 8);

        private int animFrame;
        private float animTimer;

        private float walkFrameDuration = 0.15f;
        private float attackFrameDuration = 0.18f;

        private float attackCooldown = 0f;
        private const float MAX_ATTACK_COOLDOWN = 2.0f;

        public EnemyLizard(Vector2 startPos)
        {
            Position = startPos;
        }

        public void Update(GameTime gameTime, Stage stage, Vector2 playerPos)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (currentState == LizardState.Walking)
                UpdateWalking(dt, stage, playerPos);
            else
                UpdateAttacking(dt);

            animTimer += dt;
            float currentFrameDuration = (currentState == LizardState.Walking) ? walkFrameDuration : attackFrameDuration;

            if (animTimer >= currentFrameDuration)
            {
                animTimer = 0;
                animFrame++;

                if (currentState == LizardState.Walking)
                {
                    if (animFrame >= 6) animFrame = 0;
                }
                else if (currentState == LizardState.Attacking)
                {
                    if (animFrame >= 5)
                    {
                        attackCooldown = MAX_ATTACK_COOLDOWN;
                        ChangeState(LizardState.Walking);
                    }
                }
            }
        }

        private void UpdateWalking(float dt, Stage stage, Vector2 playerPos)
        {
            velocity.X = speed * direction;
            Position.X += velocity.X * dt;

            Rectangle boundsX = new Rectangle((int)Position.X + 0, (int)Position.Y - 12, 10, 12);
            bool hitWall = false;
            foreach (Rectangle platform in stage.Platforms)
            {
                if (boundsX.Intersects(platform))
                {
                    hitWall = true;
                    break;
                }
            }

            if (hitWall)
            {
                Position.X -= velocity.X * dt;
                direction *= -1;
            }

            velocity.Y += gravity * dt;
            Position.Y += velocity.Y * dt;

            Rectangle boundsY = new Rectangle((int)Position.X + 11, (int)Position.Y - 12, 10, 12);
            bool isOnGround = false;

            foreach (Rectangle platform in stage.Platforms)
            {
                if (boundsY.Intersects(platform))
                {
                    if (velocity.Y > 0)
                    {
                        Position.Y = platform.Top;
                        velocity.Y = 0;
                        isOnGround = true;
                        break;
                    }
                }
            }

            if (isOnGround)
            {
                int checkX = direction > 0 ? (int)Position.X + 25 : (int)Position.X + 5;
                Point checkPoint = new Point(checkX, (int)Position.Y + 2);

                bool holeAhead = true;
                foreach (Rectangle platform in stage.Platforms)
                {
                    if (platform.Contains(checkPoint))
                    {
                        holeAhead = false;
                        break;
                    }
                }
                if (holeAhead)
                {
                    direction *= -1;
                }
            }

            attackCooldown -= dt;
            float distToPlayer = Vector2.Distance(Position, playerPos);
            bool isFacingPlayer = (playerPos.X > Position.X && direction > 0) || (playerPos.X < Position.X && direction < 0);

            if (distToPlayer < 60f && isFacingPlayer && attackCooldown <= 0)
                ChangeState(LizardState.Attacking);
        }

        private void ChangeState(LizardState newState)
        {
            currentState = newState;
            animFrame = 0;
            animTimer = 0;
        }

        private void UpdateAttacking(float dt)
        {
            velocity.X = 0; 
        }

        public void Draw(SpriteBatch sb, Texture2D walkTex, Texture2D tongueTex)
        {
            Texture2D currentTex = (currentState == LizardState.Walking) ? walkTex : tongueTex;
            int maxFrames = (currentState == LizardState.Walking) ? 6 : 5;


            int frameWidth = currentTex.Width / maxFrames;
            int frameHeight = currentTex.Height;

            Rectangle sourceRect = new Rectangle(animFrame * frameWidth, 0, frameWidth, frameHeight);

            Vector2 origin = new Vector2(frameWidth / 2f, frameHeight);
            Vector2 drawPos = new Vector2((int)Position.X + 16, (int)Position.Y);

            SpriteEffects flip = direction < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            sb.Draw(currentTex, drawPos, sourceRect, Color.White, 0f, origin, 1.0f, flip, 0f);

        }

        public bool IsHittingPlayer(Rectangle playerBounds)
        {
            if (currentState != LizardState.Attacking) return false;

            if (animFrame >= 2 && animFrame <= 4)
            {
                return TongueBounds.Intersects(playerBounds);
            }

            return false;
        }
    }
}