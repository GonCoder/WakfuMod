using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System;
using Terraria.Audio;
using System.Collections.Generic; // Para el diccionario de objetivos

namespace WakfuMod.Content.Projectiles
{
    // --- CLASE DE GESTIÓN DE OBJETIVOS (Integrada en este archivo) ---
    // Esta clase estática se reiniciará al cargar un mundo.
    public static class FleaTargetManager
    {
        // Un diccionario que guarda:
        // Key: El ID del objetivo (whoAmI del NPC o Player)
        // Value: El ID del proyectil de la pulga que lo está atacando
        private static Dictionary<int, int> _targetedEntities;

        // Cargar/Reiniciar al entrar a un mundo
        // Se puede llamar desde un ModSystem.OnWorldLoad si es necesario,
        // pero para single player, una comprobación de nulidad es suficiente.
        private static void Initialize()
        {
            _targetedEntities = new Dictionary<int, int>();
        }

        public static bool IsTargetTaken(int targetWhoAmI)
        {
            if (_targetedEntities == null) Initialize(); // Inicializar si no se ha hecho
            return _targetedEntities.ContainsKey(targetWhoAmI);
        }

        public static void AssignTarget(int fleaWhoAmI, int targetWhoAmI)
        {
            if (_targetedEntities == null) Initialize();
            _targetedEntities[targetWhoAmI] = fleaWhoAmI;
        }

        public static void ReleaseTarget(int fleaWhoAmI, int targetWhoAmI)
        {
            if (_targetedEntities == null) Initialize();

            // Solo liberar si ESTA pulga era la que tenía asignado el objetivo
            if (_targetedEntities.ContainsKey(targetWhoAmI) && _targetedEntities[targetWhoAmI] == fleaWhoAmI)
            {
                _targetedEntities.Remove(targetWhoAmI);
            }
        }
    }


    // --- CLASE DEL PROYECTIL DE LA PULGA ---
    public class EcaflipFlea : ModProjectile
    {
        // --- Constantes ---
        private const float SearchRadius = 800f;
        private const int TickRate = 30; // 0.5 segundos
        private const int Lifetime = 8 * 60; // 8 segundos

        // --- Variables de Estado ---
        // ai[0]: Estado (0 = buscando, 1 = atacando NPC, 2 = curando Jugador)
        // ai[1]: Índice del objetivo (whoAmI del NPC o Player)
        // localAI[0]: Temporizador para el tick de daño/curación

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.alpha = 0; // Visible
            Projectile.DamageType = DamageClass.Summon;
            Projectile.damage = 0; // Daño manual
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, Color.LightGreen.ToVector3() * 0.5f);
            Projectile.localAI[0]++;

            // Comprobar si el objetivo actual sigue siendo válido
            bool targetIsValid = false;
            if (Projectile.ai[0] == 1) { // Atacando NPC
                NPC npc = Main.npc[(int)Projectile.ai[1]];
                targetIsValid = npc.active && npc.CanBeChasedBy(Projectile);
            } else if (Projectile.ai[0] == 2) { // Curando Jugador
                Player player = Main.player[(int)Projectile.ai[1]];
                targetIsValid = player.active && !player.dead && player.statLife < player.statLifeMax2;
            }

            // Si el objetivo ya no es válido, soltarlo del sistema y volver a buscar
            if (!targetIsValid && Projectile.ai[0] != 0) {
                FleaTargetManager.ReleaseTarget(Projectile.whoAmI, (int)Projectile.ai[1]);
                Projectile.ai[0] = 0;
            }

            // Si no tiene objetivo, buscar uno nuevo
            if (Projectile.ai[0] == 0) {
                FindTarget();
            }

            // --- Lógica de Acción ---
            if (Projectile.ai[0] == 1) { FollowAndDamageNPC(); }
            else if (Projectile.ai[0] == 2) { FollowAndHealPlayer(); }
            else { WanderErraticly(); }

