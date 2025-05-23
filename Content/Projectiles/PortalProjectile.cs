using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;

namespace WakfuMod.Content.Projectiles
{
    public class PortalProjectile : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 90;
            Projectile.height = 90;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = int.MaxValue;
            Projectile.alpha = 0; // Forzamos opacidad completa
        }

        public override void AI()
        {
            int frameSpeed = 5; // Cambia cada 5 ticks
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= frameSpeed)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
                if (Projectile.frame >= 4) // 4 frames en el spritesheet
                {
                    Projectile.frame = 0;
                }
            }
            // Gira lentamente el portal
            // Projectile.rotation += 0.1f;
            
            foreach (Projectile proj in Main.projectile)
            {
                if (proj.active && !proj.hostile && proj.friendly && proj.type != Projectile.type) // Sólo afecta proyectiles del jugador
                {
                    if (Vector2.Distance(proj.Center, Projectile.Center) < 45f) // Si está cerca del portal
                    {
                        TeleportProjectile(proj);
                    }
                }
            }
        }

        private static void TeleportProjectile(Projectile proj)
        {
            // Verificar que existan ambos portales para evitar problemas de referencia nula
            if (!PortalHandler.portal1.HasValue || !PortalHandler.portal2.HasValue)
                return;
            
            if (proj.ai[1] > 0) // Cooldown
            {
                proj.ai[1]--;
                return;
            }
            
            Vector2? targetPortal = null;

            if (Vector2.Distance(proj.Center, PortalHandler.portal1.Value) < 45f)
            {
                targetPortal = PortalHandler.portal2;
            }
            else if (Vector2.Distance(proj.Center, PortalHandler.portal2.Value) < 45f)
            {
                targetPortal = PortalHandler.portal1;
            }

            if (targetPortal.HasValue)
{
    proj.position = targetPortal.Value - new Vector2(proj.width / 2, proj.height / 2);
    proj.ai[1] = 60; // Cooldown de teletransporte
   proj.localAI[1] = 1f;
proj.netUpdate = true;

}

        }

        public override void PostDraw(Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>("WakfuMod/Content/Projectiles/PortalSheet").Value;
            int frameHeight = texture.Height / 4; // 4 frames en la hoja
            Rectangle sourceRectangle = new Rectangle(0, Projectile.frame * frameHeight, texture.Width, frameHeight);
            Vector2 origin = new Vector2(texture.Width / 2f, frameHeight / 2f);

            // Usamos un color fijo (teal) para asegurar visibilidad
            Color drawColor = Color.White * Projectile.Opacity;
            Main.EntitySpriteDraw(texture, 
                Projectile.Center - Main.screenPosition, 
                sourceRectangle, 
                drawColor, 
                Projectile.rotation, 
                origin, 
                0.5f, 
                SpriteEffects.None, 
                0);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return false; // Evita que se dibuje el sprite base
        }
    }
}
