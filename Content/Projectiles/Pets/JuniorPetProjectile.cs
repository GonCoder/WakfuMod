using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using WakfuMod.Content.Buffs;
using Terraria.Audio; // Asegúrate que este es el namespace correcto para tu JuniorBuff

namespace WakfuMod.Content.Projectiles.Pets
{
    public class JuniorPetProjectile : ModProjectile // Asumiendo que lo renombraste así
    {
        // --- Constantes de Animación según tu descripción ---
        private const int FrameIdleStart = 0;
        private const int FrameIdleEnd = 1;
        private const int FrameAirStart = 2;
        private const int FrameAirEnd = 3;
        private const int FrameWalkStart = 4;
        private const int FrameWalkEnd = 8; // Frames 4,5,6,7,8

        private const int AnimSpeedIdle = 15;
        private const int AnimSpeedAir = 10;
        private const int AnimSpeedWalk = 8;

        // --- Constantes de IA (Terrestre) ---
        private const float FollowDistanceStart = 120f;
        private const float FollowDistanceStop = 50f; // Que se detenga un poco más cerca
        private const float TeleportDistance = 1000f;
        private const float GroundMoveSpeed = 4.5f;    // Velocidad de mascota (más lenta que minion)
        private const float MaxGroundMoveSpeed = 6.5f;
        private const float GroundAcceleration = 0.12f;
        private const float AirAcceleration = 0.08f;
        private const float MaxAirMoveSpeed = 4f;
        private const float JumpHeight = 9.5f;       // Salto de mascota
        private const float Gravity = 0.3f;
        private const float MaxFallSpeed = 8f;

        private int potentiallyStuckTimer = 0; // Contador para el "salto de desatasco"
        private const int MaxStuckTime = 60;    // 1 segundo antes de intentar desatascarse
        private const float MinStuckDistance = 100f; // Distancia mínima para considerar "atascado"
        private const float UnstickPetJumpForce = 10.5f; // Salto un poco más fuerte para desatascarse

        private bool isOnGround = false;
        private int jumpCooldown = 0;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 9; // Total de frames (0-8)
            Main.projPet[Projectile.type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 26;  // Ajusta
            Projectile.height = 24; // Ajusta
            Projectile.aiStyle = -1; // IA Personalizada
            Projectile.friendly = true; // No hace daño
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.tileCollide = true; // Colisiona con tiles
            Projectile.ignoreWater = false;
            Projectile.netImportant = true; // Importante para mascotas

            // NO USAR AIType si queremos control total
            // AIType = ProjectileID.Bunny; // <-- ELIMINAR O COMENTAR ESTA LÍNEA
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];

            if (owner.dead || !owner.active)
            {
                Projectile.active = false; // En lugar de Kill() directamente, prueba desactivar primero
                return;
            }

            bool hasBuff = owner.HasBuff(ModContent.BuffType<JuniorBuff>()); // Nombre correcto del buff

            if (hasBuff)
            {
                Projectile.timeLeft = 2;
            }
            else
            {
                // Si el buff no está, el proyectil debe desactivarse
                Projectile.active = false; // Desactivar
                                           // Main.NewText("Pet projectile deactivated due to missing buff.", Color.Orange); // DEBUG
                return; // Importante salir para que no siga ejecutando AI
            }

            if (jumpCooldown > 0) jumpCooldown--;

            // --- LÓGICA DE IA PERSONALIZADA ---
            ApplyGravityAndTileCollisions();
            HandlePetMovement(owner);
            HandlePetAnimation(); // Tu animación personalizada
        }


