// Content/Projectiles/TymadorBomb.cs
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria.Audio;
using WakfuMod.Content.Buffs;   // Para TymadorCableShockDebuff
using WakfuMod.ModSystems;      // Para TymadorBombManager y TymadorTickSystem
using ReLogic.Content;
using Terraria.DataStructures;          // Para Asset<T>

namespace WakfuMod.Content.Projectiles
{
    public class TymadorBomb : ModProjectile
    {
        // --- Propiedades y Constantes ---
        public int Tier => (int)Projectile.ai[0];
        public float State { get => Projectile.localAI[1]; set => Projectile.localAI[1] = value; } // 0=Idle, 1=Flying, 2=Bouncing, 3=Settled

        private const float BombGravity = 0.3f;      // Gravedad (ajusta si es necesario)
        private const float BombMaxFallSpeed = 10f;  // Velocidad caída máxima
        private const float BounceFactor = 0.5f;     // Elasticidad (0 a 1)
        private const float DragFactor = 0.98f;      // Fricción
        private const int DefaultTimeLeft = 9900;    // Tiempo de vida largo (tu valor)

        public float kickDamageBonusPercent = 0f; // Acumula el % (0.01 = 1%)// --- NUEVO: Campo para acumular bono de daño por patadas ---

        // --- Texturas ---
        private static Asset<Texture2D> _bombTextureTier1;
        private static Asset<Texture2D> _bombTextureTier2;
        private static Asset<Texture2D> _bombTextureTier3;

        // --- Carga/Descarga de Recursos ---
        public override void Load()
        {
            if (!Main.dedServ)
            {
                _bombTextureTier1 = ModContent.Request<Texture2D>("WakfuMod/Content/Projectiles/TymadorBomb_Tier1");
                _bombTextureTier2 = ModContent.Request<Texture2D>("WakfuMod/Content/Projectiles/TymadorBomb_Tier2");
                _bombTextureTier3 = ModContent.Request<Texture2D>("WakfuMod/Content/Projectiles/TymadorBomb_Tier3");
            }
        }
        public override void Unload()
        {
            _bombTextureTier1 = null;
            _bombTextureTier2 = null;
            _bombTextureTier3 = null;
        }

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Tymador Bomb");
        }

