using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace WakfuMod.Content.Projectiles
{
    public class RufusMinion : ModProjectile
    {
        private const int FRAME_WALK_START = 0;
        private const int FRAME_WALK_END = 1;
        private const int FRAME_IDLE_START = 2;
        private const int FRAME_IDLE_END = 15;
        private const int FRAME_ATTACK_START = 16;
        private const int FRAME_ATTACK_END = 17;

        private enum State { Idle, Chasing, Attacking }
        private State CurrentState = State.Idle;
        
        private int attackTimer = 0;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 18;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
            Main.projPet[Projectile.type] = true;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.CultistIsResistantTo[Projectile.type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 80;
            Projectile.height = 80;
            Projectile.tileCollide = true;
            Projectile.friendly = true;
            Projectile.minion = true;
            Projectile.minionSlots = 1f;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.Magic;
            // Attack Speed Control (0.65s = ~39 ticks)
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 39;
        }

        public override bool? CanCutTiles() => false;
        public override bool MinionContactDamage() => true;

        public override void AI()
        {
            // Visual Offset (Down 25px)
            Projectile.gfxOffY = 25f;
            
            Player player = Main.player[Projectile.owner];

            #region Active Check
            if (player.dead || !player.active)
            {
                player.ClearBuff(ModContent.BuffType<Content.Buffs.RufusBuff>());
            }
            if (player.HasBuff(ModContent.BuffType<Content.Buffs.RufusBuff>()))
            {
                Projectile.timeLeft = 2;
            }
            #endregion

            #region Scaling Logic
            // Base: 10
            // +5 per 20 Max HP (added HP only, so MaxHP - 100)
            // +20 per 10% Magic Bonus
            
            int baseDamage = 10;
            
            // HP Scaling (-100 to ignore base HP)
            int validHP = Math.Max(0, player.statLifeMax2 - 100);
            int hpSegments = validHP / 20;
            int hpBonus = hpSegments * 5;
            
            // Magic Scaling
            float magicFactor = player.GetDamage(DamageClass.Magic).Additive; 
            int magicSegments = 0;
            if (magicFactor > 1f)
            {
                magicSegments = (int)((magicFactor - 1f) / 0.10f); // Floor value
            }
            int magicBonus = magicSegments * 20;
            
            Projectile.damage = baseDamage + hpBonus + magicBonus;
            #endregion

            #region General Movement & Target
            float viewDist = 700f;
            float chaseDist = 200f;
            float shootDist = 40f; 
            
            NPC target = null;
            
             if (player.HasMinionAttackTargetNPC)
            {
                target = Main.npc[player.MinionAttackTargetNPC];
            }
            else
            {
                float closestDist = viewDist;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];
                    if (npc.CanBeChasedBy(Projectile) && Vector2.Distance(Projectile.Center, npc.Center) < closestDist)
                    {
                        closestDist = Vector2.Distance(Projectile.Center, npc.Center);
                        target = npc;
                    }
                }
            }
            #endregion

            #region Behavior (Grounded Combat)
            // Apply Gravity
            if (Projectile.velocity.Y < 16f)
            {
                Projectile.velocity.Y += 0.5f;
            }
            
            float speed = 6f;
            float inertia = 20f;
            float runSpeed = 8f;

            if (target != null)
            {
                 float distToTarget = Vector2.Distance(Projectile.Center, target.Center);
                 
                 // Movement
                 if (distToTarget > shootDist)
                 {
                     CurrentState = State.Chasing;
                     // Only affect X for walking
                     float dir = 0;
                     if (target.Center.X > Projectile.Center.X) dir = 1;
                     if (target.Center.X < Projectile.Center.X) dir = -1;
                     
                     // Run towards target
                     Projectile.velocity.X = (Projectile.velocity.X * (inertia - 1) + dir * runSpeed) / inertia;
                     
                     // Robust Wall Slide (Combat)
                     bool wallAhead = false;
                     
                     if (Math.Abs(Projectile.velocity.X) < 1f && Math.Abs(dir) > 0.1f) wallAhead = true;
                     
                     Vector2 checkOffset = new Vector2(Projectile.width / 2 + 10, 0) * dir;
                     Vector2 checkPos = Projectile.Center + checkOffset - new Vector2(16, 32);
                     
                     if (Collision.SolidTiles(checkPos, 32, 64)) wallAhead = true;

                     if (target.Center.Y < Projectile.Center.Y - 50) wallAhead = true;

                     if (wallAhead)
                     {
                          // Slide Up
                          if (Projectile.velocity.Y > -7f)
                          {
                              Projectile.velocity.Y = -9f;
                          }
                          Projectile.velocity.X = dir * 1.5f;
                     }
                 }
                 else
                 {
                     // Close enough to attack
                     // "Meta dentro del enemigo": Force position overlap or minimal distance
                     // To avoid physics pushing him out, we can reduce velocity friction or nudge him closer.
                     CurrentState = State.Attacking;
                     
                     // Constant nudge to stay "inside" horizontally
                     float dir = 0;
                     if (target.Center.X > Projectile.Center.X) dir = 1;
                     if (target.Center.X < Projectile.Center.X) dir = -1;
                     
                     // Keep moving towards center even while attacking, but slower
                     Projectile.velocity.X = (Projectile.velocity.X * (inertia - 1) + dir * 2f) / inertia;
                     
                     // If really close, stop X to punch
                     if (distToTarget < 20f) Projectile.velocity.X *= 0.8f;
                 }
            }
            else
            {
                // Idle around player
                CurrentState = State.Idle;
                
                // Return to player if far
                if (Vector2.Distance(Projectile.Center, player.Center) > 2000f)
                {
                    Projectile.Center = player.Center;
                }
                
                Vector2 directionToPlayer = player.Center - Projectile.Center;
                 if (Math.Abs(directionToPlayer.X) > 60f) // Deadzone
                 {
                     // Walk to player
                     float dir = directionToPlayer.X > 0 ? 1 : -1;
                     Projectile.velocity.X = (Projectile.velocity.X * (inertia - 1) + dir * speed) / inertia;
                     
                     // Robust Jump Logic (Idle/Follow)
                     bool jump = false;
                     
                     // Robust Wall Slide / Jump Logic (Idle/Follow)
                     bool wallAhead = false;
                     
                     // 1. Stuck Check
                     if (Math.Abs(Projectile.velocity.X) < 0.5f) wallAhead = true;

                     // 2. Wall Detection
                     Vector2 checkOffset = new Vector2(Projectile.width / 2 + 10, 0) * dir;
                     Vector2 checkPos = Projectile.Center + checkOffset - new Vector2(16, 32); 
                     
                     if (Collision.SolidTiles(checkPos, 32, 64)) wallAhead = true;

                     // Wall Slide Execution
                     if ((wallAhead || player.Center.Y < Projectile.Center.Y - 50) && Projectile.position.Y > player.position.Y + 40)
                     {
                         // Continuous Wall Slide
                         // If we are falling or staying still Y-wise, boost UP.
                         if (Projectile.velocity.Y > -6f)
                         {
                             Projectile.velocity.Y = -8f; // Slide speed
                         }
                         
                         // Slight nudge to keep contact for sliding or push over lip
                         Projectile.velocity.X = dir * 1.5f;
                     }
                 }
                 else
                 {
                     Projectile.velocity.X *= 0.9f;
                 }
            }
            
            // Sprite direction (Flipped)
            if (Projectile.velocity.X > 0.1f) Projectile.spriteDirection = 1;
            if (Projectile.velocity.X < -0.1f) Projectile.spriteDirection = -1;
            
            #endregion

            #region Animation
            Projectile.frameCounter++;
            

            
            if (CurrentState == State.Attacking)
            {
                 // Punch animation (Frames 16-17)
                 if (Projectile.frame < FRAME_ATTACK_START || Projectile.frame > FRAME_ATTACK_END)
                 {
                     Projectile.frame = FRAME_ATTACK_START;
                     Projectile.frameCounter = 0;
                 }
                 
                 if (Projectile.frameCounter > 5)
                 {
                     Projectile.frame++;
                     Projectile.frameCounter = 0;
                     if (Projectile.frame > FRAME_ATTACK_END)
                     {
                         Projectile.frame = FRAME_ATTACK_START;
                     }
                 }
            }
            else if (Math.Abs(Projectile.velocity.X) > 0.1f) // Walking
            {
                if (Projectile.frame < FRAME_WALK_START || Projectile.frame > FRAME_WALK_END)
                 {
                     Projectile.frame = FRAME_WALK_START;
                     Projectile.frameCounter = 0;
                 }
                 
                 if (Projectile.frameCounter > 8) // Walk speed
                 {
                     Projectile.frame++;
                     Projectile.frameCounter = 0;
                     if (Projectile.frame > FRAME_WALK_END)
                     {
                         Projectile.frame = FRAME_WALK_START;
                     }
                 }
            }
            else // Idle
            {
                if (Projectile.frame < FRAME_IDLE_START || Projectile.frame > FRAME_IDLE_END)
                 {
                     Projectile.frame = FRAME_IDLE_START;
                     Projectile.frameCounter = 0;
                 }
                 
                 if (Projectile.frameCounter > 6)
                 {
                     Projectile.frame++;
                     Projectile.frameCounter = 0;
                     if (Projectile.frame > FRAME_IDLE_END)
                     {
                         Projectile.frame = FRAME_IDLE_START;
                     }
                 }
            }
            #endregion
        }
    }
}
