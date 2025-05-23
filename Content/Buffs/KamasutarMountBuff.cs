using Terraria;
using Terraria.ModLoader;
using WakfuMod.Content.Mounts; // Asegúrate que el namespace sea correcto

namespace WakfuMod.Content.Buffs // Ajusta el namespace
{
    public class KamasutarMountBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Montando Mi Montura"); // Usa .hjson
            // Description.SetDefault("¡A la aventura!"); // Usa .hjson
            Main.buffNoTimeDisplay[Type] = true; // No mostrar tiempo
            Main.buffNoSave[Type] = true;        // No guardar el buff (se aplica al usar el item)
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // El sistema de monturas de Terraria maneja la aplicación/eliminación
            // del buff y la activación de la montura a través de Item.mountType.
            // Esta función Update del buff se llama mientras el buff está activo.
            // Aquí es donde puedes aplicar efectos al jugador MIENTRAS está montado.

            // Ejemplo: Ligeramente más velocidad de movimiento mientras está montado
            // player.moveSpeed += 0.1f;

            // Mantener el mountType del jugador asignado
            player.mount.SetMount(ModContent.MountType<KamasutarSheet>(), player);
            player.buffTime[buffIndex] = 10; // Mantener el buff activo (se resetea cada tick por el sistema de monturas)
        }
    }
}