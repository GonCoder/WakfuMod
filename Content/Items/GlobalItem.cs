using Terraria;
using Terraria.ModLoader;
using WakfuMod.jugador;

namespace WakfuMod.Content.Items
{
    public class WakfuGlobalItem : GlobalItem
    {
        // --- OnHitNPC: GANA RAGE con CUALQUIER GOLPE MELEE DIRECTO ---
         public override void OnHitNPC(Item item, Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!player.TryGetModPlayer<WakfuPlayer>(out var wakfuPlayer)) return;
            if (wakfuPlayer.claseElegida != WakfuClase.Yopuka) return;

            // --- LLAMADA CORREGIDA ---
            // Si el golpe directo es Melee, llama al método específico para items
            if (item.DamageType == DamageClass.Melee)
            {
                wakfuPlayer.TryGainRageFromItemHit(); // <-- Llama al nuevo método
            }
        }


        public override void ModifyHitNPC(Item item, Player player, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (player.GetModPlayer<WakfuPlayer>().claseElegida == WakfuClase.Yopuka)
            {
                modifiers.FinalDamage *= player.GetModPlayer<WakfuPlayer>().GetRageMultiplier();
            }
        }
    }
}
