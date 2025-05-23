using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace WakfuMod.Content.Backgrounds // Asegúrate que esta es la ruta correcta
{
    // IMPORTANTE: El namespace y la ubicación del archivo SÍ importan para Autoloading.
    // Este archivo DEBE estar dentro de una carpeta llamada "Backgrounds" en tu estructura de Content.
    // Ejemplo: TuMod/Content/Backgrounds/MyForestBackgroundStyle.cs

    public class MyForestBackgroundStyle : ModSurfaceBackgroundStyle
    {

        public override int ChooseFarTexture()
        {
            // Apunta a la nueva ubicación en Assets
            return ModContent.GetModBackgroundSlot($"{Mod.Name}/Assets/Backgrounds/SadidaFar");
            // O si los moviste a Assets/Backgrounds/Sadida/:
            // return ModContent.GetModBackgroundSlot($"{Mod.Name}/Assets/Backgrounds/Sadida/SadidaFar");
        }

        public override int ChooseMiddleTexture()
        {
            return ModContent.GetModBackgroundSlot($"{Mod.Name}/Assets/Backgrounds/SadidaMid");
            // O si los moviste a Assets/Backgrounds/Sadida/:
            // return ModContent.GetModBackgroundSlot($"{Mod.Name}/Assets/Backgrounds/Sadida/SadidaMid");
        }

        public override int ChooseCloseTexture(ref float scale, ref double parallax, ref float a, ref float b)
        {
            return ModContent.GetModBackgroundSlot($"{Mod.Name}/Assets/Backgrounds/SadidaClose");
            // O si los moviste a Assets/Backgrounds/Sadida/:
            // return ModContent.GetModBackgroundSlot($"{Mod.Name}/Assets/Backgrounds/Sadida/SadidaClose");
        }

        // --- Otros Overrides ---
        public override void ModifyFarFades(float[] fades, float transitionSpeed) { }
        // public override Color AverageColor => new Color(100, 150, 255);
    }
}