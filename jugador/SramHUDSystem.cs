using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;
using System.Collections.Generic;

namespace WakfuMod.jugador
{
    public class SramHUDSystem : ModSystem
    {
        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int index = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Resource Bars"));
            if (index != -1)
            {
                layers.Insert(index + 1, new LegacyGameInterfaceLayer(
                    "WakfuMod: Sram UI",
                    delegate
                    {
                        DrawSramUI(Main.LocalPlayer);
                        return true;
                    },
                    InterfaceScaleType.UI));
            }
        }

        private void DrawSramUI(Player player)
        {
            if (Main.gameMenu || Main.dedServ || player == null)
                return;

            var wakfuPlayer = player.GetModPlayer<WakfuPlayer>();

            if (wakfuPlayer.claseElegida == WakfuClase.Sram)
            {
                Vector2 barPosition = new Vector2(Main.screenWidth / 2f - 60, Main.screenHeight - 150);
                DrawInvisibilityCooldownBar(player, wakfuPlayer, barPosition);
                
                Vector2 skill1BarPosition = new Vector2(Main.screenWidth / 2f - 60, Main.screenHeight - 180);
                DrawSkill1CooldownBar(player, wakfuPlayer, skill1BarPosition);
            }
        }

        private void DrawSkill1CooldownBar(Player player, WakfuPlayer wakfuPlayer, Vector2 position)
        {
            int maxCooldown = WakfuPlayer.SramAbility1BaseCooldown;
            int current = wakfuPlayer.sramAbility1Cooldown;

            if (current <= 0)
                return;

            float progress = 1f - (current / (float)maxCooldown);

            Texture2D tex = TextureAssets.MagicPixel.Value;
            int width = 120;
            int height = 10;

            // Background
            Main.spriteBatch.Draw(tex, new Rectangle((int)position.X, (int)position.Y, width, height), Color.DarkSlateGray * 0.8f);

            // Progress
            Main.spriteBatch.Draw(tex, new Rectangle((int)position.X, (int)position.Y, (int)(width * progress), height), Color.Red);

            // Text
            Utils.DrawBorderStringFourWay(
                Main.spriteBatch, 
                FontAssets.MouseText.Value, 
                $"Shadow Step: {current / 60f:F1}s", 
                position.X + 10, 
                position.Y - 20, 
                Color.White, 
                Color.Black, 
                Vector2.Zero, 
                0.8f
            );
        }

        private void DrawInvisibilityCooldownBar(Player player, WakfuPlayer wakfuPlayer, Vector2 position)
        {
            int maxCooldown = WakfuPlayer.SramInvisibilityBaseCooldown;
            int current = wakfuPlayer.sramInvisibilityCooldown;

            if (current <= 0)
                return;

            float progress = 1f - (current / (float)maxCooldown);

            Texture2D tex = TextureAssets.MagicPixel.Value;
            int width = 120;
            int height = 10;

            // Background
            Main.spriteBatch.Draw(tex, new Rectangle((int)position.X, (int)position.Y, width, height), Color.DarkSlateGray * 0.8f);

            // Progress
            Main.spriteBatch.Draw(tex, new Rectangle((int)position.X, (int)position.Y, (int)(width * progress), height), Color.Purple);

            // Text
            Utils.DrawBorderStringFourWay(
                Main.spriteBatch, 
                FontAssets.MouseText.Value, 
                $"Invisibility: {current / 60f:F1}s", 
                position.X + 10, 
                position.Y - 20, 
                Color.White, 
                Color.Black, 
                Vector2.Zero, 
                0.8f
            );
        }
    }
}
