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
    public class TorossHomingProjectile : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Stasis Homing Orb");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 20; // Long trail
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 600; // Will be managed by AI
            Projectile.alpha = 255;
        }

        public override void AI()
        {
            // --- FRIENDLY / PLAYER VERSION ---
            if (Projectile.friendly)
            {
                // Initialize
                if (Projectile.localAI[0] == 0)
                {
                    Projectile.alpha = 0;
                    Projectile.timeLeft = 300; // 5 seconds
                }

                // Find nearest NPC
                NPC target = null;
                float maxDist = 1000f;
                
                foreach (NPC npc in Main.npc)
                {
                    if (npc.CanBeChasedBy(Projectile) && Vector2.Distance(Projectile.Center, npc.Center) < maxDist)
                    {
                        target = npc;
                        maxDist = Vector2.Distance(Projectile.Center, npc.Center);
                    }
                }

                if (target != null)
                {
                    float speed = 15f;
                    float inertia = 10f;
                    Vector2 direction = (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero);
                    Projectile.velocity = (Projectile.velocity * (inertia - 1) + direction * speed) / inertia;
                }

                Projectile.rotation += 0.2f;
                
                // Trail dust
                if (Main.rand.NextBool(3))
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PinkTorch, Vector2.Zero);
                    d.noGravity = true;
                }
                
                Projectile.localAI[0]++;
                return;
            }

            // --- HOSTILE / BOSS VERSION ---
            // ai[0] = Boss whoAmI
            // ai[1] = Target whoAmI
            // ai[2] = Offset Index (0-1)

            int chargeTime = 180; // 3 seconds
            int activeTime = 360; // 6 seconds
            
            if (Projectile.localAI[0] < chargeTime)
            {
                // --- CHARGING PHASE ---
                NPC boss = Main.npc[(int)Projectile.ai[0]];
                if (!boss.active || boss.type != ModContent.NPCType<Content.NPCs.Bosses.Toross.Toross>())
                {
                    Projectile.Kill();
                    return;
                }

                Projectile.alpha -= 5;
                if (Projectile.alpha < 0) Projectile.alpha = 0;

                // Stick to boss high above head
                // Use ai[2] as angle for positioning in a circle/arc
                float angle = Projectile.ai[2];
                float distance = 150f; // Radius of the ring of projectiles
                Vector2 orbitOffset = angle.ToRotationVector2() * distance;

                // Position relative to Boss Center but shifted UP by 450 pixels
                Projectile.Center = boss.Center + new Vector2(0, -450f) + orbitOffset;

                // Charge visual
                if (Main.rand.NextBool(2))
                {
                    Vector2 dustPos = Projectile.Center + Main.rand.NextVector2Circular(30f, 30f);
                    Vector2 dustVel = (Projectile.Center - dustPos).SafeNormalize(Vector2.Zero) * 2f;
                    Dust d = Dust.NewDustPerfect(dustPos, DustID.PinkTorch, dustVel);
                    d.noGravity = true;
                }
            }
            else if (Projectile.localAI[0] == chargeTime)
            {
                // --- LAUNCH ---
                SoundEngine.PlaySound(SoundID.Item33, Projectile.Center); // Energy sound
                Projectile.timeLeft = activeTime; // Set duration
            }
            else
            {
                // --- HOMING PHASE ---
                Player target = Main.player[(int)Projectile.ai[1]];
                if (target.active && !target.dead)
                {
                    // Scale speed with Boss Health
                    NPC boss = Main.npc[(int)Projectile.ai[0]];
                    float healthRatio = 1f;
                    if (boss.active && boss.type == ModContent.NPCType<Content.NPCs.Bosses.Toross.Toross>())
                    {
                        healthRatio = (float)boss.life / boss.lifeMax;
                    }

                    // Speed adjusted to be ~99% of player max speed (approx 9f for Lightning Boots/Wings)
                    // Previous was 12f-24f which was way too fast.
                    float speed = 6f + (1f - healthRatio) * 3f; // 6f -> 9f
                    float inertia = 30f - (1f - healthRatio) * 15f; // 30f -> 15f (Sharper turns)
                    
                    Vector2 direction = (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero);
                    Projectile.velocity = (Projectile.velocity * (inertia - 1) + direction * speed) / inertia;
                }
                
                Projectile.rotation += 0.2f;
                
                // Trail dust
                if (Main.rand.NextBool(3))
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PinkTorch, Vector2.Zero);
                    d.noGravity = true;
                }
            }

            Projectile.localAI[0]++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 drawOrigin = new Vector2(texture.Width * 0.5f, Projectile.height * 0.5f);

            // Draw Trail
            for (int k = 0; k < Projectile.oldPos.Length; k++)
            {
                Vector2 drawPos = (Projectile.oldPos[k] - Main.screenPosition) + drawOrigin + new Vector2(0f, Projectile.gfxOffY);
                Color color = Color.HotPink * ((Projectile.oldPos.Length - k) / (float)Projectile.oldPos.Length) * 0.5f;
                Main.EntitySpriteDraw(texture, drawPos, null, color, Projectile.rotation, drawOrigin, Projectile.scale, SpriteEffects.None, 0);
            }
            
            // Draw Core
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, drawOrigin, Projectile.scale, SpriteEffects.None, 0);

            return false;
        }
    }
}
