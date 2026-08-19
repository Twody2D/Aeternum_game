using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;

namespace Aeternum.WorldGen.Systems;

// Второй шаг фазы "эволюция" после домов: больницы отвечают за отдельную грань
// жизни поселения — здоровье, а не безопасность/оседлость (см. HousingSystem).
// Строятся из ткани и утвари (не дерева/камня — те заняты домами), реже домов
// и с более сильной отдачей — снижают детскую смертность от болезни и тяжесть эпидемий
public static class HospitalSystem
{
    private const double TextileCost = 20;
    private const double ClayCost = 15;

    private const int ResidentsPerHospital = 15; // Целевое число больниц растёт вместе с населением, медленнее домов

    private const double MaxHospitalBonus = 0.4; // Максимум -40% к смертности при достаточном числе больниц
    private const double BonusPerHospital = 0.1;

    public static void Process(World world)
    {
        foreach (var settlement in world.Settlements)
        {
            // Опустевшее поселение не пропускаем — брошенное должно доветшать (см. HousingSystem)
            var alivePopulation = settlement.Members.Count(m => m.Alive);

            var targetHospitals = (int)Math.Ceiling(alivePopulation / (double)ResidentsPerHospital);

            if (settlement.Hospitals >= targetHospitals)
            {
                if (DecaySystem.ShouldDecay(settlement.Hospitals, targetHospitals, world))
                {
                    settlement.Hospitals--; // Лечить некого — лечебница приходит в запустение
                }

                continue;
            }

            var textile = settlement.MaterialStocks.GetValueOrDefault(MaterialType.Textile);
            var clay = settlement.MaterialStocks.GetValueOrDefault(MaterialType.Clay);

            if (textile < TextileCost || clay < ClayCost)
            {
                continue;
            }

            settlement.MaterialStocks[MaterialType.Textile] = textile - TextileCost;
            settlement.MaterialStocks[MaterialType.Clay] = clay - ClayCost;
            settlement.Hospitals++;
        }
    }

    // Понижающий множитель для смертности от болезни/эпидемии (1.0 — больниц нет, ниже — чем больше больниц)
    public static double GetHospitalFactor(Settlement? settlement)
    {
        if (settlement == null)
        {
            return 1.0;
        }

        var bonus = Math.Min(MaxHospitalBonus, settlement.Hospitals * BonusPerHospital);

        return 1 - bonus;
    }
}
