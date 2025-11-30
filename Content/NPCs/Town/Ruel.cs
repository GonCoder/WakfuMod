// Content/NPCs/Town/Ruel.cs
using Microsoft.Xna.Framework;               // Vector2
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent.Personalities;
using System.Collections.Generic;
using WakfuMod.Content.Items.Weapons;
using WakfuMod.Content.Items.Currency;
using WakfuMod.Content.Items.BossSpawners;
using WakfuMod.ModSystems;
using Terraria.Utilities;
using Terraria.GameContent.ItemDropRules;
using System;
using WakfuMod.Content.Items.Mounts;
using WakfuMod.Content.Items.Pets;
using WakfuMod.Content.Items.Consumables;
using Terraria.Audio;                        // SoundEngine
using WakfuMod.Content.Projectiles;          // RuelCoinDrop

namespace WakfuMod.Content.NPCs.Town
{
    [AutoloadHead]
    public class Ruel : ModNPC
    {
        private const string ShopName = "TiendaRuel";

        // === Ataque manual ===
        private const int AttackRange = 450;
        private const int AttackCooldownTicks = 60; // 1s entre ataques; ajústalo a gusto
        private int ruelAttackTimer = 0;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 8;

            NPCID.Sets.ExtraFramesCount[Type] = 9;
            NPCID.Sets.AttackFrameCount[Type] = 4;
            NPCID.Sets.DangerDetectRange[Type] = AttackRange; // rango de “veo enemigo”
            NPCID.Sets.AttackType[Type] = 0;
            NPCID.Sets.AttackTime[Type] = 45;
            NPCID.Sets.AttackAverageChance[Type] = 20;
            NPCID.Sets.HatOffsetY[Type] = 4;
            NPCID.Sets.CannotSitOnFurniture[Type] = false;
            NPCID.Sets.CantTakeLunchMoney[Type] = true;

            NPC.Happiness
                .SetBiomeAffection<UndergroundBiome>(AffectionLevel.Love)
                .SetBiomeAffection<DesertBiome>(AffectionLevel.Like)
                .SetBiomeAffection<SnowBiome>(AffectionLevel.Dislike)
                .SetBiomeAffection<HallowBiome>(AffectionLevel.Hate)
                .SetNPCAffection(NPCID.GoblinTinkerer, AffectionLevel.Love)
                .SetNPCAffection(NPCID.Merchant, AffectionLevel.Like)
                .SetNPCAffection(NPCID.TaxCollector, AffectionLevel.Hate)
                .SetNPCAffection(NPCID.Pirate, AffectionLevel.Dislike);
        }

        public override void SetDefaults()
        {
            NPC.width = 25;
            NPC.height = 80;
            NPC.lifeMax = 666;
            NPC.defense = 30;
            NPC.knockBackResist = 0.4f;
            NPC.aiStyle = 7;
            NPC.townNPC = true;
            NPC.friendly = true;
            NPC.damage = 69;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;

            AnimationType = -1; // usamos FindFrame()
        }

        public override bool CanTownNPCSpawn(int numTownNPCs)
        {
            return ModContent.GetInstance<NoxDefeatSystem>().noxDefeated;
        }

        public override List<string> SetNPCNameList()
        {
            return new List<string>() {
                "Ruel Stroud"
            };
        }

        public override string GetChat()
        {
            WeightedRandom<string> chat = new WeightedRandom<string>(Main.rand);

            chat.Add("Hey, young man! Looking for treasure? I have just what you need... for a modest price in Kamas.", 1.0);
            chat.Add("Where are Eva and Amalia? Did you see 'em?", 1.0);
            chat.Add("Your 'gold' looks like Kamas, can you give me?", 1.0);
            if (Main.LocalPlayer.HasItem(ModContent.ItemType<Kama>()))
            {
                chat.Add("¡Is that a Kama?!", 1.5);
            }
            if (NPC.AnyNPCs(NPCID.TaxCollector))
            {
                chat.Add("I hope that son of a 'Gobbly' tax collector doesn't come near my Kamas!", 0.8);
            }

            return chat;
        }

        public override void SetChatButtons(ref string button, ref string button2)
        {
            button = Lang.inter[28].Value;
        }

        public override void OnChatButtonClicked(bool firstButton, ref string shopName)
        {
            if (firstButton)
            {
                shopName = ShopName;
            }
        }

