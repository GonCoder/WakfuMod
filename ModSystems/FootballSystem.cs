// ModSystems/FootballSystem.cs
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria.ID;
using WakfuMod.Content.Tiles;
using Terraria.Localization; // Para MessageID

namespace WakfuMod.ModSystems // Reemplaza con tu namespace
{
    public enum FootballTeam { None, Red, Blue }

    public class FootballSystem : ModSystem
    {
        public static int ScoreRed { get; set; } = 0;
        public static int ScoreBlue { get; set; } = 0;
        public static Mod WakfuModInstance => ModContent.GetInstance<WakfuMod>(); // <-- OBTENER INSTANCIA


        // Diccionario para rastrear el equipo de cada jugador (ID jugador -> Equipo)
        // Se limpia al salir del servidor/mundo
        public static Dictionary<int, FootballTeam> PlayerTeams = new Dictionary<int, FootballTeam>();

        public override void ClearWorld()
        {
            ScoreRed = 0;
            ScoreBlue = 0;
            PlayerTeams.Clear();
        }

        // Obtener equipo de un jugador
        public static FootballTeam GetPlayerTeam(int playerWhoAmI)
        {
            if (PlayerTeams.TryGetValue(playerWhoAmI, out FootballTeam team))
            {
                return team;
            }
            return FootballTeam.None; // Por defecto, ningún equipo
        }

        // Correcto: Establece el equipo y sincroniza
        public static void SetPlayerTeam(int playerWhoAmI, FootballTeam newTeam)
        {
            // Actualiza el diccionario localmente primero
            if (!PlayerTeams.ContainsKey(playerWhoAmI)) { PlayerTeams.Add(playerWhoAmI, newTeam); }
            else { PlayerTeams[playerWhoAmI] = newTeam; }

            // Sincronización por red
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                // Cliente envía su cambio al servidor
                ModPacket packet = WakfuModInstance.GetPacket();
                packet.Write((byte)WakfuMod.MessageType.PlayerTeamChange); // Asume que MessageType está en WakfuMod.cs
                packet.Write((byte)playerWhoAmI); // Quién cambió
                packet.Write((byte)newTeam);      // A qué equipo
                packet.Send(); // Enviar al servidor
            }
            else if (Main.netMode == NetmodeID.Server)
            {
                // Servidor recibió (o cambió directamente) y retransmite a todos los demás
                ModPacket packet = WakfuModInstance.GetPacket();
                packet.Write((byte)WakfuMod.MessageType.PlayerTeamChange);
                packet.Write((byte)playerWhoAmI);
                packet.Write((byte)newTeam);
                // Envía a todos EXCEPTO al que originó el cambio (si fue un cliente)
                // O a todos si el servidor inició el cambio (ej. comando)
                packet.Send(-1, playerWhoAmI);

                // El servidor también necesita mostrar el mensaje en su consola/log
                Player player = Main.player[playerWhoAmI];
                string teamName = newTeam.ToString();
                Color teamColor = (newTeam == FootballTeam.Red) ? Color.Red : (newTeam == FootballTeam.Blue) ? Color.SkyBlue : Color.Gray;
                System.Console.WriteLine($"{player.name} joined Team {teamName}!"); // Log del servidor

                // Y envía un mensaje de chat a los clientes
                string chatMessage = $"{player.name} joined Team {teamName}!";
                // NetMessage.BroadcastChatMessage(NetworkText.FromLiteral(chatMessage), teamColor, -1); // <-- INCORRECTO
                NetMessage.SendData(MessageID.ChatText, -1, -1, NetworkText.FromLiteral(chatMessage), 255, teamColor.R, teamColor.G, teamColor.B); // <-- CORRECTO
            }
            else // Single Player
            {
                // Mensaje en chat para Single Player
                Player player = Main.player[playerWhoAmI];
                string teamName = newTeam.ToString();
                Color teamColor = (newTeam == FootballTeam.Red) ? Color.Red : (newTeam == FootballTeam.Blue) ? Color.SkyBlue : Color.Gray;
                Main.NewText($"{player.name} joined Team {teamName}!", teamColor);
            }
        }

