using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.DataStructures;
using WakfuMod.Content.Projectiles;
using WakfuMod.jugador;
using Terraria.Audio;
using System;
using System.Collections.Generic;

namespace WakfuMod.Content.Items.Weapons
{
    public class YopukaShockwaveSword : ModItem
    {
        private const float ShockwaveChance = 0.20f;
        public const int AltAttackCooldownTicks = 600; // 600 son 10 segundos
                                                        // Quitar referencia a proyectil de lanza
                                                        // private const float AltAttackReach = 200f;
                                                        // private const int AltAttackProjectileType = ProjectileID.Spear;

        public override void SetDefaults()
        {
            Item.damage = 5;
            Item.DamageType = DamageClass.Melee;
            Item.width = 88;
            Item.height = 88;
            Item.useTime = 40; // Velocidad clic izquierdo
            Item.useAnimation = 40; // Animación clic izquierdo
            Item.useStyle = ItemUseStyleID.Swing; // Clic izquierdo
            Item.knockBack = 6;
            Item.value = Item.sellPrice(gold: 5);
            Item.rare = ItemRarityID.LightRed;
            Item.UseSound = SoundID.Item1; // Sonido clic izquierdo
            Item.autoReuse = true;
            Item.noUseGraphic = false; // El estado por defecto es MOSTRAR el arma

            Item.reuseDelay = 0;
            Item.useTurn = true;
            Item.useAmmo = 0;
        }

          public override bool AltFunctionUse(Player player) => true;

