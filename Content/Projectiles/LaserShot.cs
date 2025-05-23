// LaserShot.cs
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System;
using WakfuMod.Content.NPCs; // Si lo necesitas para SteamerGrenade
using Terraria.DataStructures;
using Terraria.Audio;

namespace WakfuMod.Content.Projectiles
{
    public class LaserShot : ModProjectile
    {
        // --- Constantes y Cache ---
        private const float HomingRange = 80f; // Aumentar un poco el rango?
        private const float TurnStrength = 0.4f; // Qué tan rápido gira hacia el objetivo (0 a 1, valores bajos son más suaves)
        private const float BaseSpeed = 18f; // Velocidad constante deseada del proyectil
        private const float ExplosionRadius = 80f; // Radio de la explosión final
        private const int ExplosionDustCount = 85; // Cantidad de polvo en la explosión

        public override void SetStaticDefaults() {
             ProjectileID.Sets.TrailCacheLength[Type] = 8;
             ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults() {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = true; // Puede ser false si quieres que atraviese bloques al seguir
            Projectile.timeLeft = 300;
            Projectile.penetrate = 1; // Se ajusta en OnSpawn
            Projectile.DamageType = DamageClass.Summon;
            Projectile.light = 0.3f;
            Projectile.alpha = 50;
            Projectile.extraUpdates = 1; // Mantiene la velocidad extra
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void OnSpawn(IEntitySource source) {
             if (Main.player.IndexInRange(Projectile.owner)) {
                 Player player = Main.player[Projectile.owner];
                 Projectile.penetrate = 1 + player.maxMinions;
             } else {
                 Projectile.penetrate = 1;
             }
             // --- Asegurar Velocidad Inicial ---
             // Si la velocidad inicial es muy baja o cero, darle la velocidad base
             if (Projectile.velocity.LengthSquared() < 1f) {
                 Projectile.velocity = (Main.MouseWorld - Projectile.Center).SafeNormalize(Vector2.UnitX) * BaseSpeed;
             } else {
                 // Normalizar y aplicar velocidad base para asegurar consistencia
                 Projectile.velocity = Vector2.Normalize(Projectile.velocity) * BaseSpeed;
             }
        }

        // --- Lógica de Movimiento (Homing por Rotación) y Partículas ---
        public override void AI()
        {
            // --- Homing Suave por Rotación ---
            NPC target = FindClosestEnemy(HomingRange);
            if (target != null)
            {
                // 1. Vector y ángulo hacia el objetivo
                Vector2 vectorToTarget = target.Center - Projectile.Center;
                float angleToTarget = vectorToTarget.ToRotation();

                // 2. Ángulo actual del proyectil
                float currentAngle = Projectile.velocity.ToRotation();

                // 3. Interpolar suavemente el ángulo
                // Usamos WrapAngle para manejar correctamente el paso de -Pi a Pi
                float targetAngleWrapped = MathHelper.WrapAngle(angleToTarget);
                float currentAngleWrapped = MathHelper.WrapAngle(currentAngle);
                float interpolatedAngle = MathHelper.Lerp(currentAngleWrapped, targetAngleWrapped, TurnStrength);

                // Asegura que la interpolación tome el camino más corto alrededor del círculo
                interpolatedAngle = currentAngleWrapped + MathHelper.WrapAngle(targetAngleWrapped - currentAngleWrapped) * TurnStrength;


                // 4. Reconstruir el vector de velocidad con el nuevo ángulo y velocidad constante
                Projectile.velocity = interpolatedAngle.ToRotationVector2() * BaseSpeed;
            }
            // Si no hay objetivo, simplemente sigue recto con la velocidad actual (que debería ser BaseSpeed)

            // --- Rotación del Sprite ---
            // Apuntar en la dirección del movimiento
            Projectile.rotation = Projectile.velocity.ToRotation(); // + MathHelper.PiOver2; // Añade PiOver2 si tu sprite apunta hacia arriba en lugar de a la derecha

            // --- Partículas ---
            if (Main.rand.NextBool(2)) {
                int dustType = DustID.VilePowder;
                Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, dustType, 0f, 0f, 100, default, 1.2f);
                dust.velocity *= 0.3f;
                dust.noGravity = true;
                dust.alpha = 100;
            }
        }

        // --- Buscar Enemigo Cercano (Sin cambios) ---
        private NPC FindClosestEnemy(float maxDist) {
            NPC closest = null;
            float minDstSq = maxDist * maxDist;
            for (int i=0; i < Main.maxNPCs; i++) {
                 NPC npc = Main.npc[i];
                 if (npc.CanBeChasedBy(Projectile, false)) {
                     float distSq = Projectile.DistanceSQ(npc.Center);
                     if (distSq < minDstSq) {
                         // --- Opcional: Chequeo de línea de visión ---
                        //  Descomenta esto si quieres que SOLO siga a enemigos visibles
                         if (Collision.CanHitLine(Projectile.position, Projectile.width, Projectile.height, npc.position, npc.width, npc.height)) {
                             closest = npc;
                             minDstSq = distSq;
                         }
                     }
                 }
            }
            return closest;
        }


       // --- OnHitNPC (Sin cambios respecto a tu versión anterior) ---
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
            int extraDamage = (int)(target.lifeMax * 0.01f); // Ajusta el porcentaje si lo cambiaste
            Player owner = Main.player[Projectile.owner];
            owner.ApplyDamageToNPC(target, extraDamage, 0f, hit.HitDirection, false, Projectile.DamageType);

            for (int i = 0; i < Main.maxProjectiles; i++) {
                Projectile proj = Main.projectile[i];
                // ASUNCIÓN: SteamerGrenade tiene el método IsStuckTo(NPC target)
                if (proj.active && proj.owner == Projectile.owner && proj.ModProjectile is SteamerGrenade grenade) {
                     if (grenade.IsStuckTo(target)) { // Llama al método
                         proj.ai[0] = 1f;
                         proj.Kill();
                         break;
                     }
                }
            }
        }

