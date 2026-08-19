using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Events;

namespace Aeternum.WorldGen.Systems;

// Переезд между поселениями вне брака: одинокие бездетные взрослые из
// голодающего поселения могут уехать туда, где запас еды больше.
// Брак тоже переселяет (невеста — в поселение мужа, см. MarriageSystem),
// но это отдельный случай и здесь не обрабатывается
public static class MigrationSystem
{
    private static readonly Random _random = new();

    private const double BraveMigrationMultiplier = 1.5;
    private const double PrudentMigrationMultiplier = 0.5;

    public static void Process(World world)
    {
        if (world.Settlements.Count < 2)
        {
            return; // Мигрировать некуда
        }

        var candidates = world.Characters.Where(c =>
            c.Alive &&
            c.LifeStage == LifeStage.Adult &&
            c.CurrentFamily == null &&
            c.Settlement != null &&
            c.Settlement.FoodStock < 0 &&
            !HasChildren(c, world));

        foreach (var character in candidates.ToList())
        {
            var chance = world.Settings.MigrationChance;

            if (character.Traits.Contains(Trait.Brave))
            {
                chance *= BraveMigrationMultiplier;
            }

            if (character.Traits.Contains(Trait.Prudent))
            {
                chance *= PrudentMigrationMultiplier;
            }

            chance *= HousingSystem.GetHousingFactor(character.Settlement);

            if (_random.NextDouble() >= chance)
            {
                continue;
            }

            var origin = character.Settlement!;

            var destination = world.Settlements
                .Where(s => s != origin)
                .OrderByDescending(s => s.FoodStock)
                .First();

            if (destination.FoodStock <= origin.FoodStock)
            {
                continue; // Нигде не лучше — остаёмся
            }

            Relocate(character, origin, destination, world);
        }
    }

    // Не даём одинокому родителю уехать, бросив детей — переезжают только бездетные
    private static bool HasChildren(Character character, World world)
    {
        return world.Families.Any(f =>
            (f.Father == character || f.Mother == character) &&
            f.Children.Count > 0);
    }

    private static void Relocate(Character character, Settlement origin, Settlement destination, World world)
    {
        character.Settlement = destination;
        destination.Members.Add(character);

        var verb = character.Gender == Gender.Female ? "переехала" : "переехал";

        // "из X в Y" потребовало бы падежей произвольных названий поселений,
        // которые мы не умеем склонять — используем стрелку вместо предлогов
        world.Events.Add(new WorldEvent
        {
            Year = world.CurrentYear,
            Type = EventType.Migration,
            Description = $"{SurnameSystem.GetDisplayFullName(character)} {verb}: {origin.Name} → {destination.Name}"
        });
    }
}
