using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using WakfuMod.Content.Projectiles;
using System.Linq;
using Terraria.DataStructures; // Necesario para IEntitySource
using Terraria.ID;
using Terraria.Audio;

namespace WakfuMod
{
    public class TymadorAbilityHandler
    {
        private static int lastBombTime = 0;
        private const int BombCooldown = 180; // Cooldown en ticks (3 segundos a 60 FPS) Cooldown en ticks (60 ticks = 1 segundo) -> 5 segundos = 300 ticks
        private static int nextBombSequenceID = 1; // Contador para el ID de secuencia

        public static void TryPlaceBomb(Player player)
        {
            if (Main.GameUpdateCount - lastBombTime < BombCooldown)
            {
                // Opcional: Mensaje de cooldown
                // Main.NewText("¡No puedes colocar bombas tan rápido!", Color.OrangeRed);
                return;
            }

            // var playerBombs = TymadorBombManager.ActiveBombs.Where(b => b.owner == player.whoAmI).ToList();

          var playerBombs = TymadorBombManager.ActiveBombs
                                .Where(b => b != null && b.active && b.owner == player.whoAmI)
                                .OrderBy(b => b.ai[1]) // Ordenar por secuencia
                                .ToList();

            // Si hay 3 bombas, intercambiar posición con la bomba Tier 2 (la más antigua)
            if (playerBombs.Count >= 3)
            {
                // Intenta encontrar la bomba con Tier 2. Si falla, usa la primera en la lista ordenada (la más vieja).
                Projectile bombToSwap = playerBombs.FirstOrDefault(b => b.ai[0] == 2) ?? playerBombs.FirstOrDefault();

                if (bombToSwap != null && bombToSwap.active)
                {
                    Vector2 playerOriginalPos = player.Center; // Guarda posición original del jugador
                    Vector2 bombOriginalPos = bombToSwap.Center; // Guarda posición original de la bomba

                    // --- Efectos Visuales/Sonoros del Swap (OPCIONAL pero recomendado) ---
                    if (Main.netMode != NetmodeID.Server)
                    {
                        //  SoundEngine.PlaySound(SoundID.Item6, playerOriginalPos); // Woosh en jugador
                        //  SoundEngine.PlaySound(SoundID.Item6, bombOriginalPos);  // Woosh en bomba
                         
                    }
                    // --- FIN EFECTOS ---

                    // Teleportar jugador a la posición de la bomba
                    player.Teleport(bombOriginalPos, 1, 0); // Estilo 1
                    if (Main.netMode == NetmodeID.MultiplayerClient) {
                         NetMessage.SendData(MessageID.TeleportEntity, -1, -1, null, player.whoAmI, bombOriginalPos.X, bombOriginalPos.Y, 1);
                    }

                    // Mover la bomba a la posición original del jugador
                    bombToSwap.Center = playerOriginalPos;
                    bombToSwap.velocity = Vector2.Zero; // Detener velocidad residual

                    // --- RESETEAR ESTADO Y COLISIÓN DE LA BOMBA ---
                    // Intenta acceder a la instancia específica para usar la propiedad State
                    if (bombToSwap.ModProjectile is TymadorBomb bombInstance)
                    {
                        bombInstance.State = 0; // Resetear a estado Idle (Asentado Inicial)
                        bombInstance.Projectile.tileCollide = false; // Desactivar colisión con tiles temporalmente
                    }
                    else
                    {
                        // Fallback usando localAI si no se puede castear (menos ideal pero funciona)
                        bombToSwap.localAI[1] = 0f; // Resetea el estado
                        bombToSwap.tileCollide = false; // Desactiva colisión
                    }
                    // --- FIN RESETEO ---

                    // Sincronizar los cambios de posición, velocidad, estado (localAI) y tileCollide
                    bombToSwap.netUpdate = true;

                    // Aplicar cooldown a la habilidad de colocar/swapear
                    lastBombTime = (int)Main.GameUpdateCount;
                    return; // Importante salir para no intentar colocar una bomba nueva
                }
                // Si no se encontró una bomba válida para swapear (raro), continúa para intentar colocar una nueva.
            }

           // --- NUEVA LÓGICA DE SPAWN DELANTE DEL JUGADOR ---

// 1. Definir la distancia horizontal desde el jugador
float spawnDistanceX = 40f; // Distancia horizontal (ajusta este valor)
// 2. Definir una pequeña elevación inicial opcional
float spawnOffsetY = -8f; // Ligeramente por encima de los pies del jugador (ajusta si es necesario)

// 3. Calcular la posición base de spawn relativa al jugador
Vector2 baseSpawnPos = player.MountedCenter + new Vector2(player.direction * spawnDistanceX, spawnOffsetY);

// 4. (Opcional pero Recomendado) Buscar suelo debajo de la posición base
//    Esto evita que la bomba spawnee flotando muy alto o dentro de bloques si el jugador está cerca de un techo/pared.
Vector2 finalSpawnPos = baseSpawnPos; // Posición por defecto si no se encuentra suelo
bool foundGround = false;
int searchLimitY = 30; // Cuántos tiles hacia abajo buscar (30 * 16 = 480 píxeles)
int bombHeight = ModContent.GetInstance<TymadorBomb>().Projectile.height; // Obtener altura de la bomba

Point baseTileCoords = baseSpawnPos.ToTileCoordinates();

// Buscar suelo hacia abajo desde la posición base
for (int i = 0; i < searchLimitY; i++)
{
    Point checkCoords = new Point(baseTileCoords.X, baseTileCoords.Y + i);

    // Comprobar límites del mundo
    if (!WorldGen.InWorld(checkCoords.X, checkCoords.Y, 5)) break; // Salir si nos salimos del mundo

    Tile tile = Framing.GetTileSafely(checkCoords.X, checkCoords.Y);

    // Comprobar si el tile es sólido o una plataforma
    if (WorldGen.SolidTile(checkCoords.X, checkCoords.Y) || (tile.HasTile && TileID.Sets.Platforms[tile.TileType]))
    {
        // Encontrado suelo: Colocar la bomba justo ENCIMA
        // Usamos la X de la posición base, y la Y del tile encontrado menos la mitad de la altura de la bomba
        finalSpawnPos = new Vector2(baseSpawnPos.X, checkCoords.Y * 16f - bombHeight / 2f);
        foundGround = true;
        break;
    }
}

// Si no se encontró suelo en la búsqueda limitada, simplemente usa la posición base
// (puede quedar flotando si el jugador está muy alto, pero es mejor que no spawnear)
if (!foundGround)
{
    finalSpawnPos = baseSpawnPos;
    // Podrías añadir aquí lógica para evitar spawnear dentro de bloques sólidos
    // si la posición base está dentro de uno, moviéndola ligeramente.
    Point finalTileCoords = finalSpawnPos.ToTileCoordinates();
    if(WorldGen.SolidTile(finalTileCoords.X, finalTileCoords.Y))
    {
        // Si está en sólido, intentar moverla un poco hacia arriba o no spawnear?
        // Por simplicidad ahora, la dejamos ahí, pero podría quedar atascada.
    }
}


            // Crear la nueva bomba ASIGNANDO ai[1]
            IEntitySource source = player.GetSource_FromThis("TymadorBombPlacement");
            Projectile proj = Projectile.NewProjectileDirect(
                source,
                finalSpawnPos,
                Vector2.Zero, // Sin velocidad inicial
                ModContent.ProjectileType<TymadorBomb>(),
                0, // Daño (manejado en Kill y cable)
                0f, // Knockback (manejado en Kill y cable)
                player.whoAmI,
                ai0: 0, // ai[0] (Tier) será actualizado por el Manager
                ai1: nextBombSequenceID // *** ASIGNAR EL ID DE SECUENCIA ***
            );

            // Incrementar el ID para la próxima bomba
            nextBombSequenceID++;

             // Registrar inmediatamente si es necesario (aunque tu bomba lo hace en AI)
             // TymadorBombManager.RegisterBomb(proj); // Comentado porque la bomba se registra a sí misma

            // Actualizar tiempo de cooldown
            lastBombTime = (int)Main.GameUpdateCount;
        }

        // Resetear el ID y cooldown al entrar al mundo (o al cambiar de clase, etc.)
        public static void ResetBombSystem()
        {
            lastBombTime = 0;
            nextBombSequenceID = 1; // Reiniciar el contador de secuencia
            TymadorBombManager.ActiveBombs.Clear(); // Limpiar bombas existentes al resetear
        }

         // Llamar a ResetBombSystem en OnEnterWorld de WakfuPlayer
         // y potencialmente al cambiar de clase si quieres un reset completo.
    }
}