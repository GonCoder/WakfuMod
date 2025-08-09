// Content/Projectiles/RuelCoinDrop.cs
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using System;

namespace WakfuMod.Content.Projectiles
{
    public class RuelCoinDrop : ModProjectile
    {
        // Estados:
        // localAI[0] == 0 => Hover/materialización (60 ticks)
        // localAI[0] == 1 => Caída teledirigida

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4; // 3 de creación + 1 final
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 600;
            Projectile.tileCollide = false;     // ataque que no falla (atraviesa bloques)
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.ArmorPenetration = 20;
        }

        private const int HoverTime = 60;          // 1 segundo (60 ticks)
        private const float HoverAbove = 150f;      // altura sobre la cabeza del target mientras “nace”
        private const float MaxFallSpeed = 14f;
        private const float GravityAccel = 0.6f;
        private const float HomingFactor = 0.20f;  // seguimiento lateral
        private const float MaxHomingSpeedX = 12f;

        public override void AI()
        {
            int targetIndex = (int)Projectile.ai[1];
            NPC target = (targetIndex >= 0 && targetIndex < Main.npc.Length) ? Main.npc[targetIndex] : null;

            // si el objetivo no es válido, retarget
            if (target == null || !target.active || target.friendly || target.townNPC || !target.CanBeChasedBy())
            {
                target = FindNewTarget(600f);
                if (target != null)
                    Projectile.ai[1] = target.whoAmI;
                else
                {
                    Projectile.Kill();
                    return;
                }
            }

            // FASE 0: materialización sobre la cabeza del enemigo
            if (Projectile.localAI[0] == 0f)
            {
                Vector2 desiredPos = target.Center + new Vector2(0f, -(target.height * 0.5f + HoverAbove));
                Projectile.Center = desiredPos;
                Projectile.velocity = Vector2.Zero;

                // chispitas doradas suaves durante la creación
                if (Main.rand.NextBool(3))
                {
                    int d = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GoldCoin, 0f, 0f, 150, default, 1.1f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].velocity *= 0.2f;
                    Main.dust[d].scale = 1.0f + Main.rand.NextFloat(0.2f);
                }

                int frame = Math.Clamp((int)(Projectile.ai[0] / 20f), 0, 2); // 3 frames en 60 ticks
                Projectile.frame = frame;

                Projectile.ai[0]++;
                if (Projectile.ai[0] >= HoverTime)
                {
                    Projectile.localAI[0] = 1f; // pasar a caída
                    Projectile.frame = 3;       // forma final
                    Projectile.velocity = new Vector2(0f, 2f);
                    SoundEngine.PlaySound(SoundID.Coins, Projectile.Center);

                    // pequeño destello al terminar de “forjarse”
                    for (int i = 0; i < 6; i++)
                    {
                        Vector2 v = new Vector2(Main.rand.NextFloat(-1.5f, 1.5f), Main.rand.NextFloat(-1.5f, 1.5f));
                        int d = Dust.NewDust(Projectile.Center - new Vector2(4, 4), 8, 8, DustID.GoldCoin, v.X, v.Y, 120, default, 1.2f);
                        Main.dust[d].noGravity = true;
                    }
                }
                return;
            }

            // FASE 1: caída teledirigida
            if (Projectile.localAI[0] == 1f)
            {
                Projectile.frame = 3;

                // “gravedad”
                Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + GravityAccel, MaxFallSpeed);

                // homing lateral para seguir al target
                float dx = (target.Center.X - Projectile.Center.X);
                float vxDesired = MathHelper.Clamp(dx * HomingFactor, -MaxHomingSpeedX, MaxHomingSpeedX);
                Projectile.velocity.X = (Projectile.velocity.X * 3f + vxDesired) / 4f;

                // rastro dorado al caer (ligero)
                if (Main.rand.NextBool(2))
                {
                    int d = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GoldCoin, 0f, 0f, 140, default, 1.0f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].velocity *= 0.3f;
                    Main.dust[d].scale = 0.9f + Main.rand.NextFloat(0.2f);
                }
            }
        }

        public override bool? CanHitNPC(NPC target)
        {
            if (target.friendly || target.townNPC)
                return false;
            return base.CanHitNPC(target);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SoundEngine.PlaySound(SoundID.Coins, Projectile.Center);

            // chispa de impacto
            for (int i = 0; i < 8; i++)
            {
                Vector2 v = Main.rand.NextVector2Circular(2.2f, 2.2f);
                int d = Dust.NewDust(Projectile.Center - new Vector2(4, 4), 8, 8, DustID.GoldCoin, v.X, v.Y, 100, default, 1.2f);
                Main.dust[d].noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            // si muere sin golpear, que también suelte un pequeño destello
            for (int i = 0; i < 6; i++)
            {
                Vector2 v = Main.rand.NextVector2Circular(1.8f, 1.8f);
                int d = Dust.NewDust(Projectile.Center - new Vector2(4, 4), 8, 8, DustID.GoldCoin, v.X, v.Y, 130, default, 1.1f);
                Main.dust[d].noGravity = true;
            }
        }

        private NPC FindNewTarget(float range)
        {
            NPC best = null;
            float bestDist = range;
            for (int i = 0; i < Main.npc.Length; i++)
            {
                NPC n = Main.npc[i];
                if (n.active && !n.friendly && !n.townNPC && n.CanBeChasedBy(null, false))
                {
                    float d = Vector2.Distance(Projectile.Center, n.Center);
                    if (d <= bestDist)
                    {
                        bestDist = d;
                        best = n;
                    }
                }
            }
            return best;
        }
    }
}
