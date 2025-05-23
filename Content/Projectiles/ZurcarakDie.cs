// Content/Projectiles/ZurcarakDie.cs
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Audio;
using ReLogic.Content;
using WakfuMod.ModSystems; // Asumiendo que los efectos se manejan en un ModSystem
using WakfuMod.jugador;   // Para posiblemente ocultar jugador si la animación lo requiere aquí

namespace WakfuMod.Content.Projectiles // Reemplaza WakfuMod si es necesario
{
    public class ZurcarakDie : ModProjectile
    {
        // --- Configuración de Animación y Duración ---
        // ASUNCIÓN: Tienes un spritesheet VERTICAL con 6 frames (o más para animación de giro)
        // representando las caras del dado o una animación de giro.
        private const int TotalAnimationFrames = 6; // Número total de frames en tu spritesheet
        private const int FramesPerFace = 1;     // Cuántos frames de animación por cada cara (si solo muestras la cara final)
                                                 // O si es una animación de giro: total de frames de giro
        private const int AnimationSpeed = 8;    // Ticks por frame de animación
        private const int RollDuration = 90;     // Ticks que dura la animación antes de mostrar resultado (1.5 segundos)
        private const int LingerDuration = 60;   // Ticks que la cara final permanece visible (1 segundo)

        // --- Variables Internas ---
        private int _currentFaceFrame = 0; // Frame actual de la animación
        private int _frameCounter = 0;
        private bool _resultChosen = false;
        private int _finalResult = 1; // El número que salió (1-6)

        // --- Textura ---
        private static Asset<Texture2D> _dieTexture;

        public override void Load() {
             if (!Main.dedServ) {
                 _dieTexture = ModContent.Request<Texture2D>("WakfuMod/Content/Projectiles/ZurcarakDieSheet"); // Ruta a tu spritesheet
             }
        }
        public override void Unload() { _dieTexture = null; }

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Ecaflip's Die");
            Main.projFrames[Type] = TotalAnimationFrames; // Informa a Terraria
        }

        public override void SetDefaults()
        {
            Projectile.width = 40; // Tamaño visual/hitbox (ajusta a tu sprite)
            Projectile.height = 40;
            Projectile.friendly = false; // No daña directamente
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = RollDuration + LingerDuration; // Duración total
            Projectile.tileCollide = false; // Atraviesa bloques
            Projectile.ignoreWater = true;
            Projectile.aiStyle = -1; // AI personalizada
            Projectile.alpha = 0; // Visible
            // Projectile.scale = 1.5f; // Escalar si es necesario
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];

            // --- Ocultar Jugador (Si no lo hace ya WakfuPlayer) ---
            // Si IsRollingDie en WakfuPlayer ya oculta, no necesitas esto aquí.
            // Si quieres que el proyectil controle el ocultamiento:
            /*
            if (owner.active && owner.TryGetModPlayer<WakfuPlayer>(out var wp)) {
                 wp.HidePlayerForSkill = true; // Necesitarías un bool genérico HidePlayerForSkill
            }
            */

            // --- Movimiento Ligero (Opcional) ---
            // Puedes darle una trayectoria parabólica simple o dejarlo flotar
            if (Projectile.timeLeft > LingerDuration) {
                 Projectile.velocity.Y += 0.1f; // Gravedad muy leve
                 Projectile.velocity.X *= 0.99f; // Fricción leve
                 Projectile.rotation += Projectile.velocity.X * 0.05f; // Rotación leve
            } else {
                 // Frenar y detenerse cuando está mostrando el resultado
                 Projectile.velocity *= 0.8f;
                 if (Projectile.velocity.Length() < 0.1f) Projectile.velocity = Vector2.Zero;
                 Projectile.rotation = 0f; // Detener rotación
            }

            // --- Animación ---
            _frameCounter++;
            if (_frameCounter >= AnimationSpeed)
            {
                _frameCounter = 0;
                // Si aún está "girando"
                if (Projectile.timeLeft > LingerDuration) {
                    _currentFaceFrame = (_currentFaceFrame + 1) % TotalAnimationFrames; // Cicla todos los frames
                }
                 // Si ya eligió resultado, NO cambia el frame (se queda en el final)
            }

            // --- Elegir Resultado ---
            // Elige el resultado justo cuando termina la fase de giro
            if (!_resultChosen && Projectile.timeLeft <= LingerDuration)
            {
                _resultChosen = true;
                _finalResult = Main.rand.Next(1, 7); // Número aleatorio entre 1 y 6
                _currentFaceFrame = _finalResult - 1; // Asume que frame 0=Cara 1, frame 1=Cara 2, etc. Ajusta si no es así.

                // --- ACTIVAR EL EFECTO ---
                // Llama a un método (probablemente en un ModSystem) para activar el efecto
                // ModContent.GetInstance<ZurcarakEffectSystem>()?.ActivateDieEffect(owner, Projectile.Center, _finalResult); // Reemplaza con tu sistema

                // Sonido del resultado
                SoundEngine.PlaySound(SoundID.Tink, Projectile.Center); // Sonido "ding!"

                Projectile.netUpdate = true; // Sincroniza el resultado (ai podría usarse para esto también)
            }

