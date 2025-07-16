using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using WakfuMod.Content.NPCs.Bosses.Nox; // Para el NPC Nox

namespace WakfuMod.Content.Items.BossSpawners // Ajusta el namespace
{
    public class NoxSpawner : ModItem
    {
        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("Un reloj que late con una energía extraña...\nInvoca a Nox por la noche");
            ItemID.Sets.SortingPriorityBossSpawns[Type] = 12; // Prioridad en el inventario
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;
            Item.maxStack = 20;
            Item.rare = ItemRarityID.Lime;
            Item.useAnimation = 30;
            Item.useTime = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.consumable = false; // Se consume al usarlo
        }

        public override bool CanUseItem(Player player)
        {
            // Se puede usar en cualquier momento si Nox no está activo
            return !NPC.AnyNPCs(ModContent.NPCType<Nox>());
        }

        public override bool? UseItem(Player player)
        {
            // Solo invocar si no es un cliente en multijugador
            if (player.whoAmI == Main.myPlayer && Main.netMode != NetmodeID.MultiplayerClient)
            {
                // Sonido de invocación
                SoundEngine.PlaySound(SoundID.Roar, player.position);

                // Posición de spawn: encima del jugador
                Vector2 spawnPos = player.Center + new Vector2(0, -300f);

                // Invocar al jefe
                int npcIndex = NPC.NewNPC(player.GetSource_ItemUse(Item), (int)spawnPos.X, (int)spawnPos.Y, ModContent.NPCType<Nox>());

                // Sincronizar en multijugador
                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npcIndex);
                }
            }
            return true;
        }

        // Opcional: Receta para crear el item
        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.SoulofNight, 5)
                .AddIngredient(ItemID.Cog, 10)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}