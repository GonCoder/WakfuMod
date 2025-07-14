using Terraria;
using Terraria.ModLoader;
using WakfuMod.Content.Buffs; // Asegúrate que el namespace es correcto para tu buff
using Terraria.ID; // Para Main.netMode, NetmodeID.Server, DustID
using Microsoft.Xna.Framework.Graphics; // Para Texture2D
using System.Linq;
using System;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria.DataStructures;
using ReLogic.Content; // Para Enumerable.Repeat

namespace WakfuMod.Content.Mounts
{
    public class KamasutarSheet : ModMount
    {
        // --- Propiedad para la Textura Principal ---
        // public override string Texture => "WakfuMod/Assets/Mounts/KamasutarSheet"; // Ruta a TU textura, SIN .png

        // --- CONSTANTES DE EXCAVACIÓN ---
        private const int DigDelayTicks = 60; // 1 segundo de espera para el PRIMER cavado
        private const int ContinuousDigDelayTicks = 18; // 0.3 segundos (18 ticks) para cavados siguientes
        private const int DigUpwardsBoostTicks = 30; // 0.5 segundos de impulso después de cavar
        private const int DigWidth = 4; // Ancho de excavación horizontal (en tiles)
        private const int DigHeight = 4; // Alto de excavación horizontal (en tiles)
        private const int DigDownDepth = 6; // Profundidad de excavación hacia abajo (en tiles)


        // --- VARIABLES DE ESTADO PARA EXCAVACIÓN ---
        // Necesitamos almacenar estos datos por jugador, así que usamos _mountSpecificData
        protected class KamasutarData
        {
            public int HorizontalStuckTimer = 0;
            public int DownStuckTimer = 0;
            public int UpStuckTimer = 0; // NUEVO: Timer para cavar hacia arriba
            public int UpwardsBoostTimer = 0; // NUEVO: Timer para el impulso vertical
            public int UpHoldTimer = 0; // NUEVO: Contador para cuánto tiempo se mantiene W
        }

        public override void SetMount(Player player, ref bool skipDust)
        {
            // Inicializar los datos específicos de esta montura para este jugador
            player.mount._mountSpecificData = new KamasutarData();

        }

        public override void SetStaticDefaults()
        {
            MountData.buff = ModContent.BuffType<KamasutarMountBuff>();
            MountData.heightBoost = 16;
            MountData.fallDamage = 0.2f;
            MountData.runSpeed = 3f;
            MountData.dashSpeed = MountData.runSpeed;
            MountData.flightTimeMax = 0;
            MountData.fatigueMax = 0;
            MountData.jumpHeight = 12;
            MountData.acceleration = 0.8f;
            MountData.jumpSpeed = 6.2f;
            MountData.blockExtraJumps = false;
            MountData.constantJump = true;
            MountData.totalFrames = 10; // 0-9

            MountData.playerYOffsets = new int[10] { /* TUS 10 OFFSETS AQUÍ */
                18, 18, 20, 21, 22, 23, 22, 21, 20, 19
            };

            MountData.xOffset = 0;
            MountData.bodyFrame = 3;
            MountData.yOffset = 6;
            MountData.playerHeadOffset = 0;

            MountData.standingFrameStart = 0;
            MountData.standingFrameCount = 2;
            MountData.standingFrameDelay = 12;
            MountData.idleFrameStart = 0;
            MountData.idleFrameCount = 2;
            MountData.idleFrameDelay = 14;
            MountData.idleFrameLoop = true;
            MountData.runningFrameStart = 2;
            MountData.runningFrameCount = 8;
            MountData.runningFrameDelay = 8;
            MountData.inAirFrameStart = 2;
            MountData.inAirFrameCount = 8;
            MountData.inAirFrameDelay = MountData.runningFrameDelay;
            MountData.swimFrameStart = 2;
            MountData.swimFrameCount = 8;
            MountData.swimFrameDelay = MountData.runningFrameDelay;
            MountData.spawnDust = DustID.Sand;

            if (Main.netMode != NetmodeID.Server)
            {
                MountData.textureWidth = MountData.backTexture.Width();
                MountData.textureHeight = MountData.backTexture.Height();
            }
        }




