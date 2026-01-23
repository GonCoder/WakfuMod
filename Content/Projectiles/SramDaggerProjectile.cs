using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace WakfuMod.Content.Projectiles
{
    public class SramDaggerProjectile : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 32;
            Projectile.aiStyle = 1; // Arrow style
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 600;
            Projectile.ignoreWater = false;
            Projectile.tileCollide = true;
            AIType = ProjectileID.ThrowingKnife;
            Projectile.scale = 0.65f;
        }

        public override void AI()
        {
            Projectile.rotation += 0.4f * (float)Projectile.direction;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Player player = Main.player[Projectile.owner];
            // Life steal: Heal 10 HP on hit (per dagger)
            int healAmount = 10;
            player.statLife += healAmount;
            player.HealEffect(healAmount);
        }
    }
}
