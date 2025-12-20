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

        public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
        {
            var wakfuPlayer = Main.LocalPlayer.GetModPlayer<jugador.WakfuPlayer>();
            tip = $"Current Life Tank: {wakfuPlayer.uginakExtraLife}/{wakfuPlayer.uginakMaxExtraLife}\nAbsorbs incoming damage.";
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // El efecto real se maneja en WakfuPlayer
        }
    }
}
