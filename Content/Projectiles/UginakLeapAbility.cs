using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using WakfuMod.jugador;
using System;

namespace WakfuMod.Content.Projectiles
{
    public class UginakLeapAbility : ModProjectile
    {
        private const int LeapDuration = 45; // Más corto que el de Yopuka (era 90)
        private const float LeapSpeedX = 22f; // Un poco más rápido
        private const float InitialLeapSpeedY = -12f;
        private const float LeapGravity = 0.8f;
        public const int LandingDuration = 20;

        private bool HasLanded { get => Projectile.localAI[1] == 1f; set => Projectile.localAI[1] = value ? 1f : 0f; }
        private int Direction { get => (int)Projectile.ai[1]; set => Projectile.ai[1] = value; }
        public float LandingTimer { get => Projectile.localAI[0]; set => Projectile.localAI[0] = value; }

        public override void SetDefaults()
        {
            Projectile.width = 48;
            Projectile.height = 64;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = LeapDuration + LandingDuration + 10;
            Projectile.friendly = true; // Daña al pasar?
            Projectile.DamageType = DamageClass.Melee;
            Projectile.hostile = false;
            Projectile.ignoreWater = true;
        }

        public override void OnSpawn(IEntitySource source)
        {
            HasLanded = false;
            LandingTimer = 0;

            Player player = Main.player[Projectile.owner];
            if (player != null)
            {
                if (Direction == 0) Direction = player.direction;
                
                Projectile.velocity.X = LeapSpeedX * Direction;
                Projectile.velocity.Y = InitialLeapSpeedY;
                Projectile.Center = player.Center;

                if (Main.netMode != NetmodeID.Server)
                {
                    SoundEngine.PlaySound(SoundID.Item71, Projectile.Center); // Sonido de rugido/esfuerzo
                    for (int i = 0; i < 20; i++)
                    {
                        Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Blood, Main.rand.NextVector2Circular(5f, 5f), 100, default, 1.5f);
                        d.noGravity = true;
                    }
                }
            }
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (!player.active || player.dead) { Projectile.Kill(); return; }

            if (HasLanded)
            {
                LandingTimer++;
                Projectile.velocity = Vector2.Zero;
                player.Center = Projectile.Center;
                if (LandingTimer >= LandingDuration) { Projectile.Kill(); }
                return;
            }

            Projectile.velocity.Y += LeapGravity;
            player.Center = Projectile.Center;
            player.velocity = Projectile.velocity;
            player.fallStart = (int)(player.position.Y / 16f);
            player.immuneTime = Math.Max(player.immuneTime, 2);
            
            // Bloquear controles
            player.controlJump = player.controlDown = player.controlLeft = player.controlRight = player.controlUp = false;
            player.controlUseItem = player.controlUseTile = player.controlThrow = player.controlHook = false;

            if (Projectile.velocity.Y > 0f)
            {
                if (Collision.SolidCollision(Projectile.position + new Vector2(0, 4), Projectile.width, Projectile.height))
                {
                    Land();
                }
            }

            if (LandingTimer < LeapDuration) LandingTimer++;
            else if (!HasLanded) { Projectile.Kill(); }
        }

        private void Land()
        {
            if (HasLanded) return;
            HasLanded = true;
            LandingTimer = 0;
            Projectile.velocity = Vector2.Zero;
            Projectile.netUpdate = true;

            Player player = Main.player[Projectile.owner];
            player.Bottom = Projectile.Bottom;

            if (Main.netMode != NetmodeID.Server)
            {
                SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
                for (int i = 0; i < 30; i++)
                {
                    Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, 0, 0, 100, default, 1.5f);
                }
            }
        }

        public override bool PreDraw(ref Color lightColor) => false; // No tiene sprite propio por ahora, usa al player
    }
}
