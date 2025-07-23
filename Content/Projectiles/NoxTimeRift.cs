using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using WakfuMod.Content.NPCs.Bosses.Nox; // Para excluir a Nox y Noxinas de la ralentización
using Terraria.Graphics.Effects; // Para Filters
using WakfuMod.jugador; // Para ScreenShaderData

namespace WakfuMod.Content.Projectiles
{
    public class NoxTimeRift : ModProjectile
    {
        // --- Constantes del Proyectil ---
        private const int TotalAnimationFrames = 3; // Frames en tu spritesheet para la animación del domo
        private const int AnimationSpeed = 15;      // Ticks por frame, para una animación lenta y ondulante
        private const float SlowdownFactor = 0.85f; // 15% de slow

        // --- Constantes del Shader de Onda ---

        private const int Lifetime = 10 * 60; // 10 segundos
        private const int ShockwaveDuration = 120; // 2 segundos para que la onda se expanda
        private const string ShockwaveFilterName = "WakfuMod:NoxShockwave"; // Nombre único para nuestro filtro

        // --- Propiedad para la Textura ---
        // Asegúrate de que la ruta sea correcta y que el archivo PNG esté en Assets/Projectiles/
        public override string Texture => "WakfuMod/Content/Projectiles/NoxTimeRift";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = TotalAnimationFrames;
        }

        public override void SetDefaults()
        {
            Projectile.width = 2000;
            Projectile.height = 2000;
            Projectile.friendly = true; // Necesario para detectar colisiones
            Projectile.hostile = true;  // Necesario para detectar colisiones
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.alpha = 20; // Semitransparente
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.aiStyle = -1;
            Projectile.damage = 0; // No hace daño directo
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;

            // --- LÓGICA DE ANIMACIÓN (AQUÍ ES DONDE DEBE ESTAR) ---
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= AnimationSpeed)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
                if (Projectile.frame >= Main.projFrames[Projectile.type]) // Usar Main.projFrames es más robusto
                {
                    Projectile.frame = 0; // Loopear la animación
                }
            }
            // --- FIN LÓGICA DE ANIMACIÓN ---

            UpdateShockwave();
            ApplySlowdownEffect();
        }

        private void UpdateShockwave()
        {
            // Solo ejecutar en cliente
            if (Main.netMode == NetmodeID.Server) return;

            float totalLifetime = Lifetime;
            float timeElapsed = totalLifetime - Projectile.timeLeft;

            // Activar/Actualizar el shader durante los primeros segundos
            if (timeElapsed < ShockwaveDuration)
            {
                float progress = timeElapsed / ShockwaveDuration;

                Filters.Scene.Activate(ShockwaveFilterName, Projectile.Center)
                    .GetShader()
                    .UseProgress(progress)
                    .UseColor(0.3f, 0.8f, 1.0f) // Color cian semitransparente
                    .UseTargetPosition(Projectile.Center);
            }
            else // Si la animación de la onda ya terminó, asegurarse de que está desactivada
            {
                if (Filters.Scene[ShockwaveFilterName].IsActive())
                {
                    Filters.Scene[ShockwaveFilterName].Deactivate();
                }
            }
        }

        private void ApplySlowdownEffect()
        {
            // Iterar por todos los jugadores
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player.active && !player.dead && Projectile.Hitbox.Intersects(player.Hitbox))
                {
                    // Ralentizar al jugador
                    // Para que sea más suave, aplicamos un factor en lugar de multiplicar directamente
                    player.velocity *= SlowdownFactor;
                    player.delayUseItem = true;
                    // --- AÑADIDO: Activar el flag en el ModPlayer ---
                    if (player.TryGetModPlayer<TimeRiftPlayer>(out var timeRiftPlayer))
                    {
                        timeRiftPlayer.isInTimeRift = true;
                    }
                }

            }

            // Iterar por todos los NPCs
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                // Excluir a Nox, sus noxinas, y NPCs que no deben ser afectados
                if (npc.active && !npc.friendly && npc.type != ModContent.NPCType<Nox>() && npc.type != ModContent.NPCType<Noxine>())
                {
                    if (Projectile.Hitbox.Intersects(npc.Hitbox))
                    {
                        npc.velocity *= 0.1f;
                    }
                }
            }

            // Iterar por todos los proyectiles
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                // Excluir los proyectiles del propio jefe y este mismo proyectil
                if (proj.active)
                {
                    if (Projectile.Hitbox.Intersects(proj.Hitbox))
                    {
                        proj.velocity *= SlowdownFactor;
                    }
                }
            }
        }

        // No queremos que haga daño directo, la ralentización es el efecto
        public override bool? CanHitNPC(NPC target) => false;
        public override bool CanHitPlayer(Player target) => false;

        // Limpiar el shader cuando el proyectil se destruye
        public override void OnKill(int timeLeft)
        {
            if (Main.netMode != NetmodeID.Server)
            {
                if (Filters.Scene[ShockwaveFilterName].IsActive())
                {
                    Filters.Scene[ShockwaveFilterName].Deactivate();
                }
            }
        }

        // El dibujado del domo se hace con el sistema por defecto de Terraria
        // basado en la textura, el frame actual y el alpha.
        // Si quieres un efecto de dibujado especial, lo harías aquí.
        public override bool PreDraw(ref Color lightColor)
        {
            // Cargar nuestro spritesheet de animación
            Texture2D animationTexture = ModContent.Request<Texture2D>("WakfuMod/Content/Projectiles/NoxTimeRift").Value;

            // --- OBTENER EL FRAME ACTUAL (que se calcula en AI) ---
            // 'Projectile.frame' ya tiene el índice del frame actual (0, 1, o 2)
            int currentFrameIndex = Projectile.frame;

            // Calcular el sourceRectangle para cortar el frame del spritesheet
            int frameHeight = animationTexture.Height / TotalAnimationFrames;
            Rectangle sourceRect = new Rectangle(0, currentFrameIndex * frameHeight, animationTexture.Width, frameHeight);

            // Calcular escala para estirar el frame a la hitbox
            float scale = (float)Projectile.width / sourceRect.Width;

            Vector2 origin = sourceRect.Size() / 2f;
            Color color = lightColor * 0.03f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            Main.spriteBatch.Draw(
                animationTexture,
                drawPosition,
                sourceRect,
                color,
                0f, // Sin rotación
                origin,
                scale,
                SpriteEffects.None,
                0f);

            return false;
        }
    }
}