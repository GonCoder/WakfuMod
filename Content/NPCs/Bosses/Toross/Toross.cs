using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.GameContent.ItemDropRules;
using WakfuMod.Content.Items.Currency;
using WakfuMod.Content.Projectiles.Bosses.Toross;

namespace WakfuMod.Content.NPCs.Bosses.Toross
{
    [AutoloadBossHead]
    public class Toross : ModNPC
    {
        // Attack State Management
        private float AttackTimer { get => NPC.ai[3]; set => NPC.ai[3] = value; }
        private float AttackPhase { get => NPC.localAI[0]; set => NPC.localAI[0] = value; }

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Toross");
            Main.npcFrameCount[NPC.type] = 9;
            
            NPCID.Sets.MPAllowedEnemies[Type] = true;
            NPCID.Sets.BossBestiaryPriority.Add(Type);
            
            // Immunities
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
        }

        public override void SetDefaults()
        {
            NPC.width = 300; // Increased from 120 to account for wide sprite/cape
            NPC.height = 240; // Increased from 200
            NPC.damage = 60;
            NPC.defense = 25;
            NPC.lifeMax = 15000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.boss = true;
            NPC.aiStyle = -1; 
            NPC.value = Item.buyPrice(gold: 15);
            
            // Music
            NPC.boss = true;
            if (!Main.dedServ)
            {
                Music = MusicID.Boss2;
            }
        }

        public override void AI()
        {
            // --- Initialization ---
            if (NPC.ai[0] == 0)
            {
                NPC.TargetClosest(true);
                Player player = Main.player[NPC.target];
                if (!player.active || player.dead) return;

                float worldCenter = Main.maxTilesX * 16f / 2f;
                float spawnOffset = 900f; // Spawn distance from player

                // Determine direction: Move away from nearest beach (towards center/other side)
                if (player.Center.X < worldCenter)
                {
                    NPC.ai[1] = 1f; // Move Right
                    NPC.Center = new Vector2(player.Center.X - spawnOffset, player.Center.Y);
                }
                else
                {
                    NPC.ai[1] = -1f; // Move Left
                    NPC.Center = new Vector2(player.Center.X + spawnOffset, player.Center.Y);
                }
                
                NPC.ai[2] = 3f; // Base Speed
                NPC.ai[0] = 1; // Initialized
                AttackPhase = 0; // Start Phase
                AttackTimer = 0;
                NPC.netUpdate = true;
            }

            // --- Target & Despawn ---
            NPC.TargetClosest(false);
            Player target = Main.player[NPC.target];
            if (target.dead || !target.active)
            {
                NPC.TargetClosest(false);
                target = Main.player[NPC.target];
                if (target.dead || !target.active)
                {
                    NPC.velocity.Y -= 0.5f;
                    NPC.EncourageDespawn(10);
                    return;
                }
            }

            // --- Movement ---
            float speed = NPC.ai[2];
            // Enrage/Speed up as health drops
            float healthRatio = (float)NPC.life / NPC.lifeMax;
            float currentSpeed = speed + (1f - healthRatio) * 4f; 
            
            NPC.velocity.X = NPC.ai[1] * currentSpeed;
            
            // Set direction for sprite (Ensure it matches movement)
            NPC.direction = (int)NPC.ai[1];
            NPC.spriteDirection = -NPC.direction;

            // --- Obstacle Avoidance & Floating ---
            // 1. Check for ground below to maintain height (2 blocks = 32 pixels)
            float desiredDistFromGround = (NPC.height / 2f) + (2 * 16f); // Distance from Center to Ground
            float distToGround = 1000f;
            
            Point centerTile = NPC.Center.ToTileCoordinates();
            // Scan down
            for (int i = 0; i < 50; i++)
            {
                if (WorldGen.SolidTile(centerTile.X, centerTile.Y + i))
                {
                    distToGround = i * 16f; // Distance from center to ground tile
                    break;
                }
            }

            // 2. Check for wall ahead (8 blocks)
            bool wallAhead = false;
            int checkDir = (int)NPC.ai[1];
            float checkDist = 8 * 16f;
            
            Vector2 frontCheck = NPC.Center + new Vector2(checkDir * (NPC.width/2 + checkDist), 0);
            
            // Check a vertical strip ahead to detect walls (covering boss height)
            for (int y = -NPC.height/2; y < NPC.height/2; y += 16)
            {
                Point tilePos = (frontCheck + new Vector2(0, y)).ToTileCoordinates();
                if (WorldGen.SolidTile(tilePos.X, tilePos.Y))
                {
                    wallAhead = true;
                    break;
                }
            }

            float targetYVel = 0f;

            if (wallAhead)
            {
                // Fly up to clear wall
                targetYVel = -5f;
            }
            else
            {
                // Maintain altitude
                if (distToGround < desiredDistFromGround)
                {
                    // Too low (Too close to ground)
                    targetYVel = -3f;
                }
                else if (distToGround > desiredDistFromGround + 48f) // Buffer zone
                {
                    // Too high
                    targetYVel = 3f;
                }
                else
                {
                    // Good height
                    targetYVel = 0f;
                }
            }
            
            // Apply Y velocity smoothly
            NPC.velocity.Y = MathHelper.Lerp(NPC.velocity.Y, targetYVel, 0.1f);

            // --- Wall Logic ---
            ManageWall();

            // --- Attack Logic ---
            HandleAttacks();
        }

