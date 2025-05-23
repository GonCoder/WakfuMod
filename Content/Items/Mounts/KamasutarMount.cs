using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WakfuMod.Content.Mounts; // Asegúrate que el namespace sea correcto
using WakfuMod.Content.Buffs;  // Asegúrate que el namespace sea correcto

namespace WakfuMod.Content.Items.Mounts // Ajusta el namespace
{
    public class KamasutarMount : ModItem
    {
        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("Invoca una fiel montura"); // Usa .hjson
        }

        public override void SetDefaults()
        {
            Item.width = 28; // Ajusta al tamaño de tu icono
            Item.height = 28; // Ajusta al tamaño de tu icono
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Swing; // O ItemUseStyleID.HoldUp
            Item.value = Item.sellPrice(gold: 1); // Ajusta el valor
            Item.rare = ItemRarityID.Green;       // Ajusta la rareza
            Item.UseSound = SoundID.Item79; // Sonido de invocar montura (ej. Unicorn)
            Item.noMelee = true; // No es un arma
            Item.mountType = ModContent.MountType<KamasutarSheet>(); // ¡IMPORTANTE! Enlaza al ModMount
        }

        // No necesitas UseItem para aplicar el buff, Item.mountType lo hace automáticamente.
        // Al usar el item, si el jugador no está en esta montura, se le aplica el buff de la montura.
        // Si ya está en esta montura, se le quita.
    }
}