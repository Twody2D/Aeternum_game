using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;

namespace Aeternum.WorldGen.Tests.Systems;

// Множители построек — это ровно те формулы, которыми держится весь баланс
// "эволюции": они снижают риски и потери, и у каждой есть потолок, чтобы
// достаточно застроенное поселение не стало неуязвимым
public class BuildingFactorTests
{
    [Fact]
    public void GetHousingFactor_NoSettlement_IsNeutral()
    {
        // Персонаж без поселения не должен получать ни скидки, ни надбавки к риску
        Assert.Equal(1.0, HousingSystem.GetHousingFactor(null));
    }

    [Fact]
    public void GetHousingFactor_MoreHouses_LowersRisk()
    {
        var few = HousingSystem.GetHousingFactor(new Settlement { Houses = 1 });
        var many = HousingSystem.GetHousingFactor(new Settlement { Houses = 4 });

        Assert.True(many < few);
        Assert.True(few < 1.0);
    }

    [Fact]
    public void GetHousingFactor_IsCappedAndNeverReachesZero()
    {
        // Без потолка обжитое поселение сделало бы несчастные случаи невозможными
        var absurd = HousingSystem.GetHousingFactor(new Settlement { Houses = 10_000 });

        Assert.True(absurd > 0, "множитель риска не должен обнуляться");
        Assert.Equal(HousingSystem.GetHousingFactor(new Settlement { Houses = 100 }), absurd, precision: 10);
    }

    [Fact]
    public void GetWallFactor_IsCappedAndNeverReachesZero()
    {
        // Иначе достаточно укреплённое поселение не теряло бы вообще никого на войне
        var absurd = WallSystem.GetWallFactor(new Settlement { Walls = 10_000 });

        Assert.True(absurd > 0, "множитель потерь не должен обнуляться");
        Assert.Equal(WallSystem.GetWallFactor(new Settlement { Walls = 100 }), absurd, precision: 10);
    }

    [Fact]
    public void GetHospitalFactor_IsCappedAndNeverReachesZero()
    {
        var world = new World();
        var absurd = HospitalSystem.GetHospitalFactor(new Settlement { Hospitals = 10_000 }, world);

        Assert.True(absurd > 0, "смертность не должна обнуляться больницами");
        Assert.Equal(HospitalSystem.GetHospitalFactor(new Settlement { Hospitals = 100 }, world), absurd, precision: 10);
    }

    [Fact]
    public void GetHospitalFactor_AdvancedEra_HelpsMoreThanDarkAges()
    {
        // Знание усиливает отдачу тех же стен — лечат и тем, что научились лечить
        var settlement = new Settlement { Hospitals = 1 };

        var darkAges = HospitalSystem.GetHospitalFactor(settlement, new World { Knowledge = 0 });
        var enlightened = HospitalSystem.GetHospitalFactor(settlement, new World { Knowledge = 100_000 });

        Assert.True(enlightened < darkAges);
    }

    [Fact]
    public void GetProductionMultiplier_MoreWorkshops_ProduceMore()
    {
        var settlement = new Settlement();
        settlement.Workshops[MaterialType.Wood] = 2;

        var withWorkshops = WorkshopSystem.GetProductionMultiplier(settlement, MaterialType.Wood);
        var withoutWorkshops = WorkshopSystem.GetProductionMultiplier(settlement, MaterialType.Metal);

        Assert.True(withWorkshops > withoutWorkshops);
        Assert.Equal(1.0, withoutWorkshops);
    }

    [Fact]
    public void GetProductionMultiplier_IsCapped()
    {
        var settlement = new Settlement();
        settlement.Workshops[MaterialType.Wood] = 10_000;

        var capped = new Settlement();
        capped.Workshops[MaterialType.Wood] = 100;

        Assert.Equal(
            WorkshopSystem.GetProductionMultiplier(capped, MaterialType.Wood),
            WorkshopSystem.GetProductionMultiplier(settlement, MaterialType.Wood),
            precision: 10);
    }
}
