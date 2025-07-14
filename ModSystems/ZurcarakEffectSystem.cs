// En ModSystems/ZurcarakEffectSystem.cs
using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.ID;
using System.IO;
using System;
using WakfuMod;
using Terraria.Audio;
using static Humanizer.In;
using WakfuMod.Content.Projectiles; // Para acceder a WakfuMod y su enum de mensajes de red

namespace WakfuMod.ModSystems
{
    public class ZurcarakEffectSystem : ModSystem
    {
        // El jugador local llama a este método
        public void ActivateDieEffect(Player player, Vector2 position, int dieResult)
        {
            // Solo el dueño debe iniciar la lógica
            if (player.whoAmI != Main.myPlayer) return;

            // --- LÓGICA DE RED / SINGLEPLAYER ---
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                // --- MODO SINGLEPLAYER ---
                // Si estamos en un solo jugador, no enviamos paquetes.
                // Llamamos directamente a la lógica de efectos.
                ApplyEffects(player, position, dieResult);
            }
            else // Si estamos en multijugador (cliente)
            {
                // --- MODO MULTIJUGADOR (CLIENTE) ---
                // Crear un paquete de red para sincronizar el efecto con el servidor.
                ModPacket packet = Mod.GetPacket();
                packet.Write((byte)WakfuMod.MessageType.ZurcarakDieEffect);
                packet.Write((byte)player.whoAmI);
                packet.WriteVector2(position);
                packet.Write((byte)dieResult);
                packet.Send(); // Enviar al servidor
            }
        }

