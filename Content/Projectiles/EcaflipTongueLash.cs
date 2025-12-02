using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System;
using Terraria.Audio;
using WakfuMod.jugador; // Para WakfuPlayer

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

            // --- Lógica de Estiramiento y Rotación (CORREGIDO PARA MULTIJUGADOR) ---
            // Usamos Projectile.velocity para la dirección y Projectile.ai[0] para la distancia
            Vector2 directionToCursor = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float distanceToCursor = Projectile.ai[0];
            float maxDistance = 600f;
            float actualLength = Math.Min(distanceToCursor, maxDistance);

            Projectile.Center = owner.Center + directionToCursor * (actualLength / 2f);
            Projectile.rotation = directionToCursor.ToRotation();

            // --- DETECTAR JUGADORES (Manual Collision Check) ---
            // Solo el dueño del proyectil realiza la detección para evitar duplicados
            if (Projectile.owner == Main.myPlayer && Projectile.localAI[0] == 0) 
            {
                Vector2 start = owner.Center;
                Vector2 end = start + directionToCursor * actualLength;
                float _collisionPoint = 0f;

                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player target = Main.player[i];
                    // Verificar: Activo, No muerto, No es el dueño, Mismo equipo
                    if (target.active && !target.dead && target.whoAmI != owner.whoAmI && target.team == owner.team)
                    {
                        // Chequear colisión con la línea de la lengua
                        if (Collision.CheckAABBvLineCollision(target.Hitbox.TopLeft(), target.Hitbox.Size(), start, end, Projectile.height, ref _collisionPoint))
                        {
                            // Calcular dirección de atracción
                            Vector2 pullDirection = owner.Center - target.Center;
                            if (pullDirection != Vector2.Zero) pullDirection.Normalize();
                            Vector2 newVelocity = pullDirection * 16f;

                            // Enviar paquete al servidor (o al objetivo si somos host)
                            ModPacket packet = Mod.GetPacket();
                            packet.Write((byte)WakfuMod.MessageType.PullPlayer);
                            packet.Write((byte)target.whoAmI);
                            packet.WriteVector2(newVelocity);
                            packet.Send();

                            // Curación en Modo Balance
                            if (owner.GetModPlayer<WakfuPlayer>().BalanceMode)
                            {
                                target.Heal(100);
                                CombatText.NewText(target.getRect(), Color.Green, "+100 HP");
                            }

                            // Marcar como usado para no spamear paquetes (un golpe por uso)
                            Projectile.localAI[0] = 1; 
                            
                            // Efectos visuales locales
                            SoundEngine.PlaySound(SoundID.Item4, target.Center);
                            break; // Solo un jugador por frame/uso
                        }
                    }
                }
            }

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
            
            // Usar los mismos valores sincronizados que en AI
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float maxDistance = 600f;
            float distanceToCursor = Projectile.ai[0];
            float actualLength = Math.Min(distanceToCursor, maxDistance);
            
            Vector2 end = start + direction * actualLength;

            float _collisionPoint = 0f; // No necesitamos este valor
            // Comprobar si la hitbox del objetivo choca con una línea que va del jugador al cursor
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, Projectile.height, ref _collisionPoint);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            Player owner = Main.player[Projectile.owner];
            if (owner.GetModPlayer<WakfuPlayer>().BalanceMode)
            {
                modifiers.SourceDamage.Base = 50; // Daño base fijo
                // El daño se escalará con Summon porque Projectile.DamageType es Summon
            }
            else
            {
                // Daño del 5% de la vida máxima
                modifiers.SourceDamage.Base = target.lifeMax * 0.05f;
            }
            // Knockback muy fuerte
            modifiers.Knockback.Base = 18f;
        }

        public override bool? CanHitNPC(NPC target)
        {
            // Evitar golpear a los Town NPCs
            return !target.townNPC;
        }
    }
}