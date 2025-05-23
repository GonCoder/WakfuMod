// ModSystems/ZurcarakEffectSystem.cs
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.DataStructures; // Para IEntitySource
using WakfuMod.Content.Projectiles; // Para la pulga
using System; // Para Math
using Terraria.Audio; // Para SoundEngine

namespace WakfuMod.ModSystems // Reemplaza con tu namespace
{
    public class ZurcarakEffectSystem : ModSystem
    {
        // Método llamado por el proyectil del dado (ZurcarakDie)
        public void ActivateDieEffect(Player player, Vector2 position, int dieResult)
        {
            // Sonido general del efecto activándose (distinto al del resultado?)
            // SoundEngine.PlaySound(SoundID.Item4, position);

            // Efecto visual general en la posición del dado?
            // for (int i = 0; i < 15; i++) Dust.NewDustPerfect(position, DustID.GoldCoin, Main.rand.NextVector2Circular(3f, 3f));

            // Lógica para cada resultado
            switch (dieResult)
            {
                case 1: // --- CURACIÓN TOTAL ---
                    player.statLife = player.statLifeMax2; // Rellena vida al máximo
                    player.statMana = player.statManaMax2; // Rellena maná al máximo
                    player.HealEffect(player.statLifeMax2, true); // Efecto visual de curación de vida
                    player.ManaEffect(player.statManaMax2);      // Efecto visual de curación de maná
                    SoundEngine.PlaySound(SoundID.Item29 with { Pitch = -0.2f }, player.position); // Sonido de curación potente
                    CombatText.NewText(player.getRect(), Color.LimeGreen, "Full Heal!", true); // true para crítico/importante
                    break;

                case 2: // --- BUFFS OFENSIVOS ---
                    int buffDurationOffensive = 600; // 10 segundos
                    player.AddBuff(BuffID.RapidHealing, buffDurationOffensive); // Regeneración vida
                    player.AddBuff(BuffID.MagicPower, buffDurationOffensive); // +20% Daño Mágico
                    player.AddBuff(BuffID.ManaRegeneration, buffDurationOffensive); // Regeneración Maná
                    player.AddBuff(BuffID.Summoning, buffDurationOffensive); // +1 Minion
                    // Añade aquí los buffs específicos que quieras:
                    // player.AddBuff(BuffID.Wrath, buffDurationOffensive); // +10% Daño General
                    // player.AddBuff(BuffID.Rage, buffDurationOffensive); // +10% Prob. Crítico General
                    // player.AddBuff(BuffID.Swiftness, buffDurationOffensive); // +25% Velocidad Movimiento
                    // player.AddBuff(BuffID.Mining, buffDurationOffensive); // +25% Velocidad Minería (ejemplo)
                    // player.AddBuff(ModContent.BuffType<TuBuffDeVelocidadAtaque>(), buffDurationOffensive); // Si tienes uno propio

                    SoundEngine.PlaySound(SoundID.Item44, player.position); // Sonido de buff
                    CombatText.NewText(player.getRect(), Color.OrangeRed, "Offensive Boost!", true);
                    break;

                case 3: // --- EXPLOSIÓN MASIVA ---
                    float explosionRadius = 250f; // Radio grande
                    int explosionDamageBase = 1; // Daño base mínimo
                    float explosionKnockback = 8f;

                    // Sonido y Efectos Visuales de la Explosión
                    SoundEngine.PlaySound(SoundID.Item62, position); // Sonido potente (Grenade Launcher)
                    if (!Main.dedServ) {
                        for (int i = 0; i < 60; i++) { // Mucho polvo dorado/explosivo
                             Dust d = Dust.NewDustPerfect(position, DustID.GoldFlame, Main.rand.NextVector2Circular(12f, 12f), 0, default, 2.0f);
                             d.noGravity = true;
                             d.velocity *= 0.8f;
                        }
                        for (int g = 0; g < 5; g++) {
                             Gore.NewGore(Projectile.GetSource_None(), position, Main.rand.NextVector2Circular(3f, 3f), GoreID.Smoke1, 1.5f);
                        }
                    }

                    // Aplicar daño a enemigos cercanos
                    if (Main.myPlayer == player.whoAmI || Main.netMode == NetmodeID.Server) {
                        for (int i = 0; i < Main.maxNPCs; i++) {
                             NPC npc = Main.npc[i];
                             if (npc.active && !npc.friendly && npc.CanBeChasedBy(null, false) && npc.DistanceSQ(position) <= explosionRadius * explosionRadius) {
                                 // Calcular 30% vida máxima
                                 int percentDamage = (int)(npc.lifeMax * 0.30f);
                                 int totalDamage = explosionDamageBase + percentDamage; // Daño base + % vida

                                 // Aplicar golpe (sin necesidad de modificadores aquí, ya que el daño principal es el %)
                                 // WakfuPlayer aplicará el roll aleatorio si es necesario en sus hooks
                                 player.ApplyDamageToNPC(npc, totalDamage, explosionKnockback, Math.Sign(npc.Center.X - position.X), false, DamageClass.Generic); // Usa Generic o el que prefieras
                             }
                        }
                    }
                    CombatText.NewText(player.getRect(), Color.Red, "BIG BOOM!", true);
                    break;

                case 4: // --- BUFFS DEFENSIVOS ---
                    int buffDurationDefensive = 720; // 12 segundos
                    player.AddBuff(BuffID.RapidHealing, buffDurationDefensive); // Regeneración vida
                    player.AddBuff(BuffID.ManaRegeneration, buffDurationDefensive); // Regeneración Maná
                    player.AddBuff(BuffID.Ironskin, buffDurationDefensive); // +8 Defensa
                    player.AddBuff(BuffID.Endurance, buffDurationDefensive); // 10% Reducción Daño
                    player.AddBuff(BuffID.Swiftness, buffDurationDefensive); // Velocidad Movimiento

                    SoundEngine.PlaySound(SoundID.Item46, player.position); // Sonido de escudo/buff
                    CombatText.NewText(player.getRect(), Color.SkyBlue, "Defensive Stance!", true);
                    break;

                case 5: // --- INVOCAR PULGAS ---
                    int fleaCount = 6;
                    int fleaDamage = 1; // Daño base 1, el real es % vida
                    float fleaKnockback = 0f;

                    SoundEngine.PlaySound(SoundID.Item97, position); // Sonido de invocación de abejas/avispas

                    if (Main.myPlayer == player.whoAmI) // Solo el dueño debe invocar minions
                    {
                        for (int i = 0; i < fleaCount; i++)
                        {
                             // Spawnea las pulgas cerca de la posición del dado con velocidad inicial aleatoria
                             Vector2 spawnPos = position + Main.rand.NextVector2Circular(40f, 40f);
                             Vector2 initialVel = Main.rand.NextVector2Circular(3f, 3f);

                             Projectile.NewProjectile(
                                 player.GetSource_FromThis("DieEffect_Fleas"),
                                 spawnPos,
                                 initialVel,
                                 ModContent.ProjectileType<ZurcarakFlea>(), // <<<--- NUEVO PROYECTIL
                                 fleaDamage,
                                 fleaKnockback,
                                 player.whoAmI
                                 // Puedes usar ai[] para pasarles info si es necesario
                             );
                        }
                    }
                    CombatText.NewText(player.getRect(), Color.SandyBrown, "Fleas Attack!", true);
                    break;

                case 6: // --- DROP DE ORO ---
                    int amount = 10; // 10 monedas de oro
                    if (Main.myPlayer == player.whoAmI) // Solo el jugador local genera los items
                    {
                         int itemType = ItemID.GoldCoin;
                         int itemStack = amount;
                         player.QuickSpawnItem(player.GetSource_FromThis("DieEffect_Gold"), itemType, itemStack);
                    }
                    SoundEngine.PlaySound(SoundID.CoinPickup, position); // Sonido de monedas
                    CombatText.NewText(player.getRect(), Color.Gold, "Gold Rush!", true);
                    break;
            }
         }
    }
}