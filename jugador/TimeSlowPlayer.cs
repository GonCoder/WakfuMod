// En TimeSlowPlayer.cs
using Terraria;
using Terraria.ModLoader;

namespace WakfuMod.jugador
{
    public class TimeSlowPlayer : ModPlayer
    {
        public bool isTimeSlowed = false;
        public float timeSlowFactor = 0.10f; // 90% de ralentización (0.10 de velocidad)

        public override void ResetEffects()
        {
            isTimeSlowed = false;
        }

        // Usamos PreUpdateMovement para afectar la física ANTES de que se aplique el movimiento
        public override void PreUpdateMovement()
        {
            if (isTimeSlowed)
            {
                // Ralentizar la velocidad de movimiento del jugador
                Player.maxRunSpeed *= timeSlowFactor;
                Player.runAcceleration *= timeSlowFactor;
                Player.jumpSpeedBoost -= 5f; // Reducir altura de salto
                Player.gravity *= timeSlowFactor;

                // Ralentizar la velocidad de uso de items
                if (Player.itemAnimation > 0 && Main.GameUpdateCount % 10 != 0)
                {
                    Player.itemAnimation++; // "Rebobinar" el decremento
                }
            }
        }
    }
}