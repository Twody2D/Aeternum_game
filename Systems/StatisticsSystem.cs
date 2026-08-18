using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Models;

namespace Aeternum.WorldGen.Systems;


// Строит итоговые данные по миру — никакого текста, только объекты и числа.
// Как это показать (консоль, UI), решает вызывающий код
public static class StatisticsSystem
{
    // Стартовое население — сами персонажи, никакой обёртки не нужно
    public static List<Character> BuildInitialPopulationReport(World world)
    {
        return world.Characters;
    }

    // Итоговая статистика по завершении симуляции: демография и возрастные группы
    public static WorldStatistics BuildFinalReport(World world)
    {
        var settlements = world.Settlements
            .Select(s => new SettlementStat(s, world.Characters.Count(c => c.Alive && c.Settlement == s)))
            .ToList();

        var ageGroups = world.Characters
            .Where(c => c.Alive)
            .GroupBy(c => GetAgeGroup(c, world.Settings))
            .Select(g => new AgeGroupCount(g.Key, g.Count()))
            .ToList();

        return new WorldStatistics(
            world.CurrentYear,
            world.Characters.Count,
            world.Characters.Count(c => c.Alive),
            world.TotalBirths,
            world.TotalDeaths,
            settlements,
            ageGroups);
    }

    private static AgeGroup GetAgeGroup(Character character, WorldSettings settings)
    {
        if (character.Age < settings.AdultAge)
        {
            return AgeGroup.Child;
        }

        if (character.Age < settings.ElderAge)
        {
            return AgeGroup.Adult;
        }

        return AgeGroup.Elder;
    }
}
