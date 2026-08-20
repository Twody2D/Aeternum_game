using Aeternum.WorldGen.Models;

namespace Aeternum.WorldGen.Events;


// Одно событие мира (рождение, смерть, брак, основание династии),
// накапливается в World.Events и читается любым потребителем (консоль, UI)
public class WorldEvent
{
    public int Year { get; set; } // Год, в котором произошло событие

    public EventType Type { get; set; } // Тип события

    public string Description { get; set; } = ""; // Готовый текст для отображения

    // Государства, которых касается событие — пусто у личных и местных событий
    // (рождение, эпидемия и т.п.). Без этой ссылки летопись отдельной короны
    // (см. KingdomChronicleSystem) была бы невозможна: имя в Description — это
    // готовый текст на момент события, а не проверяемая связь с Kingdom,
    // которая к тому же может измениться (война — не событие одного государства,
    // а спор нескольких сразу)
    public List<Kingdom> Kingdoms { get; set; } = new();
}
