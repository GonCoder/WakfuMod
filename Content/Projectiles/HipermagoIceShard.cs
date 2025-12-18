using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.Audio;

namespace WakfuMod.Content.Projectiles
{
    public class HipermagoIceShard : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            // Clonar comportamiento visual del FrostDaggerfish para tener sprite de pincho de hielo
            Main.projFrames[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            // Copiar propiedades del proyectil vanilla IceSpike/FrostDaggerfish
            Projectile.CloneDefaults(ProjectileID.FrostDaggerfish);
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1; // Muere al impactar
            Projectile.timeLeft = 180; // 3 segundos
            Projectile.tileCollide = true;
            Projectile.ignoreWater = false;
            Projectile.alpha = 50;
            Projectile.light = 0.4f;
            Projectile.coldDamage = true; // Daño de hielo
            Projectile.aiStyle = -1; // AI personalizada (sin gravedad)
        }

        public override void AI()
        {
            // Rotación hacia la dirección de movimiento
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            // SIN GRAVEDAD - vuela recto hacia el cursor
            // (no modificamos velocity.Y)

            // Trail de partículas de hielo
            if (Main.rand.NextBool(2))
            {
                int dust = Dust.NewDust(Projectile.Center - new Vector2(4, 4), 8, 8,
                    DustID.IceTorch, -Projectile.velocity.X * 0.2f, -Projectile.velocity.Y * 0.2f, 100, default, 0.8f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.3f;
            }

            // Partículas de escarcha
            if (Main.rand.NextBool(3))
            {
                int dust = Dust.NewDust(Projectile.Center - new Vector2(3, 3), 6, 6,
                    DustID.Snow, 0, 0, 100, default, 0.5f);
                Main.dust[dust].noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Aplicar debuff de hielo vanilla (Frostburn o Chilled)
            target.AddBuff(BuffID.Frostburn, 180); // 3 segundos de Frostburn
            target.AddBuff(BuffID.Chilled, 300);   // 5 segundos de Chilled (ralentiza)

            // Efectos visuales de impacto de hielo
            ImpactEffects();
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            ImpactEffects();
            return true;
        }

        private void ImpactEffects()
        {
            // Sonido de hielo rompiéndose
            SoundEngine.PlaySound(SoundID.Item27, Projectile.Center);

            // Explosión de partículas de hielo
            for (int i = 0; i < 15; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(4f, 4f);
                int dust = Dust.NewDust(Projectile.Center, 0, 0, DustID.IceTorch, velocity.X, velocity.Y, 100, default, 1.3f);
                Main.dust[dust].noGravity = true;
            }

            // Copos de nieve
            for (int i = 0; i < 10; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(3f, 3f);
                int dust = Dust.NewDust(Projectile.Center, 0, 0, DustID.Snow, velocity.X, velocity.Y - 1f, 100, default, 1f);
                Main.dust[dust].noGravity = false;
            }

            // Fragmentos de hielo (gore vanilla)
            for (int i = 0; i < 2; i++)
            {
                Gore.NewGore(Projectile.GetSource_Death(), Projectile.Center,
                    Main.rand.NextVector2Circular(2f, 2f), GoreID.Smoke1);
            }
        }

        public override void OnKill(int timeLeft)
        {
            // Más partículas al morir
            for (int i = 0; i < 8; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height,
                    DustID.IceTorch, 0, 0, 100, default, 0.8f);
                Main.dust[dust].noGravity = true;
            }
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(180, 220, 255, 150);
        }
    }
}
