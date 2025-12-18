using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace WakfuMod.ModSystems
{
    public class NoxDefeatSystem : ModSystem
    {
        public bool noxDefeated = false;
        public int whisperersKilled = 0;

        public override void ClearWorld()
        {
            noxDefeated = false;
            whisperersKilled = 0;
        }

        public override void SaveWorldData(TagCompound tag)
        {
            if (noxDefeated)
                tag["noxDefeated"] = true;
            if (whisperersKilled > 0)
                tag["whisperersKilled"] = whisperersKilled;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            noxDefeated = tag.ContainsKey("noxDefeated");
            whisperersKilled = tag.ContainsKey("whisperersKilled") ? tag.Get<int>("whisperersKilled") : 0;
        }

        public static void SetNoxDefeated()
        {
            ModContent.GetInstance<NoxDefeatSystem>().noxDefeated = true;
        }
        
        public static void AddWhispererKill()
        {
            ModContent.GetInstance<NoxDefeatSystem>().whisperersKilled++;
        }
        
        public static int GetWhisperersKilled()
        {
            return ModContent.GetInstance<NoxDefeatSystem>().whisperersKilled;
        }

        public override void NetSend(System.IO.BinaryWriter writer)
        {
            writer.Write(noxDefeated);
            writer.Write(whisperersKilled);
        }

        public override void NetReceive(System.IO.BinaryReader reader)
        {
            noxDefeated = reader.ReadBoolean();
            whisperersKilled = reader.ReadInt32();
        }
    }
}
