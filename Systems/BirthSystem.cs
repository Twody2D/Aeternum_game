using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Generators;
using Aeternum.WorldGen.Events;
namespace Aeternum.WorldGen.Systems;

public static class BirthSystem
{
    private static readonly Random _random = new();
    
    public static void ProcessBirths(List<Character> newborns, World world)
    {
            var mothers = PopulationSystem.GetAdultFemales(world);
            var fathers = PopulationSystem.GetPotentialFathers(world);
            var families = world.Families.Where(f => f.Father.Alive && f.Mother.Alive).ToList();
            if (families.Count == 0)
            {
                return;
            }

            if(mothers.Count == 0)
            {
                return;
            }
            double birthRate = PopulationSystem.GetBirthRate(world);

            int birthAttempts = (int)(families.Count * birthRate); // Количество попыток рождения, пропорциональное количеству взрослых женщин и коэффициенту рождаемости

            for (int i = 0; i < birthAttempts; i++)
            {
                //Ищем родителей для рождения ребенка
                var family = families[_random.Next(families.Count)];
                var mother = family.Mother;
                var father = family.Father;

                if(_random.NextDouble() > birthRate)
                {
                    continue;
                }
              
                var newborn = CharacterGenerator.CreateNewborn();

                newborn.Mother = mother; // Устанавливаем ссылку на мать новорожденного
                newborn.Father = father; // Устанавливаем ссылку на отца новорожден
                newborn.LastName = father.LastName;

                FamilySystem.AddChildToFamily(family, newborn);
                                          
                newborns.Add(newborn); // Добавляем новорожденного персонажа в список newborns
                
                world.TotalBirths++;
                world.AliveCount++;
                    
                    world.Events.Add(new WorldEvent
                    {
                        Year = world.CurrentYear,
                        Type = EventType.Birth,
                        Description = $"Родился {newborn.Name} {newborn.LastName}. " +
                        $"Родители: {mother.Name} {mother.LastName} и {father.Name} {father.LastName}"
                    });
            }
    }
}