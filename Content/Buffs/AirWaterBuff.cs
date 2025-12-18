using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace WakfuMod.Content.Buffs
{
    public class AirWaterBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
            Main.debuff[Type] = false; // Es un buff positivo
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // Velocidad de movimiento +40%
            player.moveSpeed += 0.4f;
            player.maxRunSpeed += 3f;
            player.runAcceleration += 0.1f;
            
            // Velocidad de ataque +20%
            player.GetAttackSpeed(DamageClass.Generic) += 0.2f;
            
            // Daño +15%
            player.GetDamage(DamageClass.Generic) += 0.15f;
            
            // Críticos +10% probabilidad
            player.GetCritChance(DamageClass.Generic) += 10f;
            
            // Daño crítico +50%
            player.GetModPlayer<jugador.WakfuPlayer>().critDamageBonus += 0.5f;
            
            // Regeneración de vida
            player.lifeRegen += 8;
            
            // Efecto visual de viento/agua
            if (Main.rand.NextBool(5))
            {
                int dustType = Main.rand.NextBool() ? DustID.Cloud : DustID.Water;
                int dust = Dust.NewDust(player.position, player.width, player.height, 
                    dustType, Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-2f, 0f), 100, default, 0.8f);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}
