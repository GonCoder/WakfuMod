using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.DataStructures;
using ReLogic.Content;

namespace WakfuMod.jugador
{
    public class HipermagoRuneDrawLayer : PlayerDrawLayer
    {
        // Sprites de las runas
        private static Asset<Texture2D> _fireRuneTexture;
        private static Asset<Texture2D> _earthRuneTexture;
        private static Asset<Texture2D> _airRuneTexture;
        private static Asset<Texture2D> _waterRuneTexture;
        
        public override void Load()
        {
            _fireRuneTexture = ModContent.Request<Texture2D>("WakfuMod/Assets/HUD/Runes/FireRune");
            _earthRuneTexture = ModContent.Request<Texture2D>("WakfuMod/Assets/HUD/Runes/EarthRune");
            _airRuneTexture = ModContent.Request<Texture2D>("WakfuMod/Assets/HUD/Runes/AirRune");
            _waterRuneTexture = ModContent.Request<Texture2D>("WakfuMod/Assets/HUD/Runes/WaterRune");
        }
        
        public override Position GetDefaultPosition() => new AfterParent(Terraria.DataStructures.PlayerDrawLayers.Head);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            var wakfuPlayer = drawInfo.drawPlayer.GetModPlayer<WakfuPlayer>();
            return wakfuPlayer.claseElegida == WakfuClase.Hipermago && wakfuPlayer.GetTotalRunes() > 0;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            var player = drawInfo.drawPlayer;
            var wakfuPlayer = player.GetModPlayer<WakfuPlayer>();
            
            int totalRunes = wakfuPlayer.GetTotalRunes();
            if (totalRunes <= 0) return;

            // Posición base encima de la cabeza del jugador
            Vector2 basePos = player.Top - Main.screenPosition;
            basePos.Y -= 25; // Un poco más arriba de la cabeza
            
            int runeSize = 20; // Tamaño del sprite de runa
            int spacing = 6;
            int totalWidth = (runeSize * totalRunes) + (spacing * (totalRunes - 1));
            float startX = basePos.X - totalWidth / 2f;
            
            int runeIndex = 0;
            
            // Dibujar runas de fuego
            for (int i = 0; i < wakfuPlayer.hipermagoFireRunes; i++)
            {
                DrawRune(ref drawInfo, _fireRuneTexture, startX, basePos.Y, runeIndex, runeSize, spacing);
                runeIndex++;
            }
            
            // Dibujar runas de tierra
            for (int i = 0; i < wakfuPlayer.hipermagoEarthRunes; i++)
            {
                DrawRune(ref drawInfo, _earthRuneTexture, startX, basePos.Y, runeIndex, runeSize, spacing);
                runeIndex++;
            }
            
            // Dibujar runas de aire
            for (int i = 0; i < wakfuPlayer.hipermagoAirRunes; i++)
            {
                DrawRune(ref drawInfo, _airRuneTexture, startX, basePos.Y, runeIndex, runeSize, spacing);
                runeIndex++;
            }
            
            // Dibujar runas de agua
            for (int i = 0; i < wakfuPlayer.hipermagoWaterRunes; i++)
            {
                DrawRune(ref drawInfo, _waterRuneTexture, startX, basePos.Y, runeIndex, runeSize, spacing);
                runeIndex++;
            }
        }
        
        private void DrawRune(ref PlayerDrawSet drawInfo, Asset<Texture2D> runeTexture, float startX, float baseY, int index, int size, int spacing)
        {
            if (runeTexture == null || !runeTexture.IsLoaded) return;
            
            Texture2D tex = runeTexture.Value;
            Vector2 runePos = new Vector2(startX + index * (size + spacing), baseY);
            
            // Efecto de brillo/pulsación
            float pulse = 1f + 0.15f * (float)System.Math.Sin(Main.GameUpdateCount * 0.1f + index);
            float scale = (float)size / tex.Width * pulse;
            
            // Centro del sprite
            Vector2 origin = new Vector2(tex.Width / 2f, tex.Height / 2f);
            Vector2 drawPos = runePos + new Vector2(size / 2f, size / 2f);
            
            drawInfo.DrawDataCache.Add(new DrawData(
                tex, 
                drawPos, 
                null, 
                Color.White * 0.95f, 
                0f, 
                origin, 
                scale, 
                SpriteEffects.None, 
                0
            ));
        }
    }
}
