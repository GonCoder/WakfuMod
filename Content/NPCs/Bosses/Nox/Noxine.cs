using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System;
using Terraria.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace WakfuMod.Content.NPCs.Bosses.Nox // Asegúrate de que el namespace sea correcto
{
    public class Noxine : ModNPC
    {
        // --- Constantes de Animación de Explosión ---
        private const int TotalExplosionFrames = 2;
        private const int ExplosionAnimSpeed = 8;
        private const int ExplosionDuration = ExplosionAnimSpeed * TotalExplosionFrames;

        public override void SetStaticDefaults()
        {
            NPCID.Sets.NPCBestiaryDrawModifiers bestiaryData = new() { Hide = true };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, bestiaryData);
        }

        public override void SetDefaults()
        {
            NPC.width = 24;
            NPC.height = 24;
            NPC.aiStyle = -1;
            NPC.damage = 20;
            NPC.defense = 5;
            NPC.lifeMax = 20;
            NPC.knockBackResist = 0.8f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.HitSound = SoundID.NPCHit5;
            NPC.DeathSound = SoundID.NPCDeath7;
            // IMPORTANTE: Un NPC con vida 1 no puede ser dañado por defecto.
            // Si la hitbox expandida debe hacer daño, el daño de contacto original debe ser 0,
            // y el daño real debe aplicarse de otra forma (como un proyectil).
            // Por ahora, el daño de la explosión será solo visual/de contacto si toca al jugador.
        }

        public override bool CheckDead()
        {
            if (NPC.ai[3] != 2) // Si no está ya en el estado de "muriendo"
            {
                // Entrar en el estado de "muriendo"
                NPC.life = 1; // Mantener con 1 de vida para que siga existiendo
                NPC.ai[3] = 2; // Cambiar a modo MURIENDO
                NPC.ai[1] = 0; // Resetear timer para la animación de explosión
                NPC.damage = 25; // Aumentar daño de contacto
                NPC.velocity = Vector2.Zero; // Detenerse
                NPC.knockBackResist = 0f;
                NPC.dontTakeDamage = true; // No puede recibir más daño
                NPC.timeLeft = ExplosionDuration;
                SoundEngine.PlaySound(SoundID.Item14, NPC.position);

                // --- EXPANDIR HITBOX CORRECTAMENTE ---
                Vector2 center = NPC.Center; // Guardar el centro
                NPC.width = 58; // Doble del original
                NPC.height = 58;
                NPC.Center = center; // Restaurar el centro
                // --- FIN ---

                NPC.netUpdate = true;
                return false; // Prevenir la muerte normal
            }
            return true; // Permitir que desaparezca al final de timeLeft
        }

        public override void AI()
        {
            if (NPC.ai[3] == 2) // Si está en modo MURIENDO
            {
                NPC.velocity = Vector2.Zero; // Asegurar que permanezca quieto
                NPC.ai[1]++; // Incrementar el timer de la animación de explosión

                // --- COMPROBAR SI LA ANIMACIÓN DE EXPLOSIÓN HA TERMINADO ---
                if (NPC.ai[1] >= ExplosionDuration)
                {
                    NPC.life = 0; // Poner vida a 0 de nuevo
                    NPC.active = false; // Desactivar inmediatamente para que desaparezca
                                        // NPC.checkDead(); // Opcional: forzar una última comprobación
                }
                return; // No ejecutar ninguna otra lógica de IA
            }
            // --- FIN LÓGICA DE MUERTE ---


            // --- Lógica de IA Normal ---
            if (!Main.npc.IndexInRange((int)NPC.ai[0]))
            {
                NPC.active = false;
                return;
            }

            NPC nox = Main.npc[(int)NPC.ai[0]];
            if (!nox.active || nox.type != ModContent.NPCType<Nox>())
            {
                NPC.active = false;
                return;
            }

            if (NPC.ai[3] == 1)
            { // Modo Atacante
                AttackerAI(nox);
            }
            else
            { // Modo Orbital (por defecto)
                OrbitalAI(nox);
            }
        }
        // --- PreDraw CON LA FIRMA CORRECTA ---
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (NPC.ai[3] == 2) // Si está explotando
            {
                // Cargar la textura de explosión
                Texture2D explosionTexture = ModContent.Request<Texture2D>("WakfuMod/Content/NPCs/Bosses/Nox/NoxineXplosion").Value;

                // Calcular la altura de un frame de la animación de EXPLOSIÓN
                int frameHeight = explosionTexture.Height / TotalExplosionFrames;

                // Calcular el frame actual de la animación
                int currentExplosionFrame = (int)(NPC.ai[1] / ExplosionAnimSpeed);
                if (currentExplosionFrame >= TotalExplosionFrames) {
                    currentExplosionFrame = TotalExplosionFrames - 1;
                }

                // Calcular el sourceRectangle
                Rectangle sourceRect = new Rectangle(0, currentExplosionFrame * frameHeight, explosionTexture.Width, frameHeight);

                // El resto de la lógica de dibujado
                SpriteEffects spriteEffects = NPC.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                Vector2 drawOrigin = sourceRect.Size() / 2f;
                Vector2 drawPos = NPC.Center - screenPos + new Vector2(0f, NPC.gfxOffY); // Usa el screenPos del parámetro

                // Usar el spriteBatch del parámetro
                spriteBatch.Draw(explosionTexture, drawPos, sourceRect, NPC.GetAlpha(drawColor), NPC.rotation, drawOrigin, NPC.scale, spriteEffects, 0f);

                return false; // No dibujar la textura normal.
            }

            return true; // Dejar que se dibuje la textura normal si no está explotando.
        }


        private void OrbitalAI(NPC nox)
        {
            NPC.ai[1]++; // Timer para la rotación
            float angle = MathHelper.ToRadians(NPC.ai[2] + NPC.ai[1] * 1.5f);
            float distance = 150f;
            Vector2 orbitPos = nox.Center + new Vector2(distance, 0).RotatedBy(angle);

            float interpolationFactor = 0.05f;
            if (NPC.localAI[0] < 120)
            {
                NPC.localAI[0]++;
                NPC.Center = Vector2.Lerp(NPC.Center, orbitPos, interpolationFactor);
                NPC.velocity = (orbitPos - NPC.Center) * 0.1f;
            }
            else
            {
                Vector2 direction = orbitPos - NPC.Center;
                NPC.velocity = (NPC.velocity * 10f + direction) / 11f;
            }
        }

        private void AttackerAI(NPC nox)
        {
            NPC.ai[1]++;
            int targetPlayerIndex = (int)NPC.ai[2];
            if (!Main.player.IndexInRange(targetPlayerIndex) || !Main.player[targetPlayerIndex].active || Main.player[targetPlayerIndex].dead)
            {
                NPC.TargetClosest(true);
                if (NPC.target >= 0 && Main.player[NPC.target].active)
                {
                    targetPlayerIndex = NPC.target;
                }
                else
                {
                    NPC.active = false;
                    return;
                }
            }

            Player targetPlayer = Main.player[targetPlayerIndex];
            Vector2 targetPos = targetPlayer.Center;
            Vector2 direction = targetPos - NPC.Center;
            direction.Normalize();

            bool isPhase2 = nox.ai[2] == 1;
            float speed = isPhase2 ? 10f : 7f;
            float inertia = isPhase2 ? 20f : 30f;
            if (NPC.ai[1] < 30)
            {
                speed *= 1.5f;
            }
            NPC.velocity = (NPC.velocity * (inertia - 1) + direction * speed) / inertia;

            if (NPC.timeLeft > 300)
            {
                NPC.timeLeft = 300;
            }
        }
    }
}