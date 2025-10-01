using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System;
using Terraria.Audio;

namespace WakfuMod.Content.Projectiles
{
    public class EcaflipTongueLash : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.width = 150; // Largo de la lengua
            Projectile.height = 30; // Ancho
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.timeLeft = 15; // Vida útil muy corta
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20; // Solo un golpe por enemigo
            Projectile.DamageType = DamageClass.Summon;
        }

         public override void AI()
        {
            Player owner = Main.player[Projectile.owner];

            // --- Lógica de Estiramiento y Rotación (SIN CAMBIOS) ---
            Vector2 directionToCursor = (Main.MouseWorld - owner.Center).SafeNormalize(Vector2.UnitX);
            float distanceToCursor = Vector2.Distance(owner.Center, Main.MouseWorld);
            float maxDistance = 600f;
            float actualLength = Math.Min(distanceToCursor, maxDistance);

            Projectile.Center = owner.Center + directionToCursor * (actualLength / 2f);
            Projectile.rotation = directionToCursor.ToRotation();

            // --- EFECTO VISUAL DE LA LENGUA (POLVO ROSA) - CORREGIDO ---
            // Generar polvo a lo largo de toda la lengua
            for (int i = 0; i < 5; i++) // Aumentar un poco la cantidad de polvo para que se vea bien
            {
                // 1. Elegir una distancia aleatoria a lo largo de la lengua
                float randomDistance = Main.rand.NextFloat(actualLength);

                // 2. Calcular la posición base en ese punto de la línea
                Vector2 baseDustPos = owner.Center + directionToCursor * randomDistance;

                // 3. Añadir un pequeño offset perpendicular para que no sea una línea perfecta
                // 'directionToCursor.RotatedBy(MathHelper.PiOver2)' da un vector a 90 grados.
                float perpendicularOffset = Main.rand.NextFloat(-Projectile.height / 2f, Projectile.height / 2f);
                Vector2 dustPos = baseDustPos + directionToCursor.RotatedBy(MathHelper.PiOver2) * perpendicularOffset;

                // 4. Crear el polvo
                Dust.NewDustPerfect(
                    dustPos,
                    DustID.PinkFairy,
                    Projectile.velocity * 0.1f, // Velocidad del polvo (casi nula)
                    150, // Alpha
                    Color.HotPink,
                    1.5f // Escala
                );
            }
        }

        // --- SOBREESCRIBIR COLISIÓN PARA HITBOX DINÁMICA ---
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Player owner = Main.player[Projectile.owner];
            Vector2 start = owner.Center;
            Vector2 direction = (Main.MouseWorld - owner.Center).SafeNormalize(Vector2.UnitX);
            float maxDistance = 600f;
            float distanceToCursor = Vector2.Distance(owner.Center, Main.MouseWorld);
            float actualLength = Math.Min(distanceToCursor, maxDistance);
            Vector2 end = start + direction * actualLength;

            float _collisionPoint = 0f; // No necesitamos este valor
            // Comprobar si la hitbox del objetivo choca con una línea que va del jugador al cursor
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, Projectile.height, ref _collisionPoint);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            // Daño del 5% de la vida máxima
            modifiers.SourceDamage.Base = target.lifeMax * 0.05f;
            // Knockback muy fuerte
            modifiers.Knockback.Base = 18f;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            Player owner = Main.player[Projectile.owner];
            // Solo afectar a jugadores del mismo equipo que no sean el propio lanzador
            if (target.team == owner.team && target.whoAmI != owner.whoAmI)
            {
                // Curar el 50% de la vida máxima del OBJETIVO
                int healAmount = target.statLifeMax2 / 2;
                target.Heal(healAmount);
                CombatText.NewText(target.getRect(), Color.LawnGreen, $"+{healAmount}");
                SoundEngine.PlaySound(SoundID.Item4, target.Center);

                // Atraer al jugador hacia el lanzador
                Vector2 pullDirection = owner.Center - target.Center;
                pullDirection.Normalize();
                target.velocity = pullDirection * 16f; // Velocidad de atracción

                // TODO: Sincronizar el cambio de velocidad en multijugador si es necesario.
            }
        }

        public override bool? CanHitNPC(NPC target)
        {
            // Evitar golpear a los Town NPCs
            return !target.townNPC;
        }

        public override bool CanHitPlayer(Player target)
        {
            // Permitir golpear a jugadores del mismo equipo (y que no seas tú mismo)
            return target.team == Main.player[Projectile.owner].team && target.whoAmI != Projectile.owner;
        }
    }
}