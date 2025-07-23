using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Utilities;
using WakfuMod.Content.Projectiles.Enemies;

namespace WakfuMod.Content.NPCs
{
    /// <summary>
    /// Whisperer – NPC con IA personalizada de persecución + ataque cargado.
    /// Incluye lógica avanzada de escalada/esquiva de terreno:
    /// - StepUp usando la rutina vanilla de colisiones (slopes, medios bloques).
    /// - Escaneo multi-columna en profundidad para detectar pendientes irregulares.
    /// - Mini-salto vs salto completo según altura detectada.
    /// - Cooldown anti-"pogo" para evitar saltitos constantes en llano.
    /// - Failsafe de salto por atasco.
    /// Estados:
    ///   ai[1] = 0 Wander/Chase | 1 Charging | 2 Attacking
    ///   ai[0] = timer del subestado (carga/ataque)
    ///   ai[2] = timer de atasco (frames consecutivos sin avanzar)
    /// </summary>
    public class Whisperer : ModNPC
    {
        // ===== AI state enum (opcional, sólo para claridad) =====
        private const int State_WanderChase = 0;
        private const int State_ChargingAttack = 1;
        private const int State_Attacking = 2;

        // ===== Configuración de escalada / salto =====
        private const int LookAheadTiles = 5;        // Aumentamos profundidad horizontal (antes 3) para anticipar pendientes más irregulares
        private const int MaxDetectTilesUp = 4;      // Escanea un tile extra hacia arriba para detectar bordes dentados
        private const float StepHopVelocity = -4.25f;// Salto corto para escalones bajos / slopes (ajusta a gusto)
        private const float FullJumpVelocity = -7f;  // Salto completo para muros más altos
        private const int JumpCooldownFrames = 20;   // Anti-pogo: subimos cooldown para reducir saltitos en llano (~0.2s a 60fps)
        private const int StuckJumpDelay = 25;       // Un poco más de tolerancia antes del salto forzado

        // ===== Configuración visual =====
        private const float SpriteFootPad = 0f;      // píxeles de padding debajo de los pies en el sprite (ajusta si ves hundimiento/flotación)
        private static readonly bool AnchorSpriteToBottomLeft = false;

        // ===== Campos runtime =====
        private Vector2 lastPosition; // Posición del frame anterior (para detectar avance)
        private int jumpCooldown;     // Frames restantes hasta permitir otro salto

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 6; // 0-3 andar, 4 cargar, 5 atacar
        }

        public override void SetDefaults()
        {
            NPC.width = 20;
            NPC.height = 30;
            NPC.aiStyle = -1; // IA totalmente custom
            NPC.damage = 12;
            NPC.defense = 4;
            NPC.lifeMax = 80;
            NPC.knockBackResist = 0.6f;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath2;
            NPC.value = Item.buyPrice(copper: 50);

            // IMPORTANTE: Apoyarse en la gravedad vanilla; no apliques gravedad manual en AI.
            NPC.noGravity = false;
            NPC.noTileCollide = false;
        }

        public override void OnSpawn(IEntitySource source)
        {
            lastPosition = NPC.position;
            jumpCooldown = 0;
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            if (spawnInfo.Player.ZoneForest && spawnInfo.Player.ZoneOverworldHeight && Main.dayTime)
            {
                return SpawnCondition.OverworldDaySlime.Chance * 0.9f;
            }
            return 0f;
        }

        public override void AI()
        {
            // Reducir cooldown de salto cada tick
            if (jumpCooldown > 0)
                jumpCooldown--;

            // Asegurar target válido
            NPC.TargetClosest(true);
            Player player = Main.player[NPC.target];

            if (!player.active || player.dead)
            {
                NPC.TargetClosest(false);
                player = Main.player[NPC.target];
                if (!player.active || player.dead)
                {
                    NPC.velocity.X *= 0.9f;
                    if (NPC.timeLeft > 10) NPC.timeLeft = 10;
                    return;
                }
            }

            // Máquina de estados
            switch ((int)NPC.ai[1])
            {
                case State_WanderChase:
                    WanderChase(player);
                    break;
                case State_ChargingAttack:
                    ChargingAttack(player);
                    break;
                case State_Attacking:
                    Attacking(player);
                    break;
                default:
                    NPC.ai[1] = State_WanderChase;
                    break;
            }
        }

