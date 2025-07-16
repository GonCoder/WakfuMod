// En Content/NPCs/Bosses/Nox.cs

using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using WakfuMod.Content.Items.BossSpawners; // Para el spawner
using WakfuMod.ModSystems; // Asumiendo que el control de spawn/derrota está en un ModSystem
using System.Collections.Generic;


namespace WakfuMod.Content.NPCs.Bosses.Nox
{
    [AutoloadBossHead]
    public class Nox : ModNPC
    {
        // --- Constantes ---
        private const int Frame_Idle = 0;
        private const int Frame_Blink_Start = 1;
        private const int Total_Blink_Frames = 3;
        private const int Total_Frames_In_Sheet = 5; // Total de frames
        private const int BlinkAnimSpeed = 5; // Ticks por cada frame de la animación de blink

        // --- Constantes de Combate ---
        private const int OrbitalNoxineCount = 12;
        private const int AttackerNoxineCountPerPlayer = 4;
        private const int BlinkFadeTime = 20; // Ticks para desvanecerse/aparecer

        // --- Constantes de Animación ---
        private const int Frame_Transition_Start = 4; // Asume que el frame 4 es para la transición
        private const int Total_Transition_Frames = 1; // Por ahora solo un frame
        private const int Transition_Anim_Duration = 120; // 2 segundos de animación

        // --- Variables de IA ---
        // ai[0]: Temporizador general para el estado actual (ej. cuánto tiempo lleva en Idle)
        // ai[1]: El estado actual de la IA (ver enum abajo)
        // ai[2]: La fase actual del combate (0 = Fase 1, 1 = Fase 2)
        // ai[3]: Temporizador específico para la animación de blink

        private enum AI_State
        {
            Idle,
            StartBlink,
            EndBlink,
            Attacking,
            PhaseTransition // <-- NUEVO ESTADO
        }

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = Total_Frames_In_Sheet;
            NPCID.Sets.MPAllowedEnemies[Type] = true;
            NPCID.Sets.BossBestiaryPriority.Add(Type);
        }

        public override void SetDefaults()
        {
            NPC.width = 100;
            NPC.height = 120;
            NPC.aiStyle = -1;
            NPC.damage = 30;
            NPC.defense = 10;
            NPC.lifeMax = 4000;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.value = Item.buyPrice(gold: 5);
            NPC.boss = true;
            NPC.npcSlots = 10f;

            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath14;

            Music = MusicID.Boss2;
        }

        public override void AI()
        {
            if (NPC.target < 0 || NPC.target == 255 || Main.player[NPC.target].dead || !Main.player[NPC.target].active)
            {
                NPC.TargetClosest(true);
            }
            Player player = Main.player[NPC.target];
            if (player.dead)
            {
                NPC.velocity.Y += 0.1f;
                if (NPC.timeLeft > 10) NPC.timeLeft = 10;
                return;
            }

            // --- Lógica de Transición de Fase ---
            // Primero, comprobar si está en transición, ya que esto tiene la máxima prioridad
            if ((AI_State)NPC.ai[1] == AI_State.PhaseTransition)
            {
                PhaseTransition(player);
                return; // Salir de la AI para no ejecutar otra lógica
            }

            // Después, comprobar si DEBE entrar en transición
            bool isPhase2 = (float)NPC.life / NPC.lifeMax <= 0.5f;
            if (isPhase2 && NPC.ai[2] == 0)
            {
                NPC.ai[2] = 1; // Marcar como Fase 2
                NPC.ai[0] = 0; // Resetear timer de acción
                NPC.ai[1] = (float)AI_State.PhaseTransition; // <-- CAMBIAR A NUEVO ESTADO
                NPC.ai[3] = 0;
                NPC.velocity = Vector2.Zero;
                NPC.netUpdate = true;
                return; // Salir de la AI este frame para empezar la transición limpiamente
            }

            // --- Máquina de Estados Normal ---
            switch ((AI_State)NPC.ai[1])
            {
                case AI_State.Idle:
                    Idle(player, isPhase2);
                    break;
                case AI_State.StartBlink:
                    StartBlink(player, isPhase2);
                    break;
                case AI_State.EndBlink:
                    EndBlink(player, isPhase2);
                    break;
                case AI_State.Attacking:
                    Attacking(player, isPhase2);
                    break;
                default:
                    NPC.ai[1] = (float)AI_State.Idle;
                    break;
            }
        }

        private void Idle(Player player, bool isPhase2)
        {
            NPC.ai[0]++;
            NPC.velocity *= 0.9f; // Frenar suavemente

            int idleTime = isPhase2 ? 45 : 90; // Pausa más corta en Fase 2
            if (NPC.ai[0] >= idleTime)
            {
                NPC.ai[0] = 0;
                NPC.ai[3] = 0; // Resetear el timer de animación para el blink
                NPC.ai[1] = (float)AI_State.StartBlink;
                NPC.netUpdate = true;
            }
        }

