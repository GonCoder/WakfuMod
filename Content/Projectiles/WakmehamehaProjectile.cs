// WakmehamehaProjectile.cs
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace WakfuMod.Content.Projectiles
{
    public class WakmehamehaProjectile : ModProjectile
    {
        private const int ChargeTime = 36;
        private const int ActiveTime = 240;
        private const float MaxBeamLength = 1000f;
        private const float BeamWidth = 40f;
        private bool hasExplodedPortals = false;

        // Cooldown para daño por NPC
        private Dictionary<int, int> npcDustCooldowns = new();

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 3;
        }

        public override void SetDefaults()
        {
            Projectile.width = 4;
            Projectile.height = 4;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = ChargeTime + ActiveTime;
            Projectile.DamageType = DamageClass.Ranged;
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];

            if (Projectile.timeLeft > ActiveTime)
            {
                Projectile.velocity = Vector2.Zero;
                Projectile.Center = player.MountedCenter;
                Projectile.frameCounter++;
                if (Projectile.frameCounter >= 12)
                {
                    Projectile.frameCounter = 0;
                    Projectile.frame++;
                    if (Projectile.frame >= 3)
                        Projectile.frame = 0;
                }
                return;
            }

            // Firing Phase Setup - No changes needed
            if (Projectile.localAI[0] < ChargeTime)
            {
                Vector2 direction = player.DirectionTo(Main.MouseWorld).SafeNormalize(Vector2.UnitX);
                Projectile.velocity = direction;
                Projectile.localAI[0] = ChargeTime; // Mark as fired
                                                    // Play firing sound once
                SoundEngine.PlaySound(SoundID.Item125, Projectile.Center); // Example: Zenith swing sound
            }

            // Calculate beam length - No changes needed
            float beamLength = MaxBeamLength;
            Vector2 beamStart = Projectile.Center;
            Vector2 beamEnd = beamStart + Projectile.velocity * beamLength;
            float[] samples = new float[3];
            Collision.LaserScan(beamStart, Projectile.velocity, 0, MaxBeamLength, samples);
            beamLength = (samples[0] + samples[1] + samples[2]) / 3f;
            beamEnd = beamStart + Projectile.velocity * beamLength;

            // Only perform damage and portal checks if portals haven't been exploded by this beam yet
            if (!hasExplodedPortals)
            {
                ApplyBeamDamage(beamStart, beamEnd, player); // Pass player for damage calculation context
                CheckAndTriggerPortalExplosion(beamStart, beamEnd, player); // Updated method call
            }

            UpdateDustCooldowns();
            SpawnBeamDust(beamStart, Projectile.velocity, beamLength, player); // Pass player for dust damage context
        }

        private void UpdateDustCooldowns()
        {
            List<int> keys = new(npcDustCooldowns.Keys);
            foreach (int npcId in keys)
            {
                npcDustCooldowns[npcId]--;
                if (npcDustCooldowns[npcId] <= 0)
                    npcDustCooldowns.Remove(npcId);
            }
        }



        // --- Updated Portal Check Logic ---
        private void CheckAndTriggerPortalExplosion(Vector2 beamStart, Vector2 beamEnd, Player player)
        {
            // Only check if we haven't already exploded portals with this shot
            if (hasExplodedPortals) return;

            bool portalHit = false;
            float unusedCollisionPoint = 0f;

            // Define portal hitbox (adjust size as needed, e.g., 32x32 centered)
            Vector2 portalHitboxSize = new Vector2(32f, 32f);

            // Check collision with portal 1
            if (PortalHandler.portal1.HasValue && PortalHandler.portal1ID != -1) // Check ID too
            {
                Rectangle portal1Rect = Utils.CenteredRectangle(PortalHandler.portal1.Value, portalHitboxSize);
                if (Collision.CheckAABBvLineCollision(portal1Rect.TopLeft(), portal1Rect.Size(), beamStart, beamEnd, BeamWidth, ref unusedCollisionPoint))
                {
                    portalHit = true;
                }
            }

            // Check collision with portal 2 (only if portal 1 wasn't hit, or check anyway if desired)
            if (!portalHit && PortalHandler.portal2.HasValue && PortalHandler.portal2ID != -1) // Check ID too
            {
                Rectangle portal2Rect = Utils.CenteredRectangle(PortalHandler.portal2.Value, portalHitboxSize);
                if (Collision.CheckAABBvLineCollision(portal2Rect.TopLeft(), portal2Rect.Size(), beamStart, beamEnd, BeamWidth, ref unusedCollisionPoint))
                {
                    portalHit = true;
                }
            }

            // If ANY portal was hit by the beam...
            if (portalHit)
            {
                // ...trigger the VIOLENT explosion sequence in PortalHandler
                PortalHandler.TriggerViolentPortalExplosion(player);
                hasExplodedPortals = true; // Set flag to prevent re-triggering and further beam damage

                // Optional: Stop the beam immediately after hitting a portal
                // Projectile.timeLeft = 20; // Or set to a small value for fade out
            }
        }

        public void ApplyBeamDamage(Vector2 start, Vector2 end, Player player) // 'player' ya se pasa
        {
            float beamActualLength = (end - start).Length();
            // Hitbox para detección inicial (puede ser aproximada)
            Rectangle beamHitbox = Utils.CenteredRectangle(start + Projectile.velocity * beamActualLength * 0.5f, new Vector2(beamActualLength, BeamWidth));

            int owner = Projectile.owner;

            foreach (NPC npc in Main.npc)
            {
                // Comprobaciones iniciales (activo, hostil, puede ser objetivo, dentro de la hitbox AABB)
                if (npc.active && !npc.friendly && npc.CanBeChasedBy(Projectile) && !npc.dontTakeDamage && npc.getRect().Intersects(beamHitbox))
                {
                    // --- Comprobación de Colisión más Precisa (Línea vs Rectángulo) ---
                    // Es importante para rayos asegurar que el NPC realmente toca la línea del rayo
                    float _collisionPoint = 0f; // Variable dummy
                    if (Collision.CheckAABBvLineCollision(npc.getRect().TopLeft(), npc.getRect().Size(), start, end, BeamWidth, ref _collisionPoint))
                    {
                        // --- Aplicar Daño SOLO si no tiene inmunidad por ESTE proyectil ---
                        // Usamos la inmunidad estándar del jugador para rayos continuos
                        if (npc.immune[owner] <= 0)
                        {
                            // --- Calcular Daño % Vida ---
                            float percentOfMaxLife = 0.005f; // 0.5%
                            int calculatedDamage = 5 + (int)(npc.lifeMax * percentOfMaxLife);

                            // --- Aplicar Daño con SimpleStrikeNPC (Ignora Defensa) ---
                            int hitDirection = Math.Sign(Projectile.velocity.X);
                            if (hitDirection == 0) hitDirection = Math.Sign(npc.Center.X - player.Center.X); // Dirección si el rayo es vertical
                            if (hitDirection == 0) hitDirection = 1;

                            npc.SimpleStrikeNPC(
                                damage: calculatedDamage,
                                hitDirection: hitDirection,
                                crit: true, // No crítico
                                knockBack: 1f, // Aplicar el knockback base 1 que pusiste
                                damageType: DamageClass.Ranged,
                                noPlayerInteraction: true //ni puta idea de que coño es esto
                            );

                            // --- Establecer Inmunidad ---
                            // ¡Importante para rayos continuos! Evita golpes múltiples cada tick.
                            // Un valor bajo (ej. 5-10) permite que el rayo golpee frecuentemente pero no en cada frame.
                            npc.immune[owner] = 10; // Ejemplo: 10 ticks (1/6 seg) de inmunidad a este jugador
                        }
                    }
                }
            }
        }



        public void SpawnBeamDust(Vector2 start, Vector2 direction, float length, Player player)
        {
            if (Projectile.timeLeft < 5) return; // Fade out dust quickly at the end

            int numDustPerSegment = 3; // How many dust particles per spacing segment
            float spacing = 8f; // Distance between dust spawn points along the beam
            int numSegments = (int)(length / spacing);
            if (numSegments <= 0) return;

            Vector2 perp = new Vector2(-direction.Y, direction.X); // Get vector perpendicular to beam direction
            float dustSpread = BeamWidth * 0.4f; // How far dust spreads perpendicular to the beam

            for (int i = 0; i < numSegments; i++)
            {
                Vector2 segmentPos = start + direction * (i * spacing);
                for (int j = 0; j < numDustPerSegment; j++)
                {
                    // Spawn dust slightly offset perpendicularly
                    Vector2 pos = segmentPos + perp * Main.rand.NextFloat(-dustSpread, dustSpread);

                    // Use a visually appropriate dust
                    int dustIndex = Dust.NewDust(pos - Vector2.One * 2, 4, 4, DustID.BlueCrystalShard, 0f, 0f, 100, Color.Cyan, 1.2f); // Adjusted parameters
                    Main.dust[dustIndex].noGravity = true;
                    Main.dust[dustIndex].velocity *= 0.1f; // Slow down dust quickly
                    Main.dust[dustIndex].fadeIn = 1.3f; // Make dust fade in

                    // --- Optional: Damage from Dust ---
                    // This is generally performance-intensive and often not needed if the main beam damage works well.
                    // If you keep this, ensure proper cooldowns and damage calculation.

                    // Rectangle dustHitbox = new Rectangle((int)pos.X - 2, (int)pos.Y - 2, 4, 4);
                    // foreach (NPC npc in Main.npc)
                    // {
                    //     if (npc.active && !npc.friendly && npc.Hitbox.Intersects(dustHitbox))
                    //     {
                    //         if (!npcDustCooldowns.TryGetValue(npc.whoAmI, out int cooldown) || cooldown <= 0)
                    //         {
                    //             npcDustCooldowns[npc.whoAmI] = 60; // Cooldown in ticks (60 = 1 sec)

                    //             int dustDamage = Projectile.damage / 10; // Example: Dust does 10% of beam damage
                    //             if (dustDamage < 1) dustDamage = 1;

                    //             NPC.HitInfo hitInfo = new NPC.HitInfo();
                    //             hitInfo.Damage = dustDamage;
                    //             hitInfo.Knockback = 0f;
                    //             hitInfo.HitDirection = (npc.Center.X < pos.X) ? -1 : 1;
                    //             hitInfo.DamageType = Projectile.DamageType;

                    //             player.ApplyDamageToNPC(npc, dustDamage, 0f, hitInfo.HitDirection, false, Projectile.DamageType);
                    //             // Apply minimal immunity if using ApplyDamageToNPC
                    //             if(npc.immune[player.whoAmI] < 5) npc.immune[player.whoAmI] = 5;
                    //         }
                    //     }
                    // }

                }
            }
        }

        // Colliding - No changes needed generally, but ensure BeamWidth matches visual/intended hitbox
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float collisionPoint = 0f;
            Vector2 beamStart = Projectile.Center;
            // Recalculate beam end based on current velocity and length for accurate collision
            float beamLength = MaxBeamLength;
            Vector2 beamEnd = beamStart + Projectile.velocity * beamLength;
            float[] samples = new float[3];
            Collision.LaserScan(beamStart, Projectile.velocity, 0, MaxBeamLength, samples);
            beamLength = (samples[0] + samples[1] + samples[2]) / 3f;
            beamEnd = beamStart + Projectile.velocity * beamLength;

            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), beamStart, beamEnd, BeamWidth, ref collisionPoint);
        }

        // Drawing - No changes needed, but consider using custom textures for the beam
        public override bool PreDraw(ref Color lightColor) => false; // Keep hiding default draw

        public override void PostDraw(Color lightColor)
        {
            if (Projectile.timeLeft <= ActiveTime && Projectile.velocity != Vector2.Zero) // Only draw if firing
            {
                Texture2D texture = ModContent.Request<Texture2D>("Terraria/Images/Projectile_" + ProjectileID.ShadowBeamFriendly).Value; // Example texture
                // Texture2D texture = ModContent.Request<Texture2D>("YourMod/Assets/Projectiles/WakmehamehaBeam").Value; // Use your own texture!

                Vector2 beamStart = Projectile.Center;
                // Recalculate beam end for drawing consistency
                float beamLength = MaxBeamLength;
                Vector2 beamEnd = beamStart + Projectile.velocity * beamLength;
                float[] samples = new float[3];
                Collision.LaserScan(beamStart, Projectile.velocity, 0, MaxBeamLength, samples); // Use 0 width for scan here
                beamLength = (samples[0] + samples[1] + samples[2]) / 3f;
                // beamEnd = beamStart + Projectile.velocity * beamLength; // Not needed for drawing method below

                float scaleY = BeamWidth / texture.Height; // Scale based on desired width
                float scaleX = beamLength / texture.Width; // Scale based on calculated length

                Vector2 origin = new Vector2(0, texture.Height / 2f); // Draw from the start origin

                Color drawColor = Color.Lerp(Color.Cyan, Color.White, 0.7f) * 0.8f; // Example color tinting

                // Draw the beam texture stretched
                Main.spriteBatch.Draw(texture,
                                      beamStart - Main.screenPosition,
                                      null, // Source rectangle (null for whole texture)
                                      drawColor, // Beam color
                                      Projectile.velocity.ToRotation(), // Rotation
                                      origin, // Origin (start of the beam texture)
                                      new Vector2(scaleX, scaleY), // Scale (X stretches length, Y sets width)
                                      SpriteEffects.None,
                                      0f);
            }
        }
    }
}
