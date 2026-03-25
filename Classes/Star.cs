using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GalactaJumperMo.Classes
{
    public class Star
    {
        public Texture2D Texture;
        public Vector2 Position;
        public bool IsCollected;
        
        // เพิ่มตัวแปร Scale (1.0f คือขนาดปกติ, 0.5f คือลดครึ่งนึง)
        public float Scale = 0.05f; 

        public Rectangle Bounds
        {
            get
            {
                // เอา Scale มาคูณกับความกว้าง-ยาวของรูป เพื่อให้กรอบชนเล็กลงตามรูป
                return new Rectangle(
                    (int)Position.X, 
                    (int)Position.Y, 
                    (int)(Texture.Width * Scale), 
                    (int)(Texture.Height * Scale)
                );
            }
        }

        public Star(Texture2D texture, Vector2 position)
        {
            Texture = texture;
            Position = position;
            IsCollected = false;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (!IsCollected)
            {
                // เปลี่ยนวิธีวาดรูป เพื่อใส่ค่า Scale เข้าไป
                spriteBatch.Draw(
                    Texture, 
                    Position, 
                    null, 
                    Color.White, 
                    0f, 
                    Vector2.Zero, 
                    Scale, 
                    SpriteEffects.None, 
                    0f
                );
            }
        }
    }
}