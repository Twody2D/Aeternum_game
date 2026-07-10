using Aeternum.WorldGen.Characters;

namespace Aeternum.WorldGen.GameWorld;

public class World
{
    public int CurrentYear { get; set; } //Текущий год

    public List<Character> Characters { get; set; } = []; //Список жителей

    public int TotalBirths { get; set; } //Всего рождений в мире

    public int TotalDeaths { get; set; } //Всего смертей в мире
}