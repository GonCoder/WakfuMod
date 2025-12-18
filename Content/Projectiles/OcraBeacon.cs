using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace WakfuMod.Content.Projectiles
{
    public class OcraBeacon : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 26; // Adjusted size
            Projectile.height = 86;
            Projectile.friendly = false; // Does not deal contact damage
            Projectile.hostile = false;
            Projectile.tileCollide = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 3600; // Lasts 1 minute? Or unlimited until exploded? Let's say 2 mins for now
            Projectile.ignoreWater = false;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            return false; // Don't die on tile collide, just sit there
        }

        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
        {
            fallThrough = false;
            return true;
        }

        public override void AI()
        {
            Projectile.velocity.X = 0;
            Projectile.velocity.Y += 0.4f; // Gravity
            // Simple visual effect to show it's active
            if (Main.rand.NextBool(10))
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, Terraria.ID.DustID.Electric);
            }
        }
    }
}
