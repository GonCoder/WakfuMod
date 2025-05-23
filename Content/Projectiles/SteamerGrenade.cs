using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics; // Necesario
using Terraria.Audio;
using WakfuMod.Content.NPCs;
using System;
using ReLogic.Content; // Para Math.Sign

namespace WakfuMod.Content.Projectiles
{
    public class SteamerGrenade : ModProjectile
    {
        // --- Cache para Textura Glowmask ---
        public static Asset<Texture2D> SteamerGrenade_Glow { get; private set; }
        // --- Constantes de Animación ---
        private const int FrameCount = 4;
        private const int FrameSpeed = 10; // Ticks por frame (ajustar)

        public override void Load()
        {
            if (!Main.dedServ)
            {
                SteamerGrenade_Glow = ModContent.Request<Texture2D>(Texture + "_Glow");
            }
        }
        public override void Unload()
        {
            SteamerGrenade_Glow = null;
        }


        private const int ExplodeTime = 5 * 60;
        private bool stuckToEnemy = false;
        private NPC stuckTarget;
        private Vector2 offsetFromTarget;

        // --- Registrar Frames ---
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Steamer Grenade"); // .hjson
            Main.projFrames[Type] = FrameCount; // Indicar número de frames
        }

        public override void SetDefaults()
        {
            // Ajustar tamaño a UN SOLO frame
            Projectile.width = 14; // Ancho de un frame
            Projectile.height = 14; // Alto de un frame
            Projectile.aiStyle = -1; // Usaremos AI personalizada para animación
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = ExplodeTime + 1;
            Projectile.ignoreWater = false;
            Projectile.tileCollide = true;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;

            // --- Ajustes para comportamiento Granada ---
            Projectile.usesLocalNPCImmunity = true; // Evitar multi-hit rápido antes de pegarse
            Projectile.localNPCHitCooldown = 10;
        }

