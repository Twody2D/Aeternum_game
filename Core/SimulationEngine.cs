using Aeternum.WorldGen.Simulation;

namespace Aeternum.WorldGen.Core;

// Точка входа в движок симуляции: крутит мир на заданное число лет
public class SimulationEngine
{
    // Обрабатывает ровно один год. Публичный отдельный метод — чтобы будущий
    // клиент (например, Godot-нода с кнопкой "Следующий год") мог управлять
    // симуляцией пошагово, не завися от Run и не зная про Simulation/YearProcessor
    public void Tick(World world)
    {
        YearProcessor.Process(world);
    }

    // Метод для запуска симуляции на определённое количество лет.
    // onYearProcessed вызывается после каждого обработанного года и позволяет
    // вызывающей стороне (консоль, Godot-нода, тесты) сама решить, как
    // показать прогресс, не привязывая к этому ядро симуляции.
    public void Run(World world, int years, Action<World>? onYearProcessed = null)
    {
        for(int i = 0; i < years; i++)
        {
            Tick(world);

            onYearProcessed?.Invoke(world);
        }
    }
}