        private void HandleAttacks()
        {
            AttackTimer++;

            float healthRatio = (float)NPC.life / NPC.lifeMax;
            int extraProjectiles = (int)((1f - healthRatio) / 0.05f); // +1 projectile every 5% missing health
            
            // Spawn much higher above head (Adjusted to 450f)
            Vector2 spawnPos = NPC.Center - new Vector2(0, 490f); 

            // Phase 0: Initial Wait / Cooldown
            int phase0Time = (int)(60 + 60 * healthRatio); // 1-2 seconds
            if (AttackPhase == 0)
            {
                if (AttackTimer >= phase0Time) 
                {
                    AttackPhase = 1;
                    AttackTimer = 0;
                }
            }
            // Phase 1: Spawn Stasis Lasers
            else if (AttackPhase == 1)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    // Target current target
                    Player targetPlayer = Main.player[NPC.target];
                    
                    // Find active players
                    int playerCount = 0;
                    for (int i = 0; i < Main.maxPlayers; i++) if (Main.player[i].active && !Main.player[i].dead) playerCount++;
                    
                    if (playerCount > 0)
                    {
                        int totalLasers = 4 + extraProjectiles;
                        
                        for (int i = 0; i < totalLasers; i++)
                        {
                            // Calculate angle for this laser to prevent stacking
                            float angle = (MathHelper.TwoPi / totalLasers) * i;

                            Projectile.NewProjectile(
                                NPC.GetSource_FromAI(),
                                spawnPos,
                                Vector2.Zero,
                                ModContent.ProjectileType<TorossStasisLaser>(),
                                40, // Damage
                                0f,
                                Main.myPlayer,
                                NPC.whoAmI, // ai[0]: Boss
                                targetPlayer.whoAmI, // ai[1]: Target
                                angle // ai[2]: Angle (Radians)
                            );
                        }
                    }
                }
                
