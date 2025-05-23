// Content/Projectiles/TymadorKickProjectile.cs
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using WakfuMod.jugador; // Para WakfuPlayer
using System; // Para Math.Sign
using Terraria.Audio; // Para SoundEngine
using ReLogic.Content; // Para Asset<T>
using Terraria.DataStructures; // Para IEntitySource
using System.Collections.Generic; // Para HashSet si lo necesitaras (ahora no)

namespace WakfuMod.Content.Projectiles // Reemplaza WakfuMod si es necesario
{
    public class RedKickProjectile : ModProjectile
    {
        // ai[1] indica si es patada fuerte (puede usarse para efectos visuales/sonido)
        private bool IsStrongKick => Projectile.ai[1] == 1f;

        // --- Configuración de Animación y Hitbox ---
        private const int AnimationFrames = 4;      // Número de frames en tu sprite
        private const int FrameDuration = 5;        // Ticks por frame
        private const int TotalLifetime = AnimationFrames * FrameDuration; // Duración total
        private const int HitboxWidth = 80;         // Ancho de la hitbox
        private const int HitboxHeight = 60;        // Alto de la hitbox
        private const float HitboxOffsetX = 45f;    // Offset horizontal de la hitbox desde el jugador
        private const float HitboxOffsetY = -10f;   // Offset vertical de la hitbox desde el jugador
        private const float VisualVerticalOffset = 30f; // Offset vertical para dibujar el sprite
                                                        // --- Cooldown para patear la MISMA bomba ---
        private HashSet<int> _hitBombsThisKick; // Evita múltiples empujes de la MISMA patada
        // Podríamos necesitar un cooldown global si una patada rápida puede volver a golpear
        // la misma bomba que acaba de patear, pero empecemos con esto.
         // --- HashSet para evitar multi-hit a CUALQUIER proyectil ---
        private HashSet<int> _hitProjectilesThisKick;

        // --- Cache de Textura ---
        private static Asset<Texture2D> _kickTextureAsset;

        public override void Load()
        {
            if (!Main.dedServ) // Cargar solo en cliente
            {
                _kickTextureAsset = ModContent.Request<Texture2D>("WakfuMod/Content/Projectiles/RedKickProjectile"); // Ruta a tu sprite
            }
        }
        public override void Unload()
        {
            _kickTextureAsset = null; // Descargar textura
        }

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = AnimationFrames; // Informar a Terraria sobre los frames
        }

        public override void SetDefaults()
        {
            // --- Hitbox ---
            Projectile.width = HitboxWidth;
            Projectile.height = HitboxHeight;

            // --- Propiedades de Combate ---
            Projectile.friendly = true;         // Puede golpear NPCs y (con CanBeHitBy) bombas
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Melee; // Tipo de daño
            Projectile.penetrate = -1;          // Golpea a todos los objetivos en el área
            Projectile.usesLocalNPCImmunity = true; // Evita multi-hit en el mismo NPC
            Projectile.localNPCHitCooldown = 10; // Cooldown corto entre golpes al MISMO NPC (ajusta)

            // --- Comportamiento General ---
            Projectile.knockBack = 6f;
            Projectile.timeLeft = TotalLifetime;  // Duración basada en la animación
            Projectile.tileCollide = false;       // No choca con bloques
            Projectile.ignoreWater = true;
            Projectile.aiStyle = -1;             // AI personalizada
            Projectile.ownerHitCheck = true;      // Requiere que el jugador esté cerca para golpear

            // --- Visual ---
            Projectile.alpha = 0;               // Visible
            Projectile.hide = false;             // Oculta la hitbox rectangular por defecto (dibujamos nosotros)
            Projectile.scale = 1f;              // Escala del sprite
        }

        // --- Flag Interno para Controlar Visibilidad del Jugador ---
        private bool _playerHidden = false;

