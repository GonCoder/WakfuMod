using Terraria;
using Terraria.ModLoader;

namespace WakfuMod.Content.Buffs
{
    public class PrecisionBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // Apply damage bonus based on stacks
            // +4% per stack
            var modPlayer = player.GetModPlayer<jugador.WakfuPlayer>();
            player.GetDamage(DamageClass.Ranged) += modPlayer.precisionStacks * 0.04f;
        }

        public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
        {
            Player player = Main.LocalPlayer;
            var modPlayer = player.GetModPlayer<jugador.WakfuPlayer>();
            int bonus = (int)(modPlayer.precisionStacks * 4);
            tip += $"\nRanged Damage: +{bonus}%";
        }
    }
}
