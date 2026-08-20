using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Generators;
using Aeternum.WorldGen.Events;

namespace Aeternum.WorldGen.Systems;

// Дети рождались только в браке, и мир от этого был устроен аккуратнее, чем
// бывает на самом деле. Здесь появляются те, кто родился вне семьи: у матери
// без мужа и от того, кто мужем ей не стал.
//
// Отдельного признака "незаконнорождённый" заводить не пришлось — он уже был
// выводим: законный ребёнок принадлежит семье рождения (Character.BirthFamily),
// а этот не принадлежит никакой. Отсюда же и все последствия: он носит фамилию
// матери, идёт по её дому, а в очереди на трон стоит позади законных
// (см. SuccessionSystem).
//
// Отца выбирает та же взаимная склонность, что сводит и законные пары
// (см. MarriageSystem.GetAffinity) — женатый мужчина исключением не является,
// в этом и суть
public static class BastardSystem
{
    private const double BastardBirthShare = 0.25; // Доля от обычной годовой рождаемости

    public static void Process(List<Character> newborns, World world)
    {
        var birthRate = PopulationSystem.GetBirthRate(world) * BastardBirthShare;

        var unwedMothers = PopulationSystem.GetAdultFemales(world)
            .Where(w => w.CurrentFamily == null && w.Settlement != null)
            .ToList();

        foreach (var mother in unwedMothers)
        {
            if (Rng.NextDouble() >= birthRate)
            {
                continue;
            }

            var father = FindFather(mother, world);

            if (father == null)
            {
                continue; // Не с кем — в поселении нет ни одного подходящего мужчины
            }

            newborns.Add(Beget(mother, father, world));
        }
    }

    // Ребёнок незаконнорождён тогда и только тогда, когда не принадлежит
    // семье рождения: этим он и отличается от прочих во всех системах мира
    public static bool IsBastard(Character character)
    {
        return character.BirthFamily == null && (character.Mother != null || character.Father != null);
    }

    private static Character? FindFather(Character mother, World world)
    {
        return world.Characters
            .Where(c => c.Alive
                        && c.Gender == Gender.Male
                        && c.LifeStage is LifeStage.Adult or LifeStage.Elder
                        && c.Settlement == mother.Settlement
                        && !IsCloseKin(mother, c)
                        && !mother.Enemies.Contains(c))
            .OrderByDescending(c => MarriageSystem.GetAffinity(mother, c, world))
            .ThenBy(c => c.Id)
            .FirstOrDefault();
    }

    // Те же запреты, что и при браке (см. MarriageSystem.AreRelated), только
    // без учёта вражды — она проверяется отдельно
    private static bool IsCloseKin(Character a, Character b)
    {
        if (a.Mother == b || a.Father == b || b.Mother == a || b.Father == a)
        {
            return true;
        }

        if (a.Mother != null && (a.Mother == b.Mother || a.Mother == b.Father))
        {
            return true;
        }

        return a.Father != null && (a.Father == b.Mother || a.Father == b.Father);
    }

    private static Character Beget(Character mother, Character father, World world)
    {
        var newborn = CharacterGenerator.CreateNewborn();

        newborn.Mother = mother;
        newborn.Father = father;
        newborn.LastName = mother.LastName; // Фамилия материнская: отцовской семьи у него нет
        newborn.Settlement = mother.Settlement;
        newborn.BirthYear = world.CurrentYear;

        mother.Settlement?.Members.Add(newborn);

        // Дом матери принимает его, хотя семья — нет
        if (mother.Dynasty != null)
        {
            newborn.Dynasty = mother.Dynasty;
            DynastySystem.AddMember(mother.Dynasty, newborn);
        }

        world.TotalBirths++;
        world.AliveCount++;

        world.Events.Add(new WorldEvent
        {
            Year = world.CurrentYear,
            Type = EventType.Birth,
            Description = $"У {SurnameSystem.GetDisplayFullName(mother)} вне брака " +
                          $"{(newborn.Gender == Gender.Female ? "родилась" : "родился")} " +
                          $"{SurnameSystem.GetDisplayFullName(newborn)}. " +
                          $"Отец — {SurnameSystem.GetDisplayFullName(father)}"
        });

        return newborn;
    }
}
