// YopukaSlashProjectile.cs
using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ID;
using WakfuMod.Content.Items.Weapons; // Para llamar a SpawnShockwaveFromPlayer
using WakfuMod.jugador; // Para obtener WakfuPlayer
using System;
using Terraria.Audio;
using Terraria.DataStructures;
using ReLogic.Content;

namespace WakfuMod.Content.Projectiles
{
    public class YopukaSlashProjectile : ModProjectile
    {
         // ai[0] = RageLevel
        private int RageLevel => (int)Projectile.ai[0];

        // --- Configuración ---
        // Ajusta estos valores para definir el tamaño del golpe delante del jugador
      private const int SlashHitboxWidth = 380;
        private const int SlashHitboxHeight = 80;
        private const int AnimationFrames = 4;
        private const int FrameDuration = 4;
        private const int Lifetime = AnimationFrames * FrameDuration;
        private const int HitCooldown = 30; // Cooldown más corto entre golpes a MISMO NPC si el slash golpeara varias veces (con penetrate=-1)

        // --- DAÑO BASE DEL PROYECTIL ---
        private const int BaseSlashDamage = 10; // Daño fijo base de este ataque específico

        

        private static Asset<Texture2D> _textureAsset;

        public override void Load() {
            _textureAsset = ModContent.Request<Texture2D>("WakfuMod/Content/Projectiles/iop_sword_sheet");
        }
        public override void Unload() {
            _textureAsset = null;
        }

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            // --- Usamos Width/Height para la Hitbox ---
            Projectile.width = SlashHitboxWidth; // El ancho SÍ importa ahora
            Projectile.height = SlashHitboxHeight; // La altura SÍ importa ahora
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.alpha = 0; // Visible
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = HitCooldown;
            Projectile.ownerHitCheck = true; // Requiere estar cerca del jugador
            Projectile.hide = false; // ¡IMPORTANTE! Queremos ver el sprite
            Projectile.scale = 1f; // Escala base
            Projectile.aiStyle = -1; // Sin AI por defecto
            Projectile.velocity = Vector2.Zero; // No se mueve por sí mismo
             // --- Establecer Daño Base ---
            // Aunque el item pueda tener otro daño, aquí definimos el daño de ESTE proyectil
            Projectile.damage = BaseSlashDamage; // <<<--- DAÑO BASE DEL SLASH
        }

        public override void OnSpawn(IEntitySource source)
        {
            Player owner = Main.player[Projectile.owner];

            // --- Shockwave ---
           if (owner.whoAmI == Main.myPlayer) {
                 // La Shockwave ahora también debería tener daño base 1 + % vida
                 float shockwaveBaseDamage = 1f; // <<<--- DAÑO BASE SHOCKWAVE
                 float shockwaveKnockback = 5f;
                 YopukaShockwaveSword.SpawnShockwaveFromPlayer(owner, RageLevel, (int)shockwaveBaseDamage, shockwaveKnockback);
            }

            // Establece la dirección del sprite basada en la dirección del jugador
            Projectile.direction = owner.direction;
            Projectile.spriteDirection = owner.direction; // Para el flip en PreDraw

            // Posiciona el CENTRO de la hitbox delante del jugador
            // Ajusta el 'offsetMultiplier' para cambiar qué tan delante aparece
            float offsetMultiplier = 0.5f; // Ejemplo: 50% del ancho de la hitbox delante
            Projectile.Center = owner.MountedCenter + new Vector2(owner.direction * Projectile.width * offsetMultiplier, 0f);
              // Inicializar contador de frames
             Projectile.frame = 0;
             Projectile.frameCounter = 0;
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];

            // --- Mantener Posición Relativa al Jugador ---
            // Recalcula la posición basada en la dirección actual del jugador
            float offsetMultiplier = 0f;
            Projectile.Center = owner.MountedCenter + new Vector2(owner.direction * Projectile.width * offsetMultiplier, 0f);

