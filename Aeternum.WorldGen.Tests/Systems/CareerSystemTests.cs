using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;

namespace Aeternum.WorldGen.Tests.Systems;

// Перемена ремесла происходит по нужде поселения, а не по желанию персонажа,
// и стоит ему всего накопленного умения. Проверяется и то и другое, включая
// случаи, когда меняться не должен никто
public class CareerSystemTests
{
    private static (World World, Settlement Settlement) BuildSettlement()
    {
        var world = new World { CurrentYear = 100 };
        var settlement = new Settlement { Id = 1, Name = "Тестовка", X = 500, Y = 500 };
        world.Settlements.Add(settlement);

        return (world, settlement);
    }

    private static Character Add(World world, Settlement settlement, string profession, int age = 30, int professionYear = 100)
    {
        var character = new Character
        {
            Id = world.Characters.Count + 1,
            Name = $"Житель{world.Characters.Count}",
            LastName = "Тестов",
            Age = age,
            Alive = true,
            LifeStage = age >= 60 ? LifeStage.Elder : age >= 16 ? LifeStage.Adult : LifeStage.Student,
            Settlement = settlement,
            Profession = profession,
            ProfessionYear = professionYear
        };

        settlement.Members.Add(character);
        world.Characters.Add(character);

        return character;
    }

    // Поселение, где все обязательные ремёсла заняты: иначе нехватка перебивает
    // всё остальное и проверять другие поводы бессмысленно
    private static void FillEssentials(World world, Settlement settlement)
    {
        foreach (var profession in new[] { "Фермер", "Кузнец", "Столяр", "Каменщик", "Ткач", "Гончар", "Пастырь" })
        {
            Add(world, settlement, profession);
        }
    }

    [Fact]
    public void Process_HungrySettlement_PushesPeopleToFarming()
    {
        var (world, settlement) = BuildSettlement();
        FillEssentials(world, settlement);

        var smith = Add(world, settlement, "Кузнец");
        settlement.FoodStock = -50;

        Rng.Initialize(seed: 1);

        for (var year = 0; year < 200 && ProfessionSystem.GetCategory(smith.Profession) != ProfessionCategory.FoodProducer; year++)
        {
            world.CurrentYear = 100 + year;
            CareerSystem.Process(world);
        }

        Assert.Equal(ProfessionCategory.FoodProducer, ProfessionSystem.GetCategory(smith.Profession));
    }

    [Fact]
    public void Process_MissingEssentialTrade_IsTakenUp()
    {
        // Кузнеца нет вовсе — кто-то обязан за это взяться
        var (world, settlement) = BuildSettlement();
        settlement.FoodStock = 100;

        Add(world, settlement, "Фермер");
        Add(world, settlement, "Фермер");
        Add(world, settlement, "Фермер");

        Rng.Initialize(seed: 1);

        for (var year = 0; year < 200; year++)
        {
            world.CurrentYear = 100 + year;
            CareerSystem.Process(world);
        }

        var professions = settlement.Members.Select(m => m.Profession).ToList();

        Assert.Contains(professions, p => p != "Фермер");
    }

    [Fact]
    public void Process_ContentSettlement_ChangesNothing()
    {
        // Ни голода, ни нехватки ремёсел — поводов бросать своё дело нет
        var (world, settlement) = BuildSettlement();
        FillEssentials(world, settlement);
        settlement.FoodStock = 100;

        var before = settlement.Members.Select(m => m.Profession).ToList();

        Rng.Initialize(seed: 1);

        for (var year = 0; year < 200; year++)
        {
            world.CurrentYear = 100 + year;
            CareerSystem.Process(world);
        }

        Assert.Equal(before, settlement.Members.Select(m => m.Profession).ToList());
    }

    [Fact]
    public void Process_ChangingTrade_ResetsMastery()
    {
        // Вся цена перемены: годы, вложенные в прежнее дело, пропадают
        var (world, settlement) = BuildSettlement();
        FillEssentials(world, settlement);

        var veteran = Add(world, settlement, "Кузнец", age: 40, professionYear: 70);
        settlement.FoodStock = -50;

        Assert.True(ProfessionSystem.GetMastery(veteran, world) > 1.0, "проверяем на том, кому есть что терять");

        Rng.Initialize(seed: 1);

        for (var year = 0; year < 200 && veteran.Profession == "Кузнец"; year++)
        {
            world.CurrentYear = 100 + year;
            CareerSystem.Process(world);
        }

        Assert.NotEqual("Кузнец", veteran.Profession);
        Assert.Equal(world.CurrentYear, veteran.ProfessionYear);
        Assert.Equal(1.0, ProfessionSystem.GetMastery(veteran, world));
    }

    [Fact]
    public void Process_SettledAgeWorker_StaysWithTheirTrade()
    {
        // Начинать заново поздно ещё до старости: проверяется именно возрастной
        // порог, поэтому берётся тот, кто по всем прочим признакам работник
        // в самом соку (LifeStage.Adult)
        var (world, settlement) = BuildSettlement();
        FillEssentials(world, settlement);

        var elder = Add(world, settlement, "Кузнец", age: 55);
        settlement.FoodStock = -50;

        Rng.Initialize(seed: 1);

        for (var year = 0; year < 200; year++)
        {
            world.CurrentYear = 100 + year;
            CareerSystem.Process(world);
        }

        Assert.Equal("Кузнец", elder.Profession);
    }

    [Fact]
    public void Process_Student_DoesNotChangeTrade()
    {
        var (world, settlement) = BuildSettlement();
        FillEssentials(world, settlement);

        var student = Add(world, settlement, "Школьник", age: 12);
        settlement.FoodStock = -50;

        Rng.Initialize(seed: 1);

        for (var year = 0; year < 200; year++)
        {
            world.CurrentYear = 100 + year;
            CareerSystem.Process(world);
        }

        Assert.Equal("Школьник", student.Profession);
    }

    [Fact]
    public void Process_MastersChangeTradeLessOftenThanNovices()
    {
        // Вложенные годы держат человека при его деле сильнее любых обстоятельств
        var novices = CountChanges(professionYear: 100);
        var masters = CountChanges(professionYear: 50); // Полвека стажа — потолок умения

        Assert.True(novices > masters, $"новички должны срываться чаще мастеров: {novices} против {masters}");
    }

    private static int CountChanges(int professionYear)
    {
        var (world, settlement) = BuildSettlement();
        FillEssentials(world, settlement);
        settlement.FoodStock = -50;

        var watched = new List<Character>();

        // Выборка нарочно большая: разница в шансах невелика, и на полусотне
        // человек её легко перекрыл бы разброс одного броска
        for (var i = 0; i < 500; i++)
        {
            watched.Add(Add(world, settlement, "Кузнец", age: 45, professionYear: professionYear));
        }

        Rng.Initialize(seed: 1);
        CareerSystem.Process(world);

        return watched.Count(c => c.Profession != "Кузнец");
    }
}