        #region Wander / Movement
        private void WanderChase(Player player)
        {
            float moveSpeed = 3f;
            float acceleration = 0.08f;
            int direction = NPC.direction; // Se actualiza vía TargetClosest en AI()

            // ===== Movimiento horizontal prensado hacia el jugador =====
            if (direction == 1)
            {
                if (NPC.velocity.X < moveSpeed) NPC.velocity.X += acceleration;
                if (NPC.velocity.X > moveSpeed) NPC.velocity.X = moveSpeed;
            }
            else if (direction == -1)
            {
                if (NPC.velocity.X > -moveSpeed) NPC.velocity.X -= acceleration;
                if (NPC.velocity.X < -moveSpeed) NPC.velocity.X = -moveSpeed;
            }

            // Detectar si estamos "apoyados" (suelo o slope) incluso si collideY es falso.
            bool isOnGroundish = IsSupportedOnGround();

            // ===== Escalada / salto anticipado =====
            if (Math.Abs(NPC.velocity.X) > 0.1f)
            {
                // 1) StepUp vanilla (slopes / medios bloques). Siempre probar.
                bool stepped = TryStepUp();

                // 2) Si seguimos en la misma Y y estamos sobre slope, probar StepUp "fuerte" con velocidad mínima.
                if (!stepped && IsSlopeUnderfoot(out _))
                    stepped = TryStrongStepUp();

                // 3) Resolver bloqueo frontal / salto.
                if (!stepped)
                {
                    // Bloque muy cercano (2px) → importante cuando slope choca con bloque plano.
                    bool immediateBlock = IsBlockedAhead(direction, 2f);

                    if (immediateBlock && IsSlopeUnderfoot(out _))
                    {
                        // salto corto ligeramente más alto para romper el labio
                        DoJump(StepHopVelocity * 1.15f);
                    }
                    else if (isOnGroundish && jumpCooldown == 0)
                    {
                        int required = GetRequiredClimbAhead(direction);
                        if (required == 0 && IsAscendingGroundSlopeAhead(direction))
                            required = 1;

                        if (required == 1)
                            DoJump(StepHopVelocity);
                        else if (required >= 2)
                            DoJump(FullJumpVelocity);
                        else if (immediateBlock)
                            DoJump(StepHopVelocity);
                    }
                }
            }

            // ===== Detección de atasco de respaldo =====
            bool isTryingToMove = Math.Abs(NPC.velocity.X) > 0.1f ||
                                  (direction != 0 && Math.Abs(player.Center.X - NPC.Center.X) > NPC.width);
            bool isStuck = Math.Abs(NPC.position.X - lastPosition.X) < 0.5f; // sólo delta X
            if (isTryingToMove && isStuck && isOnGroundish)
            {
                NPC.ai[2]++;
                if (NPC.ai[2] >= StuckJumpDelay && jumpCooldown == 0)
                {
                    DoJump(FullJumpVelocity);
                    NPC.ai[2] = 0;
                }
            }
            else
            {
                NPC.ai[2] = 0;
            }

            // Guardar posición para el siguiente tick
            lastPosition = NPC.position;

            // ===== Paso a estado de carga de ataque =====
            float attackRange = 200f;
            if (Vector2.Distance(NPC.Center, player.Center) < attackRange &&
                Collision.CanHitLine(NPC.position, NPC.width, NPC.height, player.position, player.width, player.height))
            {
                NPC.ai[1] = State_ChargingAttack;
                NPC.ai[0] = 0;
                NPC.velocity.X = 0f;
                NPC.ai[2] = 0;
                NPC.netUpdate = true;
            }
        }
        #endregion

