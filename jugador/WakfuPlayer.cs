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
    public enum WakfuClase { Ninguna, Selatrop, Yopuka, Steamer, Tymador, Zurcarac, Xelor, Hipermago, Ocra, Uginak, Aniripsa, Sram, Sacrogrito, Feca }

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
        
        // --- NUEVO: Sistema de Presets ---
        private int currentPreset = 1; // 1 o 2

        // --- NUEVO: Variables del Sacrogrito ---
        public int sacrogritoAbility1Cooldown = 0;
        public const int SacrogritoAbility1BaseCooldown = 180; // 3 segundos
        public int sacrogritoAbility2Cooldown = 0;
        public const int SacrogritoAbility2BaseCooldown = 600; // 10 segundos
        public int sacrierExtraMaxLife = 0;

        // --- NUEVO: Variables del Feca ---
        public int fecaAbility1Cooldown = 0;
        public const int FecaAbility1BaseCooldown = 180; // 3 segundos
        public int fecaAbility2Cooldown = 0;
        public const int FecaAbility2BaseCooldown = 7200; // 2 minutos (120 * 60)
        public int fecaShieldHP = 0;
        public int fecaShieldMaxHP = 0; // For UI or Limits
        public int fecaShieldDuration = 0; // Timer in ticks
        public int fecaLastShieldTarget = -1; // WhoAmI of the last shielded player (Only relevant for the Feca caster)

        // --- NUEVO: Variables del Sram ---
        public int sramInvisibilityCooldown = 0;
        public const int SramInvisibilityBaseCooldown = 1200; // 20 segundos
        public bool sramInvisibilityActive = false;
        public bool sramFirstAttackMultiplier = false;
        public int sramAbility1Cooldown = 0;
        public const int SramAbility1BaseCooldown = 300; // 5 segundos

        // --- NUEVO: Variables del Xelor ---
        public int xelorTeleportCooldown = 0;
        public const int XelorTeleportBaseCooldown = 360; // 6 segundos
        
        public bool xelorTimeSuspensionActive = false;
        public int xelorTimeSuspensionTimer = 0;
        public int xelorAbility2Cooldown = 0;
        public const int XelorAbility2BaseCooldown = 1200; // 20 segundos
        public const int XelorTimeSuspensionDuration = 600; // 10 segundos

        // --- NUEVO: Variables del Hipermago ---
        public int hipermagoAbility1Cooldown = 0;
        public const int HipermagoAbility1BaseCooldown = 900; // 15 segundos
        public int hipermagoAbility2Cooldown = 0;
        public const int HipermagoAbility2BaseCooldown = 1200; // 20 segundos
        
        // CD para combo elemental (independiente de la spear)
        public int hipermagoElementalComboCooldown = 0;
        public const int HipermagoElementalComboCooldown = 300; // 5 segundos
        
        // CDs de las armas elementales (Fuego/Tierra y Aire/Agua)
        public int hipermagoFireCooldown = 0;
        public const int HipermagoFireBaseCooldown = 180; // 3 segundos
        public int hipermagoEarthCooldown = 0;
        public const int HipermagoEarthBaseCooldown = 90; // 1.5 segundos
        public int hipermagoAirCooldown = 0;
        public const int HipermagoAirBaseCooldown = 60; // 1 segundo
        public int hipermagoWaterCooldown = 0;
        public const int HipermagoWaterBaseCooldown = 120; // 2 segundos
        
        // Sistema para disparar la segunda bola con delay
        public int hipermagoSecondBallTimer = 0; // Timer para la segunda bola
        public bool hipermagoSecondBallPending = false;
        public Vector2 hipermagoSecondBallDirection = Vector2.Zero;

        // --- NUEVO: Variables del Ocra ---
        public int ocraAbility1Cooldown = 0;
        public const int OcraAbility1BaseCooldown = 300; // 5 segundos para la Baliza
        public int ocraAbility2Cooldown = 0;
        public const int OcraAbility2BaseCooldown = 120; // 2 segundos para la Flecha
        
        // --- NUEVO: Variables del Uginak ---
        public int uginakAbility1Cooldown = 0;
        public const int UginakAbility1BaseCooldown = 360; // 6 segundos
        public int uginakAbility2Cooldown = 0;
        public const int UginakAbility2BaseCooldown = 600; // 10 segundos (placeholder)
        
        // Sistema de Marca del Cazador
        public int uginakMarkedNPC = -1; // WhoAmI del NPC marcado (-1 = ninguno)
        public int uginakExtraLife = 0; // Vida extra del tanque de vida
        public int uginakMaxExtraLife = 0; // Máximo de vida extra actual
        
        // --- Sistema de Runas Elementales ---
        // Las runas se acumulan al atacar con las armas elementales (máximo 2 EN TOTAL)
        // Arma 1: Fuego (clic izq) / Tierra (clic der)
        // Arma 2: Aire (clic izq) / Agua (clic der)
        public int hipermagoFireRunes = 0;     // Runas de fuego
        public int hipermagoEarthRunes = 0;    // Runas de tierra
        public int hipermagoAirRunes = 0;      // Runas de aire
        public int hipermagoWaterRunes = 0;    // Runas de agua
        public const int MAX_TOTAL_RUNES = 2;  // Máximo de runas EN TOTAL
        
        // Cuando tiene 2 runas, la habilidad 2 cambia de comportamiento
        public int GetTotalRunes() => hipermagoFireRunes + hipermagoEarthRunes + hipermagoAirRunes + hipermagoWaterRunes;
        public bool HasRuneCombo() => GetTotalRunes() >= 2;
        
        // Método para añadir una runa (llamado desde las armas)
        public void AddRune(string element)
        {
            // Solo añadir si no hemos llegado al máximo total
            if (GetTotalRunes() >= MAX_TOTAL_RUNES) return;
            
            switch (element.ToLower())
            {
                case "fire": hipermagoFireRunes++; break;
                case "earth": hipermagoEarthRunes++; break;
                case "air": hipermagoAirRunes++; break;
                case "water": hipermagoWaterRunes++; break;
            }
            // TODO: Sincronizar runas en multiplayer
        }
        
        // Método para consumir runas y resetear CDs (para combo Fuego+Tierra)
        public void ConsumeRunesForCombo()
        {
            hipermagoFireRunes = 0;
            hipermagoEarthRunes = 0;
            hipermagoAirRunes = 0;
            hipermagoWaterRunes = 0;
            hipermagoAbility1Cooldown = 0; // Resetear CD de las bolas de luz
            hipermagoAbility2Cooldown = 0; // Resetear CD de la spear
            // TODO: Sincronizar en multiplayer
        }

        // --- NUEVO: Variables del Zurcarac ---
        public bool zurcarakMinionActive = false; // ¿Está el gatito invocado?
        public int zurcarakAbility1Cooldown = 0; // Cooldown para el Arañazo Loco
        public const int ZurcarakAbility1BaseCooldown = 180; // 3 segundos
        public int zurcarakAbility2Cooldown = 0; // Cooldown para el Dado
        public const int ZurcarakAbility2BaseCooldown = 3600; //1200 20 segundos (ejemplo, ajustar)
        public bool IsRollingDie = false; // Flag para ocultar jugador durante la habilidad 2

        // --- NUEVO: Variables del Aniripsa ---
        public int aniripsaAbility1Cooldown = 0;
        public const int AniripsaAbility1BaseCooldown = 60; // 1 segundo (cooldown de casteo, efecto persistente)
        public int aniripsaAbility2Cooldown = 0;

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

            // --- Feca Shield Timer Logic ---
            if (fecaShieldHP > 0)
            {
                if (fecaShieldDuration > 0)
                {
                    fecaShieldDuration--;
                }
                else
                {
                    // Time's up!
                    fecaShieldHP = 0;
                    fecaShieldMaxHP = 0;
                    // TODO: Could send a sync packet here, but if duration is synced (it isn't yet, but starts at same value), 
                    // eventually it will expire locally.
                    // For precision in MP, the source of truth is spread out, but if duration is deterministic, it's fine.
                }
            }

            // --- Resetear estado del minion ---
            // El buff se encargará de mantenerlo activo si existe
            zurcarakMinionActive = false;
            
            // Resetear bonus de daño crítico
            critDamageBonus = 0f;

            // --- Resetear stacks de Ocra Precision ---
            if (!Player.HasBuff(ModContent.BuffType<PrecisionBuff>()))
            {
                precisionStacks = 0;
            }

            // --- Sram Invisibility Effects ---
            if (sramInvisibilityActive)
            {
                Player.aggro -= 10000; // Reduce aggro significantly
                Player.opacityForAnimation = 0.3f; // Make player transparent (0 is invisible, 1 is opaque)
            }
        }
        
        // Bonus de daño crítico (multiplicador adicional)
        public float critDamageBonus = 0f;

        // Ocra
        public int precisionStacks = 0;

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
                    Main.NewText("Press F1-F8 to choose your class. (Press F9 to switch PRESETS)\nF1-Selatrop-\nF2-Yopuka/Aniripsa-\nF3-Steamer-\nF4-Rogue/Sacrier-\nF5-Zurcarac-\nF6-Xelor-\nF7-Hipermago-\nF8-Ocra-", Color.OrangeRed);
                    haMostradoMensajeClase = true;
                }
                HandleClaseSeleccion(); // Manejar la selección si aún no tiene clase
            }

            // --- Actualizar Cooldowns y Estados (Para todos) ---
            if (espadaCooldown > 0) espadaCooldown--;
            if (steamerTorretaCooldown > 0) steamerTorretaCooldown--;
            if (steamerGranadaCooldown > 0) steamerGranadaCooldown--;
            if (sramInvisibilityCooldown > 0) sramInvisibilityCooldown--;
            if (sramAbility1Cooldown > 0) sramAbility1Cooldown--;
            if (rageCooldown > 0) rageCooldown--;
            if (zurcarakAbility1Cooldown > 0) zurcarakAbility1Cooldown--;
            if (zurcarakAbility2Cooldown > 0) zurcarakAbility2Cooldown--;
            if (portalPhysicsCooldown > 0) portalPhysicsCooldown--;
            if (xelorTeleportCooldown > 0) xelorTeleportCooldown--;
            if (xelorAbility2Cooldown > 0) xelorAbility2Cooldown--;
            if (hipermagoAbility1Cooldown > 0) hipermagoAbility1Cooldown--;
            if (hipermagoAbility2Cooldown > 0) hipermagoAbility2Cooldown--;
            if (hipermagoElementalComboCooldown > 0) hipermagoElementalComboCooldown--;
            if (hipermagoFireCooldown > 0) hipermagoFireCooldown--;
            if (hipermagoEarthCooldown > 0) hipermagoEarthCooldown--;
            if (hipermagoAirCooldown > 0) hipermagoAirCooldown--;
            if (hipermagoWaterCooldown > 0) hipermagoWaterCooldown--;
            if (ocraAbility1Cooldown > 0) ocraAbility1Cooldown--;
            if (ocraAbility2Cooldown > 0) ocraAbility2Cooldown--;
            if (uginakAbility1Cooldown > 0) uginakAbility1Cooldown--;
            if (uginakAbility2Cooldown > 0) uginakAbility2Cooldown--;
            if (aniripsaAbility1Cooldown > 0) aniripsaAbility1Cooldown--;
            if (aniripsaAbility2Cooldown > 0) aniripsaAbility2Cooldown--;
            if (sacrogritoAbility1Cooldown > 0) sacrogritoAbility1Cooldown--;
            if (sacrogritoAbility2Cooldown > 0) sacrogritoAbility2Cooldown--;
            if (fecaAbility1Cooldown > 0) fecaAbility1Cooldown--;
            if (fecaAbility2Cooldown > 0) fecaAbility2Cooldown--;

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

                        case WakfuClase.Hipermago:
                            // Habilidad 1 (V): Doble Bola de Energía de Luz
                            if (WakfuMod.Habilidad1Keybind.JustPressed && hipermagoAbility1Cooldown <= 0 && !hipermagoSecondBallPending)
                            {
                                Vector2 direction = Main.MouseWorld - Player.Center;
                                direction.Normalize();
                                float speed = 12f;
                                int baseDamage = 50; // Daño base de cada bola
                                float knockback = 3f;
                                
                                // Disparo de la primera bola
                                int projType = ModContent.ProjectileType<Content.Projectiles.HipermagoLightBall>();
                                
                                // Posición elevada para evitar colisión con el suelo
                                Vector2 spawnPos = Player.Center - new Vector2(0, 30);
                                
                                // Primera bola (ai[0] = 0)
                                Projectile.NewProjectile(
                                    Player.GetSource_FromThis(),
                                    spawnPos,
                                    direction * speed,
                                    projType,
                                    baseDamage,
                                    knockback,
                                    Player.whoAmI,
                                    0f, // ai[0] = 0 -> Primera bola
                                    Player.whoAmI // ai[1] = ID del jugador
                                );
                                
                                // Programar segunda bola para 0.7 segundos después (42 ticks)
                                hipermagoSecondBallPending = true;
                                hipermagoSecondBallTimer = 42; // 0.7 segundos
                                hipermagoSecondBallDirection = direction;
                                
                                // Efectos
                                SoundEngine.PlaySound(SoundID.Item72, Player.Center);
                                for (int i = 0; i < 10; i++)
                                {
                                    Dust.NewDust(Player.Center, 0, 0, DustID.GoldFlame, 
                                        Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-2f, 2f), 100, default, 1.2f);
                                }
                                
                                // Cooldown
                                hipermagoAbility1Cooldown = HipermagoAbility1BaseCooldown;
                            }
                            
                            // Disparar segunda bola cuando el timer llegue a 0
                            if (hipermagoSecondBallPending)
                            {
                                hipermagoSecondBallTimer--;
                                if (hipermagoSecondBallTimer <= 0)
                                {
                                    hipermagoSecondBallPending = false;
                                    
                                    float speed = 12f;
                                    int baseDamage = 50;
                                    float knockback = 3f;
                                    int projType = ModContent.ProjectileType<Content.Projectiles.HipermagoLightBall>();
                                    
                                    // Posición elevada para evitar colisión con el suelo
                                    Vector2 spawnPos = Player.Center - new Vector2(0, 30);
                                    
                                    // Segunda bola (ai[0] = 1)
                                    Projectile.NewProjectile(
                                        Player.GetSource_FromThis(),
                                        spawnPos,
                                        hipermagoSecondBallDirection * speed,
                                        projType,
                                        baseDamage,
                                        knockback,
                                        Player.whoAmI,
                                        1f, // ai[0] = 1 -> Segunda bola
                                        Player.whoAmI
                                    );
                                    
                                    // Efectos de la segunda bola
                                    SoundEngine.PlaySound(SoundID.Item72, Player.Center);
                                    for (int i = 0; i < 10; i++)
                                    {
                                        Dust.NewDust(Player.Center, 0, 0, DustID.GoldFlame, 
                                            Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-2f, 2f), 100, default, 1.2f);
                                    }
                                }
                            }
                            
                            // Habilidad 2 (X): Holy Spear o Combo Elemental
                            if (WakfuMod.Habilidad2Keybind.JustPressed)
                            {
                                if (HasRuneCombo() && hipermagoElementalComboCooldown <= 0)
                                {
                                    // --- TIENE 2 RUNAS Y COMBO DISPONIBLE ---
                                    
                                    Vector2 targetPos = Main.MouseWorld;
                                    
                                    if (hipermagoFireRunes >= 1 && hipermagoEarthRunes >= 1)
                                    {
                                        // COMBO FUEGO+TIERRA: Lluvia de Meteoritos
                                        int meteorDamage = 40;
                                        float knockback = 5f;
                                        int projType = ModContent.ProjectileType<Content.Projectiles.HipermagoMeteor>();
                                        
                                        // Disparar 5 meteoritos desde el cielo - MUY DISPERSOS
                                        for (int i = 0; i < 5; i++)
                                        {
                                            float offsetX = Main.rand.NextFloat(-400f, 400f);
                                            float offsetY = Main.rand.NextFloat(-100f, 100f);
                                            Vector2 spawnPos = targetPos + new Vector2(offsetX, -700f - offsetY);
                                            
                                            Vector2 direction = new Vector2(Main.rand.NextFloat(-0.3f, 0.3f), 1f);
                                            direction.Normalize();
                                            float speed = 5f + Main.rand.NextFloat(-1f, 1f);
                                            
                                            Projectile.NewProjectile(
                                                Player.GetSource_FromThis(),
                                                spawnPos,
                                                direction * speed,
                                                projType,
                                                meteorDamage,
                                                knockback,
                                                Player.whoAmI
                                            );
                                        }
                                        
                                        SoundEngine.PlaySound(SoundID.Item45, Player.Center);
                                    }
                                    else if (hipermagoFireRunes >= 1 && hipermagoAirRunes >= 1)
                                    {
                                        // COMBO FUEGO+AIRE: Tornado de Fuego
                                        int projType = ModContent.ProjectileType<Content.Projectiles.HipermagoFireTornado>();
                                        
                                        // Encontrar el bloque más cercano al cursor (buscar hacia abajo)
                                        Vector2 cursor = targetPos;
                                        Vector2 spawnPos = cursor;
                                        int tornadoHeight = 240; // Altura del tornado
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
                                                    // El suelo está en coords.Y * 16
                                                    // El proyectil se crea con Center en spawnPos
                                                    // Para que la BASE esté en el suelo: Center.Y = suelo - (altura/2)
                                                    float groundY = coords.Y * 16f;
                                                    spawnPos = new Vector2(cursor.X, groundY - (tornadoHeight / 2f));
                                                    foundGround = true;
                                                    break;
                                                }
                                            }
                                        }
                                        
                                        if (foundGround)
                                        {
                                            Projectile.NewProjectile(
                                                Player.GetSource_FromThis(),
                                                spawnPos,
                                                Vector2.Zero,
                                                projType,
                                                10, // 10 de daño por tick
                                                0f, // Sin knockback
                                                Player.whoAmI
                                            );
                                            
                                            SoundEngine.PlaySound(SoundID.Item74, spawnPos);
                                        }
                                    }
                                    else if (hipermagoFireRunes >= 1 && hipermagoWaterRunes >= 1)
                                    {
                                        // COMBO FUEGO+AGUA: Explosiones de Vapor (ceguera)
                                        int projType = ModContent.ProjectileType<Content.Projectiles.HipermagoSteamExplosion>();
                                        
                                        // 5 explosiones erráticas alrededor del cursor
                                        for (int i = 0; i < 5; i++)
                                        {
                                            Vector2 offset = Main.rand.NextVector2Circular(80f, 80f);
                                            Vector2 explosionPos = targetPos + offset;
                                            
                                            // Delay aleatorio para cada explosión (usando ai[0])
                                            int delay = i * 8 + Main.rand.Next(0, 5);
                                            
                                            Projectile.NewProjectile(
                                                Player.GetSource_FromThis(),
                                                explosionPos,
                                                Vector2.Zero,
                                                projType,
                                                30, 
                                                4f, 
                                                Player.whoAmI, 
                                                delay
                                            );
                                        }
                                        
                                        SoundEngine.PlaySound(SoundID.Item13, targetPos);
                                    }
                                    else if (hipermagoAirRunes >= 1 && hipermagoEarthRunes >= 1)
                                    {
                                        // COMBO AIRE+TIERRA: Remolino de Escombros
                                        int projType = ModContent.ProjectileType<Content.Projectiles.HipermagoDebrisWhirl>();
                                        int damage = 10; // 10 de daño cada medio segundo
                                        
                                        // Crear 4 rocas orbitando alrededor del jugador
                                        for (int i = 0; i < 4; i++)
                                        {
                                            float angleOffset = (MathHelper.TwoPi / 4f) * i; // Distribuir en círculo
                                            
                                            Projectile.NewProjectile(
                                                Player.GetSource_FromThis(),
                                                Player.Center,
                                                Vector2.Zero,
                                                projType,
                                                damage,
                                                3f,
                                                Player.whoAmI,
                                                angleOffset // ai[0] = ángulo inicial
                                            );
                                        }
                                        
                                        SoundEngine.PlaySound(SoundID.Item66, Player.Center); // Sonido de viento
                                    }
                                    else if (hipermagoFireRunes >= 2)
                                    {
                                        // COMBO 2x FUEGO: Explosión MEGA (triple tamaño, triple daño)
                                        int projType = ModContent.ProjectileType<Content.Projectiles.HipermagoFireExplosion>();
                                        int megaDamage = 60; // Triple de 20
                                        
                                        Projectile proj = Main.projectile[Projectile.NewProjectile(
                                            Player.GetSource_FromThis(),
                                            targetPos,
                                            Vector2.Zero,
                                            projType,
                                            megaDamage,
                                            3f,
                                            Player.whoAmI,
                                            3f // ai[0] = scale factor (3x para mega explosión)
                                        )];
                                        
                                        SoundEngine.PlaySound(SoundID.Item74, targetPos); // Sonido épico
                                    }
                                    else if (hipermagoEarthRunes >= 2)
                                    {
                                        // COMBO 2x TIERRA: Roca MEGA (triple tamaño, doble daño)
                                        int projType = ModContent.ProjectileType<Content.Projectiles.HipermagoEarthRock>();
                                        int megaDamage = 100; // Doble de 50
                                        
                                        Vector2 spawnPos = new Vector2(targetPos.X, targetPos.Y - 600);
                                        Vector2 fallVelocity = new Vector2(0, 12f);
                                        
                                        Projectile proj = Main.projectile[Projectile.NewProjectile(
                                            Player.GetSource_FromThis(),
                                            spawnPos,
                                            fallVelocity,
                                            projType,
                                            megaDamage,
                                            8f,
                                            Player.whoAmI
                                        )];
                                        proj.scale = 3f; // Triple de tamaño
                                        
                                        SoundEngine.PlaySound(SoundID.Item14, Player.Center);
                                    }
                                    else if (hipermagoWaterRunes >= 2)
                                    {
                                        // COMBO 2x AGUA: Burbuja que atrapa enemigos
                                        int projType = ModContent.ProjectileType<Content.Projectiles.HipermagoBubble>();
                                        
                                        Projectile.NewProjectile(
                                            Player.GetSource_FromThis(),
                                            targetPos,
                                            Vector2.Zero,
                                            projType,
                                            1, // 1 de daño por tick
                                            0f, // Sin knockback
                                            Player.whoAmI
                                        );
                                        
                                        SoundEngine.PlaySound(SoundID.Item85, targetPos);
                                    }
                                    else if (hipermagoAirRunes >= 1 && hipermagoWaterRunes >= 1)
                                    {
                                        // COMBO AIRE+AGUA: Buff de velocidad, ataque, daño, vuelo, regen + curación
                                        Player.AddBuff(ModContent.BuffType<Content.Buffs.AirWaterBuff>(), 600); // 10 segundos
                                        
                                        // Curar 30 de vida
                                        Player.Heal(30);
                                        
                                        // Efecto visual de agua y viento
                                        for (int i = 0; i < 20; i++)
                                        {
                                            int dustType = Main.rand.NextBool() ? DustID.Cloud : DustID.Water;
                                            Vector2 velocity = Main.rand.NextVector2Circular(5f, 5f);
                                            int dust = Dust.NewDust(Player.position, Player.width, Player.height, 
                                                dustType, velocity.X, velocity.Y - 2f, 100, default, 1.2f);
                                            Main.dust[dust].noGravity = true;
                                        }
                                        
                                        SoundEngine.PlaySound(SoundID.Item66, Player.Center);
                                    }
                                    else if (hipermagoEarthRunes >= 1 && hipermagoWaterRunes >= 1)
                                    {
                                        // COMBO TIERRA+AGUA: Buff de defensa, anti-kb, anti-lava, regen + curación
                                        Player.AddBuff(ModContent.BuffType<Content.Buffs.EarthWaterBuff>(), 600); // 10 segundos
                                        
                                        // Curar 50 de vida
                                        Player.Heal(50);
                                        
                                        // Efecto visual de tierra y agua (barro)
                                        for (int i = 0; i < 20; i++)
                                        {
                                            int dustType = Main.rand.NextBool() ? DustID.Dirt : DustID.Water;
                                            Vector2 velocity = Main.rand.NextVector2Circular(4f, 4f);
                                            int dust = Dust.NewDust(Player.position, Player.width, Player.height, 
                                                dustType, velocity.X, velocity.Y, 100, default, 1f);
                                            Main.dust[dust].noGravity = false;
                                        }
                                        
                                        SoundEngine.PlaySound(SoundID.Item13, Player.Center);
                                    }
                                    else if (hipermagoAirRunes >= 2)
                                    {
                                        // COMBO 2x AIRE: Tornado de Viento (lanza enemigos hacia arriba)
                                        int projType = ModContent.ProjectileType<Content.Projectiles.HipermagoWindTornado>();
                                        
                                        // Encontrar el bloque más cercano al cursor (buscar hacia abajo)
                                        Vector2 cursor = targetPos;
                                        Vector2 spawnPos = cursor;
                                        int tornadoHeight = 240; // Altura del tornado
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
                                                    // El suelo está en coords.Y * 16
                                                    // El proyectil se crea con Center en spawnPos
                                                    // Para que la BASE esté en el suelo: Center.Y = suelo - (altura/2)
                                                    float groundY = coords.Y * 16f;
                                                    spawnPos = new Vector2(cursor.X, groundY - (tornadoHeight / 2f));
                                                    foundGround = true;
                                                    break;
                                                }
                                            }
                                        }
                                        
                                        if (foundGround)
                                        {
                                            Projectile.NewProjectile(
                                                Player.GetSource_FromThis(),
                                                spawnPos,
                                                Vector2.Zero,
                                                projType,
                                                50, // 50 de daño
                                                15f, // Alto knockback vertical
                                                Player.whoAmI
                                            );
                                            
                                            SoundEngine.PlaySound(SoundID.Item66, spawnPos);
                                        }
                                    }
                                    
                                    // Consumir runas y resetear CDs
                                    ConsumeRunesForCombo();
                                    
                                    // Poner CD del combo elemental
                                    hipermagoElementalComboCooldown = HipermagoElementalComboCooldown;
                                }
                                else if (!HasRuneCombo() && hipermagoAbility2Cooldown <= 0)
                                {
                                    // --- NO TIENE RUNAS: Dispara Holy Spear ---
                                    Vector2 direction = Main.MouseWorld - Player.Center;
                                    direction.Normalize();
                                    float speed = 15f;
                                    int baseDamage = 50;
                                    float knockback = 4f;
                                    
                                    int projType = ModContent.ProjectileType<Content.Projectiles.HipermagoHolySpear>();
                                    
                                    Projectile.NewProjectile(
                                        Player.GetSource_FromThis(),
                                        Player.Center,
                                        direction * speed,
                                        projType,
                                        baseDamage,
                                        knockback,
                                        Player.whoAmI
                                    );
                                    
                                    // Efectos
                                    SoundEngine.PlaySound(SoundID.Item117, Player.Center); // Sonido sagrado
                                    for (int i = 0; i < 20; i++)
                                    {
                                        Vector2 dustVel = direction.RotatedByRandom(0.3f) * Main.rand.NextFloat(2f, 5f);
                                        Dust.NewDust(Player.Center, 0, 0, DustID.GoldFlame, 
                                            dustVel.X, dustVel.Y, 100, default, 1.5f);
                                    }
                                    
                                    // Cooldown de 20 segundos
                                    hipermagoAbility2Cooldown = HipermagoAbility2BaseCooldown;
                                }
                            }
                            break;

                        case WakfuClase.Ocra:
                            // Habilidad 1 (V): Colocar Baliza
                            if (WakfuMod.Habilidad1Keybind.JustPressed && ocraAbility1Cooldown <= 0)
                            {
                                ocraAbility1Cooldown = OcraAbility1BaseCooldown;
                                
                                // Buscar suelo debajo del cursor
                                Vector2 constrictCursor = Main.MouseWorld;
                                Vector2 spawnPos = constrictCursor;
                                
                                // Lógica simple de encontrar suelo (Raycast hacia abajo)
                                Point tileCoords = constrictCursor.ToTileCoordinates();
                                for (int y = 0; y < 20; y++) // Buscar hasta 20 bloques abajo
                                {
                                    if (WorldGen.SolidTile(tileCoords.X, tileCoords.Y + y))
                                    {
                                        // Place ON TOP of the tile.
                                        // Tile top Y is (tileCoords.Y + y) * 16.
                                        // Projectile Center Y should be TileTop - (Height / 2)
                                        // Height is 86. Half is 43.
                                        spawnPos = new Vector2(constrictCursor.X, ((tileCoords.Y + y) * 16) - 43); 
                                        break;
                                    }
                                }
                                
                                // Spawn Baliza
                                Projectile.NewProjectile(
                                    Player.GetSource_FromThis("OcraBeacon"),
                                    spawnPos,
                                    Vector2.Zero,
                                    ModContent.ProjectileType<OcraBeacon>(),
                                    0, // Sin daño
                                    0f,
                                    Player.whoAmI
                                );
                            }
                            
                            // Habilidad 2 (X): Disparar Flecha Homing
                            if (WakfuMod.Habilidad2Keybind.Current && ocraAbility2Cooldown <= 0)
                            {
                                ocraAbility2Cooldown = OcraAbility2BaseCooldown;
                                
                                Vector2 mousePos = Main.MouseWorld;
                                Vector2 direction = (mousePos - Player.Center).SafeNormalize(Vector2.UnitX);
                                float speed = 12f;
                                
                                int damage = 40;
                                if (BalanceMode)
                                {
                                     damage = (int)(20f * Player.GetDamage(DamageClass.Ranged).Multiplicative);
                                }
                                else
                                {
                                    // Daño base + escalado
                                     damage = (int)Player.GetDamage(DamageClass.Ranged).ApplyTo(40);
                                }

                                Projectile.NewProjectile(
                                    Player.GetSource_FromThis("OcraArrow"),
                                    Player.Center,
                                    direction * speed,
                                    ModContent.ProjectileType<OcraArrow>(),
                                    damage,
                                    5f,
                                    Player.whoAmI
                                );
                                
                                SoundEngine.PlaySound(SoundID.Item5, Player.Center);
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


                        case WakfuClase.Uginak:
                            // Habilidad 1 (V): Marca del Cazador
                            if (WakfuMod.Habilidad1Keybind.JustPressed && uginakAbility1Cooldown <= 0)
                            {
                                Vector2 cursorPos = Main.MouseWorld;
                                float searchRadius = 800f; // 800 píxeles de búsqueda
                                int closestNPC = -1;
                                float closestDist = float.MaxValue;
                                
                                // Buscar enemigo más cercano al cursor
                                for (int i = 0; i < Main.maxNPCs; i++)
                                {
                                    NPC npc = Main.npc[i];
                                    if (npc.active && !npc.friendly && npc.lifeMax > 5 && !npc.dontTakeDamage)
                                    {
                                        float dist = Vector2.Distance(npc.Center, cursorPos);
                                        if (dist < searchRadius && dist < closestDist)
                                        {
                                            closestDist = dist;
                                            closestNPC = i;
                                        }
                                    }
                                }
                                
                                if (closestNPC != -1)
                                {
                                    // Desmarcar anterior
                                    if (uginakMarkedNPC != -1 && uginakMarkedNPC < Main.maxNPCs) {
                                        Main.npc[uginakMarkedNPC].GetGlobalNPC<Common.GlobalNPCs.WakfuGlobalNPC>().uginakMarked = false;
                                    }

                                    // Marcar enemigo
                                    uginakMarkedNPC = closestNPC;
                                    var globalNPC = Main.npc[closestNPC].GetGlobalNPC<Common.GlobalNPCs.WakfuGlobalNPC>();
                                    globalNPC.uginakMarked = true;
                                    globalNPC.uginakMarkedByPlayer = Player.whoAmI;
                                    
                                    // Efectos visuales y sonido
                                    SoundEngine.PlaySound(SoundID.Item37, Main.npc[closestNPC].Center);
                                    for (int i = 0; i < 20; i++)
                                    {
                                        Dust.NewDust(Main.npc[closestNPC].position, Main.npc[closestNPC].width, Main.npc[closestNPC].height,
                                            DustID.Torch, 0, 0, 100, Color.Orange, 1.5f);
                                    }
                                    
                                    Main.NewText("Target Marked!", Color.Orange);
                                    uginakAbility1Cooldown = UginakAbility1BaseCooldown;
                                }
                                else
                                {
                                    Main.NewText("No target found near cursor!", Color.Gray);
                                }
                            }
                            
                            // Mantener marca activa si el enemigo sigue vivo
                            if (uginakMarkedNPC >= 0 && uginakMarkedNPC < Main.maxNPCs)
                            {
                                NPC markedNPC = Main.npc[uginakMarkedNPC];
                                if (markedNPC.active && markedNPC.life > 0)
                                {
                                    var globalNPC = markedNPC.GetGlobalNPC<Common.GlobalNPCs.WakfuGlobalNPC>();
                                    globalNPC.uginakMarked = true;
                                    globalNPC.uginakMarkedByPlayer = Player.whoAmI;
                                }
                                else
                                {
                                    if (markedNPC.active) markedNPC.GetGlobalNPC<Common.GlobalNPCs.WakfuGlobalNPC>().uginakMarked = false;
                                    uginakMarkedNPC = -1; // El enemigo murió o se desactivó
                                }
                            }
                            
                            // Habilidad 2 (X): Invocar al Wuau / Ataque Comandado / Salto
                            if (WakfuMod.Habilidad2Keybind.JustPressed)
                            {
                                bool hasWuau = Player.HasBuff(ModContent.BuffType<Content.Buffs.UginakWuauBuff>());
                                
                                if (!hasWuau)
                                {
                                    // Convocar si no existe
                                    if (uginakAbility2Cooldown <= 0)
                                    {
                                        Player.AddBuff(ModContent.BuffType<Content.Buffs.UginakWuauBuff>(), 2);
                                        SoundEngine.PlaySound(SoundID.Item44, Player.Center);
                                        uginakAbility2Cooldown = 300; // 5s CD para invocar
                                    }
                                }
                                else if (uginakAbility2Cooldown <= 0)
                                {
                                    // Si ya existe, decidir entre Ataque o Salto
                                    if (uginakMarkedNPC != -1)
                                    {
                                        // ATAQUE COMANDADO
                                        Projectile wuau = FindPlayerMinion(Player, ModContent.ProjectileType<UginakWuauMinion>());
                                        if (wuau != null)
                                        {
                                            wuau.ai[1] = 1f; // Trigger de ataque
                                            wuau.netUpdate = true;
                                            uginakAbility2Cooldown = 900; // 15s CD
                                        }
                                    }
                                    else
                                    {
                                        // SALTO (LEAP)
                                        Projectile.NewProjectile(
                                            Player.GetSource_FromThis("UginakLeap"),
                                            Player.Center, Vector2.Zero,
                                            ModContent.ProjectileType<UginakLeapAbility>(),
                                            0, 0f, Player.whoAmI,
                                            ai1: (float)Player.direction
                                        );
                                        uginakAbility2Cooldown = 900; // 15s CD
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

                                case WakfuClase.Aniripsa:

                                    // Habilidad 1 (V): Marca Curativa / Maldita
                                    if (WakfuMod.Habilidad1Keybind.JustPressed && aniripsaAbility1Cooldown <= 0)
                                    {
                                        int targetID = -1;
                                        int targetType = -1; // 0 = Player, 1 = NPC
                                        
                                        // Buscar target bajo el mouse (Prioridad: Player > NPC)
                                        Vector2 mousePos = Main.MouseWorld;
                                        float checkRadius = 40f; 

                                        // 1. Chequear Jugadores (incluído uno mismo)
                                        for (int i = 0; i < Main.maxPlayers; i++)
                                        {
                                            Player p = Main.player[i];
                                            if (p.active && !p.dead && p.getRect().Intersects(Utils.CenteredRectangle(mousePos, new Vector2(20, 20))))
                                            {
                                                targetID = i;
                                                targetType = 0;
                                                break;
                                            }
                                        }
                                        
                                        // 2. Si no hay jugador, Chequear NPCs
                                        if (targetID == -1)
                                        {
                                            for (int i = 0; i < Main.maxNPCs; i++)
                                            {
                                                NPC n = Main.npc[i];
                                                if (n.active && n.life > 0 && n.getRect().Intersects(Utils.CenteredRectangle(mousePos, new Vector2(20, 20))))
                                                {
                                                    targetID = i;
                                                    targetType = 1;
                                                    break;
                                                }
                                            }
                                        }
                                        
                                        if (targetID != -1)
                                        {
                                            // Eliminar marks existentes de este Aniripsa
                                            for (int i = 0; i < Main.maxProjectiles; i++)
                                            {
                                                Projectile p = Main.projectile[i];
                                                if (p.active && p.owner == Player.whoAmI && p.type == ModContent.ProjectileType<Content.Projectiles.AniripsaMark>())
                                                {
                                                    p.Kill();
                                                }
                                            }
                                            
                                            // Crear nueva marca
                                            Projectile.NewProjectile(
                                                Player.GetSource_FromThis(),
                                                mousePos,
                                                Vector2.Zero,
                                                ModContent.ProjectileType<Content.Projectiles.AniripsaMark>(),
                                                0,
                                                0,
                                                Player.whoAmI,
                                                targetID, // ai[0]
                                                targetType // ai[1]
                                            );
                                            
                                            aniripsaAbility1Cooldown = AniripsaAbility1BaseCooldown;
                                            SoundEngine.PlaySound(SoundID.Item29, mousePos); // Sonido mágico suave
                                        }
                                        else
                                        {
                                            Main.NewText("Not target near cursor!", Color.Red);
                                        }
                                    }
                                    
                                    // Habilidad 2 (X): Explosión Reconstituyente
                                    // 20 Daño/Cura base + Scaling normal de magia?
                                    // User said: "Escala con el bono de magic damage y de entrada tiene 20 de efecto" -> Standard scaling implied.
                                    if (WakfuMod.Habilidad2Keybind.JustPressed && aniripsaAbility1Cooldown <= 0) // Share cooldown or separate? Usually separate.
                                    
                                    {
                                         
                                        if (aniripsaAbility2Cooldown <= 0)
                                        {
                                            Vector2 mousePos = Main.MouseWorld;
                                            
                                            // Spawn Explosion
                                            // Damage calculation: 20 * MagicMultiplier
                                            int baseDamage = 20;
                                            
                                            
                                            // Use ai[0] for Scale? User didn't ask for size scaling, but Hipermago has it. Default 1f.
                                            Projectile.NewProjectile(
                                                Player.GetSource_FromThis(),
                                                mousePos,
                                                Vector2.Zero,
                                                ModContent.ProjectileType<Content.Projectiles.AniripsaExplosion>(),
                                                0, // Damage handled in AI
                                                0, // Knockback
                                                Player.whoAmI,
                                                6.0f // Scale (6x default)
                                            );
                                            
                                            aniripsaAbility2Cooldown = 600; // 10 seconds
                                        }
                                    }

                                    break;

                                case WakfuClase.Sram:
                                    // Habilidad 1 (V): Shadow Step
                                    if (WakfuMod.Habilidad1Keybind.JustPressed && sramAbility1Cooldown <= 0)
                                    {
                                        // Find closest NPC to cursor
                                        NPC closestNPC = null;
                                        float minDistance = 600f; // Max range for teleport
                                        Vector2 mousePos = Main.MouseWorld;

                                        for (int i = 0; i < Main.maxNPCs; i++)
                                        {
                                            NPC npc = Main.npc[i];
                                            if (npc.active && !npc.friendly && npc.life > 0 && !npc.dontTakeDamage)
                                            {
                                                float distance = Vector2.Distance(mousePos, npc.Center);
                                                if (distance < minDistance)
                                                {
                                                    minDistance = distance;
                                                    closestNPC = npc;
                                                }
                                            }
                                        }

                                        if (closestNPC != null)
                                        {
                                            // Teleport behind NPC
                                            int direction = closestNPC.direction;
                                            if (direction == 0) direction = 1; 
                                            
                                            // Calculate initial teleport position (Behind NPC)
                                            // Align Player Bottom with NPC Bottom to start
                                            Vector2 teleportPos = new Vector2(
                                                closestNPC.Center.X - (direction * 50) - (Player.width / 2),
                                                closestNPC.Bottom.Y - Player.height
                                            );

                                            // --- Collision Detection & Ground Snapping ---
                                            // 1. If inside blocks, move UP until free
                                            bool stuck = true;
                                            for (int i = 0; i < 30; i++) // Try moving up 30 tiles max
                                            {
                                                if (!Collision.SolidCollision(teleportPos, Player.width, Player.height))
                                                {
                                                    stuck = false;
                                                    break;
                                                }
                                                teleportPos.Y -= 16f;
                                            }

                                            // 2. If floating (and not stuck), try to snap to ground BELOW
                                            if (!stuck)
                                            {
                                                // Check if there is ground within 10 tiles below
                                                for (int i = 0; i < 10; i++)
                                                {
                                                    Vector2 checkPos = teleportPos + new Vector2(0, 16f);
                                                    if (Collision.SolidCollision(checkPos, Player.width, Player.height))
                                                    {
                                                        // Found ground! Stay at current teleportPos (which is just above ground)
                                                        break;
                                                    }
                                                    // If no ground yet, move down to check next tile
                                                    // But only update teleportPos if we confirm it's valid (not solid)
                                                    // Actually, we want to move teleportPos DOWN if it's free.
                                                    if (!Collision.SolidCollision(checkPos, Player.width, Player.height))
                                                    {
                                                        teleportPos.Y += 16f;
                                                    }
                                                }
                                            }
                                            
                                            // Teleport Player
                                            Player.Teleport(teleportPos, 1, 0);
                                            Player.direction = direction; // Face the enemy
                                            
                                            // Calculate Damage
                                            int baseDamage = 50;
                                            float meleeMult = Player.GetDamage(DamageClass.Melee).Additive;
                                            // Extra damage: +20 for every 5% melee damage bonus
                                            // Bonus is (meleeMult - 1). e.g. 1.10 - 1 = 0.10 (10%)
                                            float damageBonus = meleeMult - 1f;
                                            if (damageBonus < 0) damageBonus = 0;
                                            
                                            int extraDamageChunks = (int)(damageBonus / 0.05f);
                                            int extraDamage = extraDamageChunks * 20;
                                            
                                            int totalDamage = (int)(baseDamage * meleeMult) + extraDamage;
                                            
                                            // Apply Damage
                                            Player.ApplyDamageToNPC(closestNPC, totalDamage, 5f, direction, false);
                                            
                                            // Visuals
                                            SoundEngine.PlaySound(SoundID.Item71, closestNPC.Center);
                                            for (int i = 0; i < 20; i++)
                                            {
                                                Dust.NewDust(closestNPC.position, closestNPC.width, closestNPC.height, DustID.Blood, 0, 0, 100, default, 1.5f);
                                                Dust.NewDust(Player.position, Player.width, Player.height, DustID.Smoke, 0, 0, 100, Color.Gray, 1.5f);
                                            }
                                            
                                            sramAbility1Cooldown = SramAbility1BaseCooldown;
                                        }
                                    }

                                    // Habilidad 2 (X): Invisibilidad
                                    if (WakfuMod.Habilidad2Keybind.JustPressed && sramInvisibilityCooldown <= 0)
                                    {
                                        sramInvisibilityActive = true;
                                        sramInvisibilityCooldown = SramInvisibilityBaseCooldown;
                                        sramFirstAttackMultiplier = true;
                                        
                                        // Smoke Bomb Effect
                                        for (int i = 0; i < 50; i++)
                                        {
                                            int dust = Dust.NewDust(Player.position, Player.width, Player.height, DustID.Smoke, 0, 0, 100, Color.Gray, 2f);
                                            Main.dust[dust].velocity *= 1.5f;
                                            Main.dust[dust].noGravity = true;
                                        }
                                        SoundEngine.PlaySound(SoundID.Item74, Player.Center); // Shadow sound
                                    }
                                    break;

                                case WakfuClase.Sacrogrito:
                                    // Habilidad 1 (V): Blood Hook
                                    if (WakfuMod.Habilidad1Keybind.JustPressed && sacrogritoAbility1Cooldown <= 0)
                                    {
                                        // Spawn Hook Projectile
                                        Vector2 direction = (Main.MouseWorld - Player.Center).SafeNormalize(Vector2.UnitX);
                                        float speed = 24f;
                                        int damage = 40; // Base damage
                                        
                                        // Apply passive damage boost to hook damage too
                                        float missingHealthPct = 1f - ((float)Player.statLife / Player.statLifeMax2);
                                        damage = (int)(damage * (1f + missingHealthPct));

                                        Projectile.NewProjectile(
                                            Player.GetSource_FromThis("SacrierHook"),
                                            Player.Center,
                                            direction * speed,
                                            ModContent.ProjectileType<SacrierHookProjectile>(),
                                            damage,
                                            5f,
                                            Player.whoAmI
                                        );
                                        
                                        SoundEngine.PlaySound(SoundID.Item1, Player.Center); // Throw sound
                                        sacrogritoAbility1Cooldown = SacrogritoAbility1BaseCooldown;
                                    }

                                    // Habilidad 2 (X): Punishment / Sacrifice
                                    if (WakfuMod.Habilidad2Keybind.JustPressed && sacrogritoAbility2Cooldown <= 0)
                                    {
                                        // Cost: 50% Current HP
                                        int cost = Player.statLife / 2;
                                        if (cost > 0)
                                        {
                                            Player.statLife -= cost;
                                            CombatText.NewText(Player.getRect(), Color.Red, "-" + cost, true);
                                            
                                            // Buffs: Defense, Regen, Thorns, Inferno
                                            int duration = 7200; // 2 minutes
                                            Player.AddBuff(BuffID.Ironskin, duration);
                                            Player.AddBuff(BuffID.Regeneration, duration);
                                            Player.AddBuff(BuffID.Thorns, duration);
                                            Player.AddBuff(BuffID.Inferno, duration);
                                            
                                            // Visuals
                                            SoundEngine.PlaySound(SoundID.Roar, Player.Center);
                                            for (int i = 0; i < 30; i++)
                                            {
                                                Dust.NewDust(Player.position, Player.width, Player.height, DustID.Blood, 0, -2f, 100, default, 2f);
                                            }
                                            
                                            sacrogritoAbility2Cooldown = SacrogritoAbility2BaseCooldown;
                                        }
                                    }
                                    break;

                                case WakfuClase.Feca:
                                    // Habilidad 1 (V): Glyphs
                                    if (WakfuMod.Habilidad1Keybind.JustPressed && fecaAbility1Cooldown <= 0)
                                    {
                                        // 1. Detect Existing Glyph
                                        Projectile existingGlyph = null;
                                        for (int i = 0; i < Main.maxProjectiles; i++)
                                        {
                                            if (Main.projectile[i].active && 
                                                Main.projectile[i].owner == Player.whoAmI && 
                                                Main.projectile[i].type == ModContent.ProjectileType<Content.Projectiles.FecaGlyphProjectile>())
                                            {
                                                existingGlyph = Main.projectile[i];
                                                break; // Only 1 allowed
                                            }
                                        }

                                        // 2. Check overlap
                                        bool isOverlap = false;
                                        if (existingGlyph != null)
                                        {
                                            if (existingGlyph.getRect().Contains(Main.MouseWorld.ToPoint()))
                                            {
                                                isOverlap = true;
                                            }
                                            // Kill the old one in both cases (Move or Upgrade = Replace)
                                            existingGlyph.Kill();
                                        }

                                        // 3. Stats Calculation
                                        float magicBonus = Player.GetTotalDamage(DamageClass.Magic).Additive;
                                        int bonusStacks = 0;
                                        if (magicBonus > 1.0f)
                                        {
                                            bonusStacks = (int)((magicBonus - 1.0f) / 0.05f);
                                        }

                                        int damage;
                                        int empowered = 0;

                                        if (isOverlap)
                                        {
                                            // Upgrade Stats: Base +5, Bonus +5 -> 45 + 45 per stack
                                            damage = 45 + (45 * bonusStacks);
                                            empowered = 1; // ai[1] for Empowered
                                        }
                                        else
                                        {
                                            // Normal Stats: 45 + 45 per stack
                                            damage = 45 + (45 * bonusStacks);
                                            empowered = 0;
                                        }

                                        // 4. Spawn
                                        // Projectile.NewProjectile expects the CENTER of the projectile, and then subtracts half width/height internally.
                                        // So we just pass Main.MouseWorld.
                                        Vector2 spawnPos = Main.MouseWorld;
                                        
                                        Projectile.NewProjectile(
                                            Player.GetSource_Misc("FecaGlyph"),
                                            spawnPos, 
                                            Vector2.Zero,
                                            ModContent.ProjectileType<Content.Projectiles.FecaGlyphProjectile>(),
                                            damage,
                                            0f,
                                            Player.whoAmI,
                                            0, // ai[0] timer
                                            empowered // ai[1] empowered state
                                        );

                                        fecaAbility1Cooldown = FecaAbility1BaseCooldown;
                                    }
                                    // Habilidad 2 (X): Shield
                                    if (WakfuMod.Habilidad2Keybind.JustPressed && fecaAbility2Cooldown <= 0)
                                    {
                                        // 1. Calculate Shield Amount
                                        float magicBonus = Player.GetTotalDamage(DamageClass.Magic).Additive;
                                        int bonusStacks = 0;
                                        if (magicBonus > 1.0f)
                                        {
                                            bonusStacks = (int)((magicBonus - 1.0f) / 0.05f);
                                        }
                                        int shieldAmount = 500 + (25 * bonusStacks);

                                        // 2. Find Target (Closest Player to Cursor)
                                        Player target = null;
                                        float maxDist = 400f; // Range check
                                        
                                        // Self check first ? No, prioritize closest to cursor
                                        if (Vector2.Distance(Main.MouseWorld, Player.Center) < maxDist)
                                        {
                                            target = Player; // Default to self
                                        }
                                        
                                        // Check others
                                        for (int i = 0; i < Main.maxPlayers; i++)
                                        {
                                            Player p = Main.player[i];
                                            if (p.active && !p.dead)
                                            {
                                                float d = Vector2.Distance(Main.MouseWorld, p.Center);
                                                if (d < maxDist)
                                                {
                                                    maxDist = d;
                                                    target = p;
                                                }
                                            }
                                        }

                                        if (target != null)
                                        {
                                            // --- IMPLEMENTACIÓN: SOLO 1 ESCUDO ---
                                            // 0. Si ya habíamos escudado a alguien antes, quitarle el escudo
                                            if (fecaLastShieldTarget != -1)
                                            {
                                                // Si es el mismo de antes, ya se sobrescribirá. Pero si es diferente...
                                                if (fecaLastShieldTarget != target.whoAmI)
                                                {
                                                    // Mandar mensaje para borrar el escudo del anterior
                                                    Player oldTarget = Main.player[fecaLastShieldTarget];
                                                    // Solo si sigue activo. Si se desconectó, da igual.
                                                    if (oldTarget.active) 
                                                    {
                                                        // Update Local
                                                        var oldWP = oldTarget.GetModPlayer<WakfuPlayer>();
                                                        oldWP.fecaShieldHP = 0;
                                                        oldWP.fecaShieldMaxHP = 0;
                                                        oldWP.fecaShieldDuration = 0;
                                                        
                                                        // Sync Removal
                                                        if (Main.netMode == NetmodeID.MultiplayerClient)
                                                        {
                                                            ModPacket packet = Mod.GetPacket();
                                                            packet.Write((byte)WakfuMod.MessageType.FecaShieldUpdate);
                                                            packet.Write((byte)oldTarget.whoAmI);
                                                            packet.Write(0); // 0 Amount = Remove
                                                            packet.Send();
                                                        }
                                                    }
                                                }
                                            }

                                            // Actualizar tracking
                                            fecaLastShieldTarget = target.whoAmI;

                                            // Apply Shield Locally (Target Update)
                                            // Since we are the sender and the server won't echo back to us, we must update the target locally.
                                            var targetWP = target.GetModPlayer<WakfuPlayer>();
                                            targetWP.fecaShieldHP = shieldAmount;
                                            targetWP.fecaShieldMaxHP = shieldAmount;
                                            targetWP.fecaShieldDuration = 7200; // 2 minutos duracion

                                            if (target.whoAmI == Player.whoAmI)
                                            {
                                                Main.NewText($"Shielded Self: {shieldAmount} HP! (2m)", Color.LightGreen);
                                            }
                                            else
                                            {
                                                Main.NewText($"Shielded {target.name}: {shieldAmount} HP! (2m)", Color.LightGreen);
                                            }

                                            // Sync Packet
                                            if (Main.netMode == NetmodeID.MultiplayerClient)
                                            {
                                                ModPacket packet = Mod.GetPacket();
                                                packet.Write((byte)WakfuMod.MessageType.FecaShieldUpdate);
                                                packet.Write((byte)target.whoAmI);
                                                packet.Write(shieldAmount);
                                                packet.Send();
                                            }
                                            
                                            fecaAbility2Cooldown = FecaAbility2BaseCooldown;
                                        }
                                    }
                                    break;

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

            // --- MANEJAR F9: CAMBIAR PRESET (solo si NO tiene clase aún) ---
            if (claseElegida == WakfuClase.Ninguna && ks.IsKeyDown(Keys.F9) && oldKs.IsKeyUp(Keys.F9))
            {
                // Alternar entre preset 1 y 2
                currentPreset = (currentPreset == 1) ? 2 : 1;
                
                string classList = currentPreset == 1 ? 
                    "Selatrop (F1), Yopuka (F2), Steamer (F3), Tymador (F4), Zurcarac (F5), Xelor (F6), Hipermago (F7), Ocra (F8)" : 
                    "Uginak (F1), Aniripsa (F2), Sram (F3), Sacrier (F4), Feca (F5), [Coming Soon] (F6-F8)";

                Main.NewText($"Preset {currentPreset} selected: {classList}", Color.Yellow);
                return; // Salir para que el mensaje sea visible antes de seleccionar
            }

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
                if (currentPreset == 1)
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
                else // preset 2
                {
                    claseSeleccionada = WakfuClase.Uginak;
                    mensaje = "¡You are Uginak!\\nMaster of Hounds and Hunting.\\nSkill 1 (V): Summon/Command War Hound (6s CD)\\nSkill 2 (X): Hunter's Mark - Mark enemy for bonus damage (10s CD)";
                    colorMensaje = Color.Brown;
                    accionExtra = () => {
                        Player.QuickSpawnItem(Player.GetSource_GiftOrReward(), ItemID.WoodenBow, 1);
                        Player.QuickSpawnItem(Player.GetSource_GiftOrReward(), ItemID.WoodenArrow, 200);
                    };
                }
            }
            else if (ks.IsKeyDown(Keys.F2))
            {
                if (currentPreset == 1)
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
                else
                {
                    // Preset 2: Aniripsa
                    claseSeleccionada = WakfuClase.Aniripsa;
                    mensaje = "¡You are Aniripsa!\nRufus weapon and Fairy Wings sent to your inventory\nSkill 1 (V): Healing/Cursed Mark (target cursor Heal ally / Dmg enemy)\nSkill 2 (X): Explosion (Heal and DMG enemies)";
                    colorMensaje = Color.Pink;
                    accionExtra = () =>
                    {
                        Player.QuickSpawnItem(Player.GetSource_Misc("ClaseAniripsa"), ModContent.ItemType<Content.Items.Weapons.Rufus>());
                        Player.QuickSpawnItem(Player.GetSource_Misc("ClaseAniripsa"), ItemID.FairyWings);
                    };
                }
            }
            else if (ks.IsKeyDown(Keys.F3))
            {
                if (currentPreset == 1)
                {
                claseSeleccionada = WakfuClase.Steamer;
                mensaje = "¡You are Steamer!\nStasis Gun sent to your inventory\nSkill 1: Place a Stasis Turret, if already placed: Concentrated Laser\nSkill 2: Detonate Turret for replacement";
                colorMensaje = Color.SkyBlue;
                accionExtra = () => Player.QuickSpawnItem(Player.GetSource_Misc("ClaseSteamer"), ModContent.ItemType<SteamerPistol>());
                }
                else
                {
                    claseSeleccionada = WakfuClase.Sram;
                    mensaje = "¡You are Sram!\nSram Dagger sent to your inventory\nSkill 1 (V): Shadow Step (Backstab)\nSkill 2 (X): Invisibility (20s CD)\nFirst attack from stealth deals 8x CRIT damage.";
                    colorMensaje = Color.Purple;
                    accionExtra = () => Player.QuickSpawnItem(Player.GetSource_Misc("ClaseSram"), ModContent.ItemType<SramDagger>());
                }
            }
            else // --- Lógica específica para F4 ---
    if (ks.IsKeyDown(Keys.F4)) // Detectar JustPressed para F4
            {
                if (currentPreset == 1)
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
                else
                {
                    // Preset 2: Sacrogrito
                    claseSeleccionada = WakfuClase.Sacrogrito;
                    mensaje = "¡You are Sacrier!\nBerserker Tank.\nSkill 1 (V): Blood Hook - Grapple and steal life.\nSkill 2 (X): Punishment - Sacrifice HP for massive buffs.\nPassive: +Damage based on missing HP.";
                    colorMensaje = Color.DarkRed;
                    accionExtra = () =>
                    {
                        // Dar items iniciales si es necesario
                        // Player.QuickSpawnItem(Player.GetSource_Misc("ClaseSacrogrito"), ModContent.ItemType<SacrierWeapon>()); 
                    };
                }
            }

            else if (ks.IsKeyDown(Keys.F5))
            {
                if (currentPreset == 1)
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
                else
                {
                    claseSeleccionada = WakfuClase.Feca;
                    mensaje = "¡You are Feca!\nProtector of the party.\nSkill 1 (V): Glyphs - Place a magical glyph on the ground.\nSkill 2 (X): Shield - Protect yourself or allies.";
                    colorMensaje = Color.Orange;
                }
            }
            else if (ks.IsKeyDown(Keys.F6))
            {
                if (currentPreset == 1)
                {
                claseSeleccionada = WakfuClase.Xelor;
                mensaje = "¡You are Xelor!\nMaster of Time.\nSkill 1 (V): Teleport to cursor (6s CD).\nSkill 2 (X): Time Suspension (10s) / Rewind (20s CD).\nSlows enemies and projectiles, then rewinds them!";
                colorMensaje = Color.Purple;
                }
                else
                {
                    Main.NewText("Class slot F6 (Preset 2) - Coming Soon!", Color.Gray);
                }
            }
            else if (ks.IsKeyDown(Keys.F7))
            {
                if (currentPreset == 1)
                {
                claseSeleccionada = WakfuClase.Hipermago;
                mensaje = "¡You are Huppermage/Hipermago!\nMaster of Elemental Magic.\nSkill 1 (V): Double Light Ball - Fire 2 energy balls. If both hit, +25 armor-piercing damage!\nSkill 2 (X): [Coming Soon]\nAll damage scales with Ranged damage!";
                colorMensaje = Color.Magenta;
                accionExtra = () => {
                    // Dar las 2 armas del Hipermago
                    Player.QuickSpawnItem(Player.GetSource_GiftOrReward(), ModContent.ItemType<Content.Items.Weapons.HipermagoFireEarthStaff>(), 1);
                    Player.QuickSpawnItem(Player.GetSource_GiftOrReward(), ModContent.ItemType<Content.Items.Weapons.HipermagoAirWaterStaff>(), 1);
                };
                }
                else
                {
                    Main.NewText("Class slot F7 (Preset 2) - Coming Soon!", Color.Gray);
                }
            }
            else if (ks.IsKeyDown(Keys.F8))
            {
                if (currentPreset == 1)
                {
                claseSeleccionada = WakfuClase.Ocra;
                mensaje = "¡You are Ocra!\nCra Bow sent to your inventory (TBD)\nSkill 1: Place Beacon\nSkill 2: Homing Arrow (Explodes on Beacon)";
                colorMensaje = Color.Green;
                accionExtra = () => {
                     // Start with iron bow just in case, or nothing specific requested yet
                     Player.QuickSpawnItem(Player.GetSource_GiftOrReward(), ItemID.IronBow, 1);
                     Player.QuickSpawnItem(Player.GetSource_GiftOrReward(), ItemID.WoodenArrow, 100);
                };
                }
                else
                {
                    Main.NewText("Class slot F8 (Preset 2) - Coming Soon!", Color.Gray);
                }
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

        public override void PostUpdateEquips()
        {
            // Sacrogrito Passive: Damage increases with missing health
            if (claseElegida == WakfuClase.Sacrogrito)
            {
                float missingHealthPct = 1f - ((float)Player.statLife / Player.statLifeMax2);
                // Increase all damage by the missing health percentage
                Player.GetDamage(DamageClass.Generic) += missingHealthPct;
                
                // Passive: Base +100 HP + Extra from Crystals
                Player.statLifeMax2 += 100 + sacrierExtraMaxLife;
            }
        }

        // --- Sacrogrito: Life Crystal and Heart Pickup Bonus ---
        public override bool OnPickup(Item item)
        {
            if (claseElegida == WakfuClase.Sacrogrito)
            {
                // Check for hearts
                if (item.type == ItemID.Heart || item.type == ItemID.CandyApple || item.type == ItemID.CandyCane)
                {
                    // Vanilla hearts heal 20. We add +10 extra.
                    int extraHeal = 10;
                    Player.statLife += extraHeal;
                    if (Player.statLife > Player.statLifeMax2)
                    {
                        Player.statLife = Player.statLifeMax2;
                    }
                    Player.HealEffect(extraHeal); // Show the extra heal number
                }
            }
            return base.OnPickup(item);
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
        
        // --- NUEVO: OnHitNPC para detectar muerte de enemigo marcado (Uginak) ---
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Uginak: Detectar muerte de enemigo marcado
            if (claseElegida == WakfuClase.Uginak && target.life <= 0)
            {
                var globalNPC = target.GetGlobalNPC<Common.GlobalNPCs.WakfuGlobalNPC>();
                if (globalNPC.uginakMarked && globalNPC.uginakMarkedByPlayer == Player.whoAmI)
                {
                    // El Uginak mató al enemigo marcado!
                    int maxLifeBonus = (int)(target.lifeMax * 0.5f); // 50% de vida máxima del enemigo
                    uginakExtraLife += maxLifeBonus;
                    uginakMaxExtraLife += maxLifeBonus;
                    
                    // Aplicar buff de 2 minutos (7200 ticks)
                    Player.AddBuff(ModContent.BuffType<Content.Buffs.UginakLifeTankBuff>(), 7200);
                    
                    // Feedback visual
                    Main.NewText($"+{maxLifeBonus} Life Tank!", Color.LightGreen);
                    SoundEngine.PlaySound(SoundID.Item4, Player.Center);
                    for (int i = 0; i < 30; i++)
                    {
                        Dust.NewDust(Player.position, Player.width, Player.height, DustID.LifeDrain, 0, -2f, 100, Color.Green, 1.5f);
                    }
                    
                    // Desmarcar
                    uginakMarkedNPC = -1;
                }
            }
        }

        // --- ModifyHurt: Absorción de daño con vida extra (Uginak) ---
        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            // Feca Shield Logic
            if (fecaShieldHP > 0)
            {
                // We cannot accurately predict dmg here easily without intricate math, 
                // but we can try to mitigate.
                // However, standard mods usually reduce damage in PreHurt or ModifyHurt.
                // Since ModifyHurt uses modifiers, let's allow the hit but use PostHurt to heal back lost HP,
                // OR reduce incoming damage if we want to effectively block it.
                // "Recibirá todo el daño antes que la vida".
                // Let's rely on ModifyHurt reducing damage to 1 if shield > damage?
                
                // Let's assume damage ~ 50.
                // If Shield=500.
                // We want Player to lose 0 HP (or 1), and Shield to lose 50.
                
                // Tricky part: We don't know exact damage yet (Defense calc happens later?).
                // Usually ModifyHurt happens before Defense? Yes.
                // Let's just create a flat absorption logic in OnHurt which is cleaner for syncing final damage, 
                // but won't prevent death if hit > maxHP.
                // Risk: One-shot kills player even with Shield.
                
                // Implementation: PostHurt works best for "Shield takes damage instead".
                // We heal Player.statLife back by damageAmount.
                // We subtract damageAmount from Shield.
                // If damage > Shield, we heal only ShieldAmount and set Shield=0.
                
                // EXCEPT: If damage > currentHP, Player dies before PostHurt.
                // So we need ModifyHurt to reduce damage if it would kill us (ConsumableDodge-like).
                // Or just reduce damage by a huge % and track it?
                
                // Let's try to block 100% of damage in ModifyHurt if Shield > 0, 
                // and then manually subtract from Shield based on valid estimates?
                // No, estimates are bad.
                
                // Let's stick to the "Reactive Shield" logic in PostHurt for now, assuming hits aren't one-shots.
                // It's the standard tModLoader way for uncomplicated shields.
                // See PostHurt implementation below.
            }

            // Sram: Invisibility Damage Reduction
            if (sramInvisibilityActive)
            {
                modifiers.FinalDamage *= 0;
                modifiers.FinalDamage.Flat += 1;
                
                sramInvisibilityActive = false;
                sramFirstAttackMultiplier = false;
                Player.opacityForAnimation = 1f; // Reset transparency immediately
                Main.NewText("Invisibility Broken by Damage!", Color.Gray);
                
                for (int i = 0; i < 20; i++)
                {
                    Dust.NewDust(Player.position, Player.width, Player.height, DustID.Smoke, 0, 0, 100, Color.Gray, 1.5f);
                }
            }

            // Uginak: Si tiene vida extra, absorber daño primero del tanque
            if (claseElegida == WakfuClase.Uginak && uginakExtraLife > 0)
            {
                // Usaremos ModifyHurt.FinalDamage para interceptar DESPUÉS de todos los cálculos
                // Pero necesitamos saber cuánto daño se va a recibir...
                // En realidad, lo mejor es usar PostHurt para restar la vida después
                // Por ahora marcamos que tiene vida extra disponible (se usa en PostHurt)
            }
        }
        
        public override void DrawEffects(PlayerDrawSet drawInfo, ref float r, ref float g, ref float b, ref float a, ref bool fullBright)
        {
            if (fecaShieldHP > 0)
            {
                // Circular Green Particles (Wakfu-like Shield)
                if (Main.rand.NextBool(5)) // Not every frame
                {
                    float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                    float radius = 40f;
                    Vector2 offset = angle.ToRotationVector2() * radius;
                    
                    Dust d = Dust.NewDustPerfect(Player.Center + offset, DustID.TerraBlade, Vector2.Zero, 150, Color.LightGreen, 1.2f);
                    d.noGravity = true;
                    // Make them sweep?
                    d.velocity = angle.ToRotationVector2().RotatedBy(MathHelper.PiOver2) * 2f;
                }
            }
        }

        // --- PostHurt: Absorber daño del tanque de vida después del golpe ---
        public override void PostHurt(Player.HurtInfo info)
        {
            // Feca Shield Absorption
            if (fecaShieldHP > 0)
            {
                int damageReceived = info.Damage;
                if (damageReceived > 0)
                {
                    int oldShieldHP = fecaShieldHP;
                    int absorbed = 0;
                    if (fecaShieldHP >= damageReceived)
                    {
                        absorbed = damageReceived;
                        fecaShieldHP -= absorbed;
                    }
                    else
                    {
                        absorbed = fecaShieldHP;
                        fecaShieldHP = 0;
                    }

                    if (absorbed > 0)
                    {
                        Player.statLife += absorbed;
                        Player.HealEffect(absorbed, true); // Visual green number for heal (implies shield saved hp)
                        
                        // Shield Warning
                        if (fecaShieldHP > 0 && fecaShieldHP <= 100 && oldShieldHP > 100)
                        {
                            Main.NewText($"The shield has {fecaShieldHP} HP left, it is about to break!", Color.Orange);
                        }

                        // Sync Shield HP (to show others it dropped)
                        if (Main.netMode == NetmodeID.MultiplayerClient)
                        {
                            // Send update packet for SELF
                            ModPacket packet = Mod.GetPacket();
                            packet.Write((byte)WakfuMod.MessageType.FecaShieldUpdate);
                            packet.Write((byte)Player.whoAmI);
                            packet.Write(fecaShieldHP);
                            packet.Send();
                        }
                    }
                }
            }

            // Uginak: Restaurar vida si tenía tanque disponible
            if (claseElegida == WakfuClase.Uginak && uginakExtraLife > 0)
            {
                int damageReceived = info.Damage;
                
                if (uginakExtraLife >= damageReceived)
                {
                    // El tanque absorbe todo el daño
                    uginakExtraLife -= damageReceived;
                    Player.statLife += damageReceived; // Devolver la vida que se quitó
                    Player.statLife = Math.Min(Player.statLife, Player.statLifeMax2); // No exceder máximo
                    
                    // Efecto visual
                    for (int i = 0; i < 5; i++)
                    {
                        Dust.NewDust(Player.position, Player.width, Player.height, DustID.GreenTorch, 0, -1f, 100, Color.Green, 0.8f);
                    }
                }
                else
                {
                    // El tanque absorbe parte del daño
                    int absorbed = uginakExtraLife;
                    Player.statLife += absorbed; // Devolver lo que absorbió el tanque
                    uginakExtraLife = 0;
                    
                    // Efecto visual
                    for (int i = 0; i < 3; i++)
                    {
                        Dust.NewDust(Player.position, Player.width, Player.height, DustID.GreenTorch, 0, -1f, 100, Color.Green, 0.8f);
                    }
                }
                
                // Si se agotó la vida extra, remover buff
                if (uginakExtraLife <= 0)
                {
                    uginakExtraLife = 0;
                    uginakMaxExtraLife = 0;
                    Player.ClearBuff(ModContent.BuffType<Content.Buffs.UginakLifeTankBuff>());
                }
            }
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
            // Sram: Break Invisibility and Apply Damage Multiplier
            if (sramInvisibilityActive && proj.owner == Player.whoAmI)
            {
                if (sramFirstAttackMultiplier)
                {
                    modifiers.SetCrit(); // 100% Crit Chance
                    modifiers.CritDamage *= 4f; // 8x Crit Damage (Base 2x * 4)
                    sramFirstAttackMultiplier = false;
                }
                sramInvisibilityActive = false;
                Player.opacityForAnimation = 1f; // Reset transparency immediately
                Main.NewText("Invisibility Broken by Attack!", Color.Gray);
                for (int i = 0; i < 20; i++)
                {
                    Dust.NewDust(Player.position, Player.width, Player.height, DustID.Smoke, 0, 0, 100, Color.Gray, 1.5f);
                }
            }

            // Aplicar bonus de daño crítico si tiene
            if (critDamageBonus > 0f)
            {
                modifiers.CritDamage += critDamageBonus;
            }
            
            // Uginak: +25% daño a enemigos marcados
            if (claseElegida == WakfuClase.Uginak)
            {
                var globalNPC = target.GetGlobalNPC<Common.GlobalNPCs.WakfuGlobalNPC>();
                if (globalNPC.uginakMarked && globalNPC.uginakMarkedByPlayer == Player.whoAmI)
                {
                    modifiers.SourceDamage *= 1.25f; // +25% daño
                }
            }
            
            // Hipermago: Duplicar el bonus de daño ranged para proyectiles del Hipermago
            if (claseElegida == WakfuClase.Hipermago && proj.owner == Player.whoAmI && IsHipermagoProjectile(proj))
            {
                // Obtener el bonus de daño ranged actual (ej: 1.5f significa +50%)
                float rangedDamageBonus = Player.GetDamage(DamageClass.Ranged).Additive - 1f;
                // Aplicar ese mismo bonus adicional (duplicando el efecto)
                if (rangedDamageBonus > 0f)
                {
                    modifiers.SourceDamage *= (1f + rangedDamageBonus);
                }
            }
            
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
        
        // Helper para identificar proyectiles del Hipermago
        private bool IsHipermagoProjectile(Projectile proj)
        {
            int type = proj.type;
            return type == ModContent.ProjectileType<Content.Projectiles.HipermagoLightBall>() ||
                   type == ModContent.ProjectileType<Content.Projectiles.HipermagoFireExplosion>() ||
                   type == ModContent.ProjectileType<Content.Projectiles.HipermagoEarthRock>() ||
                   type == ModContent.ProjectileType<Content.Projectiles.HipermagoTornado>() ||
                   type == ModContent.ProjectileType<Content.Projectiles.HipermagoIceShard>() ||
                   type == ModContent.ProjectileType<Content.Projectiles.HipermagoHolySpear>() ||
                   type == ModContent.ProjectileType<Content.Projectiles.HipermagoMeteor>() ||
                   type == ModContent.ProjectileType<Content.Projectiles.HipermagoBubble>() ||
                   type == ModContent.ProjectileType<Content.Projectiles.HipermagoFireTornado>() ||
                   type == ModContent.ProjectileType<Content.Projectiles.HipermagoDebrisWhirl>() ||
                   type == ModContent.ProjectileType<Content.Projectiles.HipermagoWindTornado>() ||
                   type == ModContent.ProjectileType<Content.Projectiles.HipermagoSteamExplosion>();
        }
        
        // --- ModifyHitNPCWithItem (Aplica bonus de críticos a armas cuerpo a cuerpo) ---
        public override void ModifyHitNPCWithItem(Item item, NPC target, ref NPC.HitModifiers modifiers)
        {
            // Sram: Break Invisibility and Apply Damage Multiplier
            if (sramInvisibilityActive)
            {
                if (sramFirstAttackMultiplier)
                {
                    modifiers.SetCrit(); // 100% Crit Chance
                    modifiers.CritDamage *= 4f; // 8x Crit Damage (Base 2x * 4)
                    sramFirstAttackMultiplier = false;
                }
                sramInvisibilityActive = false;
                Player.opacityForAnimation = 1f; // Reset transparency immediately
                Main.NewText("Invisibility Broken by Attack!", Color.Gray);
                for (int i = 0; i < 20; i++)
                {
                    Dust.NewDust(Player.position, Player.width, Player.height, DustID.Smoke, 0, 0, 100, Color.Gray, 1.5f);
                }
            }

            // Aplicar bonus de daño crítico si tiene
            if (critDamageBonus > 0f)
            {
                modifiers.CritDamage += critDamageBonus;
            }
            
            // Uginak: +25% daño a enemigos marcados
            if (claseElegida == WakfuClase.Uginak)
            {
                var globalNPC = target.GetGlobalNPC<Common.GlobalNPCs.WakfuGlobalNPC>();
                if (globalNPC.uginakMarked && globalNPC.uginakMarkedByPlayer == Player.whoAmI)
                {
                    modifiers.SourceDamage *= 1.25f; // +25% daño
                }
            }
            
            // Hipermago: Duplicar el bonus de daño ranged para armas del Hipermago
            if (claseElegida == WakfuClase.Hipermago && IsHipermagoWeapon(item))
            {
                float rangedDamageBonus = Player.GetDamage(DamageClass.Ranged).Additive - 1f;
                if (rangedDamageBonus > 0f)
                {
                    modifiers.SourceDamage *= (1f + rangedDamageBonus);
                }
            }
        }
        
        // Helper para identificar armas del Hipermago
        private bool IsHipermagoWeapon(Item item)
        {
            int type = item.type;
            return type == ModContent.ItemType<Content.Items.Weapons.HipermagoFireEarthStaff>() ||
                   type == ModContent.ItemType<Content.Items.Weapons.HipermagoAirWaterStaff>();
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

            string scalingText = "";
            switch (claseElegida)
            {
                case WakfuClase.Selatrop:
                    scalingText = "\n--- SELATROP MECHANICS ---\n" +
                                  "Scaling: Ranged Damage.\n" +
                                  "Skill 1 (V): Portal - Place a portal at cursor. Teleport between them.\n" +
                                  "Skill 2 (X): Portal Detonation - Explode all active portals.\n" +
                                  "Passive: You and your projectiles and allies can pass through portals.";
                    break;
                case WakfuClase.Yopuka:
                    scalingText = "\n--- YOPUKA MECHANICS ---\n" +
                                  "Scaling: Melee Damage.\n" +
                                  "Skill 1 (V): God's Punch - Massive god hand falls from the sky.\n" +
                                  "Skill 2 (X): Jump - Leap and stomp enemies.\n" +
                                  "Passive: Rage - Gain damage bonus when hitting melee enemies or taking damage.";
                    break;
                case WakfuClase.Steamer:
                    scalingText = "\n--- STEAMER MECHANICS ---\n" +
                                  (BalanceMode ? "Scaling: Ranged Damage (Balance Mode ON).\n" : "Scaling: Summon Damage (Balance Mode OFF).\n") +
                                  "Skill 1 (V): Stasis Turret - Place turret / Fire Laser if placed.\n" +
                                  "Skill 2 (X): Turret Overload - Detonate turret for massive damage.\n" +
                                  "Passive: Turrets shoot automatically to an enemy thats affected by sticky grenade from right click steamerGun for massive xplosion.";
                    break;
                case WakfuClase.Tymador:
                    scalingText = "\n--- TYMADOR (ROGUE) MECHANICS ---\n" +
                                  "Scaling: Melee Damage.\n" +
                                  "Skill 1 (V): Bomb / Swap - Place bomb or swap position with one *always with max tier bomb.\n" +
                                  "Skill 2 (X): Detonate - Explode all bombs.\n" +
                                  "Passive: Bombs link together to form laser walls.";
                    break;
                case WakfuClase.Zurcarac:
                    scalingText = "\n--- ZURCARAK (ECAFLIP) MECHANICS ---\n" +
                                  "Scaling: Summon Damage.\n" +
                                  "Skill 1 (V): Summon Kitten / Attack - Call minion or command attack.\n" +
                                  "Skill 2 (X): Roll Die - Random effect (Heal, Damage, Buffs).\n" +
                                  "Passive: Lucky Streak - All damage is randomized (-20% to +25%).";
                    break;
                case WakfuClase.Xelor:
                    scalingText = "\n--- XELOR MECHANICS ---\n" +
                                  "Scaling: Magic Damage.\n" +
                                  "Skill 1 (V): Teleport - Instant short-range teleport.\n" +
                                  "Skill 2 (X): Time Suspension - Freeze enemies / Rewind time.\n" +
                                  "Passive: WIP";
                    break;
                case WakfuClase.Hipermago:
                    scalingText = "\n--- HIPERMAGO (HUPPERMAGE) MECHANICS ---\n" +
                                  "Scaling: Ranged Damage (All magic scales with Ranged).\n" +
                                  "Skill 1 (V): Double Light Ball - Fire 2 energy balls. Armor piercing combo.\n" +
                                  "Skill 2 (X): Elemental Combo/ Light Spear - Combine runes for powerful effects or throw a spear that negates armor.\n" +
                                  "Passive: Rune Mastery - Generate runes with weapons to unlock combos.";
                    break;
                case WakfuClase.Ocra:
                    scalingText = "\n--- OCRA (CRA) MECHANICS ---\n" +
                                  "Scaling: Ranged Damage.\n" +
                                  "Skill 1 (V): Beacon - Place a tactical beacon.\n" +
                                  "Skill 2 (X): Homing Arrow - Fires an arrow that targets beacons.\n" +
                                  "Passive: Precision - Gain stacks for dealing ranged damage, increasing damage.";
                    break;
                case WakfuClase.Uginak:
                    scalingText = "\n--- UGINAK MECHANICS ---\n" +
                                  "Scaling: Melee Damage.\n" +
                                  "Skill 1 (V): War Hound - Summon a dog to fight for you. If there is a doggo summoned, UgiJump instead.\n" +
                                  "Skill 2 (X): Hunter's Mark - Mark an enemy (target at cursor) for extra damage. Get 50% hp as life tank\n" +
                                  "Passive: Life Tank - Store hp from marked enemies. Hunt em down.";
                    break;
                case WakfuClase.Aniripsa:
                    scalingText = "\n--- ANIRIPSA MECHANICS ---\n" +
                                  "Scaling: Magic Damage.\n" +
                                  "Skill 1 (V): Healing Mark - Mark ally to heal or enemy to damage.\n" +
                                  "Skill 2 (X): Reconstituting Word - Explosion that heals allies and hurts enemies.\n" +
                                  "Rufus summoning weapon and free wings";
                    break;
                case WakfuClase.Sram:
                    scalingText = "\n--- SRAM MECHANICS ---\n" +
                                  "Scaling: Melee Damage.\n" +
                                  "Passive: Attacks from Invisibility deal 3x CRIT damage (100% Crit Chance).\n" +
                                  "Skill 1 (V): Shadow Step - Teleport behind enemy + Backstab (50 Base Dmg + Scaling).\n" +
                                  "   Bonus: +20 Flat Damage for every 5% Melee Damage bonus. This skill doesnt break invisibility.\n" +
                                  "Skill 2 (X): Invisibility (20s CD) - Enemies ignore you *buged, 0% Spawn Rate.\n" +
                                  "   Defense: Shadow Slash destroys projectiles in 80px radius.\n" +
                                  "   Broken by: Attacking or Taking Damage.";
                    break;
                case WakfuClase.Sacrogrito:
                    scalingText = "\n--- SACRIER MECHANICS ---\n" +
                                  "Scaling: Melee Damage.\n" +
                                  "Passive: Berserk - Damage increases by % of missing health (up to +100% at 0 HP).\n" +
                                  "   Bonus: +100 Base HP. +10 HP from Hearts. +20 Max HP from Life Crystals.\n" +
                                  "Skill 1 (V): Blood Hook - Grapple to blocks or enemies. Deals damage and steals 50 HP from enemies.\n" +
                                  "Skill 2 (X): Punishment - Sacrifice 50% Current HP to gain Defense, Regen, Thorns, and Fire Aura for 2 minutes.";
                    break;
                case WakfuClase.Feca:
                    scalingText = "\n--- FECA MECHANICS ---\n" +
                                  "Scaling: Magic Damage.\n" +
                                  "Skill 1 (V): Glyphs - Create a fire zone (Magic Dmg). Re-casting on a glyph empowers it.\n" +
                                  "Skill 2 (X): Shield - Protect self/allies.\n" +
                                  "Passive: Glyph Master - Glyphs deal 45 base dmg + 45 per 5% Magic Dmg bonus.";
                    break;
            }

            Main.NewText($"Wakfu Balance Mode: {status}{scalingText}", color);
        }

        // --- SINCRONIZACIÓN MULTIJUGADOR ---

        public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
        {
            ModPacket packet = Mod.GetPacket();
            packet.Write((byte)WakfuMod.MessageType.SyncPlayerWakfuData);
            packet.Write((byte)Player.whoAmI);
            
            // Escribir datos
            packet.Write((int)claseElegida);
            
            // Hipermago
            packet.Write(hipermagoFireRunes);
            packet.Write(hipermagoEarthRunes);
            packet.Write(hipermagoAirRunes);
            packet.Write(hipermagoWaterRunes);
            packet.Write(hipermagoFireCooldown);
            packet.Write(hipermagoEarthCooldown);
            packet.Write(hipermagoAirCooldown);
            packet.Write(hipermagoWaterCooldown);
            packet.Write(hipermagoElementalComboCooldown);
            
            // Yopuka
            packet.Write(rageTicks);
            
            // Stats
            packet.Write(critDamageBonus);

            // Sram
            packet.Write(sramInvisibilityActive);

            // Sacrier
            packet.Write(sacrierExtraMaxLife);

            packet.Send(toWho, fromWho);
        }

        public void ReceivePlayerSync(System.IO.BinaryReader reader)
        {
            claseElegida = (WakfuClase)reader.ReadInt32();
            
            // Hipermago
            hipermagoFireRunes = reader.ReadInt32();
            hipermagoEarthRunes = reader.ReadInt32();
            hipermagoAirRunes = reader.ReadInt32();
            hipermagoWaterRunes = reader.ReadInt32();
            hipermagoFireCooldown = reader.ReadInt32();
            hipermagoEarthCooldown = reader.ReadInt32();
            hipermagoAirCooldown = reader.ReadInt32();
            hipermagoWaterCooldown = reader.ReadInt32();
            hipermagoElementalComboCooldown = reader.ReadInt32();
            
            // Yopuka
            rageTicks = reader.ReadInt32();
            
            // Stats
            critDamageBonus = reader.ReadSingle();

            // Sram
            sramInvisibilityActive = reader.ReadBoolean();

            // Sacrier
            sacrierExtraMaxLife = reader.ReadInt32();
        }

        public override void CopyClientState(ModPlayer targetCopy)
        {
            WakfuPlayer clone = (WakfuPlayer)targetCopy;
            clone.claseElegida = claseElegida;
            
            // Hipermago
            clone.hipermagoFireRunes = hipermagoFireRunes;
            clone.hipermagoEarthRunes = hipermagoEarthRunes;
            clone.hipermagoAirRunes = hipermagoAirRunes;
            clone.hipermagoWaterRunes = hipermagoWaterRunes;
            clone.hipermagoFireCooldown = hipermagoFireCooldown;
            clone.hipermagoEarthCooldown = hipermagoEarthCooldown;
            clone.hipermagoAirCooldown = hipermagoAirCooldown;
            clone.hipermagoWaterCooldown = hipermagoWaterCooldown;
            clone.hipermagoElementalComboCooldown = hipermagoElementalComboCooldown;
            
            // Yopuka
            clone.rageTicks = rageTicks;
            
            // Stats
            clone.critDamageBonus = critDamageBonus;

            // Sram
            clone.sramInvisibilityActive = sramInvisibilityActive;

            // Sacrier
            clone.sacrierExtraMaxLife = sacrierExtraMaxLife;

            // Feca
            clone.fecaShieldHP = fecaShieldHP;
        }

        public override void SendClientChanges(ModPlayer clientPlayer)
        {
            WakfuPlayer clone = (WakfuPlayer)clientPlayer;

            if (clone.claseElegida != claseElegida ||
                clone.hipermagoFireRunes != hipermagoFireRunes ||
                clone.hipermagoEarthRunes != hipermagoEarthRunes ||
                clone.hipermagoAirRunes != hipermagoAirRunes ||
                clone.hipermagoWaterRunes != hipermagoWaterRunes ||
                clone.hipermagoFireCooldown != hipermagoFireCooldown ||
                clone.hipermagoEarthCooldown != hipermagoEarthCooldown ||
                clone.hipermagoAirCooldown != hipermagoAirCooldown ||
                clone.hipermagoWaterCooldown != hipermagoWaterCooldown ||
                clone.hipermagoElementalComboCooldown != hipermagoElementalComboCooldown ||
                clone.rageTicks != rageTicks ||
                clone.critDamageBonus != critDamageBonus ||
                clone.sramInvisibilityActive != sramInvisibilityActive ||
                clone.sacrierExtraMaxLife != sacrierExtraMaxLife ||
                clone.fecaShieldHP != fecaShieldHP)
            {
                SyncPlayer(toWho: -1, fromWho: Main.myPlayer, newPlayer: false);
            }
        }


        // --- Sram: Spawn Rate Reduction ---
        public override void PreUpdateBuffs()
        {
            if (sramInvisibilityActive)
            {
                // Reduce spawn rate to 0 (MaxSpawns = 0 stops spawning)
                Player.nearbyActiveNPCs = 0; // Trick to influence some spawn logic, but mostly handled in EditSpawnRate if available or GlobalNPC
            }
        }

        // --- PostUpdate: Sram Projectile Defense ---
        public override void PostUpdate()
        {
            if (sramInvisibilityActive)
            {
                // Defense Hitbox (e.g., 80x80 around player)
                Rectangle defenseHitbox = Utils.CenteredRectangle(Player.Center, new Vector2(80, 80));
                
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p.active && p.hostile && p.damage > 0 && p.getRect().Intersects(defenseHitbox))
                    {
                        // Destroy projectile
                        p.Kill();
                        
                        // Visuals: Dagger Slash
                        SoundEngine.PlaySound(SoundID.Item71, p.Center); // Slash sound
                        
                        // Create slash dust effect
                        Vector2 direction = (p.Center - Player.Center).SafeNormalize(Vector2.Zero);
                        
                        // Sparkles
                        for (int j = 0; j < 10; j++)
                        {
                            Dust d = Dust.NewDustPerfect(p.Center, DustID.Silver, direction.RotatedByRandom(0.5f) * Main.rand.NextFloat(2f, 4f), 0, default, 1.5f);
                            d.noGravity = true;
                        }
                        
                        // Slash Line
                        Vector2 slashDir = direction.RotatedBy(MathHelper.PiOver2);
                        for (int k = -3; k <= 3; k++)
                        {
                             Vector2 offset = slashDir * k * 4;
                             Dust d = Dust.NewDustPerfect(p.Center + offset, DustID.Shadowflame, direction * 2f, 150, Color.Purple, 1.2f);
                             d.noGravity = true;
                        }
                    }
                }
            }
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