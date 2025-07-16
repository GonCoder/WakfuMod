// En Common/TimeSlowGlobalNPC.cs
using Terraria;
using Terraria.ModLoader;
using WakfuMod.Content.NPCs.Bosses.Nox;
using Microsoft.Xna.Framework;

namespace WakfuMod.Common
{
    // Indicar que queremos una instancia por cada NPC
   public class TimeSlowGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;
        public bool isSlowed = false;

        public override void ResetEffects(NPC npc)
        {
            isSlowed = false;
        }

        // No necesitamos PreAI

        public override void PostAI(NPC npc)
        {
            if (isSlowed)
            {
                // --- APLICAR RALENTIZACIÓN DIRECTA ---
                float slowFactor = 0.10f; // 90% de ralentización

                // 1. Reducir la velocidad del NPC
                npc.velocity *= slowFactor;

                // 2. Ralentizar la animación del NPC
                // Hacemos que su contador de frame solo avance 1 de cada 10 veces
                if (Main.GameUpdateCount % 10 != 0)
                {
                    // "Rebobinar" el frameCounter para que la animación se congele
                    // Nota: el frameCounter de NPC no es directamente accesible como el de Proyectil.
                    // La forma más simple es controlar el cambio de frame en FindFrame.
                    // Si el NPC no tiene FindFrame, este método es más complejo.
                    // Por ahora, nos centramos en la velocidad que es lo más visible.
                }
            }
        }

        // Opcional: Ralentizar la animación
        public override void FindFrame(NPC npc, int frameHeight)
        {
            // Si está ralentizado, solo permitir que el contador de frame avance 1 de cada 10 veces
            if (isSlowed && Main.GameUpdateCount % 10 != 0)
            {
                // Para congelar la animación, simplemente no dejamos que el contador avance.
                // Terraria incrementa npc.frameCounter internamente antes de llamar a FindFrame.
                // Al restarle 1, lo neutralizamos.
                npc.frameCounter--;
            }
        }
    }
}