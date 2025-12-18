// WakfuMod.cs - Base del Mod
using Terraria.ModLoader;
using WakfuMod.ModSystems; // Para FootballSystem y FootballTeam
using System.IO; // Para BinaryReader
using System.Collections.Generic; // Para List<T>
using Terraria; // Para Main
using Terraria.ID; // Para NetmodeID
using WakfuMod.Content.Backgrounds;
using Microsoft.Xna.Framework;
using WakfuMod.Content.Items.BossSpawners;
using WakfuMod.jugador;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using WakfuMod.Content.Items.Currency;
using Terraria.GameContent.UI;
using Terraria.Localization; // Necesario para acceder a MyForestBackgroundStyle


namespace WakfuMod
{
    public class WakfuMod : Mod
    {

        // --- ID para nuestra moneda ---
        public static int KamaCurrencyId { get; private set; }

        public override void PostSetupContent()
        {
            // --- REGISTRAR MONEDA PERSONALIZADA (LA FORMA CORRECTA PARA TU VERSIÓN) ---
            KamaCurrencyId = CustomCurrencyManager.RegisterCurrency(
                new KamaCurrency( // Usamos nuestra nueva clase simple
                    ModContent.ItemType<Kama>(),
                    999L,
                    "Kama" // Esta es la clave de localización para el nombre
                )
            );
        }
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
            SyncPlayerWakfuData,
            ClosePortals,
            RequestPortalExplosion,
            SpawnYopukaSkill,
            PullPlayer,
            PortalExplosionFX,
            KickProjectile, // Nuevo mensaje para sincronizar patadas
            SpawnZurcarakMinion, // Nuevo mensaje para invocar al gatito
            NoxTeleport, // Nuevo mensaje para sincronizar teletransporte de Nox
            XelorTimeSuspension, // Nuevo mensaje para la habilidad X del Xelor
        }

        // --- TU MÉTODO HandlePacket ---
        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            MessageType msgType = (MessageType)reader.ReadByte();

