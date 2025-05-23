using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System;
using Terraria.Audio;

namespace WakfuMod.Content.Projectiles
{
    public class SteamerTurretProjectile : ModProjectile
    {
        private int bodyFrame = 0;
        private int bodyFrameCounter = 0;

        private int headFrame = 0;
        private int headFrameCounter = 0;

        private const int MaxBodyFrames = 5;
        private const int MaxHeadFrames = 5;

        private int fireCooldown = 0;
        private const int ContactDamageCooldown = 40; // <<<--- NUEVO: 60 ticks = 1 segundo para daño por contacto
        private const int ContactDamage = 10; // <<<--- NUEVO: Daño base por contacto (ajustar)

        private Vector2 aimDirection = Vector2.UnitX;
        private float headRotationActual = MathHelper.PiOver2;

        private bool wasAimingLeft = false;
        // --- NUEVA CONSTANTE para Cooldown del Láser Especial ---
        private const int SpecialLaserCooldownTime = 180; // 3 segundos de cooldown

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = MaxBodyFrames;
        }

        public override void SetDefaults()
        {
            Projectile.width = 110;
            Projectile.height = 48;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = int.MaxValue;
            Projectile.ignoreWater = true;
            
        }

        public override void AI()
        {
             // --- Decrementar Cooldowns ---
            if (fireCooldown > 0) fireCooldown--;
            // Usaremos localAI[0] para el cooldown del láser especial
            if (Projectile.localAI[0] > 0) Projectile.localAI[0]--;

            // --- Lógica de Objetivo (igual que antes) ---
            NPC target = FindEnemyWithGrenade() ?? FindClosestEnemy(600f);
            Projectile grenadeTarget = null;
            if (target == null)
                grenadeTarget = FindGrenadeTarget();

            float targetRotation = 0;
            Vector2 targetCenter = Projectile.Center + Vector2.UnitX * Projectile.spriteDirection * 100; // Punto por defecto si no hay objetivo
            bool hasValidTarget = false;

            if (target != null) {
                targetCenter = target.Center;
                hasValidTarget = true;
            } else if (grenadeTarget != null) {
                targetCenter = grenadeTarget.Center;
                hasValidTarget = true;
            }

            // --- Actualizar Dirección y Rotación (igual que antes) ---
            if (hasValidTarget) {
                aimDirection = (targetCenter - Projectile.Center).SafeNormalize(Vector2.UnitX * Projectile.spriteDirection);
                targetRotation = aimDirection.ToRotation();
                Projectile.spriteDirection = (Projectile.Center.X < targetCenter.X).ToDirectionInt();
                wasAimingLeft = aimDirection.X < 0f;

                // --- Disparo Normal (solo si hay objetivo y cooldown listo) ---
                TryShootLaser(targetCenter); // El disparo normal sigue ocurriendo automáticamente
            } else {
                 // Lógica para cuando no hay objetivo (igual que antes)
                 aimDirection = Vector2.UnitX * Projectile.spriteDirection; // Mira hacia adelante
                 targetRotation = 0f; // O ajusta a una rotación neutral
                 if (wasAimingLeft && Projectile.spriteDirection == 1) { wasAimingLeft = false; }
                 else if (!wasAimingLeft && Projectile.spriteDirection == -1) { wasAimingLeft = true; }
            }

            // Ajuste de rotación y Lerp (igual que antes)
            float adjustedTargetRot = targetRotation;
            if (Projectile.spriteDirection == -1) { // Corrección: usar spriteDirection
                adjustedTargetRot = (float)Math.Atan2(-aimDirection.Y, -aimDirection.X);
            }
            headRotationActual = LerpRotation(headRotationActual, adjustedTargetRot, 0.15f);

            // --- Animaciones (igual que antes) ---
            AnimateBody();
            AnimateHead();

            // --- Daño por Contacto (igual que antes) ---
            CheckAndApplyContactDamage();

            // --- NUEVO: Comprobar y Disparar Láser Especial ---
            // Verifica la señal desde WakfuPlayer (ai[1] == 1f) y el cooldown local
            if (Projectile.ai[1] == 1f && Projectile.localAI[0] <= 0)
            {
                 // Solo dispara si hay un objetivo válido al que apuntar
                 if (hasValidTarget)
                 {
                    TryShootSpecialLaser(targetCenter); // Llama a la nueva función
                    Projectile.localAI[0] = SpecialLaserCooldownTime; // Poner en cooldown
                 }
                 // Resetear la señal independientemente de si disparó o no
                 Projectile.ai[1] = 0f; // Resetea la señal
                 // Sincronizar ai[1] y localAI[0] si es necesario para multijugador
                 // Projectile.netUpdate = true;
            }
        } // Fin AI

