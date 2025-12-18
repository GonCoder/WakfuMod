using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace WakfuMod.Content.Buffs
{
    public class EarthWaterBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
            Main.debuff[Type] = false; // Es un buff positivo
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // Defensa +30
            player.statDefense += 30;
            
            // Anti-knockback
            player.noKnockback = true;
            
            // Inmunidad a lava
            player.lavaImmune = true;
            player.fireWalk = true;
            
            // Regeneración de vida potente
            player.lifeRegen += 12;
            
            // Reducción de daño
            player.endurance += 0.1f; // 10% menos daño recibido
            
            // Efecto visual de tierra/agua (barro protector)
            if (Main.rand.NextBool(6))
            {
                int dustType = Main.rand.NextBool() ? DustID.Dirt : DustID.Water;
                int dust = Dust.NewDust(player.position, player.width, player.height, 
                    dustType, 0, 0, 100, default, 0.7f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.3f;
            }
        }
    }
}
