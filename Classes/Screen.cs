using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GalactaJumperMo.Classes
{
    public abstract class Screen
    {
        protected static readonly Color BgBase    = new Color( 4,  6, 16);
        protected static readonly Color TextWhite = new Color(242, 240, 252);
        protected static readonly Color TextDim   = new Color(140, 138, 158);

        public abstract void Update(GameTime gt, int sw, int sh);
        public abstract void Draw(SpriteBatch sb, int sw, int sh);

        protected static void DrawBg(SpriteBatch sb, Texture2D pixel, int sw, int sh)
        {
            sb.Draw(pixel, new Rectangle(0, 0, sw, sh), BgBase);
        }

        protected static void DrawVignette(SpriteBatch sb, Texture2D pixel, int sw, int sh, byte maxAlpha = 200)
        {
            int rim   = Math.Min(sw, sh) / 4;
            int steps = 20;
            int layer = Math.Max(1, rim / steps);
            for (int i = 0; i < steps; i++)
            {
                float t = 1f - (float)i / steps;
                byte  a = (byte)(t * t * maxAlpha);
                Color c = new Color((byte)0, (byte)0, (byte)0, a);
                int   p = i * layer;
                sb.Draw(pixel, new Rectangle(0,             p,               sw, layer), c);
                sb.Draw(pixel, new Rectangle(0,             sh - p - layer,  sw, layer), c);
                sb.Draw(pixel, new Rectangle(p,             0,       layer,  sh),        c);
                sb.Draw(pixel, new Rectangle(sw - p - layer, 0,      layer,  sh),        c);
            }
        }
    }
}
