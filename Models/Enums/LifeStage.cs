namespace Aeternum.WorldGen.Models;

// Этап жизни персонажа, обновляется в LifeSystem.UpdateLifeStage при каждом старении
public enum LifeStage
{
    Infant,      // Младенец 0-2
    Child,       // Ребёнок 3-7
    Student,     // Ученик 8-15
    Adult,       // Взрослый 16-59
    Elder        // Старик 60+
}

