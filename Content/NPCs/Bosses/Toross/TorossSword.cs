using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace WakfuMod.Content.NPCs.Bosses.Toross
{
    public class TorossSword : ModNPC
    {
        public override string Texture => "WakfuMod/Content/NPCs/Bosses/Toross/Toross_Sword";

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Toross Sword");
            Main.npcFrameCount[NPC.type] = 1;
            
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
        }

        public override void SetDefaults()
        {
            NPC.width = 70;
            NPC.height = 70; 
            NPC.scale = 3f; // Visual scale
            NPC.damage = 80; // High damage
            NPC.defense = 9999;
            NPC.lifeMax = 1000;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath14;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.knockBackResist = 0f;
            NPC.aiStyle = -1;
            NPC.dontTakeDamage = true; // Invincible
        }

        public override void AI()
        {
            // Check if Toross is alive
            if (!NPC.AnyNPCs(ModContent.NPCType<Toross>()))
            {
                NPC.active = false;
                NPC.netUpdate = true;
                return;
            }

            // Target the same player as the main boss if possible, or closest
            if (NPC.target < 0 || NPC.target == 255 || Main.player[NPC.target].dead || !Main.player[NPC.target].active)
            {
                NPC.TargetClosest(true);
            }
            
            Player player = Main.player[NPC.target];
            if (player.dead)
            {
                NPC.velocity.Y -= 0.1f;
                NPC.EncourageDespawn(10);
                return;
            }

            // AI State Machine
            // ai[0]: State (0 = Hover/Position, 1 = Slash/Dash)
            // ai[1]: Timer
            // ai[2]: Side (-1 = Left, 1 = Right)

            if (NPC.ai[2] == 0) NPC.ai[2] = 1; // Default to Right side

            float hoverDistX = 600f;
            float hoverDistY = -200f;
            
            if (NPC.ai[0] == 0) // State 0: Positioning / Hover
            {
                Vector2 targetPos = player.Center + new Vector2(NPC.ai[2] * hoverDistX, hoverDistY);
                Vector2 moveDir = targetPos - NPC.Center;
                float dist = moveDir.Length();
                
                float speed = 15f; // Reduced from 25f
                float inertia = 20f; // Increased inertia for smoother movement
                
                moveDir.Normalize();
                moveDir *= speed;
                
                NPC.velocity = (NPC.velocity * (inertia - 1) + moveDir) / inertia;

                // Rotation: Point towards player
                Vector2 lookDir = player.Center - NPC.Center;
                NPC.rotation = lookDir.ToRotation() + MathHelper.PiOver4;

                // Check if reached position
                if (dist < 100f)
                {
                    NPC.ai[1]++;
                    if (NPC.ai[1] > 20) // Wait a bit before slashing
                    {
                        NPC.ai[0] = 1; // Switch to Slash
                        NPC.ai[1] = 0;
                        // Play charge sound?
                        SoundEngine.PlaySound(SoundID.Item71, NPC.Center);
                    }
                }
            }
            else if (NPC.ai[0] == 1) // State 1: Slash / Dash
            {
                // Dash towards the OTHER side
                Vector2 targetPos = player.Center + new Vector2(-NPC.ai[2] * hoverDistX, hoverDistY + 100f);
                Vector2 moveDir = targetPos - NPC.Center;
                float dist = moveDir.Length();

                // Accelerate
                float speed = 25f; // Reduced from 45f
                float inertia = 10f; // Slightly increased inertia

                moveDir.Normalize();
                moveDir *= speed;

                NPC.velocity = (NPC.velocity * (inertia - 1) + moveDir) / inertia;
                
                // Rotation: Follow velocity
                NPC.rotation = NPC.velocity.ToRotation() + MathHelper.PiOver4;

                // Check if reached other side
                if (dist < 150f || (NPC.ai[2] == 1 && NPC.Center.X < player.Center.X - 200) || (NPC.ai[2] == -1 && NPC.Center.X > player.Center.X + 200))
                {
                    NPC.ai[0] = 0; // Back to Hover
                    NPC.ai[2] *= -1; // Switch Side
                    NPC.ai[1] = 0;
                }
            }

            // Visuals: Trail
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.PinkTorch, NPC.velocity.X * 0.2f, NPC.velocity.Y * 0.2f);
                d.noGravity = true;
                d.scale = 2f;
            }
            
            // Keep alive as long as boss exists (handled by CheckActive, but timeLeft needs maintenance)
            NPC.timeLeft = 10;
        }
        
        public override bool CheckActive()
        {
            // Despawn if Toross is gone
            if (!NPC.AnyNPCs(ModContent.NPCType<Toross>()))
            {
                return true;
            }
            return false;
        }
    }
}
