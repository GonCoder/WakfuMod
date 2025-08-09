using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace WakfuMod.ModSystems
{
    public class NoxDefeatSystem : ModSystem
    {
        public bool noxDefeated = false;

        public override void ClearWorld()
        {
            noxDefeated = false;
        }

        public override void SaveWorldData(TagCompound tag)
        {
            if (noxDefeated)
                tag["noxDefeated"] = true;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            noxDefeated = tag.ContainsKey("noxDefeated");
        }

        public static void SetNoxDefeated()
        {
            ModContent.GetInstance<NoxDefeatSystem>().noxDefeated = true;
        }
    }
}