        public override void UpdateEffects(Player player)
        {
            player.statDefense += 80; // --- AÑADIDA DEFENSA --- (Ajusta el valor)

            if (player.mount._mountSpecificData is not KamasutarData data)
            {
                bool dummySkipDust = false;
                SetMount(player, ref dummySkipDust);
                return;
            }

            bool isOnGround = player.velocity.Y == 0f;

            // --- DETECCIÓN DE ATASCO (para A, S, D, W) ---

            // Horizontal (A/D)
            bool stuckHorizontally = Math.Abs(player.velocity.X) < 0.1f && (player.controlLeft || player.controlRight) && isOnGround;
            if (stuckHorizontally) data.HorizontalStuckTimer++; else data.HorizontalStuckTimer = 0;

            // Hacia Abajo (S)
            bool tryingToDigDown = player.controlDown && isOnGround;
            if (tryingToDigDown) data.DownStuckTimer++; else data.DownStuckTimer = 0;

            // 1. Gestionar el temporizador de mantener "W"
            if (player.controlUp)
            {
                data.UpHoldTimer++; // Incrementar mientras se mantiene W
            }
            else
            {
                data.UpHoldTimer = 0; // Resetear si se suelta W
            }

            // 2. Determinar si se debe intentar cavar
            bool tryDiggingUp = false;
            // Si es la primera vez (timer llega a 60)
            if (data.UpHoldTimer == DigDelayTicks)
            {
                tryDiggingUp = true;
            }
            // Si ya pasó el primer segundo Y han pasado 0.3 segundos desde el último intento
            else if (data.UpHoldTimer > DigDelayTicks && (data.UpHoldTimer - DigDelayTicks) % ContinuousDigDelayTicks == 0)
            {
                tryDiggingUp = true;
            }

            // 3. Ejecutar la excavación si corresponde
            if (tryDiggingUp)
            {
                bool blockWasDug = false; // Flag para saber si se rompió al menos un bloque

                // Definir el área de excavación por encima de la montura
                Rectangle mountHitbox = player.getRect();
                mountHitbox.Y += player.height - MountData.heightBoost;
                Point startTile = new Vector2(mountHitbox.X, mountHitbox.Y).ToTileCoordinates();
                int areaWidth = (int)Math.Ceiling(mountHitbox.Width / 16f) + 1;

                // Excavar un área de 2 tiles de alto
                for (int y = 1; y <= 6; y++)
                {
                    for (int x = 0; x < areaWidth; x++)
                    {
                        int tileX = startTile.X - 1 + x; // Centrar un poco mejor el área de excavación
                        int tileY = startTile.Y - y;

                        // TryDigTile ahora necesita devolver si tuvo éxito
                        if (TryDigTile(tileX, tileY, player))
                        {
                            blockWasDug = true; // Marcar que se cavó algo
                        }
                    }
                }

                // 4. Aplicar el impulso vertical SOLO SI se cavó un bloque
                if (blockWasDug)
                {
                    data.UpwardsBoostTimer = DigUpwardsBoostTicks; // Activar el impulso
                }
            }


            // 5. Lógica de Impulso Vertical (como antes, pero el timer se activa de forma diferente)
            if (data.UpwardsBoostTimer > 0)
            {
                // Aplicar impulso solo si se mantiene W
                if (player.controlUp)
                {
                    player.velocity.Y = -MountData.jumpSpeed * 0.9f; // Impulso constante
                    player.fallStart = (int)(player.position.Y / 16f); // Resetear daño por caída
                }
                data.UpwardsBoostTimer--; // Decrementar el timer del impulso
            }


            // --- Ejecutar Excavación ---

            // Cavar Horizontalmente (A o D)
            if (data.HorizontalStuckTimer >= DigDelayTicks)
            {
                int direction = player.controlRight ? 1 : -1;
                Point startTile = (player.Center + new Vector2(direction * (player.width / 2f + 8f), 0)).ToTileCoordinates();

                for (int x = 0; x < DigWidth; x++)
                {
                    for (int y = 0; y < DigHeight; y++)
                    {
                        int tileX = startTile.X + (x * direction);
                        int tileY = startTile.Y - (DigHeight / 2) + y;
                        TryDigTile(tileX, tileY, player);
                    }
                }
                data.HorizontalStuckTimer = 0;
            }

            // Cavar hacia Abajo (S) - CORREGIDO
            if (data.DownStuckTimer >= DigDelayTicks)
            {
                Point startTile = player.Bottom.ToTileCoordinates();
                // Empezar a cavar desde y=0 (el tile justo debajo)
                for (int y = 0; y <= DigDownDepth; y++)
                {
                    int tileY = startTile.Y + y;
                    TryDigTile(startTile.X - 1, tileY, player);
                    TryDigTile(startTile.X, tileY, player);
                    TryDigTile(startTile.X + 1, tileY, player);
                }
                data.DownStuckTimer = 0;
                player.fallStart = (int)(player.position.Y / 16f);
            }

            // Ejemplo de polvo al correr (adaptado del ExampleMount)
            if (Math.Abs(player.velocity.X) > MountData.runSpeed * 0.5f)
            {
                Rectangle rect = player.getRect();
                // Desplazar el polvo a los pies de la montura
                Vector2 dustPos = player.BottomLeft + new Vector2(Main.rand.NextFloat(player.width), -Main.rand.NextFloat(4, 8));
                if (player.direction == -1) dustPos.X -= 6;


                if (Main.rand.NextBool(4)) // No generar polvo en cada frame
                    Dust.NewDustPerfect(dustPos, MountData.spawnDust, new Vector2(player.velocity.X * 0.2f, Main.rand.NextFloat(-1, -0.5f)), 100, default, 1.2f);
            }
        }

