using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Events;

namespace Aeternum.WorldGen.Systems;

// Двор государства. До сих пор государство состояло из одного человека:
// правитель воевал, собирал, судил и думал сам. Здесь у короны появляются
// люди — но не как украшение родословной, а как работа, которую и без них
// кто-то делал безымянно.
//
// Никаких новых свойств у персонажей для этого не заведено: должность
// занимает тот, кто и так лучший в своём деле — нужная профессия плюс
// накопленное мастерство (см. ProfessionSystem.GetMastery). Поэтому двор
// сильного государства собирается сам собой, а разорённому и обезлюдевшему
// назначить попросту некого.
//
// Сила должности равна мастерству того, кто её занимает: бывалый воевода
// водит войско лучше вчерашнего рекрута, и это одно и то же число
public static class CourtSystem
{
    public static void Process(World world)
    {
        foreach (var kingdom in world.Kingdoms)
        {
            if (kingdom.FallenYear != null)
            {
                continue;
            }

            foreach (var office in Enum.GetValues<CourtOffice>())
            {
                if (IsStillServing(kingdom, office, world))
                {
                    continue;
                }

                var appointee = FindAppointee(kingdom, office, world);

                if (appointee == null)
                {
                    kingdom.Court.Remove(office); // Некого — должность пустует
                    continue;
                }

                // Одному человеку — одна должность. Отбор кандидатов это уже
                // учитывает, но наследника определяет закон наследования, а не
                // выбор лучшего: он может оказаться и тем, кто уже служит короне.
                // Тогда прежний его пост освобождается и достаётся кому-то ещё
                // здесь же, ниже по кругу
                foreach (var held in kingdom.Court.Where(kv => kv.Value == appointee).Select(kv => kv.Key).ToList())
                {
                    kingdom.Court.Remove(held);
                }

                kingdom.Court[office] = appointee;

                world.Events.Add(new WorldEvent
                {
                    Year = world.CurrentYear,
                    Type = EventType.Appointment,
                    Description = $"{kingdom.Name}: {SurnameSystem.GetDisplayFullName(appointee)} — {GetTitle(office)}",
                    Kingdoms = [kingdom]
                });
            }
        }
    }

    // Должность держится за человеком, пока он жив и остаётся при этом
    // государстве; ушедший в чужие земли перестаёт ему служить
    private static bool IsStillServing(Kingdom kingdom, CourtOffice office, World world)
    {
        if (!kingdom.Court.TryGetValue(office, out var holder))
        {
            return false;
        }

        if (!holder.Alive || holder == kingdom.Ruler)
        {
            return false;
        }

        return office == CourtOffice.Heir
            ? holder.Dynasty == kingdom.Dynasty
            : holder.Settlement != null && kingdom.Settlements.Contains(holder.Settlement);
    }

    private static Character? FindAppointee(Kingdom kingdom, CourtOffice office, World world)
    {
        if (office == CourtOffice.Heir)
        {
            // Наследника не выбирают из лучших — его определяет тот же закон
            // наследования, по которому трон и перейдёт (см. SuccessionSystem)
            var candidates = kingdom.Dynasty.Members
                .Where(m => m.Alive && m != kingdom.Ruler && m.LifeStage is LifeStage.Adult or LifeStage.Elder)
                .ToList();

            return candidates.Count == 0 ? null : SuccessionSystem.PickHeir(candidates, kingdom, kingdom.Ruler);
        }

        var category = GetRequiredCategory(office);

        return kingdom.Settlements
            .SelectMany(s => s.Members)
            .Where(m => m.Alive
                        && m != kingdom.Ruler
                        && m.LifeStage is LifeStage.Adult or LifeStage.Elder
                        && ProfessionSystem.GetCategory(m.Profession) == category
                        && !kingdom.Court.Values.Contains(m)) // Один человек — одна должность
            .OrderByDescending(m => ProfessionSystem.GetMastery(m, world))
            .ThenBy(m => m.Id)
            .FirstOrDefault();
    }

    private static ProfessionCategory GetRequiredCategory(CourtOffice office)
    {
        return office switch
        {
            CourtOffice.Marshal => ProfessionCategory.Military,
            CourtOffice.Treasurer => ProfessionCategory.Trade,
            _ => ProfessionCategory.Knowledge
        };
    }

    // Насколько занимающий должность лучше пустого места: 1.0 — должность
    // пустует, выше — по мастерству того, кто её держит
    public static double GetOfficeStrength(Kingdom kingdom, CourtOffice office, World world)
    {
        return kingdom.Court.TryGetValue(office, out var holder) && holder.Alive
            ? ProfessionSystem.GetMastery(holder, world)
            : 1.0;
    }

    public static bool HasOffice(Kingdom kingdom, CourtOffice office)
    {
        return kingdom.Court.TryGetValue(office, out var holder) && holder.Alive;
    }

    public static string GetTitle(CourtOffice office)
    {
        return office switch
        {
            CourtOffice.Heir => "наследник",
            CourtOffice.Marshal => "воевода",
            CourtOffice.Treasurer => "казначей",
            _ => "советник"
        };
    }
}
