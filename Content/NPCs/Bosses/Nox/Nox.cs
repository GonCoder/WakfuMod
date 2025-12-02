using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using WakfuMod.Content.Items.BossSpawners;
using System.Collections.Generic;
using WakfuMod.Content.Projectiles;
using WakfuMod.ModSystems;
using Microsoft.Xna.Framework.Graphics;
using Terraria.DataStructures;
using Terraria.Chat;
using Terraria.Localization;

namespace WakfuMod.Content.NPCs.Bosses.Nox
{
    [AutoloadBossHead]
    public class Nox : ModNPC
    {
        // --- Constantes de Animación ---
        private const int Frame_Idle = 0;
        private const int Frame_Blink_Start = 1;
        private const int Total_Blink_Frames = 3;
        private const int Frame_Transition_Start = 4; // El quinto frame (índice 4)
        private const int Total_Frames_In_Sheet = 5; // Total: 0=Idle, 1,2,3=Blink
        private const int BlinkAnimSpeed = 5;

        // --- Constantes de Combate ---
        private const int OrbitalNoxineCount = 12;
        private const int AttackerNoxineCountPerPlayer = 4;
        private const int BlinkFadeTime = 20;
        private const int Transition_Anim_Duration = 120; // 2 segundos de animación de transición
        private const int TimeRiftCooldown = 20 * 60; // 20 segundos

        // --- Variables de IA ---
        // ai[0]: Temporizador general para la duración del estado actual (Idle, Attacking)
        // ai[1]: El estado actual de la IA
        // ai[2]: Fase del combate (0 = Fase 1, 1 = Fase 2)
        // ai[3]: Cooldown para la habilidad TimeRift
        // localAI[0]: Temporizador para la animación de blink

        private float _lastAIState = -1f; // Para detectar cambios de estado y resetear localAI
        private List<int> _targetSequence = new List<int>(); // Secuencia de objetivos para "Shuffle Bag"

        private enum AI_State
        {
            Idle,
            StartBlink,
            EndBlink,
            Attacking,
            PhaseTransition
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
            NPC.DeathSound = new SoundStyle("WakfuMod/audio/NoxDeath")
            {
                Volume = 5.5f,
            };
            Music = MusicLoader.GetMusicSlot(Mod, "Assets/Music/NoxTheme");
        }

        public override void AI()
        {
            // --- Comprobación de Jugador y Despawn ---
            if (NPC.target < 0 || NPC.target == 255 || Main.player[NPC.target].dead || !Main.player[NPC.target].active)
            {
                NPC.TargetClosest(true);
            }
            Player player = Main.player[NPC.target];
            
            // Si después de TargetClosest el jugador sigue siendo inválido, NO DESPAWNEAR INMEDIATAMENTE
            if (player.dead || !player.active)
            {
                NPC.velocity.Y += 0.1f;
                // NPC.EncourageDespawn(10); // --- DESACTIVADO: Causaba desaparición prematura ---
                NPC.timeLeft = 1000; // Mantener vivo para evitar "One Hit Kill" por despawn
                return;
            }
            else
            {
                // Mantener vivo si hay jugador válido
                NPC.timeLeft = 1000; 
            }

            // Decrementar Cooldown de TimeRift
            if (NPC.ai[3] > 0)
            {
                NPC.ai[3]--;
            }

            // --- Detectar Cambio de Estado para Resetear localAI (Sincronización) ---
            if (NPC.ai[1] != _lastAIState)
            {
                NPC.localAI[0] = 0;

                // --- SONIDOS Y EFECTOS AL CAMBIAR DE ESTADO ---
                if ((AI_State)NPC.ai[1] == AI_State.PhaseTransition)
                {
                    SoundEngine.PlaySound(new SoundStyle("WakfuMod/audio/NoxPhaseTransition") { Volume = 1.5f }, NPC.Center);
                    // ... (Chat messages logic remains) ...
                }
                else if ((AI_State)NPC.ai[1] == AI_State.EndBlink)
                {
                    // Reproducir sonido de reaparición (Blink End)
                    SoundEngine.PlaySound(new SoundStyle("WakfuMod/audio/NoxBlink") { Volume = 1.2f }, NPC.Center);
                    // Asegurar que alpha se resetea o se maneja en EndBlink
                    // Pero EndBlink empieza con alpha 255 y baja.
                }
                else if ((AI_State)NPC.ai[1] == AI_State.StartBlink)
                {
                    // Reproducir sonido de desaparición (Blink Start)
                    SoundEngine.PlaySound(new SoundStyle("WakfuMod/audio/NoxBlink") { Volume = 1.2f }, NPC.Center);
                }

                _lastAIState = NPC.ai[1];
            }

            // --- Lógica de Transición de Fase (SIMPLIFICADA) ---
            bool isPhase2 = (float)NPC.life / NPC.lifeMax <= 0.5f;
            if (isPhase2 && NPC.ai[2] == 0)
            {
                NPC.ai[2] = 1; // Marcar como Fase 2 "iniciada"
                NPC.ai[3] = -1; // --- FLAG: El próximo estado después del blink será PhaseTransition ---
                NPC.ai[1] = (float)AI_State.StartBlink; // Forzar un blink
                NPC.netUpdate = true;
                return; // Salir para ejecutar el blink en el próximo tick
            }

            // --- Máquina de Estados ---
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
                case AI_State.PhaseTransition:
                    PhaseTransition(player);
                    break;
                default:
                    NPC.ai[1] = (float)AI_State.Idle;
                    break;
            }
        }

