using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.DataStructures;
using WakfuMod.jugador;

namespace WakfuMod.Content.Items.Weapons
{
    public class HipermagoFireEarthStaff : ModItem
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
            Item.damage = 20; // Daño base (fuego)
            Item.knockBack = 3f;
            Item.noMelee = true;
            Item.noUseGraphic = true; // No mostrar el arma cuando se usa (es magia)
            Item.value = Item.buyPrice(gold: 5);
            Item.rare = ItemRarityID.Orange;
            Item.shoot = ModContent.ProjectileType<Projectiles.HipermagoFireExplosion>();
            Item.shootSpeed = 0f; // No dispara, aparece en el cursor
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
                // Clic derecho: Tierra
                return wakfuPlayer.hipermagoEarthCooldown <= 0;
            }
            else
            {
                // Clic izquierdo: Fuego
                return wakfuPlayer.hipermagoFireCooldown <= 0;
            }
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            var wakfuPlayer = player.GetModPlayer<WakfuPlayer>();

            if (player.altFunctionUse == 2)
            {
                // --- CLIC DERECHO: Ataque de Tierra ---
                // Piedra que cae del cielo como estrella
                Vector2 targetPos = Main.MouseWorld;
                Vector2 spawnPos = new Vector2(targetPos.X + Main.rand.Next(-50, 51), targetPos.Y - 600);
                Vector2 fallVelocity = new Vector2(0, 12f); // Cae en vertical
                
                int projType = ModContent.ProjectileType<Projectiles.HipermagoEarthRock>();
                Projectile.NewProjectile(
                    source,
                    spawnPos,
                    fallVelocity,
                    projType,
                    50, // 50 de daño
                    knockback,
                    player.whoAmI,
                    targetPos.X, // ai[0] = X objetivo (para efectos)
                    targetPos.Y  // ai[1] = Y objetivo
                );
                
                // Añadir runa de tierra
                wakfuPlayer.AddRune("earth");
                
                // Cooldown
                wakfuPlayer.hipermagoEarthCooldown = WakfuPlayer.HipermagoEarthBaseCooldown;
                
                return false; // Ya spawneamos manualmente
            }
            else
            {
                // --- CLIC IZQUIERDO: Ataque de Fuego ---
                // Explosión en la posición del cursor
                Vector2 targetPos = Main.MouseWorld;
                
                int projType = ModContent.ProjectileType<Projectiles.HipermagoFireExplosion>();
                Projectile.NewProjectile(
                    source,
                    targetPos,
                    Vector2.Zero,
                    projType,
                    20, // 20 de daño
                    knockback,
                    player.whoAmI
                );
                
                // Añadir runa de fuego
                wakfuPlayer.AddRune("fire");
                
                // Cooldown
                wakfuPlayer.hipermagoFireCooldown = WakfuPlayer.HipermagoFireBaseCooldown;
                
                return false; // Ya spawneamos manualmente
            }
        }

        public override void ModifyTooltips(System.Collections.Generic.List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "FireEarthInfo", 
                "[c/FF6600:Left Click]: Fire Explosion (20 dmg, 3s CD)\n" +
                "[c/8B4513:Right Click]: Earth Rock (35 dmg, 1.5s CD)\n" +
                "[c/FF00FF:Generates elemental runes for combos!]"));
        }
    }
}