        public override void AI()
        {

            // --- Lógica de Animación ---
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= FrameSpeed)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % FrameCount; // Ciclar frames 0, 1, 2, 0, 1, ...
            }
            // --- Lógica de Adhesión ---
            if (stuckToEnemy)
            {
                if (stuckTarget != null && stuckTarget.active && !stuckTarget.dontTakeDamage) // Asegurarse que el target sigue vivo y puede ser dañado
                {
                    Projectile.Center = stuckTarget.Center + offsetFromTarget;
                   
                }
                else
                {
                    // Si el target muere o se vuelve inválido, la granada cae y explota antes
                    stuckToEnemy = false;
                    Projectile.tileCollide = true;
                    Projectile.timeLeft = Math.Min(Projectile.timeLeft, 15); // Explota rápido
                }
                // No aplicar gravedad ni rotación si está pegado
            }
            else // Si NO está pegado
            {
                // Aplicar gravedad
                Projectile.velocity.Y += 0.3f;
                if (Projectile.velocity.Y > 10f) Projectile.velocity.Y = 10f; // Limitar velocidad caída

                // Aplicar rotación basada en velocidad horizontal
                Projectile.rotation += Projectile.velocity.X * 0.2f;

                // Comprobar colisión manual con enemigos para intentar pegarse
                // (OnHitNPC a veces no es suficiente si penetrate es -1)
                // Esta parte es opcional si OnHitNPC funciona bien con penetrate = 1
                /*
                Rectangle hitbox = Projectile.Hitbox;
                for(int i = 0; i < Main.maxNPCs; i++) {
                    NPC npc = Main.npc[i];
                    if(npc.active && !npc.friendly && !npc.dontTakeDamage && npc.Hitbox.Intersects(hitbox)) {
                        StickToNPC(npc);
                        break; // Pegarse solo al primero que toca
                    }
                }
                */
            }

            // Polvo (como antes)
            if (Main.rand.NextBool(3))
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.VilePowder, 0f, 0f, 100, default, 1.2f);
        }

        // --- Colisión con Tiles ---
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            // Rebotar un poco y reducir velocidad horizontal, mantener vertical si golpea techo
            if (Projectile.velocity.X != oldVelocity.X) Projectile.velocity.X = -oldVelocity.X * 0.8f;
            if (Projectile.velocity.Y != oldVelocity.Y) Projectile.velocity.Y = -oldVelocity.Y * 0.9f;
            // Sonido de rebote
            SoundEngine.PlaySound(SoundID.Dig, Projectile.position);
            return false; // No matar el proyectil al chocar
        }

        // --- Al Golpear NPC ---
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Solo pegarse si no está ya pegado
            if (!stuckToEnemy)
            {
                StickToNPC(target);
            }
            // No aplicar daño en el impacto inicial, solo al explotar
            // Nota: Poner hit.Damage = 0 aquí podría no funcionar, es mejor no darle daño base al proyectil
            // y calcularlo todo en Kill(). SetDefaults ya tiene damage = 0 implícito si no se pone.
        }

        // --- MÉTODO PARA COMPROBAR SI ESTÁ PEGADA A UN NPC ESPECÍFICO ---
        public bool IsStuckTo(NPC npc)
        {
            // Devuelve true si está pegada (stuckToEnemy es true) Y
            // si el NPC al que está pegada (stuckTarget) es el mismo que el npc que se pasa como parámetro.
            return stuckToEnemy && stuckTarget == npc;
        }


        // --- Función para Pegarse ---
        public void StickToNPC(NPC target)
        {
            stuckToEnemy = true;
            stuckTarget = target;
            offsetFromTarget = Projectile.Center - target.Center; // Guardar offset relativo
            Projectile.velocity = Vector2.Zero; // Detener movimiento
            Projectile.tileCollide = false; // Ya no choca con tiles
            Projectile.timeLeft = ExplodeTime; // Resetear tiempo de explosión
            Projectile.penetrate = -1; // Ya no necesita penetrar más

            // Marcar al enemigo (tu lógica original)
            var globalNPC = target.GetGlobalNPC<SteamerGlobalNPC>();
            if (globalNPC != null)
            {
                globalNPC.isMarkedByGrenade = true;
                globalNPC.linkedGrenade = Projectile.whoAmI;
            }

            // Sonido de pegarse?
            SoundEngine.PlaySound(SoundID.Item37, Projectile.position); // Sonido tipo "click"
        }


        // --- Impedir daño si está pegado ---
        // Este método es más fiable que modificar hit.Damage en OnHitNPC
        public override bool? CanHitNPC(NPC target)
        {
            // No puede golpear si ya está pegado
            // Solo puede golpear al NPC al que NO está pegado, para el primer impacto
            if (stuckToEnemy) return false;
            // Permitir el golpe inicial
            return base.CanHitNPC(target);
        }


         // --- Lógica de Explosión (Kill) ---
        public override void Kill(int timeLeft)
        {
            // Usar Projectile.Center como el punto de origen exacto
            Vector2 explosionCenter = Projectile.Center; // <-- CAMBIO: Usar variable clara para el centro

            int baseDamage = 20;
            float radius = 200f; // Radio de daño normal
            bool potenciada = Projectile.ai[0] == 1f;

            // --- Lógica de Partículas y Sonido ---
            if (potenciada)
            {
                // --- Explosión Potenciada (Morada) ---
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.8f }, explosionCenter); // Ajusta sonido si quieres

                // Aumenta la velocidad de las partículas para expansión mayor
                float dustSpeedPotenciada = 18f; // <-- AJUSTA esta velocidad (antes era 15f)
                int dustCountPotenciada = 80; // <-- AJUSTA cantidad si quieres

                for (int i = 0; i < dustCountPotenciada; i++)
                {
                    // Genera velocidad aleatoria circular con la nueva velocidad
                    Vector2 dustVelocity = Main.rand.NextVector2Circular(dustSpeedPotenciada, dustSpeedPotenciada);

                    // --- CAMBIO: Usa Dust.NewDustPerfect ---
                    Dust dustObject = Dust.NewDustPerfect( // <-- Guarda el objeto Dust
                        explosionCenter,
                        DustID.Clentaminator_Purple,
                        dustVelocity,
                        150,
                        Color.MediumPurple,
                        2.0f
                    );
                    // Modifica el objeto Dust
                    dustObject.noGravity = true; // <-- Usa dustObject.
                    // Opcional: Añadir deceleración o comportamiento extra al polvo
                     dustObject.velocity *= 0.95f; // Decelera ligeramente
                }
                 radius = 350f; // <-- Radio de daño aumentado para explosión potenciada
            }
            else
            {
                // --- Explosión Normal (Naranja) ---
                SoundEngine.PlaySound(SoundID.NPCDeath14, explosionCenter); // Sonido normal

                float dustSpeedNormal = 5f; // <-- Velocidad para explosión normal
                int dustCountNormal = 30; // <-- Cantidad normal

                for (int i = 0; i < dustCountNormal; i++)
                {
                    Vector2 dustVelocity = Main.rand.NextVector2Circular(dustSpeedNormal, dustSpeedNormal);

                    // --- CAMBIO: Usa Dust.NewDustPerfect ---
                    Dust dustObject = Dust.NewDustPerfect( // <-- Guarda el objeto Dust
                        explosionCenter,
                        DustID.Torch,
                        dustVelocity,
                        100,
                        default,
                        1.2f
                    );
                    // Modifica el objeto Dust
                    dustObject.noGravity = false; // <-- Usa dustObject. ¿Quieres que caiga?
                       dustObject.velocity.Y -= 0.5f; // Que suba un poco al inicio?
                }
                // El radio de daño se mantiene en 200f para la normal
            }

            // --- Lógica de Daño (Ajustada para usar explosionCenter y el radio correcto) ---
            if (Main.myPlayer == Projectile.owner)
            {
                foreach (NPC npc in Main.npc)
                {
                    // Usar el radio correcto (200f normal, 350f potenciada)
                    if (npc.active && !npc.friendly && npc.CanBeChasedBy(Projectile) && npc.DistanceSQ(explosionCenter) <= radius * radius)
                    {
                        int totalDamage = baseDamage;
                        if (potenciada)
                        {
                            totalDamage += 20;
                            totalDamage += (int)(npc.lifeMax * 0.1f); // Daño % vida max
                        }
                        // Aplicar daño
                        Main.player[Projectile.owner].ApplyDamageToNPC(npc, totalDamage, 2f, Math.Sign(npc.Center.X - explosionCenter.X), false, Projectile.DamageType); // Knockback bajo
                    }
                }
            }
        } // Fin Kill()


        // --- Dibujar Glowmask (Animado) ---
        public override void PostDraw(Color lightColor)
        {
            if (SteamerGrenade_Glow == null) return;

            Texture2D texture = SteamerGrenade_Glow.Value;
            // --- Obtener el frame correcto ---
            int frameHeight = texture.Height / FrameCount;
            Rectangle frame = new Rectangle(0, Projectile.frame * frameHeight, texture.Width, frameHeight);
            // --- Fin obtener frame ---

            Vector2 origin = frame.Size() / 2f;
            Vector2 position = Projectile.Center - Main.screenPosition;
            Color glowColor = Color.White * Projectile.Opacity;

            Main.spriteBatch.Draw(texture, position, frame, glowColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
        }

        // --- Opcional: PreDraw para dibujar el sprite normal animado ---
        // Si no anulas PreDraw, Terraria dibujará el frame actual automáticamente.
        // Si necesitas más control (ej. rotación especial), hazlo aquí.
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Type].Value; // Obtener textura base

            // Obtener el frame correcto
            int frameHeight = texture.Height / FrameCount;
            Rectangle frame = new Rectangle(0, Projectile.frame * frameHeight, texture.Width, frameHeight);
            Vector2 origin = frame.Size() / 2f;
            Vector2 position = Projectile.Center - Main.screenPosition;
            Color drawColor = Projectile.GetAlpha(lightColor); // Color con alpha si se desvanece

            Main.spriteBatch.Draw(texture, position, frame, drawColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);

            return false; // Indicar que ya hemos dibujado el proyectil base
        }

    } // Fin clase SteamerGrenade
}