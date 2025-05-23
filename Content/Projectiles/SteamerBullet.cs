using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System;
using WakfuMod.Content.NPCs;

namespace WakfuMod.Content.Projectiles
{
    public class SteamerBullet : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 3; // Número de frames del sprite
        }

        public override void SetDefaults()
        {
            Projectile.width = 6;  // Ancho visual del proyectil
            Projectile.height = 6; // Alto visual del proyectil
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 100;
            Projectile.tileCollide = true;
            Projectile.DamageType = DamageClass.Summon;
        }

        private int frameCounter = 0;

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();

            // Homing muy fuerte
            NPC target = FindClosestEnemy(200f);
            if (target != null)
            {
                Vector2 toTarget = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
                float speed = Projectile.velocity.Length();
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, toTarget * speed, 0.65f);
            }

            // Animación
            frameCounter++;
            if (frameCounter >= 5) // Velocidad de animación: cambia cada 5 ticks
            {
                frameCounter = 0;
                Projectile.frame++;
                if (Projectile.frame >= Main.projFrames[Projectile.type])
                {
                    Projectile.frame = 0;
                }
            }
        }

        private NPC FindClosestEnemy(float range)
        {
            NPC closest = null;
            float minDist = range;

            foreach (NPC npc in Main.npc)
            {
                if (npc.CanBeChasedBy() && !npc.friendly)
                {
                    float dist = Vector2.Distance(Projectile.Center, npc.Center);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        closest = npc;
                    }
                }
            }

            return closest;
        }

         // --- ModifyHitNPC para Daño % Vida ---
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            float percentDamage = target.lifeMax * 0.02f;
            
            modifiers.FlatBonusDamage += percentDamage;
            modifiers.DefenseEffectiveness *= 0f; // Ignora defensa
        }

        // --- OnHitNPC para Aplicar Reducción de Defensa ---
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Obtén la instancia del GlobalNPC del objetivo (AHORA USA SteamerGlobalNPC)
            if (target.TryGetGlobalNPC(out SteamerGlobalNPC globalNPC)) // <-- CAMBIO AQUÍ
            {
                // Llama al método para aplicar la reducción (-1 defensa)
                globalNPC.ApplyDefenseReduction(1);
            }
            // (Opcional) Efectos visuales/sonido
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;

            int frameHeight = texture.Height / Main.projFrames[Projectile.type];
            Rectangle frame = new Rectangle(0, Projectile.frame * frameHeight, texture.Width, frameHeight);
            Vector2 origin = new Vector2(texture.Width / 2f, frameHeight / 2f);

            Main.spriteBatch.Draw(
                texture,
                Projectile.Center - Main.screenPosition,
                frame,
                lightColor,
                Projectile.rotation,
                origin,
                0.6f,
                SpriteEffects.None,
                0f
            );

            return false;
        }
    }
}
