using Terraria;
using Terraria.ModLoader;

namespace WakfuMod.Content.Buffs // Asegúrate que el namespace coincida con tu estructura
{
    public class TymadorCableShockDebuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Cable Shock Cooldown"); // Nombre interno/debug
            // Description.SetDefault("Cannot be shocked by Tymador cables temporarily"); // Descripción interna/debug
            Main.debuff[Type] = true; // Es un debuff
            Main.pvpBuff[Type] = false; // No aplica en PvP (generalmente)
            Main.buffNoSave[Type] = true; // No guardar el buff al salir
            Main.buffNoTimeDisplay[Type] = true; // No mostrar el icono ni el tiempo
        }

        // No necesitamos ninguna lógica en Update, la sola presencia del buff es suficiente.
    }
}