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
        int adultsCount = world.Characters.Count(c => c.Alive && c.LifeStage == LifeStage.Adult); // Количество взрослых персонажей в мире
        int birthAttempts = Math.Max(1, adultsCount / 20);
        for (int i = 0; i < birthAttempts; i++)
        {
            // Получаем список взрослых персонажей в мире
            var adults = world.Characters.Where(c => c.Alive && c.LifeStage == LifeStage.Adult).ToList();
            
            //Ищем родителей для рождения ребенка
            var mother = adults
            .Where(c => c.Gender == Gender.Female)
            .OrderBy(c => _random.Next())
            .FirstOrDefault();

            var father = adults
            .Where(c => c.Gender == Gender.Male)
            .OrderBy(c => _random.Next())
            .FirstOrDefault();

           if (mother == null || father == null)
            {
                continue; // Если нет подходящей пары родителей, пропускаем попытку рождения
            }

            if (_random.Next(100) < 30)
            {
            var newborn = CharacterGenerator.CreateNewborn();
            newborn.Mother = mother; // Устанавливаем ссылку на мать новорожденного
            newborn.Father = father; // Устанавливаем ссылку на отца новорожден
            
            //if (mother.Dynasty == null && father.Dynasty == null)
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
                    Description = $"Родился {newborn.Name}. " +
                     $"Родители: {mother.Name} и {father.Name}"
                });
            }
        }
    }
}