                AttackPhase = 2;
                AttackTimer = 0;
            }
            // Phase 2: Wait after Attack 1 
            // Laser Charge (2s/120t) + Cooldown (Scaled)
            int phase2Cooldown = (int)(240 * healthRatio); // 0-4 seconds cooldown
            if (AttackPhase == 2)
            {
                if (AttackTimer >= 120 + phase2Cooldown)
                {
                    AttackPhase = 3;
                    AttackTimer = 0;
                }
            }
            // Phase 3: Spawn Homing Projectiles
            else if (AttackPhase == 3)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    // Target current target
                    Player targetPlayer = Main.player[NPC.target];
                    int totalHoming = 2 + extraProjectiles;

                    for (int i = 0; i < totalHoming; i++)
                    {
                        // Spread homing projectiles in an arc above the boss
                        // Arc from -PI/4 to PI/4 (above head) or wider?
                        // Let's do a full circle or wide arc since there can be many
                        float angle = (MathHelper.TwoPi / totalHoming) * i;

                        Projectile.NewProjectile(
                            NPC.GetSource_FromAI(),
                            spawnPos,
                            Vector2.Zero,
                            ModContent.ProjectileType<TorossHomingProjectile>(),
                            35, // Damage
                            0f,
                            Main.myPlayer,
                            NPC.whoAmI, // ai[0]: Boss
                            targetPlayer.whoAmI, // ai[1]: Target
                            angle // ai[2]: Angle (Radians)
                        );
                    }
                }

                AttackPhase = 4;
                AttackTimer = 0;
            }
            // Phase 4: Wait after Attack 2
            // Homing Charge (3s/180t) + Cooldown (Scaled)
            int phase4Cooldown = (int)(600 * healthRatio); // 0-10 seconds cooldown
            if (AttackPhase == 4)
            {
                if (AttackTimer >= 180 + phase4Cooldown)
                {
                    AttackPhase = 1; // Loop back to Attack 1
                    AttackTimer = 0;
                }
            }
        }

        private void ManageWall()
        {
            // Wall is located behind the boss
            // Increased offset to account for larger hitbox (Width 300 / 2 = 150)
            float wallOffset = 200f; 
            float wallX = NPC.Center.X - (NPC.ai[1] * wallOffset);
            
            // Visual effects for the wall (Dust)
            if (Main.netMode != NetmodeID.Server)
            {
                // Create a vertical line of dust
                for (int i = 0; i < 10; i++)
                {
                    float dustY = NPC.Center.Y - 600 + Main.rand.NextFloat(1200f); // Range around boss Y
                    // Or cover screen height?
                    Vector2 dustPos = new Vector2(wallX, dustY);
                    // Only spawn if on screen?
                    if (Vector2.Distance(dustPos, Main.screenPosition + new Vector2(Main.screenWidth/2, Main.screenHeight/2)) < 1500)
                    {
                        Dust d = Dust.NewDustPerfect(dustPos, DustID.PinkTorch, new Vector2(0, -2f), 100, default, 2f);
                        d.noGravity = true;
                        d.velocity *= 0.5f;
                    }
                }
            }

            // Push players
            foreach (Player p in Main.player)
            {
                if (!p.active || p.dead) continue;

                bool crushed = false;

                if (NPC.ai[1] == 1f) // Moving Right -> Wall is on Left
                {
                    // If player is to the LEFT of the wall (crossed it)
                    if (p.Center.X < wallX)
                    {
                        p.position.X = wallX + p.width; // Snap to right side of wall
                        p.velocity.X = 15f; // Push Right
                        p.AddBuff(BuffID.TheTongue, 10); // Disable movement skills (WoF style)

                        // Check for World Edge (Right)
                        if (p.position.X >= (Main.maxTilesX * 16) - 320) // Close to right edge
                        {
                            crushed = true;
                        }
                    }
                }
                else // Moving Left -> Wall is on Right
                {
                    // If player is to the RIGHT of the wall (crossed it)
                    if (p.Center.X > wallX)
                    {
                        p.position.X = wallX - p.width * 2; // Snap to left side of wall
                        p.velocity.X = -15f; // Push Left
                        p.AddBuff(BuffID.TheTongue, 10);

                        // Check for World Edge (Left)
                        if (p.position.X <= 320) // Close to left edge
                        {
                            crushed = true;
                        }
                    }
                }
                if (crushed)
                {
                    p.KillMe(PlayerDeathReason.ByCustomReason(NetworkText.FromLiteral(p.name + " was crushed by the Stasis Wall!")), 999999, 0);
                    
                    // If player survived (Godmode/Invincibility), despawn boss and teleport player
                    // If player survived (Godmode/Invincibility), despawn boss and teleport player
                    if (!p.dead)
                    {
                        p.Spawn(PlayerSpawnContext.RecallFromItem);
                        NPC.active = false;
                        NPC.netUpdate = true;
                        return;
                    }
                }
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            // Draw the Pink Wall Visual
            float wallOffset = 200f;
            float wallX = NPC.Center.X - (NPC.ai[1] * wallOffset);
            
            // Draw a giant semi-transparent pink strip
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            
            // Calculate draw rectangle relative to screen
            float drawX = wallX - screenPos.X;
            
            // We draw a strip that covers the screen height
            Rectangle wallRect = new Rectangle(
                (int)(drawX - 10), // Centered on wallX, 20px wide
                0, 
                20, 
                Main.screenHeight
            );

            spriteBatch.Draw(pixel, wallRect, Color.HotPink * 0.4f);
            
            // Draw Boss Texture (Default behavior will draw it after this if we return true)
            return true;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            // Drop de 5 Kamas (100% de probabilidad)
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Kama>(), 1, 5, 5));
            // Drop de la Meowmere (Espada rosa)
            npcLoot.Add(ItemDropRule.Common(ItemID.Meowmere));
        }

        public override void FindFrame(int frameHeight)
        {
            // Velocidad de animación
            int ticksPerFrame = 5;
            // Hacer que la animación sea más rápida si el boss se mueve rápido (fase final)
            if (Math.Abs(NPC.velocity.X) > 5f) ticksPerFrame = 3;

            NPC.frameCounter++;
            if (NPC.frameCounter >= ticksPerFrame)
            {
                NPC.frameCounter = 0;
                NPC.frame.Y += frameHeight;
                if (NPC.frame.Y >= frameHeight * Main.npcFrameCount[NPC.type])
                {
                    NPC.frame.Y = 0;
                }
            }

            // Dirección del sprite basada en la dirección de movimiento (ai[1])
            // Forzamos la dirección aquí también por si acaso
            NPC.spriteDirection = -(int)NPC.ai[1];
        }
    }
}