        // --- LÓGICA DE GRAVEDAD Y COLISIÓN (Adaptada del ZurcarakMinion) ---
        private void ApplyGravityAndTileCollisions()
        {
            Projectile.velocity.Y += Gravity;
            if (Projectile.velocity.Y > MaxFallSpeed) Projectile.velocity.Y = MaxFallSpeed;

            Vector2 oldVelocity = Projectile.velocity;
            Projectile.velocity = Collision.TileCollision(Projectile.position, Projectile.velocity, Projectile.width, Projectile.height, true, true); // fallThrough y fall2 en true

            isOnGround = Math.Abs(Projectile.velocity.Y) < 0.01f && oldVelocity.Y >= 0f;

            if (isOnGround)
            {
                Projectile.velocity.Y = 0f;
                // Ajuste fino de posición
                Point tileCoords = (Projectile.Bottom + Vector2.UnitY).ToTileCoordinates();
                Tile tileBelow = Framing.GetTileSafely(tileCoords.X, tileCoords.Y);
                if (tileBelow.HasTile && (Main.tileSolid[tileBelow.TileType] || TileID.Sets.Platforms[tileBelow.TileType]))
                {
                    Projectile.position.Y = tileCoords.Y * 16f - Projectile.height;
                }
            }

            // Teleport si se sale del mundo
            if (!WorldGen.InWorld((int)(Projectile.Center.X / 16), (int)(Projectile.Center.Y / 16), 30))
            { // Margen más pequeño para teleport
                Player owner = Main.player[Projectile.owner];
                Projectile.position = owner.Center; // Usar directamente owner de AI()
                Projectile.velocity = Vector2.Zero;
                jumpCooldown = 10;
                Projectile.netUpdate = true;
                // SoundEngine.PlaySound(SoundID.Item6, Projectile.position); // Sonido diferente para pet teleport. El del cristal home (no pega nada)
            }
        }