        private void Idle(Player player, bool isPhase2)
        {
            NPC.ai[0]++;
            NPC.velocity *= 0.9f;
            int idleTime = isPhase2 ? 45 : 90;
            if (NPC.ai[0] >= idleTime)
            {
                NPC.ai[0] = 0;
                NPC.localAI[0] = 0;
                
                // --- SELECCIONAR NUEVO OBJETIVO (Shuffle Bag) ---
                PickNextTarget();

                NPC.ai[1] = (float)AI_State.StartBlink;
                NPC.netUpdate = true;
            }
        }

        private void PickNextTarget()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return; // Solo el servidor decide

            // Limpiar jugadores inválidos de la secuencia
            _targetSequence.RemoveAll(id => !Main.player[id].active || Main.player[id].dead);

            // Si la secuencia está vacía, rellenarla y barajar
            if (_targetSequence.Count == 0)
            {
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    if (Main.player[i].active && !Main.player[i].dead)
                    {
                        _targetSequence.Add(i);
                    }
                }

                // Barajar (Fisher-Yates)
                int n = _targetSequence.Count;
                while (n > 1)
                {
                    n--;
                    int k = Main.rand.Next(n + 1);
                    int value = _targetSequence[k];
                    _targetSequence[k] = _targetSequence[n];
                    _targetSequence[n] = value;
                }
            }

            // Seleccionar el siguiente
            if (_targetSequence.Count > 0)
            {
                NPC.target = _targetSequence[0];
                _targetSequence.RemoveAt(0);
                NPC.netUpdate = true; // Sincronizar el nuevo target
            }
            else
            {
                NPC.TargetClosest(true); // Fallback si no hay nadie en la lista (raro)
            }
        }

        private void StartBlink(Player player, bool isPhase2)
        {
            NPC.velocity = Vector2.Zero;
            NPC.ai[0]++; // Usar ai[0] (sincronizado) en lugar de localAI[0]

            NPC.alpha = (int)(255 * (NPC.ai[0] / BlinkFadeTime));
            if (NPC.alpha > 255) NPC.alpha = 255;

            // --- LÓGICA DE TELETRANSPORTE (SOLO SERVIDOR) ---
            // Usamos ai[0] para sincronizar el tiempo.
            if (Main.netMode != NetmodeID.MultiplayerClient && NPC.ai[0] >= BlinkFadeTime)
            {
                // SoundEngine.PlaySound... (Movido a AI() para que suene en clientes si es necesario, o confiamos en el de EndBlink)

                // Eliminar noxinas viejas
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].active && Main.npc[i].type == ModContent.NPCType<Noxine>() && (int)Main.npc[i].ai[0] == NPC.whoAmI)
                    {
                        Main.npc[i].active = false;
                        if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, i); // Sincronizar muerte
                    }
                }

                // Buscar posición segura (SOLO SERVIDOR)
                // Validar target actual (si el elegido murió o desconectó)
                if (NPC.target < 0 || NPC.target == 255 || Main.player[NPC.target].dead || !Main.player[NPC.target].active)
                {
                     NPC.TargetClosest(true);
                }
                Player targetPlayer = Main.player[NPC.target]; // Usar el objetivo elegido en Idle
                
                // --- FIX: Asegurar que targetPlayer es válido antes de usarlo ---
                if (targetPlayer == null || !targetPlayer.active || targetPlayer.dead)
                {
                     // Si no hay target válido, intentar buscar otro
                     NPC.TargetClosest(true);
                     targetPlayer = Main.player[NPC.target];
                }

                Vector2 newPosition = NPC.Center; // Posición por defecto: quedarse donde está
                
                // Solo intentar calcular nueva posición si tenemos un objetivo válido
                if (targetPlayer != null && targetPlayer.active && !targetPlayer.dead)
                {
                    for (int i = 0; i < 100; i++)
                    {
                        float distance = Main.rand.NextFloat(300, 500);
                        newPosition = targetPlayer.Center + Main.rand.NextVector2Circular(distance, distance);
                        if (!Collision.SolidCollision(newPosition - NPC.Size / 2f, NPC.width, NPC.height)) break;
                        if (i == 99) newPosition = targetPlayer.Center + new Vector2(0, -300);
                    }
                }
                
                NPC.Center = newPosition;
                NPC.alpha = 255;

                // Restaurar Noxinas Orbitales
                int orbitalCount = isPhase2 ? 24 : OrbitalNoxineCount;
                for (int i = 0; i < orbitalCount; i++)
                {
                    int noxineType = ModContent.NPCType<Noxine>();
                    int npcIndex = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, noxineType, 0,
                        NPC.whoAmI, i * (360f / orbitalCount), 0f, 0f);
                    if (npcIndex < Main.maxNPCs)
                    {
                        Main.npc[npcIndex].localAI[0] = 0;
                        if (Main.netMode == NetmodeID.Server)
                        {
                            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npcIndex);
                        }
                    }
                }

                // Cambiar a estado de reaparición
                NPC.ai[0] = 0; // Resetear timer para EndBlink
                NPC.localAI[0] = 0; 
                NPC.ai[1] = (float)AI_State.EndBlink;
                
                // --- SINCRONIZACIÓN ESTÁNDAR (COMO TOROSS) ---
                // Al cambiar ai[1], Center y alpha en el servidor, netUpdate = true enviará todo a los clientes.
                NPC.netUpdate = true; 
            }
            // --- CLIENTE: Evitar que ai[0] crezca indefinidamente si el paquete se retrasa ---
            else if (Main.netMode == NetmodeID.MultiplayerClient && NPC.ai[0] > BlinkFadeTime + 60)
            {
                // Si llevamos mucho tiempo esperando (1 segundo extra), forzar visibilidad para no quedarnos invisibles
                NPC.alpha = 0; 
            }
        }

        private void EndBlink(Player player, bool isPhase2)
        {
            NPC.velocity = Vector2.Zero;
            NPC.ai[0]++; // Usar ai[0] (sincronizado)

            NPC.alpha = 255 - (int)(255 * (NPC.ai[0] / BlinkFadeTime));
            if (NPC.alpha < 0) NPC.alpha = 0;

            if (NPC.ai[0] >= BlinkFadeTime)
            {
                NPC.alpha = 0;
                NPC.ai[0] = 0;
                NPC.localAI[0] = 0;

                // --- LÓGICA DE DECISIÓN POST-BLINK ---
                if (NPC.ai[3] == -1) // Si el flag de transición está activo
                {
                    NPC.ai[1] = (float)AI_State.PhaseTransition; // Ir a la animación de transición
                    NPC.ai[3] = 0; // Resetear el flag/cooldown
                }
                else // Si es un blink normal
                {
                    NPC.ai[1] = (float)AI_State.Attacking; // Ir al ataque normal
                }
                NPC.netUpdate = true;
            }
        }

        private void Attacking(Player player, bool isPhase2)
        {
            NPC.ai[0]++;
            NPC.velocity *= 0.95f;

            if (NPC.ai[0] == 1) // Ejecutar solo una vez al entrar en este estado
            {
                // --- Lógica de Habilidad de Domo de Tiempo ---
                if (isPhase2 && NPC.ai[3] <= 0)
                {
                    // Usar habilidad
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<NoxTimeRift>(), 0, 0f, Main.myPlayer);
                    }
                    NPC.ai[3] = TimeRiftCooldown; // Poner en cooldown
                    NPC.netUpdate = true;
                }
                // --- Lógica de lanzar noxinas de ataque (si el domo no está activo) ---
                else
                {
                    int attackerCount = isPhase2 ? 8 : AttackerNoxineCountPerPlayer;
                    
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        // Atacar SOLO al objetivo actual (seleccionado en Idle)
                        Player targetPlayer = Main.player[NPC.target];
                        if (targetPlayer.active && !targetPlayer.dead)
                        {
                            for (int i = 0; i < attackerCount; i++)
                            {
                                float angleOffset = (i - (attackerCount - 1) / 2f) * 0.4f;
                                Vector2 directionToPlayer = (targetPlayer.Center - NPC.Center).SafeNormalize(Vector2.UnitY);
                                Vector2 spawnPos = NPC.Center + directionToPlayer.RotatedBy(angleOffset) * 80f;
                                int noxineType = ModContent.NPCType<Noxine>();
                                int npcIndex = NPC.NewNPC(NPC.GetSource_FromAI(), (int)spawnPos.X, (int)spawnPos.Y, noxineType, 0,
                                    NPC.whoAmI, targetPlayer.whoAmI, 0f, 1f); // Modo 1 = Atacante
                                if (Main.netMode == NetmodeID.Server && npcIndex < Main.maxNPCs)
                                {
                                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npcIndex);
                                }
                            }
                        }
                    }
                    SoundEngine.PlaySound(new SoundStyle("WakfuMod/audio/NoxineSummonAttack"), NPC.position);
                }
            }

            // Después de un breve momento, volver a Idle
            if (NPC.ai[0] >= 30)
            {
                NPC.ai[0] = 0;
                NPC.ai[1] = (float)AI_State.Idle;
                NPC.netUpdate = true;
            }
        }

        // --- MÉTODO DE TRANSICIÓN DE FASE ---
        private void PhaseTransition(Player player)
        {
            NPC.dontTakeDamage = true;
            NPC.velocity = Vector2.Zero;
            NPC.ai[0]++;

            if (NPC.ai[0] == 1)
            {
                // La lógica de sonido y texto se ha movido a AI() para garantizar sincronización
            }
            if (NPC.ai[0] % 5 == 0)
            {
                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.MagicMirror, 0, 0, 150, Color.Cyan, 1.5f);
            }

            if (NPC.ai[0] >= Transition_Anim_Duration)
            {
                NPC.dontTakeDamage = false;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<NoxTimeRift>(), 0, 0f, Main.myPlayer);
                }
                NPC.ai[3] = TimeRiftCooldown; // Poner TimeRift en cooldown

                NPC.ai[0] = 0;
                NPC.ai[1] = (float)AI_State.Idle; // Volver al ciclo normal
                NPC.netUpdate = true;
            }
        }


        public override void FindFrame(int frameHeight) // Aunque recibimos frameHeight, lo ignoraremos y calcularemos el nuestro
        {
            // --- Cargar la textura para obtener sus dimensiones reales ---
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value; // Asume que no hay override de Texture, usa autoloading
            int actualFrameHeight = texture.Height / Total_Frames_In_Sheet; // Nuestra altura de frame calculada y precisa

            // --- El resto de la lógica de animación es la misma que antes ---
            AI_State currentState = (AI_State)NPC.ai[1];

            switch (currentState)
            {
                // --- AÑADIR CASO PARA LA TRANSICIÓN ---
                case AI_State.PhaseTransition:
                    NPC.frame.Y = Frame_Transition_Start * frameHeight;
                    break;

                case AI_State.StartBlink:
                case AI_State.EndBlink:
                    int blinkFrameIndex = (int)(NPC.ai[0] / BlinkAnimSpeed); // Usar ai[0] en lugar de ai[3] o localAI[0]
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

        // --- MÉTODO PARA CONTROLAR LA INVOCACIÓN ---
        public override void OnSpawn(IEntitySource source)
        {
            // --- REPRODUCIR SONIDO DE INVOCACIÓN PERSONALIZADO ---
            // Solo reproducir el sonido en los clientes para evitar problemas en el servidor dedicado.
            if (!Main.dedServ)
            {
                // Reemplaza "Nox/NoxSpawnSound" con la ruta a tu archivo de sonido.
                SoundEngine.PlaySound(new SoundStyle("WakfuMod/audio/NoxSpawn")
                {
                    Volume = 5.5f,
                },
                 NPC.Center);
            }

        }

        public override void OnKill()
        {
            Main.NewText("Nox: ¿¡¡ONLY 20 MEASLY MINUTES!!?", new Color(0, 200, 255));
            NoxDefeatSystem.SetNoxDefeated();
            ModContent.GetInstance<NoxSpawnSystem>().OnNoxDefeated();

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC otherNpc = Main.npc[i];
                    if (otherNpc.active && otherNpc.type == ModContent.NPCType<Noxine>() && (int)otherNpc.ai[0] == NPC.whoAmI)
                    {
                        otherNpc.life = 0;
                        otherNpc.active = false;
                        if (Main.netMode == NetmodeID.Server)
                        {
                            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, i);
                        }
                    }
                }
            }
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            var classicDrops = new LeadingConditionRule(new Conditions.NotExpert());
            classicDrops.OnSuccess(ItemDropRule.Common(ModContent.ItemType<NoxSpawner>(), 1));
            classicDrops.OnSuccess(ItemDropRule.Common(ItemID.SoulofNight, 1, 5, 10));
            npcLoot.Add(classicDrops);

        }
    }
}