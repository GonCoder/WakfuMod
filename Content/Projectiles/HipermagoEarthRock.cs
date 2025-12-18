using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.Audio;

namespace WakfuMod.Content.Projectiles
{
    public class HipermagoEarthRock : ModProjectile
    {
        public override void SetDefaults()
        {
            // Usar sprite de vanilla (boulder pequeño)
            Projectile.CloneDefaults(ProjectileID.BoulderStaffOfEarth);
            
            // Custom adjustments after clone
            Projectile.width = 48;
            Projectile.height = 48;
            Projectile.aiStyle = -1; // Pero con AI personalizada
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 300;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.alpha = 0;
        }

        public override void AI()
        {
            // Gravedad simple (cae como estrella)
            Projectile.velocity.Y += 0.3f;
            if (Projectile.velocity.Y > 16f)
                Projectile.velocity.Y = 16f;

            // Sin rotación - cae estático
            Projectile.rotation = 0f;

            // Trail de polvo de tierra
            if (Main.rand.NextBool(2))
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 
                    DustID.Dirt, 0, 0, 100, default, 1f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.3f;
            }

            // Partículas de piedra
            if (Main.rand.NextBool(3))
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 
                    DustID.Stone, -Projectile.velocity.X * 0.2f, -Projectile.velocity.Y * 0.2f, 100, default, 0.8f);
                Main.dust[dust].noGravity = true;
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            // Efectos de impacto
            ImpactEffects();
            return true; // Morir
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Aplicar debuff Dazed (mareo/stun breve)
            target.AddBuff(BuffID.Dazed, 120); // 2 segundos de mareo
            
            // Aplicar debuff Blinded (ceguera)
            target.AddBuff(ModContent.BuffType<Buffs.BlindedDebuff>(), 300); // 5 segundos
            
            // Efectos de impacto en NPC
            ImpactEffects();
        }

        private void ImpactEffects()
        {
            // Sonido de impacto rocoso
            SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);

            // Explosión de partículas de tierra/piedra
            for (int i = 0; i < 20; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(4f, 4f);
                int dust = Dust.NewDust(Projectile.Center, 0, 0, DustID.Stone, velocity.X, velocity.Y, 100, default, 1.5f);
                Main.dust[dust].noGravity = false;
            }

            // Polvo de tierra
            for (int i = 0; i < 15; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(3f, 3f);
                int dust = Dust.NewDust(Projectile.Center, 0, 0, DustID.Dirt, velocity.X, velocity.Y - 1f, 100, default, 1.2f);
                Main.dust[dust].noGravity = false;
            }

            // Gore de piedras pequeñas (efecto vanilla)
            for (int i = 0; i < 3; i++)
            {
                Gore.NewGore(Projectile.GetSource_Death(), Projectile.Center, 
                    Main.rand.NextVector2Circular(2f, 2f), GoreID.Smoke1 + Main.rand.Next(3));
            }
        }

        public override void OnKill(int timeLeft)
        {
            // Más partículas al morir
            for (int i = 0; i < 10; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 
                    DustID.Stone, 0, 0, 100, default, 1f);
            }
        }
    }
}
