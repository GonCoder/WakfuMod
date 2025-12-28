using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.GameContent;

namespace WakfuMod.Content.Projectiles
{
    public class AniripsaMark : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 3600; // 1 minuto (se renovará si el target es válido)
        }

        // AI[0] = Target ID (Player whoAmI or NPC whoAmI)
        // AI[1] = Target Type (0 = Player, 1 = NPC)
        private int timer = 0;
        private const int TickRate = 120; // 2 segundos
        
        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            
            // Validar que el dueño sigue conectado y es Aniripsa
            if (!owner.active || owner.dead || owner.GetModPlayer<jugador.WakfuPlayer>().claseElegida != jugador.WakfuClase.Aniripsa)
            {
                Projectile.Kill();
                return;
            }
            
            bool targetActive = false;
            Vector2 targetCenter = Vector2.Zero;
            int targetWidth = 0;
            int targetHeight = 0;

            if (Projectile.ai[1] == 0) // Target Player
            {
                int targetPlrID = (int)Projectile.ai[0];
                if (targetPlrID >= 0 && targetPlrID < Main.maxPlayers)
                {
                    Player target = Main.player[targetPlrID];
                    if (target.active && !target.dead)
                    {
                        targetActive = true;
                        targetCenter = target.Center;
                        targetWidth = target.width;
                        targetHeight = target.height;
                        
                        // Efecto Curativo
                        timer++;
                        if (timer >= TickRate)
                        {
                            timer = 0;
                            // Curar 5 HP Base + Scaling (5 por cada 5% de magic damage)
                            int healAmount = 5;
                            float magicDamage = owner.GetDamage(DamageClass.Magic).Additive; // E.g., 1.05 = 5% bonus
                            if (magicDamage > 1f)
                            {
                                int bonusSteps = (int)((magicDamage - 1f) / 0.05f);
                                healAmount += bonusSteps * 5;
                            }

                            if (target.statLife < target.statLifeMax2)
                            {
                                target.statLife += healAmount;
                                if (target.statLife > target.statLifeMax2)
                                    target.statLife = target.statLifeMax2;
                                
                                target.HealEffect(healAmount);
                            }
                        }
                    }
                }
            }
            else // Target NPC
            {
                int targetNPCID = (int)Projectile.ai[0];
                if (targetNPCID >= 0 && targetNPCID < Main.maxNPCs)
                {
                    NPC target = Main.npc[targetNPCID];
                    if (target.active && target.life > 0)
                    {
                        targetActive = true;
                        targetCenter = target.Center;
                        targetWidth = target.width;
                        targetHeight = target.height;
                         
                        // Efecto Dañino
                        timer++;
                        if (timer >= TickRate)
                        {
                            timer = 0;
                            // Daño Flat 5 Base + Scaling (5 por cada 5% de magic damage)
                            int damage = 5;
                            float magicDamage = owner.GetDamage(DamageClass.Magic).Additive;
                            if (magicDamage > 1f)
                            {
                                int bonusSteps = (int)((magicDamage - 1f) / 0.05f);
                                damage += bonusSteps * 5;
                            }
                            
                            // Aplicar daño
                            if (Main.myPlayer == Projectile.owner)
                            {
                                owner.ApplyDamageToNPC(target, damage, 0f, 0, false);
                            }
                           
                            CombatText.NewText(target.getRect(), Color.Purple, damage.ToString(), true);
                        }
                    }
                }
            }

            if (!targetActive)
            {
                Projectile.Kill();
                return;
            }
            
            // Mantenerse sobre la cabeza del target
            Projectile.Center = targetCenter - new Vector2(0, targetHeight / 2 + 40);
            Projectile.timeLeft = 2; // Mantener vivo mientras target exista
        }
        
        public override bool PreDraw(ref Color lightColor)
        {
            // Dibujar a mano para no necesitar sprite sheet complejo
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value; 
            // Como no tenemos textura aun, usaremos placeholders o primitivas de dibujo si es posible?
            // El usuario dijo "como ponías la X roja". Asumiré que tengo que dibujar texturas.
            // Voy a usar texturas procedurales simples generadas en código o texturas existentes.
            // Para "Corazón", Terraria tiene texturas de corazones (UI).
            
            Texture2D heartTexture = TextureAssets.Heart.Value; // Vida UI
            // Texture2D? Heart is loaded via Main.heartTexture usually? No.
            
            // Usaremos TextureAssets.Extra[2] ? 
            // O mejor: Dibujar usando helper methods si no tengo asset.
            return false; 
        }

        public override void PostDraw(Color lightColor)
        {
             // AI[1] == 0: Ally -> Red Heart
             // AI[1] == 1: Enemy -> Purple Heart + Red X
             
             SpriteBatch sb = Main.spriteBatch;
             Vector2 drawPos = Projectile.Center - Main.screenPosition;
             float scale = 1.2f + (float)Math.Sin(Main.GameUpdateCount * 0.1f) * 0.1f; // Palpitante
             
             Texture2D heartTex = TextureAssets.Heart.Value;
             
             if (Projectile.ai[1] == 0) // Ally
             {
                 sb.Draw(heartTex, drawPos, null, Color.Red, 0f, heartTex.Size() / 2, scale, SpriteEffects.None, 0f);
             }
             else // Enemy
             {
                 // Corazón Morado
                 sb.Draw(heartTex, drawPos, null, Color.Purple, 0f, heartTex.Size() / 2, scale, SpriteEffects.None, 0f);
                 
                 // X Roja (Usando dos lineas o textura CD)
                 // Usaremos la textura de "X" del inventory 'junk' slot o similar?
                 // TextureAssets.Cd has the cooldown X.
                 Texture2D xTex = TextureAssets.Cd.Value;
                 sb.Draw(xTex, drawPos, null, Color.Red, 0f, xTex.Size() / 2, scale * 0.8f, SpriteEffects.None, 0f);
             }
        }
    }
}
