using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;

namespace Aeternum.WorldGen.Tests.Systems;

// Восстания и их подавление меняют состояние мира, а не возвращают число,
// поэтому проверяются по последствиям вызова Process. Случайность прижата
// зерном (см. Rng) там, где иначе исход зависел бы от броска
public class RebellionSystemTests
{
    private static (World World, Kingdom Kingdom, Settlement Settlement) BuildKingdomWithSettlement()
    {
        var world = new World();
        var ruler = new Character { Id = 1, Name = "Правитель", LastName = "Тестов", Age = 40, Alive = true };
        var capital = new Settlement { Id = 1, Name = "Столица" };
        var province = new Settlement { Id = 2, Name = "Провинция" };

        ruler.Settlement = capital;
        capital.Members.Add(ruler);

        var dynasty = new Dynasty { Id = 1, Name = "Дом Тестов", Founder = ruler, FoundedYear = 1 };
        dynasty.Members.Add(ruler);

        var kingdom = new Kingdom
        {
            Id = 1,
            Name = "Королевство Тестов",
            Dynasty = dynasty,
            Ruler = ruler,
            FoundedYear = 1,
            Settlements = [capital, province]
        };

        world.Characters.Add(ruler);
        world.Settlements.Add(capital);
        world.Settlements.Add(province);
        world.Kingdoms.Add(kingdom);

        return (world, kingdom, province);
    }

    private static Character AddResident(World world, Settlement settlement, int id, string? profession = null)
    {
        var resident = new Character
        {
            Id = id,
            Name = $"Житель{id}",
            LastName = "Тестов",
            Age = 30,
            Alive = true,
            LifeStage = LifeStage.Adult,
            Settlement = settlement,
            Profession = profession
        };

        settlement.Members.Add(resident);
        world.Characters.Add(resident);

        return resident;
    }

    [Fact]
    public void Process_ContentSettlement_DoesNotRebel()
    {
        // Ни голода, ни чужой веры, ни осады — поводов нет, и мятеж невозможен
        // при любом броске кости
        var (world, _, province) = BuildKingdomWithSettlement();
        AddResident(world, province, 10);
        province.FoodStock = 100;

        for (var i = 0; i < 50; i++)
        {
            world.CurrentYear = i;
            RebellionSystem.Process(world);
        }

        Assert.False(RebellionSystem.IsRebelling(province, world));
    }

    [Fact]
    public void Process_EmptySettlement_DoesNotRebel()
    {
        // Восставать некому — даже при голодном (отрицательном) запасе
        var (world, _, province) = BuildKingdomWithSettlement();
        province.FoodStock = -100;

        for (var i = 0; i < 50; i++)
        {
            world.CurrentYear = i;
            RebellionSystem.Process(world);
        }

        Assert.False(RebellionSystem.IsRebelling(province, world));
    }

    [Fact]
    public void Process_StarvingSettlement_EventuallyRebels()
    {
        var (world, kingdom, province) = BuildKingdomWithSettlement();
        AddResident(world, province, 10);
        province.FoodStock = -100;

        Rng.Initialize(seed: 1);

        for (var i = 1; i <= 200 && !RebellionSystem.IsRebelling(province, world); i++)
        {
            world.CurrentYear = i;
            kingdom.FoodTreasury = 0; // Подавить корона не может — проверяем именно восстание
            RebellionSystem.Process(world);
        }

        Assert.True(RebellionSystem.IsRebelling(province, world), "голодное поселение обязано когда-нибудь восстать");
        Assert.Same(kingdom, province.RebellingAgainst);
    }

    [Fact]
    public void Process_PoorCrown_CannotSuppressRebellion()
    {
        // Пустая казна означает бессилие независимо от войска
        var (world, kingdom, province) = BuildKingdomWithSettlement();
        AddResident(world, province, 10);

        world.CurrentYear = 5;
        province.RebellingUntilYear = 100;
        province.RebellingAgainst = kingdom;
        kingdom.FoodTreasury = 0;

        for (var i = 5; i < 60; i++)
        {
            world.CurrentYear = i;
            RebellionSystem.Process(world);
        }

        Assert.True(RebellionSystem.IsRebelling(province, world));
    }

