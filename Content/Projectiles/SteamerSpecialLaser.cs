// SteamerSpecialLaser.cs
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System;
using ReLogic.Content;
using Terraria.Audio;

namespace WakfuMod.Content.Projectiles
{
    // Proyectil tipo rayo para el disparo especial de la torreta
    public class SteamerSpecialLaser : ModProjectile
    {
        // --- Constantes del Rayo ---
        private const float MaxLength = 1200f; // Alcance máximo
        private const float BeamWidth = 20f;  // Ancho visual del rayo
        private const int Duration = 30;    // Cuántos ticks dura el rayo visible
        private const int DamageCooldown = 15; // Ticks entre golpes al mismo NPC

        // --- Propiedades Internas ---
        private Vector2 _startPoint;
        private Vector2 _endPoint;
        private Vector2 _direction;
        private float _currentLength;
        private bool _initialized = false;

        // Cache de Textura (Opcional)
        private static Asset<Texture2D> _laserTexture;
        private static Asset<Texture2D> _laserEndTexture;
        private static Asset<Texture2D> _laserStartTexture;

        public override void Load()
        {
            if (!Main.dedServ)
            {
                _laserTexture = ModContent.Request<Texture2D>("WakfuMod/Content/Projectiles/SteamerSpecialLaser_Beam"); // Textura del cuerpo del rayo
                _laserEndTexture = ModContent.Request<Texture2D>("WakfuMod/Content/Projectiles/SteamerSpecialLaser_End");   // Textura de la punta
                _laserStartTexture = ModContent.Request<Texture2D>("WakfuMod/Content/Projectiles/SteamerSpecialLaser_Start"); // Textura del origen (opcional)
            }
        }
        public override void Unload()
        {
            _laserTexture = null;
            _laserEndTexture = null;
            _laserStartTexture = null;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10; // Hitbox pequeña, la colisión es por línea
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1; // Atraviesa todo
            Projectile.timeLeft = Duration; // Duración corta
            Projectile.tileCollide = false; // No choca
            Projectile.DamageType = DamageClass.Summon; // O el tipo que uses
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = DamageCooldown;
            Projectile.alpha = 255; // Empieza invisible hasta que calculemos la longitud
            Projectile.hide = true; // Oculta el sprite por defecto, dibujamos todo en PreDraw
        }

        public override void AI()
        {
            // --- Inicialización en el primer frame ---
            if (!_initialized)
            {
                // El punto de inicio es donde se spawneó
                _startPoint = Projectile.Center;
                // La dirección es la velocidad inicial con la que se spawneó
                if (Projectile.velocity == Vector2.Zero) Projectile.velocity = Vector2.UnitX; // Fallback
                _direction = Vector2.Normalize(Projectile.velocity);
                Projectile.velocity = Vector2.Zero; // Detener movimiento, el rayo es instantáneo

                // Calcular longitud y punto final usando LaserScan
                float[] laserScanResults = new float[1];
                Collision.LaserScan(_startPoint, _direction, 0f, MaxLength, laserScanResults);
                _currentLength = laserScanResults[0];
                if (float.IsNaN(_currentLength) || float.IsInfinity(_currentLength) || _currentLength < 0) _currentLength = 0f; // Validar
                _currentLength = Math.Clamp(_currentLength, 0f, MaxLength); // Limitar

                _endPoint = _startPoint + _direction * _currentLength;
                Projectile.rotation = _direction.ToRotation(); // Guardar rotación para dibujo

                // Hacer visible ahora que tenemos la longitud
                Projectile.alpha = 0;
                _initialized = true;

                // Aplicar daño inicial al spawnear? O dejar que el PreDraw lo haga?
                // ApplyLaserDamage(_startPoint, _endPoint);
            }

            // --- Mantener posición y rotación ---
            // El rayo no se mueve, pero necesita mantener su rotación
            Projectile.Center = _startPoint;
            Projectile.rotation = _direction.ToRotation();

            // --- Efectos visuales (Polvo, Luz) ---
            SpawnDustAlongLaser();
            CastLight();
            // Sonido diferente para el láser especial?
            SoundEngine.PlaySound(SoundID.Item158 with { Volume = 0.3f, Pitch = 1.9f }, Projectile.position); // Sonido tipo Last Prism
            // --- Fade out al final (Opcional) ---
            // int fadeTime = 5;
            // if (Projectile.timeLeft <= fadeTime) {
            //    Projectile.alpha = (int)MathHelper.Lerp(255, 0, (float)Projectile.timeLeft / fadeTime);
            //}
        }

