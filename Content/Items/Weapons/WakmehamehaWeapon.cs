// WakmehamehaWeapon.cs (Content/Items/Weapons)
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
// using Microsoft.Xna.Framework.Graphics;
using Terraria.DataStructures;
using WakfuMod.Content.Projectiles; // Asegúrate que se incluye el namespace
using Terraria.Audio;
using System.Collections.Generic;

namespace WakfuMod.Content.Items.Weapons
{
    public class WakmehamehaWeapon : ModItem
    {
        // --- CONSTANTES DE USO ---
        private const int LeftClickUseTime = 60; // Más lento que antes
        private const float LeftClickShootSpeed = 30f; // Velocidad del proyectil líder invisible
        private const int RightClickUseTime = 36; // Carga del clic derecho

        public override void SetStaticDefaults()
        {
            ItemID.Sets.AnimatesAsSoul[Item.type] = true;
            Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(6, 4));
        }

        public override void SetDefaults()
        {
            // Base stats
            Item.damage = 1; // El daño lo hereda el RASTRO
            Item.DamageType = DamageClass.Ranged; // O Magic
            Item.width = 28;
            Item.height = 28;
            Item.scale = 0.50f;
            Item.knockBack = 0f;
            Item.value = Item.sellPrice(gold: 5);
            Item.rare = ItemRarityID.Cyan;

            // --- Defaults para Clic Izquierdo ---
            Item.useTime = LeftClickUseTime;
            Item.useAnimation = 10;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = false; // Puede mantenerse presionado
            Item.shoot = ModContent.ProjectileType<WakmehamehaLeaderProjectile>(); // Dispara el líder
            Item.shootSpeed = LeftClickShootSpeed; // Velocidad ALTA para el líder

            Item.noMelee = true;
            Item.useAmmo = AmmoID.None;
            Item.UseSound = null; // Sonido en Shoot
            Item.noUseGraphic = false; // El jugador sí muestra el arma al disparar este tipo
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            // Configuración específica para cada clic
            if (player.altFunctionUse == 2) // Clic Derecho (Exploding Beam)
            {
                // Previene si ya existe el proyectil de explosión
                 if (player.ownedProjectileCounts[ModContent.ProjectileType<WakmehamehaProjectile>()] > 0)
                     return false;

                Item.useTime = RightClickUseTime;
                Item.useAnimation = RightClickUseTime;
                Item.autoReuse = false;
                Item.shoot = ModContent.ProjectileType<WakmehamehaProjectile>();
                Item.shootSpeed = 0f; // El proyectil cargado no necesita velocidad inicial
                Item.noUseGraphic = false; // Oculta el arma durante la carga del rayo original
            }
            else // Clic Izquierdo (Leader + Trail)
            {
                 // Previene si ya existe el proyectil LÍDER
                 if (player.ownedProjectileCounts[ModContent.ProjectileType<WakmehamehaLeaderProjectile>()] > 0)
                     return false;
                 // Opcional: ¿Prevenir si también existe el de clic derecho?
                 // if (player.ownedProjectileCounts[ModContent.ProjectileType<WakmehamehaProjectile>()] > 0)
                 //    return false;

                Item.useTime = LeftClickUseTime;
                Item.useAnimation = 20;
                Item.autoReuse = true;
                Item.shoot = ModContent.ProjectileType<WakmehamehaLeaderProjectile>();
                Item.shootSpeed = LeftClickShootSpeed;
                Item.noUseGraphic = false; // Muestra el arma al disparar el líder
            }
            return true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // 'type' ya viene configurado desde CanUseItem según el clic

            if (player.altFunctionUse == 2) // Clic Derecho (Proyectil original)
            {
                // Spawnea WakmehamehaProjectile con velocidad CERO
                Projectile.NewProjectile(source, player.Center, Vector2.Zero, type, damage, knockback, player.whoAmI);
                SoundEngine.PlaySound(SoundID.Item13 with { Volume = 1.5f, Pitch = -0.5f }, player.position); // Sonido carga
            }
            else // Clic Izquierdo (Proyectil líder)
            {
                // Spawnea WakmehamehaLeaderProjectile con la velocidad calculada hacia el cursor
                Projectile.NewProjectile(source, player.Center, velocity, type, damage, knockback, player.whoAmI);
                SoundEngine.PlaySound(SoundID.Item12, player.position); // Sonido disparo láser simple (ejemplo)
            }

            return false; // Previene disparo vanilla
        }


        // HoldItem: Ajusta la rotación del jugador
        public override void HoldItem(Player player)
        {
             // Solo si el jugador local está controlando y no está en animación de carga del clic derecho
             bool isLoadingRightClick = player.altFunctionUse == 2 && player.itemAnimation > 0 && Item.shoot == ModContent.ProjectileType<WakmehamehaProjectile>();

             if (Main.myPlayer == player.whoAmI && !isLoadingRightClick)
             {
                 Vector2 aimDirection = (Main.MouseWorld - player.RotatedRelativePoint(player.MountedCenter, true)).SafeNormalize(Vector2.UnitX);
                 player.itemRotation = aimDirection.ToRotation();
                 if (player.direction == -1) player.itemRotation += MathHelper.Pi;
                 if (player.gravDir == -1f) player.itemRotation = -player.itemRotation;
             }
        }

        // ModifyTooltips: Actualiza para reflejar el nuevo comportamiento
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            int index = tooltips.FindIndex(tip => tip.Mod == "Terraria" && tip.Name == "Damage");
            if (index == -1) index = tooltips.Count - 1;
            // Añadir descripciones de daño %
    // Puedes ajustar estos porcentajes aquí y en los proyectiles
    float trailPercent = 0.3f; // Ejemplo: 0.5% para el rastro
    float beamExplosionPercent = 9f; // Ejemplo: 2.5% para la explosión de portales

    tooltips.Insert(index + 1, new TooltipLine(Mod, "WakmehamehaMode1", $"Left-click: Fires a projectile leaving a trail ({trailPercent}% max life / hit)"));
    tooltips.Insert(index + 2, new TooltipLine(Mod, "WakmehamehaMode2", $"Right-click: Charges a beam (5dmg +0.5%hp) that explodes portals\n ({beamExplosionPercent}% max life) per portal"));
    tooltips.Insert(index + 3, new TooltipLine(Mod, "WakmehamehaDamageInfo", "Left click Damage ignores defense. Uses portals."));
    tooltips.Insert(index + 3, new TooltipLine(Mod, "WakmehamehaDamageInfo", "If you use ctrl cursor you cant do right click if there is a door or interactables"));
    tooltips.Insert(index + 4, new TooltipLine(Mod, "DamageClassName", $"[c/FF8C00:Ranged Damage]")); // Color naranja para Ranged
        }
        // AddRecipes...
    }
}