        // --- Al Aparecer ---
        public override void OnSpawn(IEntitySource source)
        { TryHidePlayer();
            _hitProjectilesThisKick = new HashSet<int>();
            _hitBombsThisKick = new HashSet<int>(); // Inicializa el set
                                                    // Intenta ocultar al jugador inmediatamente
           

            // Efecto de Polvo Oscuro al aparecer
            if (Main.netMode != NetmodeID.Server)
            {
                Player owner = Main.player[Projectile.owner];
                // Sonido adaptado a la fuerza de la patada?
                SoundEngine.PlaySound(SoundID.Item1 with { Volume = IsStrongKick ? 0.6f : 0.4f, Pitch = IsStrongKick ? 0.6f : 0.8f }, Projectile.Center);
                for (int i = 0; i < (IsStrongKick ? 25 : 15); i++) // Más polvo si es fuerte
                {
                    int dustType = DustID.Smoke;
                    Vector2 dustVel = Main.rand.NextVector2Circular(IsStrongKick ? 2.5f : 1.8f, IsStrongKick ? 2.5f : 1.8f);
                    Dust d = Dust.NewDustPerfect(owner.Center + Main.rand.NextVector2Circular(25f, 15f), dustType, dustVel, 150, Color.DarkSlateGray, IsStrongKick ? 1.4f : 1.2f);
                    d.noGravity = true;
                    d.velocity *= 0.5f;
                    d.fadeIn = 0.3f;
                }
            }
        }

