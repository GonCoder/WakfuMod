using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using WakfuMod.Content.Items.BossSpawners;
using WakfuMod.Content.Items.Currency;
using WakfuMod.Content.Projectiles;
using WakfuMod.ModSystems;
using Microsoft.Xna.Framework.Graphics;
using Terraria.DataStructures;

namespace WakfuMod.Content.NPCs.Bosses.Nox
{
    [AutoloadBossHead]
    public class Nox : ModNPC
    {
        // ==================== AI SLOT USAGE ====================
        // NPC.ai[0]: Timer general (incrementa cada tick)
        // NPC.ai[1]: Estado actual (0=Idle, 1=Blinking, 2=Attacking, 3=PhaseTransition)
        // NPC.ai[2]: Fase (0=Fase1, 1=Fase2)
        // NPC.ai[3]: Cooldown del TimeRift
        // NPC.localAI[0]: Flag de inicialización
        // NPC.localAI[1]: Estado anterior (para detectar cambios de sonido)

        // ==================== CONSTANTS ====================
        private const int BlinkDuration = 40; // 20 fade out + 20 fade in
        private const int IdleTime = 90;
        private const int IdleTimePhase2 = 45;
        private const int AttackDuration = 30;
        private const int TransitionDuration = 120;
        private const int TimeRiftCooldown = 1200; // 20 seconds
        private const int OrbitalNoxineCount = 12;
        private const int AttackerNoxineCount = 4;

        // Animation
        private const int Frame_Idle = 0;
        private const int Frame_Blink_Start = 1;
        private const int Total_Blink_Frames = 3;
        private const int Frame_Transition = 4;
        private const int Total_Frames = 5;
        private const int BlinkAnimSpeed = 5;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = Total_Frames;
            NPCID.Sets.MPAllowedEnemies[Type] = true;
            NPCID.Sets.BossBestiaryPriority.Add(Type);
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
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
            NPC.DeathSound = new SoundStyle("WakfuMod/audio/NoxDeath") { Volume = 5.5f };
            
            if (!Main.dedServ)
            {
                Music = MusicLoader.GetMusicSlot(Mod, "Assets/Music/NoxTheme");
            }
        }

        // Track if we just teleported to reset interpolation on clients
        private bool _justTeleported = false;

        public override void SendExtraAI(System.IO.BinaryWriter writer)
        {
            writer.Write(_justTeleported);
            _justTeleported = false; // Reset after sending
        }

        public override void ReceiveExtraAI(System.IO.BinaryReader reader)
        {
            bool teleported = reader.ReadBoolean();
            if (teleported)
            {
                // Reset interpolation on client
                NPC.oldPosition = NPC.position;
                NPC.netOffset = Vector2.Zero;
                for (int i = 0; i < NPC.oldPos.Length; i++)
                {
                    NPC.oldPos[i] = NPC.position;
                }
            }
        }

        public override void AI()
        {
            // ==================== INITIALIZATION ====================
            if (NPC.localAI[0] == 0)
            {
                NPC.localAI[0] = 1;
                NPC.TargetClosest(true);
                
                // Play spawn sound on clients
                if (Main.netMode != NetmodeID.Server)
                {
                    SoundEngine.PlaySound(new SoundStyle("WakfuMod/audio/NoxSpawn") { Volume = 5.5f }, NPC.Center);
                }
                
                NPC.netUpdate = true;
            }

            // ==================== TARGETING ====================
            NPC.TargetClosest(false);
            Player player = Main.player[NPC.target];
            
            if (player.dead || !player.active)
            {
                NPC.TargetClosest(false);
                player = Main.player[NPC.target];
                if (player.dead || !player.active)
                {
                    // Flee upward and despawn
                    NPC.velocity.Y -= 0.5f;
                    NPC.EncourageDespawn(10);
                    return;
                }
            }

            // ==================== PHASE CHECK ====================
            bool isPhase2 = NPC.ai[2] == 1;
            float healthRatio = (float)NPC.life / NPC.lifeMax;
            
            // Phase 2 trigger at 50% HP
            if (healthRatio <= 0.5f && NPC.ai[2] == 0)
            {
                NPC.ai[2] = 1; // Mark as Phase 2
                NPC.ai[1] = 3; // Go to PhaseTransition state
                NPC.ai[0] = 0; // Reset timer
                NPC.ai[3] = -1; // Flag to spawn TimeRift after transition
                isPhase2 = true;
                
                // Play transition sound on clients
                if (Main.netMode != NetmodeID.Server)
                {
                    SoundEngine.PlaySound(new SoundStyle("WakfuMod/audio/NoxPhaseTransition") { Volume = 1.5f }, NPC.Center);
                }
                
                NPC.netUpdate = true;
            }

            // Decrement TimeRift cooldown
            if (NPC.ai[3] > 0)
                NPC.ai[3]--;

            // ==================== STATE MACHINE ====================
            int state = (int)NPC.ai[1];
            
            switch (state)
            {
                case 0: // IDLE
                    DoIdle(player, isPhase2);
                    break;
                    
                case 1: // BLINKING (fade out + teleport + fade in)
                    DoBlink(player, isPhase2);
                    break;
                    
                case 2: // ATTACKING
                    DoAttack(player, isPhase2);
                    break;
                    
                case 3: // PHASE TRANSITION
                    DoPhaseTransition(player);
                    break;
                    
                default:
                    NPC.ai[1] = 0;
                    NPC.ai[0] = 0;
                    break;
            }

            // Detect state changes for sound effects
            if (NPC.ai[1] != NPC.localAI[1] && Main.netMode != NetmodeID.Server)
            {
                if (NPC.ai[1] == 1) // Started blinking
                    SoundEngine.PlaySound(new SoundStyle("WakfuMod/audio/NoxBlink") { Volume = 1.2f }, NPC.Center);
            }
            NPC.localAI[1] = NPC.ai[1];
        }

