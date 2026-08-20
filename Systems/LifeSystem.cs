using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;

namespace Aeternum.WorldGen.Systems;

// Жизненный этап и школа/профессия персонажа — производные от возраста
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

    public static void AssignProfession(Character character, World world) // Метод для назначения профессии персонажу на основе его возраста
    {
        if (character.Age == 7 && character.Profession == null)
        {
            character.Profession = ProfessionSystem.school; // Назначаем профессию "Школьник" для персонажей, достигших возраста 7 лет
        }
        else if (character.Age == 16 && character.Profession == ProfessionSystem.school)
        {
            // Семейное дело чаще передаётся по родителю того же пола (сын перенимает
            // ремесло отца, дочь — матери), при отсутствии — от другого родителя
            var sameSexParent = character.Gender == Gender.Male ? character.Father : character.Mother;
            var otherParent = character.Gender == Gender.Male ? character.Mother : character.Father;
            var inheritedProfession = sameSexParent?.Profession ?? otherParent?.Profession;

            // Недостающая обязательная профессия в поселении важнее наследования,
            // а культура поселения смещает выбор, когда ни то ни другое не сработало
            character.Profession = ProfessionSystem.GetRandom(character.Settlement?.Culture, character.Settlement, inheritedProfession);
            character.ProfessionYear = world.CurrentYear;
        }
    }
}