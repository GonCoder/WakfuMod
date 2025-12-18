// HipermagoHUDSystem.cs
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;
using System.Collections.Generic;

namespace WakfuMod.jugador
{
    public class HipermagoHUDSystem : ModSystem
    {
        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int index = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Resource Bars"));
            if (index != -1)
            {
                layers.Insert(index + 1, new LegacyGameInterfaceLayer(
                    "WakfuMod: Hipermago UI",
                    delegate
                    {
                        DrawHipermagoUI(Main.LocalPlayer);
                        return true;
                    },
                    InterfaceScaleType.UI));
            }
        }

        private void DrawHipermagoUI(Player player)
        {
            if (Main.gameMenu || Main.dedServ || player == null)
                return;

            var wakfuPlayer = player.GetModPlayer<WakfuPlayer>();

            // Solo dibuja si es Hipermago
            if (wakfuPlayer.claseElegida == WakfuClase.Hipermago)
            {
                // --- Dibuja la Barra de Cooldown Habilidad 1 (Doble Bola de Luz) ---
                Vector2 ability1CdBarPosition = new Vector2(Main.screenWidth / 3.9f - 20, Main.screenHeight - 155);
                DrawAbility1CooldownBar(player, wakfuPlayer, ability1CdBarPosition);

                // --- Dibuja la Barra de Cooldown Habilidad 2 (Holy Spear) ---
                Vector2 ability2CdBarPosition = new Vector2(Main.screenWidth / 3.9f - 20, Main.screenHeight - 125);
                DrawAbility2CooldownBar(player, wakfuPlayer, ability2CdBarPosition);
                
                // --- Dibuja la Barra de Cooldown del Combo Elemental ---
                Vector2 comboCdBarPosition = new Vector2(Main.screenWidth / 3.9f - 20, Main.screenHeight - 95);
                DrawElementalComboCooldownBar(player, wakfuPlayer, comboCdBarPosition);
                
                // --- Dibuja las Barras de CD de las Armas Elementales ---
                Vector2 fireCdBarPosition = new Vector2(Main.screenWidth / 3.9f - 20, Main.screenHeight - 65);
                DrawFireCooldownBar(player, wakfuPlayer, fireCdBarPosition);
                
                Vector2 earthCdBarPosition = new Vector2(Main.screenWidth / 3.9f + 135, Main.screenHeight - 65);
                DrawEarthCooldownBar(player, wakfuPlayer, earthCdBarPosition);
                
                Vector2 airCdBarPosition = new Vector2(Main.screenWidth / 3.9f - 20, Main.screenHeight - 35);
                DrawAirCooldownBar(player, wakfuPlayer, airCdBarPosition);
                
                Vector2 waterCdBarPosition = new Vector2(Main.screenWidth / 3.9f + 135, Main.screenHeight - 35);
                DrawWaterCooldownBar(player, wakfuPlayer, waterCdBarPosition);
                
                // Las runas ahora se dibujan encima del jugador (HipermagoRuneDrawLayer)
            }
        }

        // --- Barra de Cooldown Habilidad 1: Doble Bola de Luz ---
        private void DrawAbility1CooldownBar(Player player, WakfuPlayer wakfuPlayer, Vector2 position)
        {
            int maxCooldown = WakfuPlayer.HipermagoAbility1BaseCooldown;
            int current = wakfuPlayer.hipermagoAbility1Cooldown;

            // No dibujar si no hay cooldown
            if (current <= 0)
                return;

            // Progreso (cuánto falta para estar listo)
            float progress = 1f - (current / (float)maxCooldown);

            Texture2D tex = TextureAssets.MagicPixel.Value;
            int width = 120;
            int height = 8;

            // Fondo gris oscuro
            Main.spriteBatch.Draw(tex, new Rectangle((int)position.X, (int)position.Y, width, height), Color.DarkSlateGray * 0.8f);

            // Barra de progreso (blanca como pidió el usuario)
            Main.spriteBatch.Draw(tex, new Rectangle((int)position.X, (int)position.Y, (int)(width * progress), height), Color.White);

            // Borde sutil
            DrawBorder(tex, position, width, height, Color.Gold * 0.5f);

            // Texto (segundos restantes)
            float secondsLeft = current / 60f;
            Utils.DrawBorderStringFourWay(
                Main.spriteBatch, 
                FontAssets.MouseText.Value, 
                $"Light Ball: {secondsLeft:F1}s", 
                position.X + width / 2f, 
                position.Y + height / 2f, 
                Color.White, 
                Color.Black, 
                new Vector2(0.5f), 
                0.65f
            );
        }

