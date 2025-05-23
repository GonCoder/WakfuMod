using Terraria;
using Terraria.ID; // Para ItemID.Sets
using Terraria.ModLoader;

namespace WakfuMod.Content.Items.Armor.Vanity.Yugo // Ajusta el namespace
{
    // La anotación [AutoloadEquip(EquipType.Head)] le dice a tModLoader
    // que este item equipa algo en la ranura de Cabeza y que busque
    // automáticamente el sprite llamado "YugoBody_Body.png" en la misma carpeta.
    [AutoloadEquip(EquipType.Body)]
    public class YugoBody : ModItem
    {
    
        public override void SetDefaults()
        {
            Item.width = 24; // Tamaño del icono en inventario
            Item.height = 22; // Tamaño del icono en inventario
            Item.value = Item.sellPrice(silver: 50); // Precio de venta
            Item.rare = ItemRarityID.Blue; // Rareza
            Item.vanity = true; // ¡IMPORTANTE! Marca el item como vanidad.
            Item.defense = 0; // Sin defensa.
          
        }

        // Opcional: Si quieres que se considere un "set" para algún logro o efecto visual
        // public override void SetMatch(bool male, ref int equipSlot, ref bool robes)
        // {
        //     // equipSlot = EquipLoader.GetEquipSlot(Mod, "YugoShirt", EquipType.Body); // Nombre de la pechera
        // }

        // Opcional: Añadir receta
        // public override void AddRecipes()
        // {
        //     CreateRecipe()
        //         .AddIngredient(ItemID.Silk, 10) // Ejemplo
        //         .AddTile(TileID.Loom) // Ejemplo
        //         .Register();
        // }
    }
}