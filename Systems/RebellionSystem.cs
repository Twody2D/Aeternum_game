using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Events;

namespace Aeternum.WorldGen.Systems;

// Восстания: до сих пор действовать могли только элиты — короли воевали
// (WarSystem), родня плела заговоры (MurderSystem), претенденты резались за
// трон (KingdomSystem), а население всё это молча терпело. Здесь голос
// получают сами жители: поселение, которому невыносимо, отказывает короне
// в повиновении.
//
// Ни одного нового источника недовольства не заводим — все поводы уже есть в
// данных и уже используются другими системами: голод (FoodStock < 0, см.
// EconomySystem), чужая вера и чужая культура правителя (те же сравнения, что
// в KingdomSystem считает расколом государства) и затяжная осада (SiegeYears).
//
// Восставшее поселение не создаёт своего государства — оно просто перестаёт
// подчиняться: пока восстание длится, дань с него не идёт (TributeSystem), а
// само оно выпадает из подконтрольных территорий короны
public static class RebellionSystem
{
    private static readonly Random _random = new();

    private const double HungerWeight = 0.10; // Голодное поселение — первый и самый частый повод
    private const double AlienFaithWeight = 0.06; // Чужая вера правителя
    private const double AlienCultureWeight = 0.06; // Чужие обычаи правителя
    private const double SiegeWeight = 0.03; // ...за каждый год осады

    private const int MaxSiegeYearsCounted = 5; // Дальше терпение уже не ухудшается

    private const double SuppressionCost = 40; // Во что обходится короне поход на мятежное поселение
    private const double SuppressionPerSoldier = 0.05; // Вклад одного воина короны в шанс подавить мятеж
    private const double MaxSuppressionChance = 0.8; // Даже сильнейшая корона не давит мятеж наверняка
    private const double SuppressionCasualtyRate = 0.1; // Доля жителей мятежного поселения, гибнущих при усмирении

    public static void Process(World world)
    {
        TrySuppress(world);
        TryRebel(world);
    }

    // Корона отвечает на мятеж — но только если ей есть чем: поход оплачивается
    // казной (см. TributeSystem), а давят его те же профессии Military, что
    // держат оборону в WarSystem. Обедневшее или обезоруженное государство
    // смотрит на мятеж бессильно, и тот держится до конца своего срока
    private static void TrySuppress(World world)
    {
        foreach (var settlement in world.Settlements)
        {
            var crown = settlement.RebellingAgainst;

            if (crown == null)
            {
                continue;
            }

            // Срок вышел сам либо корона пала — связь снимаем, чтобы мятеж не
            // остался висеть на несуществующем государстве
            if (!IsRebelling(settlement, world) || crown.FallenYear != null)
            {
                settlement.RebellingAgainst = null;
                continue;
            }

            if (crown.FoodTreasury < SuppressionCost)
            {
                continue; // Нечем платить войску — поход не состоится
            }

            var soldiers = crown.Settlements
                .SelectMany(s => s.Members)
                .Count(m => m.Alive && ProfessionSystem.GetCategory(m.Profession) == ProfessionCategory.Military);

            var chance = Math.Min(MaxSuppressionChance, soldiers * SuppressionPerSoldier);

            crown.FoodTreasury -= SuppressionCost; // Поход стоит казне независимо от исхода

            if (_random.NextDouble() >= chance)
            {
                continue;
            }

            Suppress(settlement, crown, world);
        }
    }

    private static void Suppress(Settlement settlement, Kingdom crown, World world)
    {
        var residents = settlement.Members.Where(m => m.Alive).ToList();
        var casualtyCount = (int)(residents.Count * SuppressionCasualtyRate);

        var casualties = residents
            .OrderBy(_ => _random.Next())
            .Take(casualtyCount)
            .ToList();

        foreach (var casualty in casualties)
        {
            DeathSystem.Kill(casualty, world, DeathReason.War);
        }

        settlement.RebellingUntilYear = 0;
        settlement.RebellingAgainst = null;

        world.Events.Add(new WorldEvent
        {
            Year = world.CurrentYear,
            Type = EventType.Suppression,
            Description = $"{crown.Name}: мятеж в {settlement.Name} подавлен. Погибших: {casualties.Count}"
        });
    }

    private static void TryRebel(World world)
    {
        foreach (var kingdom in world.Kingdoms)
        {
            if (kingdom.FallenYear != null)
            {
                continue;
            }

            foreach (var settlement in kingdom.Settlements.ToList())
            {
                if (settlement.RebellingUntilYear > world.CurrentYear)
                {
                    continue; // Уже восстало — второй раз подниматься не нужно
                }

                if (settlement.Members.Count(m => m.Alive) == 0)
                {
                    continue; // Восставать некому
                }

                var chance = GetDiscontent(settlement, kingdom);

                if (chance <= 0 || _random.NextDouble() >= chance)
                {
                    continue;
                }

                Rebel(settlement, kingdom, world);
            }
        }
    }

    // Сумма уже существующих в мире поводов быть недовольным короной
    private static double GetDiscontent(Settlement settlement, Kingdom kingdom)
    {
        var discontent = 0.0;

        if (settlement.FoodStock < 0)
        {
            discontent += HungerWeight;
        }

        var rulerSettlement = kingdom.Ruler.Settlement;

        if (settlement.Religion != null && rulerSettlement?.Religion != null && settlement.Religion != rulerSettlement.Religion)
        {
            discontent += AlienFaithWeight;
        }

        if (settlement.Culture != null && rulerSettlement?.Culture != null && settlement.Culture != rulerSettlement.Culture)
        {
            discontent += AlienCultureWeight;
        }

        discontent += Math.Min(settlement.SiegeYears, MaxSiegeYearsCounted) * SiegeWeight;

        return discontent;
    }

    private static void Rebel(Settlement settlement, Kingdom kingdom, World world)
    {
        settlement.RebellingUntilYear = world.CurrentYear + world.Settings.RebellionDuration;
        settlement.RebellingAgainst = kingdom;

        // Label-формат ("{Королевство}: ...") вместо склонения названия государства —
        // та же договорённость, что у DisasterSystem/WarSystem
        world.Events.Add(new WorldEvent
        {
            Year = world.CurrentYear,
            Type = EventType.Rebellion,
            Description = $"{kingdom.Name}: {settlement.Name} отказало короне в повиновении"
        });
    }

    // Поселение в открытом неповиновении короне — не платит дань и не считается
    // подконтрольной территорией (см. TributeSystem, KingdomSystem)
    public static bool IsRebelling(Settlement settlement, World world)
    {
        return settlement.RebellingUntilYear > world.CurrentYear;
    }
}
