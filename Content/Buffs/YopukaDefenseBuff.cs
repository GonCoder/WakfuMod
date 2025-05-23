using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;

namespace WakfuMod.Content.Buffs
{
    public class YopukaDefenseBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {

            Main.buffNoSave[Type] = false;
            Main.debuff[Type] = false;
            Main.pvpBuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;

        }
    }
}
