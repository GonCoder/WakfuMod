using System.Collections.Generic;
using Terraria.ModLoader;

namespace WakfuMod.ModSystems // Asegúrate que el namespace es correcto
{
    public class TymadorTickSystem : ModSystem
    {
        // Un conjunto para almacenar los whoAmI de las bombas detonadas ESTE tick
        public static HashSet<int> DetonatedThisTick = new HashSet<int>();

        // Este método se llama al final de cada actualización del mundo
        public override void PostUpdateWorld()
        {
            // Limpiar la lista al final del tick para el siguiente
            DetonatedThisTick.Clear();
        }

         // También podrías usar PreUpdateWorld si prefieres limpiar al inicio
         // public override void PreUpdateWorld()
         // {
         //     DetonatedThisTick.Clear();
         // }
    }
}