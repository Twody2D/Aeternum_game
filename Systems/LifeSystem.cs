using Aeternum.WorldGen.Models;

namespace Aeternum.WorldGen.Systems;

public static class LifeSystem
{
    public static void UpdateLifeStage(Character character) // Метод для обновления этапа жизни персонажа на основе его возраста
    {
        if (character.Age <= 2)
        {
            character.LifeStage = LifeStage.Infant;
        }
        else if (character.Age <= 7)
        {
            character.LifeStage = LifeStage.Child;
        }
        else if (character.Age <= 15)
        {
            character.LifeStage = LifeStage.Student;
        }
        else if (character.Age <= 59)
        {
            character.LifeStage = LifeStage.Adult;
        }
        else
        {
            character.LifeStage = LifeStage.Elder;
        }
    }

    public static void AssignProfession(Character character) // Метод для назначения профессии персонажу на основе его возраста
    {
        if (character.Age == 7 && character.Profession == null)
        {
            character.Profession = ProfessionSystem.school; // Назначаем профессию "Школьник" для персонажей, достигших возраста 7 лет
        }
        else if (character.Age == 16 && character.Profession == ProfessionSystem.school)
        {
            character.Profession = ProfessionSystem.GetRandom(); // Назначаем случайную профессию для персонажей в возрасте от 16
        }
    }
}