using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.GameContent;
using WakfuMod.ModSystems;

namespace WakfuMod.Content.Projectiles
{
    public class AniripsaExplosion : ModProjectile
    {
        // Animación de 7 frames en vertical
        private const int TOTAL_FRAMES = 7;
        private const int TICKS_PER_FRAME = 4;
        private const int TOTAL_DURATION = TOTAL_FRAMES * TICKS_PER_FRAME;
        
        private const int BASE_SIZE = 80;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = TOTAL_FRAMES;
        }

        public override void SetDefaults()
        {
            Projectile.width = BASE_SIZE;
            Projectile.height = BASE_SIZE;
            Projectile.friendly = true;
            Projectile.hostile = false; // Manejamos daño manuelmente para evitar golpear aliados si fuera friendly standard
            Projectile.DamageType = DamageClass.Magic; 
            Projectile.penetrate = -1; 
            Projectile.timeLeft = TOTAL_DURATION;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.light = 0.8f;
            Projectile.alpha = 0;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = TOTAL_DURATION;
        }

        public override void AI()
        {
            // First tick logic
            if (Projectile.localAI[0] == 0)
            {
                Projectile.localAI[0] = 1;
                
                // Scale from AI[0]
                float scaleFactor = Projectile.ai[0] > 0 ? Projectile.ai[0] : 1f;
                Projectile.scale = scaleFactor;
                
                // Resize
                int newSize = (int)(BASE_SIZE * scaleFactor);
                Vector2 center = Projectile.Center;
                Projectile.width = newSize;
                Projectile.height = newSize;
                Projectile.Center = center;

                SoundEngine.PlaySound(SoundID.Item14, Projectile.Center); // Explosion sound

                // --- LOGIC: HEAL ALLIES / DAMAGE ENEMIES ---
                Player owner = Main.player[Projectile.owner];
                float radius = Projectile.width / 2f;

                // 1. Allies (Players) - Heal
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player target = Main.player[i];
                    if (target.active && !target.dead && target.GetModPlayer<jugador.WakfuPlayer>().currentFootballTeam == owner.GetModPlayer<jugador.WakfuPlayer>().currentFootballTeam 
                        && Vector2.Distance(center, target.Center) < radius)
                    {
                        // Si no hay equipos (None), curar a todos los jugadores? Asumamos que sí para PvE
                        if (owner.GetModPlayer<jugador.WakfuPlayer>().currentFootballTeam == FootballTeam.None || 
                            target.whoAmI == owner.whoAmI || 
                            target.team == owner.team) // Terraria team check
                        {
                            // Calculate Heal
                            // Base 20 + Scaling (+10 per 5% bonus)
                            float damageMult = owner.GetDamage(DamageClass.Magic).Additive;
                            int healAmount = 20;
                            if (damageMult > 1f)
                            {
                                int bonusSteps = (int)((damageMult - 1f) / 0.05f);
                                healAmount += bonusSteps * 10;
                            }
                            
                            target.statLife += healAmount;
                            if (target.statLife > target.statLifeMax2) target.statLife = target.statLifeMax2;
                            target.HealEffect(healAmount);
                        }
                    }
                }

                // 2. Enemies (NPCs) - Damage (Manual Area Check)
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC target = Main.npc[i];
                    if (target.active && !target.friendly && target.life > 0 && !target.dontTakeDamage
                        && Vector2.Distance(center, target.Center) < radius)
                    {
                        // Calculate Damage
                        // Base 20 + Scaling (+10 per 5% bonus)
                        float damageMult = owner.GetDamage(DamageClass.Magic).Additive;
                        int damageAmount = 20;
                        if (damageMult > 1f)
                        {
                             int bonusSteps = (int)((damageMult - 1f) / 0.05f);
                             damageAmount += bonusSteps * 10;
                        }

                        // Apply Damage
                        if (Main.myPlayer == Projectile.owner)
                        {
                            owner.ApplyDamageToNPC(target, damageAmount, 0f, 0, false);
                        }
                        
                        // Visual
                        for (int d = 0; d < 5; d++)
                        {
                             Dust.NewDust(target.position, target.width, target.height, DustID.CursedTorch, 0, 0, 100, default, 1.2f);
                        }
                    }
                }
            }

            // Visuals
            int dustType = DustID.CursedTorch; // Green fire
            if (Main.rand.NextBool(2))
            {
                Vector2 offset = Main.rand.NextVector2Circular(Projectile.width / 2f, Projectile.height / 2f);
                int dust = Dust.NewDust(Projectile.Center + offset, 0, 0, dustType, 0, -2f, 100, default, 1.5f);
                Main.dust[dust].noGravity = true;
            }

            // Animation
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= TICKS_PER_FRAME)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
                if (Projectile.frame >= TOTAL_FRAMES)
                    Projectile.frame = TOTAL_FRAMES - 1;
            }

            if (Projectile.timeLeft < 8)
            {
                Projectile.alpha += 30;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
             // Visual burn green
            for (int i = 0; i < 8; i++)
            {
                int dust = Dust.NewDust(target.position, target.width, target.height, DustID.CursedTorch, 0, 0, 100, default, 1.2f);
                Main.dust[dust].noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            int frameHeight = texture.Height / TOTAL_FRAMES;
            Rectangle frame = new Rectangle(0, Projectile.frame * frameHeight, texture.Width, frameHeight);
            Vector2 origin = new Vector2(texture.Width / 2f, frameHeight / 2f);
            Color drawColor = Projectile.GetAlpha(lightColor);

            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                frame,
                drawColor,
                Projectile.rotation,
                origin,
                Projectile.scale,
                SpriteEffects.None,
                0
            );
            return false;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(100, 255, 100, 150) * ((255 - Projectile.alpha) / 255f); // Green tint
        }
    }
}
