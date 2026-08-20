using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;

namespace Aeternum.WorldGen.Tests.Systems;

// Языковой барьер и вытеснение наречий. Проверяется и сам барьер в тех двух
// местах, где он поставлен (обмен товаром и дипломатия), и ассимиляция —
// включая случаи, когда её быть не должно
public class LanguageSystemTests
{
    private static readonly Language Common = new() { Id = 1, Name = "Старое наречие" };
    private static readonly Language Foreign = new() { Id = 2, Name = "Речь долин" };

    private static Settlement Village(int id, Language? language, double x = 0, double y = 0)
    {
        return new Settlement { Id = id, Name = $"Село{id}", Language = language, X = x, Y = y };
    }

    private static void Populate(World world, Settlement settlement, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var resident = new Character
            {
                Id = world.Characters.Count + 1,
                Name = $"Житель{world.Characters.Count}",
                LastName = "Тестов",
                Age = 30,
                Alive = true,
                LifeStage = LifeStage.Adult,
                Settlement = settlement,
                Profession = "Фермер"
            };

            settlement.Members.Add(resident);
            world.Characters.Add(resident);
        }
    }

    [Fact]
    public void SharesLanguage_TellsApartSameForeignAndUnknown()
    {
        var a = Village(1, Common);
        var b = Village(2, Common);
        var c = Village(3, Foreign);
        var mute = Village(4, null);

        Assert.True(LanguageSystem.SharesLanguage(a, b));
        Assert.False(LanguageSystem.SharesLanguage(a, c));
        Assert.False(LanguageSystem.SharesLanguage(a, mute));
        Assert.False(LanguageSystem.SharesLanguage(a, null));
    }

    [Fact]
    public void GetTradeFactor_ForeignTongueMakesTradeHarder()
    {
        Assert.True(LanguageSystem.GetTradeFactor(Village(1, Common), Village(2, Foreign))
                    < LanguageSystem.GetTradeFactor(Village(1, Common), Village(2, Common)));
    }

    [Fact]
    public void TradeProcess_ForeignNeighbourReceivesLess()
    {
        // Одинаковые излишки и одинаковая нужда — разница только в наречии
        var understood = RunTrade(sameLanguage: true);
        var foreign = RunTrade(sameLanguage: false);

        Assert.True(understood > foreign, $"через языковую границу должно доходить меньше: {foreign:0.#} против {understood:0.#}");
    }

    private static double RunTrade(bool sameLanguage)
    {
        var world = new World { CurrentYear = 10 };

        var donor = Village(1, Common);
        var needy = Village(2, sameLanguage ? Common : Foreign);

        donor.FoodStock = 1000;
        needy.FoodStock = -500;

        var ruler = new Character { Id = 1, Name = "Правитель", LastName = "Тестов", Age = 40, Alive = true, Settlement = donor };
        var dynasty = new Dynasty { Id = 1, Name = "Дом Тестов", Founder = ruler, FoundedYear = 1 };

        world.Characters.Add(ruler);
        world.Settlements.Add(donor);
        world.Settlements.Add(needy);
        world.Kingdoms.Add(new Kingdom
        {
            Id = 1,
            Name = "Королевство Тестов",
            Dynasty = dynasty,
            Ruler = ruler,
            FoundedYear = 1,
            Settlements = [donor, needy]
        });

        TradeSystem.Process(world);

        return needy.FoodStock;
    }

    [Fact]
    public void GetDiplomacyFactor_CommonTongueHelpsToAgree()
    {
        var ruler = new Character { Id = 1, Name = "Правитель", LastName = "Тестов", Alive = true, Settlement = Village(1, Common) };
        var peer = new Character { Id = 2, Name = "Сосед", LastName = "Тестов", Alive = true, Settlement = Village(2, Common) };
        var stranger = new Character { Id = 3, Name = "Чужак", LastName = "Тестов", Alive = true, Settlement = Village(3, Foreign) };

        var dynasty = new Dynasty { Id = 1, Name = "Дом Тестов", Founder = ruler, FoundedYear = 1 };

        Kingdom Realm(int id, Character head) => new()
        {
            Id = id,
            Name = $"Королевство{id}",
            Dynasty = dynasty,
            Ruler = head,
            FoundedYear = 1
        };

        var home = Realm(1, ruler);

        Assert.True(LanguageSystem.GetDiplomacyFactor(home, Realm(2, peer))
                    > LanguageSystem.GetDiplomacyFactor(home, Realm(3, stranger)));
    }

    [Fact]
    public void Process_SmallVillageTakesUpTheTongueOfItsCrowdedNeighbour()
    {
        var world = new World { CurrentYear = 10 };

        var village = Village(1, Common, x: 100, y: 100);
        var city = Village(2, Foreign, x: 150, y: 100);

        world.Settlements.Add(village);
        world.Settlements.Add(city);

        Populate(world, village, 5);
        Populate(world, city, 100);

        Rng.Initialize(seed: 1);

        for (var year = 0; year < 500 && village.Language == Common; year++)
        {
            world.CurrentYear = 10 + year;
            LanguageSystem.Process(world);
        }

        Assert.Equal(Foreign, village.Language);
    }

    [Fact]
    public void Process_OwnMajority_KeepsItsTongue()
    {
        // Чужих рядом много, но своих больше — речь держится
        var world = new World { CurrentYear = 10 };

        var town = Village(1, Common, x: 100, y: 100);
        var kin = Village(2, Common, x: 120, y: 100);
        var neighbour = Village(3, Foreign, x: 150, y: 100);

        world.Settlements.Add(town);
        world.Settlements.Add(kin);
        world.Settlements.Add(neighbour);

        Populate(world, town, 50);
        Populate(world, kin, 50);
        Populate(world, neighbour, 60);

        Rng.Initialize(seed: 1);

        for (var year = 0; year < 500; year++)
        {
            world.CurrentYear = 10 + year;
            LanguageSystem.Process(world);
        }

        Assert.Equal(Common, town.Language);
    }

    [Fact]
    public void Process_DistantNeighbour_IsNotHeard()
    {
        // Речь перенимают у тех, с кем живут бок о бок, а не у всего мира
        var world = new World { CurrentYear = 10 };

        var village = Village(1, Common, x: 0, y: 0);
        var farAwayCity = Village(2, Foreign, x: 900, y: 900);

        world.Settlements.Add(village);
        world.Settlements.Add(farAwayCity);

        Populate(world, village, 5);
        Populate(world, farAwayCity, 500);

        Rng.Initialize(seed: 1);

        for (var year = 0; year < 500; year++)
        {
            world.CurrentYear = 10 + year;
            LanguageSystem.Process(world);
        }

        Assert.Equal(Common, village.Language);
    }

    [Fact]
    public void Process_EmptySettlement_KeepsItsTongue()
    {
        // Менять речь некому
        var world = new World { CurrentYear = 10 };

        var ruins = Village(1, Common, x: 100, y: 100);
        var city = Village(2, Foreign, x: 150, y: 100);

        world.Settlements.Add(ruins);
        world.Settlements.Add(city);

        Populate(world, city, 200);

        Rng.Initialize(seed: 1);

        for (var year = 0; year < 200; year++)
        {
            world.CurrentYear = 10 + year;
            LanguageSystem.Process(world);
        }

        Assert.Equal(Common, ruins.Language);
    }

    [Fact]
    public void GeneratedLanguages_AreFewerThanCulturesButNeverSingle()
    {
        // Язык не должен быть вторым именем культуры — иначе он ничего
        // не добавляет; но и одно наречие на весь мир не даёт барьера вовсе
        var languages = Generators.LanguageGenerator.Create(cultureCount: 3);

        Assert.True(languages.Count >= 2, "при трёх народах барьер обязан существовать");
        Assert.True(languages.Count < 3, "наречий должно быть меньше, чем народов");
    }
}