            // Asignar el frame correcto para dibujar
             Projectile.frame = _currentFaceFrame;
        }

         // --- Dibujado del Dado ---
         public override bool PreDraw(ref Color lightColor)
         {
             if (_dieTexture == null || !_dieTexture.IsLoaded) return false;

             Texture2D texture = _dieTexture.Value;
             // Asume spritesheet VERTICAL
             int frameHeight = texture.Height / TotalAnimationFrames;
             // Usa Projectile.frame que actualizamos en AI
             Rectangle sourceRectangle = new Rectangle(0, Projectile.frame * frameHeight, texture.Width, frameHeight);
             Vector2 origin = sourceRectangle.Size() / 2f; // Origen en el centro
             Color drawColor = Projectile.GetAlpha(lightColor); // Aplica alpha si se desvanece
             SpriteEffects effects = Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None; // ¿Necesita flip? Probablemente no para un dado.

             Main.EntitySpriteDraw(
                 texture,
                 Projectile.Center - Main.screenPosition,
                 sourceRectangle,
                 drawColor,
                 Projectile.rotation, // Rotación de la AI
                 origin,
                 Projectile.scale,
                 effects,
                 0f);

             return false; // Hemos dibujado
         }

         // --- Volver a Mostrar al Jugador ---
          public override void Kill(int timeLeft)
          {
             // Si este proyectil controlaba el ocultamiento, mostrar al jugador aquí
             /*
             if (Main.player.IndexInRange(Projectile.owner)) {
                  Player owner = Main.player[Projectile.owner];
                  if (owner.active && owner.TryGetModPlayer<WakfuPlayer>(out var wp)) {
                       wp.HidePlayerForSkill = false;
                  }
             }
             */
              // Efecto de desaparición? (Polvo dorado?)
              if (Main.netMode != NetmodeID.Server) {
                   for(int i=0; i<15; i++) {
                        Dust.NewDustPerfect(Projectile.Center, DustID.GoldCoin, Main.rand.NextVector2Circular(2f,2f), 0, default, 1.2f).noGravity=true;
                   }
              }
          }
    }
}

// --- Necesitarás crear este ModSystem para manejar los efectos ---
/*
// ModSystems/ZurcarakEffectSystem.cs
using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace WakfuMod.ModSystems // Reemplaza con tu namespace
{
    public class ZurcarakEffectSystem : ModSystem
    {
         public void ActivateDieEffect(Player player, Vector2 position, int dieResult)
         {
              // Aquí va la lógica para cada resultado del dado
              switch (dieResult)
              {
                   case 1:
                       // Efecto 1: ¿Curación pequeña?
                       player.Heal(player.statLifeMax2 / 20); // Cura 5%
                       CombatText.NewText(player.getRect(), Color.Green, "Heal!");
                       break;
                   case 2:
                       // Efecto 2: ¿Buff de velocidad temporal?
                       player.AddBuff(BuffID.Swiftness, 300); // 5 segundos
                       CombatText.NewText(player.getRect(), Color.Yellow, "Speed!");
                       break;
                   case 3:
                       // Efecto 3: ¿Pequeña explosión de daño?
                       Projectile.NewProjectile(player.GetSource_FromThis("DieEffect"), position, Vector2.Zero, ProjectileID.Grenade, 30, 5f, player.whoAmI); // Granada como placeholder
                       CombatText.NewText(player.getRect(), Color.Orange, "Boom!");
                       break;
                   case 4:
                       // Efecto 4: ¿Invocación de gatito hostil temporal?
                       NPC.NewNPC(player.GetSource_FromThis("DieEffect"), (int)position.X, (int)position.Y, NPCID.BlackCat); // Gato negro como placeholder
                       CombatText.NewText(player.getRect(), Color.Purple, "Bad Kitty!");
                       break;
                   case 5:
                       // Efecto 5: ¿Buff de daño temporal?
                       player.AddBuff(BuffID.Wrath, 300); // +10% daño
                       CombatText.NewText(player.getRect(), Color.Red, "Damage!");
                       break;
                   case 6:
                       // Efecto 6: ¿Jackpot? ¿Curación grande + buff fuerte?
                       player.Heal(player.statLifeMax2 / 5); // Cura 20%
                       player.AddBuff(BuffID.RapidHealing, 600);
                       player.AddBuff(BuffID.Archery, 600); // Ejemplo buff
                       CombatText.NewText(player.getRect(), Color.Gold, "Jackpot!");
                       break;
              }
              // Añadir efectos visuales/sonoros generales para el resultado
               // ...
         }
    }
}
*/