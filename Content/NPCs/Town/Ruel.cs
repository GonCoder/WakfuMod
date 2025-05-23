// // Content/NPCs/Town/Ruel.cs
// using Terraria;
// using Terraria.ID;
// using Terraria.ModLoader;
// using Terraria.GameContent.Personalities;
// using Terraria.GameContent.Events; // Para condiciones de eventos si es necesario
// using System.Collections.Generic;
// using Microsoft.Xna.Framework; // Necesario para Point
// using WakfuMod.Content.Items.Weapons; // Reemplaza TuMod por tu namespace para las armas de clase
// // using WakfuMod.Content.Items.Mounts; // Reemplaza TuMod por tu namespace para la montura
// // using WakfuMod.Content.Items.Consumables; // Reemplaza TuMod por tu namespace para el spawner de Nox
// // using WakfuMod.Common.Systems; // ASUNCIÓN: Crearás un ModSystem para manejar la lógica de "Nox Derrotado"
// // using WakfuMod.Content.Items.Currency; // ASUNCIÓN: Crearás un Item para las Kamas

// namespace WakfuMod.Content.NPCs.Town // Reemplaza TuMod con el nombre de tu mod
// {
//     [AutoloadHead]
//     public class Ruel : ModNPC
//     {
//         // --- Constantes Internas ---
//         private const string ShopName = "TiendaRuel"; // Nombre interno de la tienda

//         public override void SetStaticDefaults()
//         {
//             // --- Configuración Básica ---
//             Main.npcFrameCount[Type] = 25; // Ajusta al número de frames de tu sprite
//             NPCID.Sets.ExtraFramesCount[Type] = 9; // Frames quieto/hablando
//             NPCID.Sets.AttackFrameCount[Type] = 4; // Frames de ataque (si ataca)
//             NPCID.Sets.DangerDetectRange[Type] = 120; // Rango de detección
//             NPCID.Sets.AttackType[Type] = 0; // 0: Melee (puede usar pala?) 3: No ataca
//             NPCID.Sets.AttackTime[Type] = 45; // Tiempo entre ataques
//             NPCID.Sets.AttackAverageChance[Type] = 20; // Probabilidad de atacar
//             NPCID.Sets.HatOffsetY[Type] = 4; // Ajuste para sombreros

//             // Evitar que desaparezca en eventos como Lluvia de Slimes
//             NPCID.Sets.CannotSitOnFurniture[Type] = false; // Puede sentarse?
//             NPCID.Sets.CantTakeLunchMoney[Type] = true; // El Recaudador no le quita dinero (¡es un Anutrof!)

//             // --- Felicidad / Personalidad ---
//             NPC.Happiness
//                 .SetBiomeAffection<UndergroundBiome>(AffectionLevel.Love) // Ama las cuevas (tesoros!)
//                 .SetBiomeAffection<DesertBiome>(AffectionLevel.Like) // Le gustan los desiertos (¿ruinas?)
//                 .SetBiomeAffection<SnowBiome>(AffectionLevel.Dislike) // No le gusta el frío
//                 .SetBiomeAffection<HallowBiome>(AffectionLevel.Hate) // Odia lo brillante y cursi
//                 .SetNPCAffection(NPCID.GoblinTinkerer, AffectionLevel.Love) // Ama al que repara cosas (y cobra caro)
//                 .SetNPCAffection(NPCID.Merchant, AffectionLevel.Like) // Tolera al mercader normal
//                 .SetNPCAffection(NPCID.TaxCollector, AffectionLevel.Hate) // ¡FUERA IMPUESTOS!
//                 .SetNPCAffection(NPCID.Pirate, AffectionLevel.Dislike); // No confía en piratas
//         }

//         public override void SetDefaults()
//         {
//             // --- Tamaño y Estadísticas ---
//             NPC.width = 18;
//             NPC.height = 40; // Ajusta a tu sprite
//             NPC.lifeMax = 250;
//             NPC.defense = 18; // Algo más resistente
//             NPC.knockBackResist = 0.4f;

