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
    if (NPC.AnyNPCs(ModContent.NPCType<Nox>()))
    {
        return;
    }

    // --- Tus mensajes y sonidos se mantienen intactos ---
    SoundEngine.PlaySound(SoundID.Roar, player.position);
    Main.NewText("I'll fix my life's clock!", new Color(0, 200, 255));
    Main.NewText("It's time to go home HAHAHA!", new Color(0, 200, 255));
    Vector2 spawnPos = player.Center + new Vector2(0, -300f);

    if (Main.netMode != NetmodeID.MultiplayerClient) // La condición es correcta (solo SP o Servidor/Host)
    {
    
        // LÍNEA NUEVA (CORRECTA Y MÁS ROBUSTA):
        // Esta única llamada invoca al NPC y maneja la sincronización de red por sí misma.
        NPC.NewNPC(
            player.GetSource_ItemUse(player.HeldItem), // La fuente del evento
            (int)spawnPos.X,                           // Posición X
            (int)spawnPos.Y,                           // Posición Y
            ModContent.NPCType<Nox>(),                 // El tipo de NPC a invocar
            0,                                         // Start (whoAmI, generalmente 0 para jefes)
            0f, 0f, 0f, 0f,                            // Valores iniciales para ai[0] a ai[3]
            player.whoAmI                              // El objetivo inicial del NPC
        );

        // --- FIN DEL CAMBIO ---
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