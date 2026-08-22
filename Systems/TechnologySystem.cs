using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Events;

namespace Aeternum.WorldGen.Systems;

// Развитие знаний. Учёные, лекари и книжники (ProfessionCategory.Knowledge) до
// сих пор были профессиями почти без последствий: школы повышали шанс их
// появления, но само их присутствие ничего в мире не меняло. Здесь их труд
// наконец накапливается — не в отдельном "древе технологий", а одним числом
// (World.Knowledge), которое растёт, пока в мире есть кому думать.
//
// Знание общее на весь мир, а не на поселение: книга, ремесленный приём или
// способ лечения расходятся с людьми и товаром, и держать их запертыми в
// границах одной деревни было бы страннее, чем считать общим достоянием.
//
// Накопленное знание переходит в эпохи — именованные пороги, каждый со своим
// множителем к производству и медицине. Знание не убывает: даже если мыслящих
// не осталось, добытое прежде не забывается
public static class TechnologySystem
{
    private const double KnowledgePerScholar = 1.0; // Вклад одного живого носителя знания за год
    private const double SchoolContribution = 0.5; // Школа помогает копить знание и сама по себе
    private const double ChancellorContribution = 2.0; // Советник при короне двигает мысль сильнее книжника без покровителя

    // Усердный государь (см. Trait.Hardworking) — тот же нрав, что уже держит
    // трон крепче (см. KingdomSystem) — сам покровительствует учёности, не
    // дожидаясь, пока при дворе найдётся советник
    private const double HardworkingRulerContribution = 1.5;

    // Пороги эпох и их отдача. Первая — то состояние, в котором мир жил до сих пор,
    // поэтому её множитель равен единице: без неё прежний баланс сместился бы на ровном месте.
    //
    // Пороги подобраны по замерам, а не назначены на глаз: за век мир накапливает
    // от ~220 до ~800 знания в зависимости от того, как сложилась его история.
    // Поэтому век ремёсел берут почти все, век наук — только преуспевшие, а век
    // просвещения остаётся тем, до чего надо ещё дожить
    private static readonly (double Threshold, string Name, double Bonus)[] Eras =
    {
        (0, "Тёмные века", 1.0),
        (250, "Век ремёсел", 1.1),
        (600, "Век наук", 1.2),
        (1200, "Век просвещения", 1.35)
    };

    public static void Process(World world)
    {
        // Считаем не головы, а вклад: седой книжник продвигает мир дальше,
        // чем вчерашний школяр (см. ProfessionSystem.GetMastery)
        var scholars = world.Characters
            .Where(c => c.Alive &&
                        c.LifeStage is LifeStage.Adult or LifeStage.Elder &&
                        ProfessionSystem.GetCategory(c.Profession) == ProfessionCategory.Knowledge)
            .Sum(c => ProfessionSystem.GetMastery(c, world));

        var schools = world.Settlements.Sum(s => s.Schools);

        // Учёный, приближённый к трону, работает не только на себя: у него есть
        // и средства, и заказ (см. CourtSystem)
        var chancellors = world.Kingdoms
            .Where(k => k.FallenYear == null)
            .Sum(k => CourtSystem.HasOffice(k, CourtOffice.Chancellor)
                ? CourtSystem.GetOfficeStrength(k, CourtOffice.Chancellor, world) * ChancellorContribution
                : 0);

        var patronRulers = world.Kingdoms
            .Count(k => k.FallenYear == null && k.Ruler.Traits.Contains(Trait.Hardworking));

        var previousEra = GetEraName(world.Knowledge);

        world.Knowledge += scholars * KnowledgePerScholar + schools * SchoolContribution + chancellors
                           + patronRulers * HardworkingRulerContribution;

        var currentEra = GetEraName(world.Knowledge);

        if (currentEra != previousEra)
        {
            world.Events.Add(new WorldEvent
            {
                Year = world.CurrentYear,
                Type = EventType.Era,
                Description = $"Мир вступил в новую эпоху: {currentEra}"
            });
        }
    }

    public static string GetEraName(double knowledge)
    {
        return GetEra(knowledge).Name;
    }

    // Множитель к производству и лечению, добытый накопленным знанием
    // (см. EconomySystem, HospitalSystem)
    public static double GetProductionMultiplier(World world)
    {
        return GetEra(world.Knowledge).Bonus;
    }

    // Во сколько раз дешевле обходится постройка при нынешнем знании
    // (см. HousingSystem и остальные строительные системы). Обратная сторона
    // той же величины: чем лучше инструмент и приём, тем меньше материала уходит
    // впустую. Множитель, а не вычитаемое, — иначе дешёвые постройки уходили бы
    // в отрицательную стоимость раньше дорогих
    public static double GetBuildCostMultiplier(World world)
    {
        return 1 / GetProductionMultiplier(world);
    }

    private static (double Threshold, string Name, double Bonus) GetEra(double knowledge)
    {
        return Eras.Last(e => knowledge >= e.Threshold);
    }
}
