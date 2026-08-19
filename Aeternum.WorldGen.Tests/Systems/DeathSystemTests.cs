using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;

namespace Aeternum.WorldGen.Tests.Systems;

// Kill — единственная точка, через которую в мире умирают, и она делает заметно
// больше, чем ставит флаг: ведёт счётчики, освобождает вдову для нового брака и
// начисляет память о долгожителе. Всё это легко сломать незаметно
public class DeathSystemTests
{
    private static Character Adult(int id, int age = 30)
    {
        return new Character
        {
            Id = id,
            Name = $"Житель{id}",
            LastName = "Тестов",
            Age = age,
            Alive = true,
            LifeStage = LifeStage.Adult
        };
    }

    [Fact]
    public void Kill_UpdatesCountersAndMarksCharacterDead()
    {
        var world = new World { CurrentYear = 42, AliveCount = 1 };
        var victim = Adult(1);
        world.Characters.Add(victim);

        DeathSystem.Kill(victim, world, DeathReason.Accident);

        Assert.False(victim.Alive);
        Assert.Equal(DeathReason.Accident, victim.DeathReason);
        Assert.Equal(42, victim.DeathYear);
        Assert.Equal(1, world.TotalDeaths);
        Assert.Equal(0, world.AliveCount);
    }

    [Fact]
    public void Kill_FreesWidowForRemarriage()
    {
        // Иначе овдовевший навсегда остался бы связанным с покойным и выпал
        // из брачного пула (см. MarriageSystem)
        var world = new World { CurrentYear = 10 };
        var husband = Adult(1);
        var wife = Adult(2);

        var family = new Family { Id = 1, Father = husband, Mother = wife, FormedYear = 5 };
        husband.CurrentFamily = family;
        wife.CurrentFamily = family;

        DeathSystem.Kill(husband, world, DeathReason.OldAge);

        Assert.Null(wife.CurrentFamily);
        Assert.True(wife.Alive);
    }

    [Fact]
    public void Kill_LongLivedCharacter_LeavesLegendAndReputation()
    {
        var world = new World { CurrentYear = 100 };
        var settlement = new Settlement { Id = 1, Name = "Тестовка" };
        var elder = Adult(1, age: NotablePeopleSystem.OldAgeThreshold);
        var dynasty = new Dynasty { Id = 1, Name = "Дом Тестов", Founder = elder, FoundedYear = 1 };

        elder.Settlement = settlement;
        elder.Dynasty = dynasty;

        DeathSystem.Kill(elder, world, DeathReason.OldAge);

        Assert.Equal(1, settlement.LegendCount);
        Assert.True(dynasty.Reputation > 0);
    }

    [Fact]
    public void Kill_LongLivedWithoutDynasty_StillLeavesLegend()
    {
        // Легенда принадлежит месту, а не дому: безродный долгожитель тоже след оставляет
        var world = new World { CurrentYear = 100 };
        var settlement = new Settlement { Id = 1, Name = "Тестовка" };
        var elder = Adult(1, age: NotablePeopleSystem.OldAgeThreshold + 5);
        elder.Settlement = settlement;

        DeathSystem.Kill(elder, world, DeathReason.Accident);

        Assert.Equal(1, settlement.LegendCount);
    }

    [Fact]
    public void Kill_YoungCharacter_LeavesNoLegend()
    {
        var world = new World { CurrentYear = 100 };
        var settlement = new Settlement { Id = 1, Name = "Тестовка" };
        var young = Adult(1, age: NotablePeopleSystem.OldAgeThreshold - 1);
        young.Settlement = settlement;

        DeathSystem.Kill(young, world, DeathReason.Accident);

        Assert.Equal(0, settlement.LegendCount);
    }

    [Fact]
    public void Process_CharacterAtMaximumAge_DiesOfOldAge()
    {
        var world = new World { CurrentYear = 1, AliveCount = 1 };
        var ancient = Adult(1, age: new WorldSettings().MaximumAge);
        world.Characters.Add(ancient);

        DeathSystem.Process(world);

        Assert.False(ancient.Alive);
        Assert.Equal(DeathReason.OldAge, ancient.DeathReason);
    }

    [Fact]
    public void Process_AlreadyDead_IsNotKilledTwice()
    {
        // Двойной вызов задвоил бы счётчики смертей и увёл AliveCount в минус
        var world = new World { CurrentYear = 1, AliveCount = 0 };
        var corpse = Adult(1, age: new WorldSettings().MaximumAge);
        corpse.Alive = false;
        world.Characters.Add(corpse);

        DeathSystem.Process(world);

        Assert.Equal(0, world.TotalDeaths);
        Assert.Equal(0, world.AliveCount);
    }
}
