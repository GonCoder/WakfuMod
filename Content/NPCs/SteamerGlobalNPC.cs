using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO; // Para Save/Load

namespace WakfuMod.Content.NPCs // O el namespace donde esté tu GlobalNPC
{
    public class SteamerGlobalNPC : GlobalNPC
    {
        // --- Variables Existentes ---
        public bool isMarkedByGrenade = false; // Mantenemos esto si lo usas para algo más que el link
        public int linkedGrenade = -1; // ID del proyectil granada vinculado

        // Variables para Reducción de Defensa EFECTIVA
        public int defenseReductionApplied = 0; // Cuánta reducción se está aplicando
        public int defenseReductionTimer = 0; // Cuánto tiempo le queda

        // --- Constantes para Reducción de Defensa ---
        private const int MaxDefenseReduction = 100; // Límite máximo de defensa reducida
        private const int ReductionDuration = 9000; // Duración en ticks (infinito?)

        // --- InstancePerEntity (ya lo tenías) ---
        public override bool InstancePerEntity => true;

        // --- ResetEffects (Modificado para incluir timer) ---
        public override void ResetEffects(NPC npc)
        {
            // Resetea tus variables originales
            isMarkedByGrenade = false; // OJO: ¿Realmente quieres resetear la marca cada frame? O solo cuando la granada explota/desaparece?
                                       // Si solo quieres resetear cuando explota, quita esta línea y hazlo desde el proyectil granada.
                                       // Si es solo un indicador visual momentáneo, está bien aquí.
            linkedGrenade = -1; // Ídem

            // Baja el timer de la reducción
            if (defenseReductionTimer > 0)
            {
                defenseReductionTimer--;
            }
            else
            {
                // Si el timer expira, resetea la reducción aplicada
                defenseReductionApplied = 0;
            }
        }

        public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
        {
            // Si hay reducción activa para este NPC
            if (defenseReductionApplied > 0)
            {
                // Aumentamos el daño recibido.
                // Terraria calcula el daño como: Damage * Effectiveness - Defense * Effectiveness
                // Aumentar el daño recibido por la cantidad de defensa reducida (aproximado)
                // El cálculo exacto de la defensa es: DañoReducido = Defensa * 0.5 (Normal), * 0.75 (Expert), * 1.0 (Master)
                // Para simplificar, añadiremos un daño plano igual a la mitad de la defensa reducida.
                // Esto simula que la defensa es menor.
                float damageToAdd = defenseReductionApplied * 0.5f; // Simula reducción de defensa en modo Normal/Classic
                // Puedes ajustar el multiplicador (0.5f) si juegas principalmente en Expert (0.75f) o Master (1.0f)
                // o hacerlo dinámico basado en Main.expertMode / Main.masterMode

                modifiers.FlatBonusDamage += damageToAdd;

                 // Opcional: Añadir un pequeño multiplicador de daño para un efecto más notable
                 // modifiers.SourceDamage *= (1f + defenseReductionApplied * 0.02f); // Ej: +2% daño por cada punto de defensa reducida
            }
        }


        // --- ApplyDefenseReduction (Casi igual, pero modifica defenseReductionApplied) ---
        public void ApplyDefenseReduction(int amount = 1)
        {
            defenseReductionApplied = System.Math.Min(defenseReductionApplied + amount, MaxDefenseReduction);
            defenseReductionTimer = ReductionDuration;
        }

        // --- NUEVOS MÉTODOS: SaveData / LoadData ---
        // Guarda y carga el estado de la reducción
        public override void SaveData(NPC npc, TagCompound tag)
        {
             // Guarda la marca de granada si es necesario persistirla
             // tag["SteamerMarked"] = isMarkedByGrenade;
             // tag["SteamerLinkedGrenade"] = linkedGrenade;

             if (defenseReductionApplied > 0) {
                 tag["SteamerDefReductionApplied"] = defenseReductionApplied; // Cambiado el nombre de la key
                 tag["SteamerDefReductionTimer"] = defenseReductionTimer;
             }
        }

        public override void LoadData(NPC npc, TagCompound tag)
        {
             // Carga la marca de granada si es necesario
             // isMarkedByGrenade = tag.GetBool("SteamerMarked");
             // linkedGrenade = tag.GetInt("SteamerLinkedGrenade");

             // Carga la reducción de defensa aplicada
            if (tag.ContainsKey("SteamerDefReductionApplied")) { // Cambiado el nombre de la key
                 defenseReductionApplied = tag.GetInt("SteamerDefReductionApplied");
                 defenseReductionTimer = tag.GetInt("SteamerDefReductionTimer");
            } else {
                 defenseReductionApplied = 0;
                 defenseReductionTimer = 0;
            }
        }
    }
}