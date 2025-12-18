using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System;
using WakfuMod.Common.GlobalNPCs; // Import logic namespace
using WakfuMod.Content.Buffs;    // Import buffs namespace
using System.Collections.Generic; // For List

namespace WakfuMod.Content.Projectiles
{
    public class OcraArrow : ModProjectile
    {
        // Track hit targets for smart chaining
        private List<int> hitTargets = new List<int>();
        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            if (Projectile.owner >= 0 && Main.player[Projectile.owner].active)
            {
                Player p = Main.player[Projectile.owner];
                // Penetration: 1 + Max Minions + (Ranged Damage Bonus % / 10)
                // Example: +50% ranged dmg (0.5) -> +5 penetrate
                float rangedBonus = p.GetTotalDamage(DamageClass.Ranged).Additive - 1f; 
                if (rangedBonus < 0) rangedBonus = 0;
                
                int bonusPenetrate = (int)(rangedBonus * 10f);
                Projectile.penetrate = 1 + p.maxMinions + bonusPenetrate;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // --- 1. Armor Shred (-10, Max -150) ---
            if (target.TryGetGlobalNPC<WakfuGlobalNPC>(out var globalNPC))
            {
                if (globalNPC.ocraDefenseReduction < 150)
                {
                    int amount = 10;
                    // Cap check
                    if (globalNPC.ocraDefenseReduction + amount > 150)
                    {
                        amount = 150 - globalNPC.ocraDefenseReduction;
                    }
                    
                    if (amount > 0)
                    {
                        target.defense -= amount;
                        if (target.defense < 0) target.defense = 0;
                        
                        globalNPC.ocraDefenseReduction += amount;
                        
                        // Sync
                        if (Main.netMode != NetmodeID.SinglePlayer)
                        {
                            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, target.whoAmI);
                        }
                    }
                }
            }

            // --- 2. Precision Buff (Stacks up to 10, +5% Ranged Dmg) ---
            if (Projectile.owner >= 0)
            {
                Player player = Main.player[Projectile.owner];
                if (player.active && !player.dead)
                {
                    // Add/Refresh Buff (20 seconds)
                    player.AddBuff(ModContent.BuffType<PrecisionBuff>(), 1200);
                    
                    // Increment Stacks
                    var modPlayer = player.GetModPlayer<jugador.WakfuPlayer>();
                    if (modPlayer.precisionStacks < 10)
                    {
                        modPlayer.precisionStacks++;
                    }
                }
            }
            
            // Add to hit list for smart chaining
            if (!hitTargets.Contains(target.whoAmI))
            {
                hitTargets.Add(target.whoAmI);
            }
            
            // ACCELERATION BOOST: Speed up on hit to find next target or pass through
            // Re-added per request: +30% boost
            Projectile.velocity *= 1.3f; 
            
            // Smart Pulse: Ignore THIS target for 0.2s to prevent sticking
            ignoreTimer = 12; // 0.2s (12 ticks) ignore THIS target
            lastHitTargetWhoAmI = target.whoAmI;
        }

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 2; // 2 frames animation
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 600;
            Projectile.tileCollide = false; // Pass through blocks
            Projectile.arrow = true;
            Projectile.aiStyle = 1; // Standard arrow style initially
            
