using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace WakfuMod.Content.Buffs
{
    public class UginakWuauBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = true;
            Main.vanityPet[Type] = true; // Es como una pet
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.buffTime[buffIndex] = 18000;
            var wakfuPlayer = player.GetModPlayer<jugador.WakfuPlayer>();
            
            // Marcar que tiene el minion activo (similar a Zurcarac)
            bool hasMinion = false;

            // Buscar si ya existe el Wuau
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].active &&
                    Main.projectile[i].owner == player.whoAmI &&
                    Main.projectile[i].type == ModContent.ProjectileType<Content.Projectiles.UginakWuauMinion>())
                {
                    hasMinion = true;
                    break;
                }
            }

            // Si no existe, invocarlo
            if (!hasMinion && player.whoAmI == Main.myPlayer)
            {
                Projectile.NewProjectile(
                    player.GetSource_Buff(buffIndex),
                    player.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<Content.Projectiles.UginakWuauMinion>(),
                    0,
                    0f,
                    player.whoAmI
                );
            }
        }
    }
}
