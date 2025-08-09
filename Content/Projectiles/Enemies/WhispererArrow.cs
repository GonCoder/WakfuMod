using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.ID;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent; // Para TextureAssets

namespace WakfuMod.Content.Projectiles.Enemies
{
    public class WhispererArrow : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 24; 
            Projectile.height = 38;
            Projectile.aiStyle = 1; // Usar la IA de flecha
            AIType = ProjectileID.WoodenArrowFriendly; // Heredar comportamiento de flecha
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 600;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.damage = 15;
            Projectile.ArmorPenetration = 30;
        }

        public override void AI()
        {
            // Opcional: Mantener efectos visuales
            Lighting.AddLight(Projectile.Center, Color.White.ToVector3() * 0.4f);
            if (Main.rand.NextBool(3)) // Menos polvo para mejor rendimiento
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.WhiteTorch, Vector2.Zero, 0, default, 1.2f);
                dust.noGravity = true;
            }
        }

        // --- MÉTODO PreDraw CORREGIDO ---
        public override bool PreDraw(ref Color lightColor)
        {
            // Obtener la textura que Terraria ya cargó para este proyectil
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;

            // El origen es el centro de la textura
            Vector2 origin = texture.Size() / 2f;

            // La posición de dibujado es el centro de la hitbox
            Vector2 drawPos = Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY);

            // Escala (puedes ajustar si es necesario)
            float scale = 0.4f;
            float rotation = Projectile.rotation; // Usa la rotación calculada por aiStyle=1
            SpriteEffects spriteEffects = SpriteEffects.None;

            Main.spriteBatch.Draw(
                texture,
                drawPos,
                null, // null para usar la textura completa
                lightColor, // Usar el color de la iluminación del entorno
                rotation,
                origin,
                scale,
                spriteEffects,
                0f
            );

            return false; // Hemos hecho el dibujado, no dejar que Terraria lo haga
        }
    }
}