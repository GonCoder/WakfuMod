using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using WakfuMod.jugador; // Asegúrate que el namespace es correcto
using System; // <<< --- AÑADIR ESTA LÍNEA --- <<<

namespace WakfuMod.Content.Projectiles
{
    public class YopukaJumpAbility : ModProjectile
    {
        private const int JumpDuration = 90; // Duración del salto en ticks
        private const float JumpSpeedX = 20f; // Velocidad horizontal
        private const float InitialJumpSpeedY = -16f; // Impulso vertical inicial (negativo es hacia arriba)
        private const float JumpGravity = 0.9f; // Gravedad durante el salto
        public const int LandingDuration = 30; // Duración del estado de aterrizaje en ticks
        // ai[0] = RageAtJump
        // ai[1] = Direction
        // localAI[0] = Timer (para estado)
        // localAI[1] = HasLanded flag (0 = no, 1 = sí)

  // --- Estados y Parámetros ---
        private bool HasLanded { get => Projectile.localAI[1] == 1f; set => Projectile.localAI[1] = value ? 1f : 0f; }
        private int RageAtJump { get => (int)Projectile.ai[0]; set => Projectile.ai[0] = value; }
        private int Direction { get => (int)Projectile.ai[1]; set => Projectile.ai[1] = value; }
        public float LandingTimer { get => Projectile.localAI[0]; set => Projectile.localAI[0] = value; }


        public override void SetDefaults()
        {
            Projectile.width = 48; // Ajusta al tamaño del sprite de salto/dios
            Projectile.height = 64; // Ajusta al tamaño del sprite de salto/dios
            Projectile.tileCollide = false; // No choca hasta que decidamos
            Projectile.penetrate = -1;
            Projectile.timeLeft = JumpDuration + LandingDuration + 10; // Aumentar un poco por si acaso
            Projectile.friendly = false; // El proyectil en sí daña
            Projectile.hostile = false;
            Projectile.ignoreWater = true;
            Projectile.light = 0.5f; // Añadir un poco de luz propia
            // Projectile.alpha = 100; // Hacerlo semi-transparente si quieres
        }

         public override void OnSpawn(IEntitySource source)
        {
            HasLanded = false;
            LandingTimer = 0;

            if (source is EntitySource_Parent parentSource && parentSource.Entity is Player player)
            {
                var modPlayer = player.GetModPlayer<WakfuPlayer>();

                RageAtJump = modPlayer.GetRageTicks();
                Direction = player.direction;

                modPlayer.ConsumeRage();
                modPlayer.SetJumpVisuals(true); // Activar invisibilidad

                player.AddBuff(BuffID.Ironskin, 120 * RageAtJump);

                Projectile.velocity.X = JumpSpeedX * Direction;
                Projectile.velocity.Y = InitialJumpSpeedY;
                Projectile.Center = player.Center;

                // --- EFECTO DE PARTÍCULAS DIVINAS (INICIO) ---
                if (Main.netMode != NetmodeID.Server)
                {
                    SoundEngine.PlaySound(SoundID.Item74, Projectile.Center); // Sonido celestial/poderoso?
                    int dustType = DustID.HallowedTorch; // O DustID.SolarFlare, DustID.HallowedTorch
                    for (int i = 0; i < 40; i++) // Cantidad de partículas
                    {
                        Vector2 speed = Main.rand.NextVector2Circular(6f, 6f); // Velocidad radial
                        float scale = Main.rand.NextFloat(1.2f, 1.8f);
                        Dust d = Dust.NewDustPerfect(Projectile.Center, dustType, speed, 100, default, scale);
                        d.noGravity = true;
                        d.fadeIn = 0.5f; // Efecto de aparición
                    }
                    for (int i = 0; i < 20; i++) // Partículas secundarias
                    {
                        Vector2 speed = Main.rand.NextVector2Circular(3f, 3f);
                        float scale = Main.rand.NextFloat(0.8f, 1.2f);
                        Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Smoke, speed, 150, Color.White * 0.5f, scale);
                        d.noGravity = true;
                    }
                }
            }
        }

