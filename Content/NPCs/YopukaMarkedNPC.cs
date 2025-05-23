using Terraria;
using Terraria.ModLoader;

namespace WakfuMod.Content.NPCs
{
    public class YopukaMarkedNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public bool MarkedBySword = false;
        public int MarkDuration = 0;

        public override void ResetEffects(NPC npc)
        {
            if (MarkDuration > 0)
            {
                MarkDuration--;
                if (MarkDuration <= 0)
                {
                    MarkedBySword = false;
                }
            }
        }
    }
}