        // Dibuja un borde simple alrededor de la barra
        private void DrawBorder(Texture2D tex, Vector2 position, int width, int height, Color color)
        {
            int borderSize = 1;
            // Top
            Main.spriteBatch.Draw(tex, new Rectangle((int)position.X - borderSize, (int)position.Y - borderSize, width + borderSize * 2, borderSize), color);
            // Bottom
            Main.spriteBatch.Draw(tex, new Rectangle((int)position.X - borderSize, (int)position.Y + height, width + borderSize * 2, borderSize), color);
            // Left
            Main.spriteBatch.Draw(tex, new Rectangle((int)position.X - borderSize, (int)position.Y, borderSize, height), color);
            // Right
            Main.spriteBatch.Draw(tex, new Rectangle((int)position.X + width, (int)position.Y, borderSize, height), color);
        }

        // --- Barra de Cooldown Habilidad 2: Holy Spear ---
        private void DrawAbility2CooldownBar(Player player, WakfuPlayer wakfuPlayer, Vector2 position)
        {
            int maxCooldown = WakfuPlayer.HipermagoAbility2BaseCooldown;
            int current = wakfuPlayer.hipermagoAbility2Cooldown;

            // No dibujar si no hay cooldown
            if (current <= 0)
                return;

            float progress = 1f - (current / (float)maxCooldown);

            Texture2D tex = TextureAssets.MagicPixel.Value;
            int width = 120;
            int height = 8;

            // Fondo gris oscuro
            Main.spriteBatch.Draw(tex, new Rectangle((int)position.X, (int)position.Y, width, height), Color.DarkSlateGray * 0.8f);

            // Barra de progreso (dorada para Holy Spear)
            Main.spriteBatch.Draw(tex, new Rectangle((int)position.X, (int)position.Y, (int)(width * progress), height), Color.Gold);

            // Borde
            DrawBorder(tex, position, width, height, Color.Yellow * 0.5f);

            // Texto
            float secondsLeft = current / 60f;
            Utils.DrawBorderStringFourWay(
                Main.spriteBatch, 
                FontAssets.MouseText.Value, 
                $"Holy Spear: {secondsLeft:F1}s", 
                position.X + width / 2f, 
                position.Y + height / 2f, 
                Color.Gold, 
                Color.Black, 
                new Vector2(0.5f), 
                0.65f
            );
        }

        // --- Barra de Cooldown del Combo Elemental ---
        private void DrawElementalComboCooldownBar(Player player, WakfuPlayer wakfuPlayer, Vector2 position)
        {
            int maxCooldown = WakfuPlayer.HipermagoElementalComboCooldown;
            int current = wakfuPlayer.hipermagoElementalComboCooldown;

            // No dibujar si no hay cooldown
            if (current <= 0)
                return;

            float progress = 1f - (current / (float)maxCooldown);

            Texture2D tex = TextureAssets.MagicPixel.Value;
            int width = 120;
            int height = 8;

            // Fondo gris oscuro
            Main.spriteBatch.Draw(tex, new Rectangle((int)position.X, (int)position.Y, width, height), Color.DarkSlateGray * 0.8f);

            // Barra de progreso (magenta para combo)
            Main.spriteBatch.Draw(tex, new Rectangle((int)position.X, (int)position.Y, (int)(width * progress), height), Color.Magenta);

            // Borde
            DrawBorder(tex, position, width, height, Color.Purple * 0.5f);

            // Texto
            float secondsLeft = current / 60f;
            Utils.DrawBorderStringFourWay(
                Main.spriteBatch, 
                FontAssets.MouseText.Value, 
                $"Combo: {secondsLeft:F1}s", 
                position.X + width / 2f, 
                position.Y + height / 2f, 
                Color.Magenta, 
                Color.Black, 
                new Vector2(0.5f), 
                0.65f
            );
        }