        // --- LÓGICA DE MOVIMIENTO DE MASCOTA (Adaptada) ---
        private void HandlePetMovement(Player owner)
        {
            Vector2 targetPosition = owner.Center;
            Vector2 vectorToPlayer = targetPosition - Projectile.Center;
            float distanceToPlayer = vectorToPlayer.Length();
            bool shouldMove = false;

            // Decidir si moverse hacia el jugador
            if (distanceToPlayer > FollowDistanceStart || (distanceToPlayer > FollowDistanceStop && Math.Sign(vectorToPlayer.X) != 0 && Math.Sign(vectorToPlayer.X) != owner.direction))
            {
                shouldMove = true;
            }

            // --- LÓGICA DE DESATASCO ---
            if (!shouldMove && isOnGround && distanceToPlayer > MinStuckDistance && Math.Abs(Projectile.velocity.X) < 0.1f)
            {
                potentiallyStuckTimer++; // Incrementar si está quieto, en suelo y lejos
            }
            else
            {
                potentiallyStuckTimer = 0; // Resetear si se mueve, está en aire o está cerca
            }

            bool attemptUnstickJump = potentiallyStuckTimer >= MaxStuckTime;

            // --- SALTO DE DESATASCO (PRIORIDAD) ---
            if (attemptUnstickJump && isOnGround && jumpCooldown <= 0)
            {

                Projectile.velocity.Y = -UnstickPetJumpForce; // Salto vertical fuerte

                // --- AÑADIR IMPULSO HACIA ATRÁS ---
                // Moverlo ligeramente en la dirección OPUESTA a la que mira el jugador
                // o a la que intentaba moverse. Usar la dirección del jugador es más simple
                // si no estaba intentando moverse activamente hacia un objetivo.

                Projectile.velocity.X = -owner.direction * 4f; // Impulso de 4px/tick hacia atrás (ajusta la fuerza)
                                                               // Usar -Projectile.spriteDirection * 4f; si quieres que sea opuesto a donde mira la mascota

                jumpCooldown = 45; // Cooldown un poco más largo
                potentiallyStuckTimer = 0;
                Projectile.netUpdate = true;
                // No hacer más lógica de movimiento o salto este frame
            }
            // --- FIN SALTO DE DESATASCO ---
            else // Si no está intentando el salto de desatasco, proceder con movimiento normal
            {
                // Movimiento Horizontal (como antes)
                if (shouldMove)
                {
                    int direction = Math.Sign(vectorToPlayer.X);
                    if (direction == 0) direction = owner.direction;

                    if (isOnGround)
                    {
                        Projectile.velocity.X += direction * GroundAcceleration;
                        if (Math.Abs(Projectile.velocity.X) > GroundMoveSpeed) { Projectile.velocity.X = Math.Sign(Projectile.velocity.X) * GroundMoveSpeed; }
                    }
                    else
                    { // En aire
                        Projectile.velocity.X += direction * AirAcceleration;
                        if (Math.Abs(Projectile.velocity.X) > MaxAirMoveSpeed) { Projectile.velocity.X = Math.Sign(Projectile.velocity.X) * MaxAirMoveSpeed; }
                    }
                }
                else
                { // Si está cerca del jugador, frenar
                    Projectile.velocity.X *= 0.85f;
                    if (Math.Abs(Projectile.velocity.X) < 0.1f) Projectile.velocity.X = 0f;
                }

                // Ajustar dirección del sprite (como antes)
                if (Math.Abs(Projectile.velocity.X) > 0.1f)
                {
                    Projectile.spriteDirection = Math.Sign(Projectile.velocity.X);
                }
                else
                {
                    Projectile.spriteDirection = owner.direction;
                }

                // Lógica de Salto Normal (Superar obstáculos de 1 tile)
                if (shouldMove && isOnGround && jumpCooldown <= 0) // Solo intentar salto normal si "shouldMove"
                {
                    int direction = Projectile.spriteDirection;
                    int checkDist = (int)(Projectile.width * 0.6f);
                    Point tileCheckPos = (Projectile.Center + new Vector2(direction * checkDist, 0)).ToTileCoordinates();
                    Point feetTilePos = (Projectile.Bottom + new Vector2(0, -1)).ToTileCoordinates();

                    if (WorldGen.SolidTile(tileCheckPos.X, feetTilePos.Y) && !TileID.Sets.Platforms[Main.tile[tileCheckPos.X, feetTilePos.Y].TileType])
                    {
                        if (!WorldGen.SolidTile(tileCheckPos.X, feetTilePos.Y - 1) && !WorldGen.SolidTile(tileCheckPos.X, feetTilePos.Y - 2))
                        {
                            Projectile.velocity.Y = -JumpHeight;
                            jumpCooldown = 30;
                            Projectile.netUpdate = true;
                        }
                    }
                    else if (TileID.Sets.Platforms[Main.tile[feetTilePos.X, feetTilePos.Y + 1].TileType] && owner.Center.Y < Projectile.Center.Y - 32f)
                    {
                        Projectile.velocity.Y = -JumpHeight * 0.8f;
                        jumpCooldown = 30;
                        Projectile.netUpdate = true;
                    }
                }
            } // Fin del "else" del salto de desatasco

            // Teleport si está muy lejos (como antes)
            if (distanceToPlayer > TeleportDistance)
            {
                Projectile.position.X = owner.Center.X - Projectile.width / 2f;
                Projectile.position.Y = owner.Center.Y - owner.height / 2f - Projectile.height - 8f;
                Projectile.velocity = Vector2.Zero;
                jumpCooldown = 10;
                potentiallyStuckTimer = 0; // Resetear al teleportar
                Projectile.netUpdate = true;
                // SoundEngine.PlaySound(SoundID.Item6, Projectile.position);
            }
        }


        // --- ANIMACIÓN PERSONALIZADA ---
        private void HandlePetAnimation()
        {
            // Determinar estado actual
            bool walking = Math.Abs(Projectile.velocity.X) > 0.2f && isOnGround; // Umbral de velocidad más bajo para caminar
            bool inAir = !isOnGround;

            Projectile.frameCounter++;

            if (inAir)
            { // Frames 2-3
                if (Projectile.frameCounter >= AnimSpeedAir)
                {
                    Projectile.frameCounter = 0;
                    Projectile.frame++;
                    if (Projectile.frame < FrameAirStart || Projectile.frame > FrameAirEnd)
                    {
                        Projectile.frame = FrameAirStart;
                    }
                }
            }
            else if (walking)
            { // Frames 4-8
                if (Projectile.frameCounter >= AnimSpeedWalk)
                {
                    Projectile.frameCounter = 0;
                    Projectile.frame++;
                    if (Projectile.frame < FrameWalkStart || Projectile.frame > FrameWalkEnd)
                    {
                        Projectile.frame = FrameWalkStart;
                    }
                }
            }
            else
            { // Idle: Frames 0-1
                if (Projectile.frameCounter >= AnimSpeedIdle)
                {
                    Projectile.frameCounter = 0;
                    Projectile.frame++;
                    if (Projectile.frame < FrameIdleStart || Projectile.frame > FrameIdleEnd)
                    {
                        Projectile.frame = FrameIdleStart;
                    }
                }
            }
        }