        // --- FIRMA MODIFICADA: Añadir 'Player player' ---
        private bool TryDigTile(int x, int y, Player player)
        {
            if (!WorldGen.InWorld(x, y, 1)) return false; // No se cavó
            Tile tile = Framing.GetTileSafely(x, y);

            if (tile.HasTile)
            {
                // ... (la lógica 'isDiggable' e 'isProtected' sin cambios) ...
                bool isDiggable = TileID.Sets.Conversion.Dirt[tile.TileType] ||
                                   TileID.Sets.Conversion.Sand[tile.TileType] ||
                                   TileID.Sets.Conversion.Snow[tile.TileType] ||
                                   TileID.Sets.Mud[tile.TileType] ||
                                   TileID.Sets.Stone[tile.TileType] ||
                                   tile.TileType == TileID.Ash ||
                                   tile.TileType == TileID.Silt ||
                                   tile.TileType == TileID.Slush ||
                                   tile.TileType == TileID.ClayBlock ||
                                   tile.TileType == TileID.Grass;

                bool isProtected = Main.tileDungeon[tile.TileType] ||
                                   tile.TileType == TileID.LihzahrdBrick ||
                                   TileID.Sets.Ore[tile.TileType];

                if (isDiggable && !isProtected)
                {
                    // Guardar poder de pico original y establecer uno temporal muy alto
                    int originalPickPower = player.HeldItem.pick;
                    player.HeldItem.pick = 250; // Poder suficiente para casi todo lo excavable

                    // --- CORREGIDO: Ahora 'player' existe en este contexto ---
                    player.PickTile(x, y, player.HeldItem.pick); // Usar PickTile es más directo que HitThings

                    // Restaurar poder de pico
                    player.HeldItem.pick = originalPickPower;

                    // --- Alternativa: WorldGen.KillTile sigue siendo una opción si PickTile da problemas ---
                    WorldGen.KillTile(x, y, false, false, false);
                    // Si WorldGen.KillTile no funcionaba con hierba, PickTile debería hacerlo.

                    // Sincronización
                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendTileSquare(-1, x, y, 1);
                    }

                    // Efecto de polvo (opcional, PickTile ya puede crear algunos)
                    for (int i = 0; i < 4; i++)
                    {
                        Dust.NewDust(new Vector2(x * 16, y * 16), 16, 16, DustID.Stone);
                    }
                    // Devolver 'true' porque se rompió un bloque
                    return true;
                }
            }

            // Devolver 'false' si no había tile, o no era excavable, o estaba protegido
            return false;
        }
    }
}