        // --- Configuración por Defecto ---
        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.aiStyle = -1;
            Projectile.friendly = true; // No daña al contacto
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Ranged; // Tipo daño explosión/cable
            Projectile.penetrate = -1;
            Projectile.timeLeft = DefaultTimeLeft;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false; // Empieza sin colisionar
            Projectile.alpha = 0;
            State = 0; // Estado inicial
        }

        // --- Al Aparecer ---
        public override void OnSpawn(IEntitySource source)
        {
            Projectile.localAI[0] = 1f; // Flag inicializado
            TymadorBombManager.RegisterBomb(Projectile); // Registra y actualiza tiers
            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.8f, Pitch = -1f }, Projectile.position); // Sonido pop
            for (int i = 0; i < 5; i++) { Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, 0f, -1f); }
            Projectile.tileCollide = false; // Asegura que no colisione al spawnear en aire
        }

        // --- Inteligencia Artificial ---
        public override void AI()
        {
            // Salir si no está inicializada
            if (Projectile.localAI[0] == 0f) return;

            // Luz
            Lighting.AddLight(Projectile.Center, Color.OrangeRed.ToVector3() * (0.4f + Tier * 0.1f));

            // Lógica del Cable
            CheckAndApplyCableDamage();

            // --- LÓGICA DE MOVIMIENTO ---
            if (State == 3) // Asentado definitivamente
            {
                Projectile.velocity = Vector2.Zero;
                return;
            }

            // Comprobar si fue pateada (tenía velocidad aplicada externamente mientras estaba en State 0 o 3)
            bool justKicked = (State == 0 || State == 3) && Projectile.velocity != Vector2.Zero;
            if (justKicked)
            {
                State = 1; // Pasa a estado volando
                Projectile.tileCollide = true; // Activa colisión con tiles
                Projectile.netUpdate = true; // Sincroniza el cambio
            }

            // Física (Gravedad y Fricción) si está volando o rebotando
            if (State == 1 || State == 2)
            {
                Projectile.velocity.Y += BombGravity;
                if (Projectile.velocity.Y > BombMaxFallSpeed) Projectile.velocity.Y = BombMaxFallSpeed;
                Projectile.velocity.X *= DragFactor;
                if (Math.Abs(Projectile.velocity.X) < 0.1f) Projectile.velocity.X = 0f;
                Projectile.rotation += Projectile.velocity.X * 0.05f; // Rotación leve
            }
            // Comprobar si debe empezar a caer si está en estado inicial y sin velocidad
            else if (State == 0 && Projectile.velocity == Vector2.Zero)
            {
                Point tileBelowCoords = (Projectile.Bottom + Vector2.UnitY).ToTileCoordinates();
                if (WorldGen.InWorld(tileBelowCoords.X, tileBelowCoords.Y, 5))
                {
                    Tile tileBelow = Framing.GetTileSafely(tileBelowCoords.X, tileBelowCoords.Y);
                    if (!tileBelow.HasTile || (!Main.tileSolid[tileBelow.TileType] && !TileID.Sets.Platforms[tileBelow.TileType]))
                    {
                        State = 1;
                        Projectile.tileCollide = true;
                        Projectile.netUpdate = true;
                    }
                }
            }
        } // Fin AI

        // --- Colisión con Tiles (Rebote y Asentamiento) ---
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            // Solo reacciona si estaba volando (1) o rebotando (2)
            if (State == 1 || State == 2)
            {
                bool hitGround = oldVelocity.Y >= 0.1f && Projectile.velocity.Y > -0.1f && Projectile.velocity.Y < 0.1f; // Comprobación más robusta de tocar suelo
                bool hitWall = oldVelocity.X != 0 && Projectile.velocity.X == 0;
                bool hitCeiling = oldVelocity.Y < -0.1f && Projectile.velocity.Y == 0;

                if (hitGround)
                {
                    if (State == 1) // Primer toque
                    {
                        State = 2;
                        Projectile.velocity.Y = -oldVelocity.Y * BounceFactor;
                        Projectile.velocity.X = oldVelocity.X * (BounceFactor * 0.8f);
                        SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.7f, Pitch = 0.1f }, Projectile.position);
                        Projectile.netUpdate = true;
                    }
                    else if (State == 2) // Segundo toque
                    {
                        State = 3;
                        Projectile.velocity = Vector2.Zero;
                        Point tileCoords = (Projectile.Bottom + Vector2.UnitY).ToTileCoordinates();
                        Projectile.position.Y = tileCoords.Y * 16f - Projectile.height;
                        SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.5f, Pitch = -0.2f }, Projectile.position);
                        Projectile.netUpdate = true;
                    }

                }
                // Simplificar rebotes laterales/techo para evitar sonidos extraños
                else if (hitWall)
                {
                    Projectile.velocity.X = -oldVelocity.X * (BounceFactor * 0.5f);
                    if (Math.Abs(oldVelocity.X) > 1f) // Solo sonar si el golpe fue significativo
                        SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.4f }, Projectile.position);
                    Projectile.netUpdate = true;
                }
                else if (hitCeiling)
                {
                    Projectile.velocity.Y = -oldVelocity.Y * (BounceFactor * 0.3f);
                    if (Math.Abs(oldVelocity.Y) > 1f) // Solo sonar si el golpe fue significativo
                        SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.4f }, Projectile.position);
                    Projectile.netUpdate = true;
                }
            }
            // Si estaba en estado 0 o 3 y colisiona (no debería pasar si la lógica está bien), no hace nada especial.

            return false; // No destruir la bomba
        }
        // --- Método para la Lógica del Cable ---
        private void CheckAndApplyCableDamage()
        {
            // Solo el dueño de la bomba debe ejecutar la lógica de daño
            // (Esto es importante para multijugador, aunque StrikeNPC maneja sincronización)
            if (Projectile.owner != Main.myPlayer && Main.netMode != NetmodeID.Server) // Corrección: Solo el owner o el servidor
            {
                //return; // Descomentar si hay problemas de duplicación de daño en MP, aunque StrikeNPC debería manejarlo.
                // Por ahora, dejaremos que se ejecute en todos lados para simplicidad visual/local,
                // pero StrikeNPC solo tendrá efecto real si lo llama el owner/servidor.
            }


            // Encontrar todas las bombas activas del mismo jugador
            List<Projectile> ownerBombs = new List<Projectile>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile otherProj = Main.projectile[i];
                // Comprueba que sea activa, del mismo tipo, del mismo dueño, y que tenga un ID de secuencia válido (ai[1] > 0 si 0 es valor por defecto)
                if (otherProj.active && otherProj.type == Type && otherProj.owner == Projectile.owner && otherProj.ai[1] > 0)
                {
                    ownerBombs.Add(otherProj);
                }
            }

            // Necesitamos al menos 2 bombas para formar un cable
            if (ownerBombs.Count < 2) return;

            // Ordenar las bombas por su identificador de secuencia (ai[1])
            // ¡¡ASEGÚRATE de que TymadorBombManager o tu código de creación asignan un valor creciente y único a Projectile.ai[1]!!
            var sortedBombs = ownerBombs.OrderBy(p => p.ai[1]).ToList();

            // Encontrar la posición de ESTA bomba en la secuencia ordenada
            int myIndex = sortedBombs.FindIndex(p => p.whoAmI == Projectile.whoAmI);

            // Si esta bomba no se encontró o no tiene un ai[1] válido, salir.
            if (myIndex == -1) return;

            // Comprobar conexión con la bomba ANTERIOR en la secuencia
            // Solo si esta bomba es la segunda (índice 1) o la tercera (índice 2)
            // Y asegurarnos de que el índice anterior es válido
            if (myIndex > 0 && myIndex < sortedBombs.Count && myIndex < 3) // Limita a cables entre 1-2 y 2-3
            {
                Projectile prevBomb = sortedBombs[myIndex - 1];
                ApplyDamageBetween(prevBomb, Projectile);
            }

            // Nota: No comprobamos hacia adelante. La siguiente bomba en la secuencia
            // se encargará de comprobar la conexión con esta (su 'anterior').
        }

        // --- Método para Aplicar Daño del Cable ---
        private void ApplyDamageBetween(Projectile bombA, Projectile bombB)
        {
            Vector2 start = bombA.Center;
            Vector2 end = bombB.Center;
            // --- Define aquí el daño y knockback del CABLE ---
            // Podría escalar con el Tier de las bombas conectadas, o con stats del jugador
            int cableBaseDamage = 15 + (Tier * 2); // Ejemplo: Daño base + extra por Tier de esta bomba
            float cableKnockback = 2f;
            float collisionWidth = 12f; // Ancho de la "hitbox" del cable
            int cableHitCooldown = 60; // <--- ¡NUEVO! Cooldown en ticks (60 = 1 segundo)
            Player owner = Main.player[Projectile.owner];

            // Iterar sobre los NPCs
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];

                // Comprobar si el NPC es un objetivo válido
                // CanBeChasedBy es un buen punto de partida (activo, no amigo, no inmune a todo, visible, etc.)
                // Y comprobar inmunidad específica aplicada por este cable/jugador
                if (npc.CanBeChasedBy(this, false) && !npc.HasBuff(ModContent.BuffType<Buffs.TymadorCableShockDebuff>()))
                {
                    // Usar Collision.CheckAABBvLineCollision para una detección eficiente línea-rectángulo
                    float collisionPoint = 0f; // No necesitamos el punto exacto, solo si hay colisión
                    if (Collision.CheckAABBvLineCollision(npc.position, npc.Size, start, end, collisionWidth, ref collisionPoint))
                    {
                        // Calcular daño final aplicando bonificaciones del jugador
                        // Usamos Ranged para que coincida con la explosión, pero puedes cambiarlo
                        int finalDamage = (int)owner.GetDamage(DamageClass.Ranged).ApplyTo(cableBaseDamage);

                        // Aplicar daño
                        NPC.HitInfo hitInfo = new NPC.HitInfo
                        {
                            Damage = finalDamage,
                            Knockback = cableKnockback,
                            // Dirección del golpe basada en la posición relativa al centro del cable (aproximado)
                            HitDirection = (npc.Center.X < (start.X + end.X) / 2f) ? -1 : 1,
                            DamageType = DamageClass.Ranged // O la que corresponda
                        };

                        // Solo el servidor o el dueño deben llamar a StrikeNPC en MP
                        if (Main.netMode != NetmodeID.MultiplayerClient || Projectile.owner == Main.myPlayer)
                        {
                            npc.StrikeNPC(hitInfo);
                        }



                        npc.AddBuff(ModContent.BuffType<Buffs.TymadorCableShockDebuff>(), cableHitCooldown);


                    }
                }
            }
        }


        // --- NUEVA FUNCIÓN para manejar la cadena ---
        private void TriggerChainReaction()
        {
            // 1. Evitar si ya detonamos esta bomba por cadena este tick
            if (TymadorTickSystem.DetonatedThisTick.Contains(Projectile.whoAmI))
            {
                return;
            }

            // 2. Marcar esta bomba como detonada este tick
            TymadorTickSystem.DetonatedThisTick.Add(Projectile.whoAmI);

            // 3. Encontrar bombas conectadas (similar a CheckAndApplyCableDamage)
            List<Projectile> ownerBombs = new List<Projectile>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile otherProj = Main.projectile[i];
                // Importante: Buscar bombas activas DEL MISMO DUEÑO y tipo
                if (otherProj.active && otherProj.type == Type && otherProj.owner == Projectile.owner && otherProj.whoAmI != Projectile.whoAmI && otherProj.ai[1] > 0)
                {
                    ownerBombs.Add(otherProj);
                }
            }

            if (ownerBombs.Count == 0) return; // No hay otras bombas

            // Añadir esta bomba a la lista temporalmente para facilitar la ordenación y búsqueda de vecinos
            ownerBombs.Add(Projectile);
            var sortedBombs = ownerBombs.OrderBy(p => p.ai[1]).ToList();
            int myIndex = sortedBombs.FindIndex(p => p.whoAmI == Projectile.whoAmI);

            if (myIndex == -1) return; // No debería pasar si nos añadimos nosotros mismos

            // 4. Detonar bomba ANTERIOR si existe y está dentro del límite de 3 bombas
            if (myIndex > 0 && myIndex < 3) // Soy la 2ª o 3ª bomba
            {
                Projectile prevBomb = sortedBombs[myIndex - 1];
                // Comprobar si la bomba anterior está activa y NO ha sido detonada ya este tick
                if (prevBomb != null && prevBomb.active && !TymadorTickSystem.DetonatedThisTick.Contains(prevBomb.whoAmI))
                {
                    prevBomb.Kill(); // ¡Detonar la bomba anterior!
                }
            }

            // 5. Detonar bomba SIGUIENTE si existe y está dentro del límite de 3 bombas
            if (myIndex < sortedBombs.Count - 1 && myIndex < 2) // Soy la 1ª o 2ª bomba
            {
                Projectile nextBomb = sortedBombs[myIndex + 1];
                // Comprobar si la bomba siguiente está activa y NO ha sido detonada ya este tick
                if (nextBomb != null && nextBomb.active && !TymadorTickSystem.DetonatedThisTick.Contains(nextBomb.whoAmI))
                {
                    nextBomb.Kill(); // ¡Detonar la bomba siguiente!
                }
            }
        }

        // --- Lógica de Explosión al Morir ---
        public override void Kill(int timeLeft)
        {
            // Llamar a la lógica de cadena primero
            TriggerChainReaction();

            // Remover del Manager después
            TymadorBombManager.RemoveBomb(Projectile);

            // --- Cálculos de Explosión ---
            float baseRadius = 60f;
            float radius = Tier switch { 1 => baseRadius * 3f, 2 => baseRadius * 6f, _ => baseRadius };
            int baseDamage = Tier switch
            {
                1 => 20 + 10, // Tier 1 = 30 base
                2 => 20 + 20, // Tier 2 = 40 base
                _ => 20 // Tier 0 = 20 base
            };    
            float bonusPercent = Tier == 0 ? 0.02f : Tier == 1 ? 0.04f: 0.06f;
            Player owner = Main.player[Projectile.owner];

            // --- Sonido y Efectos Visuales (como antes) ---
            SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
            
            // #############################################################
            // ### BLOQUE DE GENERACIÓN DE PARTÍCULAS ###
            // #############################################################
            if (!Main.dedServ) // Solo generar efectos visuales en el cliente
            {
                // Ajustar cantidad, velocidad y escala de partículas según el radio
                int dustCount = (int)(40 + radius / 3f);
                float dustScaleMin = 1.5f;
                float dustScaleMax = 2.5f + radius / 100f;
                float dustVelocity = 4f + radius / 60f;
                int goreCount = 2 + (int)(radius / 120f);

                for (int i = 0; i < dustCount; i++)
                {
                    int dustType = Tier switch { 0 => DustID.Torch, 1 => DustID.OrangeTorch, 2 => DustID.RedTorch, _ => DustID.Torch };
                    Vector2 velocity = Main.rand.NextVector2Circular(dustVelocity / 2f, dustVelocity);
                    float scale = Main.rand.NextFloat(dustScaleMin, dustScaleMax);

                    // Vamos a intentar generar el dust DENTRO del radio directamente.
                    Vector2 spawnPos = Projectile.Center + Main.rand.NextVector2Circular(radius * 0.9f, radius * 0.9f); // Spawn cerca del borde del radio
                    Dust dust = Dust.NewDustDirect(spawnPos, 0, 0, dustType, velocity.X, velocity.Y, 100, default, scale);

                    // Ya no necesitamos esto: dust.position = Projectile.Center + Main.rand.NextVector2Circular(radius * 0.8f, radius * 0.8f);
                    dust.velocity *= Main.rand.NextFloat(0.8f, 1.5f);
                    dust.noGravity = true;
                    if (Main.rand.NextBool(4)) dust.fadeIn = 0.5f;
                }

                // Generar gore (humo, etc.)
                for (int g = 0; g < goreCount; g++)
                {
                    Vector2 goreVel = Main.rand.NextVector2Circular(dustVelocity / 3f, dustVelocity / 2f);
                    // Spawn gore más cerca del centro
                    Vector2 goreSpawnPos = Projectile.Center + Main.rand.NextVector2Circular(radius * 0.3f, radius * 0.3f);
                    Gore.NewGore(Projectile.GetSource_Death(), goreSpawnPos, goreVel, GoreID.Smoke1, Main.rand.NextFloat(0.8f, 1.2f));
                }

                // Luz
                if (Tier > 0)
                {
                    Lighting.AddLight(Projectile.Center, Color.Orange.ToVector3() * (1f + Tier));
                }
            }
            // #############################################################
            // ### FIN DEL BLOQUE DE PARTÍCULAS ###
            //

            // --- LÓGICA DE DAÑO DE EXPLOSIÓN (REVISADA) ---

            // Usaremos un HashSet para evitar golpear al mismo NPC dos veces (una por radio, otra por cable) en esta explosión específica.
            HashSet<int> hitNpcsThisExplosion = new HashSet<int>();

            // --- PASO 3.1: Daño a NPCs en los cables conectados ---
            if (Main.netMode != NetmodeID.MultiplayerClient || Projectile.owner == Main.myPlayer)
            {
                // Re-encontrar bombas conectadas (similar a TriggerChainReaction/CheckAndApplyCableDamage)
                List<Projectile> ownerBombs = new List<Projectile>();
                // Buscar bombas ACTIVAS del mismo owner y tipo (EXCLUYENDO esta misma bomba)
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile otherProj = Main.projectile[i];
                    if (otherProj.active && otherProj.type == Type && otherProj.owner == Projectile.owner && otherProj.whoAmI != Projectile.whoAmI && otherProj.ai[1] > 0)
                    {
                        ownerBombs.Add(otherProj);
                    }
                }

                if (ownerBombs.Count > 0)
                {
                    // Añadir esta bomba temporalmente para ordenar y encontrar vecinos
                    ownerBombs.Add(Projectile);
                    var sortedBombs = ownerBombs.OrderBy(p => p.ai[1]).ToList();
                    int myIndex = sortedBombs.FindIndex(p => p.whoAmI == Projectile.whoAmI);

                    if (myIndex != -1)
                    {
                        // Lista de cables a comprobar
                        List<Tuple<Vector2, Vector2>> cablesToCheck = new List<Tuple<Vector2, Vector2>>();

                        // Cable con bomba ANTERIOR
                        if (myIndex > 0 && myIndex < 3) // Si soy la 2ª o 3ª
                        {
                            Projectile prevBomb = sortedBombs[myIndex - 1];
                            if (prevBomb != null && prevBomb.active) // Asegurarse que la anterior sigue activa al momento de explotar
                            {
                                cablesToCheck.Add(Tuple.Create(prevBomb.Center, Projectile.Center));
                            }
                        }
                        // Cable con bomba SIGUIENTE
                        if (myIndex < sortedBombs.Count - 1 && myIndex < 2) // Si soy la 1ª o 2ª
                        {
                            Projectile nextBomb = sortedBombs[myIndex + 1];
                            if (nextBomb != null && nextBomb.active) // Asegurarse que la siguiente sigue activa al momento de explotar
                            {
                                cablesToCheck.Add(Tuple.Create(Projectile.Center, nextBomb.Center));
                            }
                        }

                        // Ahora, iterar por los cables relevantes y los NPCs
                        foreach (var cableEnds in cablesToCheck)
                        {
                            Vector2 start = cableEnds.Item1;
                            Vector2 end = cableEnds.Item2;
                            float collisionWidth = 12f; // Ancho de colisión del cable

                            for (int i = 0; i < Main.maxNPCs; i++)
                            {
                                NPC npc = Main.npc[i];
                                // Comprobar si es objetivo válido Y NO HA SIDO GOLPEADO YA POR ESTA EXPLOSIÓN
                                if (npc.CanBeChasedBy(this, false) && !hitNpcsThisExplosion.Contains(i))
                                {
                                    float collisionPoint = 0f;
                                    if (Collision.CheckAABBvLineCollision(npc.position, npc.Size, start, end, collisionWidth, ref collisionPoint))
                                    {
                                        // ¡NPC está tocando un cable conectado a la bomba que explota! Aplicar daño de EXPLOSIÓN.
                                        int dmg = baseDamage
                                        + (int)(npc.lifeMax * bonusPercent) // Bono por Tier
                                        + (int)(npc.lifeMax * kickDamageBonusPercent); // Bono por Patadas
                                        dmg = (int)owner.GetDamage(DamageClass.Generic).ApplyTo(dmg);

                                        NPC.HitInfo hitInfo = new NPC.HitInfo
                                        {
                                            Damage = dmg,
                                            Knockback = 4f + Tier * 1.5f,
                                            HitDirection = Math.Sign(npc.Center.X - Projectile.Center.X),
                                            DamageType = DamageClass.Generic
                                            // ¡NO comprobamos ni aplicamos el debuff del cable aquí!
                                        };
                                        npc.StrikeNPC(hitInfo);
                                        hitNpcsThisExplosion.Add(i); // Marcar como golpeado por esta explosión

                                        // Opcional: Podrías aplicar una inmunidad MUY corta (e.g., 2-5 ticks) aquí
                                        // para evitar que múltiples explosiones EN CADENA en el MISMO TICK golpeen varias veces.
                                        // npc.immune[Projectile.owner] = 5;
                                    }
                                }
                            }
                        }
                    }
                }
            }


            // --- PASO 3.2: Daño de Área Estándar (Radio) ---
            if (Main.netMode != NetmodeID.MultiplayerClient || Projectile.owner == Main.myPlayer)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];
                    // Comprobar si es objetivo válido, dentro del radio Y NO HA SIDO GOLPEADO YA (por el cable)
                    if (npc.active && !npc.friendly && npc.CanBeChasedBy(this, false) &&
                        !hitNpcsThisExplosion.Contains(i) && // <<<--- AÑADIDO: No golpear si ya lo hizo el cable
                        Vector2.DistanceSquared(npc.Center, Projectile.Center) <= radius * radius)
                    {
                        int dmg = baseDamage
                      + (int)(npc.lifeMax * bonusPercent) // Bono por Tier
                      + (int)(npc.lifeMax * kickDamageBonusPercent); // Bono por Patadas
                        dmg = (int)owner.GetDamage(DamageClass.Generic).ApplyTo(dmg);

                        NPC.HitInfo hitInfo = new NPC.HitInfo
                        {
                            Damage = dmg,
                            Knockback = 4f + Tier * 1.5f,
                            HitDirection = Math.Sign(npc.Center.X - Projectile.Center.X),
                            DamageType = DamageClass.Generic
                        };
                        npc.StrikeNPC(hitInfo);
                        // No necesitamos añadir a hitNpcsThisExplosion aquí porque ya es el final de la lógica para este NPC.
                    }
                }
            }
        }


        // --- Dibujado Personalizado ---
        public override bool PreDraw(ref Color lightColor)
        {
            // Selecciona la textura correcta basada en el Tier
            string texturePath = Tier switch
            {
                0 => "WakfuMod/Content/Projectiles/TymadorBomb_Tier1",
                1 => "WakfuMod/Content/Projectiles/TymadorBomb_Tier2",
                2 => "WakfuMod/Content/Projectiles/TymadorBomb_Tier3",
                _ => "WakfuMod/Content/Projectiles/TymadorBomb_Tier1" // Fallback
            };

            Texture2D tex = ModContent.Request<Texture2D>(texturePath).Value;
            Vector2 drawPos = Projectile.Center - Main.screenPosition; // Posición en pantalla
            Vector2 origin = tex.Size() / 2f; // Origen en el centro de la textura

            // --- NUEVO: Calcular Color de Tinte ---
            float maxBonus = 0.30f; // El mismo límite que usamos antes
                                    // Calcula la intensidad del rojo (0 a 1) basado en qué tan cerca está el bono del máximo
                                    // Asegúrate de que kickDamageBonusPercent no sea negativo por algún error
            float bonusRatio = Math.Clamp(kickDamageBonusPercent / maxBonus, 0f, 3f);

            // Color base (el color de la luz ambiental)
            Color baseDrawColor = Projectile.GetAlpha(lightColor);

            // Interpolar hacia un rojo brillante basado en el ratio del bono
            // Color.Lerp(colorA, colorB, amount)
            // Queremos ir desde el color de la luz ambiental hacia rojo puro.
            // Usaremos un rojo brillante como objetivo (255, 50, 50) para que no sea completamente oscuro.
            Color targetRed = new Color(255, 60, 60); // Rojo brillante chillón

            // Aplica el Lerp. El 'amount' es bonusRatio.
            // ¡Importante! Lerp mezcla los colores. Si queremos *añadir* rojo, es diferente.
            // Vamos a *mezclar* el color de la luz con rojo.

            // Color final mezclado
            Color finalDrawColor = Color.Lerp(baseDrawColor, targetRed, bonusRatio * 0.95f); // Multiplicamos por 0.85f para que no llegue a ser 100% rojo puro, 
                                                                                             // manteniendo algo de la textura original visible. Ajusta este 0.85f si quieres más o menos intensidad máxima.

            // Asegúrate de que el Alpha no se pierda
            finalDrawColor.A = baseDrawColor.A;


            // --- FIN Calcular Color de Tinte ---


            // Dibujar la bomba (Usa finalDrawColor)
            Main.spriteBatch.Draw(
                tex,
                drawPos,
                null,
                finalDrawColor, // <--- USA EL COLOR CALCULADO
                Projectile.rotation,
                origin,
                Projectile.scale,
                SpriteEffects.None,
                0f
            );

            return false; // Indica que ya hemos dibujado el proyectil, Terraria no debe dibujarlo por defecto
        }

        // --- Comportamiento de Colisión (Opcional) ---
        // Aunque tileCollide es false, podrías querer que reaccione a otros proyectiles o jugadores.
        // public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) { ... }

        // --- Importante para el cable: Asegurar que no colisiona con el suelo ---
        // Aunque tileCollide = false, si tuvieras alguna lógica de gravedad manual,
        // necesitarías asegurarte de que se detiene en el suelo más cercano.
        // Tu código actual no tiene gravedad, así que está bien.
    }
}