// Content/Tiles/GoalTileRed.cs
using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.ID;
using WakfuMod.Content.Projectiles; // Para FootballBomb
using WakfuMod.ModSystems; // Para FootballSystem
using Microsoft.Xna.Framework.Graphics; // Para dibujo si es visible

namespace WakfuMod.Content.Tiles // Reemplaza con tu namespace
{
    public class GoalTileRed : ModTile
    {
        public override void SetStaticDefaults()
        {
            Main.tileSolid[Type] = false; // No es sólido para el jugador
            Main.tileMergeDirt[Type] = false;
            Main.tileBlockLight[Type] = false; // No bloquea luz
            Main.tileNoAttach[Type] = true;
            Main.tileFrameImportant[Type] = true; // Necesario para que no se rompa fácilmente

            DustType = DustID.RedTorch; // Polvo rojo al romperse
            HitSound = SoundID.Dig; // Sonido al golpearlo (aunque no se rompa con pico)
            // ItemDrop = ModContent.ItemType<Items.Placeable.GoalTileRedItem>(); // Opcional: si quieres un item para colocarlo manualmente
            AddMapEntry(new Color(200, 50, 50), CreateMapEntryName()); // Color rojo en el mapa
        }

        // Hacerlo casi indestructible por medios normales
        public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
        {
            // Prevenir destrucción normal
            fail = true;
            effectOnly = true;
            noItem = true;
        }

        // --- COLISIÓN ESPECÍFICA CON LA PELOTA ---
        public override bool Slope(int i, int j) => false; // No permitir slopings

        // Este es el hook clave para la colisión del proyectil
        public override void NearbyEffects(int i, int j, bool closer)
        {
            // No necesitamos efectos pasivos aquí
        }



        // --- Dibujo (HACERLO INVISIBLE O SUTIL) ---
        public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
        {
            // Para hacerlo completamente invisible:
            // return false; // No dibujar nada

            // Para hacerlo visible pero sutil (ej. un borde rojo tenue):
            
            Texture2D pixel = Terraria.GameContent.TextureAssets.MagicPixel.Value;
            Vector2 zero = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange, Main.offScreenRange);
            Vector2 drawPos = new Vector2(i * 16 - (int)Main.screenPosition.X, j * 16 - (int)Main.screenPosition.Y) + zero;
            Color color = Color.Red * 0.3f; // Rojo muy transparente

            // Dibuja 4 líneas finas para el borde
            spriteBatch.Draw(pixel, new Rectangle((int)drawPos.X, (int)drawPos.Y, 16, 1), color); // Arriba
            spriteBatch.Draw(pixel, new Rectangle((int)drawPos.X, (int)drawPos.Y + 15, 16, 1), color); // Abajo
            spriteBatch.Draw(pixel, new Rectangle((int)drawPos.X, (int)drawPos.Y + 1, 1, 14), color); // Izquierda
            spriteBatch.Draw(pixel, new Rectangle((int)drawPos.X + 15, (int)drawPos.Y + 1, 1, 14), color); // Derecha

            return false; // Ya lo hemos dibujado
           
        }

         // Si quieres que tenga un ligero brillo (incluso invisible):
        public override void PostDraw(int i, int j, SpriteBatch spriteBatch) {
           Lighting.AddLight(new Vector2(i*16+8, j*16+8), Color.Red.ToVector3() * 0.1f);
        }
    }
}