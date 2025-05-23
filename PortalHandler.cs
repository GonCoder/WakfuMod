using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System;
using WakfuMod.Content.Projectiles;
using Terraria.Audio;
using Terraria.ID; // Needed for Dust and Sound IDs

namespace WakfuMod
{
    public class PortalHandler : ModSystem
    {
        // Cooldowns remain the same
        private static readonly double Cooldown = 0.6;
        public static readonly double TeleportCooldown = 1.3;

        // Portal tracking variables remain the same
        public static Vector2? portal1 = null;
        public static Vector2? portal2 = null;
        private static bool isFirstPortal = true;
        public static int portal1ID = -1;
        public static int portal2ID = -1;
        private static double lastPortalTime = 0;

         // --- Constants for STANDARD Explosion (Manual Close) ---
        private const float StandardExplosionRadius = 100f;
        private const int StandardExplosionDamage = 5;
        private const float StandardExplosionKnockback = 4f;
         private static readonly SoundStyle StandardExplosionSound = new("WakfuMod/audio/openPortal") { Volume = 1.8f, Pitch = -0.2f };
        private const int StandardExplosionDustType1 = DustID.PortalBoltTrail;
        private const int StandardExplosionDustType2 = DustID.MagicMirror;
        private const int StandardExplosionDustCount = 60;

        // --- Constants for VIOLENT Explosion (Weapon Hit) ---
        private const float ViolentExplosionRadius = 280f; // Larger radius
        private const int ViolentExplosionBaseDamage = 10; // Higher base damage
        private const float ViolentExplosionKnockback = 10f; // Higher knockback
        // Example: Use a more impactful sound
        private static readonly SoundStyle ViolentExplosionSound = SoundID.Item62 with { Volume = 1.8f, Pitch = 1.4f }; // Grenade launcher sound
        private const int ViolentExplosionDustType1 = DustID.BlueCrystalShard; // Weapon themed
        private const int ViolentExplosionDustType2 = DustID.BlueFlare; // Add fiery/energetic effect
        private const int ViolentExplosionDustCount = 500; // More dust
         private const float DustVelocityScaleFactor = 0.12f; // Adjust this multiplier to control spread (higher = more spread)


        public static void TryPlacePortal(Player player)
        {
            if (!CanPlacePortal())
                return;

            Vector2 cursorPos = Main.MouseWorld;
            // FindValidPosition might need adjustment if portals should float or stick to walls?
            // For now, assumes it finds a ground/air position near cursor.
            Vector2 placementPos = FindValidPositionNearCursor(cursorPos); // Renamed for clarity

            // --- Check if replacing an existing portal and detonate it ---
            if (isFirstPortal)
            {
                if (portal1ID != -1 && Main.projectile[portal1ID] != null && Main.projectile[portal1ID].active)
                {
                    // Use standard detonation when replacing
                    DetonatePortal(portal1ID, player, StandardExplosionRadius, StandardExplosionDamage, StandardExplosionKnockback, StandardExplosionSound, StandardExplosionDustType1, StandardExplosionDustType2, StandardExplosionDustCount);
                    portal1ID = -1;
                    portal1 = null;
                }
                portal1 = placementPos;
                portal1ID = Projectile.NewProjectile(player.GetSource_FromThis("PortalPlacement"), placementPos, Vector2.Zero, ModContent.ProjectileType<PortalProjectile>(), 0, 0, player.whoAmI);
                SoundEngine.PlaySound(new SoundStyle("WakfuMod/audio/openPortal") { Volume = 1.5f, Pitch = 0.0f }, placementPos);
            }
            else
            {
                if (portal2ID != -1 && Main.projectile[portal2ID] != null && Main.projectile[portal2ID].active)
                {
                    // Use standard detonation when replacing
                    DetonatePortal(portal2ID, player, StandardExplosionRadius, StandardExplosionDamage, StandardExplosionKnockback, StandardExplosionSound, StandardExplosionDustType1, StandardExplosionDustType2, StandardExplosionDustCount);
                    portal2ID = -1;
                    portal2 = null;
                }
                portal2 = placementPos;
                portal2ID = Projectile.NewProjectile(player.GetSource_FromThis("PortalPlacement"), placementPos, Vector2.Zero, ModContent.ProjectileType<PortalProjectile>(), 0, 0, player.whoAmI);
                SoundEngine.PlaySound(new SoundStyle("WakfuMod/audio/openPortal") { Volume = 1.5f, Pitch = 0.9f }, placementPos);
            }

            isFirstPortal = !isFirstPortal;
            lastPortalTime = Main.gameTimeCache.TotalGameTime.TotalSeconds;
        }


