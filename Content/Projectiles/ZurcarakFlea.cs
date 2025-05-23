// Content/Projectiles/ZurcarakFlea.cs
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System;
using Terraria.Audio;

namespace WakfuMod.Content.Projectiles // Reemplaza WakfuMod si es necesario
{
    public class ZurcarakFlea : ModProjectile
    {
        // --- Constantes ---
        private const int Lifetime = 20 * 60; // 20 segundos
        private const float DetectRange = 500f;
        private const float MoveSpeed = 10f;
        private const float Inertia = 20f;
        private const int AttackCooldown = 60; // 1 ataque por segundo

        // ai[0]: Cooldown de ataque

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Ecaflip Flea");
             Main.projFrames[Type] = 4; // Asume 4 frames de animación para la pulga
        }

        public override void SetDefaults()
        {
            Projectile.width = 16; // Tamaño pequeño
            Projectile.height = 16;
            Projectile.aiStyle = -1; // AI personalizada
            Projectile.friendly = true; // Puede golpear enemigos
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Summon; // Daño de invocación
            Projectile.timeLeft = Lifetime;
            Projectile.penetrate = 3; // Puede golpear 3 veces antes de desaparecer? O -1 para que dure el tiempo completo? Mejor -1.
            Projectile.penetrate = -1;
            Projectile.tileCollide = true; // Choca con bloques? O los atraviesa? Probemos true.
            Projectile.ignoreWater = true;

             // Inmunidad local para evitar multi-hit instantáneo
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = AttackCooldown / 2; // Cooldown de golpe más corto que el de ataque

            Projectile.damage = 1; // Daño base 1, el real es % vida
            Projectile.knockBack = 0.1f; // Knockback mínimo
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active) Projectile.Kill(); // Desaparece si el dueño se va

            // --- Decrementar Cooldown ---
            if (Projectile.ai[0] > 0) Projectile.ai[0]--;

            // --- Buscar Objetivo ---
            NPC target = FindClosestEnemy(DetectRange);

            // --- Movimiento ---
            if (target != null)
            {
                // --- Homing Simple ---
                Vector2 directionToTarget = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY);
                Projectile.velocity = (Projectile.velocity * (Inertia - 1) + directionToTarget * MoveSpeed) / Inertia;

                // --- Intentar Atacar si está cerca y cooldown listo ---
                 if (Projectile.DistanceSQ(target.Center) < 50f*50f && Projectile.ai[0] <= 0)
                 {
                      // La colisión normal y ModifyHitNPC se encargarán del daño
                      // Aquí solo reiniciamos el cooldown
                      Projectile.ai[0] = AttackCooldown;
                      // Sonido de ataque?
                       SoundEngine.PlaySound(SoundID.Item17, Projectile.position); // Sonido tipo abeja
                 }
            }
            else
            {
                // --- Volver al Jugador si no hay objetivo ---
                Vector2 vectorToPlayer = owner.Center - Projectile.Center;
                if (vectorToPlayer.Length() > 200f) // Solo si está lejos
                {
                     Vector2 directionToPlayer = Vector2.Normalize(vectorToPlayer);
                     Projectile.velocity = (Projectile.velocity * (Inertia - 1) + directionToPlayer * (MoveSpeed * 0.8f)) / Inertia; // Vuelve más lento
                }
                 else {
                      Projectile.velocity *= 0.95f; // Frenar cerca del jugador
                 }
            }

            // --- Rotación y Animación ---
            Projectile.rotation = Projectile.velocity.X * 0.05f; // Inclinación leve
            Projectile.spriteDirection = Projectile.direction;

            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 6) // Velocidad de animación
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type]; // Ciclar frames
            }
        }

         // --- ModifyHitNPC para Daño % Vida ---
         public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
         {
              // 0.5% de la vida máxima
              float percentDamage = target.lifeMax * 0.005f;
              if (percentDamage < 1f) percentDamage = 1f; // Mínimo 1
              modifiers.FlatBonusDamage += percentDamage;

               // El roll aleatorio del jugador se aplicará automáticamente después
         }

         // --- Buscar Enemigo ---
         private NPC FindClosestEnemy(float maxRange) {
             NPC closestNPC = null;
             float SqrMaxRange = maxRange * maxRange;
             for (int i = 0; i < Main.maxNPCs; i++) {
                 NPC npc = Main.npc[i];
                 if (npc.CanBeChasedBy(this, false)) {
                     float sqrDist = Projectile.DistanceSQ(npc.Center);
                     if (sqrDist < SqrMaxRange) {
                         //if (Collision.CanHitLine(Projectile.position, Projectile.width, Projectile.height, npc.position, npc.width, npc.height)) {
                             SqrMaxRange = sqrDist;
                             closestNPC = npc;
                         //}
                     }
                 }
             }
             return closestNPC;
         }

         // --- Comportamiento al Chocar con Tiles ---
         public override bool OnTileCollide(Vector2 oldVelocity) {
             // Rebotar ligeramente
             if (Projectile.velocity.X != oldVelocity.X) Projectile.velocity.X = -oldVelocity.X * 0.5f;
             if (Projectile.velocity.Y != oldVelocity.Y) Projectile.velocity.Y = -oldVelocity.Y * 0.5f;
             return false; // No destruir al chocar
         }

    } // Fin Clase ZurcarakFlea
}