            // --- Mantener Dirección del Sprite ---
            Projectile.direction = owner.direction;
            Projectile.spriteDirection = owner.direction;

            // --- Polvo (Opcional, ajustado a la hitbox) ---
            if (Main.rand.NextBool(3) && Main.netMode != NetmodeID.Server)
            {
                // Genera polvo dentro del rectángulo de la hitbox
                 Vector2 dustPos = Projectile.position + new Vector2(Main.rand.NextFloat(Projectile.width), Main.rand.NextFloat(Projectile.height));
                 Dust d = Dust.NewDustPerfect(dustPos, DustID.Smoke, Vector2.Zero, 0, default, 1.1f);
                 d.noGravity = true;
            }

             // --- AJUSTE: Lógica de Animación ACTIVADA ---
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= FrameDuration) // Usa la constante FrameDuration
            {
                Projectile.frameCounter = 0;
                Projectile.frame++; // Avanza al siguiente frame
                if (Projectile.frame >= AnimationFrames) // Si supera el último frame
                {
                    // Podrías querer que se quede en el último frame visualmente
                     Projectile.frame = AnimationFrames - 1;
                     // O si quieres que el proyectil se destruya justo al terminar el último frame:
                     // Projectile.Kill();
                     // Como el timeLeft ya está ajustado a la duración, se destruirá solo.
                }
            }
        } // Fin AI


        // --- Dibujado con Animación ---
        public override bool PreDraw(ref Color lightColor)
        {
            if (_textureAsset == null || !_textureAsset.IsLoaded) { return false; }
            Texture2D texture = _textureAsset.Value;

            // --- AJUSTE: Calcular frame correcto (Vertical) ---
            int frameHeight = texture.Height / AnimationFrames; // Alto de UN frame
            // Usa Projectile.frame que se actualiza en AI
            int currentFrameY = frameHeight * Projectile.frame;
             // Asegurarse que no se salga por errores de redondeo o frame final
             currentFrameY = Math.Clamp(currentFrameY, 0, texture.Height - frameHeight);
             Rectangle sourceRectangle = new Rectangle(0, currentFrameY, texture.Width, frameHeight);

            // Origen (Centro es lo más simple si no sabes el pivote exacto)
             Vector2 origin = sourceRectangle.Size() / 2f;

            // Color
             Color drawColor = Projectile.GetAlpha(lightColor);

            // Flip Horizontal
             SpriteEffects effects = Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            // Rotación CERO
             float rotation = 0f;

            // Dibuja el sprite
            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                sourceRectangle, // <<< Usa el rectángulo del frame calculado
                drawColor,
                rotation,
                origin,
                Projectile.scale,
                effects,
                0f);

            return false; // Hemos dibujado
        }

         // --- ModifyHitNPC (Añadir daño % vida máxima) ---
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            // 1. Bonus de Rabia (si lo tenías)
            modifiers.FlatBonusDamage += 1 * RageLevel; // Mantén esto si quieres el bonus plano de rabia

            // 2. Bonus % Vida Máxima del Objetivo
            // Calcula el 5% de la vida máxima del NPC objetivo
            float percentDamage = target.lifeMax * 0.15f;

            // Añade este daño como un bonus plano.
            // Usamos FlatBonusDamage para que se sume DESPUÉS de otros multiplicadores (como críticos)
            // pero ANTES de la defensa del enemigo.
            modifiers.FlatBonusDamage += percentDamage;
            modifiers.DefenseEffectiveness *= 0f; // Ignora defensa

            // Alternativa: Si quisieras que ignorase defensa (más poderoso):
            // modifiers.FinalDamage += percentDamage; // Se añade al final, después de la defensa
        }


        // --- OnHitNPC (Sin cambios necesarios) ---
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.netMode != NetmodeID.Server)
            {
                SoundEngine.PlaySound(SoundID.Item71, target.Center);
                for (int i = 0; i < 10; i++) Dust.NewDust(target.position, target.width, target.height, DustID.Blood, hit.HitDirection, -1f);
            }
        }
    }
}