// En Systems/NoxSpawnSystem.cs
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using WakfuMod.Content.NPCs.Bosses.Nox; // Para Nox
using System.IO;
using Microsoft.Xna.Framework;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.Chat;

namespace WakfuMod.ModSystems
{
    public class NoxSpawnSystem : ModSystem
    {
        public static float NoxSpawnChance = 0.05f; // 5% de probabilidad inicial

        public override void OnWorldLoad()
        {
            // Resetear al cargar el mundo si es necesario, o cargar el valor guardado
        }

        public override void SaveWorldData(TagCompound tag)
        {
            tag["NoxSpawnChance"] = NoxSpawnChance;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            if (tag.ContainsKey("NoxSpawnChance")) {
                NoxSpawnChance = tag.GetFloat("NoxSpawnChance");
            }
        }

        public override void PreUpdateWorld()
        {
            // Comprobar si se hace de día
            if (Main.dayTime && Main.time == 0.0)
            {
                // Comprobar si Nox no está ya activo
                if (!NPC.AnyNPCs(ModContent.NPCType<Nox>()))
                {
                    // Tirar el dado para el spawn
                    if (Main.rand.NextFloat() < NoxSpawnChance)
                    {
                        // Invocar a Nox
                        // En servidor, Main.LocalPlayer no es válido. Buscar un jugador activo.
                        int playerIndex = -1;
                        for (int i = 0; i < Main.maxPlayers; i++)
                        {
                            if (Main.player[i].active && !Main.player[i].dead)
                            {
                                playerIndex = i;
                                break;
                            }
                        }

                        if (playerIndex != -1)
                        {
                            Player player = Main.player[playerIndex];
                            Vector2 spawnPos = player.Center + new Vector2(0, -500f);
                            int npcIndex = NPC.NewNPC(new EntitySource_WorldEvent(), (int)spawnPos.X, (int)spawnPos.Y, ModContent.NPCType<Nox>());
                            // Mensaje para todos
                            if (Main.netMode == NetmodeID.Server)
                            {
                                ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral("An echo resound in time... ¡Nox is back, again?!"), new Color(0, 200, 255));
                                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npcIndex);
                            }
                            
                            else
                            {
                                Main.NewText("An echo resound in time... ¡Nox is back, again?!", new Color(0, 200, 255));
                            }
                        }
                    }
                }
            }
        }

        // Método que se llama desde OnKill de Nox
        public void OnNoxDefeated()
        {
            // Bajar la probabilidad de spawn
            if (NoxSpawnChance > 0.01f) // No bajar del 1%
            {
                NoxSpawnChance -= 0.01f;
            }
        }
    }
}