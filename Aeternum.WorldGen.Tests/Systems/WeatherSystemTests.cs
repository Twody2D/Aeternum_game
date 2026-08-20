using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;

namespace Aeternum.WorldGen.Tests.Systems;

// Погода делает годы разными: без неё GetFactor всегда вернул бы 1.0, и
// урожай тысячного года не отличался бы от первого. Проверяется само блуждание
// (шаг, тяга к среднему, границы) и то, что на нём действительно завязан урожай
public class WeatherSystemTests
{
    [Fact]
    public void Process_MovesFactorAwayFromDefault()
    {
        var world = new World { CurrentYear = 1 };

        Rng.Initialize(seed: 1);

        for (var year = 0; year < 20; year++)
        {
            world.CurrentYear++;
            WeatherSystem.Process(world);
        }

        Assert.NotEqual(1.0, world.WeatherFactor);
    }

    [Fact]
    public void Process_StaysWithinBounds()
    {
        var world = new World { CurrentYear = 1 };

        Rng.Initialize(seed: 7);

        for (var year = 0; year < 2000; year++)
        {
            world.CurrentYear++;
            WeatherSystem.Process(world);

            Assert.True(world.WeatherFactor >= 0.6, $"погода не должна проваливаться ниже нижней границы: {world.WeatherFactor}");
            Assert.True(world.WeatherFactor <= 1.4, $"погода не должна превышать верхнюю границу: {world.WeatherFactor}");
        }
    }

    [Fact]
    public void Process_DriftsGradually_NotJumpingBetweenExtremesEachYear()
    {
        // Год блуждает небольшим шагом — иначе за щедрым годом тут же мог бы
        // прийти скудный, и никакой засухи в несколько лет подряд не вышло бы
        var world = new World { CurrentYear = 1 };

        Rng.Initialize(seed: 3);

        var previous = world.WeatherFactor;

        for (var year = 0; year < 200; year++)
        {
            world.CurrentYear++;
            WeatherSystem.Process(world);

            Assert.True(Math.Abs(world.WeatherFactor - previous) < 0.2, "погода не должна скакать между крайностями за один год");
            previous = world.WeatherFactor;
        }
    }

    [Fact]
    public void Process_TendsBackTowardTheMean()
    {
        // Загнанная в крайность погода за много лет должна вернуться ближе к среднему,
        // а не застрять в углу шкалы, — иначе не годы отличались бы друг от друга,
        // а мир навсегда разделился бы на "везучие" и "невезучие" сиды
        var world = new World { CurrentYear = 1, WeatherFactor = 1.4 };

        Rng.Initialize(seed: 11);

        for (var year = 0; year < 100; year++)
        {
            world.CurrentYear++;
            WeatherSystem.Process(world);
        }

        Assert.True(world.WeatherFactor < 1.2, $"за сто лет погода должна отойти от крайности: осталась {world.WeatherFactor}");
    }

    [Fact]
    public void EconomyProcess_HarshWeather_YieldsLessFoodThanBountiful()
    {
        var harsh = FoodProducedAt(weatherFactor: 0.6);
        var bountiful = FoodProducedAt(weatherFactor: 1.4);

        Assert.True(bountiful > harsh, $"щедрый год должен давать больше еды, чем суровый: {bountiful} против {harsh}");
    }

    private static double FoodProducedAt(double weatherFactor)
    {
        var world = new World { CurrentYear = 1, WeatherFactor = weatherFactor };
        var settlement = new Settlement { Id = 1, Name = "Поселение", Y = ClimateSystem.MapSize / 2 };

        for (var i = 0; i < 10; i++)
        {
            var farmer = new Character
            {
                Id = i + 1,
                Name = $"Житель{i}",
                LastName = "Тестов",
                Age = 30,
                Alive = true,
                LifeStage = LifeStage.Adult,
                Settlement = settlement,
                Profession = "Фермер"
            };

            settlement.Members.Add(farmer);
            world.Characters.Add(farmer);
        }

        world.Settlements.Add(settlement);

        EconomySystem.Process(world);

        return settlement.FoodStock;
    }

    [Fact]
    public void Process_ExtremeYear_RaisesWeatherEvent()
    {
        // У нижней границы шаг и тяга к среднему вместе не могут выбраться выше
        // порога сурового года — событие обязано случиться при любом броске кубика
        var world = new World { CurrentYear = 1, WeatherFactor = 0.6 };

        Rng.Initialize(seed: 42);

        world.CurrentYear++;
        WeatherSystem.Process(world);

        Assert.Contains(world.Events, e => e.Type == EventType.Weather);
    }
}
