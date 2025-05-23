using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using WakfuMod.Content.Buffs;
using System;
using Terraria.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace WakfuMod.Content.Projectiles
{
    public class ZurcarakMinion : ModProjectile
    {
        // --- Constantes de Comportamiento (AJUSTADAS para terrestre) ---
        private const float MaxDetectRadius = 700f;
        private const float AttackRange = 40f;      // Rango melee más corto
        private const float FollowPlayerDistanceStart = 100f; // Cuándo empieza a correr hacia el jugador
        private const float FollowPlayerDistanceStop = 60f; // Cuándo deja de correr hacia el jugador
        private const float TeleportDistance = 1200f;
        private const float GroundMoveSpeed = 8f;  // Velocidad de carrera en suelo
        private const float MaxGroundMoveSpeed = 12f; // Velocidad máxima de carrera
        private const float GroundAcceleration = 0.3f; // Aceleración en suelo
        private const float AirAcceleration = 0.2f; // Aceleración en aire
        private const float MaxAirMoveSpeed = 8f; // Velocidad máxima en aire
        private const float JumpHeight = 11f;      // Fuerza del salto inicial
        private const float JumpUpPlatformBoost = 6f; // Impulso extra para subir plataformas
        private const float Gravity = 0.4f;        // Gravedad del minion
        private const float MaxFallSpeed = 10f;    // Velocidad máxima de caída

        private const int NormalAttackCooldown = 60;
        private const int FrenzyDuration = 120;
        private const int FrenzyAttackRate = 50;
        private const float FrenzyChaseSpeedMult = 1.6f; // Multiplicador de velocidad en Frenesí
        private const float FrenzyInertia = 8f;       // Inercia en Frenesí (más bajo = más ágil)

        private const float StuckCheckDistance = 20f; // Distancia mínima al jugador para considerar "atascado"
        private const int StuckTimerMax = 30; // Cuántos ticks esperar quieto antes de saltar verticalmente
        private const float UnstickJumpForce = 14f; // Fuerza del salto vertical de desatasco (¡más alta!)
        private const float VerticalFollowHeight = -40f; // A qué altura RELATIVA al jugador intenta flotar/estar (-40 = 40px por encima del centro del jugador)
        private const float VerticalFollowStrength = 0.08f; // Qué tan rápido intenta alcanzar la altura vertical deseada (0 a 1)

        // --- Añadir nueva variable de estado ---
        private int stuckTimer = 0; // Contador para detectar si está atascado



        // --- Nuevas Variables de Estado (para movimiento terrestre) ---
        private bool isOnGround = false;
        private int jumpCooldown = 0; // Para evitar saltos continuos


        // --- NUEVAS Variables de Estado (para animación Idle) ---
        private int idleTimer = 0; // Contador para los 5 segundos de idle base
        private const int IdleBaseDuration = 140; // 5 segundos * 60 ticks/segundo
        private bool playingLickAnimation = false; // Flag para la animación de lamerse

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 24; // <-- ACTUALIZADO: 0 a 23 = 24 frames
            ProjectileID.Sets.MinionTargettingFeature[Type] = true;
            ProjectileID.Sets.MinionSacrificable[Type] = true;
        }

        public override void SetDefaults()
        {
            // --- TAMAÑO DE HITBOX AJUSTADO ---
            // Tamaño original: 28x26
            // Nuevo tamaño: (28 + 4) x (26 + 4) = 32x30
            Projectile.width = 82; // <-- AJUSTADO
            Projectile.height = 70; // <-- AJUSTADO

            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.minion = true;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.minionSlots = 1f;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = false;
            Projectile.netImportant = true;
        }

        public override bool? CanCutTiles() => false;
        public override bool MinionContactDamage() => false; // ¿Daña al contacto?

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            // --- 1. COMPROBAR ESTADO DEL JUGADOR Y MANTENER BUFF ---
            if (owner.dead || !owner.active)
            {
                // Si el dueño muere o se desconecta, el minion debe desaparecer.
                Projectile.Kill(); // Matar al proyectil directamente
                return; // Salir de la AI
            }

            // Comprobar si el JUGADOR todavía QUIERE que el minion esté activo (tiene el buff)
            if (owner.HasBuff(ModContent.BuffType<ZurcarakMinionBuff>()))
            {
                Projectile.timeLeft = 2; // Mantener vivo al proyectil
            }
            else
            {
                // Si el jugador canceló el buff manualmente, el minion debe desaparecer
                Projectile.Kill();
                return;
            }
            // --- FIN COMPROBACIÓN ESTADO ---

            // --- Decrementar Timers (movido aquí para claridad) ---
            if (Projectile.ai[0] > 0) Projectile.ai[0]--; // Cooldown ataque normal
            if (Projectile.localAI[0] > 0) Projectile.localAI[0]--; // Duración Frenesí
            if (Projectile.localAI[1] > 0) Projectile.localAI[1]--; // Cooldown golpe Frenesí
            if (jumpCooldown > 0) jumpCooldown--;

            // --- Ejecutar Lógicas (ORDEN CAMBIADO) ---
            HandleMovement(owner);     // 1. Calcular dónde quiere ir
            CombatAI(owner);           // 2. Lógica de ataque
            HandleAnimation();         // 3. Actualizar animación

            // --- Aplicar Física y Colisión AL FINAL ---
            ApplyGravityAndTileCollisions(); // 4. Aplicar gravedad y resolver colisiones


        }

        // --- NUEVO: Gravedad y Colisiones ---
        private void ApplyGravityAndTileCollisions()
        {
            // Aplicar Gravedad siempre
            Projectile.velocity.Y += Gravity;
            if (Projectile.velocity.Y > MaxFallSpeed) { Projectile.velocity.Y = MaxFallSpeed; }

            Vector2 oldVelocity = Projectile.velocity;

            // --- Aplicar Colisión SOLO si tileCollide es true ---
            if (Projectile.tileCollide)
            {
                Projectile.velocity = Collision.TileCollision(Projectile.position, Projectile.velocity, Projectile.width, Projectile.height, true, true); // Con fallThrough

                isOnGround = Math.Abs(Projectile.velocity.Y) < 0.01f && oldVelocity.Y >= 0f; // Permitir isOnGround si Y es casi 0 y antes no subía

                if (isOnGround)
                {
                    Projectile.velocity.Y = 0f;
                    Point tileCoords = (Projectile.Bottom + Vector2.UnitY).ToTileCoordinates();
                    Tile tileBelow = Framing.GetTileSafely(tileCoords.X, tileCoords.Y);
                    if (tileBelow.HasTile && (Main.tileSolid[tileBelow.TileType] || TileID.Sets.Platforms[tileBelow.TileType]))
                    {
                        Projectile.position.Y = tileCoords.Y * 16f - Projectile.height; // Alinear
                    }
                }
            }
            else // Si no colisiona (Frenesí)
            {
                isOnGround = false; // No puede estar en suelo si atraviesa
                Projectile.position += Projectile.velocity; // Actualizar posición directamente
            }

            // Teleportar si se sale del mundo
            if (!WorldGen.InWorld((int)(Projectile.Center.X / 16), (int)(Projectile.Center.Y / 16), 50))
            {
                Projectile.position = Main.player[Projectile.owner].Center + Main.rand.NextVector2Circular(30f, 30f); // Aparecer cerca
                Projectile.velocity = Vector2.Zero;
                stuckTimer = 0; // Resetear
                jumpCooldown = 10;
                Projectile.netUpdate = true;
                SoundEngine.PlaySound(SoundID.Item8, Projectile.position);
            }
        }


        // --- NUEVO: Lógica de Movimiento Terrestre ---
        private void HandleMovement(Player owner)
        {
            bool isFrenzyActive = Projectile.localAI[0] > 0;
            NPC targetNPC = null;
            Vector2 targetPosition = owner.Center;
            Vector2 vectorToTarget = Vector2.Zero;
            bool hasTarget = false;
            bool shouldMove = false;

            // --- Lógica de Movimiento para Frenesí ---
            if (isFrenzyActive)
            {
                // Buscar objetivo agresivamente
                targetNPC = FindClosestEnemy(MaxDetectRadius * 1.5f, owner);
                if (targetNPC != null)
                {
                    vectorToTarget = targetNPC.Center - Projectile.Center;
                    Vector2 directionToTarget = vectorToTarget.SafeNormalize(Vector2.Zero);
                    float frenzySpeed = MaxGroundMoveSpeed * FrenzyChaseSpeedMult; // Usar multiplicador
                    Projectile.velocity = (Projectile.velocity * (FrenzyInertia - 1) + directionToTarget * frenzySpeed) / FrenzyInertia;

                    if (Projectile.velocity.LengthSquared() > frenzySpeed * frenzySpeed)
                    {
                        Projectile.velocity = Vector2.Normalize(Projectile.velocity) * frenzySpeed;
                    }
                }
                else
                {
                    vectorToTarget = owner.Center - Projectile.Center;
                    if (vectorToTarget.LengthSquared() > 150f * 150f)
                    {
                        Vector2 directionToOwner = vectorToTarget.SafeNormalize(Vector2.UnitY);
                        float returnSpeed = MaxGroundMoveSpeed * 1.3f;
                        float returnInertia = 12f;
                        Projectile.velocity = (Projectile.velocity * (returnInertia - 1) + directionToOwner * returnSpeed) / returnInertia;
                    }
                    else { Projectile.velocity *= 0.9f; }
                }
                stuckTimer = 0; // Resetear timer de atasco
                return; // Saltar lógica normal
            }
            // --- FIN Lógica de Movimiento para Frenesí ---

            // --- Lógica de Movimiento Normal ---
            targetNPC = FindClosestEnemy(MaxDetectRadius, owner);
            if (targetNPC != null)
            {
                targetPosition = targetNPC.Center;
                hasTarget = true;
            }
            else { targetPosition = owner.Center; } // Asegurar target es el owner si no hay NPC

            vectorToTarget = targetPosition - Projectile.Center;
            float distanceToTarget = vectorToTarget.Length();
            float distanceToTargetCenter = vectorToTarget.Length(); // Distancia al centro

            // Decidir si moverse
            // --- Decidir si Moverse (Con ajuste para parada) ---
            shouldMove = false;
            if (hasTarget)
            {
                // --- NUEVO: Calcular punto de parada cerca del borde del NPC ---
                float stopDistance = (Projectile.width / 2f) + (targetNPC.width / 2f) + 2f; // Distancia para detenerse (2px desde el borde)
                                                                                            // Reducir ligeramente si el NPC es muy pequeño? Opcional.
                                                                                            // stopDistance *= 0.9f;

                if (distanceToTargetCenter > stopDistance)
                { // Solo moverse si está MÁS LEJOS que la distancia de parada
                    shouldMove = true;
                    // Ajustar targetPosition para que apunte al borde, no al centro
                    // Esto ayuda a evitar que intente meterse "dentro" del NPC
                    vectorToTarget.Normalize(); // Obtener solo dirección
                    targetPosition = targetNPC.Center - vectorToTarget * (targetNPC.width / 2f + Projectile.width / 2f - 4f); // Apuntar un poco DENTRO del borde para asegurar el contacto
                    vectorToTarget = targetPosition - Projectile.Center; // Recalcular vector al nuevo punto objetivo
                }
                else
                {
                    // Si está DENTRO de la distancia de parada, no necesita moverse horizontalmente hacia él
                    shouldMove = false;
                    // Asegurar que mire al enemigo
                    Projectile.spriteDirection = Math.Sign(targetNPC.Center.X - Projectile.Center.X);
                    if (Projectile.spriteDirection == 0) Projectile.spriteDirection = owner.direction;
                }
            }
            else
            { // Si no hay NPC, seguir al jugador (lógica como antes)
                if (distanceToTargetCenter > FollowPlayerDistanceStart || (distanceToTargetCenter > FollowPlayerDistanceStop && Math.Sign(vectorToTarget.X) != 0 && Math.Sign(vectorToTarget.X) != owner.direction))
                {
                    shouldMove = true;
                }
            }

            // --- CONTADOR DE ATASCO ---
            if (shouldMove && isOnGround && Math.Abs(Projectile.velocity.X) < 0.5f && distanceToTargetCenter > StuckCheckDistance)
            {
                stuckTimer++;
            }
            else { stuckTimer = 0; }

            // --- Aplicar Movimiento Horizontal ---
            bool attemptingUnstickJump = stuckTimer >= StuckTimerMax;
            if (shouldMove && !attemptingUnstickJump)
            {
                int direction = Math.Sign(vectorToTarget.X);
                if (direction == 0) direction = owner.direction;

                if (isOnGround)
                {
                    Projectile.velocity.X += direction * GroundAcceleration;
                    if (Math.Abs(Projectile.velocity.X) > GroundMoveSpeed) { Projectile.velocity.X = Math.Sign(Projectile.velocity.X) * GroundMoveSpeed; }
                    if (Math.Sign(Projectile.velocity.X) != direction && Projectile.velocity.X != 0) { Projectile.velocity.X *= 0.9f; }
                }
                else
                {
                    Projectile.velocity.X += direction * AirAcceleration;
                    if (Math.Abs(Projectile.velocity.X) > MaxAirMoveSpeed) { Projectile.velocity.X = Math.Sign(Projectile.velocity.X) * MaxAirMoveSpeed; }
                }
            }
            else if (!shouldMove)
            {
                Projectile.velocity.X *= 0.9f;
                if (Math.Abs(Projectile.velocity.X) < 0.1f) Projectile.velocity.X = 0f;
            }

            // --- Lógica de Salto ---
            bool needsToJump = false;
            int jumpCheckDirection = Math.Sign(vectorToTarget.X);
            if (!shouldMove || jumpCheckDirection == 0) jumpCheckDirection = owner.direction;

            // 1. Salto Vertical de Desatasco
            if (attemptingUnstickJump && isOnGround && jumpCooldown <= 0)
            {
                Projectile.velocity.Y = -UnstickJumpForce;
                Projectile.velocity.X *= 0.3f;
                jumpCooldown = 45; // Cooldown largo post-desatasco
                stuckTimer = 0;
                needsToJump = false; // Ya salta, no evaluar más
                Projectile.netUpdate = true;
            }
            // 2. Otros Saltos (si no se desatasca)
            else if (isOnGround && jumpCooldown <= 0)
            {
                // Saltar por altura de objetivo
                if (vectorToTarget.Y < -Projectile.height * 2.5f && Math.Abs(vectorToTarget.X) < Projectile.width * 5)
                {
                    needsToJump = true;
                }

                // Saltar sobre obstáculos (con chequeo anticipado)
                float lookAheadFactor = 0.5f;
                float minLookAheadPixels = 8f;
                int checkDist = (int)Math.Max(minLookAheadPixels, Math.Abs(Projectile.velocity.X) * lookAheadFactor + Projectile.width * 0.5f);
                Point tileCheckPos = (Projectile.Center + new Vector2(jumpCheckDirection * checkDist, 0)).ToTileCoordinates();
                Point feetTilePos = (Projectile.Bottom + new Vector2(0, -1)).ToTileCoordinates();
                bool obstacleFeet = WorldGen.SolidTile(tileCheckPos.X, feetTilePos.Y) && !TileID.Sets.Platforms[Main.tile[tileCheckPos.X, feetTilePos.Y].TileType];
                bool obstacleKnee = WorldGen.SolidTile(tileCheckPos.X, feetTilePos.Y - 1) && !TileID.Sets.Platforms[Main.tile[tileCheckPos.X, feetTilePos.Y - 1].TileType];
                if (obstacleFeet || obstacleKnee)
                {
                    bool spaceAboveClear = !WorldGen.SolidTile(tileCheckPos.X, feetTilePos.Y - 2) && !WorldGen.SolidTile(tileCheckPos.X, feetTilePos.Y - 3);
                    if (spaceAboveClear)
                    {
                        needsToJump = true;
                    }
                }

                // Saltar para subir plataformas
                Point tileBelowFeet = (Projectile.Bottom + Vector2.UnitY).ToTileCoordinates();
                Tile tileBelow = Framing.GetTileSafely(tileBelowFeet.X, tileBelowFeet.Y);
                if (TileID.Sets.Platforms[tileBelow.TileType])
                {
                    int targetTileY = (int)(targetPosition.Y / 16);
                    if (targetTileY < tileBelowFeet.Y - 1)
                    {
                        needsToJump = true;
                        Projectile.velocity.Y -= JumpUpPlatformBoost;
                    }
                }

                // Ejecutar salto normal si es necesario
                if (needsToJump)
                {
                    Projectile.velocity.Y = -JumpHeight * 0.95f; // <-- MODIFICADO: Aumentar un poco la fuerza si 0.9 era bajo
                    jumpCooldown = 30; // <-- MODIFICADO: Cooldown razonable
                    Projectile.netUpdate = true;
                }
            } // Fin otros saltos


            // --- Seguimiento Vertical Suave ---
            bool tryVerticalFollow = !isOnGround || (hasTarget && vectorToTarget.Y < -80f);
            if (tryVerticalFollow && !isFrenzyActive)
            {
                float desiredY = owner.Center.Y + VerticalFollowHeight;
                float diffY = desiredY - Projectile.Center.Y;
                if (Math.Abs(diffY) > 10f)
                {
                    Projectile.velocity.Y += Math.Sign(diffY) * VerticalFollowStrength * Math.Clamp(Math.Abs(diffY) / 50f, 0.5f, 1.5f);
                }
                float maxVerticalFollowSpeed = MaxAirMoveSpeed * 0.8f;
                if (Projectile.velocity.Y < -maxVerticalFollowSpeed) { Projectile.velocity.Y = -maxVerticalFollowSpeed; }
            }

            // --- Teleport Final ---
            float distanceToOwnerFinal = Vector2.Distance(owner.Center, Projectile.Center);
            if (distanceToOwnerFinal > TeleportDistance)
            {
                Projectile.position.X = owner.Center.X - Projectile.width / 2f;
                Projectile.position.Y = owner.Center.Y - owner.height / 2f - Projectile.height - 8f;
                Projectile.velocity = Vector2.Zero;
                Projectile.ai[0] = 0;
                Projectile.localAI[0] = 0;
                Projectile.localAI[1] = 0;
                jumpCooldown = 10;
                stuckTimer = 0; // Resetear al teleportar
                Projectile.netUpdate = true;
                SoundEngine.PlaySound(SoundID.Item8, Projectile.position);
            }

        } // Fin HandleMovement

        // --- NUEVO: Control de Animación ---
        // --- NUEVO: Control de Animación (Basado en tu Sheet) ---
        private void HandleAnimation()
        {
            // --- Definición de Frames ---
            const int IdleBaseFrame = 0;
            const int LickAnimStartFrame = 0; // Empieza desde el frame 0
            const int LickAnimEndFrame = 4;   // Termina en el frame 4
            const int WalkStartFrame = 5;
            const int WalkEndFrame = 13;
            const int AirStartFrame = 14;
            const int AirEndFrame = 17;
            const int AttackStartFrame = 18;
            const int AttackEndFrame = 19;
            const int FrenzyStartFrame = 20;
            const int FrenzyEndFrame = 23;

            // --- Definición de Velocidades ---
            const int WalkFrameSpeed = 6; // Ticks por frame de caminar (ajusta)
            const int AirFrameSpeed = 12;  // Ticks por frame en aire (ajusta)
            const int AttackFrameSpeed = 20; // Ticks por frame de ataque normal (ajusta)
            const int FrenzyFrameSpeed = 8; // Ticks por frame de ataque frenesí (ajusta)
            const int LickAnimationFrameSpeed = 15; // Ticks por frame de lamerse (ajusta)

            bool isFrenzyActive = Projectile.localAI[0] > 0; // Timer de duración de frenesí
            bool isAttackingNormally = Projectile.ai[0] > NormalAttackCooldown - (AttackFrameSpeed * (AttackEndFrame - AttackStartFrame + 1)); // Está en cooldown pero mostrando la animación

            // --- Resetear Rotación ---
            // (El frenesí podría añadirla de nuevo si quieres)
            Projectile.rotation = 0f;

            // --- Prioridad 1: Animación de Frenesí ---
            if (isFrenzyActive)
            {
                // Girar si quieres
                // Projectile.rotation += 0.4f * Projectile.spriteDirection;

                // Ciclar frames de Frenesí (20-23)
                Projectile.frameCounter++;
                if (Projectile.frameCounter >= FrenzyFrameSpeed)
                {
                    Projectile.frameCounter = 0;
                    Projectile.frame++;
                    if (Projectile.frame < FrenzyStartFrame || Projectile.frame > FrenzyEndFrame)
                    {
                        Projectile.frame = FrenzyStartFrame; // Volver al inicio del ciclo de frenesí
                    }
                }
                // Establecer dirección del sprite basado en hacia dónde se mueve (si se mueve) o hacia el jugador
                if (Math.Abs(Projectile.velocity.X) > 0.1f) Projectile.spriteDirection = Math.Sign(Projectile.velocity.X);
                else Projectile.spriteDirection = Main.player[Projectile.owner].direction; // Mirar hacia el jugador si está quieto
                return; // Salir, animación de frenesí tiene prioridad
            }

            // --- Prioridad 2: Animación de Ataque Normal ---
            if (isAttackingNormally)
            {
                // Ciclar frames de Ataque Normal (18-19)
                Projectile.frameCounter++;
                if (Projectile.frameCounter >= AttackFrameSpeed)
                {
                    Projectile.frameCounter = 0;
                    Projectile.frame++;
                    if (Projectile.frame < AttackStartFrame || Projectile.frame > AttackEndFrame)
                    {
                        Projectile.frame = AttackStartFrame; // Volver al inicio del ciclo de ataque
                    }
                }
                // Fijar dirección hacia el objetivo durante el ataque
                // (Asumiendo que tienes una forma de saber el último objetivo o la dirección del golpe)
                // O simplemente mantener la dirección actual
                if (Math.Abs(Projectile.velocity.X) > 0.1f) Projectile.spriteDirection = Math.Sign(Projectile.velocity.X);
                return; // Salir, animación de ataque tiene prioridad
            }

            // --- Prioridad 3: Animación en Aire ---
            if (!isOnGround)
            {
                playingLickAnimation = false; // Cancelar animación de lamerse si salta/cae
                idleTimer = 0; // Resetear timer de idle

                // Ciclar frames de Aire (14-17)
                Projectile.frameCounter++;
                if (Projectile.frameCounter >= AirFrameSpeed)
                {
                    Projectile.frameCounter = 0;
                    Projectile.frame++;
                    if (Projectile.frame < AirStartFrame || Projectile.frame > AirEndFrame)
                    {
                        Projectile.frame = AirStartFrame; // Volver al inicio del ciclo de aire
                    }
                }
                // Dirección basada en movimiento horizontal
                if (Math.Abs(Projectile.velocity.X) > 0.1f) Projectile.spriteDirection = Math.Sign(Projectile.velocity.X);
                return; // Salir, animación de aire tiene prioridad
            }

            // --- Si está en el Suelo y no Atacando/Frenesí ---

            // --- Prioridad 4: Animación de Caminar ---
            if (Math.Abs(Projectile.velocity.X) > 0.1f)
            {
                playingLickAnimation = false; // Cancelar animación de lamerse si empieza a caminar
                idleTimer = 0; // Resetear timer de idle

                Projectile.spriteDirection = Math.Sign(Projectile.velocity.X);
                // Ciclar frames de Caminar (5-13)
                Projectile.frameCounter++;
                if (Projectile.frameCounter >= WalkFrameSpeed)
                {
                    Projectile.frameCounter = 0;
                    Projectile.frame++;
                    if (Projectile.frame < WalkStartFrame || Projectile.frame > WalkEndFrame)
                    {
                        Projectile.frame = WalkStartFrame; // Volver al inicio del ciclo de caminar
                    }
                }
            }
            // --- Prioridad 5: Animación de Idle (Base o Lamerse) ---
            else // Está quieto en el suelo
            {
                Projectile.velocity.X = 0; // Asegurar X=0
                Projectile.spriteDirection = Main.player[Projectile.owner].direction; // Mirar en la misma dirección que el jugador

                // Si estamos reproduciendo la animación de lamerse
                if (playingLickAnimation)
                {
                    Projectile.frameCounter++;
                    if (Projectile.frameCounter >= LickAnimationFrameSpeed)
                    {
                        Projectile.frameCounter = 0;
                        Projectile.frame++;
                        // Si hemos llegado al final de la animación de lamerse (frame 4)
                        if (Projectile.frame > LickAnimEndFrame)
                        {
                            Projectile.frame = IdleBaseFrame; // Volver al frame base
                            playingLickAnimation = false; // Terminar animación
                            idleTimer = 0; // Resetear timer para empezar los 5 segundos de nuevo
                        }
                        // Asegurarse de que no salga del rango 0-4 mientras se lame
                        else if (Projectile.frame < LickAnimStartFrame)
                        {
                            Projectile.frame = LickAnimStartFrame;
                        }
                    }
                }
                // Si NO estamos reproduciendo la animación de lamerse (Estado Idle Base)
                else
                {
                    Projectile.frame = IdleBaseFrame; // Mostrar frame 0
                    Projectile.frameCounter = 0; // Resetear contador de frame
                    idleTimer++; // Incrementar timer de idle base
                                 // Si han pasado 5 segundos
                    if (idleTimer >= IdleBaseDuration)
                    {
                        playingLickAnimation = true; // Empezar animación de lamerse
                        Projectile.frame = LickAnimStartFrame; // Ir al primer frame de lamerse (que también es 0)
                        Projectile.frameCounter = 0; // Resetear contador para la nueva animación
                                                     // No reseteamos idleTimer aquí, se resetea cuando termina la animación de lamerse
                    }
                }
            }
        }

        // --- NUEVO: Prevenir Destrucción al Chocar con Tiles ---
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            // Este método se llama cuando Projectile.tileCollide es true y el proyectil choca.
            // Por defecto, devuelve 'true', lo que causa que Projectile.Kill() se llame.
            // Al devolver 'false', prevenimos la destrucción.

            // Opcional: Podrías añadir lógica aquí si quieres que haga algo específico al chocar,
            // como emitir un sonido o ajustar ligeramente la posición/velocidad de forma diferente
            // a como lo hace Collision.TileCollision, pero generalmente no es necesario si
            // ApplyGravityAndTileCollisions está funcionando bien.

            // Ejemplo: Si choca con una pared horizontalmente, poner velocidad X a 0.
            if (Projectile.velocity.X != oldVelocity.X && Math.Abs(oldVelocity.X) > 0.1f) // Cambió la velocidad X?
            {
                Projectile.velocity.X = 0f;
            }
            // Ejemplo: Si choca con el techo verticalmente, poner velocidad Y a 0.
            if (Projectile.velocity.Y != oldVelocity.Y && oldVelocity.Y < -0.1f) // Cambió la velocidad Y y estaba subiendo?
            {
                Projectile.velocity.Y = 0f;
            }

            // Lo más importante es devolver false para que NO se destruya.
            return false;
        }


        // --- CombatAI (Ligeros ajustes posibles) ---
        private void CombatAI(Player owner)
        {
            // La lógica de activación/duración de Frenesí y cooldowns no cambia.
            bool isFrenzyActive = Projectile.localAI[0] > 0;

            // Activar Frenesí (sin cambios)
            if (Projectile.ai[1] == 1f && !isFrenzyActive)
            {
                Projectile.ai[1] = 0f;
                Projectile.localAI[0] = FrenzyDuration;
                Projectile.localAI[1] = 0;
                SoundEngine.PlaySound(SoundID.Zombie89, Projectile.Center);
                Projectile.netUpdate = true;
                isFrenzyActive = true;
            }

            // Comportamiento durante el Frenesí (sin cambios principales)
            if (isFrenzyActive)
            {
                if (Projectile.localAI[1] <= 0)
                {
                    Projectile.localAI[1] = FrenzyAttackRate;
                    PerformFrenzyAttack(owner);
                }
                // Asegurarse que no intente moverse hacia objetivo durante frenesí
                // (Ya se maneja en HandleMovement)
                return;
            }

            // Comportamiento de Ataque Normal
            if (Projectile.ai[0] <= 0) // Si el cooldown está listo
            {
                NPC target = FindClosestEnemy(AttackRange * 1.5f, owner); // Buscar un poco más allá del rango de ataque
                if (target != null)
                {
                    // Comprobar si está DENTRO del rango de ataque MELEE real
                    if (Projectile.Hitbox.Intersects(target.Hitbox) || Vector2.Distance(Projectile.Center, target.Center) <= AttackRange)
                    {
                        PerformNormalAttack(target, owner);
                        Projectile.ai[0] = NormalAttackCooldown; // Poner en cooldown
                        // Reducir velocidad X al atacar para que no "patine" sobre el enemigo
                        Projectile.velocity.X *= 0.6f;
                    }
                    // Si el objetivo existe pero está fuera de rango, HandleMovement ya se encarga de acercarse.
                }
            }
        }

        // --- PerformNormalAttack (Ajustar frame de ataque) ---
        private void PerformNormalAttack(NPC target, Player owner)
        {
            int hitDirection = Math.Sign(target.Center.X - Projectile.Center.X);
            if (hitDirection == 0) hitDirection = 1;

            bool isFrenzyActive = Projectile.localAI[0] > 0;
            if (isFrenzyActive)
            {
                return; // El daño de Frenesí se maneja por separado
            }



            // Calcular Daño Porcentual Aleatorio (1% a 10%)
            float minPercent = 0.01f;
            float maxPercent = 0.08f;
            float randomPercent = Main.rand.NextFloat(minPercent, maxPercent);
            int calculatedDamage = Math.Max(1, (int)(target.lifeMax * randomPercent));

            // --- Aplicar Daño Directamente con SimpleStrikeNPC (Usando Parámetros Posicionales) ---

            if (hitDirection == 0) hitDirection = 1;

            target.SimpleStrikeNPC(
                calculatedDamage, // 1. damage (int)
                hitDirection,     // 2. hitDirection (int, opcional, default 0)
                Main.rand.Next(100) < owner.GetCritChance(DamageClass.Summon), // 3. crit (bool, opcional, default false) - Calcula crítico basado en el jugador
                0.1f,             // 4. knockback (float, opcional, default 0f) <-- ¡Knockback bajo aquí!
                DamageClass.Summon, // 5. damageType (int, opcional, default -1)
                true              // 6. armorPenetration (bool, opcional, default false) <-- Ignorar defensa
            );

            // Aplicar Inmunidad
            target.immune[Projectile.owner] = 45;

            // Efectos Visuales/Sonoros
            SoundEngine.PlaySound(SoundID.NPCHit1 with { Volume = 0.5f, Pitch = 0.2f }, Projectile.position);
            for (int i = 0; i < 3; i++)
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Tin, hitDirection * 0.5f, -0.5f);
            }

            // Aplicar daño (ModifyHitNPC se encarga del %)
            owner.ApplyDamageToNPC(target, Projectile.damage, Projectile.knockBack, hitDirection, false, DamageClass.Summon);

            // --- AÑADIR I-FRAMES ---
            // Dar inmunidad al NPC contra ESTE JUGADOR durante el cooldown del ataque
            // Si NormalAttackCooldown es 60, darle 60 ticks de inmunidad está bien.
            if (NormalAttackCooldown > 0)
            {
                target.immune[Projectile.owner] = NormalAttackCooldown;
            }

            // --- FIN AÑADIR I-FRAMES ---

            // Sonido y Frame de Ataque
            SoundEngine.PlaySound(SoundID.NPCHit1, Projectile.Center); // Sonido de golpe melee suave
        }

        // --- PerformFrenzyAttack (Sin cambios principales) ---
        private void PerformFrenzyAttack(Player owner)
        {
            float frenzyRadius = 60f;
            Rectangle frenzyHitbox = Utils.CenteredRectangle(Projectile.Center, new Vector2(frenzyRadius * 2));
            SoundEngine.PlaySound(SoundID.NPCHit9 with { Volume = 0.6f, Pitch = 0.4f }, Projectile.Center);

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                // Comprobar si puede ser golpeado Y SI NO ES YA INMUNE por un golpe anterior de este mismo ataque/jugador
                if (npc.CanBeChasedBy(this, false) && npc.immune[Projectile.owner] <= 0 && frenzyHitbox.Intersects(npc.Hitbox))
                {
                    float knockback = 1f;
                    int hitDirection = Math.Sign(npc.Center.X - Projectile.Center.X);
                    if (hitDirection == 0) hitDirection = 1;

                    // Aplicar daño (ModifyHitNPC se encarga del %)
                    owner.ApplyDamageToNPC(npc, Projectile.damage, knockback, hitDirection, false, DamageClass.Summon);

                    // --- AÑADIR I-FRAMES (MÁS CORTOS PARA FRENESÍ) ---
                    // Queremos que coincida con el FrenzyAttackRate (30 ticks = 0.5 seg)
                    if (FrenzyAttackRate > 0)
                    {
                        npc.immune[Projectile.owner] = FrenzyAttackRate;
                    }

                    // --- FIN AÑADIR I-FRAMES ---
                }
            }
            // Efecto visual (sin cambios)
            for (int i = 0; i < 5; i++) { Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(frenzyRadius * 0.8f, frenzyRadius * 0.8f), DustID.Blood, Main.rand.NextVector2Circular(1f, 1f), 0, default, 1.1f); d.noGravity = true; }
        }


        // --- FindClosestEnemy (Sin cambios) ---
        private NPC FindClosestEnemy(float maxRange, Player owner)
        {
            NPC closestNPC = null;
            float SqrMaxRange = maxRange * maxRange;
            Vector2 center = Projectile.Center;

            if (owner.HasMinionAttackTargetNPC)
            {
                NPC target = Main.npc[owner.MinionAttackTargetNPC];
                if (target.CanBeChasedBy(this, false) && Vector2.DistanceSquared(center, target.Center) < SqrMaxRange)
                {
                    return target;
                }
            }

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.CanBeChasedBy(this, false))
                {
                    float sqrDist = Vector2.DistanceSquared(center, npc.Center);
                    if (sqrDist < SqrMaxRange)
                    {
                        // Opcional: Comprobar línea de visión
                        if (Collision.CanHitLine(Projectile.position, Projectile.width, Projectile.height, npc.position, npc.width, npc.height))
                        {
                            SqrMaxRange = sqrDist;
                            closestNPC = npc;
                        }
                    }
                }
            }
            return closestNPC;
        }

        // --- ModifyHitNPC (Sin cambios) ---
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            bool isFrenzyAttack = Projectile.localAI[0] > 0;
            float percentBonus = isFrenzyAttack ? 0.025f : 0.05f; // 2.5% frenesí, 5% normal
            float percentDamage = Math.Max(1f, target.lifeMax * percentBonus); // Mínimo 1
            modifiers.FlatBonusDamage += percentDamage; // Añade al daño base + bonos jugador
            modifiers.DefenseEffectiveness *= 0f; // Ignora defensa
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Obtener la textura (asume que ya tienes tu spritesheet cargado o usa ModContent.Request)
            // Si tienes múltiples texturas, necesitarás lógica para seleccionarla aquí.
            // Asumiendo una única textura para todos los frames:
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value; // Obtiene la textura del path definido en SetDefaults o este override

            // Obtener el frame actual de la animación
            int frameHeight = texture.Height / Main.projFrames[Type];
            int currentFrameY = Projectile.frame * frameHeight;
            Rectangle sourceRectangle = new Rectangle(0, currentFrameY, texture.Width, frameHeight);

            // --- CALCULAR EL ORIGEN CORRECTO ---
            // Para un personaje terrestre, el origen suele ser el centro inferior del frame.
            Vector2 origin = new Vector2(sourceRectangle.Width / 2f, frameHeight); // Centro X, Base Y

            // --- CALCULAR POSICIÓN DE DIBUJO ---
            // Anclar el origen (pies) al centro inferior de la hitbox del proyectil.
            // Projectile.Bottom da la coordenada Y de la base de la hitbox.
            // Necesitamos ajustar ligeramente si la hitbox no representa perfectamente los pies.
            // --- CALCULAR POSICIÓN DE DIBUJO (CON AJUSTE) ---
            // Anclar el origen (pies) al centro inferior de la hitbox y luego desplazar hacia abajo
            Vector2 drawPos = new Vector2(Projectile.Center.X, Projectile.Bottom.Y) - Main.screenPosition;
            drawPos.Y += 2f; // <-- AÑADIDO: Mover 2 píxeles hacia abajo

            Color drawColor = Projectile.GetAlpha(lightColor);
            SpriteEffects effects = Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            Main.EntitySpriteDraw(
                texture,
                drawPos, // Usa la posición ajustada
                sourceRectangle,
                drawColor,
                Projectile.rotation, // Usamos 0f en la animación, pero lo dejamos por si acaso
                origin,              // ¡USA EL ORIGEN CALCULADO!
                Projectile.scale,    // Escala (normalmente 1f)
                effects,
                0f); // Layer depth (0f es estándar)

            // Impedir que Terraria dibuje el sprite por defecto
            return false;
        }
        public override string Texture => "WakfuMod/Content/Projectiles/ZurcarakMinion"; // Ruta a tu spritesheet
    } // Fin Clase ZurcarakMinion
}