        #region Charging
        private void ChargingAttack(Player player)
        {
            // Detenemos movimiento horizontal pero dejamos la gravedad actuar.
            NPC.velocity.X = 0f;
            // no tocar NPC.velocity.Y para evitar desincronizar sprite/hitbox si la carga comienza en el aire.
            NPC.ai[0]++;
            const int chargeTime = 50;

            if (NPC.ai[0] >= chargeTime)
            {
                NPC.ai[1] = State_Attacking;
                NPC.ai[0] = 0;
                NPC.netUpdate = true;

                SoundEngine.PlaySound(SoundID.Item1, NPC.position);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 directionToPlayer = (player.Center - NPC.Center).SafeNormalize(Vector2.UnitX);
                    Vector2 spawnPos = NPC.Center + directionToPlayer * (NPC.width / 2f + 80f);
                    Projectile.NewProjectile(NPC.GetSource_FromAI(), spawnPos, directionToPlayer * 1f, ModContent.ProjectileType<LancerStab>(), 10, 40f, Main.myPlayer);
                    // ↑ Este último valor (20f) es el knockback que sí aplica al jugador

                }
            }
        }
        #endregion

        #region Attacking
        private void Attacking(Player player)
        {
            // No congelar Y: deja que la gravedad actúe para evitar desalinear sprite/hitbox en aire.
            NPC.velocity.X = 0f; // sólo frena horizontal
            // no tocar NPC.velocity.Y
            NPC.ai[0]++;
            const int attackAnimDuration = 15;

            if (NPC.ai[0] >= attackAnimDuration)
            {
                NPC.ai[1] = State_WanderChase; // volver a perseguir
                NPC.ai[0] = 0;
                NPC.netUpdate = true;
            }
        }
        #endregion

        #region Movement Helpers
        /// <summary>
        /// Devuelve true si consideramos que el NPC está apoyado en algo (suelo plano o slope),
        /// aunque <see cref="NPC.collideY"/> sea falso en algunos frames sobre rampas.
        /// </summary>
        private bool IsSupportedOnGround()
        {
            if (NPC.collideY)
                return true;

            // Miramos 2px bajo los pies para encontrar tile de soporte.
            Point foot = (NPC.Bottom + new Vector2(0f, 2f)).ToTileCoordinates();
            Tile t = Framing.GetTileSafely(foot.X, foot.Y);
            if (!t.HasTile) return false;
            if (Main.tileSolid[t.TileType] && !Main.tileSolidTop[t.TileType]) return true;
            if (t.IsHalfBlock) return true;
            if (t.Slope != Terraria.ID.SlopeType.Solid) return true;
            return false;
        }

        /// <summary>
        /// True si el tile bajo los pies es un slope/half-block.
        /// </summary>
        private bool IsSlopeUnderfoot(out Terraria.ID.SlopeType slope)
        {
            Point foot = NPC.Bottom.ToTileCoordinates();
            Tile t = Framing.GetTileSafely(foot.X, foot.Y);
            slope = t.Slope;
            if (!t.HasTile) return false;
            if (t.IsHalfBlock) return true;
            return t.Slope != Terraria.ID.SlopeType.Solid;
        }

        /// <summary>
        /// StepUp con velocidad mínima garantizada (3f) para ayudar a subir cuando casi estamos parados en slope.
        /// </summary>
        private bool TryStrongStepUp()
        {
            Vector2 pos = NPC.position;
            Vector2 vel = NPC.velocity;
            float stepSpeed = Math.Max(3f, Math.Abs(NPC.velocity.X));
            float gfxAdj = 0f;
            Collision.StepUp(ref pos, ref vel, NPC.width, NPC.height, ref stepSpeed, ref gfxAdj, 1, false, 1);
            if (pos.Y != NPC.position.Y)
            {
                NPC.position = pos;
                NPC.velocity = vel;
                NPC.gfxOffY = gfxAdj;
                return true;
            }
            return false;
        }
        /// <summary>
        /// Realiza un salto asignando velocidad vertical negativa y aplica cooldown.
        /// </summary>
        private void DoJump(float jumpVelocity)
        {
            NPC.velocity.Y = jumpVelocity;
            jumpCooldown = JumpCooldownFrames;
            NPC.netUpdate = true;
        }

        /// <summary>
        /// Usa la rutina vanilla Collision.StepUp para intentar subir un escalón / slope sin salto.
        /// Devuelve true si hubo ajuste de posición vertical.
        /// </summary>
        private bool TryStepUp()
        {
            // Siempre probar; paso ligero, coste bajo.
            // Permite que la rutina vanilla gestione slopes naturales de worldgen (dirt irregular, etc.).
            Vector2 pos = NPC.position;
            Vector2 vel = NPC.velocity;
            float stepSpeed = Math.Abs(NPC.velocity.X);
            float gfxAdj = 0f;
            // specialChecksMode=1 ayuda con tiles complicados; holdsMatching=false
            Collision.StepUp(ref pos, ref vel, NPC.width, NPC.height, ref stepSpeed, ref gfxAdj, 1, false, 1);
            if (pos.Y != NPC.position.Y)
            {
                NPC.position = pos;
                NPC.velocity = vel;
                NPC.gfxOffY = gfxAdj;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Escanea varias columnas por delante y devuelve la altura máxima (en tiles) que habría que salvar.
        /// Considera slopes / medios bloques.
        /// </summary>
        private int GetRequiredClimbAhead(int direction)
        {
            Point bottom = NPC.Bottom.ToTileCoordinates();
            int footY = bottom.Y;
            int maxHeight = 0;

            for (int dx = 1; dx <= LookAheadTiles; dx++)
            {
                int colX = bottom.X + direction * dx;
                int h = SampleColumnHeight(colX, footY);
                if (h > maxHeight)
                    maxHeight = h;
                if (maxHeight >= MaxDetectTilesUp)
                    break;
            }
            return maxHeight;
        }

        /// <summary>
        /// Mide la ocupación de una columna de tiles a partir del nivel de los pies hacia arriba.
        /// Cuenta slopes y medios bloques como altura ocupada.
        /// También hace un SolidCollision de validación a nivel de píxel.
        /// </summary>
        private int SampleColumnHeight(int tileX, int footTileY)
        {
            // IMPORTANTE: offsetY empieza en 1 (saltamos la capa de suelo sobre la que caminamos).
            // Si escaneamos offsetY=0 contaríamos el propio suelo como obstáculo → pogo en llano.
            int height = 0;

            for (int offsetY = 1; offsetY <= MaxDetectTilesUp; offsetY++)
            {
                int checkY = footTileY - offsetY;
                Tile t = Framing.GetTileSafely(tileX, checkY);
                if (t.HasTile && Main.tileSolid[t.TileType] && !Main.tileSolidTop[t.TileType])
                {
                    // Cualquier sólido por encima del nivel de los pies requiere escalar.
                    // Slopes / half blocks cuentan como 1 tile de altura mínima.
                    height = offsetY; // ya que offsetY comienza en 1
                    break; // ya detectamos el primer bloqueo superior
                }
            }

            // Validación pixel-perfect: ¿al desplazar el hitbox unos px hacia esa columna colisionaríamos?
            // Usamos un desplazamiento horizontal al centro de la columna y mantenemos la Y actual.
            if (height == 0)
            {
                float worldX = tileX * 16 + 8 - (NPC.width / 2f);
                Vector2 probePos = new Vector2(worldX, NPC.position.Y);
                if (Collision.SolidCollision(probePos, NPC.width, NPC.height))
                {
                    // Algo a nivel de píxel bloquea (slope irregular, diente, medio bloque martillado, etc.)
                    height = 1;
                }
            }

            return height;
        }

        /// <summary>
        /// Proyección rápida: ¿chocaré si avanzo 'pixels' hacia delante con mi hitbox actual?
        /// Útil para detectar slopes irregulares que no llenan un tile completo.
        /// </summary>
        private bool IsBlockedAhead(int direction, float pixels)
        {
            Vector2 probePos = NPC.position + new Vector2(direction * pixels, 0f);
            return Collision.SolidCollision(probePos, NPC.width, NPC.height);
        }
        /// <summary>
        /// Detecta si el tile de suelo inmediatamente delante es un slope que asciende en la dirección de movimiento.
        /// Esto cubre terrenos de tierra irregulares de worldgen que no ocupan un tile completo por encima.
        /// </summary>
        private bool IsAscendingGroundSlopeAhead(int direction)
        {
            Point bottom = NPC.Bottom.ToTileCoordinates();
            int checkX = bottom.X + direction;
            int checkY = bottom.Y; // suelo delante
            Tile t = Framing.GetTileSafely(checkX, checkY);
            if (!t.HasTile) return false;
            if (!Main.tileSolid[t.TileType] || Main.tileSolidTop[t.TileType]) return false;
            if (t.IsHalfBlock) return true; // medio bloque = necesitamos subir algo

            // Usamos la enum SlopeType para detectar cualquier slope (sin preocuparnos de orientación exacta).
            // Cualquier slope distinto de Solid significa que el plano del suelo no es plano horizontal ⇒ tratamos como subida.
            if (t.Slope != Terraria.ID.SlopeType.Solid)
                return true;

            return false;
        }
        #endregion

        #region Frames / Draw / Loot
        public override void FindFrame(int frameHeight)
        {
            NPC.spriteDirection = -NPC.direction; // sprite base mira a la izquierda

            if (NPC.ai[1] == State_Attacking)
            {
                NPC.frame.Y = 5 * frameHeight; // atacar
                NPC.frameCounter = 0;
            }
            else if (NPC.ai[1] == State_ChargingAttack)
            {
                NPC.frame.Y = 4 * frameHeight; // cargar
                NPC.frameCounter = 0;
            }
            else if (!NPC.collideY) // en el aire
            {
                NPC.frame.Y = 0 * frameHeight; // usar primer frame de andar como salto
                NPC.frameCounter = 0;
            }
            else if (Math.Abs(NPC.velocity.X) > 0.1f) // caminando
            {
                NPC.frameCounter += Math.Abs(NPC.velocity.X) * 0.5f;
                if (NPC.frameCounter >= 6)
                {
                    NPC.frameCounter = 0;
                    NPC.frame.Y += frameHeight;
                    if (NPC.frame.Y >= 4 * frameHeight) // cicla 0-3
                        NPC.frame.Y = 0;
                }
            }
            else // quieto
            {
                NPC.frame.Y = 0 * frameHeight;
                NPC.frameCounter = 0;
            }
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ItemID.RegenerationPotion, 10, 1, 1));
            npcLoot.Add(ItemDropRule.Common(ItemID.IronskinPotion, 10, 1, 1));
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            float desiredScale = 0.5f;

            Texture2D texture = TextureAssets.Npc[Type].Value;
            Rectangle sourceRectangle = NPC.frame;

            Vector2 origin;
            if (AnchorSpriteToBottomLeft)
                origin = new Vector2(0f, sourceRectangle.Height - SpriteFootPad);
            else
                origin = new Vector2(sourceRectangle.Width / 2f, sourceRectangle.Height - SpriteFootPad);

            // Siempre usar gfxOffY limitado, incluso en el aire
            float gfx = MathHelper.Clamp(NPC.gfxOffY, -4f, 8f);
            Vector2 drawPosition = (AnchorSpriteToBottomLeft ? NPC.BottomLeft : NPC.Bottom) - screenPos + new Vector2(0f, gfx + 3f);

            SpriteEffects spriteEffects = (NPC.direction == -1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            spriteBatch.Draw(
                texture,
                drawPosition,
                sourceRectangle,
                NPC.GetAlpha(drawColor),
                NPC.rotation,
                origin,
                desiredScale,
                spriteEffects,
                0f
            );

            return false;
        }
        #endregion
    }
}