//             // --- Comportamiento AI ---
//             NPC.aiStyle = 7; // Town NPC estándar
//             NPC.townNPC = true;
//             NPC.friendly = true;

//             // --- Sonidos y Animación ---
//             NPC.damage = 12; // Daño si ataca
//             NPC.HitSound = SoundID.NPCHit1;
//             NPC.DeathSound = SoundID.NPCDeath1; // Sonido de muerte genérico (o uno personalizado!)
//             AnimationType = NPCID.Merchant; // Usa animación base del Mercader (o Guía, o la que mejor se ajuste)
//         }

//         // --- Condiciones para Aparecer ---
//         public override bool CanTownNPCSpawn(int numTownNPCs)
//         {
//             // Comprueba si la condición "Nox Derrotado" se cumple
//             // Necesitarás un ModSystem para registrar esto, por ejemplo, en el Kill del NPC Nox.
//             // Aquí ASUMIMOS que tienes un ModSystem llamado 'NoxDefeatSystem' con una variable bool 'noxDefeated'
//              bool noxIsDefeated = ModContent.GetInstance<NoxDefeatSystem>().noxDefeated; // Reemplaza con tu sistema real

//              return noxIsDefeated; // Solo aparece si Nox ha sido derrotado al menos una vez
//         }

//         // --- Condiciones para Mudarse ---
//         // tModLoader maneja la mudanza automáticamente si hay una casa válida y CanTownNPCSpawn es true.
//         // Ya no se necesita CheckConditions para esto.

//         // --- Nombres Posibles ---
//         public override List<string> SetNPCNameList() {
//             return new List<string>() {
//                 "Ruel Stroud" // Nombre canónico
//                 // Podrías añadir otros si quieres, pero Ruel es Ruel
//             };
//         }

//         // --- Perfil del Town NPC (Icono de Mapa, etc.) ---
//         public override ITownNPCProfile TownNPCProfile() {
//             return new TownNPCProfile(
//                 NPC.GetPartyTexture(), // Usa textura de fiesta genérica o crea una Ruel_Party.png
//                 ModContent.GetModHeadSlot(HeadTexture), // Busca Ruel_Head.png automáticamente
//                 Texture // Textura principal Ruel.png
//             );
//         }

//         // --- Diálogo ---
//         public override string GetChat()
//         {
//             // Puedes usar WeightedRandom para variedad
//             WeightedRandom<string> chat = new WeightedRandom<string>(Main.rand); // Pasa Main.rand

//             chat.Add("¡Eh, joven! ¿Buscas tesoros? Tengo justo lo que necesitas... por un módico precio en Kamas.", 1.0);
//             chat.Add("Estos artefactos no se encuentran todos los días, ¡aprovecha!", 1.0);
//             chat.Add("¿Kamas? ¡Ah, la única moneda que vale la pena! El oro es... secundario.", 1.0);
//             chat.Add("Nox... ese tipo sí que sabía cómo acumular Kamas. Una pena lo suyo.", 0.5); // Menos frecuente
//             if (Main.LocalPlayer.HasItem(ModContent.ItemType<Kama>())) { // Reemplaza Kama con tu item
//                 chat.Add("¡Veo que tienes buen gusto para la moneda!", 1.5); // Más frecuente si tienes Kamas
//             }
//             if (NPC.AnyNPCs(NPCID.TaxCollector)) {
//                  chat.Add("¡Espero que ese chupasangre del recaudador no se acerque a mis Kamas!", 0.8);
//             }

//             return chat; // Devuelve una frase aleatoria
//         }

//         // --- Botón de Tienda ---
//         public override void SetChatButtons(ref string button, ref string button2)
//         {
//             button = Lang.inter[28].Value; // "Tienda"
//         }

//         // --- Acción del Botón ---
//         public override void OnChatButtonClicked(bool firstButton, ref string shopName)
//         {
//             if (firstButton)
//             {
//                 shopName = ShopName; // Abre la tienda definida en AddShops
//             }
//         }

