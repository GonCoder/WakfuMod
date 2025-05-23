using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics; // Necesario para Texture2D y SpriteBatch
using Terraria.Audio;
using WakfuMod.Content.Projectiles;
using WakfuMod.jugador;
using Terraria.DataStructures;
using ReLogic.Content;
using System;
using System.Collections.Generic; // Necesario para WakfuPlayer

namespace WakfuMod.Content.Items.Weapons
{
    public class SteamerPistol : ModItem
    {
        // --- Variable para la Textura del Glowmask (cache) ---
        // Se carga una vez en Load para eficiencia
        public static Asset<Texture2D> SteamerPistol_Glow { get; private set; }

        // --- Número de frames y velocidad ---
        private const int FrameCount = 3;
        private const int FrameSpeed = 8; // Ticks por frame (ajustar)

        public override void Load()
        {
            // Cargar la textura del glowmask si no estamos en un servidor dedicado
            if (!Main.dedServ)
            {
                SteamerPistol_Glow = ModContent.Request<Texture2D>(Texture + "_Glow");
            }
        }

        public override void Unload()
        {
            // Descargar la textura al salir
            SteamerPistol_Glow = null;
        }

        public override void SetDefaults()
        {
            Item.damage = 1;
            Item.DamageType = DamageClass.Summon;
            Item.width = 10; // Ajusta al tamaño real de tu sprite
            Item.height = 6; // Ajusta al tamaño real de tu sprite
            Item.useTime = 10;
            Item.useAnimation = 10;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 2f;
            Item.value = Item.buyPrice(0, 1, 50, 0);
            Item.rare = ItemRarityID.Purple;
            Item.UseSound = SoundID.Item11;
            Item.autoReuse = true;
            // El shoot se cambia dinámicamente en CanUseItem/Shoot
            Item.shootSpeed = 6f;
            Item.useAmmo = AmmoID.None;
            Item.useTurn = false; // Normalmente false para armas de fuego que apuntan al cursor
            Item.useStyle = ItemUseStyleID.Shoot;
            // --- AJUSTE: noUseGraphic = false (Queremos ver el arma) ---
            Item.noUseGraphic = false;
        }

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Steamer Pistol"); // Usar .hjson
            // Tooltip.SetDefault("Dispara láseres o lanza granadas adhesivas.");

            // Indica que el ítem tiene una animación y cómo se organiza
            Main.RegisterItemAnimation(Type, new DrawAnimationVertical(FrameSpeed, FrameCount));
            ItemID.Sets.AnimatesAsSoul[Type] = true; // Hace que se anime en la mano incluso sin usarlo
            // ItemID.Sets.ItemIconPulse[Type] = true; // Opcional: Efecto de pulso en el icono del inventario
        }

        public override bool AltFunctionUse(Player player) => true;

        // --- CanUseItem (MODIFICADO para establecer el shoot type antes) ---
        public override bool CanUseItem(Player player)
        {
            var wakfuPlayer = player.GetModPlayer<WakfuPlayer>();

            if (player.altFunctionUse == 2) // GRANADA
            {
                if (wakfuPlayer.steamerGranadaCooldown > 0)
                {
                    if (Main.myPlayer == player.whoAmI) // Mostrar sonido de error solo al jugador local
                        SoundEngine.PlaySound(SoundID.MenuClose  with { Volume = 0.8f, Pitch = -0.9f }, player.Center);
                    return false; // No se puede usar
                }
                // Configurar para granada (se aplicará si se usa)
                Item.useTime = 30;
                Item.useAnimation = 30;
                Item.UseSound = SoundID.Item1; // Sonido de lanzamiento suave?
                Item.shoot = ModContent.ProjectileType<SteamerGrenade>();
                Item.shootSpeed = 10f;
            }
            else // DISPARO NORMAL
            {
                Item.useTime = 30;
                Item.useAnimation = 30;
                Item.UseSound = SoundID.Item11; // Sonido láser
                Item.shoot = ModContent.ProjectileType<SteamerBullet>(); // Asegúrate que SteamerBullet existe
                                                                         //Item.shoot = ModContent.ProjectileType<LaserShot>(); // O si usabas LaserShot
                Item.shootSpeed = 20f;
            }
            // Siempre permitir que la lógica base decida (por mana, etc.)
            return base.CanUseItem(player);
        }

