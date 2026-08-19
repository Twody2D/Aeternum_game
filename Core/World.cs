using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Events;

namespace Aeternum.WorldGen.Core;

// Единое хранилище состояния мира — передаётся во все системы симуляции
public class World
{
    public int CurrentYear { get; set; } //Текущий год

    public List<Character> Characters { get; set; } = []; //Список жителей
    public List<Family> Families { get; set; } = new(); //Список семей
    public List<Dynasty> Dynasties { get; set; } = []; //Список династий
    public List<Settlement> Settlements { get; set; } = new(); //Список поселений
    public List<Culture> Cultures { get; set; } = new(); //Список культур
    public List<Religion> Religions { get; set; } = new(); //Список религий
    public List<Kingdom> Kingdoms { get; set; } = new(); //Список государств
    public List<TradeRoute> TradeRoutes { get; set; } = new(); //Список торговых путей между поселениями

    public int TotalBirths { get; set; } //Всего рождений в мире

    public int TotalDeaths { get; set; } //Всего смертей в мире

    public int AliveCount { get; set; } //Количество живых персонажей

    public List<WorldEvent> Events { get; set; } = new(); //Список событий в мире

    public WorldSettings Settings { get; set; } = new(); //Настройки мира
}