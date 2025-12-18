using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Audio;

namespace WakfuMod.Content.Projectiles
{
    public class HipermagoBubble : ModProjectile
    {
        private NPC trappedNPC = null;
        private int damageTimer = 0;
        
        public override void SetDefaults()
        {
            Projectile.width = 80;
            Projectile.height = 80;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1; // No muere por impacto
            Projectile.timeLeft = 600; // 10 segundos
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.alpha = 100; // Semi transparente
            Projectile.light = 0.3f;
            Projectile.scale = 1.5f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1; // Control manual del daño
        }

        public override void AI()
        {
            // Efecto de flotación suave
            Projectile.velocity = Vector2.Zero;
            
            // Burbujas pequeñas de agua alrededor
            if (Main.rand.NextBool(8))
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                float dist = Main.rand.NextFloat(30f, 40f) * Projectile.scale;
                Vector2 dustPos = Projectile.Center + new Vector2((float)System.Math.Cos(angle), (float)System.Math.Sin(angle)) * dist;
                int dust = Dust.NewDust(dustPos, 1, 1, DustID.BubbleBlock, 0, -1f, 100, default, 0.8f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.3f;
            }

            // Buscar NPC para atrapar
            if (trappedNPC == null || !trappedNPC.active)
            {
                trappedNPC = null;
                
                float bubbleRadius = Projectile.width * Projectile.scale * 0.5f;
                
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];
                    if (npc.active && !npc.friendly && npc.CanBeChasedBy())
                    {
                        float distance = Vector2.Distance(Projectile.Center, npc.Center);
                        if (distance < bubbleRadius + npc.width * 0.5f)
                        {
                            trappedNPC = npc;
                            SoundEngine.PlaySound(SoundID.Item85, Projectile.Center); // Sonido de agua
                            break;
                        }
                    }
                }
            }

            // Si hay un NPC atrapado
            if (trappedNPC != null && trappedNPC.active)
            {
                // Mantenerlo en el centro de la burbuja
                trappedNPC.velocity = Vector2.Zero;
                trappedNPC.position = Projectile.Center - new Vector2(trappedNPC.width * 0.5f, trappedNPC.height * 0.5f);
                
                // Daño cada medio segundo (30 ticks)
                damageTimer++;
                if (damageTimer >= 30)
                {
                    damageTimer = 0;
                    
                    // Aplicar 1 de daño sin knockback
                    if (Main.myPlayer == Projectile.owner)
                    {
                        trappedNPC.SimpleStrikeNPC(1, 0, false, 0f, DamageClass.Ranged, false, Main.player[Projectile.owner].luck);
                    }
                    
                    // Burbujas de "ahogamiento"
                    for (int i = 0; i < 5; i++)
                    {
                        Vector2 bubblePos = trappedNPC.Center + Main.rand.NextVector2Circular(10f, 10f);
                        int dust = Dust.NewDust(bubblePos, 1, 1, DustID.BubbleBlock, 0, -2f, 100, default, 1f);
                        Main.dust[dust].noGravity = true;
                    }
                }
                
                // Efecto visual de agua dentro de la burbuja
                if (Main.rand.NextBool(4))
                {
                    Vector2 waterPos = trappedNPC.Center + Main.rand.NextVector2Circular(15f, 15f);
                    int dust = Dust.NewDust(waterPos, 1, 1, DustID.Water, 0, 0, 150, default, 0.6f);
                    Main.dust[dust].noGravity = true;
                }
            }
            
            // Rotación suave
            Projectile.rotation += 0.01f;
        }

        public override bool? CanHitNPC(NPC target)
        {
            // No hacer daño directo, solo atrapar
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            // Liberar NPC si está atrapado
            if (trappedNPC != null && trappedNPC.active)
            {
                // Pequeño knockback hacia arriba al liberarse
                trappedNPC.velocity.Y = -3f;
            }
            
            // Sonido de burbuja explotando
            SoundEngine.PlaySound(SoundID.Item54, Projectile.Center);
            
            // Explosión de burbujas de agua
            for (int i = 0; i < 20; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(5f, 5f);
                int dust = Dust.NewDust(Projectile.Center, 0, 0, DustID.BubbleBlock, velocity.X, velocity.Y, 100, default, 1.2f);
                Main.dust[dust].noGravity = true;
            }
            
            // Gotas de agua
            for (int i = 0; i < 15; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(4f, 4f);
                int dust = Dust.NewDust(Projectile.Center, 0, 0, DustID.Water, velocity.X, velocity.Y, 100, default, 1f);
                Main.dust[dust].noGravity = false;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            Vector2 origin = texture.Size() / 2f;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            
            // Color azul agua con transparencia
            Color bubbleColor = new Color(100, 180, 255, 100);
            
            // Dibujar burbuja con efecto de pulsación
            float pulse = 1f + (float)System.Math.Sin(Main.GameUpdateCount * 0.1f) * 0.05f;
            float drawScale = Projectile.scale * pulse;
            
            Main.EntitySpriteDraw(texture, drawPos, null, bubbleColor, Projectile.rotation,
                origin, drawScale, SpriteEffects.None, 0);
            
            // Dibujar brillo interno
            Color innerGlow = new Color(150, 220, 255, 50);
            Main.EntitySpriteDraw(texture, drawPos, null, innerGlow, -Projectile.rotation * 0.5f,
                origin, drawScale * 0.7f, SpriteEffects.None, 0);
            
            return false;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(100, 180, 255, 100);
        }
    }
}
