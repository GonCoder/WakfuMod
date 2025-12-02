using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using WakfuMod.Content.Projectiles;
using WakfuMod.Content.Items.Weapons;
using Terraria.DataStructures;
using WakfuMod.Content.Buffs;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader.IO; // Para Save/Load Data
using System.IO;
using System;
using System.Collections.Generic; // Para List<>
using Microsoft.Xna.Framework.Input; // Para Netcode
using WakfuMod.ModSystems; // Para FootballSystem
using WakfuMod.Content.Tiles; // Para GoalTileRed y Blue

namespace WakfuMod.jugador
{
    public enum WakfuClase { Ninguna, Selatrop, Yopuka, Steamer, Tymador, Zurcarac, Xelor }

    public class WakfuPlayer : ModPlayer
    {
        // Generales
        public FootballTeam currentFootballTeam = FootballTeam.None;
        public WakfuClase claseElegida = WakfuClase.Ninguna;
        private bool haMostradoMensajeClase = false;
        public bool HideHeldYopukaSword = false; // Flag específico para la espada

        public bool HidePlayerForKick = false;

        // --- NUEVO: Balance Mode ---
        public bool BalanceMode = false;

        // --- NUEVO: Variables del Xelor ---
        public int xelorTeleportCooldown = 0;
        public const int XelorTeleportBaseCooldown = 360; // 6 segundos
        
        public bool xelorTimeSuspensionActive = false;
        public int xelorTimeSuspensionTimer = 0;
        public int xelorAbility2Cooldown = 0;
        public const int XelorAbility2BaseCooldown = 1200; // 20 segundos
        public const int XelorTimeSuspensionDuration = 600; // 10 segundos

        // --- NUEVO: Variables del Zurcarac ---
        public bool zurcarakMinionActive = false; // ¿Está el gatito invocado?
        public int zurcarakAbility1Cooldown = 0; // Cooldown para el Arañazo Loco
        public const int ZurcarakAbility1BaseCooldown = 180; // 3 segundos
        public int zurcarakAbility2Cooldown = 0; // Cooldown para el Dado
        public const int ZurcarakAbility2BaseCooldown = 3600; //1200 20 segundos (ejemplo, ajustar)
        public bool IsRollingDie = false; // Flag para ocultar jugador durante la habilidad 2

        // --- Métodos para el Arma ---
        public void ResetYopukaAbilityCooldowns()
        {
            // Resetea los cooldowns de las habilidades vinculadas a V y X
            // Asumiendo que espadaCooldown es el que usan ambas
            espadaCooldown = 0;

        }

        public void MaximizeRage()
        {
            if (claseElegida == WakfuClase.Yopuka)
            {
                rageTicks = 5; // Establecer al máximo
                rageDecayTimer = 0; // Resetear decay
                rageCooldown = 0; // Permitir ganar rabia de nuevo inmediatamente? O poner un pequeño CD?
                                  // TODO: Sincronizar rageTicks
            }
        }
        public override void ResetEffects()
        {
            // Resetear flag cada frame
            HideHeldYopukaSword = false;
            // HidePlayerForKick = false;
            IsRollingDie = false; // <-- Resetear flag del dado

            // --- Resetear estado del minion ---
            // El buff se encargará de mantenerlo activo si existe
            zurcarakMinionActive = false;
        }

        // Yopuka
        private int rageTicks = 0;
        private int rageCooldown = 0; // Cooldown para ganar rabia
        private int rageDecayTimer = 0; // Timer para perder rabia
        public int espadaCooldown = 0; // Cooldown compartido para habilidades Yopuka?
        public bool IsJumpingAsGod = false; // Estado visual/invulnerabilidad salto
        public bool old_IsJumpingAsGod = false;

        // Steamer
        public int steamerTorretaCooldown = 0;
        public int steamerGranadaCooldown = 0;

        // Selatrop
        private double lastTeleportTime = 0;
        public int portalPhysicsCooldown = 0; // Cooldown para evitar bucles de teletransporte en portales

        // --- Control de Estado Visual (Salto Yopuka) ---
        public void SetJumpVisuals(bool active)
        {
            if (IsJumpingAsGod != active)
            {
                IsJumpingAsGod = active;
                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    // TODO: Implementar sincronización de IsJumpingAsGod
                }
            }
        }

        // --- Getters para Yopuka ---
        public int GetRageTicks() => rageTicks;
        public float GetRageMultiplier() => 1f + (rageTicks * 0.10f);

