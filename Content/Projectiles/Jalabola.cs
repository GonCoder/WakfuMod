// Content/Projectiles/Jalabola.cs (Nombre cambiado como sugeriste)
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria.Audio;
using WakfuMod.ModSystems;      // Para FootballSystem
using WakfuMod.Content.Tiles; // Para GoalTileRed/Blue
using ReLogic.Content;
using Terraria.DataStructures;

namespace WakfuMod.Content.Projectiles
{
    public class Jalabola : ModProjectile // Nombre de clase cambiado
    {
        // --- Propiedades y Constantes de Física ---
        public float State { get => Projectile.localAI[1]; set => Projectile.localAI[1] = value; } // 0=Idle, 1=Flying, 2=Bouncing, 3=Settled

        // --- Usa las constantes que funcionaban para la bomba ---
        private const float BallGravity = 0.3f;      // Gravedad (TymadorBomb tenía 0.3f)
        private const float BallMaxFallSpeed = 10f;  // Velocidad caída máxima (TymadorBomb tenía 10f)
        private const float BounceFactor = 0.5f;     // Elasticidad (TymadorBomb tenía 0.5f)
        private const float DragFactor = 0.98f;      // Fricción (TymadorBomb tenía 0.98f)
        private const int DefaultTimeLeft = 9800;    // Tiempo de vida largo (TymadorBomb tenía 9800)

        // --- Textura ---
        private static Asset<Texture2D> _ballTexture;

        // --- Carga/Descarga de Recursos ---
        public override void Load()
        {
            if (!Main.dedServ)
            {
                _ballTexture = ModContent.Request<Texture2D>("WakfuMod/Content/Projectiles/Jalabola"); // Ruta a tu sprite de pelota
            }
        }
        public override void Unload()
        {
            _ballTexture = null;
        }

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Jalabol"); // .hjson
        }

        // --- Configuración por Defecto ---
        public override void SetDefaults()
        {
            Projectile.width = 24;          // Ajusta al tamaño de tu sprite de pelota
            Projectile.height = 24;
            Projectile.aiStyle = -1;        // AI personalizada
            Projectile.friendly = true;    // <<--- NO es friendly por defecto (no daña NPCs)
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Generic; // Irrelevante si no hace daño
            Projectile.penetrate = -1;      // No se destruye por golpes (excepto gol)
            Projectile.timeLeft = DefaultTimeLeft; // Tiempo de vida
            Projectile.ignoreWater = false;  // ¿Se afecta por agua?
            Projectile.tileCollide = false;  // Empieza sin colisionar
            Projectile.alpha = 0;           // Visible
            State = 0;                      // Estado inicial
            // Propiedades adicionales para simular mejor una pelota
            Projectile.knockBack = 1f;      // No aplica knockback propio
            Projectile.damage = 1;          // No hace daño propio
                                            // --- NUEVO: Propiedad para ayudar a la detección ---
                                            //  Projectile.trap = true; // <<<--- Marcar como "trampa" puede ayudar a que otros proyectiles lo detecten
        }

        // --- Al Aparecer ---
        public override void OnSpawn(IEntitySource source)
        {
            Projectile.localAI[0] = 1f; // Flag inicializado
            // NO registrar en TymadorBombManager
            SoundEngine.PlaySound(SoundID.Item2 with { Volume = 0.7f, Pitch = 0.3f }, Projectile.position); // Sonido spawn pelota
            Projectile.tileCollide = false; // Asegura no colisionar al inicio
            State = 0; // Asegura estado inicial
        }

        // --- Inteligencia Artificial (COPIADA EXACTA DE TYMADORBOMB) ---
        public override void AI()
        {
            // Salir si no está inicializada
            if (Projectile.localAI[0] == 0f) return;

            // Luz (Opcional)
            // Lighting.AddLight(Projectile.Center, Color.White.ToVector3() * 0.3f);

            // --- LÓGICA DE MOVIMIENTO IDÉNTICA A TYMADORBOMB ---
            if (State == 3) // Asentado definitivamente
            {
                Projectile.velocity = Vector2.Zero;
                Projectile.rotation = 0f; // Detener rotación
                return;
            }

            // Comprobar si fue pateada (tenía velocidad aplicada externamente mientras estaba en State 0 o 3)
            bool justMoved = Projectile.velocity != Vector2.Zero; // Renombrado
            if ((State == 0 || State == 3) && justMoved)
            {
                State = 1; // Pasa a estado volando
                Projectile.tileCollide = true; // Activa colisión con tiles
                Projectile.netUpdate = true; // Sincroniza el cambio
            }

            // Física (Gravedad y Fricción) si está volando o rebotando
            if (State == 1 || State == 2)
            {
                Projectile.velocity.Y += BallGravity; // Usa constante de pelota
                if (Projectile.velocity.Y > BallMaxFallSpeed) Projectile.velocity.Y = BallMaxFallSpeed;
                Projectile.velocity.X *= DragFactor;
                if (Math.Abs(Projectile.velocity.X) < 0.1f && State != 2) Projectile.velocity.X = 0f;
                Projectile.rotation += Projectile.velocity.X * 0.05f; // Rotación leve
            }
            // Comprobar si debe empezar a caer si está en estado inicial y sin velocidad
            else if (State == 0 && Projectile.velocity == Vector2.Zero)
            {
                Point tileBelowCoords = (Projectile.Bottom + Vector2.UnitY).ToTileCoordinates();
                if (WorldGen.InWorld(tileBelowCoords.X, tileBelowCoords.Y, 5))
                {
                    Tile tileBelow = Framing.GetTileSafely(tileBelowCoords.X, tileBelowCoords.Y);
                    bool groundPresent = tileBelow.HasTile && (Main.tileSolid[tileBelow.TileType] || TileID.Sets.Platforms[tileBelow.TileType]);
                    if (!groundPresent)
                    {
                        State = 1;
                        Projectile.tileCollide = true;
                        Projectile.netUpdate = true;
                    }
                }
                else { State = 1; Projectile.tileCollide = true; Projectile.netUpdate = true; }
            }

            // --- Comprobación de Gol (Se mantiene aquí) ---
            CheckForGoal(); // Llama a la función de gol

        } // Fin AI

