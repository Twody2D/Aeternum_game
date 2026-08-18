using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Events;

namespace Aeternum.WorldGen.Systems;

// Государства возникают эмерджентно: без карты и географии — просто когда одна
// династия становится достаточно большой и доминирует (простое большинство живых
// жителей) сразу в нескольких поселениях. Никто не назначает королевство заранее
public static class KingdomSystem
{
    private static readonly Random _random = new();

    private static int _nextId = 1;

    private const int MinDynastyMembersToFormKingdom = 20;
    private const int MinControlledSettlements = 2;

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

                    world.Events.Add(new WorldEvent
                    {
                        Year = world.CurrentYear,
                        Type = EventType.FallOfKingdom,
                        Description = $"{kingdom.Name} пало: династия {kingdom.Dynasty.Name} угасла, наследников не осталось"
                    });
                }

                continue; // Государство остаётся исторической записью, но больше не действует
            }

            var previousRuler = kingdom.Ruler;
            var newRuler = GetSenior(aliveMembers);
            kingdom.Ruler = newRuler;

            var becameVerb = newRuler.Gender == Gender.Female ? "стала" : "стал";
            var kingdomGenitive = KingdomNameGenitive(kingdom.Name);

            world.Events.Add(new WorldEvent
            {
                Year = world.CurrentYear,
                Type = EventType.Succession,
                Description = $"Новым правителем {kingdomGenitive} {becameVerb} {SurnameSystem.GetDisplayFullName(newRuler)}"
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
        if (_random.NextDouble() >= world.Settings.SuccessionCrisisChance)
        {
            return;
        }

        var rivalPool = aliveMembers.Where(m => m != newRuler).ToList();
        var casualtyCount = (int)(rivalPool.Count * world.Settings.CivilWarCasualtyRate);

        var casualties = rivalPool
            .OrderBy(_ => _random.Next())
            .Take(casualtyCount)
            .ToList();

        foreach (var casualty in casualties)
        {
            DeathSystem.Kill(casualty, world, DeathReason.War);
        }

        world.Events.Add(new WorldEvent
        {
            Year = world.CurrentYear,
            Type = EventType.CivilWar,
            Description = $"{kingdom.Name}: кризис наследования — трон перешёл не к прямому потомку, а к дальней родне " +
                          $"({SurnameSystem.GetDisplayFullName(newRuler)}). Среди наследников вспыхнула распря. Погибших: {casualties.Count}"
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

            var ruler = GetSenior(aliveMembers);

            var kingdom = new Kingdom
            {
                Id = _nextId++,
                Name = $"Королевство {dynasty.Name.Replace("Дом ", "")}",
                Dynasty = dynasty,
                Ruler = ruler,
                FoundedYear = world.CurrentYear,
                Settlements = controlled
            };

            world.Kingdoms.Add(kingdom);

            world.Events.Add(new WorldEvent
            {
                Year = world.CurrentYear,
                Type = EventType.CreationOfKingdom,
                Description = $"Образовано {kingdom.Name}. Первый правитель — {SurnameSystem.GetDisplayFullName(ruler)}"
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
        // и остаётся её родным домом (см. FamilySystem.CreateFamily)
        return world.Settlements
            .Where(s => s.Members.Count(m => m.Alive && dynasty.Members.Contains(m)) >= MinResidentsForControl)
            .ToList();
    }

    // Старший живой член династии — по минимальному году рождения
    private static Character GetSenior(List<Character> aliveMembers)
    {
        return aliveMembers.OrderBy(m => m.BirthYear).First();
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
