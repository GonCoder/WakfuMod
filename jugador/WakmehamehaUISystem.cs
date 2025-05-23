using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;
using WakfuMod.Content.Projectiles;
using System.Collections.Generic;


namespace WakfuMod.jugador
{
    public class WakmehamehaUISystem : ModSystem
    {
        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int index = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Resource Bars"));
            if (index != -1)
            {
                layers.Insert(index + 1, new LegacyGameInterfaceLayer(
                    "WakfuMod: Wakmehameha Cooldown",
                    DrawCooldownBar,
                    InterfaceScaleType.UI));
            }
        }

        private bool DrawCooldownBar()
        {
            if (Main.gameMenu || Main.dedServ)
                return true;

            Player player = Main.LocalPlayer;

            foreach (var proj in Main.projectile)
            {
                if (proj.active && proj.owner == player.whoAmI && proj.type == ModContent.ProjectileType<WakmehamehaProjectile>())
                {
                    float maxTime = 240 + 36f;
                    float remaining = proj.timeLeft;
                    float progress = 1f - (remaining / maxTime);

                    Texture2D barTexture = TextureAssets.MagicPixel.Value;
                    Vector2 position = new(Main.screenWidth / 3.9f - 20, Main.screenHeight - 90);
                    int width = 100;
                    int height = 10;

                    
                    Main.spriteBatch.Draw(barTexture, new Rectangle((int)position.X, (int)position.Y, (int)(width * progress), height), Color.Teal);
                     // Texto opcional (segundos restantes)
             float secondsLeft = remaining / 60f;
             Utils.DrawBorderStringFourWay(Main.spriteBatch, FontAssets.MouseText.Value, $"KamehameHA: {secondsLeft:F1}s", position.X + width/2f, position.Y + height/2f, Color.Teal, Color.Black, new Vector2(0.5f), 0.6f);
                    break;
                }
            }

            return true;
        }
    }
}
