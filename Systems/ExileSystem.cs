using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Events;

namespace Aeternum.WorldGen.Systems;

// Изгнание вместо казни. И проигравшая сторона распри за престол
// (KingdomSystem.TryTriggerSuccessionCrisis), и усмирённый мятеж
// (RebellionSystem.Suppress) до сих пор отбирали "жертв" одним и тем же
// приёмом — случайной выборкой из тех, кому предстоит умереть. Не каждому
// проигравшему нужна смерть: часть тех, кто уже отобран как жертва, покидает
// родное поселение живой — с враждой к тому, кто её покарал (см.
// MurderSystem.AddEnmity), и шансом однажды вернуться (уже существующие
// пути — MigrationSystem, брак), а не пропасть из мира навсегда.
//
// Куда именно бежит изгнанник — не решается по кровной вражде княжеств, как
// было бы в жизни: просто любое другое поселение мира, тем же нестрогим
// приёмом, что уже использует MigrationSystem для переезда без цели
public static class ExileSystem
{
    private const double ExileChance = 0.3; // Доля отобранных "жертв", которым смерть заменяется изгнанием

    // Принимает уже отобранный вызывающей системой список жертв и возвращает
    // тех, кого предстоит предать смерти по-настоящему — остальные изгнаны
    public static List<Character> SplitCasualties(List<Character> casualties, Character punisher, World world)
    {
        if (casualties.Count == 0)
        {
            return casualties;
        }

        var toExile = casualties.Where(_ => Rng.NextDouble() < ExileChance).ToList();

        foreach (var exile in toExile)
        {
            Exile(exile, punisher, world);
        }

        return toExile.Count == 0 ? casualties : casualties.Except(toExile).ToList();
    }

    private static void Exile(Character character, Character punisher, World world)
    {
        var origin = character.Settlement;

        MurderSystem.AddEnmity(character, punisher);

        var destination = world.Settlements
            .Where(s => s != origin)
            .OrderBy(_ => Rng.Next())
            .FirstOrDefault();

        if (destination != null)
        {
            character.Settlement = destination;
            destination.Members.Add(character);
        }

        var verb = character.Gender == Gender.Female ? "изгнана" : "изгнан";
        var route = origin != null && destination != null ? $": {origin.Name} → {destination.Name}" : "";

        world.Events.Add(new WorldEvent
        {
            Year = world.CurrentYear,
            Type = EventType.Exile,
            Description = $"{SurnameSystem.GetDisplayFullName(character)} {verb} вместо казни{route}"
        });
    }
}
