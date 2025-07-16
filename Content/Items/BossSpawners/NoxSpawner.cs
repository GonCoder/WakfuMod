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
            // La condición ahora es diferente
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                // --- SI ES UN CLIENTE EN MULTIJUGADOR ---
                // No invocar al jefe directamente. Enviar un paquete al servidor.
                ModPacket packet = Mod.GetPacket();
                packet.Write((byte)WakfuMod.MessageType.SpawnNoxBoss); // Necesitaremos añadir este tipo de mensaje
                // No necesitamos enviar más datos, el servidor sabe quién lo envió (whoAmI)
                packet.Send();
            }
            else // Si es Single Player o el Servidor/Host
            {
                // --- LÓGICA DE INVOCACIÓN DIRECTA ---
                SpawnNox(player);
            }

            return true;
        }

        // Método helper para no repetir código
        public static void SpawnNox(Player player)
        {
            // Asegurarse de que el jefe no esté ya activo
            if (NPC.AnyNPCs(ModContent.NPCType<Nox>())) {
                return;
            }

            SoundEngine.PlaySound(SoundID.Roar, player.position);
            Vector2 spawnPos = player.Center + new Vector2(0, -300f);

            if (Main.netMode != NetmodeID.MultiplayerClient) // Solo el servidor/singleplayer realmente spawnea
            {
                int npcIndex = NPC.NewNPC(player.GetSource_ItemUse(player.HeldItem), (int)spawnPos.X, (int)spawnPos.Y, ModContent.NPCType<Nox>());
                
                if (Main.netMode == NetmodeID.Server) {
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npcIndex);
                }
            }
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