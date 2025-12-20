using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace WakfuMod.Content.Projectiles
{
    public class UginakWuauMinion : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 28; // 28 frames en el sprite sheet
            Main.projPet[Projectile.type] = true; // Es una pet/minion
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.CultistIsResistantTo[Projectile.type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 34;
            Projectile.height = 26;
            Projectile.tileCollide = false; // No colisiona con bloques
            Projectile.friendly = false;
            Projectile.minion = true;
            Projectile.minionSlots = 0f; // No usa slots de minion
            Projectile.penetrate = -1;
            Projectile.timeLeft = 18000; // 5 minutos
        }

        private int tongueTimer = 0;
        private const int TongueCooldown = 600; // 10 segundos
        private bool tongueActive = false;
        private int tongueAnimationTimer = 0;
        
        // Sistema de animaciones
        private enum AnimationState { Idle, Walking, Jump }
        private AnimationState currentState = AnimationState.Idle;
        private int idleVariantTimer = 0; // Timer para cambiar a animaciones auxiliares
        private int currentIdleVariant = 0; // 0 = normal, 1 = aux1, 2 = aux2

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            
            // Verificar que el jugador sea Uginak
            var wakfuPlayer = owner.GetModPlayer<jugador.WakfuPlayer>();
            if (wakfuPlayer.claseElegida != jugador.WakfuClase.Uginak)
            {
                Projectile.Kill();
                return;
            }

            // --- Lógica de seguimiento tipo pet ---
            Vector2 targetPos = owner.Center;
            targetPos.X -= 40f * owner.direction; // Detrás del jugador
            targetPos.Y -= 20f; // Un poco arriba

            float speed = 8f;
            float distance = Vector2.Distance(Projectile.Center, targetPos);

            if (distance > 2000f)
            {
                // Teletransportar si está muy lejos
                Projectile.Center = owner.Center;
                Projectile.velocity = Vector2.Zero;
            }
            else if (distance > 100f)
            {
                // Moverse hacia el jugador
                Vector2 direction = targetPos - Projectile.Center;
                direction.Normalize();
                Projectile.velocity = direction * speed;
            }
            else
            {
                // Ralentizar cuando está cerca
                Projectile.velocity *= 0.9f;
            }

            // Orientación
            if (Projectile.velocity.X > 0.5f)
                Projectile.spriteDirection = -1;
            else if (Projectile.velocity.X < -0.5f)
                Projectile.spriteDirection = 1;

            // --- Sistema de animación mejorado ---
            speed = Projectile.velocity.Length(); // Reutilizamos la variable speed de línea 60
            
            // Determinar estado
            if (speed > 1f)
            {
                currentState = AnimationState.Walking;
                idleVariantTimer = 0; // Reset timer de variantes
                currentIdleVariant = 0;
            }
            else
            {
                currentState = AnimationState.Idle;
            }

            // Animación según estado
            Projectile.frameCounter++;
            
            switch (currentState)
            {
                case AnimationState.Idle:
                    // Timer para cambiar a animaciones auxiliares
                    if (Projectile.frameCounter % 60 == 0) // Cada segundo
                    {
                        idleVariantTimer++;
                        
                        // Probabilidad de cambiar a animación auxiliar
                        if (idleVariantTimer > 3 && Main.rand.NextBool(4)) // Cada ~4 segundos, 25% chance
                        {
                            // Elegir variante aleatoria
                            currentIdleVariant = Main.rand.Next(1, 3); // 1 o 2
                            idleVariantTimer = 0;
                        }
                    }
                    
                    int idleFrameSpeed = 6;
                    
                    if (currentIdleVariant == 0)
                    {
                        // Idle normal (frames 0-7)
                        if (Projectile.frameCounter >= idleFrameSpeed)
                        {
                            Projectile.frameCounter = 0;
                            Projectile.frame++;
                            if (Projectile.frame >= 8)
                            {
                                Projectile.frame = 0;
                            }
                        }
                    }
                    else if (currentIdleVariant == 1)
                    {
                        // Idle auxiliar 1 (frames 17-22)
                        if (Projectile.frameCounter >= idleFrameSpeed)
                        {
                            Projectile.frameCounter = 0;
                            if (Projectile.frame < 17 || Projectile.frame > 22)
                                Projectile.frame = 17;
                            else
                            {
                                Projectile.frame++;
                                if (Projectile.frame > 22)
                                {
                                    // Volver a idle normal
                                    Projectile.frame = 0;
                                    currentIdleVariant = 0;
                                }
                            }
                        }
                    }
                    else if (currentIdleVariant == 2)
                    {
                        // Idle auxiliar 2 (frames 23-27)
                        if (Projectile.frameCounter >= idleFrameSpeed)
                        {
                            Projectile.frameCounter = 0;
                            if (Projectile.frame < 23 || Projectile.frame > 27)
                                Projectile.frame = 23;
                            else
                            {
                                Projectile.frame++;
                                if (Projectile.frame > 27)
                                {
                                    // Volver a idle normal
                                    Projectile.frame = 0;
                                    currentIdleVariant = 0;
                                }
                            }
                        }
                    }
                    break;

                case AnimationState.Walking:
                    // Walking animation (frames 9-16)
                    int walkFrameSpeed = 4;
                    if (Projectile.frameCounter >= walkFrameSpeed)
                    {
                        Projectile.frameCounter = 0;
                        if (Projectile.frame < 9 || Projectile.frame > 16)
                            Projectile.frame = 9;
                        else
                        {
                            Projectile.frame++;
                            if (Projectile.frame > 16)
                            {
                                Projectile.frame = 9;
                            }
                        }
                    }
                    break;

                case AnimationState.Jump:
                    // Jump/Air (frame 8)
                    Projectile.frame = 8;
                    break;
            }

            // --- Sistema de lengua curativa ---
            tongueTimer++;
            if (tongueTimer >= TongueCooldown && !tongueActive)
            {
                // Lanzar lengua!
                tongueActive = true;
                tongueAnimationTimer = 30; // Duración de la animación
                tongueTimer = 0;

                // Curar al jugador 35% de su vida máxima
                int healAmount = (int)(owner.statLifeMax2 * 0.35f);
                owner.statLife += healAmount;
                if (owner.statLife > owner.statLifeMax2)
                    owner.statLife = owner.statLifeMax2;

                // Efectos
                SoundEngine.PlaySound(SoundID.Item2, Projectile.Center);
                Main.NewText($"Wuau healed {healAmount} HP!", Color.LightGreen);

                // Partículas de curación
                for (int i = 0; i < 10; i++)
                {
                    Vector2 vel = new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-2f, 0f));
                    Dust.NewDust(owner.position, owner.width, owner.height, DustID.LifeDrain, vel.X, vel.Y, 100, Color.Green, 1.2f);
                }
            }

            // Contar animación de lengua
            if (tongueActive)
            {
                tongueAnimationTimer--;
                if (tongueAnimationTimer <= 0)
                {
                    tongueActive = false;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Dibujar lengua si está activa
            if (tongueActive)
            {
                Player owner = Main.player[Projectile.owner];
                Vector2 start = Projectile.Center;
                Vector2 end = owner.Center;

                // Dibujar laser rojo (línea roja con partículas)
                DrawLaserTongue(start, end);
            }

            return true; // Dibujar sprite normal del perrito
        }

        private void DrawLaserTongue(Vector2 start, Vector2 end)
        {
            // Generar partículas rojas a lo largo de la línea
            int particleCount = 5;
            for (int i = 0; i < particleCount; i++)
            {
                float progress = i / (float)particleCount;
                Vector2 position = Vector2.Lerp(start, end, progress);
                
                if (Main.rand.NextBool(2))
                {
                    Dust dust = Dust.NewDustPerfect(position, DustID.Blood, Vector2.Zero, 100, Color.Red, 1.5f);
                    dust.noGravity = true;
                }
            }

            // Dibujar línea roja simple
            // Nota: Para un verdadero láser necesitarías usar SpriteBatch en el hook Draw
            // Aquí solo usamos partículas para simular el efecto
        }

        public override void Kill(int timeLeft)
        {
            // Efectos al desparecer
            for (int i = 0; i < 5; i++)
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, 0, 0, 100, Color.Gray, 0.8f);
            }
        }
    }
}