         public static void ClosePortals(Player player)
        {
            bool portalClosed = false;
            if (portal1ID != -1 && Main.projectile[portal1ID] != null && Main.projectile[portal1ID].active)
            {
                DetonatePortal(portal1ID, player, StandardExplosionRadius, StandardExplosionDamage, StandardExplosionKnockback, StandardExplosionSound, StandardExplosionDustType1, StandardExplosionDustType2, StandardExplosionDustCount);
                portal1ID = -1;
                portal1 = null;
                portalClosed = true;
            }
            if (portal2ID != -1 && Main.projectile[portal2ID] != null && Main.projectile[portal2ID].active)
            {
                DetonatePortal(portal2ID, player, StandardExplosionRadius, StandardExplosionDamage, StandardExplosionKnockback, StandardExplosionSound, StandardExplosionDustType1, StandardExplosionDustType2, StandardExplosionDustCount);
                portal2ID = -1;
                portal2 = null;
                portalClosed = true;
            }

            if (portalClosed)
            {
               
            }
            isFirstPortal = true;
        }


          // --- NEW Method: Triggered ONLY by the Weapon hitting a portal ---
        // This method will detonate BOTH portals violently if they exist.
        public static void TriggerViolentPortalExplosion(Player owner)
        {
            bool exploded = false;
            // Use a single sound instance for the combined explosion
            SoundEngine.PlaySound(ViolentExplosionSound, owner.Center); // Play sound near player or average portal position

            if (portal1ID != -1 && Main.projectile[portal1ID] != null && Main.projectile[portal1ID].active)
            {
                // Call the DETONATOR function with VIOLENT parameters
                DetonatePortal(portal1ID, owner, ViolentExplosionRadius, ViolentExplosionBaseDamage, ViolentExplosionKnockback, null, // Sound already played
                               ViolentExplosionDustType1, ViolentExplosionDustType2, ViolentExplosionDustCount, true); // Add % life damage flag
                portal1ID = -1; // Clear ID
                portal1 = null; // Clear position
                exploded = true;
            }

            if (portal2ID != -1 && Main.projectile[portal2ID] != null && Main.projectile[portal2ID].active)
            {
                 // Call the DETONATOR function with VIOLENT parameters
                DetonatePortal(portal2ID, owner, ViolentExplosionRadius, ViolentExplosionBaseDamage, ViolentExplosionKnockback, null, // Sound already played
                               ViolentExplosionDustType1, ViolentExplosionDustType2, ViolentExplosionDustCount, true); // Add % life damage flag
                portal2ID = -1; // Clear ID
                portal2 = null; // Clear position
                exploded = true;
            }

            if (exploded)
            {
                 // Optional: Message for violent explosion
                 // Main.NewText("¡Resonancia de portal catastrófica!", Color.Cyan);
                 isFirstPortal = true; // Reset placement order
            }
        }
        // --- Helper Method to Detonate a Specific Portal ---
        private static void DetonatePortal(int portalProjectileIndex, Player owner,
                                           float explosionRadius, int baseDamage, float knockback, SoundStyle? sound,
                                           int dustType1, int dustType2, int dustCount, bool addLifePercentDamage = false)
        {
            if (portalProjectileIndex < 0 || portalProjectileIndex >= Main.maxProjectiles) return;
            Projectile portal = Main.projectile[portalProjectileIndex];
            if (portal == null || !portal.active || portal.type != ModContent.ProjectileType<PortalProjectile>()) return;

            Vector2 explosionPosition = portal.Center;

            // --- Visual Effects (Client-Side) ---
            if (Main.netMode != NetmodeID.Server)
            {
                // Play sound IF provided (avoid double sound in TriggerViolentPortalExplosion)
                if (sound.HasValue)
                {
                    SoundEngine.PlaySound(sound.Value, explosionPosition);
                }

                 float maxDustSpeed = explosionRadius * DustVelocityScaleFactor; // Higher radius = faster dust

                // Create dust particles
                for (int i = 0; i < dustCount; i++)
                {
                    int dustType = (i % 2 == 0) ? dustType1 : dustType2;
                     // Generate a random velocity with magnitude up to maxDustSpeed
                    Vector2 dustVelocity = Main.rand.NextVector2Circular(maxDustSpeed, maxDustSpeed);
                    Dust dust = Dust.NewDustPerfect(
                        explosionPosition,    // Spawn at center
                        dustType,             // Use the correct dust type
                        dustVelocity,         // Apply the calculated velocity for spread control <--- THIS IS THE KEY CHANGE
                        100,                  // Alpha (starting transparency)
                        default,       // Color override (none)
                        1.8f                  // Scale (adjust dust size if needed)
                    );
                    dust.noGravity = true;    // Make dust ignore gravity
                    // Optional: Adjust fade time, etc.
                    // dust.fadeIn = 0.5f;
                }
            }

            // --- Damage Logic (Server/Singleplayer authoritative) ---
            float radiusSq = explosionRadius * explosionRadius;
            foreach (NPC npc in Main.npc)
            {
                if (npc.active && !npc.friendly && npc.CanBeChasedBy(portal) && !npc.dontTakeDamage)
                {
                    if (Vector2.DistanceSquared(npc.Center, explosionPosition) <= radiusSq)
                    {
                        int finalDamage = baseDamage;
                        // Apply owner's damage modifiers (Using Magic for portals, adjust if needed)
                        finalDamage = (int)owner.GetTotalDamage(DamageClass.Ranged).ApplyTo(finalDamage);

                        // Add % max life damage if flagged (for violent explosion)
                        if (addLifePercentDamage)
                        {
                            finalDamage += (int)(npc.lifeMax * 0.09f); // 10% max life bonus damage
                        }

                        int direction = Math.Sign(npc.Center.X - explosionPosition.X);
                        if (direction == 0) direction = 1;

                        owner.ApplyDamageToNPC(npc, finalDamage, knockback, direction, false); // Let ApplyDamageToNPC handle DamageClass now
                    }
                }
            }

            // --- Kill the Portal Projectile ---
            portal.Kill();
        }


