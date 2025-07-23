using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.DataStructures;
using Terraria.ID;

namespace WakfuMod.Content.Projectiles.Enemies
{
    public class LancerStab : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 200; // Largo de la estocada
            Projectile.height = 40; // Ancho de la estocada
            Projectile.hostile = false; // Desactivado: controlamos el daño manualmente
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 25;
            Projectile.alpha = 255; // Invisible
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.aiStyle = -1;
            Projectile.DamageType = DamageClass.Melee;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();

            foreach (Player player in Main.player)
            {
                if (player.active && !player.dead && Projectile.Hitbox.Intersects(player.Hitbox))
                {
                    Vector2 knockbackDir = (player.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
                    knockbackDir.Y = -0.6f; // Ligero empuje hacia arriba
                    float knockbackForce = 36f;

                    // Empujar al jugador
                    player.velocity = knockbackDir * knockbackForce;

                    // Aplicar daño manualmente
                    int damage = Projectile.damage > 0 ? Projectile.damage : 20;
                    player.Hurt(PlayerDeathReason.ByProjectile(player.whoAmI, Projectile.whoAmI), damage, knockbackDir.X > 0 ? 1 : -1);

                    return;
                }
            }
        }
    }
}
