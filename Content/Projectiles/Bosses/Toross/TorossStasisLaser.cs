using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace WakfuMod.Content.Projectiles.Bosses.Toross
{
    public class TorossStasisLaser : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Stasis Laser");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 10;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 600;
            Projectile.alpha = 255; // Start invisible
        }

        public override void AI()
        {
            // --- FRIENDLY / PLAYER VERSION ---
            if (Projectile.friendly)
            {
                // Simple straight flight
                Projectile.rotation = Projectile.velocity.ToRotation();
                
                // Trail dust
                if (Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PinkTorch, Vector2.Zero);
                    d.noGravity = true;
                    d.velocity = Vector2.Zero;
                }
                return;
            }

            // ai[0] = Boss whoAmI
            // ai[1] = Target whoAmI
            // ai[2] = Offset Index (0-3)
            
            int chargeTime = 120; // 2 seconds
            
            if (Projectile.localAI[0] < chargeTime)
            {
                // --- CHARGING PHASE ---
                NPC boss = Main.npc[(int)Projectile.ai[0]];
                if (!boss.active || boss.type != ModContent.NPCType<Content.NPCs.Bosses.Toross.Toross>())
                {
                    Projectile.Kill();
                    return;
                }

                // Fade in
                Projectile.alpha -= 10;
                if (Projectile.alpha < 0) Projectile.alpha = 0;

                // Orbit / Stick to boss
                // Use ai[2] as the starting angle directly
                float rotation = (float)Main.timeForVisualEffects * 0.05f + Projectile.ai[2];
                float distance = 120f; // Orbit radius
                Vector2 orbitOffset = rotation.ToRotationVector2() * distance;
                
                // Position relative to Boss Center but shifted UP by 450 pixels
                Projectile.Center = boss.Center + new Vector2(0, -450f) + orbitOffset;
                
                // Aim visual (rotation)
                Player target = Main.player[(int)Projectile.ai[1]];
                if (target.active && !target.dead)
                {
                    Vector2 dir = (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero);
                    Projectile.rotation = dir.ToRotation();
                }

                // Charge dust
                if (Main.rand.NextBool(3))
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PinkTorch, Main.rand.NextVector2Circular(2f, 2f));
                    d.noGravity = true;
                    d.scale = 1.5f;
                }
            }
            else if (Projectile.localAI[0] == chargeTime)
            {
                // --- FIRE ---
                Player target = Main.player[(int)Projectile.ai[1]];
                Vector2 shootDir;
                if (target.active && !target.dead)
                {
                    shootDir = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
                }
                else
                {
                    shootDir = Vector2.UnitY; // Fallback
                }
                
                Projectile.velocity = shootDir * 18f; // Fast laser speed
                Projectile.rotation = Projectile.velocity.ToRotation();
                SoundEngine.PlaySound(SoundID.Item12, Projectile.Center); // Laser sound
            }
            else
            {
                // --- FLYING PHASE ---
                Projectile.rotation = Projectile.velocity.ToRotation();
                
                // Trail dust
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PinkTorch, Vector2.Zero);
                d.noGravity = true;
                d.velocity = Vector2.Zero;
            }

            Projectile.localAI[0]++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            // Use a default texture if none exists (e.g., ProjectileID.DeathLaser) logic or just draw a placeholder
            // Assuming texture exists or using a fallback like a pink bar
            
            // Draw Trail
            Main.instance.LoadProjectile(Projectile.type);
            Vector2 drawOrigin = new Vector2(texture.Width * 0.5f, Projectile.height * 0.5f);
            for (int k = 0; k < Projectile.oldPos.Length; k++)
            {
                Vector2 drawPos = (Projectile.oldPos[k] - Main.screenPosition) + drawOrigin + new Vector2(0f, Projectile.gfxOffY);
                Color color = Color.HotPink * ((Projectile.oldPos.Length - k) / (float)Projectile.oldPos.Length);
                Main.EntitySpriteDraw(texture, drawPos, null, color, Projectile.rotation, drawOrigin, Projectile.scale, SpriteEffects.None, 0);
            }

            return true;
        }
    }
}
