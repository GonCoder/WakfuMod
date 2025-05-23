using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic; // Necesario para HashSet
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
// --- Quitado el using a WakfuMod.Content.NPCs si ya no usas YopukaMarkedNPC ---
// using WakfuMod.Content.NPCs; // Solo si todavía usas YopukaMarkedNPC para ALGO MÁS que inmunidad

namespace WakfuMod.Content.Projectiles
{
    public class YopukaShockwaveProjectile : ModProjectile
    {
        // --- Quitado hitTargets ---
        // private HashSet<int> hitTargets; // Ya no es necesario con inmunidad local

        private int rageLevel;
        private float expansionSpeed;
        private float maxDistance;
        private float traveledDistance = 20f;

        // --- NUEVO: Constante para el cooldown de golpe local ---
        private const int HitCooldown = 20; // 1/3 de segundo entre golpes del MISMO shockwave a un NPC

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 5;
        }

        public override void SetDefaults()
        {
            // --- Tus Defaults Originales ---
            Projectile.width = 100;
            Projectile.height = 110;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 1200;
            Projectile.DamageType = DamageClass.Melee;
            
            // DrawOriginOffsetY = -160; // Lo manejaremos en PreDraw

            // --- AÑADIDO: Inmunidad Local ---
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = HitCooldown;
        }

        public override void OnSpawn(IEntitySource source)
        {
            // --- Quitado hitTargets = new HashSet<int>(); ---

            rageLevel = (int)Projectile.ai[0]; // Obtener rabia de ai[0]

            // --- Tus Cálculos Originales ---
            maxDistance = 350f + 150f * rageLevel;
            expansionSpeed = 4f + rageLevel * 1.2f;
            Projectile.height = 40 + rageLevel * 35; // Ajustar altura de la hitbox

            // --- Tu Efecto de Dust Original ---
            if (Main.netMode != NetmodeID.Server) // Optimización: No crear dust en servidor
            {
                for (int d = 0; d < 5; d++)
                {
                    Vector2 dustVelocity = new Vector2(Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-3f, 3f));
                    int dust = Dust.NewDust(Projectile.Center, 0, 0, DustID.Ambient_DarkBrown, dustVelocity.X, dustVelocity.Y, 150, Color.OrangeRed, 2f);
                    Main.dust[dust].noGravity = true;
                }
            }

