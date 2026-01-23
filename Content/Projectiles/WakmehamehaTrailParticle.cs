// WakmehamehaTrailParticle.cs (Content/Projectiles)
using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.ID;
using Microsoft.Xna.Framework.Graphics; // Para dibujar
using Terraria.GameContent; // Para TextureAssets

namespace WakfuMod.Content.Projectiles
{
    // Partícula visible y dañina dejada por el líder
    public class WakmehamehaTrailParticle : ModProjectile
    {
        private const int Lifetime = 20; // 0.3 segundos aprox (60 ticks/s)
        private const int HitCooldown = 30; // Cooldown entre golpes del rastro al mismo NPC

        // No usamos Texture => ... porque dibujamos manualmente

        public override void SetDefaults()
        {
            Projectile.width = 30; // Tamaño de la hitbox/visual del rastro
            Projectile.height = 30;
            Projectile.friendly = true; // Este SÍ hace daño
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.alpha = 255; // Invisible (ahora el líder se encarga de lo visual o PreDraw)
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
            // Sin lógica compleja, solo reducir timeLeft
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Usamos MagicPixel para garantizar que se vea una línea sólida
            Texture2D tex = TextureAssets.MagicPixel.Value;
            
            float rotation = Projectile.ai[0];
            float length = Projectile.ai[1];
            
            if (length <= 0) length = 10f; 

            // MagicPixel es 1x1. Escala X = longitud, Escala Y = grosor
            Vector2 scale = new Vector2(length, 12f); 
            Vector2 origin = new Vector2(0, 0.5f); 
            
            // Fade out
            float alphaFactor = (float)Projectile.timeLeft / Lifetime;
            
            // Ajustamos color: NO forzar A=255. 
            // Queremos que se vuelva transparente, no negro.
            Color colorOuter = Color.Teal * alphaFactor;
            
            // Núcleo un poco más brillante pero del mismo tono o blanco
            Color colorInner = Color.White * alphaFactor;

            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            // Dibujar borde externo (Teal)
            Main.EntitySpriteDraw(
                tex,
                drawPos,
                new Rectangle(0,0,1,1),
                colorOuter,
                rotation,
                origin,
                scale,
                SpriteEffects.None,
                0
            );

            // Dibujar núcleo interno (más fino)
            Main.EntitySpriteDraw(
                tex,
                drawPos,
                new Rectangle(0,0,1,1),
                colorInner,
                rotation,
                origin,
                scale * new Vector2(1f, 0.3f), // Núcleo más fino todavía
                SpriteEffects.None,
                0
            );

            return false;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            Player player = Main.player[Projectile.owner];
            
            // ELIMINADO LOGICA ANTIGUA (Balance/Original)
            // AHORA: Usamos el daño base que viene del Projectile.
            // Este daño ya fue calculado en el arma (WakmehamehaWeapon.ModifyShootStats)
            // con la fórmula del usuario (+30 por cada 5% de daño a distancia).
            
            modifiers.SourceDamage.Base = Projectile.damage;
            
            // Mantener ignorar defensa si se desea que sea daño puro
            modifiers.DefenseEffectiveness *= 0f; 
        }

        // --- Cleaned up old code remnants ---
    }
}