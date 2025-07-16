// En Systems/NoxSpawnSystem.cs
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using WakfuMod.Content.NPCs.Bosses.Nox; // Para Nox
using System.IO;
using Microsoft.Xna.Framework;
using Terraria.DataStructures;
using Terraria.ID;

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
                        Player player = Main.LocalPlayer; // O elegir un jugador aleatorio en MP
                        Vector2 spawnPos = player.Center + new Vector2(0, -500f);
                        int npcIndex = NPC.NewNPC(new EntitySource_WorldEvent(), (int)spawnPos.X, (int)spawnPos.Y, ModContent.NPCType<Nox>());
                        Main.NewText("Un eco temporal resuena... ¡Nox ha vuelto!", new Color(0, 200, 255));
                        
                        if (Main.netMode == NetmodeID.Server)
                        {
                            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npcIndex);
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

            // Desbloquear NPC aliado (se hará más adelante)
            // Ejemplo: DownedBossSystem.downedNox = true;
            // Y luego un GlobalNPC comprobaría esa variable para que el NPC se mude.
        }
    }
}