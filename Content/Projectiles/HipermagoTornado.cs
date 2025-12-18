using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.Audio;

namespace WakfuMod.Content.Projectiles
{
    public class HipermagoTornado : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 16;  // Mitad de tamaño
            Projectile.height = 24; // Mitad de tamaño
            Projectile.scale = 0.5f; // Escala visual mitad
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 5; // Puede golpear a 5 enemigos
            Projectile.timeLeft = 120; // 2 segundos
            Projectile.tileCollide = false; // Atraviesa bloques
            Projectile.ignoreWater = true;
            Projectile.alpha = 100;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20; // Puede golpear al mismo enemigo cada 20 ticks
            
            // Doble knockback (era 10, ahora 20)
            Projectile.knockBack = 20f;
        }

        public override void AI()
        {
            // Rotación del tornado
            Projectile.rotation += 0.3f;
            
            // Vuela recto en la dirección del ratón (sin gravedad ni oscilación)
            // La velocidad se mantiene constante
            Projectile.ai[0]++;

            // Partículas de viento/aire
            for (int i = 0; i < 3; i++)
            {
                Vector2 offset = Main.rand.NextVector2Circular(Projectile.width / 2f, Projectile.height / 2f);
                Vector2 dustVel = new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-3f, 0f));
                
                int dust = Dust.NewDust(Projectile.Center + offset, 0, 0, DustID.Cloud, dustVel.X, dustVel.Y, 150, default, 1.2f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = dustVel + Projectile.velocity * 0.2f;
            }

            // Partículas de hojas/viento
            if (Main.rand.NextBool(3))
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 
                    DustID.Grass, Projectile.velocity.X * 0.5f, Projectile.velocity.Y * 0.5f, 100, default, 0.8f);
                Main.dust[dust].noGravity = true;
            }

            // Sonido de viento ocasional
            if (Projectile.soundDelay <= 0)
            {
                Projectile.soundDelay = 30;
                SoundEngine.PlaySound(SoundID.Item7, Projectile.Center);
            }

            // Fade out al final
            if (Projectile.timeLeft < 30)
            {
                Projectile.alpha += 8;
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            // Doble knockback (era +5, ahora +10)
            modifiers.Knockback += 10f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Empujar al enemigo fuertemente en la dirección del proyectil
            Vector2 pushDirection = Projectile.velocity;
            pushDirection.Normalize();
            
            // Aplicar velocidad extra al NPC (doble empuje: era 8, ahora 16)
            if (!target.boss && target.knockBackResist > 0)
            {
                target.velocity += pushDirection * 16f * target.knockBackResist;
            }

            // Efecto visual de ráfaga de viento
            for (int i = 0; i < 10; i++)
            {
                Vector2 dustVel = pushDirection * Main.rand.NextFloat(3f, 6f);
                dustVel = dustVel.RotatedByRandom(0.5f);
                int dust = Dust.NewDust(target.Center, 0, 0, DustID.Cloud, dustVel.X, dustVel.Y, 100, default, 1.5f);
                Main.dust[dust].noGravity = true;
            }
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(200, 230, 255, 100) * ((255 - Projectile.alpha) / 255f);
        }

        public override void OnKill(int timeLeft)
        {
            // Dispersión final de viento
            for (int i = 0; i < 15; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(4f, 4f);
                int dust = Dust.NewDust(Projectile.Center, 0, 0, DustID.Cloud, velocity.X, velocity.Y, 100, default, 1f);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}
