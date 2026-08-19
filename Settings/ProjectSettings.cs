using System.Globalization;

namespace Aeternum.WorldGen.Settings;

// Константы запуска приложения (в отличие от WorldSettings — не относятся к самому миру).
// Значения по умолчанию те же, что были зашиты раньше, но теперь их можно задать
// аргументами командной строки: мир на пять веков или на дюжину поселений больше не
// требует пересборки проекта
public static class ProjectSettings
{
    // Количество жителей, с которых начинается симуляция
    public static int StartingPopulation { get; private set; } = 30;

    // Количество лет, на которые запускается симуляция
    public static int SimulationYears { get; private set; } = 100;

    // Сколько поселений создаётся при старте — стартовое население делится между ними поровну.
    // Слишком много поселений при небольшом StartingPopulation даёт маленькие изолированные
    // группы, уязвимые к случайному вымиранию (перекос полов, серия смертей)
    public static int SettlementCount { get; private set; } = 3;

    // Зерно случайности мира (см. Rng). null — каждый запуск порождает новый мир;
    // заданное число воспроизводит один и тот же мир в точности. Фактически
    // использованное зерно печатается при запуске, так что понравившийся мир
    // всегда можно повторить
    public static int? Seed { get; private set; }

    // Погодовой лог событий. На веку он читается, на пяти — тонет в собственном
    // объёме, поэтому длинные прогоны разумно смотреть по одним итогам
    public static bool Quiet { get; private set; }

    public const string Usage =
        "Аргументы (все необязательны):\n" +
        "  --years=N        лет симуляции (по умолчанию 100)\n" +
        "  --settlements=N  стартовых поселений (по умолчанию 3)\n" +
        "  --population=N   стартовых жителей (по умолчанию 30)\n" +
        "  --seed=N         зерно мира; без него — случайное\n" +
        "  --quiet          не печатать погодовой лог событий\n" +
        "  --help           показать эту справку";

    // Разбирает аргументы запуска. Возвращает false, если запуск продолжать не нужно
    // (запрошена справка или аргументы неверны) — молча проглатывать опечатку в
    // имени параметра нельзя: пользователь решит, что настройка применилась
    public static bool Apply(string[] args, TextWriter output)
    {
        foreach (var arg in args)
        {
            if (arg is "--help" or "-h")
            {
                output.WriteLine(Usage);
                return false;
            }

            if (arg == "--quiet")
            {
                Quiet = true;
                continue;
            }

            var separatorIndex = arg.IndexOf('=');

            if (!arg.StartsWith("--") || separatorIndex < 0)
            {
                output.WriteLine($"Непонятный аргумент: {arg}");
                output.WriteLine(Usage);
                return false;
            }

            var name = arg[2..separatorIndex];
            var rawValue = arg[(separatorIndex + 1)..];

            if (!int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            {
                output.WriteLine($"Значение параметра {name} должно быть целым числом, получено: {rawValue}");
                return false;
            }

            switch (name)
            {
                case "years":
                    SimulationYears = value;
                    break;
                case "settlements":
                    SettlementCount = value;
                    break;
                case "population":
                    StartingPopulation = value;
                    break;
                case "seed":
                    Seed = value;
                    break;
                default:
                    output.WriteLine($"Неизвестный параметр: {name}");
                    output.WriteLine(Usage);
                    return false;
            }
        }

        return Validate(output);
    }

    // Отрицательные и нулевые значения роняли бы генерацию глубоко внутри мира,
    // где причина уже не видна, — ловим их на входе
    private static bool Validate(TextWriter output)
    {
        if (SimulationYears < 0)
        {
            output.WriteLine("Число лет не может быть отрицательным");
            return false;
        }

        if (SettlementCount < 1)
        {
            output.WriteLine("Поселений должно быть хотя бы одно");
            return false;
        }

        if (StartingPopulation < 1)
        {
            output.WriteLine("Жителей должно быть хотя бы один");
            return false;
        }

        return true;
    }
}
