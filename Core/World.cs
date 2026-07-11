using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Events;

namespace Aeternum.WorldGen.Core;

public class World
{
    public int CurrentYear { get; set; } //Текущий год

    public List<Character> Characters { get; set; } = []; //Список жителей
    public List<Family> Families { get; set; } = []; //Список семей
    public List<Dynasty> Dynasties { get; set; } = []; //Список династий

    public int TotalBirths { get; set; } //Всего рождений в мире

    public int TotalDeaths { get; set; } //Всего смертей в мире

    public int AliveCount { get; set; } //Количество живых персонажей

    public List<WorldEvent> Events { get; set; } = new(); //Список событий в мире
}