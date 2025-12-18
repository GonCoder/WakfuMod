using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.DataStructures;
using WakfuMod.jugador;

namespace WakfuMod.Content.Items.Weapons
{
    public class HipermagoAirWaterStaff : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 40;
            Item.height = 40;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.autoReuse = false;
            Item.DamageType = DamageClass.Ranged; // Escala con daño a distancia
            Item.damage = 15; // Daño base (aire)
            Item.knockBack = 8f; // Alto knockback para el tornado
            Item.noMelee = true;
            Item.noUseGraphic = true; // No mostrar el arma cuando se usa (es magia)
            Item.value = Item.buyPrice(gold: 5);
            Item.rare = ItemRarityID.Cyan;
            Item.shoot = ModContent.ProjectileType<Projectiles.HipermagoTornado>();
            Item.shootSpeed = 10f;
            Item.channel = false;
            Item.useTurn = true;
            Item.staff[Item.type] = true;
        }

        public override bool AltFunctionUse(Player player)
        {
            return true; // Habilitar clic derecho
        }

        public override bool CanUseItem(Player player)
        {
            var wakfuPlayer = player.GetModPlayer<WakfuPlayer>();
            
            // Solo el Hipermago puede usar esta arma
            if (wakfuPlayer.claseElegida != WakfuClase.Hipermago)
            {
                return false;
            }

            if (player.altFunctionUse == 2)
            {
                // Clic derecho: Agua
                return wakfuPlayer.hipermagoWaterCooldown <= 0;
            }
            else
            {
                // Clic izquierdo: Aire
                return wakfuPlayer.hipermagoAirCooldown <= 0;
            }
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            var wakfuPlayer = player.GetModPlayer<WakfuPlayer>();

            if (player.altFunctionUse == 2)
            {
                // --- CLIC DERECHO: Ataque de Agua (Proyectil del Ice Rod vanilla) ---
                Vector2 direction = Main.MouseWorld - player.Center;
                direction.Normalize();
                
                // Usar proyectil del Ice Rod vanilla (IceBlock)
                int projType = ProjectileID.IceBlock;
                Projectile.NewProjectile(
                    source,
                    player.Center,
                    direction * 12f,
                    projType,
                    20, // 20 de daño
                    knockback * 0.5f,
                    player.whoAmI
                );
                
                // Añadir runa de agua
                wakfuPlayer.AddRune("water");
                
                // Cooldown
                wakfuPlayer.hipermagoWaterCooldown = WakfuPlayer.HipermagoWaterBaseCooldown;
                
                return false;
            }
            else
            {
                // --- CLIC IZQUIERDO: Ataque de Aire (Tornado vanilla) ---
                Vector2 direction = Main.MouseWorld - player.Center;
                direction.Normalize();
                
                // Usar proyectil FairyQueenRangedItemShot (similar wind effect)
                int projType = ProjectileID.DD2ApprenticeStorm;
                Projectile.NewProjectile(
                    source,
                    player.Center,
                    direction * 10f,
                    projType,
                    15, // 15 de daño
                    knockback * 2f, // Alto knockback
                    player.whoAmI
                );
                
                // Añadir runa de aire
                wakfuPlayer.AddRune("air");
                
                // Cooldown
                wakfuPlayer.hipermagoAirCooldown = WakfuPlayer.HipermagoAirBaseCooldown;
                
                return false;
            }
        }

        public override void ModifyTooltips(System.Collections.Generic.List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "AirWaterInfo", 
                "[c/87CEEB:Left Click]: Tornado (15 dmg, pushes enemies, 1s CD)\n" +
                "[c/1E90FF:Right Click]: Ice Shard (20 dmg, slows enemies, 2s CD)\n" +
                "[c/FF00FF:Generates elemental runes for combos!]"));
        }
    }
}