        private static bool CanPlacePortal()
        {
            // Cooldown check remains the same
            return Main.gameTimeCache.TotalGameTime.TotalSeconds - lastPortalTime >= Cooldown;
        }

        // --- Adjusted Position Finding ---
        // Tries to place near cursor, slightly prioritizing open space
        private static Vector2 FindValidPositionNearCursor(Vector2 cursorPosition)
        {
            Point tileCoords = cursorPosition.ToTileCoordinates();

            // Basic bounds check
            tileCoords.X = Math.Clamp(tileCoords.X, 10, Main.maxTilesX - 10);
            tileCoords.Y = Math.Clamp(tileCoords.Y, 10, Main.maxTilesY - 10);

            // Check if the direct cursor position is inside a solid block
            if (Main.tile[tileCoords.X, tileCoords.Y].HasTile && Main.tileSolid[Main.tile[tileCoords.X, tileCoords.Y].TileType])
            {
                // If inside a block, try moving up until an empty space is found (max 10 tiles)
                for (int i = 1; i <= 10; i++)
                {
                    if (tileCoords.Y - i > 10) // Ensure we don't go out of bounds upwards
                    {
                        if (!Main.tile[tileCoords.X, tileCoords.Y - i].HasTile || !Main.tileSolid[Main.tile[tileCoords.X, tileCoords.Y - i].TileType])
                        {
                            tileCoords.Y -= i;
                            break; // Found an open spot above
                        }
                    } else {
                        // If we hit the top boundary check, just use the cursor position (it might be slightly embedded)
                        break;
                    }
                }
            }
            // TODO: Could add more sophisticated logic here to find the nearest valid edge or air block if needed.
            // For now, this just returns the tile position (converted back to world coords), potentially shifted up if initially inside a solid block.
            // It will place the portal centered on the chosen tile coordinates.
            return tileCoords.ToWorldCoordinates(8f, 8f); // Center on the tile
        }


        // --- Reset System on World Exit/Load ---
        public override void OnWorldUnload()
        {
            portal1 = null;
            portal2 = null;
            portal1ID = -1;
            portal2ID = -1;
            isFirstPortal = true;
            lastPortalTime = 0;
        }
    }
}