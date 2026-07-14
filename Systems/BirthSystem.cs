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
            if(mothers.Count == 0)
            {
                return;
            }
            double birthRate = PopulationSystem.GetBirthRate(world);


            int birthAttempts = (int)(mothers.Count * birthRate); // Количество попыток рождения, пропорциональное количеству взрослых женщин и коэффициенту рождаемости
            for (int i = 0; i < birthAttempts; i++)
            {
                 
                //Ищем родителей для рождения ребенка
                var mother = mothers[_random.Next(mothers.Count)];

                var father = fathers[_random.Next(fathers.Count)];
                if (mother == null || father == null)
                    {
                        continue; // Если нет подходящей пары родителей, пропускаем попытку рождения
                    }

                var newborn = CharacterGenerator.CreateNewborn();
                newborn.Mother = mother; // Устанавливаем ссылку на мать новорожденного
                newborn.Father = father; // Устанавливаем ссылку на отца новорожден
                newborn.LastName = father.LastName;
                
                if (father.Family !=null)
                {
                    FamilySystem.AddChildToFamily(
                        father.Family,
                        newborn
                    );
                }
                else
                    {
                        var family = FamilySystem.CreateFamily(
                            mother,
                            father,
                            world
                        );

                            FamilySystem.AddChildToFamily(
                                family,
                                newborn
                            );
                    
                    }
            
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