using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System;
using WakfuMod.Content.Projectiles;
using Terraria.Audio;
using Terraria.ID; // Needed for Dust and Sound IDs
using WakfuMod.jugador;

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
                    DetonatePortal(Main.projectile[portal1ID], player, StandardExplosionRadius, StandardExplosionDamage, StandardExplosionKnockback, StandardExplosionSound, StandardExplosionDustType1, StandardExplosionDustType2, StandardExplosionDustCount);
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
                    DetonatePortal(Main.projectile[portal2ID], player, StandardExplosionRadius, StandardExplosionDamage, StandardExplosionKnockback, StandardExplosionSound, StandardExplosionDustType1, StandardExplosionDustType2, StandardExplosionDustCount);
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
            if (Main.netMode == NetmodeID.MultiplayerClient && player.whoAmI == Main.myPlayer)
            {
                ModPacket packet = ModContent.GetInstance<WakfuMod>().GetPacket();
                packet.Write((byte)WakfuMod.MessageType.ClosePortals);
                packet.Write((byte)player.whoAmI);
                packet.Send();
            }

            // Iterate through all projectiles to find portals owned by the player
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.type == ModContent.ProjectileType<PortalProjectile>() && p.owner == player.whoAmI)
                if (p.active && p.type == ModContent.ProjectileType<PortalProjectile>() && p.owner == player.whoAmI)
                {
                    DetonatePortal(p, player, StandardExplosionRadius, StandardExplosionDamage, StandardExplosionKnockback, StandardExplosionSound, StandardExplosionDustType1, StandardExplosionDustType2, StandardExplosionDustCount);
                }
            }
            if (player.whoAmI == Main.myPlayer)
            {
                portal1ID = -1;
                portal1 = null;
                portal2ID = -1;
                portal2 = null;
                isFirstPortal = true;
            }
        }


          // --- NEW Method: Triggered ONLY by the Weapon hitting a portal ---
        // This method will detonate BOTH portals violently if they exist.
        public static void TriggerViolentPortalExplosion(Player owner)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient && owner.whoAmI == Main.myPlayer)
            {
                ModPacket packet = ModContent.GetInstance<WakfuMod>().GetPacket();
                packet.Write((byte)WakfuMod.MessageType.RequestPortalExplosion);
                packet.Write((byte)owner.whoAmI);
                packet.Send();
            }

            bool exploded = false;
            // Use a single sound instance for the combined explosion
            SoundEngine.PlaySound(ViolentExplosionSound, owner.Center); // Play sound near player or average portal position

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.type == ModContent.ProjectileType<PortalProjectile>() && p.owner == owner.whoAmI)
                {
                    DetonatePortal(p, owner, ViolentExplosionRadius, ViolentExplosionBaseDamage, ViolentExplosionKnockback, null,
                                   ViolentExplosionDustType1, ViolentExplosionDustType2, ViolentExplosionDustCount, true);
                    exploded = true;
                }
            }

            if (exploded && owner.whoAmI == Main.myPlayer)
            {
                 // Optional: Message for violent explosion
                 // Main.NewText("¡Resonancia de portal catastrófica!", Color.Cyan);
                 isFirstPortal = true; // Reset placement order
                 portal1ID = -1; portal1 = null;
                 portal2ID = -1; portal2 = null;
            }
        }
        // --- Helper Method to Detonate a Specific Portal ---
        private static void DetonatePortal(Projectile portal, Player owner,
                                           float explosionRadius, int baseDamage, float knockback, SoundStyle? sound,
                                           int dustType1, int dustType2, int dustCount, bool addLifePercentDamage = false)
        {
            if (portal == null || !portal.active || portal.type != ModContent.ProjectileType<PortalProjectile>()) return;

            Vector2 explosionPosition = portal.Center;
            bool isViolent = addLifePercentDamage; // Usamos esto como flag para saber si es violenta

            // --- Visual Effects (Client-Side) ---
            if (Main.netMode != NetmodeID.Server)
            {
                SpawnExplosionFX(explosionPosition, isViolent);
            }
            else // Server Side
            {
                 // Enviar paquete de efectos a los clientes (EXCLUYENDO al dueño, que ya lo ejecutó localmente)
                 ModPacket packet = ModContent.GetInstance<WakfuMod>().GetPacket();
                 packet.Write((byte)WakfuMod.MessageType.PortalExplosionFX);
                 packet.WriteVector2(explosionPosition);
                 packet.Write(isViolent);
                 packet.Send(-1, owner.whoAmI);
            }

            // --- Damage Logic (Server/Singleplayer authoritative) ---
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                float radiusSq = explosionRadius * explosionRadius;
                foreach (NPC npc in Main.npc)
                {
                    if (npc.active && !npc.friendly && npc.CanBeChasedBy(portal) && !npc.dontTakeDamage)
                    {
                        if (Vector2.DistanceSquared(npc.Center, explosionPosition) <= radiusSq)
                        {
                            int finalDamage = baseDamage;
                            global::WakfuMod.jugador.WakfuPlayer wakfuPlayer = owner.GetModPlayer<global::WakfuMod.jugador.WakfuPlayer>();

                            if (wakfuPlayer.BalanceMode)
                            {
                                // --- MODO BALANCEADO (Verde) ---
                                if (addLifePercentDamage) // Explosión Violenta (Arma)
                                {
                                    finalDamage = 50;
                                }
                                else // Explosión Estándar (Manual)
                                {
                                    finalDamage = 20;
                                }
                                // Escalar con daño a distancia
                                finalDamage = (int)owner.GetTotalDamage(DamageClass.Ranged).ApplyTo(finalDamage);
                            }
                            else
                            {
                                // --- MODO ORIGINAL (Rojo) ---
                                // Apply owner's damage modifiers (Using Magic for portals, adjust if needed)
                                finalDamage = (int)owner.GetTotalDamage(DamageClass.Ranged).ApplyTo(finalDamage);

                                // Add % max life damage if flagged (for violent explosion)
                                if (addLifePercentDamage)
                                {
                                    finalDamage += (int)(npc.lifeMax * 0.09f); // 10% max life bonus damage
                                }
                            }

                            int direction = Math.Sign(npc.Center.X - explosionPosition.X);
                            if (direction == 0) direction = 1;

                            owner.ApplyDamageToNPC(npc, finalDamage, knockback, direction, false); // Let ApplyDamageToNPC handle DamageClass now
                        }
                    }
                }
            }

            // --- Kill the Portal Projectile ---
            portal.Kill();
        }

        public static void SpawnExplosionFX(Vector2 position, bool violent)
        {
            float radius = violent ? ViolentExplosionRadius : StandardExplosionRadius;
            int dustType1 = violent ? ViolentExplosionDustType1 : StandardExplosionDustType1;
            int dustType2 = violent ? ViolentExplosionDustType2 : StandardExplosionDustType2;
            int dustCount = violent ? ViolentExplosionDustCount : StandardExplosionDustCount;

            // Sonido (Solo para explosión estándar, la violenta se maneja externamente o ya sonó)
            if (!violent)
            {
                SoundEngine.PlaySound(StandardExplosionSound, position);
            }

            float maxDustSpeed = radius * DustVelocityScaleFactor;

            for (int i = 0; i < dustCount; i++)
            {
                int dustType = (i % 2 == 0) ? dustType1 : dustType2;
                Vector2 dustVelocity = Main.rand.NextVector2Circular(maxDustSpeed, maxDustSpeed);
                Dust dust = Dust.NewDustPerfect(
                    position,
                    dustType,
                    dustVelocity,
                    100,
                    default,
                    1.8f
                );
                dust.noGravity = true;
            }
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