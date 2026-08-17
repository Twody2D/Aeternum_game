namespace Aeternum.WorldGen.Models;

// Тип события мира, см. WorldEvent
public enum EventType
{
    Birth,             // Рождение ребёнка
    Death,             // Смерть персонажа
    Marriage,          // Заключение брака
    Divorce,           // Развод
    CreationOfDynasty, // Основание новой династии
    Migration          // Переезд в другое поселение
}
