using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Microsoft.Xna.Framework;
using WakfuMod.Content.NPCs.Bosses.Toross;

namespace WakfuMod.Content.Items.BossSpawners
{
    public class TorossSpawner : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Toross Spawner");
            // Tooltip.SetDefault("Summons Toross");
            ItemID.Sets.SortingPriorityBossSpawns[Type] = 13;
        }

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 20;
            Item.maxStack = 20;
            Item.rare = ItemRarityID.Pink;
            Item.useAnimation = 45;
            Item.useTime = 45;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.consumable = true;
        }

        public override bool CanUseItem(Player player)
        {
            return !NPC.AnyNPCs(ModContent.NPCType<Toross>());
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                SoundEngine.PlaySound(SoundID.Roar, player.position);
                
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<Toross>());
                }
                else
                {
                    NetMessage.SendData(MessageID.SpawnBossUseLicenseStartEvent, number: player.whoAmI, number2: ModContent.NPCType<Toross>());
                }
            }
            return true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.HallowedBar, 5)
                .AddIngredient(ItemID.SoulofFright, 5)
                .AddIngredient(ItemID.SoulofMight, 5)
                .AddIngredient(ItemID.SoulofSight, 5)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}
