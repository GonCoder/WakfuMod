using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Audio;

namespace WakfuMod.Content.Projectiles
{
    public class HipermagoSteamExplosion : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 2; // 2 frames como la explosión original
        }
        
        public override void SetDefaults()
        {
            Projectile.width = 80;
            Projectile.height = 80;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1; // Atraviesa enemigos
            Projectile.timeLeft = 30; // Corta duración (explosión rápida)
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.alpha = 50;
            Projectile.light = 0.5f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1; // Solo golpea una vez por explosión
        }

        public override void AI()
        {
            // ai[0] = delay antes de aparecer
            if (Projectile.ai[0] > 0)
            {
                Projectile.ai[0]--;
                Projectile.alpha = 255; // Invisible durante delay
                return;
            }
            
            // Aparecer con sonido
            if (Projectile.alpha == 255)
            {
                Projectile.alpha = 50;
                SoundEngine.PlaySound(SoundID.Item13 with { Volume = 0.5f }, Projectile.Center);
            }
            
            // Animación
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 8) // Más lento para 2 frames
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
                if (Projectile.frame >= Main.projFrames[Projectile.type])
                {
                    Projectile.Kill();
                }
            }
            
            // Efecto de vapor
            if (Main.rand.NextBool(2))
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 
                    DustID.Cloud, Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-3f, 0f), 200, default, 1.5f);
                Main.dust[dust].noGravity = true;
            }
            
            // Burbujas de agua caliente
            if (Main.rand.NextBool(4))
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 
                    DustID.BubbleBlock, Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-2f, 0f), 100, default, 0.8f);
                Main.dust[dust].noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Aplicar debuff de ceguera (10 segundos = 600 ticks)
            target.AddBuff(ModContent.BuffType<Buffs.BlindedDebuff>(), 600);
            
            // Efecto visual de vapor
            for (int i = 0; i < 8; i++)
            {
                int dust = Dust.NewDust(target.position, target.width, target.height, 
                    DustID.Cloud, Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-3f, 0f), 150, default, 1.2f);
                Main.dust[dust].noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // No dibujar si está en delay
            if (Projectile.alpha == 255)
                return false;
                
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            int frameHeight = texture.Height / Main.projFrames[Projectile.type];
            Rectangle sourceRect = new Rectangle(0, Projectile.frame * frameHeight, texture.Width, frameHeight);
            Vector2 origin = new Vector2(texture.Width * 0.5f, frameHeight * 0.5f);
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            
            // Color blanco/gris para simular vapor (cambio de HUE a blanco)
            Color steamColor = new Color(230, 240, 255, 180);
            
            Main.EntitySpriteDraw(texture, drawPos, sourceRect, steamColor, Projectile.rotation,
                origin, Projectile.scale, SpriteEffects.None, 0);
            
            // Capa extra de brillo
            Color glowColor = new Color(255, 255, 255, 100);
            Main.EntitySpriteDraw(texture, drawPos, sourceRect, glowColor, Projectile.rotation,
                origin, Projectile.scale * 1.1f, SpriteEffects.None, 0);
            
            return false;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(230, 240, 255, 180);
        }
    }
}
