using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using WakfuMod.Content.NPCs.Bosses.Toross;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria.DataStructures;
using Terraria.ID;

namespace WakfuMod.ModSystems
{
    public class TorossSpawnSystem : ModSystem
    {
        public static float TorossSpawnChance = 0.05f; // 5% chance

        public override void OnWorldLoad()
        {
            // Reset or load if needed
        }

        public override void SaveWorldData(TagCompound tag)
        {
            tag["TorossSpawnChance"] = TorossSpawnChance;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            if (tag.ContainsKey("TorossSpawnChance")) {
                TorossSpawnChance = tag.GetFloat("TorossSpawnChance");
            }
        }

        public override void PreUpdateWorld()
        {
            // Check if it's night time and just started (time == 0.0)
            if (!Main.dayTime && Main.time == 0.0)
            {
                // Check if Moon Lord has been defeated
                if (NPC.downedMoonlord)
                {
                    // Check if Toross is not already active
                    if (!NPC.AnyNPCs(ModContent.NPCType<Toross>()))
                    {
                        // Roll dice for spawn
                        if (Main.rand.NextFloat() < TorossSpawnChance)
                        {
                            // Spawn Toross
                            Player player = Main.LocalPlayer; // Or random player in MP
                            // In MP, PreUpdateWorld runs on server too, so we need to be careful with Main.LocalPlayer
                            // But usually for spawning logic on server, we pick a random player or just spawn near a valid player.
                            
                            int playerIndex = Main.myPlayer;
                            if (Main.netMode == NetmodeID.Server)
                            {
                                // Pick first active player
                                for (int i = 0; i < Main.maxPlayers; i++)
                                {
                                    if (Main.player[i].active && !Main.player[i].dead)
                                    {
                                        playerIndex = i;
                                        break;
                                    }
                                }
                            }
                            
                            Player target = Main.player[playerIndex];
                            if (target.active && !target.dead)
                            {
                                Vector2 spawnPos = target.Center + new Vector2(0, -500f);
                                int npcIndex = NPC.NewNPC(new EntitySource_WorldEvent(), (int)spawnPos.X, (int)spawnPos.Y, ModContent.NPCType<Toross>());
                                Main.NewText("A massive Stasis energy is gathering nearby... Toross has arrived!", new Color(255, 0, 255));
                                
                                if (Main.netMode == NetmodeID.Server)
                                {
                                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npcIndex);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
