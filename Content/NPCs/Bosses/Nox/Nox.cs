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

        private enum AI_State
        {
            Idle,
            StartBlink,
            EndBlink,
            Attacking,
            PhaseTransition // <-- ESTADO REINTRODUCIDO
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
            // Music = MusicID.Boss2;
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
            if (player.dead)
            {
                NPC.velocity.Y += 0.1f;
                if (NPC.timeLeft > 10) NPC.timeLeft = 10;
                return;
            }

            // Decrementar Cooldown de TimeRift
            if (NPC.ai[3] > 0)
            {
                NPC.ai[3]--;
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
                NPC.ai[1] = (float)AI_State.StartBlink;
                NPC.netUpdate = true;
            }
        }

        private void StartBlink(Player player, bool isPhase2)
        {
            NPC.velocity = Vector2.Zero;
            NPC.localAI[0]++; // Usar localAI[0] para la animación/fade

            NPC.alpha = (int)(255 * (NPC.localAI[0] / BlinkFadeTime));
            if (NPC.alpha > 255) NPC.alpha = 255;

            if (NPC.localAI[0] >= BlinkFadeTime)
            {
                SoundEngine.PlaySound(SoundID.Item8, NPC.position);

                // Eliminar noxinas viejas
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].active && Main.npc[i].type == ModContent.NPCType<Noxine>() && (int)Main.npc[i].ai[0] == NPC.whoAmI)
                    {
                        Main.npc[i].active = false;
                    }
                }

                // Buscar posición segura
                Vector2 newPosition = Vector2.Zero;
                for (int i = 0; i < 100; i++)
                {
                    float distance = Main.rand.NextFloat(300, 500);
                    newPosition = player.Center + Main.rand.NextVector2Circular(distance, distance);
                    if (!Collision.SolidCollision(newPosition - NPC.Size / 2f, NPC.width, NPC.height)) break;
                    if (i == 99) newPosition = player.Center + new Vector2(0, -300);
                }
                NPC.Center = newPosition;
                NPC.alpha = 255;

                // Restaurar Noxinas Orbitales
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
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
                }

                // Cambiar a estado de reaparición
                NPC.ai[0] = 0;
                NPC.localAI[0] = 0; // Resetear timer de animación para EndBlink
                NPC.ai[1] = (float)AI_State.EndBlink;
                NPC.netUpdate = true;
            }
        }

        private void EndBlink(Player player, bool isPhase2)
        {
            NPC.velocity = Vector2.Zero;
            NPC.localAI[0]++;

            NPC.alpha = 255 - (int)(255 * (NPC.localAI[0] / BlinkFadeTime));
            if (NPC.alpha < 0) NPC.alpha = 0;

            if (NPC.localAI[0] >= BlinkFadeTime)
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
                    int activePlayers = 0;
                    List<int> playerIndexes = new List<int>();
                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        if (Main.player[i].active && !Main.player[i].dead)
                        {
                            activePlayers++;
                            playerIndexes.Add(i);
                        }
                    }
                    if (activePlayers == 0) return;

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        foreach (int playerIndex in playerIndexes)
                        {
                            Player targetPlayer = Main.player[playerIndex];
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
                    SoundEngine.PlaySound(SoundID.Item111, NPC.Center);
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
                SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                Main.NewText("My childrens called me Daddy...", new Color(0, 200, 255));
                Main.NewText("My wife called me Milien...", new Color(0, 200, 255));
                Main.NewText("This world will learn to call me NOX", new Color(0, 200, 255));
                Main.NewText("-->Nox TimeStop your hands<--", new Color(255, 10, 10));
                Main.NewText("-->You cant attack, escape!<--", new Color(255, 10, 10));
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
            Main.NewText("Nox: ¿¡¡ONLY 20 MEASLY MINUTES!!?", new Color(0, 200, 255));
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