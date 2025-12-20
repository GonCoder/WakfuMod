using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WakfuMod.Content.Items.Pets;   // Namespace de tu item de mascota
using WakfuMod.Content.Items.Mounts; // Namespace de tu item de montura
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;

namespace WakfuMod.Common.GlobalNPCs // Ajusta el namespace
{
    public class WakfuGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        // --- XELOR TIME SUSPENSION ---
        public bool xelorSlowed = false;
        public Vector2 xelorRewindPos = Vector2.Zero;
        public Vector2 xelorOriginalVelocity = Vector2.Zero;

        // --- OCRA ARMOR SHRED ---
        public int ocraDefenseReduction = 0;
        
        // --- UGINAK HUNTER'S MARK ---
        public bool uginakMarked = false;
        public int uginakMarkedByPlayer = -1; // WhoAmI del jugador que marcó

        public override void ResetEffects(NPC npc)
        {
            // No reseteamos xelorSlowed aquí porque dura varios frames controlado por el jugador
        }

        public override void PostAI(NPC npc)
        {
            if (xelorSlowed)
            {
                npc.velocity *= 0.92f; // Ralentizar un 8% por frame
                
                // Efecto visual opcional
                if (Main.rand.NextBool(5))
                {
                    Dust.NewDust(npc.position, npc.width, npc.height, DustID.Electric, 0, 0, 150, Color.Purple, 0.5f);
                }
            }
            
            // Efecto visual para marca del cazador
            if (uginakMarked)
            {
                if (Main.rand.NextBool(3))
                {
                    Dust.NewDust(npc.position, npc.width, npc.height, DustID.Torch, 0, 0, 100, Color.Orange, 1.2f);
                }
            }
        }

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (uginakMarked)
            {
                // Dibujar una X roja sobre el NPC
                Texture2D texture = TextureAssets.MagicPixel.Value;
                Rectangle rect = npc.getRect();
                Vector2 center = npc.Center - screenPos;
                
                float size = Math.Max(npc.width, npc.height) * 0.5f;
                float thickness = 4f;
                
                // Línea 1 \
                spriteBatch.Draw(texture, center, new Rectangle(0, 0, 1, 1), Color.Red, MathHelper.PiOver4, new Vector2(0.5f, 0.5f), new Vector2(size * 2, thickness), SpriteEffects.None, 0);
                // Línea 2 /
                spriteBatch.Draw(texture, center, new Rectangle(0, 0, 1, 1), Color.Red, -MathHelper.PiOver4, new Vector2(0.5f, 0.5f), new Vector2(size * 2, thickness), SpriteEffects.None, 0);
            }
        }

         // --- USA ESTE MÉTODO EN SU LUGAR ---
         public override void ModifyShop(NPCShop shop) // O podría ser ModifyTravelShop
        {
            if (shop.NpcType == NPCID.Merchant)
            {
                // Añadir item y luego condición
                shop.Add(ModContent.ItemType<JuniorPet>()); // Sin condición
                shop.Add(ModContent.ItemType<KamasutarMount>());

                // Para precios personalizados:
                // shop.Add(new Item(ModContent.ItemType<JuniorPet>()) { shopCustomPrice = Item.buyPrice(gold: 1) });
                // shop.Add(new Item(ModContent.ItemType<KamasutarMount>()) { shopCustomPrice = Item.buyPrice(gold: 1) });
                // O con condiciones:
                // shop.Add(new Item(ModContent.ItemType<JuniorPetItem>()) { shopCustomPrice = Item.buyPrice(gold: 1) }, Condition.DownedEyeOfCthulhu);
            }

            // Para el Traveling Merchant, el hook es diferente
            // if (shop.NpcType == NPCID.TravellingMerchant) { ... }
        }

        // Si la tienda del Mercader es una "TravelShop" en tu versión, el hook podría ser:
        /*
        public override void ModifyTravelShop(NPCShop shop)
        {
            if (shop.NpcType == NPCID.Merchant) // Aunque Mercader no es típicamente un travel shop
            {
                shop.Add(ModContent.ItemType<JuniorPetItem>());
                shop.Add(ModContent.ItemType<KamasutarItem>(), Condition.Hardmode);
            }
        }
        */
    }
    }
