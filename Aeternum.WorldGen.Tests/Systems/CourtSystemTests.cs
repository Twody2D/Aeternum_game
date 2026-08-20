using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;

namespace Aeternum.WorldGen.Tests.Systems;

// Двор — первые роли в государстве кроме самого правителя. Проверяется, что
// должность достаётся тому, кто и правда лучший в своём деле, что она не
// висит на мертвецах и что от неё есть толк в тех системах, ради которых
// она заведена
public class CourtSystemTests
{
    private static (World World, Kingdom Kingdom, Settlement Settlement) BuildKingdom()
    {
        var world = new World { CurrentYear = 100 };
        var settlement = new Settlement { Id = 1, Name = "Столица", X = 500, Y = 500 };

        var ruler = new Character
        {
            Id = 1,
            Name = "Правитель",
            LastName = "Тестов",
            Age = 45,
            Alive = true,
            LifeStage = LifeStage.Adult,
            Settlement = settlement,
            Profession = "Торговец",
            ProfessionYear = 70
        };

        settlement.Members.Add(ruler);

        var dynasty = new Dynasty { Id = 1, Name = "Дом Тестов", Founder = ruler, FoundedYear = 1 };
        dynasty.Members.Add(ruler);

        var kingdom = new Kingdom
        {
            Id = 1,
            Name = "Королевство Тестов",
            Dynasty = dynasty,
            Ruler = ruler,
            FoundedYear = 1,
            Settlements = [settlement],
            TributeRate = world.Settings.TributeRate
        };

        world.Characters.Add(ruler);
        world.Settlements.Add(settlement);
        world.Kingdoms.Add(kingdom);

        return (world, kingdom, settlement);
    }

    private static Character Add(World world, Settlement settlement, string profession, int professionYear = 100, int age = 30)
    {
        var character = new Character
        {
            Id = world.Characters.Count + 10,
            Name = $"Житель{world.Characters.Count}",
            LastName = "Тестов",
            Age = age,
            Alive = true,
            LifeStage = LifeStage.Adult,
            Settlement = settlement,
            Profession = profession,
            ProfessionYear = professionYear
        };

        settlement.Members.Add(character);
        world.Characters.Add(character);

        return character;
    }

    [Fact]
    public void Process_FillsOfficesFromMatchingTrades()
    {
        var (world, kingdom, settlement) = BuildKingdom();

        var soldier = Add(world, settlement, "Воин");
        var merchant = Add(world, settlement, "Торговец");
        var scholar = Add(world, settlement, "Учёный");

        CourtSystem.Process(world);

        Assert.Equal(soldier, kingdom.Court[CourtOffice.Marshal]);
        Assert.Equal(merchant, kingdom.Court[CourtOffice.Treasurer]);
        Assert.Equal(scholar, kingdom.Court[CourtOffice.Chancellor]);
    }

    [Fact]
    public void Process_PrefersTheMoreExperiencedCandidate()
    {
        // Должность достаётся не первому попавшемуся, а лучшему в деле
        var (world, kingdom, settlement) = BuildKingdom();

        Add(world, settlement, "Воин", professionYear: 99);
        var veteran = Add(world, settlement, "Воин", professionYear: 60);

        CourtSystem.Process(world);

        Assert.Equal(veteran, kingdom.Court[CourtOffice.Marshal]);
    }

    [Fact]
    public void Process_RulerHoldsNoOtherOffice()
    {
        // Правитель торгует не хуже прочих, но казначеем при себе не служит
        var (world, kingdom, settlement) = BuildKingdom();
        var merchant = Add(world, settlement, "Торговец", professionYear: 99);

        CourtSystem.Process(world);

        Assert.Equal(merchant, kingdom.Court[CourtOffice.Treasurer]);
        Assert.DoesNotContain(kingdom.Ruler, kingdom.Court.Values);
    }

    [Fact]
    public void Process_NoSuitablePerson_LeavesOfficeEmpty()
    {
        var (world, kingdom, _) = BuildKingdom();

        CourtSystem.Process(world);

        Assert.False(CourtSystem.HasOffice(kingdom, CourtOffice.Marshal), "воеводу не из кого поставить");
        Assert.Equal(1.0, CourtSystem.GetOfficeStrength(kingdom, CourtOffice.Marshal, world));
    }

