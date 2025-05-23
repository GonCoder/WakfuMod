// Content/Buffs/ZurcarakMinionBuff.cs
using Terraria;
using Terraria.ModLoader;
using WakfuMod.Content.Projectiles; // Necesario para asociar con el proyectil

namespace WakfuMod.Content.Buffs // Reemplaza WakfuMod con tu namespace
{
    public class ZurcarakMinionBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Ecaflip Kitten"); // Nombre del Buff (Usar .hjson)
            // Description.SetDefault("A playful but fierce kitten fights for you"); // Descripción (Usar .hjson)
            Main.buffNoSave[Type] = true; // No guardar el buff al salir
            Main.buffNoTimeDisplay[Type] = true; // No mostrar tiempo restante (es permanente mientras esté activo)
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // --- LÓGICA SIMPLIFICADA ---
            // Comprobar si el proyectil existe. Si no existe, eliminar el buff.
            if (player.ownedProjectileCounts[ModContent.ProjectileType<ZurcarakMinion>()] <= 0)
            {
                player.DelBuff(buffIndex);
                buffIndex--;
                // Actualizar flag en WakfuPlayer (si aún lo usas)
                if (player.whoAmI == Main.myPlayer && player.TryGetModPlayer<jugador.WakfuPlayer>(out var wp))
                {
                    wp.zurcarakMinionActive = false;
                }
            }
            else // Si el proyectil SÍ existe
            {
                // El proyectil se encarga de mantenerse vivo si el buff existe.
                // Solo necesitamos asegurarnos de que el buff no expire por tiempo.
                player.buffTime[buffIndex] = 48000; // Establecer a un tiempo alto (5 minutos) es suficiente
                                                    // El proyectil lo reseteará a 2 si el buff desaparece por otra razón.

                // Actualizar flag en WakfuPlayer (si aún lo usas)
                if (player.whoAmI == Main.myPlayer && player.TryGetModPlayer<jugador.WakfuPlayer>(out var wp))
                {
                    wp.zurcarakMinionActive = true;
                }
            }
        }
    }
}