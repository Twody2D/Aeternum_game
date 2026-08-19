namespace Aeternum.WorldGen.Core;

// Единый источник случайности мира. Раньше каждая система заводила свой
// Random — двадцать независимых генераторов, засеянных временем запуска, и
// один и тот же мир нельзя было получить дважды.
//
// Это мешало не абстрактно: подбирая пороги вассалитета, расколов веры и эпох,
// приходилось гонять симуляцию по десятку раз и смотреть на разброс, потому
// что сравнить "до" и "после" на одном и том же мире было невозможно. С общим
// seed правка баланса наконец проверяется на неизменных входных данных.
//
// Глобальное статическое состояние здесь — осознанная плата: весь мир уже
// собран из статических систем, и протаскивать генератор параметром через
// каждую из них означало бы переписать их все ради одного поля
public static class Rng
{
    private static Random _random = new();

    // Seed текущего мира. Хранится, чтобы его можно было показать и записать
    // в сохранение: увидел интересный мир — повторил его в точности
    public static int Seed { get; private set; }

    // Задаёт зерно мира. Если не вызвать, мир получит случайное — как и раньше,
    // но теперь хотя бы известно, какое именно
    public static void Initialize(int? seed = null)
    {
        Seed = seed ?? new Random().Next();
        _random = new Random(Seed);
    }

    public static double NextDouble()
    {
        return _random.NextDouble();
    }

    public static int Next()
    {
        return _random.Next();
    }

    public static int Next(int maxValue)
    {
        return _random.Next(maxValue);
    }

    public static int Next(int minValue, int maxValue)
    {
        return _random.Next(minValue, maxValue);
    }
}
