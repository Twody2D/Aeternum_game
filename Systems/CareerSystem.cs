using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;

namespace Aeternum.WorldGen.Systems;

// Профессия перестала быть приговором на всю жизнь. Взрослый может взяться
// за другое дело, но только по причине, которая уже видна в мире: поселение
// голодает, а он не кормит; поселению не хватает обязательного ремесла
// (см. ProfessionSystem.EssentialProfessions); город живёт ремеслом, к которому
// он непричастен.
//
// Цену перемены никто не назначал отдельно — она получается сама: стаж
// считается от того года, когда взялся за нынешнее дело (см. Character.ProfessionYear),
// поэтому уходящий бросает всё накопленное мастерство. Отсюда и то, что мастера
// своё ремесло не бросают: чем выше умение, тем реже человек начинает заново
public static class CareerSystem
{
    private const double BaseChangeChance = 0.04; // Шанс в год задуматься о перемене дела — для того, кто ещё ничего не умеет
    private const int SettledAge = 50; // После этого возраста начинать заново уже не берутся

    public static void Process(World world)
    {
        foreach (var character in world.Characters)
        {
            if (!IsOpenToChange(character, world))
            {
                continue;
            }

            var settlement = character.Settlement!;
            var replacement = FindNewTrade(character, settlement, world);

            if (replacement == null || replacement == character.Profession)
            {
                continue;
            }

            character.Profession = replacement;
            character.ProfessionYear = world.CurrentYear; // Умение прежнего дела с собой не переносится
        }
    }

    private static bool IsOpenToChange(Character character, World world)
    {
        if (!character.Alive ||
            character.LifeStage != LifeStage.Adult ||
            character.Settlement == null ||
            character.Profession == null ||
            character.Age > SettledAge)
        {
            return false;
        }

        // Чем выше мастерство, тем труднее бросить: вложенные годы держат человека
        // при его деле сильнее любых обстоятельств
        var reluctance = ProfessionSystem.GetMastery(character, world);

        return Rng.NextDouble() < BaseChangeChance * (2 - reluctance);
    }

    // Причины перемены — по убыванию неотложности. Все три взяты из состояния
    // самого поселения, а не из желаний персонажа: мир не моделирует стремления,
    // зато прекрасно показывает нужду
    private static string? FindNewTrade(Character character, Settlement settlement, World world)
    {
        // 1. Голод: когда еды не хватает, за соху берутся и те, кто ей не учился
        if (settlement.FoodStock < 0 &&
            ProfessionSystem.GetCategory(character.Profession) != ProfessionCategory.FoodProducer)
        {
            return ProfessionSystem.PickFromCategory(ProfessionCategory.FoodProducer);
        }

        // 2. Некому делать необходимое — берётся тот, кто под рукой
        var missing = ProfessionSystem.GetMissingEssential(settlement);

        if (missing != null)
        {
            return missing;
        }

        // 3. Город живёт своим ремеслом, а этот к нему непричастен (см. WorkshopSystem)
        if (ProfessionSystem.GetCategory(character.Profession) == ProfessionCategory.General)
        {
            return ProfessionSystem.PickLocalCraft(settlement);
        }

        return null;
    }
}
