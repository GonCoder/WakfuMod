using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Audio;
using System;

namespace WakfuMod.Content.Projectiles
{
    public class HipermagoFireTornado : ModProjectile
    {
        private int damageTimer = 0;
        private const int DAMAGE_INTERVAL = 60; // 1 segundo
        private const float PULL_RANGE = 200f; // Rango de atracción
        private const float PULL_STRENGTH = 4f; // Fuerza de atracción
        
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 16; // 16 frames de animación
        }
        
        public override void SetDefaults()
        {
            Projectile.width = 60;
            Projectile.height = 240; // Alto para el tornado vertical (igual que el sprite)
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1; // No muere por impacto
            Projectile.timeLeft = 300; // 5 segundos
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.alpha = 50;
            Projectile.light = 1f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1; // Control manual del daño
        }

        public override void AI()
        {
            // El proyectil no se mueve
            Projectile.velocity = Vector2.Zero;
            
            // Efectos visuales del tornado de fuego
            SpawnFireTornadoEffects();
            
            // Atraer enemigos al centro
            PullEnemies();
            
            // Daño cada segundo
            damageTimer++;
            if (damageTimer >= DAMAGE_INTERVAL)
            {
                damageTimer = 0;
                DamageNearbyEnemies();
            }
            
            // Animación de 16 frames
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 4) // Cambiar frame cada 4 ticks
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
                if (Projectile.frame >= 16)
                    Projectile.frame = 0;
            }
        }

        private void SpawnFireTornadoEffects()
        {
            // Partículas de fuego giratorias
            float time = Main.GameUpdateCount * 0.1f;
            
            for (int i = 0; i < 3; i++)
            {
                // Espiral de fuego
                float angle = time + (i * MathHelper.TwoPi / 3f);
                float yOffset = Main.rand.NextFloat(0, Projectile.height);
                float radius = 20f + (yOffset / Projectile.height) * 30f; // Más ancho arriba
                
                Vector2 offset = new Vector2(
                    (float)Math.Cos(angle) * radius,
                    -yOffset
                );
                
                Vector2 dustPos = Projectile.Bottom + offset;
                
                int dustType = Main.rand.NextBool(3) ? DustID.Torch : DustID.Smoke;
                int dust = Dust.NewDust(dustPos, 1, 1, dustType, 0, -3f, 100, default, 1.5f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = new Vector2(
                    (float)Math.Cos(angle + MathHelper.PiOver2) * 2f,
                    -2f
                );
            }
            
            // Partículas en la base
            if (Main.rand.NextBool(3))
            {
                float xOffset = Main.rand.NextFloat(-30f, 30f);
                int dust = Dust.NewDust(Projectile.Bottom + new Vector2(xOffset, 0), 1, 1, DustID.Torch, 0, -4f, 100, default, 2f);
                Main.dust[dust].noGravity = true;
            }
            
            // Chispas ocasionales
            if (Main.rand.NextBool(8))
            {
                float xOffset = Main.rand.NextFloat(-25f, 25f);
                float yOffset = Main.rand.NextFloat(0, Projectile.height);
                Vector2 sparkPos = Projectile.Bottom + new Vector2(xOffset, -yOffset);
                int dust = Dust.NewDust(sparkPos, 1, 1, DustID.FireworkFountain_Red, 
                    Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-3f, -1f), 100, default, 0.8f);
                Main.dust[dust].noGravity = true;
            }
        }

        private void PullEnemies()
        {
            Vector2 tornadoCenter = Projectile.Center;
            
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && !npc.friendly && npc.CanBeChasedBy() && !npc.boss)
                {
                    float distance = Vector2.Distance(tornadoCenter, npc.Center);
                    
                    if (distance < PULL_RANGE && distance > 10f)
                    {
                        // Calcular dirección hacia el centro del tornado
                        Vector2 pullDirection = tornadoCenter - npc.Center;
                        pullDirection.Normalize();
                        
                        // Fuerza de atracción inversamente proporcional a la distancia
                        float pullForce = PULL_STRENGTH * (1f - distance / PULL_RANGE);
                        
                        // Aplicar atracción
                        npc.velocity += pullDirection * pullForce;
                        
                        // Limitar velocidad máxima
                        if (npc.velocity.Length() > 10f)
                        {
                            npc.velocity.Normalize();
                            npc.velocity *= 10f;
                        }
                        
                        // Efecto visual de ser atraído
                        if (Main.rand.NextBool(10))
                        {
                            int dust = Dust.NewDust(npc.position, npc.width, npc.height, DustID.Torch, 
                                pullDirection.X * 2f, pullDirection.Y * 2f, 100, default, 1f);
                            Main.dust[dust].noGravity = true;
                        }
                    }
                }
            }
        }

        private void DamageNearbyEnemies()
        {
            if (Main.myPlayer != Projectile.owner)
                return;
                
            Vector2 tornadoCenter = Projectile.Center;
            
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && !npc.friendly && npc.CanBeChasedBy())
                {
                    float distance = Vector2.Distance(tornadoCenter, npc.Center);
                    
                    if (distance < PULL_RANGE * 0.8f) // Un poco menos que el rango de atracción
                    {
                        // Aplicar 10 de daño
                        npc.SimpleStrikeNPC(10, 0, false, 0f, DamageClass.Ranged, false, Main.player[Projectile.owner].luck);
                        
                        // Aplicar debuff de quemado (3 segundos)
                        npc.AddBuff(BuffID.OnFire, 180);
                        
                        // Efecto visual de quemadura
                        for (int j = 0; j < 5; j++)
                        {
                            int dust = Dust.NewDust(npc.position, npc.width, npc.height, DustID.Torch, 
                                Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-2f, 2f), 100, default, 1.2f);
                            Main.dust[dust].noGravity = true;
                        }
                    }
                }
            }
        }

        public override bool? CanHitNPC(NPC target)
        {
            // No hacer daño directo, usamos DamageNearbyEnemies
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            // Sonido de extinción
            SoundEngine.PlaySound(SoundID.Item74, Projectile.Center);
            
            // Explosión final de fuego
            for (int i = 0; i < 40; i++)
            {
                float yOffset = Main.rand.NextFloat(0, Projectile.height);
                Vector2 dustPos = Projectile.Bottom + new Vector2(Main.rand.NextFloat(-30f, 30f), -yOffset);
                Vector2 velocity = Main.rand.NextVector2Circular(5f, 5f);
                int dust = Dust.NewDust(dustPos, 0, 0, DustID.Torch, velocity.X, velocity.Y, 100, default, 1.5f);
                Main.dust[dust].noGravity = true;
            }
            
            // Humo
            for (int i = 0; i < 20; i++)
            {
                float yOffset = Main.rand.NextFloat(0, Projectile.height);
                Vector2 dustPos = Projectile.Bottom + new Vector2(Main.rand.NextFloat(-30f, 30f), -yOffset);
                int dust = Dust.NewDust(dustPos, 0, 0, DustID.Smoke, 0, -2f, 100, default, 1.2f);
                Main.dust[dust].noGravity = false;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Dibujar el sprite animado del tornado
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            int frameHeight = texture.Height / Main.projFrames[Projectile.type];
            Rectangle sourceRect = new Rectangle(0, Projectile.frame * frameHeight, texture.Width, frameHeight);
            // Origen en el centro-abajo del frame (pie del tornado)
            Vector2 origin = new Vector2(texture.Width * 0.5f, frameHeight);
            
            // Dibujar desde la posición Bottom del proyectil (pie de la hitbox)
            Vector2 drawPos = new Vector2(Projectile.Center.X, Projectile.position.Y + Projectile.height) - Main.screenPosition;
            Color drawColor = Projectile.GetAlpha(lightColor);
            
            Main.EntitySpriteDraw(texture, drawPos, sourceRect, drawColor, 0f,
                origin, Projectile.scale, SpriteEffects.None, 0);
            
            return false;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(255, 150, 50, 100);
        }
    }
}