        // --- Barra de Cooldown de Fuego ---
        private void DrawFireCooldownBar(Player player, WakfuPlayer wakfuPlayer, Vector2 position)
        {
            int maxCooldown = WakfuPlayer.HipermagoFireBaseCooldown;
            int current = wakfuPlayer.hipermagoFireCooldown;

            if (current <= 0)
                return;

            float progress = 1f - (current / (float)maxCooldown);
            Texture2D tex = TextureAssets.MagicPixel.Value;
            int width = 110;
            int height = 6;

            Main.spriteBatch.Draw(tex, new Rectangle((int)position.X, (int)position.Y, width, height), Color.DarkSlateGray * 0.8f);
            Main.spriteBatch.Draw(tex, new Rectangle((int)position.X, (int)position.Y, (int)(width * progress), height), Color.OrangeRed);
            DrawBorder(tex, position, width, height, Color.Red * 0.5f);

            float secondsLeft = current / 60f;
            Utils.DrawBorderStringFourWay(Main.spriteBatch, FontAssets.MouseText.Value, $"Fire: {secondsLeft:F1}s", 
                position.X + width / 2f, position.Y + height / 2f, Color.OrangeRed, Color.Black, new Vector2(0.5f), 0.55f);
        }

        // --- Barra de Cooldown de Tierra ---
        private void DrawEarthCooldownBar(Player player, WakfuPlayer wakfuPlayer, Vector2 position)
        {
            int maxCooldown = WakfuPlayer.HipermagoEarthBaseCooldown;
            int current = wakfuPlayer.hipermagoEarthCooldown;

            if (current <= 0)
                return;

            float progress = 1f - (current / (float)maxCooldown);
            Texture2D tex = TextureAssets.MagicPixel.Value;
            int width = 110;
            int height = 6;

            Main.spriteBatch.Draw(tex, new Rectangle((int)position.X, (int)position.Y, width, height), Color.DarkSlateGray * 0.8f);
            Main.spriteBatch.Draw(tex, new Rectangle((int)position.X, (int)position.Y, (int)(width * progress), height), Color.SaddleBrown);
            DrawBorder(tex, position, width, height, Color.Green * 0.5f);

            float secondsLeft = current / 60f;
            Utils.DrawBorderStringFourWay(Main.spriteBatch, FontAssets.MouseText.Value, $"Earth: {secondsLeft:F1}s", 
                position.X + width / 2f, position.Y + height / 2f, Color.SaddleBrown, Color.Black, new Vector2(0.5f), 0.55f);
        }

        // --- Barra de Cooldown de Aire ---
        private void DrawAirCooldownBar(Player player, WakfuPlayer wakfuPlayer, Vector2 position)
        {
            int maxCooldown = WakfuPlayer.HipermagoAirBaseCooldown;
            int current = wakfuPlayer.hipermagoAirCooldown;

            if (current <= 0)
                return;

            float progress = 1f - (current / (float)maxCooldown);
            Texture2D tex = TextureAssets.MagicPixel.Value;
            int width = 110;
            int height = 6;

            Main.spriteBatch.Draw(tex, new Rectangle((int)position.X, (int)position.Y, width, height), Color.DarkSlateGray * 0.8f);
            Main.spriteBatch.Draw(tex, new Rectangle((int)position.X, (int)position.Y, (int)(width * progress), height), Color.LightCyan);
            DrawBorder(tex, position, width, height, Color.Cyan * 0.5f);

            float secondsLeft = current / 60f;
            Utils.DrawBorderStringFourWay(Main.spriteBatch, FontAssets.MouseText.Value, $"Air: {secondsLeft:F1}s", 
                position.X + width / 2f, position.Y + height / 2f, Color.Cyan, Color.Black, new Vector2(0.5f), 0.55f);
        }

