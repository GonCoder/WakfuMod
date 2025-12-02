using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace WakfuMod.Content.Globals
{
    public class WakfuGlobalProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        // --- XELOR TIME SUSPENSION ---
        public bool xelorSlowed = false;
        public Vector2 xelorRewindPos = Vector2.Zero;
        public Vector2 xelorOriginalVelocity = Vector2.Zero;

        public override void PostAI(Projectile projectile)
        {
            if (xelorSlowed)
            {
                // projectile.velocity *= 0.96f; // YA NO SE APLICA CADA FRAME

                // Efecto visual opcional
                if (Main.rand.NextBool(5))
                {
                    Dust.NewDust(projectile.position, projectile.width, projectile.height, DustID.Electric, 0, 0, 150, Color.Purple, 0.5f);
                }
            }
        }
    }
}