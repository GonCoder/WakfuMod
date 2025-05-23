// WakmehamehaLeaderProjectile.cs (Content/Projectiles)
using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.ID;
using System;

namespace WakfuMod.Content.Projectiles
{
    // Proyectil invisible que se mueve rápido y deja un rastro
    public class WakmehamehaLeaderProjectile : ModProjectile
    {
        private const float MaxRange = 1500f; // Distancia máxima que recorrerá
        private const int TrailSpawnRate = 1; // Cada cuántos ticks spawnea una partícula de rastro (más bajo = más denso)
        private float distanceTraveled = 0f;
      
        public override void SetDefaults()
        {
            Projectile.width = 10; // Tamaño pequeño, es invisible
            Projectile.height = 10;
            Projectile.friendly = true; // Este no hace daño directamente
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 600; // Tiempo de vida largo (limitado por distancia)
            Projectile.alpha = 255; // Invisible
            Projectile.tileCollide = true; // Atraviesa tiles
            Projectile.ignoreWater = true;
            // No necesita DamageType aquí, se pasa al rastro
        }

        public override void AI()
        {
            

            // 3. Spawneo del Rastro
            // Spawnea basado en tiempo o distancia para densidad constante
            if (Projectile.frameCounter % TrailSpawnRate == 0)
            {
                SpawnTrailParticle();
            }
            Projectile.frameCounter++; // Incrementa el contador de frames


            // 4. Movimiento y Límite de Rango
            float speed = Projectile.velocity.Length(); // Obtiene la velocidad actual
            distanceTraveled += speed;
            if (distanceTraveled > MaxRange)
            {
                Projectile.Kill(); // Se destruye al alcanzar el rango máximo
            }

            // Podríamos añadir un ligero homing o efectos de partículas aquí si quisiéramos
            Lighting.AddLight(Projectile.Center, 0.1f, 0.3f, 0.5f); // Luz tenue si se desea
        }


        private void SpawnTrailParticle()
        {
            _ = Main.player[Projectile.owner];

            // Spawnea la partícula visible y dañina en la posición actual del líder
            Projectile.NewProjectile(
                Projectile.GetSource_FromAI(),
                Projectile.Center, // Posición
                Vector2.Zero,      // Sin velocidad propia
                ModContent.ProjectileType<WakmehamehaTrailParticle>(), // El tipo de partícula de rastro
                Projectile.damage, // Hereda el daño original
                Projectile.knockBack, // Hereda el knockback original
                Projectile.owner,
                ai0: 0, // Puedes usar ai si necesitas pasar más info
                ai1: 0
            );
        }

        // No necesita dibujo complejo si es invisible
        public override bool PreDraw(ref Color lightColor) => false;

         // (Función auxiliar FindNearestNPCTo si decides usarla)
        // private NPC FindNearestNPCTo(Vector2 position, Player player) { ... }
    }
}