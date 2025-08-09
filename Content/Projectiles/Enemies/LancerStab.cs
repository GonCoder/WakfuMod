using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.DataStructures;
using Terraria.ID;
using Microsoft.Xna.Framework.Graphics;

namespace WakfuMod.Content.Projectiles.Enemies
{
    public class LancerStab : ModProjectile
    {
        private const int TotalFrames = 5;

        public override void SetDefaults()
        {
            Projectile.width = 200;
            Projectile.height = 40;
            Projectile.hostile = false;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 25;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.aiStyle = -1;
            Projectile.DamageType = DamageClass.Melee;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();

            // Animación de 5 frames verticales
            if (++Projectile.frameCounter >= 4)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
                if (Projectile.frame >= TotalFrames)
                {
                    Projectile.frame = 0;
                }
            }

            foreach (Player player in Main.player)
            {
                if (player.active && !player.dead && Projectile.Hitbox.Intersects(player.Hitbox))
                {
                    Vector2 knockbackDir = (player.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
                    knockbackDir.Y = -0.6f;
                    float knockbackForce = 36f;

                    player.velocity = knockbackDir * knockbackForce;

                    int damage = Projectile.damage > 0 ? Projectile.damage : 20;
                    player.Hurt(PlayerDeathReason.ByProjectile(player.whoAmI, Projectile.whoAmI), damage, knockbackDir.X > 0 ? 1 : -1);

                    return;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>("WakfuMod/Content/Projectiles/Enemies/LancerStab").Value;
            int frameHeight = texture.Height / TotalFrames;
            int invertedFrame = TotalFrames - 1 - Projectile.frame;
            Rectangle sourceRect = new Rectangle(0, invertedFrame * frameHeight, texture.Width, frameHeight);


            Vector2 origin = sourceRect.Size() / 2f;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            Main.EntitySpriteDraw(
                texture,
                drawPos,
                sourceRect,
                Color.White,
                Projectile.rotation,
                origin,
                Projectile.scale,
                SpriteEffects.None,
                0f
            );

            return false;
        }
    }
}
