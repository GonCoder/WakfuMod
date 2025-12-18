using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Audio;

namespace WakfuMod.Content.Projectiles
{
    public class HipermagoMeteor : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1; // Muere al impactar
            Projectile.timeLeft = 300; // 5 segundos máximo
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.alpha = 0;
            Projectile.light = 0.8f;
            Projectile.scale = 2f; // Doble de grande
            
            // Propiedades de meteorito
            Projectile.extraUpdates = 0; // Sin extra updates (más lento)
        }

        public override void AI()
        {
            // Gravedad (caída normal)
            Projectile.velocity.Y += 0.15f;
            if (Projectile.velocity.Y > 12f)
                Projectile.velocity.Y = 12f;

            // Rotación hacia la dirección de movimiento
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            // Trail de fuego
            for (int i = 0; i < 2; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 
                    DustID.Torch, -Projectile.velocity.X * 0.3f, -Projectile.velocity.Y * 0.3f, 100, default, 1.5f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.5f;
            }

            // Partículas de humo
            if (Main.rand.NextBool(2))
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 
                    DustID.Smoke, 0, 0, 100, default, 0.8f);
                Main.dust[dust].noGravity = true;
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            ImpactEffects();
            return true; // Morir
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Aplicar debuff de fuego
            target.AddBuff(BuffID.OnFire, 180); // 3 segundos
            ImpactEffects();
        }

        private void ImpactEffects()
        {
            // Sonido de explosión
            SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);

            // Explosión de fuego
            for (int i = 0; i < 25; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(6f, 6f);
                int dust = Dust.NewDust(Projectile.Center, 0, 0, DustID.Torch, velocity.X, velocity.Y, 100, default, 2f);
                Main.dust[dust].noGravity = true;
            }

            // Partículas de piedra
            for (int i = 0; i < 15; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(4f, 4f);
                int dust = Dust.NewDust(Projectile.Center, 0, 0, DustID.Stone, velocity.X, velocity.Y - 2f, 100, default, 1.5f);
                Main.dust[dust].noGravity = false;
            }

            // Humo
            for (int i = 0; i < 10; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(3f, 3f);
                int dust = Dust.NewDust(Projectile.Center, 0, 0, DustID.Smoke, velocity.X, velocity.Y - 1f, 100, default, 1.2f);
                Main.dust[dust].noGravity = false;
            }
        }

        public override void OnKill(int timeLeft)
        {
            // Efecto extra al morir
            for (int i = 0; i < 8; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 
                    DustID.Torch, 0f, 0f, 100, default, 1.2f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 2f;
            }
        }

        public override Color? GetAlpha(Color lightColor)
        {
            // Sin transparencia
            return new Color(255, 200, 100, 250);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            Vector2 origin = texture.Size() / 2f;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Color drawColor = Projectile.GetAlpha(lightColor);
            
            // Flip vertical
            Main.EntitySpriteDraw(texture, drawPos, null, drawColor, Projectile.rotation,
                origin, Projectile.scale, SpriteEffects.FlipVertically, 0);
            
            return false;
        }
    }
}