        // --- Shoot (MODIFICADO para aplicar cooldown granada) ---
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            var wakfuPlayer = player.GetModPlayer<WakfuPlayer>();

            // --- Calcular Punto de Origen (Punta del Cañón - Revisado) ---
            // El origen para Shoot style está más cerca de la mano. Usaremos player.RotatedRelativePoint
            // y ajustaremos un offset a lo largo de la velocidad.
            Vector2 gunOrigin = player.RotatedRelativePoint(player.MountedCenter); // Punto base donde se sostiene el arma
            float muzzleOffsetDistance = 40f * Item.scale; // Distancia desde el origen base hasta la punta (ajustar según sprite y escala)
            Vector2 muzzleOffset = Vector2.Normalize(velocity) * muzzleOffsetDistance;

            // Calcular posición final de la boca del cañón
            Vector2 spawnPosition = gunOrigin + muzzleOffset;

            // Comprobación de colisión opcional (más simple)
            // if (Collision.CanHitLine(gunOrigin, 0, 0, spawnPosition, 0, 0)) {
            //      position = spawnPosition;
            // } else {
            position = spawnPosition; // Usar siempre la posición calculada (puede atravesar bloques finos)
            // }


            if (player.altFunctionUse == 2)
            {
                wakfuPlayer.steamerGranadaCooldown = 300;
            }

            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            return false;
        }


        // --- Dibujar Glowmask (Corregido y en Mano) ---

        public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            if (SteamerPistol_Glow == null) return;
            Texture2D texture = SteamerPistol_Glow.Value;
            Color glowColor = Color.White * (drawColor.A / 255f);

            // Usar el 'frame' correcto calculado por Terraria
            spriteBatch.Draw(texture, position, frame, glowColor, 0f, origin, scale, SpriteEffects.None, 0f);
        }

        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
        {
            if (SteamerPistol_Glow == null) return;
            Texture2D texture = SteamerPistol_Glow.Value;
            Rectangle frame = Main.itemAnimations[Type]?.GetFrame(texture, -1) ?? texture.Frame(1, FrameCount);
            Vector2 origin = frame.Size() / 2f;

            // --- CORRECCIÓN POSICIÓN GLOW EN MUNDO ---
            // Calcular el centro del sprite para dibujar desde ahí con el origen correcto
            Vector2 drawPos = Item.position - Main.screenPosition + new Vector2(Item.width / 2f, Item.height - frame.Height / 2f); // Ajustado para centrar origen

            Color glowColor = Color.White;
            spriteBatch.Draw(texture, drawPos, frame, glowColor, rotation, origin, scale, SpriteEffects.None, 0f);
        }

        // --- AÑADIR HoldoutOffset para ajustar la posición en mano ---
        public override Vector2? HoldoutOffset()
        {
            // Ajusta X e Y hasta que se vea bien (negativo X = atrás, negativo Y = arriba)
            return new Vector2(-20f, 0f); // Ejemplo: 6 píxeles hacia atrás
        }
         // ModifyTooltips: Actualiza para reflejar el nuevo comportamiento
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            int index = tooltips.FindIndex(tip => tip.Mod == "Terraria" && tip.Name == "Damage");
            if (index == -1) index = tooltips.Count - 1;
            tooltips.Insert(index + 1, new TooltipLine(Mod, "WakmehamehaMode1", "Left-click: Fires stasis homing shots (1+2%HP) that ignores defense and\nput a permanent debuff -1 defense stacking to a max -100"));
            tooltips.Insert(index + 2, new TooltipLine(Mod, "WakmehamehaMode2", "Right-click: Launches a stasis grenade that bounces or stick to an enemy.\nIts slow detonating but turret can shot to it to instant detonate\nwith plus radius explosion and damage.\n20dmg or powered 40+10%maxHP"));
            tooltips.Insert(index + 3, new TooltipLine(Mod, "WakmehamehaDamageInfo", "Turret deals 1% of max hp from the enemy and concentrated laser deals 2% in 2 ticks.\nAll Steamer scales with summoning dmg."));
        }
       
    }
}