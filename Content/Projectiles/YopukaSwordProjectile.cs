using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Audio;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures; // Para IEntitySource

namespace WakfuMod.Content.Projectiles
{
    public class YopukaSwordProjectile : ModProjectile
    {
        // Usaremos ai[0] para rastrear si ya golpeó (0 = no, 1 = sí) para evitar spawns múltiples
        private bool HasStruck { get => Projectile.ai[0] == 1f; set => Projectile.ai[0] = value ? 1f : 0f; }
        // Guardaremos la rabia inicial en ai[1]
        private int InitialRage { get => (int)Projectile.ai[1]; set => Projectile.ai[1] = value; }
        // Guardaremos el daño base en localAI[0] para no depender de originalDamage que puede ser modificado
        private int BaseDamage { get => (int)Projectile.localAI[0]; set => Projectile.localAI[0] = value; }


        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Yopuka Swordfall"); // Opcional
            Main.projFrames[Projectile.type] = 4; // 4 frames de animación
        }

        public override void SetDefaults()
        {
            Projectile.width = 84;
            Projectile.height = 200; // Hitbox base
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1; // No se destruye por golpes
            Projectile.tileCollide = true; // Choca con tiles
            Projectile.DamageType = DamageClass.Melee;
            Projectile.timeLeft = 300;
            Projectile.ignoreWater = true;

            Projectile.usesLocalNPCImmunity = true; // Usar inmunidad por proyectil
            Projectile.localNPCHitCooldown = 15; // Cada NPC solo puede ser golpeado una vez cada 15 ticks por ESTA espada
        }

        // OnSpawn es fiable para inicializar valores, incluso en MP
        public override void OnSpawn(IEntitySource source)
        {
            // Si la fuente es una entidad (como el jugador que usó la habilidad)
            if (source is EntitySource_Parent parentSource && parentSource.Entity is Player player)
            {
                // Calcula el daño base aquí, posiblemente usando la rabia del jugador en ESE momento
                var modPlayer = player.GetModPlayer<jugador.WakfuPlayer>();
                InitialRage = modPlayer.GetRageTicks(); // Obtener rabia actual
                BaseDamage = 20; // O calcula un daño base más complejo aquí si es necesario
                                 // Asignar ai[1] que sí se sincroniza
                Projectile.ai[1] = InitialRage;
            }
            // Si no, poner valores por defecto (aunque no debería pasar si se llama bien)
            else
            {
                InitialRage = 0;
                BaseDamage = 20;
                Projectile.ai[1] = 0;
            }
            HasStruck = false; // Asegurarse que ai[0] empieza en 0
        }


