using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;

namespace Aeternum.WorldGen.Tests.Systems;

// Цех — не постройка и не список, а сам собой сложившийся круг ремесленников
// одного материала. Проверяется надбавка к цене сырья, глава цеха и то, что
// рынок эту надбавку действительно платит
public class GuildSystemTests
{
    private static int _nextId = 1;

    private static Character MakeSmith(Settlement settlement, int professionYear)
    {
        var character = new Character
        {
            Id = _nextId++,
            Name = $"Кузнец{_nextId}",
            LastName = "Тестов",
            Age = 40,
            Alive = true,
            LifeStage = LifeStage.Adult,
            Settlement = settlement,
            Profession = "Кузнец",
            ProfessionYear = professionYear
        };

        settlement.Members.Add(character);

        return character;
    }

    [Fact]
    public void GetQualityPremium_WithoutGuild_IsNeutral()
    {
        var settlement = new Settlement { Id = 1, Name = "Тестовка" };
        var world = new World { CurrentYear = 100 };

        Assert.Equal(1.0, GuildSystem.GetQualityPremium(settlement, MaterialType.Metal, world));
    }

    [Fact]
    public void GetQualityPremium_MoreExperiencedCraftsmen_SellDearer()
    {
        var world = new World { CurrentYear = 100 };

        var novices = new Settlement { Id = 1, Name = "Молодой цех" };
        MakeSmith(novices, professionYear: 100);

        var masters = new Settlement { Id = 2, Name = "Старый цех" };
        MakeSmith(masters, professionYear: 40);

        var novicePremium = GuildSystem.GetQualityPremium(novices, MaterialType.Metal, world);
        var masterPremium = GuildSystem.GetQualityPremium(masters, MaterialType.Metal, world);

        Assert.True(masterPremium > novicePremium, $"опытный цех должен продавать дороже: {masterPremium} против {novicePremium}");
        Assert.True(novicePremium >= 1.0);
    }

    [Fact]
    public void GetQualityPremium_OnlyCountsMatchingMaterial()
    {
        var settlement = new Settlement { Id = 1, Name = "Тестовка" };
        var world = new World { CurrentYear = 100 };
        MakeSmith(settlement, professionYear: 40); // кузнец — металл, не дерево

        Assert.Equal(1.0, GuildSystem.GetQualityPremium(settlement, MaterialType.Wood, world));
        Assert.True(GuildSystem.GetQualityPremium(settlement, MaterialType.Metal, world) > 1.0);
    }

    [Fact]
    public void GetGuildmaster_PicksTheMostMasterfulCraftsman()
    {
        var settlement = new Settlement { Id = 1, Name = "Тестовка" };
        var world = new World { CurrentYear = 100 };

        var novice = MakeSmith(settlement, professionYear: 95);
        var master = MakeSmith(settlement, professionYear: 40);

        Assert.Equal(master, GuildSystem.GetGuildmaster(settlement, MaterialType.Metal, world));
        Assert.NotEqual(novice, GuildSystem.GetGuildmaster(settlement, MaterialType.Metal, world));
    }

    [Fact]
    public void GetGuildmaster_NoCraftsmenOfThatMaterial_IsNull()
    {
        var settlement = new Settlement { Id = 1, Name = "Тестовка" };
        var world = new World { CurrentYear = 100 };
        MakeSmith(settlement, professionYear: 40);

        Assert.Null(GuildSystem.GetGuildmaster(settlement, MaterialType.Wood, world));
    }

    [Fact]
    public void MarketProcess_ExperiencedGuild_EarnsMoreGoldThanNovices()
    {
        // Проверяется не сама шкала надбавки, а то, что рынок её слушает
        var novicesGold = GoldFromSellingMetal(professionYear: 100);
        var mastersGold = GoldFromSellingMetal(professionYear: 40);

        Assert.True(mastersGold > novicesGold, $"опытный цех должен выручать больше: {mastersGold} против {novicesGold}");
    }

    private static double GoldFromSellingMetal(int professionYear)
    {
        var world = new World { CurrentYear = 100 };
        var settlement = new Settlement { Id = 1, Name = "Тестовка" };
        MakeSmith(settlement, professionYear);

        // Металла на складе больше вместимости — весь избыток уйдёт на продажу
        settlement.MaterialStocks[MaterialType.Metal] = 10_000;
        world.Settlements.Add(settlement);

        MarketSystem.Process(world);

        return settlement.Gold;
    }
}
