using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using Microsoft.Xna.Framework;
using WakfuMod.Content.Projectiles;

namespace WakfuMod.Content.Items.Weapons
{
    public class SramDagger : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Sram Dagger");
            // Tooltip.SetDefault("Left Click to Stab\nRight Click to Throw 2 Daggers");
        }

        public override void SetDefaults()
        {
            Item.damage = 25;
            Item.DamageType = DamageClass.Melee;
            Item.width = 40;
            Item.height = 40;
            Item.useTime = 15;
            Item.useAnimation = 15;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 4;
            Item.value = 10000;
            Item.rare = ItemRarityID.Green;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.noMelee = false; 
            Item.scale = 0.65f;
        }

        public override bool AltFunctionUse(Player player)
        {
            return true;
        }

        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2)
            {
                Item.useStyle = ItemUseStyleID.Swing;
                Item.useTime = 20;
                Item.useAnimation = 20;
                Item.noMelee = true;
                Item.shoot = ModContent.ProjectileType<SramDaggerProjectile>();
                Item.shootSpeed = 12f;
                Item.UseSound = SoundID.Item39; // Throw sound
            }
            else
            {
                Item.useStyle = ItemUseStyleID.Swing;
                Item.useTime = 15;
                Item.useAnimation = 15;
                Item.noMelee = false;
                Item.shoot = ProjectileID.None;
                Item.UseSound = SoundID.Item1;
            }
            return base.CanUseItem(player);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2)
            {
                // Throw 2 daggers
                for (int i = 0; i < 2; i++)
                {
                    Vector2 perturbedSpeed = velocity.RotatedByRandom(MathHelper.ToRadians(15));
                    Projectile.NewProjectile(source, position, perturbedSpeed, type, damage, knockback, player.whoAmI);
                }
                return false;
            }
            return false; // Don't shoot on left click
        }

        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Life steal: Heal 20 HP on hit
            int healAmount = 20;
            player.statLife += healAmount;
            player.HealEffect(healAmount);
        }
    }
}