            // Limitar velocidad máxima
             if (Projectile.velocity.LengthSquared() > 12f * 12f) {
                 Projectile.velocity = Vector2.Normalize(Projectile.velocity) * 12f;
             }
        }

        private void FindTarget()
        {
            Projectile.ai[0] = 0; // Asegurar estado "buscando"

            // --- Prioridad 1: Buscar jugadores aliados heridos ---
            Player targetPlayer = null;
            float closestPlayerDist = SearchRadius;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (p.active && !p.dead && p.team == Main.player[Projectile.owner].team && p.statLife < p.statLifeMax2)
                {
                    // Comprobar si el jugador NO está ya siendo objetivo de otra pulga
                    if (!FleaTargetManager.IsTargetTaken(p.whoAmI))
                    {
                        float dist = Projectile.Distance(p.Center);
                        if (dist < closestPlayerDist)
                        {
                            closestPlayerDist = dist;
                            targetPlayer = p;
                        }
                    }
                }
            }
            if (targetPlayer != null) {
                FleaTargetManager.AssignTarget(Projectile.whoAmI, targetPlayer.whoAmI);
                Projectile.ai[0] = 2; // Estado de curación
                Projectile.ai[1] = targetPlayer.whoAmI;
                Projectile.netUpdate = true;
                return;
            }

            // --- Prioridad 2: Buscar enemigos ---
            NPC targetNPC = null;
            float closestNpcDist = SearchRadius;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.CanBeChasedBy(this, false) && !FleaTargetManager.IsTargetTaken(npc.whoAmI))
                {
                    float dist = Projectile.Distance(npc.Center);
                    if (dist < closestNpcDist)
                    {
                        closestNpcDist = dist;
                        targetNPC = npc;
                    }
                }
            }
            if (targetNPC != null) {
                FleaTargetManager.AssignTarget(Projectile.whoAmI, targetNPC.whoAmI);
                Projectile.ai[0] = 1; // Estado de ataque
                Projectile.ai[1] = targetNPC.whoAmI;
                Projectile.netUpdate = true;
            }
        }

        private void FollowAndDamageNPC()
        {
            NPC target = Main.npc[(int)Projectile.ai[1]];
            Vector2 direction = target.Center - Projectile.Center;
            float distance = direction.Length();
            direction.Normalize();

            Projectile.velocity = (Projectile.velocity * 15f + direction * 8f) / 16f;

            if (distance < 30f && Projectile.localAI[0] >= TickRate)
            {
                Projectile.localAI[0] = 0;
                target.StrikeNPC(new NPC.HitInfo() { Damage = 1, Knockback = 0f, HitDirection = 0 });
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Blood);
                SoundEngine.PlaySound(SoundID.NPCHit1 with {Volume = 0.2f, Pitch = 0.5f}, Projectile.position);
            }
        }

        private void FollowAndHealPlayer()
        {
            Player target = Main.player[(int)Projectile.ai[1]];
            Vector2 direction = target.Center - Projectile.Center;
            float distance = direction.Length();
            direction.Normalize();
            
            Projectile.velocity = (Projectile.velocity * 20f + direction * 10f) / 21f;

            if (distance < 50f && Projectile.localAI[0] >= TickRate)
            {
                Projectile.localAI[0] = 0;
                target.Heal(1);
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GreenFairy);
                SoundEngine.PlaySound(SoundID.Item4 with {Volume = 0.2f, Pitch = 0.8f}, Projectile.position);
            }
        }

        private void WanderErraticly()
        {
            Projectile.velocity = Projectile.velocity.RotatedByRandom(MathHelper.ToRadians(5));
            if (Projectile.velocity.LengthSquared() < 4f*4f) {
                Projectile.velocity.X += Main.rand.NextFloat(-0.1f, 0.1f);
                Projectile.velocity.Y += Main.rand.NextFloat(-0.1f, 0.1f);
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Projectile.ai[0] != 0) // Si tenía un objetivo
            {
                FleaTargetManager.ReleaseTarget(Projectile.whoAmI, (int)Projectile.ai[1]);
            }
        }
    }
}