using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Events;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;

namespace Aeternum.WorldGen.Tests.Systems;

// Роман на стороне — тот же довод, что уже сводит законные пары и отцов
// внебрачных детей (см. MarriageSystem.GetAffinity, BastardSystem), только
// для женатых. Большинство интрижек остаётся тайной — проверять можно
// только по большой выборке или по долгому прогону, где редкое событие
// набирается количеством лет
public class ScandalSystemTests
{
    private static int _nextId = 1;

    private static Character Person(string name, Gender gender, Settlement settlement)
    {
        var character = new Character
        {
            Id = _nextId++,
            Name = name,
            LastName = "Тестов",
            Gender = gender,
            Age = 30,
            Alive = true,
            LifeStage = LifeStage.Adult,
            Settlement = settlement,
            Profession = "Фермер"
        };

        settlement.Members.Add(character);

        return character;
    }

    // Женатая пара плюс по одному подходящему претенденту на измену для каждой
    // стороны — неважно, кто из супругов оступится первым
    private static (World World, Character Husband, Character Wife) BuildMarriedCouple()
    {
        var world = new World { CurrentYear = 1 };
        var settlement = new Settlement { Id = 1, Name = "Тестовка" };
        world.Settlements.Add(settlement);

        var husband = Person("Муж", Gender.Male, settlement);
        var wife = Person("Жена", Gender.Female, settlement);
        Person("Другая", Gender.Female, settlement);
        Person("Другой", Gender.Male, settlement);

        var family = new Family { Id = 1, Father = husband, Mother = wife, FormedYear = 1 };
        husband.CurrentFamily = family;
        wife.CurrentFamily = family;
        world.Families.Add(family);

        world.Characters.AddRange(settlement.Members);

        return (world, husband, wife);
    }

    [Fact]
    public void Process_EventuallyExposesAnAffair_AndMarksEnmityAgainstBothSides()
    {
        var (world, husband, wife) = BuildMarriedCouple();

        Rng.Initialize(seed: 1);

        WorldEvent? scandal = null;

        for (var year = 0; year < 3000 && scandal == null; year++)
        {
            world.CurrentYear++;
            ScandalSystem.Process(world);
            scandal = world.Events.FirstOrDefault(e => e.Type == EventType.Scandal);
        }

        Assert.NotNull(scandal);

        // Обманутый супруг заносит во вражду сразу двоих: и неверного супруга,
        // и разлучника — у изменника из-за симметричности AddEnmity в списке
        // врагов появляется только обманутый супруг (1 запись), у обманутого — двое (2)
        var betrayed = husband.Enemies.Count == 2 ? husband : wife;
        var strayed = betrayed == husband ? wife : husband;

        Assert.Equal(2, betrayed.Enemies.Count);
        Assert.Single(strayed.Enemies);
        Assert.Contains(strayed, betrayed.Enemies);
    }

    [Fact]
    public void Process_DiscoveredAffairs_SometimesEndInDivorce_ButNotAlways()
    {
        // Один прогон исхода не докажет — считаем оба исхода на множестве независимых миров
        var divorced = 0;
        var stayedTogether = 0;

        for (var run = 0; run < 300 && (divorced == 0 || stayedTogether == 0); run++)
        {
            var (world, husband, wife) = BuildMarriedCouple();

            Rng.Initialize(seed: run + 1);

            for (var year = 0; year < 200; year++)
            {
                world.CurrentYear++;
                ScandalSystem.Process(world);

                if (world.Events.Any(e => e.Type == EventType.Scandal))
                {
                    break;
                }
            }

            if (!world.Events.Any(e => e.Type == EventType.Scandal))
            {
                continue;
            }

            if (husband.CurrentFamily == null)
            {
                divorced++;
            }
            else
            {
                stayedTogether++;
            }
        }

        Assert.True(divorced > 0, "хотя бы часть раскрытых измен должна рвать брак");
        Assert.True(stayedTogether > 0, "хотя бы часть пар должна оставаться вместе, несмотря на скандал");
    }

    [Fact]
    public void Process_NeverPairsCloseKin()
    {
        var world = new World { CurrentYear = 1 };
        var settlement = new Settlement { Id = 1, Name = "Тестовка" };
        world.Settlements.Add(settlement);

        var father = Person("Отец", Gender.Male, settlement);
        var mother = Person("Мать", Gender.Female, settlement);
        var family = new Family { Id = 1, Father = father, Mother = mother, FormedYear = 1 };
        father.CurrentFamily = family;
        mother.CurrentFamily = family;
        world.Families.Add(family);

        // Взрослый сын — единственный, кроме родителей, того же поселения:
        // если бы запрет на родство не работал, измена отца могла бы указать только на мать (уже исключена) или на сына
        var son = Person("Сын", Gender.Male, settlement);
        son.Mother = mother;
        son.Father = father;

        // Дочь — потенциальный "разлучник" для отца при отсутствии проверки родства
        var daughter = Person("Дочь", Gender.Female, settlement);
        daughter.Mother = mother;
        daughter.Father = father;

        world.Characters.AddRange(settlement.Members);

        Rng.Initialize(seed: 1);

        for (var year = 0; year < 2000; year++)
        {
            world.CurrentYear++;
            ScandalSystem.Process(world);
        }

        Assert.DoesNotContain(world.Events, e => e.Type == EventType.Scandal);
    }
}
