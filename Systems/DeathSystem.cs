using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Events;

namespace Aeternum.WorldGen.Systems;

// Смерть персонажей: старость, детская смертность и несчастные случаи
// (у опасных профессий риск несчастного случая выше — см. ProfessionSystem.IsHazardous)
public static class DeathSystem
{
    private static readonly Random _random = new();
    public static void Process(World world)
    {
        foreach (var character in world.Characters)
        {
            if (!character.Alive)                        // Если персонаж мертв
            {
                continue;                            // Переходим к следующему персонажу, если текущий мертв
            }

            if (character.Age >= world.Settings.MaximumAge) // Предельный возраст — смерть гарантирована
            {
                Kill(character, world, DeathReason.OldAge);
                continue;
            }

            if (character.LifeStage == LifeStage.Infant &&
                _random.NextDouble() < world.Settings.InfantMortalityRate) // Детская смертность
            {
                Kill(character, world, DeathReason.Disease);
                continue;
            }

            if (character.LifeStage == LifeStage.Elder)  // Возраст 60+ — растущий с возрастом шанс смерти
            {
                int deathChance = character.Age - 60; // Шанс смерти увеличивается с возрастом
                if (_random.Next(100) < deathChance)  // Генерируем случайное число и сравниваем с шансом смерти
                {
                    Kill(character, world, DeathReason.OldAge); // Если персонаж умирает, вызываем метод Kill
                    continue;
                }
            }

            if (_random.NextDouble() < GetAccidentChance(character, world)) // Несчастный случай — риск есть в любом возрасте
            {
                Kill(character, world, DeathReason.Accident);
            }
        }
    }

    // Базовый риск несчастного случая, умноженный для опасных профессий (воин, охотник, моряк и т.п.)
    private static double GetAccidentChance(Character character, World world)
    {
        double chance = world.Settings.AccidentRate;

        if (ProfessionSystem.IsHazardous(character.Profession))
        {
            chance *= world.Settings.HazardousProfessionMultiplier;
        }

        return chance;
    }

    // Помечает персонажа мёртвым, логирует событие смерти и освобождает овдовевшего супруга для нового брака.
    // Публичный, т.к. переиспользуется EconomySystem при смерти от голода
    public static void Kill(Character character, World world, DeathReason reason)
    {
        character.Alive = false;
        character.DeathReason = reason;

        world.TotalDeaths++;
        world.AliveCount--;

        var spouse = GetSpouse(character);
        if (spouse is { Alive: true })
        {
            spouse.CurrentFamily = null; // Вдова/вдовец снова доступны для MarriageSystem
        }

        world.Events.Add(new WorldEvent
        {
            Year = world.CurrentYear,
            Type = EventType.Death,
            Description = $"{character.Name} {character.LastName} умер в возрасте {character.Age} ({DescribeReason(reason)})"
        });
    }

    private static Character? GetSpouse(Character character)
    {
        var family = character.CurrentFamily;

        if (family == null)
        {
            return null;
        }

        return family.Father == character ? family.Mother : family.Father;
    }

    private static string DescribeReason(DeathReason reason)
    {
        return reason switch
        {
            DeathReason.OldAge => "старость",
            DeathReason.Disease => "болезнь",
            DeathReason.Accident => "несчастный случай",
            DeathReason.Starvation => "голод",
            _ => "неизвестная причина"
        };
    }
}
