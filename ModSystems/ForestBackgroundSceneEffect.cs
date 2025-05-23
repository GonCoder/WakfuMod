using Terraria;
using Terraria.ModLoader;
using WakfuMod.Content.Backgrounds; // Necesario para acceder a tu ModSurfaceBackgroundStyle

namespace WakfuMod.ModSystems // Reemplaza con tu namespace
{
    public class ForestBackgroundSceneEffect : ModSceneEffect
    {
        public override bool IsSceneEffectActive(Player player)
        {
             return player.ZoneForest && (player.ZoneOverworldHeight || player.ZoneSkyHeight) && !player.ZoneDirtLayerHeight && !player.ZoneRockLayerHeight;
        }

        // --- MANTENER ESTO: Asigna la instancia del estilo ---
        public override ModSurfaceBackgroundStyle SurfaceBackgroundStyle => ModContent.GetInstance<MyForestBackgroundStyle>();

        // Opcional: Puedes cambiar otras cosas aquí también
        // public override int Music => MusicLoader.GetMusicSlot(Mod, "Assets/Music/MyForestTheme");
        // public override SceneEffectPriority Priority => SceneEffectPriority.BiomeMedium;
        // public override void SpecialVisuals(Player player, bool isActive) { ... }
    }
}