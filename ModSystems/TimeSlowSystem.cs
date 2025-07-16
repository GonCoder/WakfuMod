using Terraria;
using Terraria.ModLoader;
using WakfuMod.jugador; // Para TimeSlowPlayer
using Terraria.ID;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria.Graphics.Effects;
using WakfuMod.Content.NPCs.Bosses.Nox; // Para Nox y Noxine
using Terraria.Graphics.Shaders;
using WakfuMod.Common; // Para acceder a las clases Global

namespace WakfuMod.ModSystems
{
    public class TimeSlowSystem : ModSystem
    {
        public static bool IsTimeSlowed { get; private set; } = false;
        private static int TimeSlowDuration = 0;

        // --- Variables para la Onda ---
        private static ulong ShockwaveStartTick;
        private static int ShockwaveDuration;
        private static Vector2 ShockwaveCenter;
        private static bool isShockwaveActive = false;
        private const string ShockwaveFilterName = "WakfuMod:NoxShockwave";

        // --- MÉTODO ACTIVATE (SIN CAMBIOS SIGNIFICATIVOS) ---
        public void Activate(int duration)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            IsTimeSlowed = true;
            TimeSlowDuration = duration;
            Main.NewText($"[System] Activate() ejecutado. IsTimeSlowed = {IsTimeSlowed}. Duración: {TimeSlowDuration}", Color.LawnGreen);

            if (Main.netMode != NetmodeID.Server)
            {
                int noxIndex = NPC.FindFirstNPC(ModContent.NPCType<Nox>());
                if (noxIndex != -1)
                {
                    ShockwaveStartTick = Main.GameUpdateCount;
                    ShockwaveDuration = 120;
                    ShockwaveCenter = Main.npc[noxIndex].Center;
                    isShockwaveActive = true;

                    if (!Filters.Scene[ShockwaveFilterName].IsActive()) {
                        Filters.Scene.Activate(ShockwaveFilterName, ShockwaveCenter);
                    }
                }
            }

            if (Main.netMode == NetmodeID.Server)
            {
                ModPacket packet = Mod.GetPacket();
                packet.Write((byte)WakfuMod.MessageType.TimeSlow);
                packet.Write(duration);
                packet.Send();
            }
        }

        // --- MÉTODO PARA MANEJAR PAQUETE ENTRANTE (SIN CAMBIOS) ---
        public void ReceiveActivationPacket(int duration)
        {
            IsTimeSlowed = true;
            TimeSlowDuration = duration;
            // Podríamos añadir la lógica de activar el shockwave aquí también si un cliente recibe el paquete
        }

        // --- ELIMINAR PreUpdatePlayers ---
        // Lo vamos a reemplazar con PreUpdateEverything para manejar NPCs y Proyectiles
        // public override void PreUpdatePlayers() { ... }

        // --- NUEVO HOOK: PreUpdateEverything ---
        // Se ejecuta antes de la mayoría de las actualizaciones, ideal para configurar estados globales
        public  void PreUpdateEverything()
        {
            // --- Lógica del Temporizador (se ejecuta en todas las máquinas) ---
            if (TimeSlowDuration > 0)
            {
                TimeSlowDuration--;
                if (TimeSlowDuration <= 0 && IsTimeSlowed)
                {
                    IsTimeSlowed = false;
                    Main.NewText("[System] Ralentización del tiempo terminada.", Color.OrangeRed);

                    if (Main.netMode == NetmodeID.Server)
                    {
                        ModPacket packet = Mod.GetPacket();
                        packet.Write((byte)WakfuMod.MessageType.TimeSlow);
                        packet.Write(0); // Duración 0 para desactivar
                        packet.Send();
                    }
                }
            }

            // --- APLICAR EL EFECTO A TODO ---
            if (IsTimeSlowed)
            {
                // Aplicar al jugador local
                if (Main.myPlayer != -1) {
                    Player player = Main.LocalPlayer;
                    if (player.active && player.TryGetModPlayer<TimeSlowPlayer>(out var timeSlowPlayer)) {
                        timeSlowPlayer.isTimeSlowed = true;
                    }
                }

                // Aplicar a todos los NPCs (excepto Nox y sus noxinas)
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];
                    if (npc.active && npc.type != ModContent.NPCType<Nox>() && npc.type != ModContent.NPCType<Noxine>())
                    {
                        if (npc.TryGetGlobalNPC<TimeSlowGlobalNPC>(out var globalNpc)) {
                            globalNpc.isSlowed = true;
                        }
                    }
                }

                // Aplicar a todos los proyectiles
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile projectile = Main.projectile[i];
                    if (projectile.active)
                    {
                         if (projectile.TryGetGlobalProjectile<TimeSlowGlobalProjectile>(out var globalProj)) {
                            globalProj.isSlowed = true;
                         }
                    }
                }
            }
        }


        // --- UpdateUI para Shaders (sin cambios) ---
        public override void UpdateUI(GameTime gameTime)
        {
            if (!isShockwaveActive) return;

            ulong elapsedTicks = Main.GameUpdateCount - ShockwaveStartTick;

            if (elapsedTicks < (ulong)ShockwaveDuration)
            {
                float progress = (float)elapsedTicks / ShockwaveDuration;
                ScreenShaderData shader = Filters.Scene[ShockwaveFilterName].GetShader();
                shader.UseTargetPosition(ShockwaveCenter);
                shader.UseProgress(progress);
                shader.UseColor(0.3f, 0.8f, 1.0f);
            }
            else
            {
                if (Filters.Scene[ShockwaveFilterName].IsActive()) {
                     Filters.Scene[ShockwaveFilterName].Deactivate();
                }
                isShockwaveActive = false;
            }
        }
    }
}