        // --- Barra de Cooldown de Agua ---
        private void DrawWaterCooldownBar(Player player, WakfuPlayer wakfuPlayer, Vector2 position)
        {
            int maxCooldown = WakfuPlayer.HipermagoWaterBaseCooldown;
            int current = wakfuPlayer.hipermagoWaterCooldown;

            if (current <= 0)
                return;

            float progress = 1f - (current / (float)maxCooldown);
            Texture2D tex = TextureAssets.MagicPixel.Value;
            int width = 110;
            int height = 6;

            Main.spriteBatch.Draw(tex, new Rectangle((int)position.X, (int)position.Y, width, height), Color.DarkSlateGray * 0.8f);
            Main.spriteBatch.Draw(tex, new Rectangle((int)position.X, (int)position.Y, (int)(width * progress), height), Color.DodgerBlue);
            DrawBorder(tex, position, width, height, Color.Blue * 0.5f);

            float secondsLeft = current / 60f;
            Utils.DrawBorderStringFourWay(Main.spriteBatch, FontAssets.MouseText.Value, $"Water: {secondsLeft:F1}s", 
                position.X + width / 2f, position.Y + height / 2f, Color.DodgerBlue, Color.Black, new Vector2(0.5f), 0.55f);
        }

        // --- Indicadores de Runas Elementales ---
        private void DrawRuneIndicators(Player player, WakfuPlayer wakfuPlayer, Vector2 position)
        {
            int totalRunes = wakfuPlayer.GetTotalRunes();
            
            // No dibujar si no hay runas
            if (totalRunes <= 0)
                return;

            Texture2D tex = TextureAssets.MagicPixel.Value;
            int runeSize = 12;
            int spacing = 4;
            int currentX = (int)position.X;

            // Dibujar runas de fuego (rojo)
            for (int i = 0; i < wakfuPlayer.hipermagoFireRunes; i++)
            {
                Main.spriteBatch.Draw(tex, new Rectangle(currentX, (int)position.Y, runeSize, runeSize), Color.OrangeRed);
                DrawBorder(tex, new Vector2(currentX, position.Y), runeSize, runeSize, Color.Red * 0.8f);
                currentX += runeSize + spacing;
            }

            // Dibujar runas de tierra (marrón/verde)
            for (int i = 0; i < wakfuPlayer.hipermagoEarthRunes; i++)
            {
                Main.spriteBatch.Draw(tex, new Rectangle(currentX, (int)position.Y, runeSize, runeSize), Color.SaddleBrown);
                DrawBorder(tex, new Vector2(currentX, position.Y), runeSize, runeSize, Color.Green * 0.8f);
                currentX += runeSize + spacing;
            }

            // Dibujar runas de aire (celeste)
            for (int i = 0; i < wakfuPlayer.hipermagoAirRunes; i++)
            {
                Main.spriteBatch.Draw(tex, new Rectangle(currentX, (int)position.Y, runeSize, runeSize), Color.LightCyan);
                DrawBorder(tex, new Vector2(currentX, position.Y), runeSize, runeSize, Color.Cyan * 0.8f);
                currentX += runeSize + spacing;
            }

            // Dibujar runas de agua (azul)
            for (int i = 0; i < wakfuPlayer.hipermagoWaterRunes; i++)
            {
                Main.spriteBatch.Draw(tex, new Rectangle(currentX, (int)position.Y, runeSize, runeSize), Color.DodgerBlue);
                DrawBorder(tex, new Vector2(currentX, position.Y), runeSize, runeSize, Color.Blue * 0.8f);
                currentX += runeSize + spacing;
            }

            // Si tiene combo disponible, mostrar texto
            if (wakfuPlayer.HasRuneCombo())
            {
                Utils.DrawBorderStringFourWay(
                    Main.spriteBatch, 
                    FontAssets.MouseText.Value, 
                    "COMBO READY!", 
                    position.X + 60, 
                    position.Y + runeSize + 5, 
                    Color.Magenta, 
                    Color.Black, 
                    new Vector2(0.5f), 
                    0.6f
                );
            }
        }
    }
}
