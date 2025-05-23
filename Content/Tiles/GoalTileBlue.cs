// Content/Tiles/GoalTileBlue.cs
using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.ID;
using WakfuMod.Content.Projectiles; // Para FootballBomb
using WakfuMod.ModSystems; // Para FootballSystem
using Microsoft.Xna.Framework.Graphics; // Para dibujo si es visible

namespace WakfuMod.Content.Tiles // Reemplaza con tu namespace
{
    public class GoalTileBlue : ModTile
    {
        public override void SetStaticDefaults()
        {
            Main.tileSolid[Type] = false;
            Main.tileMergeDirt[Type] = false;
            Main.tileBlockLight[Type] = false;
            Main.tileNoAttach[Type] = true;
            Main.tileFrameImportant[Type] = true;

            DustType = DustID.BlueCrystalShard; // Polvo azul al interactuar (aunque no se rompa normalmente)
            HitSound = SoundID.Dig;
            // ItemDrop = ModContent.ItemType<Items.Placeable.GoalTileBlueItem>(); // Opcional
            AddMapEntry(new Color(50, 50, 200), CreateMapEntryName()); // Color azul en el mapa
        }

        // Indestructible por medios normales
        public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
        {
            fail = true;
            effectOnly = true;
            noItem = true;
        }

        public override bool Slope(int i, int j) => false;

       
        // --- Dibujo (INVISIBLE O SUTIL) ---
        public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
        {
            // Invisible por defecto
            // return false;

            // Visible sutil (ej. borde azul):
            
            Texture2D pixel = Terraria.GameContent.TextureAssets.MagicPixel.Value;
            Vector2 zero = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange, Main.offScreenRange);
            Vector2 drawPos = new Vector2(i * 16 - (int)Main.screenPosition.X, j * 16 - (int)Main.screenPosition.Y) + zero;
            Color color = Color.SkyBlue * 0.3f; // Azul muy transparente

            spriteBatch.Draw(pixel, new Rectangle((int)drawPos.X, (int)drawPos.Y, 16, 1), color); // Arriba
            spriteBatch.Draw(pixel, new Rectangle((int)drawPos.X, (int)drawPos.Y + 15, 16, 1), color); // Abajo
            spriteBatch.Draw(pixel, new Rectangle((int)drawPos.X, (int)drawPos.Y + 1, 1, 14), color); // Izquierda
            spriteBatch.Draw(pixel, new Rectangle((int)drawPos.X + 15, (int)drawPos.Y + 1, 1, 14), color); // Derecha

            return false;
           
        }

         // --- Brillo Opcional ---
        public override void PostDraw(int i, int j, SpriteBatch spriteBatch) {
           Lighting.AddLight(new Vector2(i*16+8, j*16+8), Color.SkyBlue.ToVector3() * 0.1f);
        }
    }
}