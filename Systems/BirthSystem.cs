using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Generators;
using Aeternum.WorldGen.Events;
namespace Aeternum.WorldGen.Systems;

public static class BirthSystem
{
    private static readonly Random _random = new();

    public static void ProcessBirths(List<Character> newborns, World world)
    {
        // Отцу достаточно быть живым, у матери дополнительно проверяем фертильный возраст
        var fertileMothers = PopulationSystem.GetAdultFemales(world).ToHashSet();

        var families = world.Families
            .Where(f => f.Father.Alive && fertileMothers.Contains(f.Mother))
            .ToList();

        if (families.Count == 0)
        {
            return;
        }

        double birthRate = PopulationSystem.GetBirthRate(world);

        // Каждая подходящая семья за год проверяется ровно один раз,
        // ожидаемое число рождений = families.Count * birthRate
        foreach (var family in families)
        {
            if (_random.NextDouble() >= birthRate)
            {
                continue;
            }

            var mother = family.Mother;
            var father = family.Father;

            var newborn = CharacterGenerator.CreateNewborn();

            newborn.Mother = mother; // Устанавливаем ссылку на мать новорожденного
            newborn.Father = father; // Устанавливаем ссылку на отца новорожден
            newborn.LastName = father.LastName;

            FamilySystem.AddChildToFamily(family, newborn);

            newborns.Add(newborn); // Добавляем новорожденного персонажа в список newborns

            world.TotalBirths++;
            world.AliveCount++;

            world.Events.Add(new WorldEvent
            {
                Year = world.CurrentYear,
                Type = EventType.Birth,
                Description = $"Родился {newborn.Name} {newborn.LastName}. " +
                $"Родители: {mother.Name} {mother.LastName} и {father.Name} {father.LastName}"
            });
        }
    }
}
