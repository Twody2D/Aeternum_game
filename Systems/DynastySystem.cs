using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Events;

namespace Aeternum.WorldGen.Systems;

// Создание династий и добавление в них новых членов
public static class DynastySystem
{
    private static int _nextDynastyId = 1;

    // Основывает новую династию на имени founder (вызывается, когда у мужчины нет своей династии)
    public static Dynasty CreateDynasty(
        Character founder,
        World world)
    {

        var dynasty = new Dynasty
        {
            Id = _nextDynastyId++,
            Name = $"Дом {founder.LastName}",
            Founder = founder,
            FoundedYear = world.CurrentYear
        };


        dynasty.Members.Add(founder);
        founder.Dynasty = dynasty;

        world.Dynasties.Add(dynasty);

        world.Events.Add(new WorldEvent
        {
            Year = world.CurrentYear,
            Type = EventType.CreationOfDynasty,
            Description = $"Основан {dynasty.Name} ({founder.Name} {founder.LastName})"
        });

        return dynasty;
    }


    // Добавляет персонажа в уже существующую династию (по браку или по рождению).
    // Идемпотентно: при повторном браке в ту же династию (вдова/вдовец женится
    // на ком-то из клана покойного супруга) не дублирует запись
    public static void AddMember(
        Dynasty dynasty,
        Character character)
    {
        if (!dynasty.Members.Contains(character))
        {
            dynasty.Members.Add(character);
        }
    }

    // Восстанавливает счётчик Id после загрузки сохранения — новые династии
    // продолжат нумерацию, а не начнут её заново с 1
    public static void SetNextDynastyId(int value)
    {
        _nextDynastyId = value;
    }
}