//         // --- Definición de la Tienda ---
//         public override void AddShops()
//         {
//             // Crear la instancia de la tienda
//             var shop = new NPCShop(Type, ShopName);

//             // --- Moneda Personalizada: Kamas ---
//             // ¡IMPORTANTE! Reemplaza 'Kama' con el nombre exacto de tu clase de item para las Kamas
//             int kamaItemID = ModContent.ItemType<Kama>();
//             shop.AddCustomCurrency(kamaItemID); // Establece Kama como la moneda para los siguientes items

//             // --- Items que Cuestan Kamas ---
//             // Añade las armas de clase (reemplaza con tus nombres de clase reales)
//             shop.Add<YopukaShockwaveSword>(Condition.NpcIsPresent(Type)) // Condición simple para asegurar que Ruel esté
//                 .SetPrice(kamaItemID, 1); // Precio: 1 Kama

//             shop.Add<WakmehamehaWeapon>(Condition.NpcIsPresent(Type))
//                 .SetPrice(kamaItemID, 1);

//             // shop.Add<TuArmaSteamer>(Condition.NpcIsPresent(Type)).SetPrice(kamaItemID, 1);
//             // shop.Add<TuArmaTymador>(Condition.NpcIsPresent(Type)).SetPrice(kamaItemID, 1);

//             // Añade la montura (reemplaza con tu nombre de clase real)
//             shop.Add<TuMontura>(Condition.NpcIsPresent(Type)) // Reemplaza TuMontura
//                 .SetPrice(kamaItemID, 1);

//             // --- Items que Cuestan Oro (Spawner de Nox) ---
//             // Cambia a la moneda por defecto (Cobre/Plata/Oro/Platino)
//             shop.UseDefaultCurrency(); // Vuelve a usar la moneda vanilla

//             // Añade el spawner de Nox (reemplaza con tu nombre de clase real)
//             shop.Add<NoxSpawnerItem>(Condition.NpcIsPresent(Type)) // Reemplaza NoxSpawnerItem
//                  .SetPrice(gold: 1); // Precio: 1 moneda de oro

//             // --- Registrar la tienda ---
//             shop.Register();
//         }

//         // --- (Opcional) Hacer que suelte algo al morir ---
//         public override void ModifyNPCLoot(NPCLoot npcLoot)
//         {
//             // Ejemplo: Podría soltar algunas Kamas si muere?
//             // npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Kama>(), 1, 5, 10)); // Suelta entre 5 y 10 Kamas al morir
//         }

//         // --- (Opcional) Animación de Ataque ---
//         // public override void FindFrame(int frameHeight) { ... } // Si necesitas controlar la animación manualmente

//         // --- (Opcional) Sonido de Ataque ---
//         // public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers) { ... } // Si ataca al jugador
//         // public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo) { ... }
//     }

//     // --- Sistema para Trackear Derrota de Nox ---
//     // Crea un archivo separado para esto, ej. Common/Systems/NoxDefeatSystem.cs
//     /*
//     using Terraria.ModLoader;
//     using Terraria.ModLoader.IO;

//     namespace TuMod.Common.Systems // Reemplaza TuMod
//     {
//         public class NoxDefeatSystem : ModSystem
//         {
//             public bool noxDefeated = false;

//             public override void ClearWorld() {
//                 noxDefeated = false; // Resetea al salir del mundo
//             }

//             public override void SaveWorldData(TagCompound tag) {
//                 if (noxDefeated) {
//                     tag["noxDefeated"] = true;
//                 }
//             }

//             public override void LoadWorldData(TagCompound tag) {
//                 noxDefeated = tag.ContainsKey("noxDefeated");
//             }

//              // --- Necesitarás llamar a esto desde el código de Nox ---
//              // Ejemplo: En el método Kill o ModifyNPCLoot del NPC Nox
//              public static void SetNoxDefeated() {
//                  var instance = ModContent.GetInstance<NoxDefeatSystem>();
//                  if (instance != null) {
//                      instance.noxDefeated = true;
//                  }
//              }
//         }
//     }
//     */
// }