        private void CheckActive(Player player)
        {
            // --- ASEGÚRATE DE USAR EL NOMBRE CORRECTO DE TU BUFF AQUÍ ---
            if (player.dead || !player.active)
            {
                player.ClearBuff(ModContent.BuffType<JuniorBuff>()); // O MiMascotaBuff
            }
            if (player.HasBuff(ModContent.BuffType<JuniorBuff>()))
            { // O MiMascotaBuff
                Projectile.timeLeft = 2;
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            // Prevenir destrucción y permitir rebotes suaves si quieres
            if (Projectile.velocity.X != oldVelocity.X && Math.Abs(oldVelocity.X) > 1f)
            {
                Projectile.velocity.X = oldVelocity.X * -0.5f; // Rebote suave
            }
            if (Projectile.velocity.Y != oldVelocity.Y && oldVelocity.Y > 1f)
            { // Si estaba cayendo
                Projectile.velocity.Y = oldVelocity.Y * -0.3f; // Rebote suave hacia arriba
            }
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            int frameHeight = texture.Height / Main.projFrames[Type];
            int currentFrameY = Projectile.frame * frameHeight;
            Rectangle sourceRectangle = new Rectangle(0, currentFrameY, texture.Width, frameHeight);

            // Origen: Centro X, Base Y del FRAME del sprite
            Vector2 origin = new Vector2(sourceRectangle.Width / 2f, frameHeight);

            // --- AJUSTE DE POSICIÓN DE DIBUJO ---
            // Posición base: Alinear el 'origin' (pies del sprite) con Projectile.Bottom.X (centro) y Projectile.Bottom.Y (base de la hitbox)
            Vector2 drawPos = new Vector2(Projectile.Center.X, Projectile.Bottom.Y) - Main.screenPosition;

            // Sumar el desplazamiento que YA TENÍAS para bajarlo visualmente:
            drawPos.Y += 6f; // Tu ajuste previo para bajarlo 2px respecto a la hitbox

            // --- NUEVO AJUSTE ADICIONAL ---
            // Si el sprite SIGUE HUNDIDO, necesitas SUBIRLO VISUALMENTE.
            // Esto significa RESTAR de drawPos.Y.
            // El valor exacto dependerá de cuánto espacio "vacío" o "extra" tenga tu sprite
            // por debajo de los pies reales del personaje dentro de su frame.
            // Prueba con valores pequeños y ajusta.
            // Ejemplo: Si los pies están 4 píxeles por encima del borde inferior del frame:
            // drawPos.Y -= 4f;

            // O, una forma más general de pensar es: ¿cuál es el offset vertical
            // desde el borde inferior de tu frame del sprite hasta donde realmente están los pies?
            float feetOffsetInSprite = 4f; // EJEMPLO: Si los pies están 4px "hacia arriba" desde el borde inferior del frame del sprite.
                                           // Si los pies están EXACTAMENTE en el borde inferior del frame, este valor sería 0.
                                           // Si el sprite tiene espacio vacío debajo de los pies, este valor podría ser negativo.
            drawPos.Y -= feetOffsetInSprite;


            Color drawColor = Projectile.GetAlpha(lightColor);
            SpriteEffects effects = Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            Main.EntitySpriteDraw(
                texture,
                drawPos, // Usa la posición ajustada
                sourceRectangle,
                drawColor,
                Projectile.rotation,
                origin,
                Projectile.scale,
                effects,
                0f);

            return false;
        }
    }
}