        // --- Colisión Personalizada para Rayo ---
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!_initialized) return false;
            float point = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), _startPoint, _endPoint, BeamWidth, ref point);
        }

        // --- NUEVO O MODIFICADO: ModifyHitNPC ---
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            // 1. Calcula el 0.5% de la vida máxima del NPC objetivo
            float percentDamage = target.lifeMax * 0.02f; // 0.005f es 0.5%

            // 2. Asegura un mínimo de daño si el cálculo es muy bajo (opcional pero recomendado)
            if (percentDamage < 1f) percentDamage = 1f;

            // 3. Añade este daño como un bonus plano
            modifiers.FlatBonusDamage += percentDamage;

            // 4. (Opcional) Puedes añadir otros modificadores aquí
            // Ejemplo: Reducir ligeramente el knockback del láser especial
            // modifiers.Knockback *= 0.5f;
        }

        // --- Efectos Visuales ---
        private void SpawnDustAlongLaser()
        {
            if (!_initialized || _currentLength <= 0) return;

            Vector2 unit = _direction;
            float step = 8f; // Espaciado del polvo

            for (float i = 0; i < _currentLength; i += step)
            {
                if (Main.rand.NextBool(4))
                { // Probabilidad de spawn
                    Vector2 dustPos = _startPoint + unit * i + Main.rand.NextVector2Circular(BeamWidth * 0.3f, BeamWidth * 0.3f);
                    Dust dust = Dust.NewDustPerfect(dustPos, DustID.Clentaminator_Purple, Vector2.Zero, 150, Color.MediumPurple, 0.9f);
                    dust.noGravity = true;
                    dust.velocity *= 0.1f;
                }
            }
        }

        private void CastLight()
        {
            DelegateMethods.v3_1 = Color.MediumPurple.ToVector3() * 0.5f; // Color y brillo de la luz
            Utils.PlotTileLine(_startPoint, _endPoint, BeamWidth * 0.5f, DelegateMethods.CastLight);
        }


        // --- Dibujado del Rayo ---
        public override bool PreDraw(ref Color lightColor)
        {
            if (!_initialized || _currentLength <= 0 || _laserTexture == null || !_laserTexture.IsLoaded) return false;

            Texture2D beamTexture = _laserTexture.Value;
            Texture2D startTexture = _laserStartTexture?.Value ?? beamTexture; // Usa beam si no hay start
            Texture2D endTexture = _laserEndTexture?.Value ?? beamTexture;   // Usa beam si no hay end

            float laserBodyLength = _currentLength - (startTexture.Height / 2f) - (endTexture.Height / 2f); // Ajusta si las texturas de inicio/fin tienen altura significativa

            // --- Dibuja el Cuerpo del Rayo ---
            if (laserBodyLength > 0)
            {
                Vector2 beamOrigin = new Vector2(beamTexture.Width / 2f, 0); // Origen en el centro superior
                Vector2 beamScale = new Vector2(BeamWidth / beamTexture.Width, laserBodyLength / beamTexture.Height);
                Vector2 beamDrawPos = _startPoint + _direction * (startTexture.Height / 2f) - Main.screenPosition; // Ajusta posición inicial
                Color beamColor = Projectile.GetAlpha(Color.White); // Color base blanco, afecta alpha

                Main.EntitySpriteDraw(beamTexture, beamDrawPos, beamTexture.Frame(), beamColor, Projectile.rotation + MathHelper.PiOver2, beamOrigin, beamScale, SpriteEffects.None, 0);
            }

            // --- Dibuja el Inicio del Rayo (Opcional) ---
            if (_laserStartTexture != null && _laserStartTexture.IsLoaded)
            {
                Vector2 startOrigin = new Vector2(startTexture.Width / 2f, startTexture.Height); // Origen en el centro inferior
                Vector2 startDrawPos = _startPoint - Main.screenPosition;
                Color startColor = Projectile.GetAlpha(Color.White);
                Main.EntitySpriteDraw(startTexture, startDrawPos, startTexture.Frame(), startColor, Projectile.rotation + MathHelper.PiOver2, startOrigin, Projectile.scale, SpriteEffects.None, 0);
            }

            // --- Dibuja el Final del Rayo ---
            if (_laserEndTexture != null && _laserEndTexture.IsLoaded)
            {
                Vector2 endOrigin = new Vector2(endTexture.Width / 2f, 0); // Origen en el centro superior
                Vector2 endDrawPos = _endPoint - Main.screenPosition;
                Color endColor = Projectile.GetAlpha(Color.White);
                Main.EntitySpriteDraw(endTexture, endDrawPos, endTexture.Frame(), endColor, Projectile.rotation + MathHelper.PiOver2, endOrigin, Projectile.scale, SpriteEffects.None, 0);
            }


            return false; // Ya hemos dibujado todo
        }

    }
}