        // --- NUEVA FUNCIÓN: CheckForGoal ---
        private void CheckForGoal()
        {
            // Obtener la hitbox actual de la pelota
            Rectangle ballHitbox = Projectile.Hitbox;

            // Definir los tipos de tile de portería
            int redGoalType = ModContent.TileType<GoalTileRed>();
            int blueGoalType = ModContent.TileType<GoalTileBlue>();

            // Calcular el rango de tiles a comprobar alrededor de la pelota
            // Convertir la hitbox de la pelota a coordenadas de tile
            Point topLeft = ballHitbox.TopLeft().ToTileCoordinates();
            Point bottomRight = ballHitbox.BottomRight().ToTileCoordinates();

            // Asegurarse de que las coordenadas no se salgan del mundo
            topLeft.X = Math.Clamp(topLeft.X, 0, Main.maxTilesX - 1);
            topLeft.Y = Math.Clamp(topLeft.Y, 0, Main.maxTilesY - 1);
            bottomRight.X = Math.Clamp(bottomRight.X, 0, Main.maxTilesX - 1);
            bottomRight.Y = Math.Clamp(bottomRight.Y, 0, Main.maxTilesY - 1);

            // Iterar sobre los tiles que la hitbox podría estar tocando
            for (int x = topLeft.X; x <= bottomRight.X; x++)
            {
                for (int y = topLeft.Y; y <= bottomRight.Y; y++)
                {
                    Tile currentTile = Framing.GetTileSafely(x, y);

                    // Comprobar si existe el tile y es una portería
                    if (currentTile.HasTile && (currentTile.TileType == redGoalType || currentTile.TileType == blueGoalType))
                    {
                        // --- ¡COLISIÓN CON PORTERÍA DETECTADA! ---
                        FootballTeam scoredAgainst = (currentTile.TileType == redGoalType) ? FootballTeam.Red : FootballTeam.Blue;
                        FootballTeam teamThatScored = (scoredAgainst == FootballTeam.Red) ? FootballTeam.Blue : FootballTeam.Red;

                        // Marcar Gol
                        FootballSystem.AddScore(teamThatScored);

                        // Efectos Visuales/Sonoros
                        if (!Main.dedServ)
                        {
                            SoundEngine.PlaySound(SoundID.Duck, Projectile.Center);
                            for (int k = 0; k < 40; k++)
                            {
                                int dustType = Main.rand.Next(new int[] { DustID.Confetti_Blue, DustID.Confetti_Green, DustID.Confetti_Pink, DustID.Confetti_Yellow });
                                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, dustType, Main.rand.NextFloat(-5, 5), Main.rand.NextFloat(-7, -3));
                            }
                        }

                        // Destruir el Tile de Portería
                        //  WorldGen.KillTile(x, y, false, false, false); // fail=false para asegurar destrucción
                        //  if (Main.netMode == NetmodeID.MultiplayerClient) NetMessage.SendTileSquare(-1, x, y, 1);


                        // Destruir la Pelota
                        Projectile.Kill();
                        return; // Salir de la función porque ya marcamos gol
                    }
                }
            }
        } // Fin CheckForGoal

        // --- Colisión con Tiles (COPIADA EXACTA DE TYMADORBOMB) ---
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            // bool scoredGoal = false; // Ya no necesitamos comprobar gol aquí

            // Lógica de rebote si está en estado 1 o 2
            if (State == 1 || State == 2)
            {
                bool hitGround = oldVelocity.Y >= 0.1f && Projectile.velocity.Y > -0.1f && Projectile.velocity.Y < 0.1f;
                bool hitWall = Math.Abs(oldVelocity.X) >= 0.1f && Projectile.velocity.X > -0.1f && Projectile.velocity.X < 0.1f;
                bool hitCeiling = oldVelocity.Y < -0.1f && Projectile.velocity.Y > -0.1f && Projectile.velocity.Y < 0.1f;

                if (hitGround)
                {
                    if (State == 1)
                    { // Primer toque
                        State = 2;
                        Projectile.velocity.Y = -oldVelocity.Y * BounceFactor;
                        Projectile.velocity.X = oldVelocity.X * (BounceFactor * 0.8f);
                        if (Math.Abs(oldVelocity.Y) > 1f) SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.6f, Pitch = 0.2f }, Projectile.position);
                        Projectile.netUpdate = true;
                    }
                    else if (State == 2)
                    { // Segundo toque -> Asentarse
                        State = 3;
                        Projectile.velocity = Vector2.Zero;
                        Point tileCoords = (Projectile.Bottom + Vector2.UnitY).ToTileCoordinates();
                        Projectile.position.Y = tileCoords.Y * 16f - Projectile.height;
                        SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.4f, Pitch = -0.1f }, Projectile.position);
                        Projectile.netUpdate = true;
                    }
                }
                else if (hitWall)
                { // Rebote pared
                    Projectile.velocity.X = -oldVelocity.X * (BounceFactor * 0.7f);
                    if (Math.Abs(oldVelocity.X) > 1f) SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.4f }, Projectile.position);
                    Projectile.netUpdate = true;
                }
                else if (hitCeiling)
                { // Rebote techo
                    Projectile.velocity.Y = -oldVelocity.Y * (BounceFactor * 0.3f);
                    if (Math.Abs(oldVelocity.Y) > 1f) SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.4f }, Projectile.position);
                    Projectile.netUpdate = true;
                }
            }
            return false; // No destruir por colisión normal
        }

        // --- Kill (Efecto simple) ---
        public override void Kill(int timeLeft)
        {
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item2, Projectile.position);
                for (int i = 0; i < 10; i++)
                {
                    Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Cloud, 0f, 0f, 150, default, 0.8f);
                }
            }
            // No necesita remover del manager si no se registró
        }

        // --- Dibujado ---
        public override bool PreDraw(ref Color lightColor)
        {
            if (_ballTexture == null || !_ballTexture.IsLoaded) return false;
            Texture2D tex = _ballTexture.Value;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Rectangle sourceRectangle = tex.Frame(); // Asume frame único
            Vector2 origin = sourceRectangle.Size() / 2f;
            Color drawColor = Projectile.GetAlpha(lightColor);
            float rotation = Projectile.rotation; // Usa la rotación de AI
            float scale = 0.7f;
            SpriteEffects effects = SpriteEffects.None;

            Main.spriteBatch.Draw(tex, drawPos, sourceRectangle, drawColor, rotation, origin, scale, effects, 0f);
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // El daño de 1 ya se aplica automáticamente debido a Projectile.damage = 1
            // y Projectile.friendly = true.

            // 1. Efecto de Sonido al Golpear NPC (un sonido suave)
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.4f, Pitch = 0.5f }, Projectile.position); // Sonido tipo "thump" suave

            // 2. Efecto Visual (polvo/partículas)
            for (int i = 0; i < 5; i++)
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Cloud, hit.HitDirection * 0.5f, -1f, 100, default, 0.8f);
            }

            // 3. Ligero Rebote Adicional (Opcional)
            // Podemos hacer que rebote un poco más enérgicamente al golpear un NPC
            // que al golpear una pared.
            // --- REVISAR LÓGICA DE REBOTE ---
            if (State == 1 || State == 2)
            {
                // Aplicar un rebote más simple y directo al golpear NPC
                // Invierte la velocidad y reduce ligeramente para simular pérdida de energía
                float bounceMultiplier = 0.6f; // Cuánto rebota (0.6 = 60%)
                Projectile.velocity *= -bounceMultiplier;

                // Añadir un pequeño impulso vertical aleatorio para que no sea predecible
                Projectile.velocity.Y -= Main.rand.NextFloat(0.5f, 1.5f);

                // Asegurar una velocidad mínima para que no se "pegue"
                if (Projectile.velocity.LengthSquared() < 1f)
                {
                    Projectile.velocity = Main.rand.NextVector2Circular(1.5f, 1.5f);
                    Projectile.velocity.Y = Math.Abs(Projectile.velocity.Y) * -1; // Asegurar que vaya hacia arriba inicialmente
                }

                Projectile.netUpdate = true;
            }

            // --- GESTIÓN DE INMUNIDAD ---
            // Aplicar una inmunidad corta al NPC golpeado para evitar multi-hits instantáneos
            // si Projectile.usesLocalNPCImmunity no está activado o no funciona bien.
            target.AddBuff(BuffID.Wet, 15); // Ejemplo: Usar un buff vanilla corto como "cooldown" visual o real
                                            // O directamente:
                                            // target.immune[Projectile.owner] = 15; // 15 ticks de inmunidad a este proyectil/jugador
        }

        // ¡IMPORTANTE! No llames a Projectile.Kill() aquí.
        // Projectile.penetrate = -1 ya evita que se destruya.
    }
}
