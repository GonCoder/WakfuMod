using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Audio;
using System;

namespace WakfuMod.Content.Projectiles
{
    public class HipermagoWindTornado : ModProjectile
    {
        private const float LAUNCH_RANGE = 200f; // Rango de efecto
        private const float LAUNCH_STRENGTH = 18f; // Fuerza de lanzamiento vertical
        
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 16; // 16 frames de animación
        }
        
        public override void SetDefaults()
        {
            Projectile.width = 60;
            Projectile.height = 240; // Alto para el tornado vertical
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.damage = 50;
            Projectile.penetrate = -1; // No muere por impacto
            Projectile.timeLeft = 120; // 2 segundos
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.alpha = 50;
            Projectile.light = 0.8f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30; // Puede golpear cada medio segundo
        }

        public override void AI()
        {
            // El proyectil no se mueve
            Projectile.velocity = Vector2.Zero;
            
            // Efectos visuales del tornado de viento
            SpawnWindTornadoEffects();
            
            // Lanzar enemigos hacia arriba
            LaunchEnemies();
            
            // Animación de 16 frames (más rápida para viento)
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 3) // Cambiar frame cada 3 ticks (más rápido)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
                if (Projectile.frame >= 16)
                    Projectile.frame = 0;
            }
        }

        private void SpawnWindTornadoEffects()
        {
            float time = Main.GameUpdateCount * 0.15f; // Más rápido que el de fuego
            
            for (int i = 0; i < 4; i++)
            {
                // Espiral de viento
                float angle = time + (i * MathHelper.TwoPi / 4f);
                float yOffset = Main.rand.NextFloat(0, Projectile.height);
                float radius = 20f + (yOffset / Projectile.height) * 35f;
                
                Vector2 offset = new Vector2(
                    (float)Math.Cos(angle) * radius,
                    -yOffset
                );
                
                Vector2 dustPos = Projectile.Bottom + offset;
                
                // Partículas de viento/nubes blancas
                int dustType = Main.rand.NextBool(2) ? DustID.Cloud : DustID.SnowBlock;
                int dust = Dust.NewDust(dustPos, 1, 1, dustType, 0, -5f, 100, default, 1.2f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = new Vector2(
                    (float)Math.Cos(angle + MathHelper.PiOver2) * 3f,
                    -4f // Movimiento hacia arriba
                );
            }
            
            // Partículas en la base
            if (Main.rand.NextBool(2))
            {
                float xOffset = Main.rand.NextFloat(-30f, 30f);
                int dust = Dust.NewDust(Projectile.Bottom + new Vector2(xOffset, 0), 1, 1, DustID.Cloud, 0, -6f, 100, default, 1.5f);
                Main.dust[dust].noGravity = true;
            }
            
            // Hojas/debris siendo levantados
            if (Main.rand.NextBool(6))
            {
                float xOffset = Main.rand.NextFloat(-25f, 25f);
                int dust = Dust.NewDust(Projectile.Bottom + new Vector2(xOffset, 0), 1, 1, DustID.Grass, 
                    Main.rand.NextFloat(-1f, 1f), -8f, 100, default, 1f);
                Main.dust[dust].noGravity = true;
            }
        }

        private void LaunchEnemies()
        {
            Vector2 tornadoCenter = Projectile.Center;
            
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && !npc.friendly && npc.CanBeChasedBy() && !npc.boss)
                {
                    float distance = Vector2.Distance(tornadoCenter, npc.Center);
                    
                    if (distance < LAUNCH_RANGE)
                    {
                        // Lanzar estrictamente hacia ARRIBA (vertical)
                        npc.velocity.Y = -LAUNCH_STRENGTH;
                        // Centrar horizontalmente hacia el tornado
                        float pullX = (tornadoCenter.X - npc.Center.X) * 0.1f;
                        npc.velocity.X += pullX;
                        
                        // Efecto visual de ser lanzado
                        if (Main.rand.NextBool(5))
                        {
                            int dust = Dust.NewDust(npc.position, npc.width, npc.height, DustID.Cloud, 
                                0, -3f, 100, default, 1f);
                            Main.dust[dust].noGravity = true;
                        }
                    }
                }
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Knockback vertical hacia arriba
            target.velocity.Y = -LAUNCH_STRENGTH;
            target.velocity.X *= 0.3f; // Reducir movimiento horizontal
            
            // Efecto visual de impacto
            for (int i = 0; i < 8; i++)
            {
                int dust = Dust.NewDust(target.position, target.width, target.height, DustID.Cloud, 
                    Main.rand.NextFloat(-2f, 2f), -5f, 100, default, 1.2f);
                Main.dust[dust].noGravity = true;
            }
            
            SoundEngine.PlaySound(SoundID.Item66 with { Volume = 0.5f }, target.Center);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            // Sin knockback horizontal del sistema vanilla
            modifiers.HitDirectionOverride = 0;
        }

        public override void OnKill(int timeLeft)
        {
            // Sonido de viento dispersándose
            SoundEngine.PlaySound(SoundID.Item66, Projectile.Center);
            
            // Explosión final de viento
            for (int i = 0; i < 30; i++)
            {
                float yOffset = Main.rand.NextFloat(0, Projectile.height);
                Vector2 dustPos = Projectile.Bottom + new Vector2(Main.rand.NextFloat(-30f, 30f), -yOffset);
                Vector2 velocity = new Vector2(Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-8f, -3f));
                int dust = Dust.NewDust(dustPos, 0, 0, DustID.Cloud, velocity.X, velocity.Y, 100, default, 1.3f);
                Main.dust[dust].noGravity = true;
            }
            
            // Hojas dispersándose
            for (int i = 0; i < 15; i++)
            {
                float yOffset = Main.rand.NextFloat(0, Projectile.height * 0.5f);
                Vector2 dustPos = Projectile.Bottom + new Vector2(Main.rand.NextFloat(-30f, 30f), -yOffset);
                int dust = Dust.NewDust(dustPos, 0, 0, DustID.Grass, 
                    Main.rand.NextFloat(-4f, 4f), Main.rand.NextFloat(-6f, -2f), 100, default, 1f);
                Main.dust[dust].noGravity = false;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Dibujar el sprite animado del tornado
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            int frameHeight = texture.Height / Main.projFrames[Projectile.type];
            Rectangle sourceRect = new Rectangle(0, Projectile.frame * frameHeight, texture.Width, frameHeight);
            Vector2 origin = new Vector2(texture.Width * 0.5f, frameHeight);
            
            Vector2 drawPos = new Vector2(Projectile.Center.X, Projectile.position.Y + Projectile.height) - Main.screenPosition;
            
            // Color blanco/celeste para tornado de viento
            Color drawColor = new Color(220, 240, 255, 150);
            
            Main.EntitySpriteDraw(texture, drawPos, sourceRect, drawColor, 0f,
                origin, Projectile.scale, SpriteEffects.None, 0);
            
            // Dibujar una capa extra con brillo
            Color glowColor = new Color(255, 255, 255, 80);
            Main.EntitySpriteDraw(texture, drawPos, sourceRect, glowColor, 0f,
                origin, Projectile.scale * 1.05f, SpriteEffects.None, 0);
            
            return false;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            // Color blanco brillante
            return new Color(220, 240, 255, 150);
        }
    }
}
