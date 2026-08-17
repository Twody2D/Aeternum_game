using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;

namespace Aeternum.WorldGen.Systems;


// Старение всех живых персонажей на один год: возраст, этап жизни, профессия
public static class AgeSystem
{
    public static void Process(World world)
    {
        foreach(var character in world.Characters)
        {
            if (!character.Alive)
            {
                continue;
            }


            character.Age++;


            LifeSystem.UpdateLifeStage(character); // Пересчитываем этап жизни по новому возрасту


            LifeSystem.AssignProfession(character); // Школа в 7 лет, профессия в 16
        }
    }
}