        public override void AddShops()
        {
            int kamaCurrencyId = WakfuMod.KamaCurrencyId;

            var ruelShop = new NPCShop(Type, ShopName)
                .Add(new Item(ModContent.ItemType<YopukaShockwaveSword>())
                {
                    shopCustomPrice = 1,
                    shopSpecialCurrency = kamaCurrencyId
                })
                .Add(new Item(ModContent.ItemType<WakmehamehaWeapon>())
                {
                    shopCustomPrice = 1,
                    shopSpecialCurrency = kamaCurrencyId
                })
                .Add(new Item(ModContent.ItemType<SteamerPistol>())
                {
                    shopCustomPrice = 1,
                    shopSpecialCurrency = kamaCurrencyId
                })
                .Add(new Item(ModContent.ItemType<TymadorKick>())
                {
                    shopCustomPrice = 1,
                    shopSpecialCurrency = kamaCurrencyId
                })
                .Add(new Item(ModContent.ItemType<KamasutarMount>())
                {
                    shopCustomPrice = 1,
                    shopSpecialCurrency = kamaCurrencyId
                })
                .Add(new Item(ModContent.ItemType<JuniorPet>())
                {
                    shopCustomPrice = 1,
                    shopSpecialCurrency = kamaCurrencyId
                })
                .Add(new Item(ModContent.ItemType<Jalaball>())
                {
                    shopCustomPrice = 1,
                    shopSpecialCurrency = kamaCurrencyId
                })
                .Add(new Item(ModContent.ItemType<NoxSpawner>())
                {
                    shopCustomPrice = Item.buyPrice(gold: 5, silver: 25)
                })
                .Add(new Item(ModContent.ItemType<TorossSpawner>())
                {
                    shopCustomPrice = Item.buyPrice(gold: 15)
                });

            ruelShop.Register();
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Kama>(), 1, 5, 10));
        }

        public override void AI()
        {
            // Que mire hacia donde se mueve
            if (NPC.velocity.X > 0.1f)
            {
                NPC.direction = 1;
                NPC.spriteDirection = -1;
            }
            else if (NPC.velocity.X < -0.1f)
            {
                NPC.direction = -1;
                NPC.spriteDirection = 1;
            }

            // --- Ataque manual si ve un enemigo ---
            if (Main.netMode != NetmodeID.MultiplayerClient) // evitar doble spawn en MP
            {
                if (ruelAttackTimer > 0)
                    ruelAttackTimer--;

                if (ruelAttackTimer <= 0)
                {
                    NPC target = null;
                    float bestDist = AttackRange;

                    for (int i = 0; i < Main.npc.Length; i++)
                    {
                        var n = Main.npc[i];
                        if (n.active && !n.friendly && !n.townNPC && n.CanBeChasedBy(NPC, false))
                        {
                            float d = Vector2.Distance(NPC.Center, n.Center);
                            if (d <= bestDist)
                            {
                                bestDist = d;
                                target = n;
                            }
                        }
                    }

                    if (target != null)
                    {
                        var src = NPC.GetSource_FromAI();

                        int p = Projectile.NewProjectile(
                            src,
                            NPC.Center,
                            Vector2.Zero,
                            ModContent.ProjectileType<RuelCoinDrop>(),
                            15,   // daño; cámbialo si quieres
                            1f,
                            Main.myPlayer,
                            0f,
                            target.whoAmI // ai[1] = objetivo
                        );

                        if (p >= 0)
                        {
                            SoundEngine.PlaySound(SoundID.Coins, NPC.Center);
                            ruelAttackTimer = AttackCooldownTicks;
                        }
                    }
                }
            }

            // IA base del town
            base.AI();
        }

        // Animación personalizada
        public override void FindFrame(int frameHeight)
        {
            const int idleFrame = 0;
            const int walkStart = 1;
            const int walkEnd = 7;
            const int ticksPerFrame = 8;

            if (Math.Abs(NPC.velocity.X) < 0.1f)
            {
                NPC.frame.Y = idleFrame * frameHeight;
                NPC.frameCounter = 0;
            }
            else
            {
                NPC.frameCounter++;
                if (NPC.frameCounter >= ticksPerFrame)
                {
                    NPC.frameCounter = 0;
                    int currentFrame = NPC.frame.Y / frameHeight;
                    currentFrame++;
                    if (currentFrame > walkEnd)
                    {
                        currentFrame = walkStart;
                    }
                    NPC.frame.Y = currentFrame * frameHeight;
                }
            }
        }
    }
}