        private void StartBlink(Player player, bool isPhase2)
        {
            NPC.velocity = Vector2.Zero;
            NPC.ai[3]++; // Incrementar el timer de la animación/fade

            // Desvanecerse
            NPC.alpha = (int)(255 * (NPC.ai[3] / BlinkFadeTime));
            if (NPC.alpha > 255) NPC.alpha = 255;

            // Al final del desvanecimiento, teletransportar
            if (NPC.ai[3] >= BlinkFadeTime)
            {
                SoundEngine.PlaySound(SoundID.Item8, NPC.position);

                // Eliminar TODAS las noxinas existentes
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].active && Main.npc[i].type == ModContent.NPCType<Noxine>() && (int)Main.npc[i].ai[0] == NPC.whoAmI)
                    {
                        Main.npc[i].active = false;
                        // O Main.npc[i].life = 0; Main.npc[i].checkDead();
                    }
                }

                // Búsqueda de Posición Segura
                Vector2 newPosition = Vector2.Zero;
                for (int i = 0; i < 100; i++)
                {
                    float distance = Main.rand.NextFloat(300, 500);
                    newPosition = player.Center + Main.rand.NextVector2Circular(distance, distance);
                    if (!Collision.SolidCollision(newPosition - NPC.Size / 2f, NPC.width, NPC.height)) break;
                    if (i == 99) newPosition = player.Center + new Vector2(0, -300);
                }
                NPC.Center = newPosition;
                NPC.alpha = 255; // Mantener invisible mientras se mueve

                // --- Restaurar Noxinas Orbitales (CON FASE 2) ---
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    // Determinar el número de noxinas orbitales según la fase
                    int orbitalCount = isPhase2 ? 24 : OrbitalNoxineCount; // 24 en Fase 2, 12 en Fase 1

                    for (int i = 0; i < orbitalCount; i++)
                    {
                        int noxineType = ModContent.NPCType<Noxine>();
                        // Resetear localAI[0] para el movimiento suave
                        int npcIndex = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, noxineType, 0,
                            NPC.whoAmI, // Jefe
                            i * (360f / orbitalCount), // Ángulo inicial
                            0f, // No usado
                            0f); // Modo 0 = Orbital

                        if (npcIndex < Main.maxNPCs)
                        {
                            Main.npc[npcIndex].localAI[0] = 0; // Asegurar que el timer de estabilización empiece en 0
                            if (Main.netMode == NetmodeID.Server)
                            {
                                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npcIndex);
                            }
                        }
                    }
                }

                // Cambiar al estado de reaparición
                NPC.ai[0] = 0;
                NPC.ai[3] = 0; // Resetear timer de animación
                NPC.ai[1] = (float)AI_State.EndBlink;
                NPC.netUpdate = true;
            }
        }

        private void EndBlink(Player player, bool isPhase2)
        {
            NPC.velocity = Vector2.Zero;
            NPC.ai[3]++;

            // Aparecer gradualmente
            NPC.alpha = 255 - (int)(255 * (NPC.ai[3] / BlinkFadeTime));
            if (NPC.alpha < 0) NPC.alpha = 0;

            if (NPC.ai[3] >= BlinkFadeTime)
            {
                NPC.alpha = 0; // Asegurarse de que es totalmente visible
                NPC.ai[0] = 0;
                NPC.ai[3] = 0;
                NPC.ai[1] = (float)AI_State.Attacking;
                NPC.netUpdate = true;
            }
        }

        private void Attacking(Player player, bool isPhase2)
        {
            NPC.velocity *= 0.95f;

            int attackerCount = isPhase2 ? 8 : AttackerNoxineCountPerPlayer; // Más atacantes en Fase 2
            int activePlayers = 0;
            // Contar jugadores y guardar sus índices
            List<int> playerIndexes = new List<int>();
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (Main.player[i].active && !Main.player[i].dead)
                {
                    activePlayers++;
                    playerIndexes.Add(i);
                }
            }
            if (activePlayers == 0) return; // No atacar si no hay jugadores

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                // Para cada jugador activo...
                foreach (int playerIndex in playerIndexes)
                {
                    Player targetPlayer = Main.player[playerIndex];
                    // ...lanzar 'attackerCount' noxinas
                    for (int i = 0; i < attackerCount; i++)
                    {
                        // --- Formación de Spawn ---
                        // Spawnean en un semicírculo delante de Nox, mirando al jugador
                        float angleOffset = (i - (attackerCount - 1) / 2f) * 0.4f; // Separación angular
                        Vector2 directionToPlayer = (targetPlayer.Center - NPC.Center).SafeNormalize(Vector2.UnitY);
                        Vector2 spawnPos = NPC.Center + directionToPlayer.RotatedBy(angleOffset) * 80f; // 80px delante de Nox

                        int noxineType = ModContent.NPCType<Noxine>();
                        int npcIndex = NPC.NewNPC(NPC.GetSource_FromAI(), (int)spawnPos.X, (int)spawnPos.Y, noxineType, 0,
                            NPC.whoAmI, // Jefe
                            targetPlayer.whoAmI, // Pasar el ÍNDICE del jugador objetivo en ai[2]
                            0f,
                            1f); // Modo 1 = Atacante

                        if (Main.netMode == NetmodeID.Server && npcIndex < Main.maxNPCs)
                        {
                            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npcIndex);
                        }
                    }
                }
            }
            SoundEngine.PlaySound(SoundID.Item111, NPC.Center);

            NPC.ai[0] = 0;
            NPC.ai[1] = (float)AI_State.Idle;
            NPC.netUpdate = true;
        }

        // --- NUEVO MÉTODO DE IA: PhaseTransition ---
        private void PhaseTransition(Player player)
        {
            NPC.dontTakeDamage = true; // Hacerlo invulnerable
            NPC.velocity = Vector2.Zero; // Asegurar que no se mueva

            NPC.ai[0]++; // Usar ai[0] como timer para la duración de la animación

            // Efectos visuales/sonoros de la transición
            if (NPC.ai[0] == 1)
            {
                SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                Main.NewText("Nox: ¡La energía del Eliacubo es mía! ¡El tiempo se doblega ante mí!", new Color(0, 200, 255));
            }
            if (NPC.ai[0] % 5 == 0)
            { // Efecto de polvo constante
                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.MagicMirror, 0, 0, 150, Color.Cyan, 1.5f);
            }

            // Cuando la animación de transición termina
            if (NPC.ai[0] >= Transition_Anim_Duration)
            {
                // Terminar la transición
                NPC.dontTakeDamage = false; // Hacerlo vulnerable de nuevo
                NPC.ai[0] = 0; // Resetear timer para la siguiente acción
                NPC.ai[1] = (float)AI_State.StartBlink; // Forzar un blink después de la transición
                NPC.netUpdate = true;

                // --- ACTIVAR RALENTIZACIÓN DE TIEMPO ---
                // Solo el servidor/singleplayer debe iniciar este evento global.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    // Llama al sistema que controla la ralentización del tiempo
                    ModContent.GetInstance<TimeSlowSystem>().Activate(20 * 60); // Activar por 20 segundos
                    Main.NewText("[Nox] Llamando a TimeSlowSystem.Activate()", Color.CornflowerBlue);

                }
            }
        }

        public override void FindFrame(int frameHeight)
        {
            AI_State currentState = (AI_State)NPC.ai[1];

            switch (currentState)
            {
                // --- AÑADIR CASO PARA LA TRANSICIÓN ---
                case AI_State.PhaseTransition:
                    NPC.frame.Y = Frame_Transition_Start * frameHeight;
                    break;

                case AI_State.StartBlink:
                case AI_State.EndBlink:
                    int blinkFrameIndex = (int)(NPC.ai[3] / BlinkAnimSpeed);
                    if (blinkFrameIndex >= Total_Blink_Frames)
                    {
                        blinkFrameIndex = Total_Blink_Frames - 1;
                    }
                    NPC.frame.Y = (Frame_Blink_Start + blinkFrameIndex) * frameHeight;
                    break;

                case AI_State.Attacking:
                case AI_State.Idle:
                default:
                    NPC.frame.Y = Frame_Idle * frameHeight;
                    break;
            }
        }

        public override void OnKill()
        {
            // Avisar al sistema que Nox fue derrotado (para la probabilidad de spawn)
            ModContent.GetInstance<NoxSpawnSystem>().OnNoxDefeated();

            // --- DESACTIVAR TODAS LAS NOXINAS RESTANTES ---
            // Solo el servidor debe manejar esto en multijugador para evitar conflictos.
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC otherNpc = Main.npc[i];
                    // Comprobar si es una noxina Y si pertenece a ESTE Nox
                    if (otherNpc.active && otherNpc.type == ModContent.NPCType<Noxine>() && (int)otherNpc.ai[0] == NPC.whoAmI)
                    {
                        otherNpc.life = 0;
                        otherNpc.active = false;
                        // Sincronizar la "muerte" de esta noxina
                        if (Main.netMode == NetmodeID.Server)
                        {
                            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, i);
                        }
                    }
                }
            }
        }
        // --- FIN ---

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            // ... (tu loot de bolsa de tesoro) ...

            var classicDrops = new LeadingConditionRule(new Conditions.NotExpert());
            // SIEMPRE dropea su spawner
            classicDrops.OnSuccess(ItemDropRule.Common(ModContent.ItemType<NoxSpawner>(), 1));
            // ... (otros drops) ...
            npcLoot.Add(classicDrops);
        }
    }
}