         // --- NUEVO MÉTODO: Lógica de la Explosión ---
        private void Explode()
        {
            // 1. Sonido
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.2f, Pitch = 0.9f }, Projectile.position); // Sonido de explosión pequeño

            // 2. Efectos Visuales (Polvo Morado)
            if (Main.netMode != NetmodeID.Server) // Solo en cliente
            {
                for (int i = 0; i < ExplosionDustCount; i++)
                {
                    // DustID.PurpleTorch, DustID.Shadowflame, DustID.Corruption, DustID.PsychosymaticBolt... elige uno que te guste
                    int dustType = DustID.Clentaminator_Purple;
                    Dust dust = Dust.NewDustPerfect(Projectile.Center, dustType, Main.rand.NextVector2Circular(3f, 3f), 100, default, 1.5f); // Velocidad y escala
                    dust.noGravity = true;
                    dust.velocity *= 0.8f; // Deceleración
                }
            }

            // 3. Lógica de Daño (Solo si el dueño es el jugador local o en servidor)
            if (Main.myPlayer == Projectile.owner || Main.netMode == NetmodeID.Server)
            {
                Player owner = Main.player[Projectile.owner];
                float radiusSq = ExplosionRadius * ExplosionRadius; // Comparar cuadrados

                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];
                    // Comprobar si el NPC es válido, hostil y está dentro del radio
                    if (npc.active && !npc.friendly && npc.CanBeChasedBy(Projectile, false) && Projectile.DistanceSQ(npc.Center) < radiusSq)
                    {
                        // Calcula el 1% de la vida máxima
                        int percentDamage = (int)(npc.lifeMax * 0.01f);
                        // Asegurarse de que haga al menos 1 de daño
                        if (percentDamage < 1) percentDamage = 1;

                        // Dirección del knockback (desde el centro de la explosión)
                        int hitDirection = Math.Sign(npc.Center.X - Projectile.Center.X);
                        if (hitDirection == 0) hitDirection = 1; // Evitar dirección 0

                        // Aplicar el daño % (sin daño base adicional de la explosión)
                        // Usa el DamageType del proyectil
                        // Knockback bajo o nulo para la explosión pequeña?
                        float knockback = 0.5f;
                        owner.ApplyDamageToNPC(npc, percentDamage, knockback, hitDirection, false, Projectile.DamageType);
                    }
                }
            }
        }


        // --- NUEVO OVERRIDE: Kill ---
        // Este método se llama cuando Projectile.timeLeft llega a 0 o cuando Projectile.Kill() es llamado.
        public override void Kill(int timeLeft)
        {
            // Llama a nuestra función de explosión personalizada
            Explode();

            // Puedes añadir aquí cualquier otra lógica de limpieza si es necesario
            // (La lógica base de Kill ya se encarga de cosas como matar el proyectil)
        }

        // --- Dibujado (Sin cambios respecto a tu versión anterior) ---
        public override bool PreDraw(ref Color lightColor) {
             Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
             Vector2 origin = texture.Frame().Size() / 2f;
             Color drawColor = Projectile.GetAlpha(lightColor);
             Vector2 scale = new Vector2(1.4f, 1.2f);
             SpriteEffects effects = SpriteEffects.None;

             if (ProjectileID.Sets.TrailCacheLength[Type] > 0) {
                 for (int k = 0; k < Projectile.oldPos.Length; k++) {
                     Vector2 drawPos = Projectile.oldPos[k] - Main.screenPosition + Projectile.Size / 2f;
                     float trailMult = (Projectile.oldPos.Length - k) / (float)Projectile.oldPos.Length;
                     Color trailColor = drawColor * trailMult * 0.5f;
                     Main.EntitySpriteDraw(texture, drawPos, texture.Frame(), trailColor, Projectile.oldRot[k], origin, scale * (0.8f * trailMult), effects, 0);
                 }
             }

             Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, texture.Frame(), drawColor, Projectile.rotation, origin, scale, effects, 0f);
             return false;
        }
        // --- Opcional: PostDraw para Glowmask ---
        /*
        public override void PostDraw(Color lightColor)
        {
            // Si tienes un LaserShot_Glow.png
             Texture2D glowTexture = ModContent.Request<Texture2D>(Texture + "_Glow").Value;
             Vector2 origin = glowTexture.Frame().Size() / 2f;
             Color glowColor = Color.White * Projectile.Opacity;
             Vector2 scale = new Vector2(1.4f, 1.2f);
             SpriteEffects effects = SpriteEffects.None;

             // Dibujar estela de glow (opcional)
             if (ProjectileID.Sets.TrailCacheLength[Type] > 0) {
                 for (int k = 0; k < Projectile.oldPos.Length; k++) {
                     Vector2 drawPos = Projectile.oldPos[k] - Main.screenPosition + Projectile.Size / 2f;
                     float trailMult = (Projectile.oldPos.Length - k) / (float)Projectile.oldPos.Length;
                     Color trailGlowColor = glowColor * trailMult * 0.7f;
                     Main.EntitySpriteDraw(glowTexture, drawPos, glowTexture.Frame(), trailGlowColor, Projectile.oldRot[k], origin, scale * (0.8f * trailMult), effects, 0);
                 }
             }

             // Dibujar glow principal
             Main.EntitySpriteDraw(
                 glowTexture,
                 Projectile.Center - Main.screenPosition,
                 glowTexture.Frame(),
                 glowColor,
                 Projectile.rotation,
                 origin,
                 scale,
                 effects,
                 0f);
        }
        */
    }
}