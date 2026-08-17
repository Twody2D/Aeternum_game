namespace Aeternum.WorldGen.Models;

// Причина смерти персонажа. Сейчас DeathSystem выставляет только OldAge —
// остальные значения зарезервированы под будущие системы (болезни, войны и т.д.)
public enum DeathReason
{
    None,
    OldAge, //Старость
    Disease, //Болезнь
    Accident, //Несчастный случай
    War, //Война
    Starvation, //Голод
    Murder //Убийство

}
