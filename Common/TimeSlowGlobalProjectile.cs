// En Common/TimeSlowGlobalProjectile.cs
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using WakfuMod.Content.NPCs.Bosses;

namespace WakfuMod.Common
{
   public class TimeSlowGlobalProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;
        public bool isSlowed = false;

        public  void ResetEffects(Projectile projectile)
        {
            isSlowed = false;
        }

        // No necesitamos PreAI

        public override void PostAI(Projectile projectile)
        {
            if (isSlowed)
            {
                // --- APLICAR RALENTIZACIÓN DIRECTA ---
                float slowFactor = 0.10f; // 90% de ralentización

                // 1. Reducir la velocidad del proyectil
                projectile.velocity *= slowFactor;

                // 2. Ralentizar la animación del proyectil
                // Si el proyectil se anima con frameCounter, podemos ralentizarlo
                if (projectile.frameCounter > 0 && Main.GameUpdateCount % 10 != 0)
                {
                    projectile.frameCounter--; // Rebobinar el incremento de frame
                }
            }
        }
    }
}