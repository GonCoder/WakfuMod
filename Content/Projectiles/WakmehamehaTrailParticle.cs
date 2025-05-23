// WakmehamehaTrailParticle.cs (Content/Projectiles)
using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.ID;
using Microsoft.Xna.Framework.Graphics; // Para dibujar

namespace WakfuMod.Content.Projectiles
{
    // Partícula visible y dañina dejada por el líder
    public class WakmehamehaTrailParticle : ModProjectile
    {
        private const int Lifetime = 120; // Cuánto tiempo permanece visible/activo el rastro (ajusta)
        private const int HitCooldown = 30; // Cooldown entre golpes del rastro al mismo NPC

        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.ShadowBeamFriendly; // Usa una textura vanilla como placeholder o tu propia textura!

        public override void SetDefaults()
        {
            Projectile.width = 30; // Tamaño de la hitbox/visual del rastro
            Projectile.height = 30;
            Projectile.friendly = true; // Este SÍ hace daño
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.alpha = 0; // Empieza visible
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Ranged; // DEBE COINCIDIR con el arma
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = HitCooldown;
            Projectile.velocity = Vector2.Zero; // Es estacionario
            Projectile.damage = 1;
            Projectile.knockBack = 0f; // Establecer knockback explícito
        }

        public override void AI()
        {
            // 1. Efecto de Fade Out
            Projectile.alpha = (int)MathHelper.Lerp(0, 255, (float)(Lifetime - Projectile.timeLeft) / Lifetime);

            // 2. Efecto de Polvo/Luz (Opcional)
            if (Main.rand.NextBool(4))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(Projectile.width * 0.4f, Projectile.height * 0.4f),
                                             DustID.MagicMirror, Vector2.Zero, 180, Color.LightCyan, 0.8f);
                d.noGravity = true;
                d.velocity *= 0.1f;
            }
            Lighting.AddLight(Projectile.Center, Color.Cyan.ToVector3() * 0.3f * (1f - Projectile.alpha / 255f));
        }

         // --- NUEVO: Hook para modificar el daño ---
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            // --- 1. Definir el porcentaje de daño ---
            // Ajusta este valor (ej: 0.015f = 1.5% ahora hace 0.3%)
            float percentOfMaxLife = 0.003f;

            // --- 2. Calcular el daño ---
            int calculatedDamage = 1 + (int)(target.lifeMax * percentOfMaxLife);

            // --- 3. Establecer el daño base ---
            modifiers.SourceDamage.Base = calculatedDamage;

            // --- 4. Ignorar defensa ---
            modifiers.DefenseEffectiveness *= 0f;

            // --- 5. Anular knockback (ya debería ser 0, pero por si acaso) ---
             modifiers.Knockback.Base = 0f;

            // --- Opcional: Deshabilitar críticos ---
            // modifiers.DisableCrit();
        }

        // 3. Dibujo Personalizado (Opcional, si no usas una textura existente)
        // Si usas tu propia textura, puedes mantener el PreDraw / PostDraw que tenías
        // Si usas la textura vanilla como arriba, este método no es estrictamente necesario
        // a menos que quieras cambiar el color o la rotación.
        /*
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Rectangle frame = texture.Frame();
            Vector2 origin = frame.Size() / 2f;
            Color color = Projectile.GetAlpha(Color.Lerp(Color.Cyan, Color.White, 0.5f)); // Color con alpha

            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                frame,
                color,
                Projectile.rotation, // Puede rotar aleatoriamente? O fijo?
                origin,
                Projectile.scale, // Escala si es necesario
                SpriteEffects.None,
                0f);

            return false; // No dibujar el sprite por defecto
        }
        */

         // No necesita Colliding complejo, la hitbox es suficiente
         // public override bool? Colliding(...) { ... }
    }
}