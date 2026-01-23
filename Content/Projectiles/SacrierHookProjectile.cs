using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System;

namespace WakfuMod.Content.Projectiles
{
    public class SacrierHookProjectile : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.aiStyle = 7; // Grappling Hook style
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false; // Handled by AI
            Projectile.timeLeft = 300;
            Projectile.DamageType = DamageClass.Generic;
        }

        // Custom AI to handle NPC collision and life steal
        public override bool PreAI()
        {
            // Check if we are hooked to an NPC (Stored in ai[1])
            // ai[1] stores NPC index + 1. 0 means no NPC.
            if (Projectile.ai[1] > 0)
            {
                int npcIndex = (int)Projectile.ai[1] - 1;
                NPC npc = Main.npc[npcIndex];

                if (!npc.active)
                {
                    Projectile.ai[1] = 0f; // Stop tracking
                    Projectile.ai[0] = 1f; // Retract
                    return true; // Let vanilla AI handle retraction
                }

                // If we are retracting (ai[0] == 1), drag the NPC with us
                if (Projectile.ai[0] == 1f)
                {
                    // Force NPC position to projectile position
                    npc.Center = Projectile.Center;
                    npc.velocity = Vector2.Zero; // Stop NPC from moving on its own
                    npc.netUpdate = true;
                    
                    // Let vanilla AI handle the movement of the hook towards the player
                    return true; 
                }
                else
                {
                    // If not retracting yet (shouldn't happen if we set ai[0]=1 on hit, but just in case)
                    // Keep attached to NPC
                    Projectile.Center = npc.Center;
                    Projectile.velocity = Vector2.Zero;
                    Projectile.timeLeft = 2; // Keep alive
                    
                    // Force retract if we are just sitting there
                    Projectile.ai[0] = 1f; 
                    
                    return false; 
                }
            }
            return true;
        }

        public override void PostAI()
        {
            // Only check for NPC collision if we haven't hooked anything yet
            // ai[0] == 0 means flying out. 
            if (Projectile.ai[0] == 0f && Projectile.ai[1] == 0f)
            {
                // Use a larger hitbox for detection to prevent tunneling
                Rectangle hitbox = Projectile.getRect();
                hitbox.Inflate(20, 20); // Make it bigger!

                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];
                    if (npc.active && !npc.friendly && !npc.dontTakeDamage && npc.getRect().Intersects(hitbox))
                    {
                        // Hit an NPC!
                        OnHitNPC(npc);

                        // Latch onto NPC
                        Projectile.ai[1] = i + 1; // Store NPC index + 1
                        
                        // Set to RETRACT immediately, so we pull the enemy
                        Projectile.ai[0] = 1f; 
                        
                        Projectile.velocity = Vector2.Zero;
                        Projectile.netUpdate = true;
                        break;
                    }
                }
            }
        }

        public void OnHitNPC(NPC target)
        {
            Player player = Main.player[Projectile.owner];
            
            // Calculate damage based on player stats
            int damage = Projectile.damage;
            
            // Apply damage
            player.ApplyDamageToNPC(target, damage, Projectile.knockBack, Projectile.direction, false);
            
            // Life Steal: 50 HP (Clamped)
            int healAmount = 50;
            
            // Visual feedback
            CombatText.NewText(player.getRect(), Color.Green, "+" + healAmount, true);
            
            // Apply heal
            player.statLife += healAmount;
            if (player.statLife > player.statLifeMax2)
            {
                player.statLife = player.statLifeMax2;
            }
            
            // Visuals
            for (int i = 0; i < 20; i++)
            {
                Dust.NewDust(target.position, target.width, target.height, DustID.Blood, 0, 0, 100, default, 2f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Draw chain
            Vector2 playerCenter = Main.player[Projectile.owner].MountedCenter;
            Vector2 center = Projectile.Center;
            Vector2 distToProj = playerCenter - Projectile.Center;
            float projRotation = distToProj.ToRotation() - 1.57f;
            float distance = distToProj.Length();
            
            Texture2D texture = ModContent.Request<Texture2D>("Terraria/Images/Chain40").Value; // Use a vanilla chain texture for now (Blood/Red chain)
            // Or Chain12 (flesh chain)
            
            while (distance > 30f && !float.IsNaN(distance))
            {
                distToProj.Normalize();                 // get unit vector
                distToProj *= 24f;                      // speed = 24
                center += distToProj;                   // update draw position
                distToProj = playerCenter - center;    // update distance
                distance = distToProj.Length();

                // Draw chain segment
                Main.EntitySpriteDraw(texture, center - Main.screenPosition,
                    new Rectangle(0, 0, texture.Width, texture.Height), Color.Red, projRotation,
                    new Vector2(texture.Width * 0.5f, texture.Height * 0.5f), 1f, SpriteEffects.None, 0);
            }
            return true;
        }
        
        // Range configuration (45 blocks = 720 pixels)
        public override float GrappleRange()
        {
            return 720f;
        }

        public override void NumGrappleHooks(Player player, ref int numHooks)
        {
            numHooks = 1;
        }
    }
}