        // Este método será llamado desde HandlePacket en tu clase principal WakfuMod.cs
        public static void ApplyEffects(Player effectedPlayer, Vector2 position, int dieResult)
        {
            // Sonidos y efectos visuales que todos deben ver/oír
            SoundEngine.PlaySound(SoundID.Tink, position);

            // Pequeña explosión de polvo/brillo dorado en la posición del dado
            for (int i = 0; i < 25; i++)
            {
                // Usar un polvo que coincida con la temática del Zurcarák (dorado, suerte)
                int dustType = Main.rand.NextBool(3) ? DustID.GoldCoin : DustID.Confetti_Yellow;
                Dust dust = Dust.NewDustPerfect(position, dustType, Main.rand.NextVector2Circular(4f, 4f), 100, default, 1.4f);
                dust.noGravity = true;
                dust.fadeIn = 0.5f;
            }

            // Mostrar un texto de combate con el número que salió
            // Solo el jugador local necesita ver este texto flotante
            if (effectedPlayer.whoAmI == Main.myPlayer)
            {
                // Color basado en el resultado (malo, bueno, jackpot)
                Color textColor = dieResult <= 3 ? Color.White : Color.Gold;
                if (dieResult == 6) textColor = Color.Cyan; // Jackpot especial
                CombatText.NewText(effectedPlayer.getRect(), textColor, dieResult.ToString(), true); // 'true' para que sea texto "crítico" (grande)
            }

            // --- Lógica de Efectos ---
            // ¡Esta lógica ahora se ejecuta en TODAS las máquinas (servidor y clientes)!
            switch (dieResult)
            {
                // --- EFECTOS MENORES ---
                case 1: // Curación Pequeña + Defensa
                    ApplyHealAndMana(effectedPlayer, 0.30f, 0.50f);
                    ApplyDefenseBuff(effectedPlayer, 30 * 60); // 30 segundos
                    break;
                case 2: // Buffs Ofensivos Pequeños
                    ApplyOffensiveBuffs(effectedPlayer, 30 * 60); // 30 segundos
                    break;
                case 3: // Explosión Pequeña
                    // Ejecutar lógica de daño en Single Player Y en Servidor
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        CreateExplosion(effectedPlayer, position, false);
                    }
                    // Todos (incluidos clientes) generan los efectos visuales
                    SpawnExplosionVisuals(position, false);
                    break;

                // --- EFECTOS POTENCIADOS ---
                case 4: // Curación Grande + Defensa Larga
                    ApplyHealAndMana(effectedPlayer, 1.0f, 1.0f); // 100%
                    ApplyDefenseBuff(effectedPlayer, 10 * 60 * 60); // 10 minutos
                    break;
                case 5: // Buffs Ofensivos Largos + Buffs de Utilidad
                    ApplyOffensiveBuffs(effectedPlayer, 10 * 60 * 60); // 10 minutos
                    ApplyUtilityBuffs(effectedPlayer, 10 * 60 * 60);
                    break;
                case 6: // Explosión Masiva + Oro
                    // Ejecutar lógica de daño y loot en Single Player Y en Servidor
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        CreateExplosion(effectedPlayer, position, true);

                        // Generar 10 de Oro
                        int itemIndex = Item.NewItem(
                            effectedPlayer.GetSource_FromThis("ZurcarakDieGold"),
                            position,
                            ItemID.GoldCoin,
                            10
                        );

                        // Si estamos en un servidor (no en singleplayer), sincronizar el item
                        if (Main.netMode == NetmodeID.Server)
                        {
                            NetMessage.SendData(MessageID.SyncItem, -1, -1, null, itemIndex, 1f);
                        }
                    }
                    // Todos (incluidos clientes) generan los efectos visuales
                    SpawnExplosionVisuals(position, true);
                    break;
            }
        }

        // --- Métodos Helper para Claridad ---

        private static void ApplyHealAndMana(Player player, float lifePercent, float manaPercent)
        {
            int lifeToHeal = (int)(player.statLifeMax2 * lifePercent);
            int manaToRestore = (int)(player.statManaMax2 * manaPercent);

            player.Heal(lifeToHeal);
            player.statMana += manaToRestore;
            if (player.statMana > player.statManaMax2) player.statMana = player.statManaMax2;
            // CombatText para el jugador local
            if (player.whoAmI == Main.myPlayer)
            {
                CombatText.NewText(player.getRect(), Color.LawnGreen, $"+{lifeToHeal} HP");
                CombatText.NewText(player.getRect(), Color.LightBlue, $"+{manaToRestore} Mana");
            }
        }

        private static void ApplyDefenseBuff(Player player, int duration)
        {
            player.AddBuff(BuffID.Ironskin, duration);
            player.AddBuff(BuffID.Endurance, duration); // <-- AÑADIDO: Buff de Endurance

        }

        private static void ApplyOffensiveBuffs(Player player, int duration)
        {
            player.AddBuff(BuffID.Wrath, duration); // Daño
            player.AddBuff(BuffID.Rage, duration); // Crítico
            player.AddBuff(BuffID.Archery, duration); // Velocidad flechas
            player.AddBuff(BuffID.MagicPower, duration); // Daño mágico
            player.AddBuff(BuffID.Summoning, duration); // Daño minions
            player.AddBuff(BuffID.Sharpened, duration); // Pen armadura melee
            player.AddBuff(BuffID.Swiftness, duration); // Velocidad movimiento
            player.AddBuff(BuffID.Tipsy, duration); // Bonus melee (¡con penalización!)
        }

        private static void ApplyUtilityBuffs(Player player, int duration)
        {
            player.AddBuff(BuffID.AmmoReservation, duration);
            player.AddBuff(BuffID.Regeneration, duration);
            player.AddBuff(BuffID.ManaRegeneration, duration);
        }

        private static void CreateExplosion(Player owner, Vector2 position, bool isLarge)
        {
            // --- 1. Definir los parámetros de la explosión ---
            int size = isLarge ? 1800 : 600; // Tamaño de la hitbox (ancho y alto)
            float minDamagePercent = isLarge ? 0.30f : 0.10f;
            float maxDamagePercent = isLarge ? 0.50f : 0.30f;

            // --- 2. Crear el proyectil ---
            // Usamos Projectile.NewProjectile, que devuelve el índice (el "whoAmI") del proyectil creado.
            int projIndex = Projectile.NewProjectile(
                owner.GetSource_FromThis("DieExplosion"),
                position, // Spawnea en el centro de la explosión
                Vector2.Zero, // Sin velocidad
                ModContent.ProjectileType<DieExplosion>(),
                1,  // Daño base 1 (se sobrescribe en ModifyHitNPC)
                0f, // Knockback 0 (se sobrescribe en ModifyHitNPC)
                owner.whoAmI,
                // Pasar los porcentajes de daño a través de ai[0] y ai[1]
                ai0: minDamagePercent * 10000f,
                ai1: maxDamagePercent * 10000f
            );

            // --- 3. Modificar el proyectil recién creado ---
            // Comprobamos si la creación fue exitosa (el índice no es Main.maxProjectiles)
            if (projIndex != Main.maxProjectiles)
            {
                // Obtenemos la instancia del proyectil que acabamos de crear
                Projectile explosionProjectile = Main.projectile[projIndex];

                // --- ESTA ES LA PARTE CLAVE ---
                // Cambiamos el tamaño de su hitbox
                explosionProjectile.width = size;
                explosionProjectile.height = size;
                // Centramos la hitbox en la posición de spawn
                explosionProjectile.Center = position;

                // Marcamos para sincronizar estos cambios en multijugador
                explosionProjectile.netUpdate = true;

                // Opcional: Log para confirmar
                // Main.NewText($"Spawned DieExplosion (projIndex {projIndex}) with size {size}x{size}");
            }
            else
            {
                // Log de error si el proyectil no se pudo crear
                ModContent.GetInstance<WakfuMod>().Logger.Warn("Failed to spawn DieExplosion projectile.");
            }
        }

        private static void SpawnExplosionVisuals(Vector2 position, bool isLarge)
        {
            // Efectos visuales de explosión (polvo, gore, sonido)
            SoundEngine.PlaySound(isLarge ? SoundID.Item62 : SoundID.Item14, position);
            int dustCount = isLarge ? 500 : 100;
            float dustSpeed = isLarge ? 24f : 12f;
            for (int i = 0; i < dustCount; i++)
            {
                Dust.NewDustPerfect(position, DustID.GoldCoin, Main.rand.NextVector2Circular(dustSpeed, dustSpeed), 0, default, 1.5f).noGravity = true;
            }
        }
    }
}