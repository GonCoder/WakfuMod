// Content/Items/Weapons/TymadorKick.cs Debería llamarse Kick-u
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.DataStructures;
using WakfuMod.Content.Projectiles; // Para el proyectil de la patada
using WakfuMod.jugador;             // Para WakfuPlayer (si es necesario para ocultar)
using Terraria.Audio;
using System; // Para Math.Sign

namespace WakfuMod.Content.Items.Weapons // Reemplaza WakfuMod si es necesario
{
    public class TymadorKick : ModItem
    {
        // --- Constantes de Patada ---
        // Floja (Clic Izquierdo)
        private const int KickWeak_UseTime = 35;
        private const int KickWeak_Damage = 5;
        private const float KickWeak_Knockback = 6f;
        private const float KickWeak_BombForce = 10f; // Fuerza de impulso a la bomba

        // Fuerte (Clic Derecho)
        private const int KickStrong_UseTime = 55; // Más lenta
        private const int KickStrong_Damage = 8; // Más daño
        private const float KickStrong_Knockback = 12f; // Más knockback
        private const float KickStrong_BombForce = 18f; // Más fuerza a la bomba

        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("Una patada rápida y engañosa.\nClick Izquierdo: Patada rápida que empuja bombas ligeramente.\nClick Derecho: Patada potente que lanza bombas lejos.");
        }

        public override void SetDefaults()
        {
            // --- Estadísticas Base (se ajustan en CanUseItem) ---
            Item.damage = KickWeak_Damage; // Daño base (débil)
            Item.DamageType = DamageClass.Melee;
            Item.width = 30; // Tamaño del ítem en inventario
            Item.height = 30;
            Item.knockBack = KickWeak_Knockback; // Knockback base (débil)

            // --- Uso ---
            Item.useTime = KickWeak_UseTime; // Tiempo de uso base (débil)
            Item.useAnimation = KickWeak_UseTime;
            Item.useStyle = ItemUseStyleID.Shoot; // Usamos Shoot para controlar el spawn del proyectil
            Item.UseSound = SoundID.Item1; // Sonido básico de swing/patada
            Item.autoReuse = true; // Permitir mantener presionado
            

            // --- IMPORTANTE: Arma Invisible, Proyectil hace el trabajo ---
            Item.noMelee = true; // No usa la hitbox del jugador/item
            Item.noUseGraphic = true; 
            Item.shoot = ModContent.ProjectileType<TymadorKickProjectile>(); // El proyectil que hace la acción
            Item.shootSpeed = 1f; // Debe ser > 0 para que se llame a Shoot

            // --- Otros ---
            Item.value = Item.sellPrice(silver: 40);
            Item.rare = ItemRarityID.Blue;
        }

        // Habilitar Clic Derecho
        public override bool AltFunctionUse(Player player) => true;

        // Configurar estadísticas según el clic
        public override bool CanUseItem(Player player)
        {
            // Prevenir usar si ya hay una patada activa? Opcional.
            // if (player.ownedProjectileCounts[ModContent.ProjectileType<TymadorKickProjectile>()] > 0) return false;

            if (player.altFunctionUse == 2) // Clic Derecho (Fuerte)
            {
                Item.useTime = KickStrong_UseTime;
                Item.useAnimation = KickStrong_UseTime;
                Item.damage = KickStrong_Damage;
                Item.knockBack = KickStrong_Knockback;
                 Item.UseSound = SoundID.Item1 with { Pitch = -0.6f, Volume = 2f }; // Sonido más grave/fuerte
            }
            else // Clic Izquierdo (Débil)
            {
                Item.useTime = KickWeak_UseTime;
                Item.useAnimation = KickWeak_UseTime;
                Item.damage = KickWeak_Damage;
                Item.knockBack = KickWeak_Knockback;
                Item.UseSound = SoundID.Item1 with { Pitch = 0.8f, Volume = 2f }; // Sonido más agudo/rápido
            }
            return base.CanUseItem(player); // Permite usar si pasa chequeos base
        }

        // Deshabilitar el uso de hitbox del item (el proyectil la tendrá)
        public override void UseItemHitbox(Player player, ref Rectangle hitbox, ref bool noHitbox)
        {
            noHitbox = true; // El proyectil se encarga de la hitbox
        }

        // Spawnear el proyectil de la patada
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Determinar la fuerza de la patada a las bombas
            float bombKickForce = (player.altFunctionUse == 2) ? KickStrong_BombForce : KickWeak_BombForce;

            // Spawnear el proyectil que contiene la animación y la lógica
            Projectile.NewProjectile(
                source,
                player.Center, // Origen en el jugador
                // La velocidad inicial del proyectil no importa mucho, ya que se ancla al jugador
                // Pero podemos pasar la dirección del jugador
                new Vector2(player.direction, 0f),
                ModContent.ProjectileType<TymadorKickProjectile>(),
                damage, // Daño configurado en CanUseItem
                knockback, // Knockback configurado en CanUseItem
                player.whoAmI,
                // Usar ai[0] para pasar la fuerza de la patada a la bomba
                ai0: bombKickForce,
                 // Usar ai[1] para indicar si fue clic derecho (1) o izquierdo (0) si es necesario para la animación/sonido
                 ai1: player.altFunctionUse == 2 ? 1f : 0f
            );

            // Reproducir sonido ya se hizo a través de Item.UseSound ajustado en CanUseItem

            return false; // Prevenir disparo vanilla
        }

         // (Opcional) Añadir Receta
         // public override void AddRecipes() { ... }
    }
}