        private void DoIdle(Player player, bool isPhase2)
        {
            NPC.velocity *= 0.9f;
            NPC.alpha = 0;
            NPC.ai[0]++;

            int idleTime = isPhase2 ? IdleTimePhase2 : IdleTime;

            if (NPC.ai[0] >= idleTime)
            {
                // Transition to Blink state
                NPC.ai[0] = 0;
                NPC.ai[1] = 1;
                NPC.netUpdate = true;
            }
        }

        private void DoBlink(Player player, bool isPhase2)
        {
            NPC.velocity = Vector2.Zero;
            NPC.ai[0]++;

            int halfBlink = BlinkDuration / 2;

            if (NPC.ai[0] <= halfBlink)
            {
                // Fade out
                float progress = NPC.ai[0] / (float)halfBlink;
                NPC.alpha = (int)(255 * progress);
            }
            else if (NPC.ai[0] == halfBlink + 1)
            {
                // Teleport and spawn noxines (server/singleplayer only)
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    // Kill old noxines
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        if (Main.npc[i].active && Main.npc[i].type == ModContent.NPCType<Noxine>() && (int)Main.npc[i].ai[0] == NPC.whoAmI)
                        {
                            Main.npc[i].active = false;
                            if (Main.netMode == NetmodeID.Server)
                                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, i);
                        }
                    }

                    // Teleport near player
                    Vector2 newPos = player.Center + Main.rand.NextVector2CircularEdge(350f, 350f);
                    for (int i = 0; i < 50; i++)
                    {
                        if (!Collision.SolidCollision(newPos - NPC.Size / 2f, NPC.width, NPC.height))
                            break;
                        newPos = player.Center + Main.rand.NextVector2CircularEdge(350f, 350f);
                    }
                    NPC.Center = newPos;
                    
                    // Reset interpolation so sprite doesn't slide
                    NPC.oldPosition = NPC.position;
                    NPC.netOffset = Vector2.Zero;
                    for (int i = 0; i < NPC.oldPos.Length; i++)
                    {
                        NPC.oldPos[i] = NPC.position;
                    }
                    _justTeleported = true; // Flag for client sync

