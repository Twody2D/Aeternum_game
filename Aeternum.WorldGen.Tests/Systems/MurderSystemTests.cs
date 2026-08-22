using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;

namespace Aeternum.WorldGen.Tests.Systems;

// Малолетний правитель (см. KingdomSystem.IsMinor) — тот же соблазн для соперника,
// что и смелость или застарелая обида: не решает, случится ли заговор вообще
// (это по-прежнему только RegicideChance), а лишь смещает выбор конкретного
// заговорщика в пользу тех, у кого нет дружбы с правителем
public class MurderSystemTests
{
    // Регентство само по себе не увеличивает число заговоров — RegicideChance
    // не зависит от возраста правителя. Но смещает, кого из нескольких соперников
    // подозревают: при регентстве непричастный друг реже попадает под подозрение,
    // чем при взрослом правителе, потому что вес соперника-чужака вырос, а вес
    // друга — нет (см. MurderSystem.RegencyWeight)
    [Fact]
    public void Process_MinorRuler_MakesFriendLessLikelyToBeBlamedThanAdultRuler()
    {
        var friendShareUnderMinor = CountFriendBlamed(LifeStage.Child, age: 6);
        var friendShareUnderAdult = CountFriendBlamed(LifeStage.Adult, age: 40);

        Assert.True(friendShareUnderMinor < friendShareUnderAdult,
            $"при регентстве друга должны подозревать реже, чем при взрослом правителе: {friendShareUnderMinor} против {friendShareUnderAdult}");
    }

    private static int CountFriendBlamed(LifeStage rulerStage, int age)
    {
        var friendBlamed = 0;

        for (var run = 0; run < 300; run++)
        {
            var world = new World { CurrentYear = 100 };

            var ruler = new Character { Id = 1, Name = "Правитель", LastName = "Тестов", Age = age, Alive = true, LifeStage = rulerStage };
            var friend = new Character { Id = 2, Name = "Друг", LastName = "Тестов", Age = 30, Alive = true, LifeStage = LifeStage.Adult };
            var stranger = new Character { Id = 3, Name = "Чужой", LastName = "Иноземцев", Age = 30, Alive = true, LifeStage = LifeStage.Adult };
            friend.Friends.Add(ruler);
            ruler.Friends.Add(friend);

            var dynasty = new Dynasty { Id = 1, Name = "Дом Тестов", FoundedYear = 1, Founder = ruler };
            dynasty.Members.AddRange([ruler, friend, stranger]);

            var kingdom = new Kingdom { Id = 1, Name = "Королевство Тестов", Dynasty = dynasty, Ruler = ruler, FoundedYear = 1 };
            world.Kingdoms.Add(kingdom);
            world.Settings.RegicideChance = 1.0; // Заговор гарантирован — единственный вопрос, кто окажется убийцей

            Rng.Initialize(seed: run + 1);
            MurderSystem.Process(world);

            var murderEvent = world.Events.Single(e => e.Type == EventType.Murder);

            if (murderEvent.Description.Contains(SurnameSystem.GetDisplayFullName(friend)))
            {
                friendBlamed++;
            }
        }

        return friendBlamed;
    }

    // Друг правителя (Character.Friends) не получает надбавку за регентство — она
    // достаётся только тем, кто и так готов был бы предать. При выборе среди двух
    // соперников друг остаётся в пуле (см. MurderSystem.DefaultWeight — не 0), но
    // заметно реже становится убийцей, чем чужак с полным весом
    [Fact]
    public void Process_FriendOfMinorRuler_ConspiresLessOftenThanStrangerRival()
    {
        var friendPicked = 0;
        var strangerPicked = 0;

        for (var run = 0; run < 300; run++)
        {
            var world = new World { CurrentYear = 100 };

            var ruler = new Character { Id = 1, Name = "Дитя", LastName = "Тестов", Age = 6, Alive = true, LifeStage = LifeStage.Child };
            var friend = new Character { Id = 2, Name = "Друг", LastName = "Тестов", Age = 30, Alive = true, LifeStage = LifeStage.Adult };
            var stranger = new Character { Id = 3, Name = "Чужой", LastName = "Иноземцев", Age = 30, Alive = true, LifeStage = LifeStage.Adult };
            friend.Friends.Add(ruler);
            ruler.Friends.Add(friend);

            var dynasty = new Dynasty { Id = 1, Name = "Дом Тестов", FoundedYear = 1, Founder = ruler };
            dynasty.Members.AddRange([ruler, friend, stranger]);

            var kingdom = new Kingdom { Id = 1, Name = "Королевство Тестов", Dynasty = dynasty, Ruler = ruler, FoundedYear = 1 };
            world.Kingdoms.Add(kingdom);
            world.Settings.RegicideChance = 1.0; // Заговор гарантирован — единственный вопрос, кто окажется убийцей

            Rng.Initialize(seed: run + 1);
            MurderSystem.Process(world);

            var murderEvent = world.Events.Single(e => e.Type == EventType.Murder);

            if (murderEvent.Description.Contains(SurnameSystem.GetDisplayFullName(friend)))
            {
                friendPicked++;
            }
            else if (murderEvent.Description.Contains(SurnameSystem.GetDisplayFullName(stranger)))
            {
                strangerPicked++;
            }
        }

        Assert.True(friendPicked < strangerPicked,
            $"друга правителя должны подозревать в заговоре реже чужака: {friendPicked} против {strangerPicked}");
    }
}
