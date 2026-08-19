using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;

namespace Aeternum.WorldGen.Tests.Systems;

// Обычаи наследования решают, кому достанется корона, и от этого напрямую
// зависит, вспыхнет ли распря: KingdomSystem считает кризисом переход трона
// не к прямому потомку
public class SuccessionSystemTests
{
    private static Character Person(int id, int birthYear, string name = "Наследник")
    {
        return new Character { Id = id, Name = name, LastName = "Тестов", BirthYear = birthYear, Alive = true };
    }

    private static Kingdom KingdomWithLaw(SuccessionLaw law, Character ruler)
    {
        var culture = new Culture { Id = 1, Name = "Тестовый народ", SuccessionLaw = law };
        var settlement = new Settlement { Id = 1, Name = "Тестовка", Culture = culture };
        var founder = Person(99, 0, "Основатель");

        ruler.Settlement = settlement;

        return new Kingdom
        {
            Id = 1,
            Name = "Королевство Тестов",
            Ruler = ruler,
            Dynasty = new Dynasty { Id = 1, Name = "Дом Тестов", Founder = founder, FoundedYear = 1 }
        };
    }

    [Fact]
    public void PickHeir_Seniority_TakesOldest()
    {
        var ruler = Person(1, 10, "Правитель");
        var elder = Person(2, 5, "Старший");
        var younger = Person(3, 30, "Младший");

        var heir = SuccessionSystem.PickHeir([elder, younger], KingdomWithLaw(SuccessionLaw.Seniority, ruler), ruler);

        Assert.Equal(elder, heir);
    }

    [Fact]
    public void PickHeir_Primogeniture_PrefersOwnChildOverOlderRelative()
    {
        // Смысл первородства: собственный ребёнок опережает старшую боковую родню
        var ruler = Person(1, 10, "Правитель");
        var olderRelative = Person(2, 5, "Старший дядя");
        var child = Person(3, 40, "Дитя");
        child.Father = ruler;

        var heir = SuccessionSystem.PickHeir([olderRelative, child], KingdomWithLaw(SuccessionLaw.Primogeniture, ruler), ruler);

        Assert.Equal(child, heir);
    }

    [Fact]
    public void PickHeir_Primogeniture_TakesEldestChild()
    {
        var ruler = Person(1, 10, "Правитель");
        var firstborn = Person(2, 35, "Первенец");
        var secondborn = Person(3, 40, "Второй");
        firstborn.Mother = ruler;
        secondborn.Mother = ruler;

        var heir = SuccessionSystem.PickHeir([secondborn, firstborn], KingdomWithLaw(SuccessionLaw.Primogeniture, ruler), ruler);

        Assert.Equal(firstborn, heir);
    }

    [Fact]
    public void PickHeir_Primogeniture_ChildlessRuler_FallsBackToSeniority()
    {
        // Обычай молчит — корона не должна повисать в воздухе
        var ruler = Person(1, 10, "Бездетный");
        var elder = Person(2, 5, "Старший");
        var younger = Person(3, 30, "Младший");

        var heir = SuccessionSystem.PickHeir([younger, elder], KingdomWithLaw(SuccessionLaw.Primogeniture, ruler), ruler);

        Assert.Equal(elder, heir);
    }

    [Fact]
    public void PickHeir_Election_PrefersWellConnectedOverOldest()
    {
        var ruler = Person(1, 10, "Правитель");
        var oldLoner = Person(2, 5, "Старый нелюдим");
        var youngLeader = Person(3, 40, "Молодой вожак");

        youngLeader.Friends.Add(Person(10, 20));
        youngLeader.Friends.Add(Person(11, 20));

        var heir = SuccessionSystem.PickHeir([oldLoner, youngLeader], KingdomWithLaw(SuccessionLaw.Election, ruler), ruler);

        Assert.Equal(youngLeader, heir);
    }

    [Fact]
    public void PickHeir_Election_EnemiesOutweighFriends()
    {
        // Нажитые враги обесценивают связи: дом не выберет того, с кем сам в ссоре
        var ruler = Person(1, 10, "Правитель");
        var quarrelsome = Person(2, 5, "Склочный");
        var quiet = Person(3, 40, "Тихий");

        quarrelsome.Friends.Add(Person(10, 20));
        quarrelsome.Enemies.Add(Person(11, 20));
        quarrelsome.Enemies.Add(Person(12, 20));

        var heir = SuccessionSystem.PickHeir([quarrelsome, quiet], KingdomWithLaw(SuccessionLaw.Election, ruler), ruler);

        Assert.Equal(quiet, heir);
    }

    [Fact]
    public void PickHeir_Election_IgnoresDeadConnections()
    {
        // Друзья и враги не удаляются после смерти (память не стирается),
        // поэтому вес человека должны определять только живые связи
        var ruler = Person(1, 10, "Правитель");
        var withDeadFriends = Person(2, 5, "С мёртвыми друзьями");
        var withLiveFriend = Person(3, 40, "С живым другом");

        var deadFriend = Person(10, 20);
        deadFriend.Alive = false;
        withDeadFriends.Friends.Add(deadFriend);
        withLiveFriend.Friends.Add(Person(11, 20));

        var heir = SuccessionSystem.PickHeir([withDeadFriends, withLiveFriend], KingdomWithLaw(SuccessionLaw.Election, ruler), ruler);

        Assert.Equal(withLiveFriend, heir);
    }

    [Fact]
    public void PickSenior_SingleCandidate_TakesThem()
    {
        var only = Person(1, 20);

        Assert.Equal(only, SuccessionSystem.PickSenior([only]));
    }
}
