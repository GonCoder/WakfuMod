// Content/Items/Consumables/Karkasa.cs (o donde prefieras ponerlo)
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.DataStructures; // Para IEntitySource
using WakfuMod.Content.Projectiles; // Para Jalabola
using Terraria.Audio; // Para SoundEngine

namespace WakfuMod.Content.Items.Consumables // Reemplaza con tu namespace
{
    public class Jalaball : ModItem
    {
        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("Blow to start a Gobbowl match!\nSpawns a Gobball at your cursor."); // Usa .hjson
            ItemID.Sets.SortingPriorityBossSpawns[Type] = 12; // Prioridad similar a otros spawners
        }

        public override void SetDefaults()
        {
            Item.width = 28; // Ajusta al tamaño de tu sprite de Karkasa
            Item.height = 28;
            Item.maxStack = 1; // No apilable, es una herramienta
            Item.value = Item.sellPrice(silver: 10); // Precio simbólico
            Item.rare = ItemRarityID.Blue; // Rareza
            Item.knockBack =6f;

            // --- Uso ---
            Item.useAnimation = 15; // Animación de "soplar"
            Item.useTime = 30;
            Item.useStyle = ItemUseStyleID.HoldUp; // Estilo de mostrar item arriba
            Item.UseSound = SoundID.Item2; // Sonido genérico de usar item (o uno más tipo silbato?)
                                           // Podrías buscar SoundID.Item37, Item38 o importar uno custom.

            // --- Funcionalidad ---
            Item.consumable = false; // NO se consume al usar
            Item.noMelee = true; // No hace daño melee
        }

 // CanUseItem siempre devuelve true, la lógica está en UseItem
        public override bool CanUseItem(Player player)
        {
            return true; // Siempre se puede intentar usar
        }

        // --- Lógica de Uso (Interruptor) ---
        public override bool? UseItem(Player player)
        {
            // Solo el jugador local debe ejecutar la lógica de spawn/kill
            if (player.whoAmI != Main.myPlayer)
            {
                return true; // Otros clientes solo ven la animación
            }

            // Buscar si YA existe una Jalabola de ESTE jugador
            int existingBallIndex = -1;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == player.whoAmI && p.type == ModContent.ProjectileType<Jalabola>())
                {
                    existingBallIndex = i;
                    break;
                }
            }

            // --- Si YA EXISTE una Jalabola: Destruirla ---
            if (existingBallIndex != -1)
            {
                Main.projectile[existingBallIndex].Kill(); // Mata la pelota existente
                SoundEngine.PlaySound(SoundID.Dig, player.position); // Sonido de "guardar" o "pop"
                Main.NewText("Gobball removed.", Color.Orange); // Mensaje opcional
            }
            // --- Si NO EXISTE una Jalabola: Spawnearla ---
            else
            {
                Vector2 spawnPosition = Main.MouseWorld; // Posición del cursor

                // Lógica opcional para evitar spawn en sólidos (igual que antes)
                 Point tileCoords = spawnPosition.ToTileCoordinates();
                 Tile checkTile = Framing.GetTileSafely(tileCoords.X, tileCoords.Y);
                 if (checkTile.HasTile && Main.tileSolid[checkTile.TileType]) {
                      for(int y = tileCoords.Y; y > tileCoords.Y - 5; y--) {
                          Tile upTile = Framing.GetTileSafely(tileCoords.X, y);
                          if (!upTile.HasTile || !Main.tileSolid[upTile.TileType]) {
                               spawnPosition.Y = y * 16 + 8;
                               break;
                          }
                     }
                 }

                // Spawnear la nueva Jalabola
                IEntitySource source = player.GetSource_ItemUse(Item);
                Projectile.NewProjectile(
                    source,
                    spawnPosition,
                    Vector2.Zero,
                    ModContent.ProjectileType<Jalabola>(),
                    0, 0f, player.whoAmI
                );
                SoundEngine.PlaySound(SoundID.Item2, player.position); // Sonido de spawn
                 Main.NewText("Gobball spawned!", Color.LimeGreen); // Mensaje opcional
            }

            return true; // Indicar que el uso fue exitoso
        }

        // Opcional: Añadir receta si no quieres que sea solo de inicio/compra
        // public override void AddRecipes() { ... }
    }
}