        // CanUseItem ahora SOLO gestiona el COOLDOWN del clic derecho
        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2) // Clic Derecho
            {
                // Solo chequea el cooldown
                return player.GetModPlayer<YopukaWeaponCDPlayer>().AltAttackSwordCooldown <= 0;
            }
            // Para el clic izquierdo, permite usar si no hay otra restricción
            return base.CanUseItem(player);
        }

        // --- UseStyle CONTROLA EL noUseGraphic FRAME A FRAME ---
        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            // Si el jugador está usando el clic derecho (altFunctionUse == 2)
            // Y la animación del item está activa (player.itemAnimation > 0)
            if (player.altFunctionUse == 2 && player.itemAnimation > 0)
            {
                Item.noUseGraphic = true; // Oculta el arma durante la animación del clic derecho
            }
            else // En cualquier otro caso (clic izquierdo o sin animación)
            {
                Item.noUseGraphic = false; // Asegura que el arma se muestre
            }
            // Llama al base.UseStyle si quieres mantener algún comportamiento visual por defecto del Swing,
            // pero probablemente no sea necesario si solo controlas noUseGraphic.
            // base.UseStyle(player, heldItemFrame);
        }


        public override bool? UseItem(Player player)
        {
            if (player.altFunctionUse == 2) // Clic Derecho
            {
                // 1. Reset Cooldowns y Max Rage
                var modPlayer = player.GetModPlayer<WakfuPlayer>();
                modPlayer.ResetYopukaAbilityCooldowns();
                modPlayer.MaximizeRage();

                // 2. Aplicar Cooldown
                player.GetModPlayer<YopukaWeaponCDPlayer>().AltAttackSwordCooldown = AltAttackCooldownTicks;

                // 3. Spawnear Slash
                int damage = (int)(Item.damage * 1.5f);
                float knockback = Item.knockBack * 1.5f;
                Vector2 direction = (Main.MouseWorld - player.Center).SafeNormalize(Vector2.UnitX); //Esto hace que apunte hacia el ratón
                float speed = 0.2f;
                Projectile.NewProjectile(
                    player.GetSource_ItemUse(Item), player.Center, direction * speed,
                    ModContent.ProjectileType<YopukaSlashProjectile>(),
                    damage, knockback, player.whoAmI, ai0: modPlayer.GetRageTicks());

                // 4. Sonido
                SoundEngine.PlaySound(SoundID.Item71, player.position);

                // 5. Animación CORTA del jugador (solo el gesto)
                // ¡IMPORTANTE! Asegúrate que la animación que pones aquí sea la correcta para el GESTO
                // del clic derecho. NO es la animación del Swing.
                player.itemAnimation = 10; // Animación corta del gesto
                player.itemAnimationMax = 10; // Debe coincidir
                player.itemTime = 10;      // Y el tiempo de uso del item
                player.ChangeDir(Math.Sign(direction.X));

                return true; // Manejamos el uso
            }

            // Clic Izquierdo: Devolvemos null para que use el useStyle = Swing normal
            // UseStyle se encargará de que noUseGraphic sea false.
            // --- YA NO NECESITAMOS CAMBIAR noUseGraphic AQUÍ ---
            return null;
        }


       // --- OnHitNPC (Clic Izquierdo - Efecto Shockwave) ---
        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Este hook es para EFECTOS *después* del golpe (como la shockwave)
            // SOLO se ejecuta para el clic izquierdo porque el clic derecho usa un proyectil.
             if (player.altFunctionUse != 2)
             {
                 var modPlayer = player.GetModPlayer<WakfuPlayer>();
                 if (Main.rand.NextFloat() < ShockwaveChance)
                 {
                     SpawnShockwaveFromPlayer(player, modPlayer.GetRageTicks(), Item.damage / 2, Item.knockBack / 2);
                 }
             }
        }

        // --- AÑADIR O MODIFICAR ESTE MÉTODO: ModifyHitNPC ---
        public override void ModifyHitNPC(Player player, NPC target, ref NPC.HitModifiers modifiers)
        {
            // Este hook modifica el DAÑO *antes* de que se aplique.
            // SOLO queremos aplicar el % vida al clic izquierdo (swing normal).
            // El clic derecho usa el ModifyHitNPC de YopukaSlashProjectile.
            if (player.altFunctionUse != 2) // Asegurarse de que es el Clic Izquierdo
            {
                // 1. Calcula el 2% de la vida máxima del NPC objetivo
                float percentDamage = target.lifeMax * 0.03f;

                // 2. Añade este daño como un bonus plano al golpe del swing
                modifiers.FlatBonusDamage += percentDamage;
                modifiers.DefenseEffectiveness *= 0f; // Ignora defensa

                // 3. (Opcional) Puedes añadir otros modificadores aquí específicos del swing
                // Ejemplo: Aumentar ligeramente el daño basado en la rabia para el swing también?
                var modPlayer = player.GetModPlayer<WakfuPlayer>();
                modifiers.FlatBonusDamage += modPlayer.GetRageTicks() * 0.5f; // Daño extra muy pequeño
            }
            // No hacemos nada si es altFunctionUse == 2, porque ese daño lo maneja el proyectil.
        }

        // --- SpawnShockwaveFromPlayer (Modificado para UNA onda desde el JUGADOR) ---
        public static void SpawnShockwaveFromPlayer(Player player, int rage, int damage, float knockback)
        {
            if (player.whoAmI != Main.myPlayer) return;
            SoundEngine.PlaySound(SoundID.Item69, player.Center);

            int direction = player.direction; // Dirección del jugador

            Vector2 groundPos = FindGroundBelow(player.Bottom); // Encontrar suelo bajo el jugador
            int shockwaveHeightEst = 10 + 50 * rage; // Estimar altura para spawn Y
            Vector2 finalSpawnPos = groundPos - new Vector2(0, shockwaveHeightEst / 2f); // Ajustar Y para centrar? O base? Probar. Podría ser 'groundPos - new Vector2(0, shockwaveHeightEst)' para la base.

            Vector2 velocity = new Vector2(direction * 6f, 0f); // Velocidad base shockwave

            Projectile.NewProjectile(
                player.GetSource_ItemUse(player.HeldItem), // Fuente
                finalSpawnPos, velocity,
                ModContent.ProjectileType<YopukaShockwaveProjectile>(),
                damage, knockback, player.whoAmI,
                ai0: rage, ai1: direction);
        }

        // --- Función Auxiliar para Encontrar Suelo (similar a la del proyectil) ---
        private static Vector2 FindGroundBelow(Vector2 position)
        {
            int tileX = (int)(position.X / 16f);
            int startTileY = (int)(position.Y / 16f);
            int endTileY = startTileY + 30; // Buscar 30 tiles abajo

            for (int y = startTileY; y < endTileY; y++)
            {
                if (WorldGen.InWorld(tileX, y, 5))
                {
                    Tile tile = Framing.GetTileSafely(tileX, y);
                    if (tile.HasTile && (Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]))
                    {
                        return new Vector2(position.X, y * 16f); // Devuelve X original, Y del borde superior del tile
                    }
                }
            }
            return position; // Devuelve posición original si no encuentra suelo
        }


        // --- Añadir Receta (Ejemplo) ---
        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.DirtBlock, 1) // Ejemplo
                // .AddIngredient(ItemID.HallowedBar, 5) // Ejemplo
                .AddTile(TileID.Anvils) // Ejemplo
                .Register();
        }

        public override void UseItemHitbox(Player player, ref Rectangle hitbox, ref bool noHitbox)
        {
            // Solo queremos modificar la hitbox del clic izquierdo (el swing normal)
            if (player.altFunctionUse != 2)
            {
                // --- Define las nuevas dimensiones deseadas para el swing ---
                // Puedes basarte en el tamaño original o poner valores fijos.
                // Queremos que sea más ancho. El ancho original implícito se basa en Item.width/height/scale.
                int desiredWidth = 160; // Ajusta este valor para la anchura deseada (ej. casi el doble de 88)
                int desiredHeight = 90; // Puedes ajustar la altura también si quieres un arco más plano o alto

                // Mantenemos el centro original de la hitbox calculada por Terraria
                // para que la posición relativa al jugador durante el swing sea correcta.
                Vector2 originalCenter = hitbox.Center();

                // Recalculamos las coordenadas X, Y basadas en el nuevo tamaño y el centro original
                hitbox.X = (int)(originalCenter.X - desiredWidth / 2f);
                hitbox.Y = (int)(originalCenter.Y - desiredHeight / 2f);

                // Establecemos el nuevo tamaño
                hitbox.Width = desiredWidth;
                hitbox.Height = desiredHeight;

                // Ahora la 'hitbox' que Terraria usará para las colisiones durante este frame
                // del swing tendrá estas nuevas dimensiones.
            }
            // No hacemos nada si es el clic derecho (altFunctionUse == 2),
            // ya que ese ataque usa un proyectil y no la hitbox del item.
        }
          // ModifyTooltips: Actualiza para reflejar el nuevo comportamiento
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            int index = tooltips.FindIndex(tip => tip.Mod == "Terraria" && tip.Name == "Damage");
            if (index == -1) index = tooltips.Count - 1;
            tooltips.Insert(index + 1, new TooltipLine(Mod, "WakmehamehaMode1", "Left-click: Normal swing attack, passively has a 2% chance of spawning a shockwave"));
            tooltips.Insert(index + 2, new TooltipLine(Mod, "WakmehamehaMode2", "Right-click: Front slash where you are facing. Its always horizontal.\nInstantly reset Gods CDs and put your rage at max.\nAlso spawns a shockwave. Slash CD:10s"));
            tooltips.Insert(index + 3, new TooltipLine(Mod, "WakmehamehaDamageInfo", "Shockwaves deals damage and knockback equal your level of rage. It varies between 5% or 10% max HP scalating with meele"));
        }

         public override Vector2? HoldoutOffset() //No funciona
        {
            // Ajusta X e Y hasta que se vea bien (negativo X = atrás, negativo Y = arriba)
            return new Vector2(-20f, 0f); // Ejemplo: 6 píxeles hacia atrás
        }
    }
    




    // --- ModPlayer para guardar el Cooldown del Ataque Secundario ---
    public class YopukaWeaponCDPlayer : ModPlayer
    {
        public int AltAttackSwordCooldown = 0;

        public override void PreUpdate()
        {
            if (AltAttackSwordCooldown > 0)
            {
                AltAttackSwordCooldown--;
            }
        }

        // Guardar/Cargar Cooldown
        public override void SaveData(Terraria.ModLoader.IO.TagCompound tag)
        {
            tag["yopukaAltSwordCD"] = AltAttackSwordCooldown;
        }
        public override void LoadData(Terraria.ModLoader.IO.TagCompound tag)
        {
            if (tag.ContainsKey("yopukaAltSwordCD")) AltAttackSwordCooldown = tag.Get<int>("yopukaAltSwordCD");
        }
    }
}