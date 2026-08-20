using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;

namespace Aeternum.WorldGen.Tests.Systems;

// Выбор профессии — цепочка приоритетов, и проверять её можно только по
// распределению: каждый отдельный бросок ничего не доказывает. Поэтому все
// тесты здесь считают доли на большой выборке при фиксированном зерне
public class ProfessionSystemTests
{
    private const int Sample = 2000;

    // Поселение, где все обязательные профессии уже заняты: иначе GetRandom
    // сначала закрывает нехватку и до специализации дело не доходит
    private static Settlement SettledSettlement(double y = 500)
    {
        var settlement = new Settlement { Id = 1, Name = "Тестовка", X = 500, Y = y };

        var essentials = new[] { "Фермер", "Кузнец", "Столяр", "Каменщик", "Ткач", "Гончар" };

        for (var i = 0; i < essentials.Length; i++)
        {
            settlement.Members.Add(new Character
            {
                Id = i + 1,
                Name = $"Житель{i}",
                LastName = "Тестов",
                Age = 30,
                Alive = true,
                LifeStage = LifeStage.Adult,
                Settlement = settlement,
                Profession = essentials[i]
            });
        }

        return settlement;
    }

    private static double ShareOf(Settlement settlement, MaterialType material, int seed = 1)
    {
        Rng.Initialize(seed);

        var matching = 0;

        for (var i = 0; i < Sample; i++)
        {
            var profession = ProfessionSystem.GetRandom(settlement: settlement);

            if (ProfessionSystem.GetMaterialProduction(profession).Type == material)
            {
                matching++;
            }
        }

        return matching / (double)Sample;
    }

    private static double ShareOfCategory(Settlement settlement, ProfessionCategory category, int seed = 1)
    {
        Rng.Initialize(seed);

        var matching = 0;

        for (var i = 0; i < Sample; i++)
        {
            if (ProfessionSystem.GetCategory(ProfessionSystem.GetRandom(settlement: settlement)) == category)
            {
                matching++;
            }
        }

        return matching / (double)Sample;
    }

    [Fact]
    public void GetRandom_WorkshopTown_RaisesItsOwnCraft()
    {
        var plain = SettledSettlement();
        var forge = SettledSettlement();
        forge.Workshops[MaterialType.Metal] = 3;

        Assert.True(ShareOf(forge, MaterialType.Metal) > ShareOf(plain, MaterialType.Metal),
            "город с кузницами обязан растить кузнецов чаще обычного");
    }

    [Fact]
    public void GetRandom_WorkshopTown_DoesNotRaiseOtherCrafts()
    {
        // Кузницы растят кузнецов, а не ткачей — иначе это была бы просто
        // прибавка к ремеслу вообще, а не специализация места
        var plain = SettledSettlement();
        var forge = SettledSettlement();
        forge.Workshops[MaterialType.Metal] = 3;

        Assert.True(ShareOf(forge, MaterialType.Textile) <= ShareOf(plain, MaterialType.Textile) + 0.02);
    }

    [Fact]
    public void GetRandom_ManyWorkshops_StillLeaveRoomForOtherTrades()
    {
        // Петля "мастерские растят ремесленников, ремесленники строят мастерские"
        // без потолка схлопнула бы поселение в одно-единственное занятие
        var forge = SettledSettlement();
        forge.Workshops[MaterialType.Metal] = 100;

        Assert.True(ShareOf(forge, MaterialType.Metal) < 0.7, "у города должно оставаться место и для других занятий");
    }

    [Fact]
    public void GetRandom_FertileLand_RaisesFarming()
    {
        // Плодородие зависит только от координат (см. ClimateSystem), поэтому
        // два одинаковых поселения различаются здесь одним лишь местом
        var fertile = SettledSettlement(y: ClimateSystem.MapSize / 2);
        var barren = SettledSettlement(y: 0);

        Assert.True(ClimateSystem.GetFertility(fertile) > ClimateSystem.GetFertility(barren), "проверяем на заведомо разной земле");
        Assert.True(ShareOfCategory(fertile, ProfessionCategory.FoodProducer)
                    > ShareOfCategory(barren, ProfessionCategory.FoodProducer));
    }

    [Fact]
    public void GetRandom_MissingEssentialProfession_OutweighsSpecialization()
    {
        // Без кузнеца поселение не выживет, каким бы ни был его уклад:
        // нехватка обязательного ремесла перевешивает всю цепочку
        var settlement = new Settlement { Id = 1, Name = "Тестовка", X = 500, Y = 500 };
        settlement.Workshops[MaterialType.Textile] = 10;

        Rng.Initialize(seed: 1);

        var essentials = new[] { "Фермер", "Кузнец", "Столяр", "Каменщик", "Ткач", "Гончар" };

        for (var i = 0; i < 50; i++)
        {
            Assert.Contains(ProfessionSystem.GetRandom(settlement: settlement), essentials);
        }
    }

    [Fact]
    public void GetRandom_InheritedProfession_IsPassedOnAtLeastSometimes()
    {
        // Семейное дело идёт раньше уклада места — проверяем, что специализация
        // его не вытеснила
        var settlement = SettledSettlement();
        settlement.Workshops[MaterialType.Metal] = 3;

        Rng.Initialize(seed: 1);

        var inherited = 0;

        for (var i = 0; i < Sample; i++)
        {
            if (ProfessionSystem.GetRandom(settlement: settlement, inheritedProfession: "Пекарь") == "Пекарь")
            {
                inherited++;
            }
        }

        Assert.True(inherited > Sample / 10, $"ремесло родителя должно передаваться заметно чаще, а вышло {inherited} из {Sample}");
    }
}
