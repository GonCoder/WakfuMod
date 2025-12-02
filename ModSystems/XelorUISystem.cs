using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;
using System.Collections.Generic;
using WakfuMod.jugador;

namespace WakfuMod.ModSystems
{
    public class XelorUISystem : ModSystem
    {
        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int index = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Resource Bars"));
            if (index != -1)
            {
                layers.Insert(index + 1, new LegacyGameInterfaceLayer(
                    "WakfuMod: Xelor UI",
                    delegate
                    {
                        DrawXelorUI(Main.LocalPlayer);
                        return true;
                    },
                    InterfaceScaleType.UI));
            }
        }

        private void DrawXelorUI(Player player)
        {
            if (Main.gameMenu || Main.dedServ || player == null) return;
            var wakfuPlayer = player.GetModPlayer<WakfuPlayer>();
            
            if (wakfuPlayer.claseElegida == WakfuClase.Xelor)
            {
                // Posición similar a la de Yopuka pero ajustada si es necesario
                Vector2 barPos = new Vector2(Main.screenWidth / 2f - 50, Main.screenHeight - 120); 
                
                DrawTeleportCooldownBar(wakfuPlayer, barPos);

                // Barra de Habilidad 2 (X)
                Vector2 barPos2 = new Vector2(Main.screenWidth / 2f - 50, Main.screenHeight - 90);
                DrawTimeSuspensionBar(wakfuPlayer, barPos2);
            }
        }

        private void DrawTeleportCooldownBar(WakfuPlayer wakfuPlayer, Vector2 position)
        {
            int maxCooldown = WakfuPlayer.XelorTeleportBaseCooldown;
            int current = wakfuPlayer.xelorTeleportCooldown;

            if (current <= 0) return;

            float progress = 1f - (current / (float)maxCooldown);
            
            Texture2D tex = TextureAssets.MagicPixel.Value;
            int width = 100;
            int height = 10;

            // Fondo
            Main.spriteBatch.Draw(tex, new Rectangle((int)position.X, (int)position.Y, width, height), Color.Gray * 0.8f);
            
            // Barra
            Main.spriteBatch.Draw(tex, new Rectangle((int)position.X, (int)position.Y, (int)(width * progress), height), Color.Purple);
            
            // Texto
            float secondsLeft = current / 60f;
            Utils.DrawBorderStringFourWay(Main.spriteBatch, FontAssets.MouseText.Value, $"Teleport: {secondsLeft:F1}s", position.X + width/2f, position.Y + height/2f, Color.White, Color.Black, new Vector2(0.5f), 0.7f);
        }

        private void DrawTimeSuspensionBar(WakfuPlayer wakfuPlayer, Vector2 position)
        {
            // Si está activa, mostrar duración restante
            if (wakfuPlayer.xelorTimeSuspensionActive)
            {
                int maxDuration = WakfuPlayer.XelorTimeSuspensionDuration;
                int current = wakfuPlayer.xelorTimeSuspensionTimer;
                float progress = current / (float)maxDuration;

                Texture2D tex = TextureAssets.MagicPixel.Value;
                int width = 100;
                int height = 10;

                // Fondo
                Main.spriteBatch.Draw(tex, new Rectangle((int)position.X, (int)position.Y, width, height), Color.Gray * 0.8f);
                // Barra (Color diferente para activo)
                Main.spriteBatch.Draw(tex, new Rectangle((int)position.X, (int)position.Y, (int)(width * progress), height), Color.Cyan);
                
                float secondsLeft = current / 60f;
                Utils.DrawBorderStringFourWay(Main.spriteBatch, FontAssets.MouseText.Value, $"Rewind: {secondsLeft:F1}s", position.X + width/2f, position.Y + height/2f, Color.White, Color.Black, new Vector2(0.5f), 0.7f);
            }
            // Si está en cooldown, mostrar cooldown
            else if (wakfuPlayer.xelorAbility2Cooldown > 0)
            {
                int maxCooldown = WakfuPlayer.XelorAbility2BaseCooldown;
                int current = wakfuPlayer.xelorAbility2Cooldown;
                float progress = 1f - (current / (float)maxCooldown);

                Texture2D tex = TextureAssets.MagicPixel.Value;
                int width = 100;
                int height = 10;

                Main.spriteBatch.Draw(tex, new Rectangle((int)position.X, (int)position.Y, width, height), Color.Gray * 0.8f);
                Main.spriteBatch.Draw(tex, new Rectangle((int)position.X, (int)position.Y, (int)(width * progress), height), Color.DarkBlue);
                
                float secondsLeft = current / 60f;
                Utils.DrawBorderStringFourWay(Main.spriteBatch, FontAssets.MouseText.Value, $"Suspension: {secondsLeft:F1}s", position.X + width/2f, position.Y + height/2f, Color.White, Color.Black, new Vector2(0.5f), 0.7f);
            }
        }
    }
}