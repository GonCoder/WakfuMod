// En Content/Projectiles/DieExplosion.cs

using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System;

namespace WakfuMod.Content.Projectiles
{
    public class DieExplosion : ModProjectile
    {
        // ai[0] = minDamagePercent * 10000
        // ai[1] = maxDamagePercent * 10000

        public override void SetDefaults()
        {
            // La hitbox se establecerá dinámicamente al spawnear.
            // Estos son solo valores por defecto.
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2; // Solo necesita existir por un instante para registrar golpes
            Projectile.alpha = 255; // Completamente invisible
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true; // Para que cada NPC sea golpeado solo una vez
            Projectile.localNPCHitCooldown = 5; // Un cooldown corto es suficiente
            Projectile.DamageType = DamageClass.Generic; // No escala con bonificaciones
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            // Leer los porcentajes de daño pasados en los campos de IA
            float minDamagePercent = Projectile.ai[0] / 10000f;
            float maxDamagePercent = Projectile.ai[1] / 10000f;

            float randomPercent = Main.rand.NextFloat(minDamagePercent, maxDamagePercent);
            int calculatedDamage = 1 + (int)(target.lifeMax * randomPercent);

            modifiers.SourceDamage.Base = calculatedDamage; // Establecer el daño
            modifiers.DefenseEffectiveness *= 0f; // Ignorar defensa
            modifiers.Knockback.Base = 2f; 
            modifiers.DisableCrit(); // El daño porcentual no debería ser crítico
        }

        // No necesita AI, ni OnSpawn, ni Colliding.
        // El sistema de colisión estándar funcionará si le damos un tamaño grande.
        // public override void AI() { }

        // public override bool PreDraw(ref Color lightColor) => false; // No dibujar nada
    }
}