            switch (msgType)
            {
                case MessageType.XelorTimeSuspension:
                    byte action = reader.ReadByte(); // 0 = Activate, 1 = Rewind, 2 = Clear
                    int senderPlayer = reader.ReadByte();

                    // Sincronizar estado en el jugador que envió el mensaje
                    if (senderPlayer >= 0 && senderPlayer < Main.maxPlayers)
                    {
                        Player p = Main.player[senderPlayer];
                        if (p.active)
                        {
                            WakfuPlayer wp = p.GetModPlayer<WakfuPlayer>();
                            if (action == 0)
                            {
                                wp.xelorTimeSuspensionActive = true;
                                wp.xelorTimeSuspensionTimer = WakfuPlayer.XelorTimeSuspensionDuration;
                            }
                            else
                            {
                                wp.xelorTimeSuspensionActive = false;
                            }
                        }
                    }

                    if (action == 0) // Activate
                    {
                        // Listas temporales para retransmisión
                        List<int> npcIndices = new List<int>();
                        List<Vector2> npcPositions = new List<Vector2>();
                        List<int> projIndices = new List<int>();
                        List<Vector2> projPositions = new List<Vector2>();

                        int npcCount = reader.ReadInt32();
                        for (int i = 0; i < npcCount; i++)
                        {
                            int npcIndex = reader.ReadInt32();
                            Vector2 pos = reader.ReadVector2();
                            
                            npcIndices.Add(npcIndex);
                            npcPositions.Add(pos);

                            if (npcIndex >= 0 && npcIndex < Main.maxNPCs && Main.npc[npcIndex].active)
                            {
                                var global = Main.npc[npcIndex].GetGlobalNPC<Common.GlobalNPCs.WakfuGlobalNPC>();
                                global.xelorSlowed = true;
                                global.xelorRewindPos = pos;
                                global.xelorOriginalVelocity = Main.npc[npcIndex].velocity;
                                // Main.npc[npcIndex].velocity *= 0.2f; // No aplicar ralentización única a NPCs
                            }
                        }

                        int projCount = reader.ReadInt32();
                        for (int i = 0; i < projCount; i++)
                        {
                            int projIndex = reader.ReadInt32();
                            Vector2 pos = reader.ReadVector2();

                            projIndices.Add(projIndex);
                            projPositions.Add(pos);

                            if (projIndex >= 0 && projIndex < Main.maxProjectiles && Main.projectile[projIndex].active)
                            {
                                var global = Main.projectile[projIndex].GetGlobalProjectile<Content.Globals.WakfuGlobalProjectile>();
                                global.xelorSlowed = true;
                                global.xelorRewindPos = pos;
                                global.xelorOriginalVelocity = Main.projectile[projIndex].velocity;
                                Main.projectile[projIndex].velocity *= 0.2f; // Aplicar ralentización UNA VEZ
                            }
                        }

                        // Si es el servidor, retransmitir a otros clientes
                        if (Main.netMode == NetmodeID.Server)
                        {
                            ModPacket packet = GetPacket();
                            packet.Write((byte)MessageType.XelorTimeSuspension);
                            packet.Write(action);
                            packet.Write((byte)senderPlayer);
                            
                            packet.Write(npcCount);
                            for(int i=0; i<npcCount; i++) { packet.Write(npcIndices[i]); packet.WriteVector2(npcPositions[i]); }
                            
                            packet.Write(projCount);
                            for(int i=0; i<projCount; i++) { packet.Write(projIndices[i]); packet.WriteVector2(projPositions[i]); }

                            packet.Send(-1, whoAmI);
                        }
                    }
                    else if (action == 1) // Rewind
                    {
                        // Iterar todo y rebobinar lo que esté marcado
                        for (int i = 0; i < Main.maxNPCs; i++)
                        {
                            if (Main.npc[i].active)
                            {
                                var global = Main.npc[i].GetGlobalNPC<Common.GlobalNPCs.WakfuGlobalNPC>();
                                if (global.xelorSlowed)
                                {
                                    Main.npc[i].Center = global.xelorRewindPos;
                                    // Main.npc[i].velocity = global.xelorOriginalVelocity; // No restaurar velocidad a NPCs
                                    global.xelorSlowed = false;
                                    if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, i);
                                }
                            }
                        }
                        for (int i = 0; i < Main.maxProjectiles; i++)
                        {
                            if (Main.projectile[i].active)
                            {
                                var global = Main.projectile[i].GetGlobalProjectile<Content.Globals.WakfuGlobalProjectile>();
                                if (global.xelorSlowed)
                                {
                                    Main.projectile[i].Center = global.xelorRewindPos;
                                    Main.projectile[i].velocity = global.xelorOriginalVelocity; // Restaurar velocidad
                                    global.xelorSlowed = false;
                                    if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, i);
                                }
                            }
                        }
                        
