using Aeternum.WorldGen.Simulation;

namespace Aeternum.WorldGen.Core;

public class SimulationEngine
{
    // Метод для запуска симуляции на определённое количество лет.
    // onYearProcessed вызывается после каждого обработанного года и позволяет
    // вызывающей стороне (консоль, Godot-нода, тесты) сама решить, как
    // показать прогресс, не привязывая к этому ядро симуляции.
    public void Run(World world, int years, Action<World>? onYearProcessed = null)
    {
        for(int i = 0; i < years; i++)
        {
            YearProcessor.Process(world);

            onYearProcessed?.Invoke(world);
        }
    }
}