    [Fact]
    public void Process_DeadOfficer_IsReplaced()
    {
        var (world, kingdom, settlement) = BuildKingdom();
        var first = Add(world, settlement, "Воин");

        CourtSystem.Process(world);
        Assert.Equal(first, kingdom.Court[CourtOffice.Marshal]);

        first.Alive = false;
        var second = Add(world, settlement, "Воин");

        CourtSystem.Process(world);

        Assert.Equal(second, kingdom.Court[CourtOffice.Marshal]);
    }

    [Fact]
    public void Process_OnePersonHoldsOneOfficeAtATime()
    {
        // Наследника определяет закон наследования, и он может совпасть с тем,
        // кто уже служит короне: тогда прежний пост освобождается
        var (world, kingdom, settlement) = BuildKingdom();

        var kin = Add(world, settlement, "Учёный");
        kin.Dynasty = kingdom.Dynasty;
        kingdom.Dynasty.Members.Add(kin);

        CourtSystem.Process(world);
        CourtSystem.Process(world);

        var held = kingdom.Court.Where(kv => kv.Value == kin).Select(kv => kv.Key).ToList();

        Assert.Single(held);
    }

    [Fact]
    public void Process_HeirComesFromTheRulingHouse()
    {
        var (world, kingdom, settlement) = BuildKingdom();

        Add(world, settlement, "Воин"); // Не родня — в наследники не годится
        var kin = Add(world, settlement, "Фермер");
        kin.Dynasty = kingdom.Dynasty;
        kingdom.Dynasty.Members.Add(kin);

        CourtSystem.Process(world);

        Assert.Equal(kin, kingdom.Court[CourtOffice.Heir]);
    }

    [Fact]
    public void Process_OfficerWhoLeftTheRealm_LosesTheOffice()
    {
        var (world, kingdom, settlement) = BuildKingdom();
        var marshal = Add(world, settlement, "Воин");

        CourtSystem.Process(world);
        Assert.Equal(marshal, kingdom.Court[CourtOffice.Marshal]);

        var abroad = new Settlement { Id = 99, Name = "Чужбина" };
        marshal.Settlement = abroad;
        settlement.Members.Remove(marshal);

        CourtSystem.Process(world);

        Assert.False(CourtSystem.HasOffice(kingdom, CourtOffice.Marshal));
    }

    [Fact]
    public void GetOfficeStrength_GrowsWithTheHoldersMastery()
    {
        var (world, kingdom, settlement) = BuildKingdom();
        var novice = Add(world, settlement, "Воин", professionYear: 100);

        CourtSystem.Process(world);
        var weak = CourtSystem.GetOfficeStrength(kingdom, CourtOffice.Marshal, world);

        novice.ProfessionYear = 50; // Тот же человек, но с полувековым стажем

        Assert.True(CourtSystem.GetOfficeStrength(kingdom, CourtOffice.Marshal, world) > weak);
    }

    [Fact]
    public void TributeProcess_TreasurerCollectsMore()
    {
        var withoutTreasurer = BuildKingdom();
        withoutTreasurer.Settlement.FoodStock = 1000;

        var withTreasurer = BuildKingdom();
        withTreasurer.Settlement.FoodStock = 1000;
        Add(withTreasurer.World, withTreasurer.Settlement, "Торговец", professionYear: 50);
        CourtSystem.Process(withTreasurer.World);

        TributeSystem.Process(withoutTreasurer.World);
        TributeSystem.Process(withTreasurer.World);

        Assert.True(withTreasurer.Kingdom.FoodTreasury > withoutTreasurer.Kingdom.FoodTreasury);
    }

    [Fact]
    public void TechnologyProcess_ChancellorAdvancesKnowledgeFaster()
    {
        var plain = BuildKingdom();
        var advised = BuildKingdom();

        Add(advised.World, advised.Settlement, "Учёный", professionYear: 50);
        Add(plain.World, plain.Settlement, "Учёный", professionYear: 50);

        CourtSystem.Process(advised.World);

        TechnologySystem.Process(plain.World);
        TechnologySystem.Process(advised.World);

        Assert.True(advised.World.Knowledge > plain.World.Knowledge);
    }
}
