using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;
using WakfuMod.jugador;
using System.Collections.Generic;

namespace WakfuMod.jugador
{
    public class ZurcaDadoHUDcd : ModSystem
    {
        private LegacyGameInterfaceLayer _dieCooldownLayer;
        private LegacyGameInterfaceLayer _frenzyCooldownLayer; // <-- NUEVA CAPA para Habilidad 1

        public override void Load()
        {
            _dieCooldownLayer = new LegacyGameInterfaceLayer(
                "WakfuMod: Zurcarak Die Cooldown",
                DrawDieCooldownBar,
                InterfaceScaleType.UI);

            // Cargar la nueva capa para el Arañazo Loco
            _frenzyCooldownLayer = new LegacyGameInterfaceLayer(
                "WakfuMod: Zurcarak Frenzy Cooldown",
                DrawFrenzyCooldownBar, // Nuevo método de dibujo
                InterfaceScaleType.UI);
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int resourceBarIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Resource Bars"));
            if (resourceBarIndex != -1)
            {
                // Insertar ambas capas, una después de la otra
                layers.Insert(resourceBarIndex + 1, _dieCooldownLayer);
                layers.Insert(resourceBarIndex + 2, _frenzyCooldownLayer); // Insertar la nueva capa
            }
        }

        // --- Método para el DADO (Habilidad 2) ---
        private bool DrawDieCooldownBar()
        {
            // (Este método se queda exactamente como estaba)
            if (Main.gameMenu || Main.dedServ) return true;
            Player player = Main.LocalPlayer;
            WakfuPlayer wp = player.GetModPlayer<WakfuPlayer>();
            if (wp.claseElegida != WakfuClase.Zurcarac || wp.zurcarakAbility2Cooldown <= 0) return true;

            float maxCooldown = WakfuPlayer.ZurcarakAbility2BaseCooldown;
            float remainingCooldown = wp.zurcarakAbility2Cooldown;
            float progress = remainingCooldown / maxCooldown;

            Texture2D barTexture = TextureAssets.MagicPixel.Value;
            Vector2 position = new Vector2(Main.screenWidth / 2f + 50, Main.screenHeight - 20); // esta es la posición de la barra, +50 hacia la derecha desde el centro y -20 hacia arriba desde abajo
            int width = 100;
            int height = 10;
            Color barColor = Color.Pink;

            Main.spriteBatch.Draw(barTexture, new Rectangle((int)position.X, (int)position.Y, width, height), Color.DarkGoldenrod * 0.5f);
            Main.spriteBatch.Draw(barTexture, new Rectangle((int)position.X, (int)position.Y, (int)(width * progress), height), barColor);

            float secondsLeft = remainingCooldown / 60f;
            Utils.DrawBorderStringFourWay(Main.spriteBatch, FontAssets.MouseText.Value, $"Dados/Dice: {secondsLeft:F1}s", position.X + width / 2f, position.Y + height / 2f, Color.White, Color.Black, new Vector2(0.5f), 0.7f);

            return true;
        }

        // --- NUEVO MÉTODO para el ARAÑAZO LOCO (Habilidad 1) ---
        private bool DrawFrenzyCooldownBar()
        {
            if (Main.gameMenu || Main.dedServ) return true;

            Player player = Main.LocalPlayer;
            WakfuPlayer wp = player.GetModPlayer<WakfuPlayer>();

            // Solo dibujar si es Zurcarák Y el cooldown de la habilidad 1 está activo
            if (wp.claseElegida != WakfuClase.Zurcarac || wp.zurcarakAbility1Cooldown <= 0)
            {
                return true;
            }

            // Acceder a los valores del Cooldown de la HABILIDAD 1
            float maxCooldown = WakfuPlayer.ZurcarakAbility1BaseCooldown;
            float remainingCooldown = wp.zurcarakAbility1Cooldown;
            float progress = remainingCooldown / maxCooldown;

            // --- Definir Posición y Tamaño de la Barra ---
            Texture2D barTexture = TextureAssets.MagicPixel.Value;
            // Posicionar esta barra a la izquierda para no solaparse con la del dado
            Vector2 position = new Vector2(Main.screenWidth / 2f - 180, Main.screenHeight - 20); // esta es la posición de la barra, -150 hacia la derecha desde el centro y -20 hacia arriba desde abajo
            int width = 100;
            int height = 10;
            Color barColor = Color.IndianRed; // Un color diferente, rojizo para el frenesí

            // --- Dibujar los Elementos ---
            Main.spriteBatch.Draw(barTexture, new Rectangle((int)position.X, (int)position.Y, width, height), Color.DarkRed * 0.5f);
            Main.spriteBatch.Draw(barTexture, new Rectangle((int)position.X, (int)position.Y, (int)(width * progress), height), barColor);

            float secondsLeft = remainingCooldown / 60f;
            Utils.DrawBorderStringFourWay(
                Main.spriteBatch,
                FontAssets.MouseText.Value,
                $"Frenzy: {secondsLeft:F1}s", // Texto diferente
                position.X + width / 2f,
                position.Y + height / 2f,
                Color.White,
                Color.Black,
                new Vector2(0.5f),
                0.7f
            );

            return true;
        }
    }
}