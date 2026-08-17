using Aeternum.WorldGen.Core;

namespace Aeternum.WorldGen.Systems;


// Строит текстовые отчёты по миру. Ничего не печатает сама — только возвращает
// готовые строки, вывод (консоль/UI) решает вызывающий код
public static class StatisticsSystem
{
    // Список стартовых жителей с возрастом и профессией
    public static List<string> BuildInitialPopulationReport(World world)
    {
        var lines = new List<string>
        {
            "",
            "===== Начальное население ====="
        };

        foreach (var character in world.Characters)
        {
            lines.Add(
                $"{character.Name} {character.LastName}, {character.Age} лет, {character.Profession}");
        }

        lines.Add("");

        return lines;
    }

    // Итоговая статистика по завершении симуляции: демография и возрастные группы
    public static List<string> BuildFinalReport(World world)
    {
        var lines = new List<string>
        {
            "",
            "===== Итоги симуляции =====",
            $"Возраст мира: {world.CurrentYear} лет",
            $"Всего жителей создано: {world.Characters.Count}",
            $"Живых персонажей: {world.Characters.Count(c => c.Alive)}",
            $"Всего рождений: {world.TotalBirths}",
            $"Всего смертей: {world.TotalDeaths}",
            "",
            "Распределение по возрасту:"
        };

        var ageGroups = world.Characters
            .Where(c => c.Alive)
            .GroupBy(c =>
            {
                if(c.Age < world.Settings.AdultAge)
                    return "Дети";

                if(c.Age < world.Settings.ElderAge)
                    return "Взрослые";

                return "Пожилые";
            });

        foreach(var group in ageGroups)
        {
            lines.Add($"{group.Key}: {group.Count()}");
        }

        lines.Add("");

        return lines;
    }
}
