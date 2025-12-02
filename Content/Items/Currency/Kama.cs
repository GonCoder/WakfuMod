using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace WakfuMod.Content.Items.Currency
{
    public class Kama : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 20;
            Item.maxStack = 666;
            Item.value = Item.buyPrice(gold: 50); // Valor para vender (50 oro)
            Item.rare = ItemRarityID.Yellow;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe(5); // Crea 5 Kamas
            recipe.AddIngredient(ItemID.PlatinumCoin, 666);
            recipe.AddTile(TileID.VoidVault); // Puedes cambiarlo por otro banco
            recipe.Register();
        }
    }
}