         public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            var modPlayer = player.GetModPlayer<WakfuPlayer>();

            if (!player.active || player.dead) { Projectile.Kill(); return; }

            modPlayer.SetJumpVisuals(true); // Mantener invisibilidad

            if (HasLanded)
            {
                // --- Estado: Aterrizado ---
                LandingTimer++;
                Projectile.velocity = Vector2.Zero;
                player.Center = Projectile.Center; // Mantener jugador anclado

                if (LandingTimer >= LandingDuration) { Projectile.Kill(); }
                return;
            }

            // --- Estado: Saltando ---
            Projectile.velocity.Y += JumpGravity;
            // No limitar velocidad de caída aquí para un arco más natural

            // Forzar posición del jugador y bloquear controles
            player.Center = Projectile.Center;
            player.velocity = Projectile.velocity;
            player.fallStart = (int)(player.position.Y / 16f);
            player.immuneTime = Math.Max(player.immuneTime, 2);
            LockPlayerControls(player);

            // --- Comprobación de Aterrizaje ---
            if (Projectile.velocity.Y > 0f)
            {
                // Comprobar colisión con tiles sólidos o plataformas un poco más abajo
                if (Collision.SolidCollision(Projectile.position + new Vector2(0, 4), Projectile.width, Projectile.height)) // Comprobar 4px más abajo
                {
                    // Encontrar el Y exacto del suelo
                    Vector2 checkPos = Projectile.position + new Vector2(Projectile.width / 2f, Projectile.height + 1f);
                    Point tileCoords = checkPos.ToTileCoordinates();
                    if(WorldGen.InWorld(tileCoords.X, tileCoords.Y, 5)) {
                         Tile tile = Framing.GetTileSafely(tileCoords.X, tileCoords.Y);
                         if (tile.HasTile && (Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType])) {
                             Land(tileCoords.Y * 16f); // Pasar la Y del suelo a Land
                         } else {
                             // Si SolidCollision es true pero no encontramos tile sólido/plataforma,
                             // puede ser un error o una colisión lateral. Aterrizar de todos modos?
                             Land(Projectile.position.Y + Projectile.height); // Aterrizar donde está ahora
                         }
                    }
                     else { Land(Projectile.position.Y + Projectile.height); } // Aterrizar si está fuera del mundo?
                }
            }

            // Matar si se agota el tiempo de salto
            if (LandingTimer < JumpDuration) LandingTimer++;
            else if (!HasLanded) { Projectile.Kill(); }
        }

        private void LockPlayerControls(Player player)
        {
            player.controlJump = false;
            player.controlDown = false;
            player.controlLeft = false;
            player.controlRight = false;
            player.controlUp = false;
            player.controlUseItem = false;
            player.controlUseTile = false;
            player.controlThrow = false;
            player.controlHook = false;
            player.gravDir = 1f;
            if (player.mount.Active) player.mount.Dismount(player);
            player.RemoveAllGrapplingHooks();
        }

        // --- Land ahora acepta la Y del suelo ---
        private void Land(float groundY)
        {
            if (HasLanded) return;
            HasLanded = true;
            LandingTimer = 0; // Reset timer para usarlo en fade out/duración aterrizaje
            Projectile.netUpdate = true;

            Player player = Main.player[Projectile.owner];

            // Ajustar posición final al suelo
            Projectile.velocity = Vector2.Zero;
            Projectile.position.Y = groundY - Projectile.height; // Colocar base justo encima del suelo
            player.Bottom = Projectile.Bottom;

            // Efectos de aterrizaje (solo cliente)
            if (Main.netMode != NetmodeID.Server) {
                SoundEngine.PlaySound(SoundID.Item14, player.position);
                for (int i = 0; i < 30; i++) {  Vector2 speed = Main.rand.NextVector2Circular(3f, 3f);
                        float scale = Main.rand.NextFloat(1.8f, 1.2f);
                        Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Smoke, speed, 150, Color.White * 0.5f, scale);
                        d.noGravity = true;}
            }

            // Spawn Shockwaves (solo dueño)
            if (Main.myPlayer == Projectile.owner) {
                int damage = 1 + 1 * RageAtJump; // Ajustar daño base si es necesario
                int shockwaveRage = RageAtJump;
                for (int i = -1; i <= 1; i += 2) {
                    // Spawn un poco más arriba para que no spawneen bajo tierra
                    Vector2 spawn = player.Bottom + new Vector2(i * 16, -8f);
                    Vector2 velocity = new Vector2(i * 6f, 0f); // Velocidad shockwave original
                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(), spawn, velocity,
                        ModContent.ProjectileType<YopukaShockwaveProjectile>(),
                        damage, 3f, player.whoAmI, ai0: shockwaveRage, ai1: i);
                }
            }
        }

       public override void OnKill(int timeLeft)
        {
            // --- EFECTO DE PARTÍCULAS DIVINAS (FINAL) ---
            if (Main.netMode != NetmodeID.Server)
            {
                 SoundEngine.PlaySound(SoundID.Item68, Projectile.Center); // Otro sonido?
                 int dustType = DustID.YellowTorch;
                 for (int i = 0; i < 30; i++) // Menos partículas al desaparecer
                 {
                     Vector2 speed = Main.rand.NextVector2Circular(4f, 4f);
                     float scale = Main.rand.NextFloat(1.0f, 1.5f);
                     Dust d = Dust.NewDustPerfect(Projectile.Center, dustType, speed, 100, default, scale);
                     d.noGravity = true;
                 }
            }

            // Restaurar visibilidad del jugador
            if (Main.player.IndexInRange(Projectile.owner)) {
                Player player = Main.player[Projectile.owner];
                if (player.active && !player.dead) {
                    var modPlayer = player.GetModPlayer<WakfuPlayer>();
                    modPlayer.SetJumpVisuals(false);
                }
            }
        }

        // --- Dibujado del Proyectil (Dios Yopuka) ---
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>("WakfuMod/Content/Projectiles/YopukaJumpAbility").Value;
            int totalFrames = 3;
            int frame;
            float alpha = 1f; // Alpha para fade out

            // Determinar frame y alpha
            if (HasLanded)
            {
                frame = 2; // Frame de aterrizaje
                // Calcular alpha para fade out durante LandingDuration
                alpha = MathHelper.Lerp(1f, 2f, LandingTimer / LandingDuration);
            }
            else if (Projectile.velocity.Y < -1f) { frame = 0; } // Subida
            else { frame = 1; } // Caída

            int frameHeight = texture.Height / totalFrames;
            Rectangle sourceRect = new Rectangle(0, frame * frameHeight, texture.Width, frameHeight);

            // --- AJUSTE DE ORIGEN Y POSICIÓN PARA FRAME DE ATERRIZAJE ---
            Vector2 origin = sourceRect.Size() / 2f;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            if (HasLanded && frame == 2)
            {
                 // Si el frame de aterrizaje es más alto o necesita un offset:
                 // Opción A: Mover la posición de dibujo hacia arriba
                 // float verticalOffset = -20f; // Ajusta este valor según sea necesario
                 // drawPos.Y += verticalOffset;

                 // Opción B: Ajustar el origen Y para que el "suelo" del sprite coincida con la base del proyectil
                 origin.Y = frameHeight; // Origen en el centro-inferior del frame
                 drawPos = Projectile.Bottom - Main.screenPosition; // Dibujar desde la base
            }


            SpriteEffects flip = Direction == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            // Dibujar con Alpha aplicado
            Main.EntitySpriteDraw(
                texture,
                drawPos, // Usar la posición ajustada
                sourceRect,
                lightColor * alpha, // Aplicar alpha y luz ambiental
                Projectile.rotation,
                origin, // Usar el origen ajustado
                Projectile.scale,
                flip,
                0f
            );

            return false;
        }
    }
}