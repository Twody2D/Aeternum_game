using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Events;

namespace Aeternum.WorldGen.Systems;

// Государства возникают эмерджентно: без карты и географии — просто когда одна
// династия становится достаточно большой и доминирует (простое большинство живых
// жителей) сразу в нескольких поселениях. Никто не назначает королевство заранее
public static class KingdomSystem
{
    private static int _nextId = 1;

    private const int MinDynastyMembersToFormKingdom = 20;
    private const int MinControlledSettlements = 2;

    private const double ReputationWeight = 0.2; // Вес репутации династии (см. Dynasty.Reputation) в стабильности казны
    private const double HeirStability = 1.0; // Сколько спокойствия трону даёт признанный наследник (см. CourtSystem)
    private const double BastardPenalty = 1.5; // ...и сколько его отнимает происхождение правителя вне брака
    private const double ReligiousDiversityPenalty = 0.15; // Штраф к стабильности за каждое инаковерующее поселение в составе государства
    private const double CulturalDiversityPenalty = 0.15; // Штраф за каждое поселение с иной культурой — независим от религиозного, оба вычитаются

    public static void Process(World world)
    {
        UpdateExistingKingdoms(world);
        DetectNewKingdoms(world);
    }

    private static void UpdateExistingKingdoms(World world)
    {
        foreach (var kingdom in world.Kingdoms)
        {
            kingdom.Settlements = GetControlledSettlements(kingdom.Dynasty, world);

            if (kingdom.Ruler.Alive)
            {
                continue;
            }

            var aliveMembers = kingdom.Dynasty.Members.Where(m => m.Alive).ToList();

            if (aliveMembers.Count == 0)
            {
                if (kingdom.FallenYear == null)
                {
                    kingdom.FallenYear = world.CurrentYear;

                    // Не оставляем вассалов привязанными к несуществующему сюзерену —
                    // тот же принцип, что уже освобождает овдовевшего супруга в DeathSystem.Kill
                    foreach (var vassal in world.Kingdoms.Where(k => k.Suzerain == kingdom))
                    {
                        vassal.Suzerain = null;
                    }

                    world.Events.Add(new WorldEvent
                    {
                        Year = world.CurrentYear,
                        Type = EventType.FallOfKingdom,
                        Description = $"{kingdom.Name} пало: династия {kingdom.Dynasty.Name} угасла, наследников не осталось",
                        Kingdoms = [kingdom]
                    });
                }

                continue; // Государство остаётся исторической записью, но больше не действует
            }

            var previousRuler = kingdom.Ruler;
            var newRuler = SuccessionSystem.PickHeir(aliveMembers, kingdom, previousRuler);
            kingdom.Ruler = newRuler;

            var becameVerb = newRuler.Gender == Gender.Female ? "стала" : "стал";
            var kingdomGenitive = KingdomNameGenitive(kingdom.Name);

            world.Events.Add(new WorldEvent
            {
                Year = world.CurrentYear,
                Type = EventType.Succession,
                Description = $"Новым правителем {kingdomGenitive} {becameVerb} {SurnameSystem.GetDisplayFullName(newRuler)}",
                Kingdoms = [kingdom]
            });

            var isDirectHeir = newRuler.Father == previousRuler || newRuler.Mother == previousRuler;

            if (!isDirectHeir)
            {
                TryTriggerSuccessionCrisis(kingdom, newRuler, aliveMembers, world);
            }
        }
    }

