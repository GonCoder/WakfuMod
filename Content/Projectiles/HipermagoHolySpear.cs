using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using WakfuMod.Content.Buffs;

namespace WakfuMod.Content.Projectiles
{
    public class HipermagoHolySpear : ModProjectile
    {
        // ai[0] = 0 viajando, 1 muriendo
        // ai[1] = timer de muerte
        
        private const int DEATH_ANIM_DURATION = 12; // 6 ticks por frame de muerte
        private const int LIGHT_DEBUFF_DURATION = 1200; // 20 segundos de debuff de luz

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 3; // 1 viaje (sin usar), 2 muerte (frames 1-2)
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 8;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            // Hitbox cuadrada para que funcione en cualquier dirección de rotación
            // Usamos el lado menor como base (37px)
            Projectile.width = 37;
            Projectile.height = 37;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Ranged; // Escala con daño a distancia
            Projectile.penetrate = 1;
            Projectile.timeLeft = 180; // 3 segundos
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.light = 0f; // Sin luz
            Projectile.alpha = 0;
            Projectile.scale = 0.7f; // 30% más pequeña
            
            // La Holy Spear ignora armadura
            Projectile.ArmorPenetration = 9999;
        }

        public override void AI()
        {
            bool isDying = Projectile.ai[0] == 1f;
            
            if (isDying)
            {
                Projectile.ai[1]++;
                // Frames 1-2 para muerte (6 ticks cada uno)
                Projectile.frame = 1 + ((int)Projectile.ai[1] / 6) % 2;
                
                if (Projectile.ai[1] >= DEATH_ANIM_DURATION)
                {
                    Projectile.Kill();
                }
                return;
            }
            
            // Frame 0 mientras viaja (no se dibuja, usamos trail)
            Projectile.frame = 0;
            
            // Rotación hacia la dirección de movimiento
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            StartDeathAnimation();
            return false;
        }

        private void StartDeathAnimation()
        {
            if (Projectile.ai[0] != 1f)
            {
                Projectile.ai[0] = 1f; // Modo muerte
                Projectile.ai[1] = 0; // Timer
                Projectile.frame = 1;
                Projectile.velocity = Vector2.Zero;
                Projectile.tileCollide = false;
                Projectile.penetrate = -1;
                Projectile.friendly = false;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Aplicar debuff de luz (anula armadura)
            target.AddBuff(ModContent.BuffType<HipermagoLightDebuff>(), LIGHT_DEBUFF_DURATION);
            
            StartDeathAnimation();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            bool isDying = Projectile.ai[0] == 1f;
            
            // Dibujar sprite centrado correctamente
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            int frameHeight = texture.Height / Main.projFrames[Projectile.type];
            Rectangle sourceRect = new Rectangle(0, Projectile.frame * frameHeight, texture.Width, frameHeight);
            // Centro real del sprite (0.5, 0.5)
            Vector2 origin = new Vector2(texture.Width * 0.5f, frameHeight * 0.5f);
            
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Color drawColor = Projectile.GetAlpha(lightColor);
            
            Main.EntitySpriteDraw(texture, drawPos, sourceRect, drawColor, Projectile.rotation,
                origin, Projectile.scale, SpriteEffects.None, 0);
            
            // Sin trail ni efectos adicionales
            
            return false; // No dibujar el sprite default (ya lo dibujamos manualmente)
        }

        public override Color? GetAlpha(Color lightColor)
        {
            // Color normal del sprite, ligeramente brillante
            return Color.White;
        }

        public override void OnKill(int timeLeft)
        {
            // Sin efectos de partículas
        }
    }
}
