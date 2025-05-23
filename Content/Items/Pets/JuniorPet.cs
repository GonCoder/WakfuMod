using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WakfuMod.Content.Buffs;
using WakfuMod.Content.Projectiles.Pets;
// Microsoft.Xna.Framework.Rectangle no es necesario aquí si no se usa en UseStyle

namespace WakfuMod.Content.Items.Pets
{
    public class JuniorPet : ModItem
    {
        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("Invoca una adorable mascota para que te acompañe");
            ItemID.Sets.GamepadWholeScreenUseRange[Item.type] = true;
            ItemID.Sets.LockOnIgnoresCollision[Item.type] = true;
        }

        public override void SetDefaults()
        {
            // Clonar defaults de un item de mascota vanilla es una buena práctica
            Item.CloneDefaults(ItemID.ZephyrFish); // Esto establece useStyle, useTime, useAnimation, etc.
            

            // --- ASIGNAR TU MASCOTA Y BUFF ---
            Item.shoot = ModContent.ProjectileType<JuniorPetProjectile>();
            Item.buffType = ModContent.BuffType<JuniorBuff>();
            // Item.damage y Item.knockBack se heredan (generalmente 0 para mascotas)

            Item.width = 28;
            Item.height = 26;
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.sellPrice(silver: 50);
            // Item.vanity = true; // ZephyrFish ya es vanity, pero no hace daño reafirmarlo.
        }

        // --- ELIMINAR UseStyle ---
        // public override void UseStyle(Player player) { ... }

        // --- USAR UseItem PARA APLICAR EL BUFF ---
        // Este método se llama cuando el item se "usa" exitosamente.
        // Para items de mascota, el "uso" es aplicar el buff.
        public override bool? UseItem(Player player)
        {
            // Solo el jugador local que usa el item debe gestionar el buff directamente así.
            // Para multijugador, la invocación del proyectil a través del buff es clave.
            if (player.whoAmI == Main.myPlayer)
            {
                // Alternar el buff: Si ya lo tiene, lo quita. Si no, lo añade.
                if (player.HasBuff(Item.buffType))
                {
                    player.ClearBuff(Item.buffType);
                }
                else
                {
                    // Aplica el buff. El buff se encargará de invocar el proyectil.
                    // La duración no importa mucho aquí si el buff se mantiene por sí mismo.
                    player.AddBuff(Item.buffType, 3600);
                }
            }
            // Devolver null o true para permitir que la animación de uso vanilla ocurra.
            // 'true' suele ser más común para items que realizan una acción.
            return true;
        }

        // Opcional: Receta
        // public override void AddRecipes() { ... }
    }
}