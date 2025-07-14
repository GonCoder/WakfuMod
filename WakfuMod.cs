// WakfuMod.cs - Base del Mod
using Terraria.ModLoader;
using WakfuMod.ModSystems; // Para FootballSystem y FootballTeam
using System.IO; // Para BinaryReader
using Terraria; // Para Main
using Terraria.ID; // Para NetmodeID
using WakfuMod.Content.Backgrounds;
using Microsoft.Xna.Framework; // Necesario para acceder a MyForestBackgroundStyle


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
            ZurcarakDieEffect
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

                // Otros cases para otros tipos de mensajes...
                default:
                    Logger.WarnFormat("WakfuMod: Unknown Message type: {0}", msgType);
                    break;
            }
        }

    }
}


// Please read https://github.com/tModLoader/tModLoader/wiki/Basic-tModLoader-Modding-Guide#mod-skeleton-contents for more information about the various files in a mod.