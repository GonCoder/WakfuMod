using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.Audio;
using Terraria.ID;

namespace WakfuMod.Content.Projectiles
{
    public static class TymadorBombManager
    {
        public static List<Projectile> ActiveBombs = new();

        public static void RegisterBomb(Projectile proj)
        {
            // 💣 Elimina la más antigua si hay más de 3 bombas
            if (ActiveBombs.Count >= 3)
            {
                if (ActiveBombs[0] != null && ActiveBombs[0].active)
                {
                    ActiveBombs[0].Kill();
                }
                ActiveBombs.RemoveAt(0);
            }

            ActiveBombs.Add(proj);

            // 🧠 Actualiza los tiers: la bomba más reciente es Tier 0, la más antigua Tier 2
            for (int i = 0; i < ActiveBombs.Count; i++)
            {  PlayTierChangeEvent(ActiveBombs[0]);
                ActiveBombs[i].ai[0] = ActiveBombs.Count - 1 - i;
                ActiveBombs[i].netUpdate = true;

            }
        }

        public static void RemoveBomb(Projectile proj)
        {
            ActiveBombs.Remove(proj);
        }

         // --- Función Helper para Efecto Visual de Cambio de Tier ---
        private static void PlayTierChangeEvent(Projectile bomb)
        {
            if (Main.netMode != NetmodeID.Server)
            {
                // Sonido
                SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.6f, Pitch = 0.3f }, bomb.position);

                // Polvo
                for (int i = 0; i < 8; i++)
                {
                    Dust dust = Dust.NewDustDirect(bomb.position, bomb.width, bomb.height, DustID.Smoke,
                                        Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-1.5f, 0.5f),
                                        100, default, 1.1f);
                    dust.noGravity = true;
                    dust.velocity *= 0.5f;
                }
                // Polvo de color opcional según el NUEVO tier
                int dustType = (int)bomb.ai[0] switch { 1 => DustID.OrangeTorch, 2 => DustID.RedTorch, _ => -1 }; // -1 para no spawnear si es tier 0
                if (dustType != -1) {
                    for (int i = 0; i < 4; i++) {
                        Dust.NewDustPerfect(bomb.Center, dustType, Main.rand.NextVector2Circular(1f, 1f), 0, default, 0.9f).noGravity=true;
                    }
                }
            }
        }

        public static void DrawBombLinks()
        {
            if (Main.spriteBatch == null || Main.gameMenu) return;

            for (int i = 0; i < ActiveBombs.Count - 1; i++)
            {
                Vector2 start = ActiveBombs[i].Center;
                Vector2 end = ActiveBombs[i + 1].Center;

                if (Vector2.Distance(start, end) > 400f) continue;

                float step = 20f;
                Vector2 dir = Vector2.Normalize(end - start) * step;
                Vector2 pos = start;

                for (float k = 0; k < Vector2.Distance(start, end); k += step)
                {
                    Dust.NewDustPerfect(pos, Terraria.ID.DustID.Smoke, Vector2.Zero).noGravity = true;
                    pos += dir;
                }

                // 🔥 Aquí también puedes aplicar lógica de daño si deseas
            }
        }
    }
}
