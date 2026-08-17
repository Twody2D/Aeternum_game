using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Events;

namespace Aeternum.WorldGen.Systems;

// Выборка событий мира — сама ничего не выводит, решение "как показать" остаётся за вызывающим кодом
public static class EventSystem
{
    // Возвращает события, произошедшие в текущем году мира
    public static List<WorldEvent> GetYearEvents(World world)
    {
        return world.Events
            .Where(e => e.Year == world.CurrentYear)
            .ToList();
    }
}
