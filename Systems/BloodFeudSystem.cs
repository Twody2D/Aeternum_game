using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;

namespace Aeternum.WorldGen.Systems;

// Вражда (Character.Enemies) сама по себе никогда не гаснет — но до сих пор
// гасла вместе со смертью одной из сторон: врагов у покойного было сколько было,
// а дальше это переставало что-либо значить. Насильственная смерть — другое дело:
// у убитого или павшего на войне была причина враждовать, и с некоторым шансом
// повод переходит детям, ещё живым на момент гибели родителя.
//
// Не путать с MurderSystem.GetBereaved: там — свежая вражда к конкретному
// заговорщику, гарантированная и bound к самому факту цареубийства. Здесь —
// куда более общий случай, унаследованные же старые счёты покойного, при любой
// насильственной смерти (не только правителя) и не обязательно связанные с тем,
// кто и как убил
public static class BloodFeudSystem
{
    private const double InheritChance = 0.4; // Не каждый ребёнок держит зла на врага покойного родителя

    public static void OnViolentDeath(Character character, World world, DeathReason reason)
    {
        if (reason is not (DeathReason.Murder or DeathReason.War) || character.Enemies.Count == 0)
        {
            return;
        }

        var children = world.Characters
            .Where(c => c.Alive && (c.Father == character || c.Mother == character))
            .ToList();

        if (children.Count == 0)
        {
            return;
        }

        foreach (var enemy in character.Enemies.Where(e => e.Alive).ToList())
        {
            foreach (var child in children)
            {
                if (Rng.NextDouble() < InheritChance)
                {
                    MurderSystem.AddEnmity(child, enemy);
                }
            }
        }
    }
}