    [Fact]
    public void Process_ExpiredRebellion_ClearsCrownLink()
    {
        // Иначе мятеж остался бы висеть на короне навсегда после истечения срока
        var (world, kingdom, province) = BuildKingdomWithSettlement();
        AddResident(world, province, 10);
        province.FoodStock = 100;

        province.RebellingUntilYear = 10;
        province.RebellingAgainst = kingdom;
        world.CurrentYear = 11;

        RebellionSystem.Process(world);

        Assert.Null(province.RebellingAgainst);
    }

    [Fact]
    public void Process_FallenCrown_ReleasesRebellingSettlement()
    {
        var (world, kingdom, province) = BuildKingdomWithSettlement();
        AddResident(world, province, 10);

        world.CurrentYear = 5;
        province.RebellingUntilYear = 100;
        province.RebellingAgainst = kingdom;
        kingdom.FallenYear = 4;

        RebellionSystem.Process(world);

        Assert.Null(province.RebellingAgainst);
    }

    [Fact]
    public void Process_ArmedAndRichCrown_SuppressesRebellion()
    {
        var (world, kingdom, province) = BuildKingdomWithSettlement();
        AddResident(world, province, 10);

        // Гарнизон столицы — те самые профессии Military, что держат оборону в войне
        var capital = kingdom.Settlements[0];

        for (var i = 100; i < 120; i++)
        {
            AddResident(world, capital, i, "Воин");
        }

        world.CurrentYear = 5;
        province.RebellingUntilYear = 100;
        province.RebellingAgainst = kingdom;

        Rng.Initialize(seed: 1);

        for (var i = 5; i < 40 && RebellionSystem.IsRebelling(province, world); i++)
        {
            world.CurrentYear = i;
            kingdom.FoodTreasury = 10_000; // Казна не должна кончиться раньше, чем мятеж
            RebellionSystem.Process(world);
        }

        Assert.False(RebellionSystem.IsRebelling(province, world), "войско и казна обязаны рано или поздно подавить мятеж");
        Assert.Null(province.RebellingAgainst);
    }

    // Проверяет само подключение ExileSystem внутри Suppress, а не только
    // ExileSystem изолированно — иначе разрыв связи между системами прошёл бы
    // мимо тестов, хотя ExileSystemTests целиком зелёные (см. ExileSystemTests)
    [Fact]
    public void Process_SuppressedRebellion_SometimesExilesInsteadOfKillingEveryone()
    {
        var exiled = false;

        for (var run = 0; run < 300 && !exiled; run++)
        {
            var (world, kingdom, province) = BuildKingdomWithSettlement();

            for (var i = 0; i < 30; i++)
            {
                AddResident(world, province, 10 + i);
            }

            var capital = kingdom.Settlements[0];

            for (var i = 0; i < 20; i++)
            {
                AddResident(world, capital, 1000 + i, "Воин");
            }

            world.CurrentYear = 5;
            province.RebellingUntilYear = 100;
            province.RebellingAgainst = kingdom;
            kingdom.FoodTreasury = 10_000;

            Rng.Initialize(seed: run + 1);
            RebellionSystem.Process(world);

            exiled = world.Events.Any(e => e.Type == EventType.Exile);
        }

        Assert.True(exiled, "хотя бы раз за 300 попыток подавление мятежа должно кого-то изгнать, а не только казнить");
    }

    [Fact]
    public void Process_SuppressionAttempt_DrainsTreasuryEvenWhenItFails()
    {
        // Поход стоит казне независимо от исхода — иначе бесплатные попытки
        // делали бы любую корону непобедимой
        var (world, kingdom, province) = BuildKingdomWithSettlement();
        AddResident(world, province, 10);

        world.CurrentYear = 5;
        province.RebellingUntilYear = 100;
        province.RebellingAgainst = kingdom;
        kingdom.FoodTreasury = 1000;

        RebellionSystem.Process(world);

        Assert.True(kingdom.FoodTreasury < 1000, "неудачный поход тоже обязан стоить казне");
    }
}