            // --- Establecer velocidad inicial basada en ai[1] (Dirección) ---
            // Es importante que la velocidad se establezca correctamente al spawnear
             int direction = (int)Projectile.ai[1]; // Obtener dirección de ai[1]
             Projectile.velocity.X = expansionSpeed * direction; // Establecer velocidad X inicial
             Projectile.velocity.Y = 0f; // Asegurarse de que no tiene velocidad Y inicial
        }

        public override void AI()
        {
            // --- Quitado if (hitTargets == null) ... ---

            // --- Rotación (Opcional, si tu sprite lo necesita) ---
            // Projectile.rotation = Projectile.velocity.ToRotation(); // Rotación simple si el sprite apunta a la derecha

            // --- Tu Lógica Original de Corrección de Altura ---
            // Esta lógica es crucial para que siga el terreno como querías.
            Point below = Projectile.Bottom.ToTileCoordinates();
            if (WorldGen.SolidTile(below.X, below.Y) /*|| WorldGen.SolidTile(below.X + 1, below.Y)*/) // Comprobar solo bajo el centro? O ambos? Ajusta si es necesario
            {
                 // Sube si está enterrado (o justo en el tile)
                 while (WorldGen.SolidTile(below.X, below.Y) /*|| WorldGen.SolidTile(below.X + 1, below.Y)*/) {
                    Projectile.position.Y -= 1;
                    below = Projectile.Bottom.ToTileCoordinates();
                    if (Projectile.position.Y < 16) break; // Evitar salir del mundo hacia arriba
                 }
                 // Asegurarse de que la velocidad Y es 0 después de ajustar
                 Projectile.velocity.Y = 0f;
            } else {
                 // Baja si está flotando
                 // Comprobar el tile Y+1
                 while (!WorldGen.SolidTile(below.X, below.Y + 1) /*&& !WorldGen.SolidTile(below.X + 1, below.Y + 1)*/) {
                     Projectile.position.Y += 1;
                     below = Projectile.Bottom.ToTileCoordinates();
                     // Condición de salida si cae demasiado (evita caer fuera del mundo)
                     if (Projectile.position.Y > Main.maxTilesY * 16 - Projectile.height - 16) break;
                     // Condición de salida por si la lógica falla (evita bucle infinito)
                     if (Projectile.position.Y > Main.screenPosition.Y + Main.screenHeight + 200) break;
                 }
                 // Podríamos añadir una pequeña velocidad de caída si sigue flotando después de esto,
                 // pero tu while debería colocarlo justo encima del suelo.
                 // Si sigue flotando, es que no hay suelo debajo en muchos tiles.
                 // Ajuste final para asegurar que esté exactamente encima
                  Point finalBelow = Projectile.Bottom.ToTileCoordinates();
                  Projectile.position.Y = (finalBelow.Y + 1) * 16f - Projectile.height;
                  Projectile.velocity.Y = 0f; // Detener velocidad Y
            }

            // --- Tu Animación Original ---
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 5)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Projectile.type];
            }

            // --- Movimiento Lateral y Distancia (Usando Velocity) ---
            // La velocidad X ya se estableció en OnSpawn y se mantiene
            // Terraria actualiza Projectile.position basado en Projectile.velocity automáticamente.
             traveledDistance += Math.Abs(Projectile.velocity.X); // Acumular distancia basada en velocidad X

            // --- Comprobación de Distancia Máxima ---
            if (traveledDistance >= maxDistance)
            {
                Projectile.Kill();
                return;
            }

            // --- Quitado el bucle de detección de colisión manual ---
            // La colisión ahora se maneja con los hooks ModifyHitNPC/OnHitNPC gracias a la inmunidad local.
            /*
            Rectangle hitbox = new Rectangle((int)Projectile.Center.X - 180, (int)Projectile.Center.Y - Projectile.height, 360, Projectile.height);
            foreach (NPC npc in Main.npc)
            {
                // ... tu lógica de colisión y golpe manual ...
            }
            */
        } // Fin de AI

        // --- ModifyHitNPC para aplicar daño % (Como lo teníamos) ---
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            int rage = rageLevel; // Usar la rabia guardada
            // Tu cálculo de daño extra original (1% o 2%)
            float extraPercent = (rage >= 5) ? 0.02f : 0.01f;
            int extraDamage = (int)(target.lifeMax * extraPercent);

            // Añadir el daño porcentual y el daño base que tenías (1 + rageLevel)
            modifiers.FlatBonusDamage += 5 + rageLevel + extraDamage;

            // Aplicar tu knockback original
            modifiers.Knockback.Base = 1f * rageLevel; // Usar .Base para establecerlo directamente
              // 1. Calcula el 2% de la vida máxima del NPC objetivo
            float percentDamage = target.lifeMax * 0.02f;

            // 2. Añade este daño como un bonus plano
            modifiers.FlatBonusDamage += percentDamage;
        }

        // --- OnHitNPC para efectos visuales/sonoros al golpear (Opcional) ---
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Puedes poner aquí el código que tenías para marcar al NPC con tu GlobalNPC
            // si todavía necesitas esa marca para *otras* mecánicas.
            // Ejemplo:
            // var globalNPC = target.GetGlobalNPC<YopukaMarkedNPC>();
            // if (globalNPC != null) {
            //      globalNPC.MarkedBySword = true;
            //      globalNPC.MarkDuration = 60; // O el tiempo que necesites
            // }

            // O simplemente añadir efectos visuales
             if (Main.netMode != NetmodeID.Server) {
                 for(int i=0; i<5; i++) {
                     Dust.NewDust(target.position, target.width, target.height, DustID.OrangeTorch, hit.HitDirection * 2f, -1f, 100, default, 1.2f);
                 }
             }
        }

        // --- Tu PreDraw Original (Asegúrate que la ruta es correcta) ---
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>("WakfuMod/Content/Projectiles/YopukaShockwaveProjectile").Value;
            int frameHeight = tex.Height / Main.projFrames[Projectile.type];
            Rectangle sourceRect = new Rectangle(0, Projectile.frame * frameHeight, tex.Width, frameHeight);

            // Origen en la base del sprite (centro-inferior)
            Vector2 origin = new Vector2(tex.Width / 2f, frameHeight);

            // Escala X/Y para que coincida con Projectile.width/height actuales
            // (Projectile.height cambia con la rabia en tu OnSpawn)
            float scaleY = (float)Projectile.height / frameHeight;
            float scaleX = (float)Projectile.width / tex.Width;
            Vector2 scale = new Vector2(scaleX, scaleY);

            // Flip horizontal basado en la dirección inicial guardada en ai[1] o velocidad actual
            int direction = (int)Projectile.ai[1]; // Obtener dirección de ai[1]
            SpriteEffects effects = direction == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            // Alternativa si ai[1] no se usa: SpriteEffects effects = Projectile.velocity.X >= 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;


            // Dibujar desde la base del proyectil (Projectile.Bottom)
            Vector2 drawPos = Projectile.Bottom - Main.screenPosition;

            // Usar EntitySpriteDraw
            Main.EntitySpriteDraw(
                tex,
                drawPos,
                sourceRect,
                lightColor, // Usar color de luz ambiente
                0f, // Sin rotación adicional para la shockwave
                origin,
                scale,
                effects,
                0f
            );

            return false; // No dibujar sprite por defecto
        }

    } // Fin Clase
}