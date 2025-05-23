using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using WakfuMod.Content.Projectiles.Pets; // Asegúrate que este es el namespace correcto

namespace WakfuMod.Content.Buffs // Ajusta el namespace
{
    public class JuniorBuff : ModBuff // O MiMascotaBuff
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Nombre De Tu Mascota");
            // Description.SetDefault("Descripción de tu mascota");
            Main.buffNoTimeDisplay[Type] = true;
            Main.vanityPet[Type] = true;         // Esto es lo importante
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // --- ELIMINAR ESTA LÍNEA ---
            // player.petFlag[Type] = true; // Ya no es necesario y causa error

            // Invocar el proyectil si no existe
            bool petProjectileNotSpawned = player.ownedProjectileCounts[ModContent.ProjectileType<JuniorPetProjectile>()] <= 0;

            if (petProjectileNotSpawned && player.whoAmI == Main.myPlayer)
            {
                Projectile.NewProjectile(
                    player.GetSource_Buff(buffIndex),
                    player.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<JuniorPetProjectile>(),
                    0,
                    0f,
                    player.whoAmI
                );
            }

            // Mantener el buff activo mientras el proyectil exista.
            if (!petProjectileNotSpawned) // Si el proyectil SÍ existe
            {
                player.buffTime[buffIndex] = 18000;
            }
            // Si el proyectil muere, la lógica en su AI (comprobando HasBuff) lo matará,
            // y al usar el item de nuevo, si el buff se quitó, se volverá a añadir.
            // Si el buff se quitó y el proyectil aún existe, el proyectil se matará.
            // Esto crea el ciclo correcto.
        }
    }
}