        // --- NUEVA FUNCIÓN: Comprobar y Aplicar Daño por Contacto ---
        private void CheckAndApplyContactDamage()
        {
            Player owner = Main.player[Projectile.owner];
            Rectangle hitbox = Projectile.Hitbox; // Hitbox actual de la torreta

            // Iterar sobre todos los NPCs hostiles
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];

                // Comprobar si es un objetivo válido, está vivo, puede ser dañado,
                // está tocando la hitbox Y si su cooldown local para ESTE proyectil ha terminado.
                 if (npc.active && !npc.friendly && npc.life > 0 && !npc.dontTakeDamage &&
                     hitbox.Intersects(npc.Hitbox) && npc.immune[Projectile.owner] == 0) // <<<--- CORRECCIÓN AQUÍ
                {
                    // Aplicar daño por contacto
                    // Usamos GetDamage para escalar con bonos de invocación
                    int finalDamage = (int)owner.GetDamage(DamageClass.Summon).ApplyTo(ContactDamage);
                    // Usamos ApplyDamageToNPC que maneja mejor los modificadores
                    owner.ApplyDamageToNPC(npc, finalDamage, 20f /* knockback */, Math.Sign(npc.Center.X - Projectile.Center.X), false, Projectile.DamageType);

                    // Aplicar el cooldown local al NPC para ESTA torreta
                    npc.immune[Projectile.owner] = ContactDamageCooldown;

                    // Opcional: Efecto visual/sonido al electrocutar por contacto
                    if (Main.netMode != NetmodeID.Server)
                    {
                        SoundEngine.PlaySound(SoundID.Item93 with { Volume = 0.3f, Pitch = 0.9f }, npc.Center); // Sonido eléctrico bajo
                        for (int d = 0; d < 3; d++) Dust.NewDust(npc.position, npc.width, npc.height, DustID.Electric, 0, 0, 0, default, 0.6f);
                    }
                }
            }
        }

        private void TryShootLaser(Vector2 destination)
        {
            if (fireCooldown > 0) return;

            Player owner = Main.player[Projectile.owner];
            float summonMult = owner.GetDamage(DamageClass.Summon).Additive;
            float cooldownMult = MathHelper.Clamp(1f / summonMult, 0.2f, 2f);
            fireCooldown = (int)(120 * cooldownMult);

            int extraPenetrate = owner.maxMinions;
            Vector2 shootVelocity = (destination - Projectile.Center).SafeNormalize(Vector2.UnitX) * 50f;
            Vector2 shootFrom = Projectile.Center + shootVelocity.SafeNormalize(Vector2.UnitX) * 14f - new Vector2(0, 8f);

            SoundEngine.PlaySound(SoundID.Item12, Projectile.position);

            int proj = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                shootFrom,
                shootVelocity,
                ModContent.ProjectileType<LaserShot>(),
                15,
                0f,
                owner.whoAmI
            );
            Main.projectile[proj].DamageType = DamageClass.Summon;
            Main.projectile[proj].penetrate = 1 + extraPenetrate;
        }

          // --- NUEVA FUNCIÓN: Disparar Láser Especial ---
        private void TryShootSpecialLaser(Vector2 destination)
        {
            Player owner = Main.player[Projectile.owner];

            // Calcular dirección y posición de disparo (puede ser la misma que el láser normal o diferente)
             Vector2 shootDirection = (destination - Projectile.Center).SafeNormalize(Vector2.UnitX * Projectile.spriteDirection);
             // Posición de disparo ajustada para el láser especial (quizás más centrada?)
             Vector2 shootFrom = Projectile.Center + shootDirection * 20f; // Ajusta el offset

            

            // Spawnea el nuevo proyectil láser
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis("SpecialLaser"), // Fuente diferente
                shootFrom,
                shootDirection, // El proyectil láser especial calculará su propio camino
                ModContent.ProjectileType<SteamerSpecialLaser>(), // <<<--- NUEVO TIPO DE PROYECTIL
                45, // Daño del láser especial (ajusta)
                2f, // Knockback (ajusta)
                owner.whoAmI
                // Puedes pasar información extra usando ai[] si el láser la necesita
            );
        }

        private NPC FindEnemyWithGrenade()
        {
            foreach (NPC npc in Main.npc)
            {
                if (!npc.active || npc.friendly) continue;

                foreach (Projectile proj in Main.projectile)
                {
                    if (proj.active && proj.type == ModContent.ProjectileType<SteamerGrenade>() &&
                        proj.ModProjectile is SteamerGrenade grenade &&
                        grenade.IsStuckTo(npc))
                    {
                        return npc;
                    }
                }
            }
            return null;
        }

        private Projectile FindGrenadeTarget()
        {
            foreach (Projectile proj in Main.projectile)
            {
                if (proj.active && proj.type == ModContent.ProjectileType<SteamerGrenade>())
                {
                    return proj;
                }
            }
            return null;
        }

        private float LerpRotation(float current, float target, float speed)
        {
            float difference = MathHelper.WrapAngle(target - current);
            return current + difference * speed;
        }

        private void AnimateBody()
        {
            bodyFrameCounter++;
            if (bodyFrameCounter >= 6)
            {
                bodyFrameCounter = 0;
                bodyFrame = (bodyFrame + 1) % MaxBodyFrames;
            }
        }

        private void AnimateHead()
        {
            headFrameCounter++;
            if (headFrameCounter >= 6)
            {
                headFrameCounter = 0;
                headFrame = (headFrame + 1) % MaxHeadFrames;
            }
        }

        private NPC FindClosestEnemy(float range)
        {
            NPC closest = null;
            float minDist = range;

            foreach (NPC npc in Main.npc)
            {
                if (npc.CanBeChasedBy() && Vector2.Distance(Projectile.Center, npc.Center) < minDist)
                {
                    minDist = Vector2.Distance(Projectile.Center, npc.Center);
                    closest = npc;
                }
            }

            return closest;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            SpriteBatch sb = Main.spriteBatch;

            Texture2D bodyTexture = ModContent.Request<Texture2D>("WakfuMod/Content/Projectiles/SteamerTurretBody").Value;
            Texture2D headTexture = ModContent.Request<Texture2D>("WakfuMod/Content/Projectiles/SteamerTurretHead").Value;

            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            Rectangle bodySource = new Rectangle(0, bodyFrame * (bodyTexture.Height / MaxBodyFrames), bodyTexture.Width, bodyTexture.Height / MaxBodyFrames);
            Rectangle headSource = new Rectangle(0, headFrame * (headTexture.Height / MaxHeadFrames), headTexture.Width, headTexture.Height / MaxHeadFrames);

            Vector2 bodyOrigin = new Vector2(bodyTexture.Width / 2f, bodySource.Height / 2f - 6f);
            Vector2 headOrigin = new Vector2(headTexture.Width / 2f, headSource.Height / 2f + 1f);

            sb.Draw(bodyTexture, drawPos, bodySource, lightColor, 0f, bodyOrigin, 1f, SpriteEffects.None, 0f);

            SpriteEffects flip = Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            sb.Draw(
                headTexture,
                drawPos,
                headSource,
                lightColor,
                headRotationActual,
                headOrigin,
                1f,
                flip,
                0f
            );

            return false;
        }
    }
}
