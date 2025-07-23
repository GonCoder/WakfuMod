using Terraria;
using Terraria.ModLoader;


namespace WakfuMod.jugador
{
    public class TimeRiftPlayer : ModPlayer
    {
        public bool isInTimeRift = false;

        public override void ResetEffects()
        {
            // Resetear el flag en cada frame
            isInTimeRift = false;
        }

        public override void PostUpdate()
        {
            // Ralentizar la animación de uso de items si está en el domo de ralentizacion de Nox (me parece que no funciona)
            if (isInTimeRift)
            {
                // Si el item tiene una animación en curso
                if (Player.itemAnimation > 0)
                {
                    // La animación avanza la mitad de rápido
                    // (porque en cada tick impar, "rebobinamos" el decremento automático)
                    if (Main.GameUpdateCount % 2 == 0)
                    {
                        Player.itemAnimation++;
                    }
                }
            }
        }
    }
}