        // --- Ciclo de Actualización Principal ---
        public override void PreUpdate()
        {
            // Guardar estado anterior
            old_IsJumpingAsGod = IsJumpingAsGod;

            // --- Mensaje y Selección de Clase (solo local) ---
            if (Main.myPlayer == Player.whoAmI)
            {
                if (claseElegida == WakfuClase.Ninguna && !haMostradoMensajeClase && Main.GameUpdateCount > 120)
                {
                    Main.NewText("Press F1-F6 to choose your class.\nF1-Selatrop-\nF2-Yopuka-\nF3-Steamer-\nF4-Rogue-\nF5-Zurcarac-\nF6-Xelor-", Color.OrangeRed);
                    haMostradoMensajeClase = true;
                }
                HandleClaseSeleccion(); // Manejar la selección si aún no tiene clase
            }

            // --- Actualizar Cooldowns y Estados (Para todos) ---
            if (espadaCooldown > 0) espadaCooldown--;
            if (steamerTorretaCooldown > 0) steamerTorretaCooldown--;
            if (steamerGranadaCooldown > 0) steamerGranadaCooldown--;
            if (rageCooldown > 0) rageCooldown--;
            if (zurcarakAbility1Cooldown > 0) zurcarakAbility1Cooldown--;
            if (zurcarakAbility2Cooldown > 0) zurcarakAbility2Cooldown--;
            if (portalPhysicsCooldown > 0) portalPhysicsCooldown--;
            if (xelorTeleportCooldown > 0) xelorTeleportCooldown--;
            if (xelorAbility2Cooldown > 0) xelorAbility2Cooldown--;

            // --- Lógica Xelor Time Suspension ---
            if (xelorTimeSuspensionActive)
            {
                // Solo el dueño controla el tiempo
                if (Main.myPlayer == Player.whoAmI)
                {
                    xelorTimeSuspensionTimer--;
                    if (xelorTimeSuspensionTimer <= 0)
                    {
                        // Se acabó el tiempo -> REBOBINAR AUTOMÁTICAMENTE
                        xelorTimeSuspensionActive = false;
                        xelorAbility2Cooldown = XelorAbility2BaseCooldown;

                        if (Main.netMode == NetmodeID.MultiplayerClient)
                        {
                            ModPacket packet = Mod.GetPacket();
                            packet.Write((byte)WakfuMod.MessageType.XelorTimeSuspension);
                            packet.Write((byte)1); // Action: Rewind (Antes era 2: Clear)
                            packet.Write((byte)Player.whoAmI);
                            packet.Send();
                        }

                        Main.NewText("Time Rewind!", Color.MediumPurple);

                        // Aplicar rewind localmente SIEMPRE
                        for (int i = 0; i < Main.maxNPCs; i++)
                        {
                            if (Main.npc[i].active)
                            {
                                var g = Main.npc[i].GetGlobalNPC<Common.GlobalNPCs.WakfuGlobalNPC>();
                                if (g.xelorSlowed)
                                {
                                    Main.npc[i].Center = g.xelorRewindPos;
                                    // Main.npc[i].velocity = g.xelorOriginalVelocity; // No restaurar velocidad a NPCs
                                    g.xelorSlowed = false;
                                }
                            }
                        }
                        for (int i = 0; i < Main.maxProjectiles; i++)
                        {
                            if (Main.projectile[i].active)
                            {
                                var g = Main.projectile[i].GetGlobalProjectile<Content.Globals.WakfuGlobalProjectile>();
                                if (g.xelorSlowed)
                                {
                                    Main.projectile[i].Center = g.xelorRewindPos;
                                    g.xelorSlowed = false;
                                    Main.projectile[i].velocity = g.xelorOriginalVelocity; // Restaurar velocidad
                                }
                            }
                        }
                    }
                }
            }

            // --- APLICAR EFECTO CONTINUAMENTE (Para nuevos proyectiles/NPCs) ---
            // Solo si sigue activo después de la comprobación de tiempo
            if (xelorTimeSuspensionActive)
            {
                float range = 1200f;
                // Proyectiles
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    if (Main.projectile[i].active && Main.projectile[i].hostile && Vector2.Distance(Player.Center, Main.projectile[i].Center) <= range)
                    {
                        var g = Main.projectile[i].GetGlobalProjectile<Content.Globals.WakfuGlobalProjectile>();
                        if (!g.xelorSlowed)
                        {
                            g.xelorSlowed = true;
                            g.xelorRewindPos = Main.projectile[i].Center; // Guardar posición actual como punto de rebobinado
                            g.xelorOriginalVelocity = Main.projectile[i].velocity; // Guardar velocidad original
                            Main.projectile[i].velocity *= 0.2f; // Aplicar ralentización UNA VEZ
                        }
                    }
                }
                // NPCs
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].active && !Main.npc[i].friendly && Vector2.Distance(Player.Center, Main.npc[i].Center) <= range)
                    {
                        var g = Main.npc[i].GetGlobalNPC<Common.GlobalNPCs.WakfuGlobalNPC>();
                        if (!g.xelorSlowed)
                        {
                            g.xelorSlowed = true;
                            g.xelorRewindPos = Main.npc[i].Center;
                            g.xelorOriginalVelocity = Main.npc[i].velocity; // Guardar velocidad original (por si acaso)
                            // Main.npc[i].velocity *= 0.2f; // No aplicar ralentización única a NPCs
                        }
                    }
                }
            }

            if (claseElegida == WakfuClase.Yopuka)
            {
                rageDecayTimer++;
                if (rageDecayTimer >= 180 && rageTicks > 0)
                {
                    rageTicks--;
                    rageDecayTimer = 0;
                    // TODO: Sincronizar rageTicks
                }
            }

            // --- Lógica de Activación de Habilidades (SOLO DUEÑO) ---
            if (Main.myPlayer != Player.whoAmI) return;

            // --- Lógica de Habilidades (AÑADIR LÓGICA FOOTBALL) ---
            if (Main.myPlayer == Player.whoAmI) // Sigue siendo solo para el jugador local
            {
                // --- LÓGICA PARA JUGADORES EN EQUIPO DE FÚTBOL ---
                if (currentFootballTeam != FootballTeam.None)
                {
                    // Habilidad 1: Colocar Portería del Equipo
                    if (WakfuMod.Habilidad1Keybind.JustPressed)
                    {
                        int tileTypeToPlace = -1;
                        if (currentFootballTeam == FootballTeam.Red)
                            tileTypeToPlace = ModContent.TileType<GoalTileRed>();
                        else if (currentFootballTeam == FootballTeam.Blue)
                            tileTypeToPlace = ModContent.TileType<GoalTileBlue>(); // Necesitarás crear GoalTileBlue.cs

                        if (tileTypeToPlace != -1)
                        {
                            // Obtener coordenadas del MUNDO del cursor
                            Vector2 mouseWorld = Main.MouseWorld;
                            // Convertir a coordenadas de TILE
                            Point placeCoords = mouseWorld.ToTileCoordinates();
                            // Validar si se puede colocar (ej. no dentro de bloques sólidos)
                            if (WorldGen.InWorld(placeCoords.X, placeCoords.Y) && !Main.tile[placeCoords.X, placeCoords.Y].HasTile)
                            {
                                // Colocar el tile
                                WorldGen.PlaceTile(placeCoords.X, placeCoords.Y, tileTypeToPlace, false, true, Player.whoAmI);
                                // Sincronizar colocación
                                if (Main.netMode == NetmodeID.MultiplayerClient)
                                {
                                    NetMessage.SendTileSquare(Player.whoAmI, placeCoords.X, placeCoords.Y, 1);
                                }
                                SoundEngine.PlaySound(SoundID.Dig, Main.MouseWorld);
                            }
                            else
                            {
                                Main.NewText("Cannot place goal here!", Color.OrangeRed);
                            }
                        }
                    }

                    // Habilidad 2: Borrar Porterías del Equipo
                    if (WakfuMod.Habilidad2Keybind.JustPressed)
                    {
                        int tileTypeToRemove = -1;
                        Color teamColor = Color.Gray;
                        if (currentFootballTeam == FootballTeam.Red)
                        {
                            tileTypeToRemove = ModContent.TileType<GoalTileRed>();
                            teamColor = Color.Red;
                        }
                        else if (currentFootballTeam == FootballTeam.Blue)
                        {
                            tileTypeToRemove = ModContent.TileType<GoalTileBlue>();
                            teamColor = Color.SkyBlue;
                        }

                        if (tileTypeToRemove != -1)
                        {
                            int tilesRemoved = 0;
                            int range = 80; // Rango alrededor del jugador
                            Point playerTile = Player.Center.ToTileCoordinates();

                            for (int x = playerTile.X - range; x < playerTile.X + range; x++)
                            {
                                for (int y = playerTile.Y - range; y < playerTile.Y + range; y++)
                                {
                                    // Comprobar límites del mundo primero
                                    if (!WorldGen.InWorld(x, y, 5)) continue; // Salta si está fuera de límites

                                    Tile currentTile = Main.tile[x, y]; // Acceso directo al tile

                                    // Comprobar si tiene tile y es del tipo correcto
                                    if (currentTile.HasTile && currentTile.TileType == tileTypeToRemove)
                                    {
                                        // --- MÉTODO MÁS DIRECTO PARA BORRAR ---
                                        currentTile.HasTile = false; // Marca como que ya no tiene tile
                                        currentTile.TileType = TileID.Dirt; // Resetea el tipo (opcional pero bueno)
                                                                  // WorldGen.SquareTileFrame(x, y, true); // Fuerza actualización de frames alrededor (Opcional)
                                                                  // --- FIN MÉTODO DIRECTO ---

                                        // WorldGen.KillTile(x, y, false, false, true); // Método anterior (puede fallar a veces)

                                        tilesRemoved++;

                                        // --- SINCRONIZACIÓN CRUCIAL ---
                                        // Enviar actualización para ESTE tile específico a todos
                                        NetMessage.SendTileSquare(-1, x, y, 1); // -1 = a todos, x, y, size=1
                                    }
                                }
                            }
                            if (tilesRemoved > 0)
                            {
                                Main.NewText($"Removed {tilesRemoved} goal tiles for Team {currentFootballTeam}.", teamColor);
                                SoundEngine.PlaySound(SoundID.Grab, Player.position);
                            }
                            else
                            {
                                Main.NewText($"No goal tiles found nearby for Team {currentFootballTeam}.", teamColor * 0.7f);
                            }
                        }
                    } // Fin Habilidad 2
                }
                // --- SI NO ESTÁ EN EQUIPO, EJECUTAR LÓGICA DE CLASE WAKFU ---
                else
                {

                    switch (claseElegida)
                    {
                        case WakfuClase.Selatrop:
                            if (WakfuMod.Habilidad1Keybind.JustPressed) PortalHandler.TryPlacePortal(Player);
                            if (WakfuMod.Habilidad2Keybind.JustPressed) PortalHandler.ClosePortals(Player);
                            // La lógica de teletransporte ahora se maneja en PortalProjectile.cs para que funcione con todos los jugadores
                            break;

                        case WakfuClase.Xelor:
                            // Habilidad 1 (V): Teletransporte (Similar al Bastón de la Discordia)
                            if (WakfuMod.Habilidad1Keybind.JustPressed && xelorTeleportCooldown <= 0)
                            {
                                Vector2 targetPos = Main.MouseWorld;
                                // Validar colisión (no teletransportarse dentro de bloques sólidos)
                                // Usamos el tamaño del jugador para comprobar si cabe
                                Vector2 playerSize = new Vector2(Player.width, Player.height);
                                Vector2 checkPos = targetPos - playerSize / 2f; // Centrar la comprobación

                                if (!Collision.SolidCollision(checkPos, Player.width, Player.height))
                                {
                                    // Teletransportar
                                    Player.Teleport(targetPos, 1, 0); 
                                    Player.Center = targetPos; // Asegurar posición exacta
                                    
                                    // Sincronizar
                                    if (Main.netMode == NetmodeID.MultiplayerClient)
                                    {
                                        NetMessage.SendData(MessageID.TeleportEntity, -1, -1, null, 0, (float)Player.whoAmI, targetPos.X, targetPos.Y, 1);
                                    }

                                    // Efectos
                                    SoundEngine.PlaySound(new SoundStyle("WakfuMod/audio/NoxBlink"), Player.Center); // Sonido de teletransporte mágico
                                    for (int i = 0; i < 30; i++)
                                    {
                                        Dust.NewDust(Player.position, Player.width, Player.height, DustID.Electric, 0, 0, 100, default, 1.5f);
                                    }

                                    // Cooldown
                                    xelorTeleportCooldown = XelorTeleportBaseCooldown;
                                }
                                else
                                {
                                    // Feedback visual de fallo (opcional)
                                    Main.NewText("Cannot teleport there!", Color.Red);
                                }
                            }

                            // Habilidad 2 (X): Suspensión Temporal / Rebobinado
                            if (WakfuMod.Habilidad2Keybind.JustPressed)
                            {
                                if (!xelorTimeSuspensionActive)
                                {
                                    // ACTIVAR
                                    if (xelorAbility2Cooldown <= 0)
                                    {
                                        xelorTimeSuspensionActive = true;
                                        xelorTimeSuspensionTimer = XelorTimeSuspensionDuration;

                                        // Buscar objetivos
                                        List<int> npcTargets = new List<int>();
                                        List<Vector2> npcPositions = new List<Vector2>();
                                        List<int> projTargets = new List<int>();
                                        List<Vector2> projPositions = new List<Vector2>();

                                        float range = 1200f;

                                        for (int i = 0; i < Main.maxNPCs; i++)
                                        {
                                            if (Main.npc[i].active && !Main.npc[i].friendly && Vector2.Distance(Player.Center, Main.npc[i].Center) <= range)
                                            {
                                                npcTargets.Add(i);
                                                npcPositions.Add(Main.npc[i].Center);
                                            }
                                        }

                                        for (int i = 0; i < Main.maxProjectiles; i++)
                                        {
                                            if (Main.projectile[i].active && Main.projectile[i].hostile && Vector2.Distance(Player.Center, Main.projectile[i].Center) <= range)
                                            {
                                                projTargets.Add(i);
                                                projPositions.Add(Main.projectile[i].Center);
                                            }
                                        }

                                        // Enviar paquete
                                        if (Main.netMode == NetmodeID.MultiplayerClient)
                                        {
                                            ModPacket packet = Mod.GetPacket();
                                            packet.Write((byte)WakfuMod.MessageType.XelorTimeSuspension);
                                            packet.Write((byte)0); // Action: Activate
                                            packet.Write((byte)Player.whoAmI);
                                            
                                            packet.Write(npcTargets.Count);
                                            for(int i=0; i<npcTargets.Count; i++) { packet.Write(npcTargets[i]); packet.WriteVector2(npcPositions[i]); }
                                            
                                            packet.Write(projTargets.Count);
                                            for(int i=0; i<projTargets.Count; i++) { packet.Write(projTargets[i]); packet.WriteVector2(projPositions[i]); }
                                            
                                            packet.Send();
                                        }

                                        // Efectos visuales locales
                                        SoundEngine.PlaySound(SoundID.Item113, Player.Center); // Sonido mágico
                                        Main.NewText("Time Suspension Activated!", Color.Purple);
                                        
                                        // Aplicar localmente SIEMPRE (para feedback instantáneo)
                                        foreach(int id in npcTargets) {
                                            if (id >= 0 && id < Main.maxNPCs) {
                                                var g = Main.npc[id].GetGlobalNPC<Common.GlobalNPCs.WakfuGlobalNPC>();
                                                g.xelorSlowed = true; g.xelorRewindPos = Main.npc[id].Center;
                                                g.xelorOriginalVelocity = Main.npc[id].velocity;
                                                // Main.npc[id].velocity *= 0.2f; // No aplicar ralentización única a NPCs
                                            }
                                        }
                                        foreach(int id in projTargets) {
                                            if (id >= 0 && id < Main.maxProjectiles) {
                                                var g = Main.projectile[id].GetGlobalProjectile<Content.Globals.WakfuGlobalProjectile>();
                                                g.xelorSlowed = true; g.xelorRewindPos = Main.projectile[id].Center;
                                                g.xelorOriginalVelocity = Main.projectile[id].velocity;
                                                Main.projectile[id].velocity *= 0.2f; // Aplicar ralentización UNA VEZ
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // Cooldown msg
                                    }
                                }
                                else
                                {
                                    xelorAbility2Cooldown = XelorAbility2BaseCooldown;
                                    xelorTimeSuspensionActive = false; // <--- FIX: Desactivar estado activo

                                    if (Main.netMode == NetmodeID.MultiplayerClient)
                                    {
                                        ModPacket packet = Mod.GetPacket();
                                        packet.Write((byte)WakfuMod.MessageType.XelorTimeSuspension);
                                        packet.Write((byte)1); // Action: Rewind
                                        packet.Write((byte)Player.whoAmI);
                                        packet.Send();
                                    }

                                    SoundEngine.PlaySound(SoundID.Item4, Player.Center); // Sonido diferente
                                    Main.NewText("Time Rewind!", Color.MediumPurple);

                                    // Aplicar rewind localmente SIEMPRE
                                    for (int i = 0; i < Main.maxNPCs; i++) {
                                        if (Main.npc[i].active) {
                                            var g = Main.npc[i].GetGlobalNPC<Common.GlobalNPCs.WakfuGlobalNPC>();
                                            if (g.xelorSlowed) {
                                                Main.npc[i].Center = g.xelorRewindPos;
                                                g.xelorSlowed = false;
                                                // Main.npc[i].velocity = g.xelorOriginalVelocity; // No restaurar velocidad a NPCs
                                            }
                                        }
                                    }
                                    for (int i = 0; i < Main.maxProjectiles; i++) {
                                        if (Main.projectile[i].active) {
                                            var g = Main.projectile[i].GetGlobalProjectile<Content.Globals.WakfuGlobalProjectile>();
                                            if (g.xelorSlowed) {
                                                Main.projectile[i].Center = g.xelorRewindPos;
                                                g.xelorSlowed = false;
                                                Main.projectile[i].velocity = g.xelorOriginalVelocity; // Restaurar velocidad
                                            }
                                        }
                                    }
                                }
                            }
                            break;

                        case WakfuClase.Yopuka:
                            // Habilidad 1: Espadazo (Basado en tu código original)
                            if (WakfuMod.Habilidad1Keybind.JustPressed && rageTicks > 0 && espadaCooldown <= 0)
                            {
                                // Nota: Tu código original pasaba 'damage' (100*rage) a originalDamage.
                                // El proyectil moderno lo calcula en OnSpawn/ModifyHit. Pasaremos la rabia.
                                int baseProjDamage = 20; // Daño base del proyectil, ajusta si es necesario
                                float baseProjKnockback = 5f;
                                Vector2 spawn = new Vector2(Main.MouseWorld.X, Player.Center.Y - 700);
                                Vector2 velocity = Vector2.UnitY * 25f;

                                // --- SPAWN LOCAL (CLIENTE) ---
                                // Spawneamos el proyectil localmente. Terraria se encarga de sincronizarlo
                                // automáticamente porque es un proyectil propiedad del jugador local.
                                int p = Projectile.NewProjectile(
                                    Player.GetSource_FromThis("YopukaSword"),
                                    spawn, velocity,
                                    ModContent.ProjectileType<YopukaSwordProjectile>(),
                                    baseProjDamage, baseProjKnockback, Player.whoAmI,
                                    ai0: 0, // ai[0] = HasStruck (empieza en 0)
                                    ai1: rageTicks // Pasar la rabia actual en ai[1]
                                );
                                
                                // Forzar actualización de red para asegurar que ai[1] (Rage) llegue a otros clientes
                                Main.projectile[p].netUpdate = true;

                                ConsumeRage(); // Consumir rabia
                                espadaCooldown = 300; // 5 seg cooldown
                            }
                            // Habilidad 2: Salto Divino (Basado en tu código original)
                            if (WakfuMod.Habilidad2Keybind.JustPressed && rageTicks > 0 && espadaCooldown <= 0)
                            {
                                // --- SPAWN LOCAL (CLIENTE) ---
                                // Spawneamos el proyectil localmente para respuesta instantánea.
                                int p = Projectile.NewProjectile(
                                    Player.GetSource_FromThis("YopukaJump"),
                                    Player.Center, Vector2.Zero, // Posición inicial, velocidad se calcula en OnSpawn
                                    ModContent.ProjectileType<YopukaJumpAbility>(),
                                    0, 0f, Player.whoAmI,
                                    ai0: rageTicks, // Pasar la rabia actual en ai[0]
                                    ai1: Player.direction // Pasar dirección en ai[1]
                                );

                                // Forzar actualización de red
                                Main.projectile[p].netUpdate = true;

                                ConsumeRage(); // Consumir rabia inmediatamente para feedback visual en UI
                                espadaCooldown = 300; // Aplicar cooldown
                            }
                            break;

                        case WakfuClase.Steamer:
                            // Habilidad 1: Invocar Torreta O Disparar Láser Especial
                            if (WakfuMod.Habilidad1Keybind.JustPressed)
                            {
                                // Buscar si ya existe una torreta activa del jugador
                                int existingTurretIndex = -1;
                                for (int i = 0; i < Main.maxProjectiles; i++)
                                {
                                    Projectile p = Main.projectile[i];
                                    if (p.active && p.owner == Player.whoAmI && p.type == ModContent.ProjectileType<SteamerTurretProjectile>())
                                    {
                                        existingTurretIndex = i;
                                        break;
                                    }
                                }

                                // --- SI LA TORRETA YA EXISTE: Disparar Láser Especial ---
                                if (existingTurretIndex != -1)
                                {
                                    Projectile turret = Main.projectile[existingTurretIndex];
                                    // Comprobar si la torreta está lista para disparar (opcional, podría tener su propio cooldown)
                                    bool canShootSpecial = turret.localAI[0] <= 0; // Ejemplo: Usar localAI[0] como cooldown

                                    if (canShootSpecial)
                                    {
                                        // Indicar a la torreta que dispare usando un índice de AI no utilizado
                                        // Usaremos ai[1] para la señal de disparo especial (0 = no disparar, 1 = disparar)
                                        // Asegúrate de que ai[1] no se use para otra cosa en la torreta
                                        if (turret.ai[1] == 0f) // Solo si no está ya marcada para disparar
                                        {
                                            turret.ai[1] = 1f; // Señal para disparar
                                                               // Opcional: Resetear cooldown si usas localAI
                                            turret.localAI[0] = 90; // Ejemplo: 1.5 segundos de cooldown para este disparo
                                                                    // Networking: ai[] se sincroniza automáticamente por Terraria/tModLoader
                                                                    // Si usas localAI, necesitarás sincronizarlo manualmente si es importante para otros jugadores.
                                                                    // Jugar un sonido desde el jugador para indicar la orden?
                                            SoundEngine.PlaySound(SoundID.Item13 with { Volume = 0.8f, Pitch = 2f }, Player.position); // Sonido tipo Last Prism
                                                                                                                                       // --- 3. Spawnea el Proyectil de Señal Visual ---
                                            Vector2 playerCenter = Player.MountedCenter;
                                            Vector2 turretCenter = turret.Center; // Ya tenemos la torreta aquí
                                            Vector2 directionToTurret = (turretCenter - playerCenter).SafeNormalize(Vector2.UnitX);
                                            float signalSpeed = 24f;

                                            Projectile.NewProjectile(
                                                Player.GetSource_FromThis("TurretSignal"),
                                                playerCenter,
                                                directionToTurret * signalSpeed,
                                                ModContent.ProjectileType<TurretActivationSignal>(),
                                                0, 0f, Player.whoAmI,
                                                ai0: turret.whoAmI // <<<--- PASA EL ÍNDICE (whoAmI) DE LA TORRETA EN AI[0]
                                            );
                                        }
                                    }
                                    else
                                    {
                                        SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 1f, Pitch = 1.2f }, Player.position); // Ejemplo: un tick suave
                                                                                                                                     // Sonido diferente para el CD

                                    }
                                }
                                // --- SI LA TORRETA NO EXISTE: Invocarla (Misma lógica que antes) ---
                                else
                                {
                                    if (steamerTorretaCooldown <= 0) // Usa el cooldown general de invocación
                                    {
                                        Vector2 cursor = Main.MouseWorld;
                                        Vector2 spawnPos = cursor;
                                        bool foundGround = false;
                                        for (int i = 0; i < 60; i++)
                                        {
                                            Vector2 check = cursor + new Vector2(0, i * 16);
                                            Point coords = check.ToTileCoordinates();
                                            if (WorldGen.InWorld(coords.X, coords.Y, 10))
                                            {
                                                Tile tile = Framing.GetTileSafely(coords.X, coords.Y);
                                                if (tile.HasTile && (Main.tileSolid[tile.TileType] || TileID.Sets.Platforms[tile.TileType]))
                                                {
                                                    // Obtener altura de la torreta (puede fallar si la instancia no existe, mejor hardcodear o usar una constante)
                                                    int turretHeightApprox = 48; // Usa el valor de SetDefaults
                                                    spawnPos = new Vector2(cursor.X, coords.Y * 16f - turretHeightApprox);
                                                    foundGround = true;
                                                    break;
                                                }
                                            }
                                        }

                                        if (foundGround)
                                        {
                                            // Ya no necesitamos eliminar la torreta anterior aquí, porque este bloque solo se ejecuta si no existe.
                                            SoundEngine.PlaySound(SoundID.Item37, spawnPos);
                                            Projectile.NewProjectile(Player.GetSource_FromThis("SteamerTurret"), spawnPos, Vector2.Zero,
                                                ModContent.ProjectileType<SteamerTurretProjectile>(),
                                                20, 0f, Player.whoAmI,
                                                ai0: 0f, // ai[0] puede usarse para otra cosa
                                                ai1: 0f); // ai[1] empieza en 0 (no disparar láser especial)
                                            steamerTorretaCooldown = 180; // Cooldown para volver a invocar
                                        }
                                        else
                                        {
                                            Main.NewText("Cannot place turret here.", Color.Red);
                                        }
                                    } // Fin else (no existe torreta)
                                } // Fin Habilidad1Keybind
                            }
                            // Habilidad 2: Detonar Torreta (Tu lógica original adaptada)
                            if (WakfuMod.Habilidad2Keybind.JustPressed)
                            {
                                for (int j = 0; j < Main.maxProjectiles; j++)
                                {
                                    Projectile proj = Main.projectile[j];
                                    if (proj.active && proj.owner == Player.whoAmI && proj.type == ModContent.ProjectileType<SteamerTurretProjectile>())
                                    {
                                        Vector2 explosionPosition = proj.Center;
                                        // Efectos visuales (solo cliente)
                                        if (Main.netMode != NetmodeID.Server)
                                        {
                                            for (int i = 0; i < 150; i++)
                                            {
                                                Vector2 dustVel = Main.rand.NextVector2Circular(8f, 8f); // Ajustar velocidad dust
                                                Dust.NewDustPerfect(explosionPosition + Main.rand.NextVector2Circular(10f, 10f), DustID.PurpleTorch, dustVel, 100, default, 2.5f).noGravity = true; // Usar NewDustPerfect para más control
                                            }
                                            SoundEngine.PlaySound(SoundID.Item14, explosionPosition);
                                        }
                                        // Lógica de Daño
                                        float radius = 160f;
                                        foreach (NPC npc in Main.npc)
                                        {
                                            if (npc.active && !npc.friendly && npc.CanBeChasedBy(proj) && Vector2.DistanceSquared(npc.Center, explosionPosition) <= radius * radius)
                                            {
                                                int dmg = 0;
                                                if (BalanceMode)
                                                {
                                                    dmg = (int)(30f * Player.GetDamage(DamageClass.Ranged).Multiplicative);
                                                }
                                                else
                                                {
                                                    dmg = 20 + (int)(npc.lifeMax * 0.03f);
                                                }
                                                // Aplicar daño usando modificadores del jugador
                                                Player.ApplyDamageToNPC(npc, dmg, 0f, Math.Sign(npc.Center.X - explosionPosition.X), false, DamageClass.Summon); // O tu DamageClass preferida
                                            }
                                        }
                                        proj.Kill();
                                    }
                                }
                            }
                            break;


                        case WakfuClase.Tymador:
                            if (WakfuMod.Habilidad1Keybind.JustPressed) TymadorAbilityHandler.TryPlaceBomb(Player);
                            if (WakfuMod.Habilidad2Keybind.JustPressed)
                            {
                                for (int i = 0; i < Main.maxProjectiles; i++)
                                {
                                    Projectile bomb = Main.projectile[i];
                                    if (bomb.active && bomb.owner == Player.whoAmI && bomb.type == ModContent.ProjectileType<TymadorBomb>())
                                    {
                                        bomb.Kill();
                                    }
                                }
                            }
                            break;

                        case WakfuClase.Zurcarac:
                            // --- Habilidad 1: Invocar Gatito / Arañazo Loco ---
                            if (WakfuMod.Habilidad1Keybind.JustPressed)
                            {
                                // Comprobar si el buff (y por tanto el minion) está activo
                                bool minionIsActuallyActive = Player.HasBuff(ModContent.BuffType<ZurcarakMinionBuff>());

                                if (!minionIsActuallyActive)
                                {
                                    // --- Invocar Gatito ---
                                    if (Main.netMode == NetmodeID.MultiplayerClient)
                                    {
                                        ModPacket packet = Mod.GetPacket();
                                        packet.Write((byte)WakfuMod.MessageType.SpawnZurcarakMinion);
                                        packet.Write((byte)Player.whoAmI);
                                        packet.Send();
                                    }
                                    else
                                    {
                                        Projectile.NewProjectile(Player.GetSource_FromThis("ZurcarakMinionSummon"),
                                            Player.Center, Vector2.Zero,
                                            ModContent.ProjectileType<ZurcarakMinion>(),
                                            1, // Daño base 1 (el daño real es % vida)
                                            0f, // Knockback base 0
                                            Player.whoAmI);
                                    }
                                    // Añadir el buff para mantenerlo vivo
                                    Player.AddBuff(ModContent.BuffType<ZurcarakMinionBuff>(), 2); // Duración 2 ticks, se refresca solo
                                }
                                else // --- Activar Arañazo Loco ---
                                {
                                    if (zurcarakAbility1Cooldown <= 0)
                                    {
                                        // Buscar el proyectil del minion
                                        Projectile minion = FindPlayerMinion(Player, ModContent.ProjectileType<ZurcarakMinion>());
                                        if (minion != null)
                                        {
                                            // Enviar señal al minion para que ataque (usando ai[1])
                                            if (minion.ai[1] == 0f) // Solo si no está ya atacando
                                            {
                                                minion.ai[1] = 1f; // Señal para activar Arañazo Loco
                                                minion.netUpdate = true; // Sincronizar
                                                zurcarakAbility1Cooldown = ZurcarakAbility1BaseCooldown; // Poner habilidad en CD
                                                                                                         // Sonido de activación
                                                SoundEngine.PlaySound(SoundID.Roar with { Pitch = 0.5f, Volume = 0.7f }, minion.Center); // Ejemplo: Rugido de gato
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // Opcional: Mensaje de cooldown
                                        // Main.NewText($"Feline Frenzy on cooldown! ({zurcarakAbility1Cooldown / 60f:F1}s)", Color.Orange);
                                    }
                                }
                            } // Fin Habilidad 1

                                    // --- Habilidad 2: Lanzar el Dado ---
                                    if (WakfuMod.Habilidad2Keybind.JustPressed && zurcarakAbility2Cooldown <= 0)
                                    {
                                        // 1. Poner en Cooldown
                                        zurcarakAbility2Cooldown = ZurcarakAbility2BaseCooldown;
        
                                        // 2. Ocultar jugador (Señal para HideDrawLayers)
                                        IsRollingDie = true;
        
                                        // --- 3. Spawnea el proyectil visual del dado (CON OFFSETS) ---
        
                                        // Define tus offsets aquí (ajusta estos valores)
                                        float offsetX = 80f; // 40 píxeles delante del jugador
                                        float offsetY = -90f; // 60 píxeles POR ENCIMA del centro del jugador
        
                                        // Calcular la posición de spawn final
                                        Vector2 spawnPosition = Player.Center + new Vector2(Player.direction * offsetX, offsetY);
        
                                        Projectile.NewProjectile(
                                            Player.GetSource_FromThis("ZurcarakDieCast"),
                                            spawnPosition, // <-- Usar la posición con offset
                                            Vector2.Zero, // El dado es estático, no necesita velocidad inicial
                                            ModContent.ProjectileType<ZurcarakDie>(),
                                            0, // Sin daño directo
                                            0f,
                                            Player.whoAmI
                                        // ya no necesitamos pasar la dirección en ai[0] si el dado no se mueve
                                        );
        
                                        // 4. Sonido de lanzar dado
                                        SoundEngine.PlaySound(SoundID.Item35, Player.position); // Sonido de "lanzar"
                                    }
                                    break; // Fin case Zurcarac
                            } // Fin del switch
                    }
                } // --- Comprobar si la espada Yopuka está siendo usada con clic derecho ---
                  // Esto debe hacerse DESPUÉS de que CanUseItem/UseItem se hayan procesado potencialmente en el tick anterior,
                  // pero ANTES de que el dibujo ocurra. PreUpdate es un buen lugar.
                if (Player.HeldItem.type == ModContent.ItemType<YopukaShockwaveSword>() // Si tiene la espada
                    && Player.altFunctionUse == 2 // Y está usando clic derecho
                    && Player.itemAnimation > 0) // Y la animación está activa
                {
                    HideHeldYopukaSword = true;
                }

                // COSAS TYMADOR
                if (Main.myPlayer == Player.whoAmI) // Solo el dueño necesita controlar esto directamente
                {
                    // Comprueba si existe algún proyectil de patada activo
                    bool kickProjectileExists = false;
                    for (int i = 0; i < Main.maxProjectiles; i++)
                    {
                        Projectile p = Main.projectile[i];
                        if (p.active && p.owner == Player.whoAmI && p.type == ModContent.ProjectileType<TymadorKickProjectile>())
                        {
                            kickProjectileExists = true;
                            break;
                        }
                    }
                    // Si NO hay proyectil de patada activo, asegúrate de que el flag esté en false
                    if (!kickProjectileExists)
                    {
                        HidePlayerForKick = false;
                    }
                }
        } //Fin del Pre-update

        // --- Helper para encontrar minion ---
        private Projectile FindPlayerMinion(Player player, int projectileType)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == player.whoAmI && p.type == projectileType)
                {
                    return p;
                }
            }
            return null;
        }


        // --- Al entrar al mundo ---
        public override void OnEnterWorld()
        {
            TymadorAbilityHandler.ResetBombSystem();
            espadaCooldown = 0;
            steamerTorretaCooldown = 0;
            steamerGranadaCooldown = 0;
            // La rabia se carga desde LoadData si existe
            rageCooldown = 0;
            rageDecayTimer = 0;
            IsJumpingAsGod = false;
        }

        // --- Manejo de Selección de Clase ---
        private void HandleClaseSeleccion()
        {
            var ks = Main.keyState;
            var oldKs = Main.oldKeyState; // Necesitamos el estado anterior

            // --- Comprobar si YA TIENE CLASE ---
            if (claseElegida != WakfuClase.Ninguna)
            {
                // --- SI YA ES TYMADOR y F4 se acaba de presionar ---
                if (claseElegida == WakfuClase.Tymador && ks.IsKeyDown(Keys.F4) && oldKs.IsKeyUp(Keys.F4)) // <-- AÑADIR && oldKs.IsKeyUp(Keys.F4)
                {
                    // Ciclar entre equipos: None -> Red -> Blue -> None ...
                    FootballTeam nextTeam;
                    switch (currentFootballTeam)
                    {
                        case FootballTeam.None: nextTeam = FootballTeam.Red; break;
                        case FootballTeam.Red: nextTeam = FootballTeam.Blue; break;
                        case FootballTeam.Blue: default: nextTeam = FootballTeam.None; break;
                    }
                    FootballSystem.SetPlayerTeam(Player.whoAmI, nextTeam);
                    currentFootballTeam = nextTeam;
                }
                // Si ya tiene clase (sea Tymador o no), no hacer nada más de selección
                return;
            }


            WakfuClase claseSeleccionada = WakfuClase.Ninguna;
            string mensaje = "";
            Color colorMensaje = Color.White;
            Action accionExtra = null;

            if (ks.IsKeyDown(Keys.F1))
            {
                claseSeleccionada = WakfuClase.Selatrop;
                mensaje = "¡You are Selatrop!\nWakmeha weapon and Yugo Skin sent to your inventory\nSkill 1: Place a Portal at your cursor\nSkill 2: Detonate Portals\nYou and your projectiles can pass throu portals";
                colorMensaje = Color.Teal;
                accionExtra = () =>
                {
                    Player.QuickSpawnItem(Player.GetSource_Misc("ClaseSelatrop"), ModContent.ItemType<WakmehamehaWeapon>());
                    Player.QuickSpawnItem(Player.GetSource_Misc("ClaseSelatrop"), ModContent.ItemType<Content.Items.Armor.Vanity.Yugo.YugoHead>());
                    Player.QuickSpawnItem(Player.GetSource_Misc("ClaseSelatrop"), ModContent.ItemType<Content.Items.Armor.Vanity.Yugo.YugoBody>()); // Asegúrate del nombre de clase correcto
                    Player.QuickSpawnItem(Player.GetSource_Misc("ClaseSelatrop"), ModContent.ItemType<Content.Items.Armor.Vanity.Yugo.YugoLegs>());
                };
            }
            else if (ks.IsKeyDown(Keys.F2))
            {
                claseSeleccionada = WakfuClase.Yopuka;
                mensaje = "¡You are Yopuka!\nIop Rage Sword and Tristepin Skin sent to your inventory\nSkill 1: God's Punch falls from the sky\nSkill 2: Jump + Stomp";
                colorMensaje = Color.Red;
                accionExtra = () =>
                {
                    Player.QuickSpawnItem(Player.GetSource_Misc("ClaseYopuka"), ModContent.ItemType<YopukaShockwaveSword>());
                    Player.QuickSpawnItem(Player.GetSource_Misc("ClaseSelatrop"), ModContent.ItemType<Content.Items.Armor.Vanity.Pinpan.PinpanHead>());
                    Player.QuickSpawnItem(Player.GetSource_Misc("ClaseSelatrop"), ModContent.ItemType<Content.Items.Armor.Vanity.Pinpan.PinpanBody>()); // Asegúrate del nombre de clase correcto
                    Player.QuickSpawnItem(Player.GetSource_Misc("ClaseSelatrop"), ModContent.ItemType<Content.Items.Armor.Vanity.Pinpan.PinpanLegs>());
                };
            }
            else if (ks.IsKeyDown(Keys.F3))
            {
                claseSeleccionada = WakfuClase.Steamer;
                mensaje = "¡You are Steamer!\nStasis Gun sent to your inventory\nSkill 1: Place a Stasis Turret, if already placed: Concentrated Laser\nSkill 2: Detonate Turret for replacement";
                colorMensaje = Color.SkyBlue;
                accionExtra = () => Player.QuickSpawnItem(Player.GetSource_Misc("ClaseSteamer"), ModContent.ItemType<SteamerPistol>());
            }
            else // --- Lógica específica para F4 ---
    if (ks.IsKeyDown(Keys.F4)) // Detectar JustPressed para F4
            {
                // --- CASO 1: Aún no tiene clase ---
                if (claseElegida == WakfuClase.Ninguna)
                {
                    claseElegida = WakfuClase.Tymador;
                    string mensajeInicial = "¡You are Rogue/Tymador!\nKick-u! weapon sent to your inventory\nSkill 1: Place Bomb / Swap\nSkill 2: Detonate Bombs\nBombs link and combo!\nPress F4 again to join a Gobbowl team!"; // Mensaje actualizado
                    colorMensaje = Color.DarkGray;
                    Main.NewText(mensajeInicial, colorMensaje);
                    haMostradoMensajeClase = true; // Marcar que ya eligió
                    TymadorAbilityHandler.ResetBombSystem(); // Acción específica del Tymador
                    Player.QuickSpawnItem(Player.GetSource_Misc("ClaseTymador"), ModContent.ItemType<TymadorKick>()); // Dar el arma de patada

                    // Sincronizar claseElegida (Necesitarás un paquete para esto si no lo tienes ya)
                    // SendClasePacket(claseElegida); // Ejemplo de función de envío
                }

                // Si no es Ninguna ni Tymador, no debería llegar aquí por la condición inicial del método
            }

            else if (ks.IsKeyDown(Keys.F5))
            {
                claseSeleccionada = WakfuClase.Zurcarac;
                mensaje = "¡You are Ecaflip/Zurcarák!\nLucky Dice weapon sent to your inventory.\nSkill 1: Summon Ecaflip's Kitten / Order Kitten to attack.\nSkill 2: Roll the Ecaflip Die for a random effect!\nPassive: All your damage is randomized (-20% to +25%).";
                colorMensaje = Color.Gold;
                // Acción extra: Dar el arma inicial y el buff del minion por primera vez?
                accionExtra = () =>
                {
                    // Player.QuickSpawnItem(Player.GetSource_Misc("ClaseZurcarac"), ModContent.ItemType<ZurcarakStarterWeapon>()); // Reemplaza con tu arma
                };
            }
            else if (ks.IsKeyDown(Keys.F6))
            {
                claseSeleccionada = WakfuClase.Xelor;
                mensaje = "¡You are Xelor!\nMaster of Time.\nSkill 1 (V): Teleport to cursor (6s CD).\nSkill 2 (X): Time Suspension (10s) / Rewind (20s CD).\nSlows enemies and projectiles, then rewinds them!";
                colorMensaje = Color.Purple;
            }

            if (claseSeleccionada != WakfuClase.Ninguna)
            {
                claseElegida = claseSeleccionada;
                Main.NewText(mensaje, colorMensaje);
                haMostradoMensajeClase = true;
                accionExtra?.Invoke();
                // TODO: Sincronizar claseElegida
            }
        }

        // --- Lógica de Rage ---
        public void TryGainRageFromProj(Projectile proj) // Renombrado y 'proj' ya no es opcional
        {
            // Condiciones: Clase Yopuka, Cooldown Listo, Proyectil no nulo y tipo Melee
            if (claseElegida != WakfuClase.Yopuka || rageCooldown > 0 || proj == null || proj.DamageType != DamageClass.Melee)
            {
                return; // No cumple condiciones para ganar rabia DESDE PROYECTIL
            }

            // Ganar Rabia
            GainRageInternal(); // Llama a función interna para evitar duplicar código
        }

        // --- NUEVO MÉTODO para Golpes Directos de Items ---
        public void TryGainRageFromItemHit()
        {
            // Condiciones: Clase Yopuka, Cooldown Listo
            // No necesitamos comprobar DamageType aquí porque WakfuGlobalItem ya lo hizo.
            if (claseElegida != WakfuClase.Yopuka || rageCooldown > 0)
            {
                return; // No cumple condiciones para ganar rabia DESDE ITEM
            }

            // Ganar Rabia
            GainRageInternal(); // Llama a función interna
        }

        // --- NUEVO Método Interno para la Lógica Común de Ganar Rabia ---
        public void GainRageInternal()
        {
            if (rageTicks < 5)
            {
                rageTicks++;
                // Main.NewText($"Rage Gained! Ticks: {rageTicks}"); // DEBUG
            }
            rageDecayTimer = 0; // Resetea decaimiento
            rageCooldown = 60;  // Pone en cooldown (1 segundo)
                                // TODO: Sincronizar rageTicks si es necesario
        }

        // --- Hook OnHitNPCWithProj (Llama al método de proyectil) ---
        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Llama a la función específica para proyectiles
            TryGainRageFromProj(proj); // <-- Llama a la función renombrada/específica

            // Podrías añadir lógica adicional aquí si fuera necesario
        }


        // --- ModifyHitNPCWithProj (Aplica Multiplicador de Rabia a Proyectiles) ---
        public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
        {
            // Lógica Zurcarák
            if (claseElegida == WakfuClase.Zurcarac && proj.owner == Player.whoAmI)
            {
                ApplyZurcarakDamageRoll(ref modifiers);
            }
            // Lógica Yopuka
            else if (claseElegida == WakfuClase.Yopuka && rageTicks > 0)
            {
                // Aplicar a SourceDamage (recomendado)
                modifiers.SourceDamage *= GetRageMultiplier();
                // O a FinalDamage:
                // modifiers.FinalDamage *= GetRageMultiplier();
            }
        }

        // Función helper para aplicar el roll de daño
        private void ApplyZurcarakDamageRoll(ref NPC.HitModifiers modifiers)
        {
            // Genera un multiplicador aleatorio entre 0.80 (-20%) y 1.25 (+25%)
            float randomMultiplier = Main.rand.NextFloat(0.5f, 1.5f);
            // Aplica el multiplicador al daño base (antes de defensa y críticos)
            modifiers.SourceDamage *= randomMultiplier;
        }
        public void ConsumeRage()
        {
            if (rageTicks > 0)
            {
                rageTicks = 0;
                rageDecayTimer = 0;
                // TODO: Sincronizar rageTicks
            }
        }

        // --- Guardar/Cargar Datos (Revisado) ---
        public override void SaveData(TagCompound tag)
        {
            tag["wakfuClase"] = (int)claseElegida;
            tag["yopukaRage"] = rageTicks;
            tag["yopukaEspadaCD"] = espadaCooldown;
            tag["steamerTorretaCD"] = steamerTorretaCooldown;
            tag["steamerGranadaCD"] = steamerGranadaCooldown;
            tag["xelorTeleportCD"] = xelorTeleportCooldown;
            tag["BalanceMode"] = BalanceMode;
        }
        public override void LoadData(TagCompound tag)
        {
            if (tag.ContainsKey("wakfuClase")) claseElegida = (WakfuClase)tag.Get<int>("wakfuClase");
            else claseElegida = WakfuClase.Ninguna;

            if (tag.ContainsKey("yopukaRage")) rageTicks = tag.Get<int>("yopukaRage");
            if (tag.ContainsKey("yopukaEspadaCD")) espadaCooldown = tag.Get<int>("yopukaEspadaCD");
            if (tag.ContainsKey("steamerTorretaCD")) steamerTorretaCooldown = tag.Get<int>("steamerTorretaCD");
            if (tag.ContainsKey("steamerGranadaCD")) steamerGranadaCooldown = tag.Get<int>("steamerGranadaCD");
            if (tag.ContainsKey("xelorTeleportCD")) xelorTeleportCooldown = tag.Get<int>("xelorTeleportCD");
            if (tag.ContainsKey("BalanceMode")) BalanceMode = tag.Get<bool>("BalanceMode");

            IsJumpingAsGod = false;
            haMostradoMensajeClase = claseElegida != WakfuClase.Ninguna;
        }

        public void ToggleBalanceMode()
        {
            BalanceMode = !BalanceMode;
            string status = BalanceMode ? "ON (Green)" : "OFF (Red)";
            Color color = BalanceMode ? Color.Green : Color.Red;
            Main.NewText($"Wakfu Balance Mode: {status}", color);
        }

        // --- Desconexión ---
        public override void PlayerDisconnect()
        {
            IsJumpingAsGod = false;
        }

        // --- Invisibilidad ---
        public override void HideDrawLayers(PlayerDrawSet drawInfo)
        {
            bool hidePlayer = IsJumpingAsGod || HidePlayerForKick || IsRollingDie; // Ocultar jugador si está saltando
            bool hideItem = HideHeldYopukaSword; // Ocultar item si el flag está activo

            if (hidePlayer || hideItem) // Si necesitamos ocultar algo
            {
                foreach (var layer in PlayerDrawLayerLoader.Layers)
                {
                    // Ocultar todas las capas del jugador si hidePlayer es true
                    if (hidePlayer)
                    {
                        // Excepciones? Si quieres que algo SÍ se vea durante el salto, añádelo aquí
                        // if (layer == PlayerDrawLayers.HeldItem) continue; // Ejemplo: No ocultar item sostenido
                        layer.Hide(); // Oculta la capa para este drawInfo
                    }
                    // Ocultar ESPECÍFICAMENTE la capa del item sostenido si hideItem es true y hidePlayer es false
                    else if (hideItem && layer == PlayerDrawLayers.HeldItem) // Comprobar si es la capa del item
                    {
                        layer.Hide(); // Ocultar solo el item
                    }
                }
            }
        }
        // public class VisualControlPlayer : ModPlayer // O WakfuPlayer : ModPlayer
        // {
        //     public bool HideHeldItemForGlow = false;

        //     public override void ResetEffects()
        //     {
        //         HideHeldItemForGlow = false;

        //         // Resetear otros flags si están aquí (IsJumpingAsGod)
        //     }


        // } // Fin de la clase WakfuPlayer
    }
}