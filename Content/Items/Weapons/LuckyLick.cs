using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.DataStructures;
using WakfuMod.Content.Projectiles; // Asegúrate que este namespace es correcto

namespace WakfuMod.Content.Items.Weapons
{
    public class LuckyLick : ModItem
    {
        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("Un arma viviente con el espíritu del Zurcarák.\n'¿Necesitas un golpe de suerte... o una buena lamida?'");
        }

        public override void SetDefaults()
        {
            Item.damage = 1; // El daño de las pulgas es simbólico
            Item.DamageType = DamageClass.Summon; // Encaja temáticamente con "invocar" pulgas
            Item.mana = 5;
            Item.width = 40;
            Item.height = 40;
            Item.useTime = 120; // Velocidad de ataque muy baja
            Item.useAnimation = 120;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 0.5f;
            Item.value = Item.sellPrice(gold: 2);
            Item.rare = ItemRarityID.Green;
            Item.UseSound = SoundID.Item20; // Sonido de "pop" suave
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<EcaflipFlea>();
            Item.shootSpeed = 4f;
        }

        public override bool AltFunctionUse(Player player)
        {
            return true; // Habilitar clic derecho
        }

        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2) // Modo Clic Derecho (Lengüetazo)
            {
                Item.useTime = 60;
                Item.useAnimation = 60;
                Item.mana = 100;
                Item.shoot = ModContent.ProjectileType<EcaflipTongueLash>();
                Item.shootSpeed = 1f; // La velocidad no importa mucho, el proyectil se controla solo
                Item.UseSound = SoundID.Item1; // Sonido de swing
            }
            else // Modo Clic Izquierdo (Pulgas)
            {
                Item.useTime = 45;
                Item.useAnimation = 45;
                Item.mana = 2;
                Item.shoot = ModContent.ProjectileType<EcaflipFlea>();
                Item.shootSpeed = 8f;
                Item.UseSound = SoundID.Item20;
            }
            return base.CanUseItem(player);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2) // Clic Derecho
            {
                // Dispara un solo lengüetazo
                Projectile.NewProjectile(source, player.Center, velocity, type, damage, knockback, player.whoAmI);
            }
            else // Clic Izquierdo
            {
                // Dispara 5 pulgas en un cono
                int numberOfFleas = 5;
                for (int i = 0; i < numberOfFleas; i++)
                {
                    Vector2 perturbedSpeed = velocity.RotatedByRandom(MathHelper.ToRadians(20)); // Dispersión de 20 grados
                    Projectile.NewProjectile(source, position, perturbedSpeed, type, damage, knockback, player.whoAmI);
                }
            }
            return false; // Prevenir el disparo por defecto de Terraria
        }
    }
}