        // --- Inteligencia Artificial ---
        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];

            // Asegurarse de que el jugador esté oculto si este proyectil está activo
            TryHidePlayer(); // Llama de nuevo por si acaso hubo un problema

            // Mantener Posición relativa al jugador y Dirección del sprite
            Projectile.direction = owner.direction;
            Projectile.spriteDirection = owner.direction;
            Projectile.Center = owner.MountedCenter + new Vector2(owner.direction * HitboxOffsetX, HitboxOffsetY);

            // Animación del Sprite
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= FrameDuration)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % AnimationFrames; // Ciclar frames
            }

            // --- REINTRODUCIR: Comprobar y Empujar Bombas ---
            CheckAndPushBombs();
             CheckAndPushFriendlyProjectiles();
              CheckAndReflectHostileProjectiles(); // Para proyectiles enemigos
        }

        // --- MÉTODO REVISADO: CheckAndPushBombs ---
        private void CheckAndPushBombs()
        {
            Player owner = Main.player[Projectile.owner];
            Rectangle kickHitbox = Projectile.Hitbox;
            float itemKnockback = owner.HeldItem.knockBack; // Knockback débil o fuerte
            bool strongKick = Projectile.ai[1] == 1f; // Comprueba si fue clic derecho

            // --- Ángulo de Lanzamiento Dinámico ---
            // Define ángulos diferentes para cada tipo de patada
            float weakKickAngleDegrees = -65f; // Ángulo para patada débil (Clic Izquierdo)
            float strongKickAngleDegrees = -30f; // Ángulo MÁS vertical para patada fuerte (Clic Derecho)

            // Selecciona el ángulo correcto basado en si fue patada fuerte
            float launchAngleDegrees = strongKick ? strongKickAngleDegrees : weakKickAngleDegrees;
            // Convierte el ángulo seleccionado a radianes
            float launchAngleRadians = MathHelper.ToRadians(launchAngleDegrees);
            // --- Fin Ángulo Dinámico ---

            float kbToVelocityFactor = 1.6f; // Ajusta este factor si es necesario

            foreach (var bomb in TymadorBombManager.ActiveBombs)
            {
                if (bomb != null && bomb.active && bomb.owner == owner.whoAmI && !_hitBombsThisKick.Contains(bomb.whoAmI))
                {
                    if (kickHitbox.Intersects(bomb.Hitbox))
                    {
                        // --- Aplicar Knockback Manualmente ---

                        // 1. Calcula la dirección del knockback usando el ángulo correcto
                        Vector2 kbDirection = new Vector2(
                            owner.direction * (float)Math.Cos(launchAngleRadians), // Usa el ángulo en radianes
                            (float)Math.Sin(launchAngleRadians)                   // Usa el ángulo en radianes
                        );
                        // No es estrictamente necesario normalizar aquí si Cos/Sin vienen de un ángulo, pero no hace daño
                        kbDirection.Normalize();

                        // 2. Calcula la magnitud de la velocidad
                        Vector2 appliedVelocity = kbDirection * itemKnockback * kbToVelocityFactor;

                        // 3. Aplica la velocidad a la bomba
                        bomb.velocity = appliedVelocity;

                        // --- Forzar Estado y Sincronizar en la Bomba ---
                        if (bomb.ModProjectile is TymadorBomb tymadorBombInstance)
                        {
                            if (tymadorBombInstance.State == 0 || tymadorBombInstance.State == 3)
                            {
                                tymadorBombInstance.State = 1;
                                tymadorBombInstance.Projectile.tileCollide = true;
                            }
                        }
                        bomb.netUpdate = true;

                        // --- Marcar como golpeada y efectos ---
                        _hitBombsThisKick.Add(bomb.whoAmI);
                        SoundEngine.PlaySound(SoundID.Dig, bomb.position);
                        Dust.NewDust(bomb.position, bomb.width, bomb.height, DustID.Smoke, 0, 0, 0, default, 1f);
                    }
                }
            }
        }

       // --- MÉTODO GENERALIZADO CON VELOCIDAD FIJA ---
        private void CheckAndPushFriendlyProjectiles()
        {
            Player owner = Main.player[Projectile.owner];
            Rectangle kickHitbox = Projectile.Hitbox;
            bool strongKick = Projectile.ai[1] == 1f; // Comprueba si fue clic derecho

            // --- VELOCIDADES FIJAS (Basadas en tus logs funcionales) ---
            // Vector para Patada Débil (Clic Izquierdo)
             // Tomamos los valores absolutos de tu log y aplicamos owner.direction para X
             // Asumiendo que el log era cuando owner.direction = -1 (mirando izquierda)
             // Si era mirando derecha, quita el Math.Abs de la X
            Vector2 weakKickVelocity = new Vector2(owner.direction * 4.5642767f, -9.788125f);

            // Vector para Patada Fuerte (Clic Derecho)
             // Asumiendo que el log era cuando owner.direction = -1
            Vector2 strongKickVelocity = new Vector2(owner.direction * 15.2642767f, -15.2642767f);
            // --- FIN VELOCIDADES FIJAS ---


            // Iterar sobre TODOS los proyectiles
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile targetProj = Main.projectile[i];

                // Condiciones para ser Pateado (activo, friendly, no patada, no golpeado ya)
                if (targetProj.active &&
                    targetProj.friendly &&
                    targetProj.whoAmI != Projectile.whoAmI &&
                    !_hitProjectilesThisKick.Contains(targetProj.whoAmI))
                {
                    // Comprobar colisión
                    if (kickHitbox.Intersects(targetProj.Hitbox))
                    {
                        // --- Aplicar Velocidad FIJA Directamente ---
                        // Elige la velocidad correcta según el tipo de patada
                        Vector2 appliedVelocity = strongKick ? strongKickVelocity : weakKickVelocity;

                        // --- DEBUG (Opcional) ---
                        // Main.NewText($"Applying FIXED Kick Vel to Proj {targetProj.whoAmI}: {appliedVelocity}", Color.Lime);

                        // Aplica la velocidad directamente a la pelota/proyectil
                        targetProj.velocity = appliedVelocity;

                        // --- Forzar Estado y Sincronizar ---
                        // (Igual que antes)
                         if (targetProj.ModProjectile is Jalabola ballInstance) {
                             if (ballInstance.State == 0 || ballInstance.State == 3) {
                                 ballInstance.State = 1;
                                 ballInstance.Projectile.tileCollide = true;
                             }
                             targetProj.netUpdate = true;
                         }
                         // else if (targetProj.ModProjectile is TymadorBomb bombInstance) { ... } // Si también afecta bombas

                        // --- Marcar como golpeado y efectos ---
                        _hitProjectilesThisKick.Add(targetProj.whoAmI);
                        SoundEngine.PlaySound(SoundID.Dig with { Pitch = strongKick ? 0.3f : 0.6f }, targetProj.position);
                        Dust.NewDust(targetProj.position, targetProj.width, targetProj.height, DustID.Smoke, 0, 0, 0, default, 1.2f);
                    }
                }
            }
        } // Fin CheckAndPushFriendlyProjectiles

          // --- NUEVO MÉTODO: CheckAndReflectHostileProjectiles ---
        private void CheckAndReflectHostileProjectiles()
        {
            Player owner = Main.player[Projectile.owner];
            Rectangle kickHitbox = Projectile.Hitbox;

            // Iterar sobre todos los proyectiles
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile hostileProj = Main.projectile[i];

                // --- CONDICIONES PARA REFLEJAR ---
                // 1. Activo
                // 2. ES Hostil
                // 3. NO ha sido golpeado/reflejado por ESTA patada
                // 4. Opcional: ¿Tiene un tipo específico que NO se puede reflejar? (ej. Láseres continuos)
                if (hostileProj.active &&
                    hostileProj.hostile && // <<<--- Comprueba si es hostil
                    !_hitProjectilesThisKick.Contains(hostileProj.whoAmI) &&
                    CanBeReflected(hostileProj)) // <<<--- Función helper opcional para filtrar
                {
                    // Comprobar colisión
                    if (kickHitbox.Intersects(hostileProj.Hitbox))
                    {
                        // --- LÓGICA DE REFLEJO ---

                        // 1. Marcar como golpeado por esta patada
                        _hitProjectilesThisKick.Add(hostileProj.whoAmI);
                        

                        // 2. Invertir Velocidad
                        hostileProj.velocity *= -1f;

                        // 3. Cambiar Afiliación
                        hostileProj.hostile = false;
                        hostileProj.friendly = true;
                        hostileProj.owner = owner.whoAmI; // El jugador ahora es el dueño

                        // 4. Opcional: Aumentar Daño/Tiempo de Vida del Proyectil Reflejado
                        hostileProj.damage = (int)(hostileProj.damage * 1.1f); // Aumenta daño un 10%
                        hostileProj.timeLeft = Math.Min(50, 200); // Añade tiempo de vida, máximo 5 seg

                        // 5. Sincronizar Cambios
                        hostileProj.netUpdate = true;

                        // 6. Efectos Visuales/Sonoros del Reflejo
                        SoundEngine.PlaySound(SoundID.Item109 with { Volume = 0.7f, Pitch = 0.1f }, hostileProj.position); // Sonido de "parry" o reflejo
                        // Polvo de impacto/reflejo
                        for (int d = 0; d < 7; d++) {
                             Dust.NewDust(hostileProj.position, hostileProj.width, hostileProj.height, DustID.MagicMirror, -hostileProj.velocity.X * 0.1f, -hostileProj.velocity.Y * 0.1f, 100, Color.Cyan, 1.1f);
                        }
                    }
                }
            }
        } // Fin CheckAndReflectHostileProjectiles

        // --- Función Helper Opcional para Filtrar Proyectiles Reflejables ---
        private bool CanBeReflected(Projectile proj)
        {
            // Aquí puedes añadir lógica para evitar reflejar ciertos tipos
            // Ejemplo: No reflejar láseres continuos (como los de Moon Lord o Empress)
            if (proj.aiStyle == ProjAIStyleID.Beam) return false; // Estilo de IA de Rayo
            //if (proj.type == ProjectileID.PhantasmalDeathray) return false; // Ejemplo específico

            // Por defecto, permite reflejar la mayoría
            return true;
        }

        // --- Al Morir (Tiempo agotado o Kill llamado) ---
        public override void Kill(int timeLeft)
        {
            // Intenta volver a mostrar al jugador
            TryShowPlayer();

            // Efecto de Polvo Oscuro al desaparecer
            if (Main.netMode != NetmodeID.Server)
            {
                SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.3f, Pitch = 0.5f }, Projectile.Center);
                for (int i = 0; i < (IsStrongKick ? 28 : 18); i++) // Menos polvo que al aparecer
                {
                    int dustType = DustID.Asphalt; // Polvo diferente al desaparecer?
                    Vector2 dustPos = Projectile.position + new Vector2(Main.rand.NextFloat(Projectile.width), Main.rand.NextFloat(Projectile.height));
                    Vector2 dustVel = Main.rand.NextVector2Circular(IsStrongKick ? 2.0f : 1.2f, IsStrongKick ? 2.0f : 1.2f);
                    Dust d = Dust.NewDustPerfect(dustPos, dustType, dustVel, 180, Color.DimGray, IsStrongKick ? 1.2f : 1.0f);
                    d.noGravity = true;
                    d.velocity *= 0.3f;
                }
            }
        }

        // --- Dibujado del Sprite ---
        public override bool PreDraw(ref Color lightColor)
        {
            // Verifica textura
            if (_kickTextureAsset == null || !_kickTextureAsset.IsLoaded) { return false; }

            Texture2D texture = _kickTextureAsset.Value;

            // Calcula frame y origen
            int frameHeight = texture.Height / AnimationFrames;
            int currentFrame = Projectile.frame % AnimationFrames; // Usa módulo para seguridad
            Rectangle sourceRectangle = new Rectangle(0, currentFrame * frameHeight, texture.Width, frameHeight);
            Vector2 origin = new Vector2(sourceRectangle.Width / 2f, sourceRectangle.Height); // Origen Centro Inferior

            // Color
            Color drawColor = lightColor; // Color normal

            // Flip
            SpriteEffects effects = Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            // Rotación
            float rotation = 0f;

            // Escala
            float scale = Projectile.scale;

            // Posición de Dibujo (Anclada al jugador con offset)
            Player owner = Main.player[Projectile.owner];
            Vector2 drawPosBase = owner.MountedCenter - Main.screenPosition;
            drawPosBase.Y += VisualVerticalOffset; // Aplica el offset vertical constante

            // Dibuja
            Main.EntitySpriteDraw(
                texture,
                drawPosBase,
                sourceRectangle,
                drawColor,
                rotation,
                origin,
                scale,
                effects,
                0f);

            return false; // Hemos dibujado
        }


        // --- Al Golpear un NPC ---
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // El daño y knockback a NPCs vienen del Item (a través de los stats de este proyectil)
            Player owner = Main.player[Projectile.owner];
            SoundEngine.PlaySound(SoundID.Item1 with { Pitch = IsStrongKick ? -0.2f : 0.1f }, target.Center);
            for (int i = 0; i < (IsStrongKick ? 10 : 5); i++)
            {
                Dust.NewDust(target.position, target.width, target.height, DustID.Dirt, hit.HitDirection * (IsStrongKick ? 3f : 1.5f), -1.5f, 50, default, IsStrongKick ? 1.2f : 1.0f);
            }
        }

        // --- Métodos Helper para Ocultar/Mostrar Jugador ---
        private void TryHidePlayer()
        {
            if (!_playerHidden && Projectile.owner == Main.myPlayer) // Solo ocultar si es nuestro y no está oculto ya
            {
                Player owner = Main.player[Projectile.owner];
                if (owner.active && owner.TryGetModPlayer<WakfuPlayer>(out var wakfuPlayer))
                {
                    wakfuPlayer.HidePlayerForKick = true;
                    _playerHidden = true;
                }
            }
        }

        private void TryShowPlayer()
        {
            if (_playerHidden && Projectile.owner == Main.myPlayer) // Solo mostrar si es nuestro y lo ocultamos nosotros
            {
                Player owner = Main.player[Projectile.owner];
                if (owner.active && owner.TryGetModPlayer<WakfuPlayer>(out var wakfuPlayer))
                {
                    wakfuPlayer.HidePlayerForKick = false;
                }
                _playerHidden = false; // Marcar como ya no oculto por este proyectil
            }
        }

    } // Fin Clase
}