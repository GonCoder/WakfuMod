using Terraria;
using Terraria.ModLoader;
// using WakfuMod.Content.Backgrounds; // Desactivado para evitar errores si borras la carpeta

namespace WakfuMod.ModSystems 
{
    public class ForestBackgroundSceneEffect : ModSceneEffect
    {
        public override bool IsSceneEffectActive(Player player)
        {
             return false; // Desactivado completamente
        }

        // --- MANTENER ESTO: Asigna la instancia del estilo ---
        // public override ModSurfaceBackgroundStyle SurfaceBackgroundStyle => ModContent.GetInstance<MyForestBackgroundStyle>();

        // Corregido: La prioridad debe ser mayor que 'None' para sobrescribir el bioma vanilla
        public override SceneEffectPriority Priority => SceneEffectPriority.None;
    }
}