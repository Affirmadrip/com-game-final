using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GalactaJumperMo.Classes
{
    /// <summary>
    /// Represents a title text display in the game world.
    /// Parsed from the Titles entity layer in LDTK with Title field.
    /// </summary>
    public class TitleDisplay
    {
        public Vector2 Position { get; private set; }
        public string Title { get; private set; }
        
        private float animationTimer = 0f;
        
        public TitleDisplay(Vector2 position, string title)
        {
            Position = position;
            Title = title ?? "";
        }
        
        public void Update(GameTime gameTime)
        {
            animationTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
        }
        
        public void Draw(SpriteBatch spriteBatch, SpriteFont titleFont)
        {
            if (!string.IsNullOrEmpty(Title) && titleFont != null)
            {
                // Gentle floating animation
                float bobOffset = (float)System.Math.Sin(animationTimer * 2f) * 2f;
                
                Vector2 textSize = titleFont.MeasureString(Title);
                Vector2 drawPos = new Vector2(
                    Position.X - textSize.X / 2,
                    Position.Y - textSize.Y / 2 + bobOffset
                );
                
                // Draw shadow
                spriteBatch.DrawString(
                    titleFont,
                    Title,
                    drawPos + new Vector2(2, 2),
                    Color.Black * 0.5f,
                    0f,
                    Vector2.Zero,
                    0.5f,
                    SpriteEffects.None,
                    0f
                );
                
                // Draw main text
                spriteBatch.DrawString(
                    titleFont,
                    Title,
                    drawPos,
                    Color.White,
                    0f,
                    Vector2.Zero,
                    0.5f,
                    SpriteEffects.None,
                    0f
                );
            }
        }
    }
}
