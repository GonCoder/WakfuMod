using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Audio;
using System;

namespace WakfuMod.Content.Projectiles
{
    public class HipermagoDebrisWhirl : ModProjectile
    {
        private int damageTimer = 0;
        private const int DAMAGE_INTERVAL = 30; // Medio segundo
        private const float ORBIT_RADIUS = 80f; // Radio de órbita
        private const float ORBIT_SPEED = 0.08f; // Velocidad de rotación
        
        // ai[0] = ángulo de órbita inicial (para que cada roca tenga un offset diferente)
        
        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1; // No muere por impacto
            Projectile.timeLeft = 360; // 6 segundos
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.alpha = 0;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30; // Cada medio segundo puede dañar al mismo NPC
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            
            // Si el dueño no existe o está muerto, morir
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }
            
            // Calcular posición orbital
            float angle = Projectile.ai[0] + (Main.GameUpdateCount * ORBIT_SPEED);
            Vector2 orbitOffset = new Vector2(
                (float)Math.Cos(angle) * ORBIT_RADIUS,
                (float)Math.Sin(angle) * ORBIT_RADIUS * 0.6f // Elipse más plana
            );
            
            // Posicionar alrededor del jugador
            Projectile.Center = owner.Center + orbitOffset;
            
            // Rotación de la roca
            Projectile.rotation += 0.15f;
            
            // Efectos de viento vanilla
            if (Main.rand.NextBool(3))
            {
                // Partículas de viento/polvo
                int dustType = Main.rand.NextBool() ? DustID.Cloud : DustID.Dirt;
                Vector2 dustVel = new Vector2(
                    (float)Math.Cos(angle + MathHelper.PiOver2) * 2f,
                    (float)Math.Sin(angle + MathHelper.PiOver2) * 2f
                );
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 
                    dustType, dustVel.X, dustVel.Y, 100, default, 0.8f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.5f;
            }
            
            // Partículas de piedra pequeñas
            if (Main.rand.NextBool(6))
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 
                    DustID.Stone, Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-1f, 1f), 100, default, 0.6f);
                Main.dust[dust].noGravity = true;
            }
            
            // Efecto de viento (líneas de aire)
            if (Main.rand.NextBool(8))
            {
                Vector2 windPos = owner.Center + Main.rand.NextVector2Circular(ORBIT_RADIUS * 1.2f, ORBIT_RADIUS * 0.8f);
                int dust = Dust.NewDust(windPos, 1, 1, DustID.Cloud, 
                    (float)Math.Cos(angle) * 3f, (float)Math.Sin(angle) * 2f, 150, default, 0.5f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].fadeIn = 1.2f;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Efecto de impacto
            for (int i = 0; i < 5; i++)
            {
                int dust = Dust.NewDust(target.position, target.width, target.height, 
                    DustID.Stone, Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-2f, 2f), 100, default, 0.8f);
                Main.dust[dust].noGravity = true;
            }
            
            // Sonido de impacto
            SoundEngine.PlaySound(SoundID.Dig, target.Center);
        }

        public override void OnKill(int timeLeft)
        {
            // Efecto de dispersión al terminar
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.5f }, Projectile.Center);
            
            for (int i = 0; i < 10; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(4f, 4f);
                int dust = Dust.NewDust(Projectile.Center, 0, 0, DustID.Stone, velocity.X, velocity.Y, 100, default, 1f);
                Main.dust[dust].noGravity = false;
            }
            
            for (int i = 0; i < 5; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(3f, 3f);
                int dust = Dust.NewDust(Projectile.Center, 0, 0, DustID.Cloud, velocity.X, velocity.Y, 100, default, 0.8f);
                Main.dust[dust].noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            Vector2 origin = texture.Size() / 2f;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            
            // Dibujar la roca con un poco de transparencia para efecto de velocidad
            Color drawColor = lightColor * 0.9f;
            
            Main.EntitySpriteDraw(texture, drawPos, null, drawColor, Projectile.rotation,
                origin, Projectile.scale, SpriteEffects.None, 0);
            
            // Dibujar un "trail" fantasma detrás
            float trailAngle = Projectile.ai[0] + (Main.GameUpdateCount * ORBIT_SPEED) - 0.3f;
            Vector2 trailOffset = new Vector2(
                (float)Math.Cos(trailAngle) * ORBIT_RADIUS,
                (float)Math.Sin(trailAngle) * ORBIT_RADIUS * 0.6f
            );
            Vector2 trailPos = Main.player[Projectile.owner].Center + trailOffset - Main.screenPosition;
            Main.EntitySpriteDraw(texture, trailPos, null, drawColor * 0.3f, Projectile.rotation - 0.3f,
                origin, Projectile.scale * 0.8f, SpriteEffects.None, 0);
            
            return false;
        }
    }
}