    // Трон ушёл не к прямому ребёнку покойного правителя, а в боковую ветвь —
    // с некоторым шансом среди прочей родни вспыхивает распря за само наследство.
    // Не путать с MurderSystem: там — заговор против ещё живого правителя
    private static void TryTriggerSuccessionCrisis(Kingdom kingdom, Character newRuler, List<Character> aliveMembers, World world)
    {
        // Богатая казна (см. TributeSystem) даёт правителю чем откупиться от
        // претендентов — снижает шанс, но никогда не убирает риск полностью.
        // Легендарная история рода (см. DeathSystem) добавляет легитимности сверху,
        // а инаковерующие поселения в составе государства (см. Settlement.Religion) её подрывают
        var dissentingSettlements = kingdom.Settlements.Count(s => s.Religion != null && s.Religion != kingdom.Ruler.Settlement?.Religion);
        var dissentingCultureSettlements = kingdom.Settlements.Count(s => s.Culture != null && s.Culture != kingdom.Ruler.Settlement?.Culture);
        // Признанный наследник — сам по себе довод против распри: спорить
        // становится не о чем (см. CourtSystem)
        var heirClarity = CourtSystem.HasOffice(kingdom, CourtOffice.Heir) ? HeirStability : 0;

        // Корона на голове рождённого вне брака — сама по себе повод оспорить
        // её у соперников (см. BastardSystem)
        if (BastardSystem.IsBastard(newRuler))
        {
            heirClarity -= BastardPenalty;
        }

        var stability = Math.Max(0, heirClarity + (kingdom.FoodTreasury + kingdom.GoldTreasury) / world.Settings.TreasuryStabilityDivisor
                                     + kingdom.Dynasty.Reputation * ReputationWeight
                                     - dissentingSettlements * ReligiousDiversityPenalty
                                     - dissentingCultureSettlements * CulturalDiversityPenalty);
        var effectiveChance = world.Settings.SuccessionCrisisChance / (1 + stability);

        if (Rng.NextDouble() >= effectiveChance)
        {
            return;
        }

        var rivalPool = aliveMembers.Where(m => m != newRuler).ToList();
        var casualtyCount = (int)(rivalPool.Count * world.Settings.CivilWarCasualtyRate);

        var casualties = rivalPool
            .OrderBy(_ => Rng.Next())
            .Take(casualtyCount)
            .ToList();

        foreach (var casualty in casualties)
        {
            DeathSystem.Kill(casualty, world, DeathReason.War);
        }

        // Выжившие проигравшие не получили трон и затаили обиду на нового правителя
        foreach (var survivor in rivalPool.Except(casualties))
        {
            MurderSystem.AddEnmity(survivor, newRuler);
        }

        world.Events.Add(new WorldEvent
        {
            Year = world.CurrentYear,
            Type = EventType.CivilWar,
            Description = $"{kingdom.Name}: кризис наследования — трон перешёл не к прямому потомку, а к дальней родне " +
                          $"({SurnameSystem.GetDisplayFullName(newRuler)}). Среди наследников вспыхнула распря. Погибших: {casualties.Count}",
            Kingdoms = [kingdom]
        });
    }

    private static void DetectNewKingdoms(World world)
    {
        var dynastiesWithKingdom = world.Kingdoms.Select(k => k.Dynasty).ToHashSet();

        foreach (var dynasty in world.Dynasties)
        {
            if (dynastiesWithKingdom.Contains(dynasty))
            {
                continue;
            }

            var aliveMembers = dynasty.Members.Where(m => m.Alive).ToList();

            if (aliveMembers.Count < MinDynastyMembersToFormKingdom)
            {
                continue;
            }

            var controlled = GetControlledSettlements(dynasty, world);

            if (controlled.Count < MinControlledSettlements)
            {
                continue;
            }

            // Первого правителя выбирает не обычай наследования, а сама жизнь:
            // передачи трона ещё не было, у дома просто становится во главе старейший
            var ruler = SuccessionSystem.PickSenior(aliveMembers);

            var kingdom = new Kingdom
            {
                Id = _nextId++,
                Name = $"Королевство {dynasty.Name.Replace("Дом ", "")}",
                Dynasty = dynasty,
                Ruler = ruler,
                FoundedYear = world.CurrentYear,
                Settlements = controlled,
                TributeRate = world.Settings.TributeRate // Пока правитель не осмотрелся — как у всех
            };

            world.Kingdoms.Add(kingdom);

            world.Events.Add(new WorldEvent
            {
                Year = world.CurrentYear,
                Type = EventType.CreationOfKingdom,
                Description = $"Образовано {kingdom.Name}. Первый правитель — {SurnameSystem.GetDisplayFullName(ruler)}",
                Kingdoms = [kingdom]
            });
        }
    }

    // Поселение под контролем династии, если там заметное число живых членов —
    // не обязательно большинство (при браках преимущественно внутри своего
    // поселения абсолютное большинство сразу в двух деревнях почти недостижимо)
    private const int MinResidentsForControl = 3;

    private static List<Settlement> GetControlledSettlements(Dynasty dynasty, World world)
    {
        // Dynasty.Members — источник истины о принадлежности (включает вошедших в дом
        // браком), а не Character.Dynasty: это поле у невесты при браке не меняется
        // и остаётся её родным домом (см. FamilySystem.CreateFamily).
        // Восставшее поселение присутствие родни короне не возвращает — пока длится
        // неповиновение, оно не считается подконтрольным (см. RebellionSystem)
        return world.Settlements
            .Where(s => !RebellionSystem.IsRebelling(s, world) &&
                        s.Members.Count(m => m.Alive && dynasty.Members.Contains(m)) >= MinResidentsForControl)
            .ToList();
    }

    // Восстанавливает счётчик Id после загрузки сохранения
    public static void SetNextId(int value)
    {
        _nextId = value;
    }

    // "Королевство X" -> "Королевства X" для родительного падежа в тексте события.
    // Прибавленная фамилия династии не склоняется — та же договорённость, что и у Dynasty.Name ("Дом X")
    private static string KingdomNameGenitive(string kingdomName)
    {
        return kingdomName.Replace("Королевство ", "Королевства ");
    }
}
