using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.Audio;
using Terraria.GameContent;
using Microsoft.Xna.Framework.Graphics;

namespace WakfuMod.Content.Projectiles
{
    public class HipermagoFireExplosion : ModProjectile
    {
        // Animación de 2 frames
        private const int TOTAL_FRAMES = 2;
        private const int TICKS_PER_FRAME = 8;
        private const int TOTAL_DURATION = TOTAL_FRAMES * TICKS_PER_FRAME; // 16 ticks
        
        private const int BASE_SIZE = 80; // Tamaño base de hitbox

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = TOTAL_FRAMES;
        }

        public override void SetDefaults()
        {
            Projectile.width = BASE_SIZE;
            Projectile.height = BASE_SIZE;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1; // Golpea a todos los enemigos en el área
            Projectile.timeLeft = TOTAL_DURATION;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.light = 1.2f;
            Projectile.alpha = 0;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = TOTAL_DURATION; // Solo golpea una vez por enemigo
        }

        public override void AI()
        {
            // Primera vez: aplicar scale y ajustar hitbox
            if (Projectile.localAI[0] == 0)
            {
                Projectile.localAI[0] = 1;
                
                // ai[0] contiene el scale factor (default 1)
                float scaleFactor = Projectile.ai[0] > 0 ? Projectile.ai[0] : 1f;
                Projectile.scale = scaleFactor;
                
                // Ajustar hitbox proporcionalmente
                int newSize = (int)(BASE_SIZE * scaleFactor);
                Vector2 center = Projectile.Center;
                Projectile.width = newSize;
                Projectile.height = newSize;
                Projectile.Center = center; // Mantener el centro después de cambiar tamaño
                
                SoundEngine.PlaySound(SoundID.Item74, Projectile.Center); // Sonido de explosión
                
                // Explosión de partículas de fuego (más partículas si es más grande)
                int particleCount = (int)(30 * scaleFactor);
                for (int i = 0; i < particleCount; i++)
                {
                    Vector2 velocity = Main.rand.NextVector2Circular(6f * scaleFactor, 6f * scaleFactor);
                    int dust = Dust.NewDust(Projectile.Center, 0, 0, DustID.Torch, velocity.X, velocity.Y, 100, default, 2f * scaleFactor);
                    Main.dust[dust].noGravity = true;
                }
                
                // Humo
                int smokeCount = (int)(15 * scaleFactor);
                for (int i = 0; i < smokeCount; i++)
                {
                    Vector2 velocity = Main.rand.NextVector2Circular(3f * scaleFactor, 3f * scaleFactor);
                    int dust = Dust.NewDust(Projectile.Center, 0, 0, DustID.Smoke, velocity.X, velocity.Y - 1f, 150, default, 1.5f * scaleFactor);
                    Main.dust[dust].noGravity = true;
                }
            }

            // Animación de frames
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= TICKS_PER_FRAME)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
                if (Projectile.frame >= TOTAL_FRAMES)
                {
                    Projectile.frame = TOTAL_FRAMES - 1; // Quedarse en el último frame
                }
            }

            // Partículas continuas de fuego
            if (Main.rand.NextBool(2))
            {
                Vector2 offset = Main.rand.NextVector2Circular(Projectile.width / 2f, Projectile.height / 2f);
                int dust = Dust.NewDust(Projectile.Center + offset, 0, 0, DustID.Torch, 0, -2f, 100, default, 1.5f);
                Main.dust[dust].noGravity = true;
            }

            // Fade out al final
            if (Projectile.timeLeft < 8)
            {
                Projectile.alpha += 30;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Efecto de quemadura visual
            for (int i = 0; i < 8; i++)
            {
                int dust = Dust.NewDust(target.position, target.width, target.height, DustID.Torch, 0, 0, 100, default, 1.2f);
                Main.dust[dust].noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Rectangle frame = texture.Frame(1, TOTAL_FRAMES, 0, Projectile.frame);
            
            // Fix: Force origin to be the center of the actual 64x64 sprite content
            // ignoring any extra empty space in the texture file
            Vector2 origin = new Vector2(BASE_SIZE / 2f, BASE_SIZE / 2f);
            
            Color drawColor = Projectile.GetAlpha(lightColor);

            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                frame,
                drawColor,
                Projectile.rotation,
                origin,
                Projectile.scale,
                SpriteEffects.None,
                0
            );
            return false; 
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(255, 200, 100, 150) * ((255 - Projectile.alpha) / 255f);
        }
    }
}
