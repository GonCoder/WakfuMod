// YopukaRageBarSystem.cs
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;
using System.Collections.Generic;
using WakfuMod.Content.Items.Weapons; // Necesario para acceder a la constante del cooldown

namespace WakfuMod.jugador // Asegúrate que el namespace es correcto
{
    public class YopukaRageBarSystem : ModSystem
    {
        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int index = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Resource Bars"));
            if (index != -1)
            {
                layers.Insert(index + 1, new LegacyGameInterfaceLayer(
                    "WakfuMod: Yopuka UI", // Nombre genérico para incluir varias barras
                    delegate
                    {
                        // Llama a las funciones de dibujo aquí
                        DrawYopukaUI(Main.LocalPlayer);
                        return true;
                    },
                    InterfaceScaleType.UI));
            }
        }

        // Función principal que llama a las otras barras
        private void DrawYopukaUI(Player player)
        {
            if (Main.gameMenu || Main.dedServ || player == null)
                return;

            var wakfuPlayer = player.GetModPlayer<WakfuPlayer>();
            var cdPlayer = player.GetModPlayer<YopukaWeaponCDPlayer>(); // Obtén la instancia del ModPlayer de cooldown

            // Solo dibuja si es Yopuka
            if (wakfuPlayer.claseElegida == WakfuClase.Yopuka)
            {
                // --- Dibuja la Barra de Rabia ---
                // (Misma posición relativa que antes, ajusta Y si es necesario)
                Vector2 rageBarPosition = new Vector2(Main.screenWidth / 3.9f - 20, Main.screenHeight - 65);
                DrawRageBar(player, wakfuPlayer, rageBarPosition);

                // --- Dibuja la Barra de Cooldown Habilidades (V/X) ---
                // (Misma posición relativa que antes, ajusta Y si es necesario)
                Vector2 abilityCdBarPosition = new Vector2(Main.screenWidth / 3.7f - 20, Main.screenHeight - 40);
                DrawAbilityCooldownBar(player, wakfuPlayer, abilityCdBarPosition);

                // --- Dibuja la Barra de Cooldown de la Espada (Clic Derecho) ---
                // Nueva posición, por ejemplo, debajo de la barra de habilidades
                Vector2 swordCdBarPosition = new Vector2(Main.screenWidth / 3.5f - 20, Main.screenHeight - 20); // Ajusta esta Y
                DrawSwordCooldownBar(player, cdPlayer, swordCdBarPosition);
            }
        }


        // --- Dibujar Barra de Rabia (Modificada para aceptar parámetros) ---
        private void DrawRageBar(Player player, WakfuPlayer wakfuPlayer, Vector2 position)
        {
            int rage = wakfuPlayer.GetRageTicks();
            float progress = rage / 5f; // Asume que 5 es el máximo

            Texture2D tex = TextureAssets.MagicPixel.Value;
            int width = 100;
            int height = 10;

            // Fondo gris
            Main.spriteBatch.Draw(tex, new Rectangle((int)position.X, (int)position.Y, width, height), Color.Gray * 0.8f);

            // Barra de progreso
            Color rageColor = Color.Lerp(Color.Orange, Color.Red, progress);
            Main.spriteBatch.Draw(tex, new Rectangle((int)position.X, (int)position.Y, (int)(width * progress), height), rageColor);

            // Borde opcional
             Utils.DrawBorderStringFourWay(Main.spriteBatch, FontAssets.MouseText.Value, $"Rage: {rage}/5", position.X + width/2f, position.Y + height/2f, Color.White, Color.Black, new Vector2(0.5f), 0.7f); // Texto centrado
        }

        // --- Dibujar Barra de Cooldown Habilidades (V/X) (Modificada para aceptar parámetros) ---
        private void DrawAbilityCooldownBar(Player player, WakfuPlayer wakfuPlayer, Vector2 position)
        {
            // Asume que 300 es el max cooldown de habilidades V/X
            int maxCooldown = 300; // Podrías hacerlo una constante si es fijo
            int current = wakfuPlayer.espadaCooldown;

            // No dibujar si no hay cooldown
            if (current <= 0)
                return;

            // Progreso (cuánto falta para estar listo)
            float progress = 1f - (current / (float)maxCooldown);

            Texture2D tex = TextureAssets.MagicPixel.Value;
            int width = 100;
            int height = 6; // Barra más fina

            // Fondo gris oscuro
            Main.spriteBatch.Draw(tex, new Rectangle((int)position.X, (int)position.Y, width, height), Color.DarkSlateGray * 0.8f);

            // Barra de progreso (azul?)
            Main.spriteBatch.Draw(tex, new Rectangle((int)position.X, (int)position.Y, (int)(width * progress), height), Color.SkyBlue);

             // Texto opcional (segundos restantes)
             float secondsLeft = current / 60f;
             Utils.DrawBorderStringFourWay(Main.spriteBatch, FontAssets.MouseText.Value, $"God Skill: {secondsLeft:F1}s", position.X + width/2f, position.Y + height/2f, Color.White, Color.Black, new Vector2(0.5f), 0.6f);
        }

        // --- NUEVA Función para Dibujar Barra de Cooldown de la Espada (Clic Derecho) ---
        private void DrawSwordCooldownBar(Player player, YopukaWeaponCDPlayer cdPlayer, Vector2 position)
        {
            // Usa la constante definida en la clase del arma para el máximo cooldown
            int maxCooldown = YopukaShockwaveSword.AltAttackCooldownTicks; // Accede a la constante
            int current = cdPlayer.AltAttackSwordCooldown;

            // No dibujar si no hay cooldown
            if (current <= 0)
                return;

            // Progreso (cuánto falta para estar listo)
            float progress = 1f - (current / (float)maxCooldown);

            Texture2D tex = TextureAssets.MagicPixel.Value;
            int width = 100;
            int height = 6; // Barra fina igual

            // Fondo (un gris diferente?)
            Main.spriteBatch.Draw(tex, new Rectangle((int)position.X, (int)position.Y, width, height), Color.DimGray * 0.8f);

            // Barra de progreso (un color diferente, ej. Naranja/Rojo?)
            Main.spriteBatch.Draw(tex, new Rectangle((int)position.X, (int)position.Y, (int)(width * progress), height), Color.BlueViolet);

             // Texto opcional (segundos restantes)
             float secondsLeft = current / 60f;
             Utils.DrawBorderStringFourWay(Main.spriteBatch, FontAssets.MouseText.Value, $"Slash: {secondsLeft:F1}s", position.X + width/2f, position.Y + height/2f, Color.White, Color.Black, new Vector2(0.5f), 0.6f); // Texto diferente
        }
    }
}