            // Fix double-hit issue
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20; // 1/3 second immunity per enemy per arrow
        }

        // Track boost timer
        private int ignoreTimer = 0;
        private int lastHitTargetWhoAmI = -1;

        public override void AI()
        {
            // --- Animación (2 Frames) ---
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 5) // Velocidad de animación
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
                if (Projectile.frame >= 2)
                {
                    Projectile.frame = 0;
                }
            }

            // Custom Homing Logic
            // aiStyle 1 handles gravity/rotation, but we want homing
            // We might overwrite velocity modifications

            float homingRange = 400f;
            float homingSpeed = 12f;
            float homingInertia = 20f;
            
            // --- TARGET RE-SEEK LOGIC ---
            // Simply decrease timer that prevents locking back onto the same target immediately
            if (ignoreTimer > 0)
            {
                ignoreTimer--;
            }

            Vector2 targetCenter = Vector2.Zero;
            bool foundTarget = false;

            // 1. Priority: OcraBeacon
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.type == ModContent.ProjectileType<OcraBeacon>() && p.owner == Projectile.owner)
                {
                    float dist = Vector2.Distance(Projectile.Center, p.Center);
                    if (dist < homingRange)
                    {
                        targetCenter = p.Center;
                        foundTarget = true;
                        homingRange = dist; // Closest beacon
                    }
                }
            }

            // 2. Secondary: NPCs (if no beacon found or maybe prioritize beacon?)
            // If beacon found, we home to it. If not, look for NPCs.
            if (!foundTarget)
            {
                NPC closestNPC = FindClosestNPC(homingRange);
                if (closestNPC != null)
                {
                    targetCenter = closestNPC.Center;
                    foundTarget = true;
                }
            }

            if (foundTarget)
            {
                Projectile.aiStyle = -1; // Disable vanilla arrow AI to control movement exactly
                Vector2 direction = (targetCenter - Projectile.Center).SafeNormalize(Vector2.Zero);
                Projectile.velocity = (Projectile.velocity * (homingInertia - 1) + direction * homingSpeed) / homingInertia;
                
                // Rotación basada en velocidad (la animación es vertical, asumimos que el sprite mira hacia arriba o derecha por defecto)
                // Si la animación es una bola de energía o efecto que no debe rotar, comenta esta línea.
                // Si es una flecha girando, `Projectile.rotation` podría interferir con `Projectile.frame` si el sheet es pre-rotado??
                // Asumiremos que debe rotar hacia donde va.
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.Pi;
            }
            else
            {
                 Projectile.aiStyle = 1; // Revert to arrow physics if no target
                 Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.Pi;
            }
        }

        private NPC FindClosestNPC(float maxRange)
        {
            NPC closest = null;
            float minDist = maxRange;
            
            // 1. First pass: Find closest NPC that is NOT in hitTargets
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.CanBeChasedBy(Projectile) && !hitTargets.Contains(npc.whoAmI))
                {
                    float dist = Vector2.Distance(Projectile.Center, npc.Center);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        closest = npc;
                    }
                }
            }
            
            // 2. Second pass: If no new target found, find any closest NPC (fallback)
            if (closest == null)
            {
                minDist = maxRange;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];
                    
                    // If ignoring last target temporarily (0.1s), skip it in fallback too
                    if (ignoreTimer > 0 && npc.whoAmI == lastHitTargetWhoAmI) continue;

                    if (npc.CanBeChasedBy(Projectile))
                    {
                        float dist = Vector2.Distance(Projectile.Center, npc.Center);
                        if (dist < minDist)
                        {
                            minDist = dist;
                            closest = npc;
                        }
                    }
                }
            }

            return closest;
        }

        // Removed redundant OnKill and CheckBeaconCollision logic as it is now handled in PreAI

        public override bool PreAI()
        {
            // Check collision with Beacon every frame to trigger explosion BEFORE hitting tiles/enemies if they overlap
            // Or prioritize Beacon?
             for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.type == ModContent.ProjectileType<OcraBeacon>() && p.owner == Projectile.owner)
                {
                    if (Projectile.Hitbox.Intersects(p.Hitbox))
                    {
                         // Explosion Logic
                         // Calculate damage: 80 base * Ranged multi
                         int damage = 80;
                         if (Main.player[Projectile.owner].active)
                         {
                             damage = (int)Main.player[Projectile.owner].GetDamage(DamageClass.Ranged).ApplyTo(80);
                         }

                         // SCALING BONUS: +100% damage per enemy penetrated (hit count)
                         int penetratedCount = hitTargets.Count;
                         if (penetratedCount > 0)
                         {
                             float bonusMultiplier = 1f + (penetratedCount * 1.1f);
                             damage = (int)(damage * bonusMultiplier);
                         }

                         // Spawn Explosion Projectile
                         Projectile.NewProjectile(Projectile.GetSource_FromThis(), p.Center, Vector2.Zero, ModContent.ProjectileType<OcraExplosion>(), damage, 0f, Projectile.owner);

                         // Kill Beacon
                         p.Kill();
                         
                         // Kill Arrow
                         Projectile.Kill();
                         return false; 
                    }
                    break; // Stop checking
                }
            }
            return true;
        }

    }
}
