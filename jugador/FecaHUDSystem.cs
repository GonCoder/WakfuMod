using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using System.Collections.Generic;
using WakfuMod.jugador;
using Terraria.GameContent;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace WakfuMod.jugador
{
    public class FecaHUDSystem : ModSystem
    {
        // HUD State (Positions) - stored per HUD instance in memory (reset on reload)
        // If we want to save this, we need a Config or Player file. Let's keep it simple for now (session based or defaults).
        // Actually, let's just use static defaults that can be changed.
        
        private static Vector2 skill1Pos = new Vector2(-1, -1); // -1 means uninitialized
        private static Vector2 skill2Pos = new Vector2(-1, -1);
        private static bool isDragging1 = false;
        private static bool isDragging2 = false;
        private static Vector2 dragOffset = Vector2.Zero;

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int index = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Resource Bars"));
            if (index != -1)
            {
                layers.Insert(index + 1, new LegacyGameInterfaceLayer(
                    "WakfuMod: Feca UI",
                    delegate
                    {
                        DrawFecaUI(Main.LocalPlayer);
                        return true;
                    },
                    InterfaceScaleType.UI));
            }
        }

        private void DrawFecaUI(Player player)
        {
            if (Main.gameMenu || Main.dedServ || player == null)
                return;

            var wakfuPlayer = player.GetModPlayer<WakfuPlayer>();

            if (wakfuPlayer.claseElegida == WakfuClase.Feca)
            {
                // Init defaults if needed
                if (skill1Pos.X == -1) skill1Pos = new Vector2(Main.screenWidth / 2f - 60, Main.screenHeight - 180);
                if (skill2Pos.X == -1) skill2Pos = new Vector2(Main.screenWidth / 2f - 60, Main.screenHeight - 150);

                // Draw Ability 1 Cooldown (Glyphs)
                DrawDraggableSkillBar(player, wakfuPlayer.fecaAbility1Cooldown, WakfuPlayer.FecaAbility1BaseCooldown, ref skill1Pos, "Glyphs (V)", ref isDragging1);

                // Draw Ability 2 Cooldown (Shield)
                DrawDraggableSkillBar(player, wakfuPlayer.fecaAbility2Cooldown, WakfuPlayer.FecaAbility2BaseCooldown, ref skill2Pos, "Shield (X)", ref isDragging2);
            }
        }

        private void DrawDraggableSkillBar(Player player, int currentCooldown, int maxCooldown, ref Vector2 position, string label, ref bool isDragging)
        {
            int width = 120;
            int height = 20;
            Rectangle rect = new Rectangle((int)position.X, (int)position.Y, width, height);

            // Drag Logic (Only if Inventory is Open)
            if (Main.playerInventory)
            {
                if (rect.Contains(Main.mouseX, Main.mouseY))
                {
                    Main.LocalPlayer.mouseInterface = true; 
                    if (Main.mouseLeft && !isDragging)
                    {
                        isDragging = true;
                        dragOffset = new Vector2(Main.mouseX, Main.mouseY) - position;
                    }
                }

                if (isDragging)
                {
                    if (!Main.mouseLeft)
                    {
                        isDragging = false;
                    }
                    else
                    {
                         position = new Vector2(Main.mouseX, Main.mouseY) - dragOffset;
                         rect = new Rectangle((int)position.X, (int)position.Y, width, height); // Update rect for drawing
                    }
                }
            }
            else
            {
                // Safety: Stop dragging if inventory closes
                isDragging = false;
            }

            // Draw Bar
            float quotient = 1f;
            if (maxCooldown > 0)
                quotient = 1f - (float)currentCooldown / maxCooldown;

            Texture2D colorBar = TextureAssets.MagicPixel.Value;

            // Background
            Main.spriteBatch.Draw(colorBar, rect, Color.Gray * 0.5f);

            // Fill
            Main.spriteBatch.Draw(colorBar, new Rectangle((int)position.X, (int)position.Y, (int)(width * quotient), height), Color.Orange * 0.8f);

            // Hover indicator (white border) when draggable
            if (Main.playerInventory && rect.Contains(Main.mouseX, Main.mouseY))
            {
                 Utils.DrawBorderString(Main.spriteBatch, "*", position + new Vector2(-10, 0), Color.Yellow);
            }

            // Text
            Utils.DrawBorderString(Main.spriteBatch, label, position + new Vector2(width / 2 - 20, -20), Color.White, 0.8f);

            if (currentCooldown > 0)
            {
                string timeLeft = (currentCooldown / 60f).ToString("0.0");
                Utils.DrawBorderString(Main.spriteBatch, timeLeft, position + new Vector2(width/2 - 10, 2), Color.White, 0.8f);
            }
            else
            {
                 Utils.DrawBorderString(Main.spriteBatch, "READY", position + new Vector2(width/2 - 20, 2), Color.White, 0.8f);
            }
        }
    }
}
