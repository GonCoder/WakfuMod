using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using WakfuMod.Content.Projectiles;

namespace WakfuMod.Content.Items.Weapons
{
    public class Rufus : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Rufus"); // No longer needed in 1.4.4+ usually, but kept if needed by older tModLoader versions or relying on hjson
            // Tooltip.SetDefault("Summons Rufus to fight for you.\nScales with Magic Damage and Max HP.");
        }

        public override void SetDefaults()
        {
            Item.damage = 10; // Base damage, but minion overwrites this
            Item.knockBack = 3f;
            Item.mana = 10;
            Item.width = 32;
            Item.height = 32;
            Item.useTime = 36;
            Item.useAnimation = 36;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.value = Item.buyPrice(0, 5, 0, 0);
            Item.rare = ItemRarityID.Orange;
            Item.UseSound = SoundID.Item44;
            
            // Minion setup
            Item.noMelee = true;
            Item.DamageType = DamageClass.Magic; // Requested change
            Item.buffType = ModContent.BuffType<Content.Buffs.RufusBuff>(); // Need to create this buff too? User didn't ask for buff, but minions usually need one.
            // If I skip buff, the minion might despawn immediately if standard minion logic applies (CheckActive).
            // Detailed prompt didn't ask for buff, but it's standard Terraria architecture.
            // I'll assume I need to create a simple buff or handle "CheckActive" to strictly return true for testing.
            // I'll implement a simple CheckActive in projectile without separate Buff file first to save steps,
            // or just create the buff file in the next step. Using a buff is cleaner.
            Item.shoot = ModContent.ProjectileType<RufusMinion>();
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Summon the minion at mouse cursor
            player.AddBuff(Item.buffType, 2);

            var projectile = Projectile.NewProjectileDirect(source, Main.MouseWorld, default, type, damage, knockback, player.whoAmI);
            projectile.originalDamage = damage;

            return false; // Manual spawning
        }
    }
}
