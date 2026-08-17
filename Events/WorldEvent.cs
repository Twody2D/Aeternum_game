using Aeternum.WorldGen.Models;

namespace Aeternum.WorldGen.Events;


// Одно событие мира (рождение, смерть, брак, основание династии),
// накапливается в World.Events и читается любым потребителем (консоль, UI)
public class WorldEvent
{
    public int Year { get; set; } // Год, в котором произошло событие

    public EventType Type { get; set; } // Тип события

    public string Description { get; set; } = ""; // Готовый текст для отображения
}
