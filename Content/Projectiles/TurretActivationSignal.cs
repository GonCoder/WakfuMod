// TurretActivationSignal.cs
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System;
using Terraria.Audio; // Para Math.Max

namespace WakfuMod.Content.Projectiles
{
    public class TurretActivationSignal : ModProjectile
    {
        private const int DustSpawnRate = 2;
        private const float DustSpread = 8f;
        private const float StopDistance = 20f; // A qué distancia de la torreta debe detenerse

        // Guarda la referencia a la torreta objetivo
        private Projectile _targetTurret = null;
        private bool _targetInitialized = false;

        public override void SetDefaults()
        {
             Projectile.width = 20; // Hitbox un poco más grande para el daño
            Projectile.height = 20;
            // --- CAMBIO: Ahora es friendly ---
            Projectile.friendly = true; // Puede dañar NPCs
            Projectile.hostile = false;
            Projectile.penetrate = -1; // <-- CAMBIO: Que golpee solo a 1 enemigo y desaparezca
                                      // O -1 si quieres que atraviese y siga hacia la torreta
            Projectile.timeLeft = 120;
            Projectile.alpha = 255; // Sigue siendo invisible
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.aiStyle = -1;
            // --- NUEVO: Establecer daño y knockback ---
            Projectile.DamageType = DamageClass.Summon; // O Summon, o Generic... Elige uno apropiado
            Projectile.damage = 2; // Daño mínimo
            Projectile.knockBack = 1.5f; // Knockback bajo
            Projectile.ContinuouslyUpdateDamageStats = true;
        }

        public override void AI()
        {
            // --- Inicializar Objetivo (Primer Frame) ---
            if (!_targetInitialized)
            {
                int targetIndex = (int)Projectile.ai[0]; // Lee el índice de ai[0]
                if (targetIndex >= 0 && targetIndex < Main.maxProjectiles && Main.projectile[targetIndex].active && Main.projectile[targetIndex].type == ModContent.ProjectileType<SteamerTurretProjectile>())
                {
                    _targetTurret = Main.projectile[targetIndex]; // Guarda la referencia
                }
                else
                {
                    // Si no se encuentra la torreta (raro, pero posible), mátalo
                    Projectile.Kill();
                    return;
                }
                _targetInitialized = true;
            }

            // --- Si el objetivo se vuelve inválido, mátalo ---
            if (_targetTurret == null || !_targetTurret.active)
            {
                Projectile.Kill();
                return;
            }

            // --- Comprobación de Distancia para Detenerse ---
            Vector2 vectorToTarget = _targetTurret.Center - Projectile.Center;
            float distanceToTarget = vectorToTarget.Length();

            if (distanceToTarget < StopDistance)
            {
                // Llegó al objetivo, mátalo
                Projectile.Kill();
                // Opcional: Crear un último burst de polvo en la torreta
                SpawnFinalBurst(_targetTurret.Center);
                return;
            }

            // --- Ajuste de Velocidad si se Pasa (Opcional pero robusto) ---
            // Si en el próximo frame va a sobrepasar el objetivo, ajusta la velocidad
            // para que aterrice exactamente en él (o cerca).
            float speed = Projectile.velocity.Length();
             if (speed > 0 && distanceToTarget < speed) // Si la distancia es menor que lo que se moverá
             {
                 // Mueve el proyectil directamente al punto de parada cerca de la torreta
                 Projectile.velocity = Vector2.Normalize(vectorToTarget) * Math.Max(0, distanceToTarget - StopDistance / 2f); // Mueve hasta casi tocar
                 // O simplemente mátalo aquí también si prefieres
                 // Projectile.Kill();
                 // return;
             }


            // --- Spawneo del Polvo Eléctrico (Igual que antes) ---
            if (Projectile.frameCounter % DustSpawnRate == 0)
            {
                Vector2 perpendicular = Projectile.velocity.RotatedBy(MathHelper.PiOver2).SafeNormalize(Vector2.Zero);
                Vector2 dustPos = Projectile.Center + perpendicular * Main.rand.NextFloat(-DustSpread, DustSpread);
                Dust dust = Dust.NewDustPerfect(dustPos, DustID.Electric, Projectile.velocity * 0.1f, 100, Color.Cyan, 1.0f);
                dust.noGravity = true;
                dust.velocity *= 0.3f;
            }
            Projectile.frameCounter++;

            // --- (Opcional) Luz ---
            Lighting.AddLight(Projectile.Center, Color.Cyan.ToVector3() * 0.2f);
        }

          // --- NUEVO: OnHitNPC para efecto al golpear ---
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
             // Efecto visual/sonoro al golpear un NPC
             if (Main.netMode != NetmodeID.Server) {
                 SoundEngine.PlaySound(SoundID.Item93 with { Volume = 0.5f, Pitch = 0.5f }, Projectile.position); // Sonido eléctrico corto
                 for (int i = 0; i < 5; i++) {
                     Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Electric, hit.HitDirection * 0.5f, -0.5f, 100, default, 0.8f);
                     d.noGravity = true;
                     d.velocity *= 0.6f;
                 }
             }

        }
          public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
             // Asegura que el daño sea siempre 1, ignorando defensa (por ejemplo)
             modifiers.SourceDamage.Base = 5; // Fija el daño base
             modifiers.DisableCrit();        // No puede ser crítico
             modifiers.DefenseEffectiveness *= 0f; // Ignora defensa
        }

        // (Opcional) Método para el burst final
        
        private void SpawnFinalBurst(Vector2 position) {
             if (Main.netMode == NetmodeID.Server) return;
             for(int i=0; i<15; i++) {
                  Dust d = Dust.NewDustPerfect(position, DustID.Electric, Main.rand.NextVector2Circular(2f, 2f), 100, Color.White, 1.2f);
                  d.noGravity = true;
             }
        }
       

        public override bool PreDraw(ref Color lightColor) => false;
    }
}