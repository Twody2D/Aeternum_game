using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;

namespace Aeternum.WorldGen.Export;

// Сборка снимка мира для клиента-отрисовщика (см. WorldSnapshot). Как и
// SaveSystem, работает со строкой, а не с файлом: консоль сама решает писать
// на диск, а Godot-клиенту достаточно того же JSON, полученного любым путём.
//
// Здесь же разворачиваются все ссылки, которые иначе пришлось бы разворачивать
// на стороне отрисовки: поселение сразу знает, какому государству принадлежит,
// а государство — как зовут правителя, а не по какому он Id
public static class SnapshotExporter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic) // Не эскейпить кириллицу — снимок остаётся читаемым
    };

    public static void Export(World world, string path)
    {
        File.WriteAllText(path, Serialize(world));
    }

    public static string Serialize(World world)
    {
        return JsonSerializer.Serialize(Build(world), Options);
    }

    public static WorldSnapshot Build(World world)
    {
        // Действующие государства — те, чья династия ещё не угасла. Павшие
        // остаются в списке как историческая запись (с FallenYear), но
        // закрашивать их владения на карте уже нечем
        var kingdomBySettlement = new Dictionary<int, Kingdom>();

        foreach (var kingdom in world.Kingdoms.Where(k => k.FallenYear == null))
        {
            foreach (var settlement in kingdom.Settlements)
            {
                kingdomBySettlement.TryAdd(settlement.Id, kingdom);
            }
        }

        return new WorldSnapshot(
            Year: world.CurrentYear,
            Era: TechnologySystem.GetEraName(world.Knowledge),
            Knowledge: Math.Round(world.Knowledge, 1),
            Population: world.Characters.Count(c => c.Alive),
            TotalBirths: world.TotalBirths,
            TotalDeaths: world.TotalDeaths,
            MapSize: ClimateSystem.MapSize,
            Settlements: world.Settlements.Select(s => BuildSettlement(s, world, kingdomBySettlement)).ToList(),
            Kingdoms: world.Kingdoms.Select(BuildKingdom).ToList(),
            TradeRoutes: world.TradeRoutes.Select(r => new TradeRouteSnapshot(r.A.Id, r.B.Id, r.Years)).ToList(),
            Timeline: world.Events.Select(e => new EventSnapshot(e.Year, e.Type.ToString(), e.Description)).ToList());
    }

    private static SettlementSnapshot BuildSettlement(Settlement settlement, World world, Dictionary<int, Kingdom> kingdomBySettlement)
    {
        return new SettlementSnapshot(
            Id: settlement.Id,
            Name: settlement.Name,
            X: Math.Round(settlement.X, 1),
            Y: Math.Round(settlement.Y, 1),
            Population: settlement.Members.Count(m => m.Alive),
            Fertility: Math.Round(ClimateSystem.GetFertility(settlement), 2),
            Culture: settlement.Culture?.Name,
            Religion: settlement.Religion?.Name,
            Houses: settlement.Houses,
            Hospitals: settlement.Hospitals,
            Schools: settlement.Schools,
            Walls: settlement.Walls,
            Legends: settlement.LegendCount,
            IsUnderSiege: settlement.SiegeYears > 0,
            IsRebelling: RebellionSystem.IsRebelling(settlement, world),
            RulingKingdom: kingdomBySettlement.GetValueOrDefault(settlement.Id)?.Name);
    }

    private static KingdomSnapshot BuildKingdom(Kingdom kingdom)
    {
        return new KingdomSnapshot(
            Id: kingdom.Id,
            Name: kingdom.Name,
            Ruler: SurnameSystem.GetDisplayFullName(kingdom.Ruler),
            Dynasty: kingdom.Dynasty.Name,
            FoundedYear: kingdom.FoundedYear,
            FallenYear: kingdom.FallenYear,
            Reputation: kingdom.Dynasty.Reputation,
            Suzerain: kingdom.Suzerain?.Name,
            Allies: kingdom.AlliedKingdoms.Select(a => a.Name).ToList(),
            SettlementIds: kingdom.Settlements.Select(s => s.Id).ToList());
    }
}
