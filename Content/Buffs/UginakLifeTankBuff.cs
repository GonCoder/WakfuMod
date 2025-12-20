using Terraria;
using Terraria.ModLoader;

namespace WakfuMod.Content.Buffs
{
    public class UginakLifeTankBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = false;
            Main.debuff[Type] = false; // Es un buff positivo
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // El efecto real se maneja en WakfuPlayer
            // Este buff solo indica que el jugador tiene vida extra activa
        }
    }
}
