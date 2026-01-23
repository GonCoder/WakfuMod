// WakmehamehaLeaderProjectile.cs (Content/Projectiles)
using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.ID;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;
using System;

namespace WakfuMod.Content.Projectiles
{
    // Proyectil visible (Láser) que deja hitbox residual
    public class WakmehamehaLeaderProjectile : ModProjectile
    {
        private const float MaxRange = 1500f; // Distancia máxima que recorrerá
        private const int TrailSpawnRate = 2; // Spawnea hitbox cada X ticks
        private float distanceTraveled = 0f;
      
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 40; // Rastro visual largo
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0; // Guardar posiciones exactas
        }

        public override void SetDefaults()
        {
            Projectile.width = 14; 
            Projectile.height = 14;
            Projectile.friendly = true; // El líder también hace daño al impactar
            Projectile.hostile = false;
            Projectile.penetrate = -1; // Infinita penetración (o lo que se desee)
            Projectile.timeLeft = 600; 
            Projectile.alpha = 255; // Se dibuja manualmente en PreDraw
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 2; // Más actualizaciones por frame = movimiento más rápido y fluido
        }

        public override void AI()
        {
            // 0. Ensure Texture is loaded (if using vanilla texture manually)
            Main.instance.LoadProjectile(ProjectileID.ShadowBeamFriendly);

            // 1. Rotación hacia la velocidad
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            // 2. Spawneo del Rastro (Hitboxes invisibles)
            if (Projectile.frameCounter % TrailSpawnRate == 0)
            {
                SpawnTrailParticle();
            }
            Projectile.frameCounter++; // Incrementa el contador de frames


            // 4. Movimiento y Límite de Rango
            float speed = Projectile.velocity.Length(); 
            distanceTraveled += speed; // (Al tener extraUpdates este valor sube más rápido por frame real)
            if (distanceTraveled > MaxRange)
            {
                Projectile.Kill(); 
            }

            // Luz en la punta
            Lighting.AddLight(Projectile.Center, 0.1f, 0.5f, 0.7f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[ProjectileID.ShadowBeamFriendly].Value;
            Vector2 drawOrigin = new Vector2(texture.Width * 0.5f, texture.Height * 0.5f);
            
            // Dibujar efecto de 'Afterimage' / Laser Trail
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                // Posición ajustada
                Vector2 drawPos = Projectile.oldPos[i] - Main.screenPosition + Projectile.Size / 2f;
                
                // Color fade out - Usando blanco/teal brillante
                float progress = (float)(Projectile.oldPos.Length - i) / Projectile.oldPos.Length;
                Color color = Color.Lerp(Color.Cyan, Color.White, 0.5f) * progress;
                color.A = 255; // Forzar opacidad en el dibujo

                // Escala fade out
                float scale = Projectile.scale * (1f - (float)i / Projectile.oldPos.Length);
                if (i == 0) scale = 1.2f; // Cabeza más grande

                Main.EntitySpriteDraw(
                    texture, 
                    drawPos, 
                    null, 
                    color, 
                    Projectile.rotation, 
                    drawOrigin, 
                    scale, 
                    SpriteEffects.None, 
                    0
                );
            }
            return false; // No dibujar la versión vanilla
        }

        private void SpawnTrailParticle()
        {
            // Calculate parameters for the visual line
            // ai0: Rotation (Angle of velocity)
            // ai1: Length of the segment (Gap size) to stretch the texture
            float rotation = Projectile.velocity.ToRotation();
            float gapSize = Projectile.velocity.Length() * TrailSpawnRate;

            // Spawnea la partícula hitbox invisible (que ahora será visual también)
            Projectile.NewProjectile(
                Projectile.GetSource_FromAI(),
                Projectile.Center, 
                Vector2.Zero,      
                ModContent.ProjectileType<WakmehamehaTrailParticle>(), 
                Projectile.damage, 
                Projectile.knockBack, 
                Projectile.owner,
                ai0: rotation,
                ai1: gapSize * 1.5f // un poco de overlap para que la línea se vea continua
            );
        }
    }
}