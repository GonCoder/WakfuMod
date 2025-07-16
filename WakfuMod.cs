// WakfuMod.cs - Base del Mod
using Terraria.ModLoader;
using WakfuMod.ModSystems; // Para FootballSystem y FootballTeam
using System.IO; // Para BinaryReader
using Terraria; // Para Main
using Terraria.ID; // Para NetmodeID
using WakfuMod.Content.Backgrounds;
using Microsoft.Xna.Framework;
using WakfuMod.Content.Items.BossSpawners;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders; // Necesario para acceder a MyForestBackgroundStyle


namespace WakfuMod
{
    public class WakfuMod : Mod
    {
        public static ModKeybind Habilidad1Keybind { get; private set; }
        public static ModKeybind Habilidad2Keybind { get; private set; }

        public override void Load()
        {
            Habilidad1Keybind = KeybindLoader.RegisterKeybind(this, "Skill 1 Wakfu", "V");
            Habilidad2Keybind = KeybindLoader.RegisterKeybind(this, "Skill 2 Wakfu", "X");

             if (!Main.dedServ)
            {
                // Usar un nombre único para nuestro filtro para evitar conflictos
                string filterName = "WakfuMod:NoxShockwave";

                // Registrar nuestro filtro de shader en el juego
                Filters.Scene[filterName] = new Filter(new ScreenShaderData("FilterMiniTower"), EffectPriority.VeryHigh);
                Filters.Scene[filterName].Load(); // Cargar explícitamente
            }

        }


        public override void Unload()
        {
            Habilidad1Keybind = null;
            Habilidad2Keybind = null;
        }

        // --- TU ENUM MessageType ---
        public enum MessageType : byte
        {
            PlayerTeamChange,
            ScoreUpdate,
            ZurcarakDieEffect,
            SpawnNoxBoss,
            TimeSlow
            // Añade aquí otros tipos de mensajes de red que necesites para otras cosas
        }

        // --- TU MÉTODO HandlePacket ---
        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            MessageType msgType = (MessageType)reader.ReadByte();

            switch (msgType)
            {
                // --- CASO REFACTORIZADO ---
                case MessageType.PlayerTeamChange:
                    // Simplemente llama al manejador específico en FootballSystem
                    FootballSystem.HandlePlayerTeamChangePacket(reader, whoAmI);
                    break;

                // --- CASO REFACTORIZADO ---
                case MessageType.ScoreUpdate:
                    // Simplemente llama al manejador específico en FootballSystem
                    // (Este necesitará la corrección del punto 4)
                    FootballSystem.HandleScoreUpdatePacket(reader, whoAmI);
                    break;

                case MessageType.ZurcarakDieEffect:
                    if (Main.netMode == NetmodeID.Server)
                    {
                        // El servidor recibe, aplica su lógica (daño/loot) y retransmite
                        byte playerID = reader.ReadByte();
                        Vector2 position = reader.ReadVector2();
                        byte dieResult = reader.ReadByte();

                        // Aplicar efectos en el servidor (principalmente daño/loot)
                        ZurcarakEffectSystem.ApplyEffects(Main.player[playerID], position, dieResult);

                        // Retransmitir a otros clientes
                        ModPacket packet = GetPacket();
                        packet.Write((byte)MessageType.ZurcarakDieEffect);
                        packet.Write(playerID);
                        packet.WriteVector2(position);
                        packet.Write(dieResult);
                        packet.Send(-1, whoAmI); // Enviar a todos menos al que lo originó
                    }
                    else // Si es un cliente recibiendo del servidor
                    {
                        // Leer los datos que el servidor nos retransmitió
                        byte playerID = reader.ReadByte();
                        Vector2 position = reader.ReadVector2();
                        byte dieResult = reader.ReadByte();
                        // Aplicar los efectos localmente (buffs, curación, efectos visuales)
                        ZurcarakEffectSystem.ApplyEffects(Main.player[playerID], position, dieResult);
                    }
                    break;

                case MessageType.SpawnNoxBoss:
                    // Este paquete solo debería ser recibido por el servidor desde un cliente.
                    if (Main.netMode == NetmodeID.Server)
                    {
                        // 'whoAmI' es el índice del jugador que envió el paquete.
                        Player player = Main.player[whoAmI];
                        // Llamar al método de invocación en el servidor,
                        // que se encargará de crear el NPC y sincronizarlo.
                        NoxSpawner.SpawnNox(player);
                    }
                    break;

                // --- NUEVO CASE PARA LA RALENTIZACIÓN ---
                case MessageType.TimeSlow:
                    // Este paquete solo lo reciben los clientes desde el servidor.
                    // El servidor NUNCA debería recibir este paquete.
                    if (Main.netMode == NetmodeID.MultiplayerClient)
                    {
                        int duration = reader.ReadInt32(); // Leer la duración
                        // Llamar a un método en el sistema para activar el efecto localmente
                        ModContent.GetInstance<ModSystems.TimeSlowSystem>().ReceiveActivationPacket(duration);
                    }
                    break;

                // Otros cases para otros tipos de mensajes...
                default:
                    Logger.WarnFormat("WakfuMod: Unknown Message type: {0}", msgType);
                    break;
            }
        }

    }
}


// Please read https://github.com/tModLoader/tModLoader/wiki/Basic-tModLoader-Modding-Guide#mod-skeleton-contents for more information about the various files in a mod.