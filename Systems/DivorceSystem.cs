using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Events;

namespace Aeternum.WorldGen.Systems;

// Развод: раз в год у бездетных браков, переживших порог ChildlessDivorceThresholdYears,
// есть небольшой шанс распасться. И муж, и жена освобождаются для нового брака —
// как и при вдовстве (см. DeathSystem.Kill), но без смерти.
//
// Распадается не всякий бездетный брак: та же взаимная склонность, что свела
// этих двоих (см. MarriageSystem.GetAffinity), потом их и удерживает. Союз
// по расчёту рвётся втрое легче, чем союз по сердцу
public static class DivorceSystem
{
    public static void Process(World world)
    {
        var activeMarriages = world.Families.Where(f =>
            f.Father.Alive &&
            f.Mother.Alive &&
            f.Father.CurrentFamily == f &&
            f.Mother.CurrentFamily == f &&
            f.Children.Count == 0 &&
            world.CurrentYear - f.FormedYear >= world.Settings.ChildlessDivorceThresholdYears);

        foreach (var family in activeMarriages.ToList())
        {
            // Чем выше склонность, тем ниже риск: единица — полное безразличие
            var effectiveChance = world.Settings.DivorceChance / MarriageSystem.GetAffinity(family.Father, family.Mother, world);

            if (Rng.NextDouble() >= effectiveChance)
            {
                continue;
            }

            Divorce(family, world);
        }
    }

    private static void Divorce(Family family, World world)
    {
        family.Father.CurrentFamily = null;
        family.Mother.CurrentFamily = null;

        world.Events.Add(new WorldEvent
        {
            Year = world.CurrentYear,
            Type = EventType.Divorce,
            Description = $"{SurnameSystem.GetDisplayFullName(family.Father)} и " +
            $"{SurnameSystem.GetDisplayFullName(family.Mother)} расстались"
        });
    }
}
