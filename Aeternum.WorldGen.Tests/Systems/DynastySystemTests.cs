using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;

namespace Aeternum.WorldGen.Tests.Systems;

public class DynastySystemTests
{
    [Fact]
    public void AddMember_SamePersonTwice_AddsOnlyOnce()
    {
        // Повторный брак в тот же дом (вдова/вдовец женится на ком-то из
        // клана покойного супруга) не должен дублировать запись в Members
        var founder = new Character { Id = 1, Name = "Основатель", LastName = "Тестов", Gender = Gender.Male };
        var dynasty = new Dynasty { Id = 1, Name = "Дом Тестов", Founder = founder, FoundedYear = 1 };
        var member = new Character { Id = 2, Name = "Член", LastName = "Тестов", Gender = Gender.Female };

        DynastySystem.AddMember(dynasty, member);
        DynastySystem.AddMember(dynasty, member);

        Assert.Single(dynasty.Members, member);
    }

    [Fact]
    public void AddMember_DifferentPeople_AddsEach()
    {
        var founder = new Character { Id = 1, Name = "Основатель", LastName = "Тестов", Gender = Gender.Male };
        var dynasty = new Dynasty { Id = 1, Name = "Дом Тестов", Founder = founder, FoundedYear = 1 };
        var first = new Character { Id = 2, Name = "Первый", LastName = "Тестов", Gender = Gender.Male };
        var second = new Character { Id = 3, Name = "Вторая", LastName = "Тестова", Gender = Gender.Female };

        DynastySystem.AddMember(dynasty, first);
        DynastySystem.AddMember(dynasty, second);

        Assert.Equal(2, dynasty.Members.Count);
    }
}
