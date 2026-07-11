using Aeternum.WorldGen.Characters;
using Aeternum.WorldGen.GameWorld;
namespace Aeternum.WorldGen.Simulation;

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
            if (father.Dynasty !=null)
            {
                // Если у обоих родителей нет династии, создаем новую династию
                // var newDynasty = new Dynasty
                // {
                //     Name = $"{mother.Name} & {father.Name} Dynasty"
                // };
                // mother.Dynasty = newDynasty;
                // father.Dynasty = newDynasty;
                // world.Dynasties.Add(newDynasty);
                newborn.Dynasty = father.Dynasty; // Устанавливаем династию новорожденного на основе родителей
                DynastySystem.AddMember(
                father.Dynasty,
                newborn
                );
            }
            // else if (mother.Dynasty != null && father.Dynasty == null)
            // {
            //     // Если у матери есть династия, а у отца нет, присваиваем династию матери отцу
            //     father.Dynasty = mother.Dynasty;
            // }
            // else if (mother.Dynasty == null && father.Dynasty != null)
            // {
            //     // Если у отца есть династия, а у матери нет, присваиваем династию отца матери
            //     mother.Dynasty = father.Dynasty;
            // }
            //newborn.Dynasty = father.Dynasty; // Устанавливаем династию новорожденного на основе родителей
            
            
            
            FamilySystem.CreateFamily(
                mother,
                father,
                newborn,
                world
        );
            
            newborns.Add(newborn); // Добавляем новорожденного персонажа в список newborns
            
            world.TotalBirths++;
            world.AliveCount++;

                Console.WriteLine($"Родился {newborn.Name}. Родители: {mother.Name} и {father.Name}"); // Выводим информацию о рождении новорожденного
                
            }
        }
    }
}