                    // Spawn orbital noxines
                    int count = isPhase2 ? OrbitalNoxineCount * 2 : OrbitalNoxineCount;
                    for (int i = 0; i < count; i++)
                    {
                        float angle = i * (360f / count);
                        int idx = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, 
                            ModContent.NPCType<Noxine>(), 0, NPC.whoAmI, angle, 0f, 0f);
                        if (Main.netMode == NetmodeID.Server && idx < Main.maxNPCs)
                            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, idx);
                    }
                }
                
                NPC.alpha = 255;
                NPC.netUpdate = true;
            }
            else
            {
                // Fade in
                float progress = (NPC.ai[0] - halfBlink) / (float)halfBlink;
                NPC.alpha = 255 - (int)(255 * progress);
            }

            if (NPC.ai[0] >= BlinkDuration)
            {
                // Transition to Attack state
                NPC.alpha = 0;
                NPC.ai[0] = 0;
                NPC.ai[1] = 2;
                NPC.netUpdate = true;
            }
        }

        private void DoAttack(Player player, bool isPhase2)
        {
            NPC.velocity *= 0.95f;
            NPC.ai[0]++;

            // Spawn attackers on first tick (server/singleplayer only)
            if (NPC.ai[0] == 1 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                // Check if should spawn TimeRift
                if (isPhase2 && NPC.ai[3] <= 0)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero, 
                        ModContent.ProjectileType<NoxTimeRift>(), 0, 0f, Main.myPlayer);
                    NPC.ai[3] = TimeRiftCooldown;
                }
                else
                {
                    // Spawn attacker noxines
                    int count = isPhase2 ? AttackerNoxineCount * 2 : AttackerNoxineCount;
                    for (int i = 0; i < count; i++)
                    {
                        float offset = (i - (count - 1) / 2f) * 0.4f;
                        Vector2 dir = (player.Center - NPC.Center).SafeNormalize(Vector2.UnitY);
                        Vector2 pos = NPC.Center + dir.RotatedBy(offset) * 80f;
                        
                        int idx = NPC.NewNPC(NPC.GetSource_FromAI(), (int)pos.X, (int)pos.Y, 
                            ModContent.NPCType<Noxine>(), 0, NPC.whoAmI, player.whoAmI, 0f, 1f);
                        if (Main.netMode == NetmodeID.Server && idx < Main.maxNPCs)
                            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, idx);
                    }
                }
                NPC.netUpdate = true;
            }

            if (NPC.ai[0] >= AttackDuration)
            {
                // Back to Idle
                NPC.ai[0] = 0;
                NPC.ai[1] = 0;
                NPC.netUpdate = true;
            }
        }

        private void DoPhaseTransition(Player player)
        {
            NPC.velocity = Vector2.Zero;
            NPC.dontTakeDamage = true;
            NPC.ai[0]++;

            // Visual effects on clients
            if (Main.netMode != NetmodeID.Server && NPC.ai[0] % 5 == 0)
            {
                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.MagicMirror, 0, 0, 150, Color.Cyan, 1.5f);
            }

            if (NPC.ai[0] >= TransitionDuration)
            {
                NPC.dontTakeDamage = false;
                
                // Spawn TimeRift if flagged (server/singleplayer only)
                if (NPC.ai[3] == -1 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero, 
                        ModContent.ProjectileType<NoxTimeRift>(), 0, 0f, Main.myPlayer);
                    NPC.ai[3] = TimeRiftCooldown;
                }

                // Back to Idle
                NPC.ai[0] = 0;
                NPC.ai[1] = 0;
                NPC.netUpdate = true;
            }
        }

        public override void FindFrame(int frameHeight)
        {
            int state = (int)NPC.ai[1];

            switch (state)
            {
                case 3: // Phase Transition
                    NPC.frame.Y = Frame_Transition * frameHeight;
                    break;

                case 1: // Blinking
                    int blinkFrame = (int)(NPC.ai[0] / BlinkAnimSpeed);
                    if (blinkFrame >= Total_Blink_Frames)
                        blinkFrame = Total_Blink_Frames - 1;
                    NPC.frame.Y = (Frame_Blink_Start + blinkFrame) * frameHeight;
                    break;

                default: // Idle, Attacking
                    NPC.frame.Y = Frame_Idle * frameHeight;
                    break;
            }
        }

        public override void OnKill()
        {
            Main.NewText("Nox: ¿¡¡ONLY 20 MEASLY MINUTES!!?", new Color(0, 200, 255));
            NoxDefeatSystem.SetNoxDefeated();
            ModContent.GetInstance<NoxSpawnSystem>().OnNoxDefeated();

            // Kill all noxines
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].active && Main.npc[i].type == ModContent.NPCType<Noxine>() && (int)Main.npc[i].ai[0] == NPC.whoAmI)
                    {
                        Main.npc[i].life = 0;
                        Main.npc[i].active = false;
                        if (Main.netMode == NetmodeID.Server)
                            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, i);
                    }
                }
            }
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            // Drop de 10 Kamas (Nox es más difícil que Toross)
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Kama>(), 1, 10, 15));
            
            var classicDrops = new LeadingConditionRule(new Conditions.NotExpert());
            classicDrops.OnSuccess(ItemDropRule.Common(ModContent.ItemType<NoxSpawner>(), 1));
            classicDrops.OnSuccess(ItemDropRule.Common(ItemID.SoulofNight, 1, 5, 10));
            npcLoot.Add(classicDrops);
        }
    }
}