                        if (Main.netMode == NetmodeID.Server)
                        {
                            ModPacket packet = GetPacket();
                            packet.Write((byte)MessageType.XelorTimeSuspension);
                            packet.Write(action);
                            packet.Write((byte)senderPlayer);
                            packet.Send(-1, whoAmI);
                        }
                    }
                    else if (action == 2) // Clear (Timeout)
                    {
                        // Limpiar flags sin rebobinar
                        for (int i = 0; i < Main.maxNPCs; i++)
                        {
                            if (Main.npc[i].active)
                            {
                                var global = Main.npc[i].GetGlobalNPC<Common.GlobalNPCs.WakfuGlobalNPC>();
                                if (global.xelorSlowed)
                                {
                                    global.xelorSlowed = false;
                                    // Main.npc[i].velocity = global.xelorOriginalVelocity; // No restaurar velocidad a NPCs
                                }
                            }
                        }
                        for (int i = 0; i < Main.maxProjectiles; i++)
                        {
                            if (Main.projectile[i].active)
                            {
                                var global = Main.projectile[i].GetGlobalProjectile<Content.Globals.WakfuGlobalProjectile>();
                                if (global.xelorSlowed)
                                {
                                    global.xelorSlowed = false;
                                    Main.projectile[i].velocity = global.xelorOriginalVelocity; // Restaurar velocidad
                                }
                            }
                        }

                        if (Main.netMode == NetmodeID.Server)
                        {
                            ModPacket packet = GetPacket();
                            packet.Write((byte)MessageType.XelorTimeSuspension);
                            packet.Write(action);
                            packet.Write((byte)senderPlayer);
                            packet.Send(-1, whoAmI);
                        }
                    }
                    break;

                case MessageType.NoxTeleport:
                    if (Main.netMode == NetmodeID.MultiplayerClient)
                    {
                        int npcIndex = reader.ReadInt32();
                        Vector2 newPos = reader.ReadVector2();
                        float nextState = reader.ReadSingle();

                        if (npcIndex >= 0 && npcIndex < Main.maxNPCs)
                        {
                            NPC npc = Main.npc[npcIndex];
                            if (npc.active && npc.type == ModContent.NPCType<Content.NPCs.Bosses.Nox.Nox>())
                            {
                                npc.Center = newPos;
                                npc.ai[1] = nextState;
                                npc.localAI[0] = 0; // Resetear timer de animación
                                npc.alpha = 255; // Forzar invisibilidad inicial para el fade-in
                            }
                        }
                    }
                    break;

                case MessageType.SpawnZurcarakMinion:
                    if (Main.netMode == NetmodeID.Server)
                    {
                        int playerIndex = reader.ReadByte();
                        Player p = Main.player[playerIndex];
                        
                        // --- FIX CRÍTICO: Añadir el buff en el servidor ANTES de spawnear el minion ---
                        // Esto evita que el minion se suicide inmediatamente al no encontrar el buff en el primer frame.
                        p.AddBuff(ModContent.BuffType<Content.Buffs.ZurcarakMinionBuff>(), 18000);

                        // Spawneamos el minion en el servidor
                        Projectile.NewProjectile(
                            p.GetSource_FromThis("ZurcarakMinionSummon"),
                            p.Center,
                            Vector2.Zero,
                            ModContent.ProjectileType<Content.Projectiles.ZurcarakMinion>(),
                            1, // Daño base
                            0f, // Knockback
                            playerIndex
                        );
                    }
                    break;

                case MessageType.KickProjectile:
                    int kickedProjIndex = reader.ReadInt32();
                    Vector2 newKickVelocity = reader.ReadVector2();

                    if (Main.netMode == NetmodeID.Server)
                    {
                        // El servidor recibe la notificación de que un proyectil fue pateado.
                        // Validamos que el índice sea correcto.
                        if (kickedProjIndex >= 0 && kickedProjIndex < Main.maxProjectiles)
                        {
                            Projectile p = Main.projectile[kickedProjIndex];
                            if (p.active)
                            {
                                // Actualizamos la velocidad en el servidor
                                p.velocity = newKickVelocity;
                                
                                // Forzamos actualización de red para que todos los demás clientes lo vean
                                p.netUpdate = true; 

                                // Opcional: Si es una bomba o jalabola, forzar estado de movimiento
                                // Esto depende de si queremos lógica específica aquí o confiamos en que el netUpdate sincronice el estado si cambió
                                // Por seguridad, podemos replicar la lógica básica de "despertar" el proyectil
                                if (p.ModProjectile is Content.Projectiles.TymadorBomb bomb)
                                {
                                    if (bomb.State == 0 || bomb.State == 3) { bomb.State = 1; bomb.Projectile.tileCollide = true; }
                                }
                                else if (p.ModProjectile is Content.Projectiles.Jalabola ball)
                                {
                                    if (ball.State == 0 || ball.State == 3) { ball.State = 1; ball.Projectile.tileCollide = true; }
                                }
                            }
                        }
                    }
                    break;

                case MessageType.PortalExplosionFX:
                    Vector2 explosionPos = reader.ReadVector2();
                    bool violent = reader.ReadBoolean();
                    if (Main.netMode != NetmodeID.Server)
                    {
                        PortalHandler.SpawnExplosionFX(explosionPos, violent);
                    }
                    break;

                case MessageType.PullPlayer:
                    int targetIndex = reader.ReadByte();
                    Vector2 pullVelocity = reader.ReadVector2();

                    if (Main.netMode == NetmodeID.Server)
                    {
                        // El servidor recibe la solicitud de atraer a un jugador.
                        // Reenviamos el paquete al jugador objetivo para que actualice su velocidad localmente.
                        ModPacket packet = GetPacket();
                        packet.Write((byte)MessageType.PullPlayer);
                        packet.Write((byte)targetIndex);
                        packet.WriteVector2(pullVelocity);
                        packet.Send(targetIndex, -1); // Enviar solo al jugador afectado
                    }
                    else
                    {
                        // Cliente: Si somos el jugador objetivo, aplicamos la velocidad.
                        if (Main.myPlayer == targetIndex)
                        {
                            Main.player[targetIndex].velocity = pullVelocity;
                            // También aplicamos la curación (50% vida)
                            int healAmount = Main.player[targetIndex].statLifeMax2 / 2;
                            Main.player[targetIndex].Heal(healAmount);
                        }
                    }
                    break;

                case MessageType.SpawnYopukaSkill:
                    if (Main.netMode == NetmodeID.Server)
                    {
                        // Leer datos del paquete
                        int playerIndex = reader.ReadByte();
                        int projType = reader.ReadInt32();
                        Vector2 position = reader.ReadVector2();
                        Vector2 velocity = reader.ReadVector2();
                        int damage = reader.ReadInt32();
                        float knockback = reader.ReadSingle();
                        int ai0 = reader.ReadInt32();
                        int ai1 = reader.ReadInt32();

                        // Spawneamos el proyectil en el servidor (se sincroniza automáticamente)
                        Projectile.NewProjectile(
                            Main.player[playerIndex].GetSource_FromThis("YopukaSkill"),
                            position,
                            velocity,
                            projType,
                            damage,
                            knockback,
                            playerIndex,
                            ai0,
                            ai1
                        );
                    }
                    break;
                case MessageType.ClosePortals:
                    if (Main.netMode == NetmodeID.Server)
                    {
                        int playerIndex = reader.ReadByte();
                        Player p = Main.player[playerIndex];
                        PortalHandler.ClosePortals(p);
                    }
                    break;
                case MessageType.RequestPortalExplosion:
                    if (Main.netMode == NetmodeID.Server)
                    {
                        int playerIndex = reader.ReadByte();
                        Player p = Main.player[playerIndex];
                        PortalHandler.TriggerViolentPortalExplosion(p);
                    }
                    break;
                case MessageType.SyncPlayerWakfuData:
                    byte playernumber = reader.ReadByte();
                    WakfuPlayer wakfuPlayer = Main.player[playernumber].GetModPlayer<WakfuPlayer>();
                    wakfuPlayer.ReceivePlayerSync(reader);

                    if (Main.netMode == NetmodeID.Server)
                    {
                        // Forward the changes to the other clients
                        wakfuPlayer.SyncPlayer(-1, whoAmI, false);
                    }
                    break;

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


                // Otros cases para otros tipos de mensajes...
                default:
                    Logger.WarnFormat("WakfuMod: Unknown Message type: {0}", msgType);
                    break;
            }
        }

    }

    // --- NUEVA CLASE DE DATOS SIMPLE (HEREDANDO DE CustomCurrencySingleCoin) ---
    // Esta clase solo existe para pasar el nombre y el color a la clase base.
    public class KamaCurrency : CustomCurrencySingleCoin
    {
        public KamaCurrency(int coinItemID, long currencyCap, string currencyTextKey) : base(coinItemID, currencyCap)
        {
            // Pasamos la clave de localización a la clase base
            CurrencyTextKey = currencyTextKey;
            // Establecemos el color del texto del precio
            CurrencyTextColor = Color.Gold;
        }
    }


}


// Please read https://github.com/tModLoader/tModLoader/wiki/Basic-tModLoader-Modding-Guide#mod-skeleton-contents for more information about the various files in a mod.