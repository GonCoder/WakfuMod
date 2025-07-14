// En Content/Projectiles/ZurcarakDie.cs
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Audio;
using ReLogic.Content;
using WakfuMod.ModSystems;
using System.IO;
using Terraria.DataStructures;

namespace WakfuMod.Content.Projectiles
{
    public class ZurcarakDie : ModProjectile
    {
        // --- Constantes ---
        private const int TotalAnimationFrames = 9; // 9 frames por spritesheet
        private const int AnimationSpeed = 7;      // Ticks por frame
        private const int AnimationDuration = AnimationSpeed * (TotalAnimationFrames - 1); // Duración HASTA LLEGAR al último frame

        private const int LingerDuration = 60; // 60 ticks = 1 segundo que el último frame permanece visible,
        //  esto sirve para que el último frame dure un segundo más en la animación

        // --- Variables de Estado ---
        // ai[0] almacena el resultado final del dado (1-6). Se sincroniza.
        private int FinalResult { get => (int)Projectile.ai[0]; set => Projectile.ai[0] = value; }
        public bool _effectActivated = false; // Para que el efecto se active solo una vez

        // --- Almacén de Texturas ---
        private static Asset<Texture2D>[] _dieTextures;

        public override void Load()
        {
            if (!Main.dedServ)
            {
                _dieTextures = new Asset<Texture2D>[7]; // Índices 1-6
                for (int i = 1; i <= 6; i++)
                {
                    _dieTextures[i] = ModContent.Request<Texture2D>($"WakfuMod/Content/Projectiles/ZurcarakDieSheet{i}");
                }
            }
        }
        public override void Unload() { _dieTextures = null; }

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = TotalAnimationFrames;
        }

        public override void SetDefaults()
        {
            Projectile.width = 48;
            Projectile.height = 48;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = AnimationDuration + LingerDuration;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.aiStyle = -1;
            Projectile.alpha = 0;
            Projectile.scale = 1.5f;
            Projectile.netImportant = true; // Importante para sincronizar el estado inicial
        }

        public override void OnSpawn(IEntitySource source)
        {
            // Solo el jugador que lanza el dado decide el resultado
            if (Projectile.owner == Main.myPlayer)
            {
                FinalResult = Main.rand.Next(1, 7); // 1 a 6
                Projectile.netUpdate = true; // Sincronizar este resultado inmediatamente
            }
            // Sonido de "lanzar" el dado
            SoundEngine.PlaySound(SoundID.Item35, Projectile.position);
        }

        public override void AI()
        {
            // --- MOVIMIENTO ---
            // El dado es estático, no se mueve.
            Projectile.velocity = Vector2.Zero;
            Projectile.rotation = 0f;

            // --- ANIMACIÓN (MODIFICADA) ---
            // Solo avanzar el frame si NO hemos llegado al último
            if (Projectile.frame < TotalAnimationFrames - 1)
            {
                Projectile.frameCounter++;
                if (Projectile.frameCounter >= AnimationSpeed)
                {
                    Projectile.frameCounter = 0;
                    Projectile.frame++;
                }
            }
            // Si ya está en el último frame (TotalAnimationFrames - 1), no hacemos nada,
            // por lo que se quedará en ese frame hasta que timeLeft se agote.
        }


        // --- MÉTODO Kill: Activar el efecto al final ---
        [System.Obsolete]
        public override void Kill(int timeLeft)
        {
            // El efecto se activa cuando el proyectil "muere" (al final de su timeLeft).
            // Solo el dueño debe iniciar el proceso de red.
            if (Projectile.owner == Main.myPlayer && FinalResult > 0)
            {
                // Llamar al sistema que maneja los efectos y la sincronización.
                ModContent.GetInstance<ZurcarakEffectSystem>().ActivateDieEffect(Main.player[Projectile.owner], Projectile.Center, FinalResult);
            }

            // Efecto de desaparición/resolución
            if (Main.netMode != NetmodeID.Server)
            {
                // Sonido de "resultado final"
                SoundEngine.PlaySound(SoundID.Tink, Projectile.Center);
                for (int i = 0; i < 20; i++)
                {
                    Dust.NewDustPerfect(Projectile.Center, DustID.GoldCoin, Main.rand.NextVector2Circular(3f, 3f), 0, default, 1.3f).noGravity = true;
                }
            }
        }


        public override bool PreDraw(ref Color lightColor)
        {
            if (_dieTextures == null || FinalResult < 1 || FinalResult > 6) return false;

            // Seleccionar la textura correcta basada en el resultado
            Asset<Texture2D> currentTexture = _dieTextures[FinalResult];
            if (!currentTexture.IsLoaded) return false;

            Texture2D texture = currentTexture.Value;
            // El alto de un frame se calcula a partir de la textura COMPLETA
            int frameHeight = texture.Height / TotalAnimationFrames;
            // El sourceRectangle corta el frame correcto de la animación
            Rectangle sourceRectangle = new Rectangle(0, Projectile.frame * frameHeight, texture.Width, frameHeight);
            Vector2 origin = sourceRectangle.Size() / 2f;
            Color drawColor = Projectile.GetAlpha(lightColor);

            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                sourceRectangle,
                drawColor,
                Projectile.rotation,
                origin,
                Projectile.scale,
                SpriteEffects.None,
                0f);

            return false;
        }
    }
}