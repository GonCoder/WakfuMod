using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace WakfuMod.Content.Projectiles
{
    public class OcraExplosion : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 800; // Massive explosion
            Projectile.height = 400;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1; // Hit everything
            Projectile.timeLeft = 5; // Lasts very briefly, just for damage frame
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.alpha = 255; // Invisible
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            // Sound
            SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);

            // Visuals - Fire and Dust
            if (Main.netMode != NetmodeID.Server)
            {
                // Fire Dust
                for (int i = 0; i < 50; i++)
                {
                    int dustIndex = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0f, 0f, 100, default, 3f);
                    Main.dust[dustIndex].noGravity = true;
                    Main.dust[dustIndex].velocity *= 5f;
                }
                // Smoke/Debris
                for (int i = 0; i < 30; i++)
                {
                    int dustIndex = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, 0f, 0f, 100, default, 2f);
                    Main.dust[dustIndex].velocity *= 2f;
                }
                // Gore/Particles if desired (simple dust for now as requested)
            }
        }
    }
}
