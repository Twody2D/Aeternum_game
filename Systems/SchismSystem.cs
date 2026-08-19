using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Events;
using Aeternum.WorldGen.Generators;

namespace Aeternum.WorldGen.Systems;

// Расколы веры: до сих пор список религий был неизменным с первого года мира —
// сколько ReligionGenerator создал на старте, столько и оставалось навсегда, а
// колонии наследовали веру родителя один в один (ColonizationSystem). Вера могла
// только распространяться вместе с людьми, но никогда не менялась.
//
// Раскол здесь — не медленный дрейф обряда вдали от единоверцев, а акт разрыва.
// Дрейф в этом мире и не мог бы случиться: единоверцы поселения — это почти
// всегда его же колонии, а те по построению оседают рядом с родителем
// (SettlementGenerator.ColonyOffsetRange), так что "община на отшибе" попросту
// не возникает. Зато разрыв с властью в данных есть, и уже в двух видах:
// открытое неповиновение короне (RebellionSystem) и жизнь под иноверным
// правителем. Собственная вера становится продолжением этого разрыва.
//
// Никаких новых последствий подключать не нужно — вся уже написанная механика
// религии (союзы, святые войны, стабильность государства, браки, восстания)
// подхватывает новую веру сама, просто потому что она не равна прежней
public static class SchismSystem
{
    private const int MinPopulationForSchism = 10; // Нужна община, а не горстка жителей

    public static void Process(World world)
    {
        foreach (var settlement in world.Settlements.ToList())
        {
            var religion = settlement.Religion;

            if (religion == null || settlement.Members.Count(m => m.Alive) < MinPopulationForSchism)
            {
                continue;
            }

            if (!IsBreakingWithAuthority(settlement, world))
            {
                continue;
            }

            if (Rng.NextDouble() >= world.Settings.SchismChance)
            {
                continue;
            }

            Split(settlement, religion, world);
        }
    }

    // Поселение уже в разрыве с властью: либо отказало короне в повиновении,
    // либо живёт под правителем чужой веры
    private static bool IsBreakingWithAuthority(Settlement settlement, World world)
    {
        if (RebellionSystem.IsRebelling(settlement, world))
        {
            return true;
        }

        var kingdom = world.Kingdoms.FirstOrDefault(k => k.FallenYear == null && k.Settlements.Contains(settlement));
        var rulerReligion = kingdom?.Ruler.Settlement?.Religion;

        return rulerReligion != null && rulerReligion != settlement.Religion;
    }

    private static void Split(Settlement settlement, Religion parent, World world)
    {
        var schism = ReligionGenerator.CreateSchism(parent, settlement, world.CurrentYear);

        world.Religions.Add(schism);
        settlement.Religion = schism;

        world.Events.Add(new WorldEvent
        {
            Year = world.CurrentYear,
            Type = EventType.Schism,
            Description = $"{settlement.Name}: разрыв с властью довершился разрывом в вере — основан {schism.Name}"
        });
    }
}
