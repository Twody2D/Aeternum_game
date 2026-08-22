using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;

namespace Aeternum.WorldGen.Tests.Systems;

// Насильственная смерть родителя с врагами (Character.Enemies) — с некоторым
// шансом передаёт вражду живым детям (см. DeathSystem.Kill). Не гарантировано —
// проверяется на выборке, как и любой другой шанс в проекте
public class BloodFeudSystemTests
{
    private static (Character Parent, Character Enemy, Character Child) BuildFamily(World world)
    {
        var parent = new Character { Id = 1, Name = "Родитель", LastName = "Тестов", Age = 40, Alive = true, LifeStage = LifeStage.Adult };
        var enemy = new Character { Id = 2, Name = "Враг", LastName = "Иноземцев", Age = 40, Alive = true, LifeStage = LifeStage.Adult };
        var child = new Character { Id = 3, Name = "Дитя", LastName = "Тестов", Age = 15, Alive = true, LifeStage = LifeStage.Student, Father = parent };

        parent.Enemies.Add(enemy);
        enemy.Enemies.Add(parent);

        world.Characters.AddRange([parent, enemy, child]);

        return (parent, enemy, child);
    }

    [Fact]
    public void Process_ParentMurderedWithEnemy_ChildSometimesInheritsEnmity()
    {
        var inherited = false;

        for (var run = 0; run < 200 && !inherited; run++)
        {
            var world = new World { CurrentYear = 100 };
            var (parent, enemy, child) = BuildFamily(world);

            Rng.Initialize(seed: run + 1);
            DeathSystem.Kill(parent, world, DeathReason.Murder);

            inherited = child.Enemies.Contains(enemy) && enemy.Enemies.Contains(child);
        }

        Assert.True(inherited, "хотя бы раз за 200 попыток вражда должна перейти к ребёнку");
    }

    [Fact]
    public void Process_ParentDiedOfOldAge_NeverInheritsEnmity()
    {
        for (var run = 0; run < 200; run++)
        {
            var world = new World { CurrentYear = 100 };
            var (parent, enemy, child) = BuildFamily(world);

            Rng.Initialize(seed: run + 1);
            DeathSystem.Kill(parent, world, DeathReason.OldAge);

            Assert.DoesNotContain(enemy, child.Enemies);
        }
    }

    [Fact]
    public void Process_ParentWithoutEnemies_NeverAddsEnmityToChild()
    {
        var world = new World { CurrentYear = 100 };
        var parent = new Character { Id = 1, Name = "Родитель", LastName = "Тестов", Age = 40, Alive = true, LifeStage = LifeStage.Adult };
        var child = new Character { Id = 2, Name = "Дитя", LastName = "Тестов", Age = 15, Alive = true, LifeStage = LifeStage.Student, Father = parent };
        world.Characters.AddRange([parent, child]);

        Rng.Initialize(seed: 1);
        DeathSystem.Kill(parent, world, DeathReason.Murder);

        Assert.Empty(child.Enemies);
    }

    [Fact]
    public void Process_DeadEnemy_IsNeverInheritedAsALivingFeud()
    {
        var world = new World { CurrentYear = 100 };
        var (parent, enemy, child) = BuildFamily(world);
        enemy.Alive = false;

        Rng.Initialize(seed: 1);
        DeathSystem.Kill(parent, world, DeathReason.War);

        Assert.DoesNotContain(enemy, child.Enemies);
    }
}
