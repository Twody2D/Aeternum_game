using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Events;

namespace Aeternum.WorldGen.Systems;

// Роман на стороне. Женатый человек до сих пор не мог оступиться иначе, чем
// овдоветь или развестись, — ровным счётом ничего не менялось, пока брак не
// заканчивался официально. Между тем повод для интрижки уже был в данных:
// та же взаимная склонность, что сводит законные пары и отцов внебрачных
// детей (см. MarriageSystem.GetAffinity, BastardSystem), работает и здесь —
// женатый человек не исключение, как и там.
//
// Не каждая интрижка становится скандалом: большинство остаётся тайной, ничего
// в мире не меняя, — ровно как в жизни. Раскрытая же бьёт по тому, что уже
// есть: обманутый супруг заносит и разлучника, и самого изменника во вражду
// (см. Character.Enemies, тот же MurderSystem.AddEnmity, что уже переиспользует
// KingdomSystem для скорбящих наследников) — а значит, при случае это может
// стоить трона тому, кто когда-то предал. Не каждый скандал рвёт брак — часть
// пар остаётся вместе, хоть и с занесённой в список врагов третьей стороной
public static class ScandalSystem
{
    private const double AffairChance = 0.02; // Шанс в год для состоящего в браке взрослого
    private const double DiscoveryChance = 0.4; // Шанс, что интрижка того же года вскроется
    private const double DivorceOnDiscoveryChance = 0.5; // Раскрытая измена рвёт брак не всегда

    public static void Process(World world)
    {
        var married = world.Characters
            .Where(c => c.Alive && c.CurrentFamily != null && c.LifeStage == LifeStage.Adult)
            .ToList();

        foreach (var person in married)
        {
            if (Rng.NextDouble() >= AffairChance)
            {
                continue;
            }

            var lover = FindLover(person, world);

            if (lover == null || Rng.NextDouble() >= DiscoveryChance)
            {
                continue; // Не с кем, либо интрижка осталась тайной — мир её не заметил
            }

            Expose(person, lover, world);
        }
    }

    // Тот же довод, что уже сводит законные пары и отцов внебрачных детей
    // (см. MarriageSystem.GetAffinity) — женатость сама по себе не исключает
    private static Character? FindLover(Character person, World world)
    {
        var spouse = GetSpouse(person);

        return world.Characters
            .Where(c => c.Alive
                        && c.Gender != person.Gender
                        && c.LifeStage is LifeStage.Adult or LifeStage.Elder
                        && c.Settlement == person.Settlement
                        && c != spouse
                        && !IsCloseKin(person, c)
                        && !person.Enemies.Contains(c))
            .OrderByDescending(c => MarriageSystem.GetAffinity(person, c, world))
            .ThenBy(c => c.Id)
            .FirstOrDefault();
    }

    // Те же запреты на близкое родство, что и у брака и у внебрачных детей
    // (см. MarriageSystem.AreRelated, BastardSystem.IsCloseKin)
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

    private static void Expose(Character person, Character lover, World world)
    {
        var spouse = GetSpouse(person);
        var endsInDivorce = spouse != null && Rng.NextDouble() < DivorceOnDiscoveryChance;

        if (spouse != null)
        {
            MurderSystem.AddEnmity(spouse, lover);
            MurderSystem.AddEnmity(spouse, person);

            if (endsInDivorce)
            {
                spouse.CurrentFamily = null;
                person.CurrentFamily = null;
            }
        }

        var outcome = endsInDivorce ? ", брак распался" : ", но супруги остались вместе";

        world.Events.Add(new WorldEvent
        {
            Year = world.CurrentYear,
            Type = EventType.Scandal,
            Description = $"Раскрыта измена: {SurnameSystem.GetDisplayFullName(person)} и " +
                          $"{SurnameSystem.GetDisplayFullName(lover)}{outcome}"
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
}
