using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace WakfuMod.Content.Buffs
{
    public class BlindedDebuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = false;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            // El NPC se mueve de forma errática (ceguera)
            if (Main.rand.NextBool(3)) // Cada 3 ticks aproximadamente
            {
                // Cambiar dirección aleatoriamente
                npc.velocity.X += Main.rand.NextFloat(-2f, 2f);
                npc.velocity.Y += Main.rand.NextFloat(-1f, 1f);
                
                // Limitar velocidad
                if (npc.velocity.Length() > 6f)
                {
                    npc.velocity.Normalize();
                    npc.velocity *= 6f;
                }
            }
            
            // Ocasionalmente cambiar dirección completamente
            if (Main.rand.NextBool(30))
            {
                npc.velocity.X *= -1;
            }
            
            // Efecto visual de confusión/vapor
            if (Main.rand.NextBool(5))
            {
                int dust = Dust.NewDust(npc.position, npc.width, npc.height, DustID.Cloud, 
                    Main.rand.NextFloat(-1f, 1f), -1f, 150, default, 0.8f);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}
