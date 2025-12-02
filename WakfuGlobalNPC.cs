using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WakfuMod.Content.Items.Pets;   // Namespace de tu item de mascota
using WakfuMod.Content.Items.Mounts; // Namespace de tu item de montura
using Microsoft.Xna.Framework;

namespace WakfuMod.Common.GlobalNPCs // Ajusta el namespace
{
    public class WakfuGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        // --- XELOR TIME SUSPENSION ---
        public bool xelorSlowed = false;
        public Vector2 xelorRewindPos = Vector2.Zero;
        public Vector2 xelorOriginalVelocity = Vector2.Zero;

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
