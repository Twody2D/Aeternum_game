using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Events;

namespace Aeternum.WorldGen.Systems;

// Погода года. До сих пор урожай зависел только от того, где стоит поселение
// (см. ClimateSystem) — год на год не приходился: тысячный год родил бы ровно
// столько же, сколько первый. Здесь у самого года появляется нрав, общий для
// всего мира, как и положено погоде, а не месту.
//
// Погода не бросает кубик заново каждый год — тогда за щедрым годом тут же
// шёл бы скудный, и ни одной засухи длиной в несколько лет подряд не
// вышло бы. Вместо этого год блуждает от прошлогоднего небольшим шагом и
// тянется обратно к среднему — отсюда сами собой получаются то полоса лет
// теплее обычного, то вереница недородов, без всякой отдельной логики
// "засухи". Это ровно то, чего не даёт неурожай из DisasterSystem — тот
// бьёт резко и по одному поселению, а здесь — плавный фон на весь мир,
// на котором такой резкий удар только случается
public static class WeatherSystem
{
    private const double StepSize = 0.06; // Насколько сильно погода может качнуться за один год
    private const double ReversionRate = 0.15; // Как быстро погода тянется обратно к среднему году
    private const double MinFactor = 0.6; // Худший год — почти как неурожайный край карты (см. ClimateSystem)
    private const double MaxFactor = 1.4;

    // Пороги подобраны по факту: блуждание держится плотно у среднего (за 300 лет
    // на seed=42 держалось в 0.785–1.159), а исходные 0.75/1.25 почти никогда не
    // пробивались — событие оказалось бы мёртвым. При 0.88/1.12 приметный год
    // случается около 7% лет (22 из 300 на том же прогоне)
    private const double HarshWinterThreshold = 0.88; // Ниже — год приметно суров
    private const double BountifulYearThreshold = 1.12; // Выше — год приметно щедр

    public static void Process(World world)
    {
        var step = (Rng.NextDouble() * 2 - 1) * StepSize;
        var reversion = (1.0 - world.WeatherFactor) * ReversionRate;

        world.WeatherFactor = Math.Clamp(world.WeatherFactor + step + reversion, MinFactor, MaxFactor);

        if (world.WeatherFactor <= HarshWinterThreshold)
        {
            world.Events.Add(new WorldEvent
            {
                Year = world.CurrentYear,
                Type = EventType.Weather,
                Description = "По всему миру суровый год: урожай скуден"
            });
        }
        else if (world.WeatherFactor >= BountifulYearThreshold)
        {
            world.Events.Add(new WorldEvent
            {
                Year = world.CurrentYear,
                Type = EventType.Weather,
                Description = "По всему миру щедрый год: урожай обилен"
            });
        }
    }

    // Множитель к производству еды в этом году (см. EconomySystem)
    public static double GetFactor(World world)
    {
        return world.WeatherFactor;
    }
}
