using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace WakfuMod.Content.Projectiles
{
    public class HipermagoLightBall : ModProjectile
    {
        // ai[0] = 0 para primera bola, 1 para segunda bola
        // ai[1] = ID del jugador dueño (para sincronizar hits)
        
        // Sistema estático para trackear hits del combo
        private static Dictionary<int, HashSet<int>> _recentHits = new Dictionary<int, HashSet<int>>();
        private static Dictionary<int, int> _hitTimers = new Dictionary<int, int>();
        
        private const int HIT_WINDOW = 60; // 0.5 segundos para que cuente como combo
        private const int BONUS_DAMAGE = 25;
        
        // Animación
        private bool _isDying = false;
        private int _deathAnimTimer = 0;
        private const int DEATH_ANIM_DURATION = 12; // 6 ticks por frame de muerte

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 4; // 4 frames: 0-1 viajando, 2-3 muriendo
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 5;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            // Hitbox igual al tamaño del sprite (131x134 por frame)
            Projectile.width = 131;
            Projectile.height = 134;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Ranged; // Escala con daño a distancia
            Projectile.penetrate = 1; // No penetra, muere al impactar
            Projectile.timeLeft = 180; // 3 segundos
            Projectile.tileCollide = true; // NO traspasa bloques
            Projectile.ignoreWater = true;
            Projectile.light = 0.8f;
            Projectile.alpha = 50;
            Projectile.scale = 0.5f; // 50% más pequeña
        }

        public override void AI()
        {
            // Si está muriendo, solo animar la muerte
            if (_isDying)
            {
                _deathAnimTimer++;
                // Frames 2-3 para muerte (6 ticks cada uno)
                Projectile.frame = 2 + (_deathAnimTimer / 6) % 2;
                
                // Cuando termine la animación, matar el proyectil
                if (_deathAnimTimer >= DEATH_ANIM_DURATION)
                {
                    Projectile.Kill();
                }
                return;
            }
            
            // Animación de viaje (frames 0-1)
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 6) // 6 ticks por frame
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % 2; // Solo frames 0 y 1
            }
            
            // Sin rotación - la bola mantiene su orientación
            Projectile.rotation = 0f;

            // Limpiar hits viejos (solo en el servidor/singleplayer)
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                CleanupOldHits();
            }
        }

        private void CleanupOldHits()
        {
            List<int> toRemove = new List<int>();
            foreach (var kvp in _hitTimers)
            {
                if (Main.GameUpdateCount - kvp.Value > HIT_WINDOW)
                {
                    toRemove.Add(kvp.Key);
                }
            }
            foreach (int key in toRemove)
            {
                _hitTimers.Remove(key);
                _recentHits.Remove(key);
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            // Iniciar animación de muerte en lugar de morir inmediatamente
            StartDeathAnimation();
            return false; // No morir todavía
        }
        
        private void StartDeathAnimation()
        {
            if (!_isDying)
            {
                _isDying = true;
                _deathAnimTimer = 0;
                Projectile.frame = 2; // Empezar en frame de muerte
                Projectile.velocity = Vector2.Zero; // Detener movimiento
                Projectile.tileCollide = false; // Ya no colisionar
                Projectile.penetrate = -1; // No hacer más daño
                Projectile.friendly = false;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            int playerOwner = Projectile.owner;
            int npcIndex = target.whoAmI;
            bool isSecondBall = Projectile.ai[0] == 1f;
            
            // Crear key única para este jugador
            int playerKey = playerOwner;
            
            if (!_recentHits.ContainsKey(playerKey))
            {
                _recentHits[playerKey] = new HashSet<int>();
            }

            if (isSecondBall)
            {
                // Es la segunda bola - verificar si la primera ya golpeó a este NPC
                if (_recentHits[playerKey].Contains(npcIndex))
                {
                    // ¡COMBO! Aplicar daño bonus
                    ApplyBonusDamage(target, playerOwner);
                    
                    // Limpiar el registro de este NPC
                    _recentHits[playerKey].Remove(npcIndex);
                    
                    // Efecto visual del combo
                    SpawnComboEffect(target.Center);
                }
            }
            else
            {
                // Es la primera bola - registrar el hit
                _recentHits[playerKey].Add(npcIndex);
                _hitTimers[playerKey] = (int)Main.GameUpdateCount;
            }

            // Efecto visual de impacto
            for (int i = 0; i < 10; i++)
            {
                int dust = Dust.NewDust(target.position, target.width, target.height, 
                    DustID.YellowTorch, 0f, 0f, 100, default, 1.5f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 2f;
            }
            
            // Iniciar animación de muerte después de impactar
            StartDeathAnimation();
        }

        private void ApplyBonusDamage(NPC target, int playerOwner)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            
            Player player = Main.player[playerOwner];
            
            // Daño bonus que ignora armadura (ArmorPenetration alto)
            NPC.HitInfo bonusHit = new NPC.HitInfo
            {
                Damage = BONUS_DAMAGE,
                Knockback = 2f,
                HitDirection = Projectile.velocity.X > 0 ? 1 : -1,
                Crit = false,
                DamageType = DamageClass.Ranged,
                // La segunda bola penetra toda la armadura
            };
            
            // Aplicar el daño bonus ignorando defensa
            int originalDefense = target.defense;
            target.defense = 0; // Temporalmente quitar defensa
            target.StrikeNPC(bonusHit);
            target.defense = originalDefense; // Restaurar defensa
            
            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, target.whoAmI);
            }
        }

        private void SpawnComboEffect(Vector2 position)
        {
            // Explosión de partículas doradas para el combo
            for (int i = 0; i < 20; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(5f, 5f);
                int dust = Dust.NewDust(position, 0, 0, DustID.GoldFlame, velocity.X, velocity.Y, 100, default, 2f);
                Main.dust[dust].noGravity = true;
            }
            
            // Mensaje de combo
            if (Main.netMode != NetmodeID.Server)
            {
                CombatText.NewText(new Rectangle((int)position.X, (int)position.Y, 1, 1), 
                    Color.Gold, "COMBO!", true);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Dibujar con centro real del sprite (0.5, 0.5) como la spear
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            int frameHeight = texture.Height / Main.projFrames[Projectile.type];
            Rectangle sourceRect = new Rectangle(0, Projectile.frame * frameHeight, texture.Width, frameHeight);
            Vector2 origin = new Vector2(texture.Width * 0.5f, frameHeight * 0.5f); // Centro real
            
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Color drawColor = Projectile.GetAlpha(lightColor);
            
            Main.EntitySpriteDraw(texture, drawPos, sourceRect, drawColor, Projectile.rotation,
                origin, Projectile.scale, SpriteEffects.None, 0);
            
            return false; // No dibujar el sprite default
        }

        public override Color? GetAlpha(Color lightColor)
        {
            // Color normal del sprite, ligeramente brillante
            return Color.White * ((255 - Projectile.alpha) / 255f);
        }

        public override void OnKill(int timeLeft)
        {
            // Efecto de disipación
            for (int i = 0; i < 8; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 
                    DustID.YellowTorch, 0f, 0f, 100, default, 1f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 1.5f;
            }
        }

        // Limpiar datos estáticos cuando el jugador sale
        public static void ClearPlayerData(int playerIndex)
        {
            if (_recentHits.ContainsKey(playerIndex))
            {
                _recentHits.Remove(playerIndex);
            }
            if (_hitTimers.ContainsKey(playerIndex))
            {
                _hitTimers.Remove(playerIndex);
            }
        }
    }
}