        public static void AddScore(FootballTeam scoringTeam)
        {
            // Actualiza localmente primero
            if (scoringTeam == FootballTeam.Red) { ScoreRed++; }
            else if (scoringTeam == FootballTeam.Blue) { ScoreBlue++; }

            // --- SECCIÓN MODIFICADA ---
            if (Main.netMode != NetmodeID.SinglePlayer)
            {
                // El servidor (o quien detectó el gol) envía la actualización a todos
                ModPacket packet = WakfuModInstance.GetPacket();
                packet.Write((byte)WakfuMod.MessageType.ScoreUpdate);
                packet.Write((byte)scoringTeam); // <-- AÑADIDO: Enviar qué equipo marcó
                packet.Write(ScoreRed);
                packet.Write(ScoreBlue);
                packet.Send(); // Enviar a todos

                // El mensaje de chat AHORA lo envía SIEMPRE el servidor para consistencia
                if (Main.netMode == NetmodeID.Server)
                {
                    Color scoreColor = (scoringTeam == FootballTeam.Red) ? Color.Red : Color.SkyBlue;
                    string scoreText = $"GOAL! Team {scoringTeam} scores! Red {ScoreRed} - {ScoreBlue} Blue";
                    // Usa NetMessage.SendData para enviar el chat
                    NetMessage.SendData(MessageID.ChatText, -1, -1, NetworkText.FromLiteral(scoreText), 255, scoreColor.R, scoreColor.G, scoreColor.B);
                    // El servidor no reproduce sonidos
                }
                // Los clientes NO muestran mensaje ni sonido aquí, lo harán al RECIBIR el paquete
            }
            else // Single Player (Mantiene su lógica original)
            {
                Color scoreColor = (scoringTeam == FootballTeam.Red) ? Color.Red : Color.SkyBlue;
                string scoreText = $"GOAL! Team {scoringTeam} scores! Red {ScoreRed} - {ScoreBlue} Blue";
                Main.NewText(scoreText, scoreColor);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item42, Main.LocalPlayer.Center); // O SoundID.Duck como en Jalabola?
            }
            // --- FIN SECCIÓN MODIFICADA ---
        }

        // --- FootballTileCleanupSystem ---
        // Esta clase está bien aquí o podría ir en su propio archivo. Funcionalmente es correcto.
        public class FootballTileCleanupSystem : ModSystem
        {
            public override void OnWorldLoad()
            {
                bool tilesCleared = false;
                int redGoalType = ModContent.TileType<GoalTileRed>();
                int blueGoalType = ModContent.TileType<GoalTileBlue>();

                // Log es bueno para depurar
                Mod.Logger.Info("Football Tile Cleanup: Scanning world for goal tiles...");

                for (int x = 0; x < Main.maxTilesX; x++)
                {
                    for (int y = 0; y < Main.maxTilesY; y++)
                    {
                        Tile tile = Framing.GetTileSafely(x, y);
                        // Correcto: Chequea si existe y es del tipo adecuado
                        if (tile.HasTile && (tile.TileType == redGoalType || tile.TileType == blueGoalType))
                        {
                            // Correcto: Elimina el tile sin dropear item
                            WorldGen.KillTile(x, y, false, false, true);
                            tilesCleared = true;
                            // Correcto: No se necesita NetMessage aquí
                        }
                    }
                }

                if (tilesCleared)
                {
                    Mod.Logger.Info("Football Tile Cleanup: Finished removing existing goal tiles.");
                }
                else
                {
                    Mod.Logger.Info("Football Tile Cleanup: No goal tiles found to remove.");
                }
            }
        }
        // --- Fin FootballTileCleanupSystem ---

        // Guardar/Cargar puntuación con el mundo
        public override void SaveWorldData(TagCompound tag)
        {
            tag["FootballScoreRed"] = ScoreRed;
            tag["FootballScoreBlue"] = ScoreBlue;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            // ScoreRed = tag.GetInt("FootballScoreRed");
            // ScoreBlue = tag.GetInt("FootballScoreBlue");
            ScoreRed = 0;
            ScoreBlue = 0;
            PlayerTeams.Clear(); // Limpia equipos también, lo cual tiene sentido si reseteas scores.
        }

        public static void HandlePlayerTeamChangePacket(BinaryReader reader, int whoAmI)
        {
            // El servidor recibe esto de un cliente.
            // El cliente recibe esto del servidor (retransmitido).
            byte playerID = reader.ReadByte();
            FootballTeam newTeam = (FootballTeam)reader.ReadByte();

            // Actualizar el diccionario localmente en el receptor
            if (!PlayerTeams.ContainsKey(playerID)) { PlayerTeams.Add(playerID, newTeam); }
            else { PlayerTeams[playerID] = newTeam; }

            if (Main.netMode == NetmodeID.Server)
            {
                // El servidor recibió el cambio de un cliente.
                // Ahora debe retransmitirlo a TODOS los demás clientes.
                ModPacket packet = WakfuModInstance.GetPacket();
                packet.Write((byte)WakfuMod.MessageType.PlayerTeamChange);
                packet.Write(playerID);
                packet.Write((byte)newTeam);
                // Enviar a todos EXCEPTO al cliente original (whoAmI) y al servidor mismo (-1)
                packet.Send(-1, whoAmI);

                // El servidor también necesita mostrar el mensaje en consola y enviarlo por chat
                Player player = Main.player[playerID];
                string teamName = newTeam.ToString();
                Color teamColor = (newTeam == FootballTeam.Red) ? Color.Red : (newTeam == FootballTeam.Blue) ? Color.SkyBlue : Color.Gray;
                System.Console.WriteLine($"(Network) {player.name} joined Team {teamName}!"); // Log del servidor

                string chatMessage = $"{player.name} joined Team {teamName}!";
                // NetMessage.BroadcastChatMessage(NetworkText.FromLiteral(chatMessage), teamColor, -1); // <-- INCORRECTO
                NetMessage.SendData(MessageID.ChatText, -1, -1, NetworkText.FromLiteral(chatMessage), 255, teamColor.R, teamColor.G, teamColor.B); // <-- CORRECTO
            }
            else if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                // El cliente recibió la confirmación/actualización del servidor.
                // Mostrar el mensaje de chat (si no lo mostró ya localmente)
                // Es mejor que el servidor envíe siempre el mensaje de chat para consistencia.
                // Comprobamos si el mensaje es sobre nosotros mismos para evitar duplicados si el servidor usa BroadcastChatMessage
                if (playerID != Main.myPlayer)
                {
                    // Solo muestra el mensaje si no es sobre ti, asumiendo que el servidor usó BroadcastChatMessage que ya te lo mostró.
                    // O si quieres asegurarte, podrías tener una lógica más compleja o simplemente confiar en que el servidor envía el mensaje.
                    // Por simplicidad ahora, asumimos que el BroadcastChatMessage del servidor es suficiente.
                }
            }
        }

        public static void HandleScoreUpdatePacket(BinaryReader reader, int whoAmI)
        {
            // --- LÓGICA MODIFICADA ---

            // 1. Leer QUÉ equipo marcó (¡NUEVO!)
            FootballTeam scoringTeam = (FootballTeam)reader.ReadByte();

            // 2. Leer los nuevos scores
            int newScoreRed = reader.ReadInt32();
            int newScoreBlue = reader.ReadInt32();

            // 3. Actualizar los scores locales
            ScoreRed = newScoreRed;
            ScoreBlue = newScoreBlue;

            // 4. Si somos un CLIENTE, mostrar efectos de gol
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                // Construir el mensaje usando el equipo que marcó
                Color scoreColor = (scoringTeam == FootballTeam.Red) ? Color.Red : Color.SkyBlue;
                string scoreText = $"GOAL! Team {scoringTeam} scores! Red {ScoreRed} - {ScoreBlue} Blue";

                // Mostrar el mensaje localmente en el chat del cliente
                Main.NewText(scoreText, scoreColor);

                // Reproducir el sonido de gol localmente
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item42, Main.LocalPlayer.Center); // O SoundID.Duck
                                                                                               // Considera usar el mismo sonido que pusiste en Jalabola.CheckForGoal (SoundID.Duck) para consistencia.

                // Opcional: Efectos visuales como confeti si lo deseas (ya se hace en Jalabola, quizás no necesario aquí)
                /*
                for (int k = 0; k < 20; k++) {
                    int dustType = Main.rand.Next(new int[] { DustID.Confetti_Blue, DustID.Confetti_Green, DustID.Confetti_Pink, DustID.Confetti_Yellow });
                    Dust.NewDust(Main.LocalPlayer.position, Main.LocalPlayer.width, Main.LocalPlayer.height, dustType, Main.rand.NextFloat(-3, 3), Main.rand.NextFloat(-5, -1));
                }
                */
            }
            // El servidor no necesita hacer nada más aquí, ya envió el chat global en AddScore.

            // --- FIN LÓGICA MODIFICADA ---
        }
    }
}