        public override void AI()
        {
            // Animación
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 6)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Projectile.type];
            }

            // Rotación
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            // Efecto de polvo (solo cliente)
            if (Main.rand.NextBool(3) && Main.netMode != NetmodeID.Server)
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0f, 4f, 0, Color.OrangeRed, 1.5f);
            }

            // La detección de golpes se maneja ahora en OnHitNPC y OnTileCollide
        }

        // --- Golpeando a un NPC ---
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Aplicar daño % vida máxima (calculado aquí para ser más preciso al momento del golpe)
            int rage = InitialRage; // Usar la rabia guardada
            // Ajustar porcentaje según la rabia
            float extraPercent = rage >= 5 ? 0.10f : 0.05f; // 10% o 5%
            int extraDamage = (int)(target.lifeMax * extraPercent);

            // Calcular daño final (combinando daño base del proyectil si lo hubiera + extra)
            // Nota: 'hit.Damage' ya incluye el BaseDamage y modificadores del jugador. Añadimos el extra.
            int finalDamage = hit.Damage + extraDamage;

            // Aplicar el golpe final con el daño extra (usando StrikeNPC para asegurar aplicación correcta)
            // Solo el dueño debe llamar a StrikeNPC directamente para aplicar efectos adicionales si los hubiera,
            // pero el daño base ya fue procesado por Terraria para llegar aquí.
            // Podríamos modificar 'hit.Damage' directamente aquí en lugar de llamar a StrikeNPC de nuevo,
            // pero llamar a StrikeNPC es más explícito si queremos aplicar lógicas diferentes.
            // Por simplicidad, modifiquemos hit.Damage directamente:
            // ¡¡Ojo!! Modificar hit no es estándar en OnHitNPC, el daño ya se aplicó.
            // La forma correcta es calcular el daño TOTAL en ModifyHitNPC.

            // --- Corrección: Mover cálculo de daño % a ModifyHitNPC ---
            // Dejamos OnHitNPC para efectos secundarios, como el trigger de shockwaves.

            // Trigger de explosión y shockwaves SOLO la primera vez que golpea
            if (!HasStruck)
            {
                TriggerShockwaves(target.Center); // Usar el centro del NPC como origen
                HasStruck = true;
                Projectile.netUpdate = true; // Sincronizar el cambio de estado de HasStruck
                Projectile.timeLeft = 2; // Reducir tiempo de vida para que muera pronto tras golpear
                Projectile.tileCollide = false; // Evitar que choque con tiles después de golpear
                Projectile.velocity *= 0.1f; // Reducir velocidad drásticamente
            }
        }

        // --- Modificar el golpe ANTES de que se aplique ---
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) // Firma correcta
        {
            // Aplicar daño % vida máxima aquí usando los modificadores
            int rage = (int)Projectile.ai[1]; // Obtener rabia guardada
            float extraPercent = rage >= 5 ? 0.10f : 0.05f;
            int extraDamage = (int)(target.lifeMax * extraPercent);

            // Añadir el daño extra. Usamos FlatBonusDamage para añadir un valor fijo.
            modifiers.FlatBonusDamage += extraDamage;
            modifiers.DefenseEffectiveness *= 0f; // Ignora defensa
            // Modificar el knockback multiplicándolo
            modifiers.Knockback *= 1f + 0.5f * rage; // Ejemplo: Aumenta 50% por cada punto de rabia
        }


        // --- Chocando con un Tile ---
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            // Trigger de explosión y shockwaves SOLO si no ha golpeado ya a un NPC
            if (!HasStruck)
            {
                TriggerShockwaves(Projectile.Center); // Usar el centro del proyectil como origen
                HasStruck = true;
                Projectile.netUpdate = true; // Sincronizar cambio de estado
            }

            // Detener el proyectil y matarlo
            Projectile.velocity = Vector2.Zero;
            Projectile.Kill(); // Llamará a OnKill

            return false; // Impedir que el proyectil rebote o se destruya por defecto por tile collision
        }

        // --- Al desaparecer ---
        public override void OnKill(int timeLeft)
        {
            // Efectos visuales de "muerte" (solo cliente)
            if (Main.netMode != NetmodeID.Server)
            {
                for (int i = 0; i < 20; i++)
                {
                    Vector2 velocity = Main.rand.NextVector2Circular(3f, 3f);
                    int dust = Dust.NewDust(Projectile.Center, 8, 8, DustID.SolarFlare, velocity.X, velocity.Y, 0, Color.Red, 2f);
                    Main.dust[dust].noGravity = true;
                }
                SoundEngine.PlaySound(SoundID.Item14, Projectile.position);
            }
            // No necesitamos llamar a TriggerShockwaves aquí de nuevo si ya se llamó en OnHitNPC o OnTileCollide
        }

        // --- Función para Spawnear Shockwaves (solo dueño) ---
        private void TriggerShockwaves(Vector2 origin)
        {
            // SOLO el dueño del proyectil crea las shockwaves
            if (Main.myPlayer == Projectile.owner)
            {
                Player player = Main.player[Projectile.owner];
                int rage = InitialRage;
                // Usar el BaseDamage almacenado, ya que Projectile.damage puede ser 0 o modificado
                int shockwaveDamage = 1; // O un valor fijo o escalado diferente

                for (int i = 0; i < 2; i++) // Izquierda y Derecha
                {
                    int dir = (i == 0) ? -1 : 1;
                    // Spawn ligeramente alejado del origen del impacto
                    Vector2 spawn = origin + new Vector2(dir * 24, 0);

                    // Lógica para encontrar suelo (mantenerla simple por ahora)
                    spawn.Y = FindGround(spawn); // Llamar a una función auxiliar

                    Vector2 velocity = new Vector2(6f * dir, 0f); // Velocidad base

                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(), // Usar la fuente del proyectil actual
                        spawn,
                        velocity,
                        ModContent.ProjectileType<YopukaShockwaveProjectile>(),
                        shockwaveDamage, // Pasar el daño calculado
                        3f, // Knockback base para shockwave
                        player.whoAmI,
                        ai0: rage, // Pasar la rabia en ai[0]
                        ai1: dir   // Pasar la dirección en ai[1]
                    );
                    // El DamageType se establecerá en SetDefaults de YopukaShockwaveProjectile
                }
            }
        }

        // --- Función auxiliar para encontrar el suelo ---
        private float FindGround(Vector2 position)
        {
            int tileX = (int)(position.X / 16f);
            int startTileY = (int)(position.Y / 16f);
            int endTileY = startTileY + 40; // Buscar hasta 40 tiles hacia abajo

            for (int y = startTileY; y < endTileY; y++)
            {
                if (WorldGen.InWorld(tileX, y, 10))
                {
                    Tile tile = Framing.GetTileSafely(tileX, y);
                    if (tile.HasTile && (Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]))
                    {
                        // Encontró suelo, devolver la coordenada Y del borde superior del tile
                        return y * 16f;
                    }
                }
            }
            // No encontró suelo, devolver la posición Y original (o ajustarla si es necesario)
            return position.Y;
        }


        // --- Dibujado (PreDraw) ---
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>("WakfuMod/Content/Projectiles/YopukaSwordProjectile").Value;
            int frameHeight = texture.Height / Main.projFrames[Projectile.type];
            int frameY = Projectile.frame * frameHeight;

            Rectangle sourceRectangle = new Rectangle(0, frameY, texture.Width, frameHeight);
            // Origen en el centro para rotación
            Vector2 origin = sourceRectangle.Size() / 2f;

            // Mantener la escala visual que tenías
            Vector2 scale = new Vector2(2.5f, 3f);
            // Usar Projectile.direction para flip horizontal si es necesario (aunque la rotación suele manejar esto)
            SpriteEffects effects = Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            Main.EntitySpriteDraw( // Usar EntitySpriteDraw es más moderno
                texture,
                Projectile.Center - Main.screenPosition,
                sourceRectangle,
                lightColor, // Usar el color de la luz calculado por el juego
                Projectile.rotation,
                origin,
                scale, // Aplicar la escala visual
                effects,
                0f);

            return false; // No dibujar el sprite por defecto
        }
    }
}