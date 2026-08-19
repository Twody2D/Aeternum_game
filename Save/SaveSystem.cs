using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Generators;
using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Systems;

namespace Aeternum.WorldGen.Save;

// Сохранение и загрузка мира в JSON. Serialize/Deserialize работают со строкой,
// поэтому файловый ввод-вывод не зашит внутрь — консоль пишет на диск через
// File.*, а будущий Godot-клиент сможет использовать свой FileAccess с той же логикой
public static class SaveSystem
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic) // Не эскейпить кириллицу — сохранение остаётся читаемым
    };

    public static void Save(World world, string path)
    {
        File.WriteAllText(path, Serialize(world));
    }

    public static World Load(string path)
    {
        return Deserialize(File.ReadAllText(path));
    }

    public static string Serialize(World world)
    {
        return JsonSerializer.Serialize(ToSaveData(world), Options);
    }

    public static World Deserialize(string json)
    {
        var data = JsonSerializer.Deserialize<SaveData>(json, Options)
            ?? throw new InvalidDataException("Файл сохранения повреждён или пуст");

        return FromSaveData(data);
    }

    private static SaveData ToSaveData(World world)
    {
        return new SaveData
        {
            CurrentYear = world.CurrentYear,
            TotalBirths = world.TotalBirths,
            TotalDeaths = world.TotalDeaths,
            AliveCount = world.AliveCount,
            Settings = world.Settings,
            Events = world.Events,

            Characters = world.Characters.Select(c => new CharacterData
            {
                Id = c.Id,
                Name = c.Name,
                LastName = c.LastName,
                Age = c.Age,
                Profession = c.Profession,
                Gender = c.Gender,
                Alive = c.Alive,
                DeathReason = c.DeathReason,
                LifeStage = c.LifeStage,
                BirthYear = c.BirthYear,
                DeathYear = c.DeathYear,
                MotherId = c.Mother?.Id,
                FatherId = c.Father?.Id,
                GuardianId = c.Guardian?.Id,
                BirthFamilyId = c.BirthFamily?.Id,
                CurrentFamilyId = c.CurrentFamily?.Id,
                DynastyId = c.Dynasty?.Id,
                SettlementId = c.Settlement?.Id,
                Traits = c.Traits.ToList(),
                EnemyIds = c.Enemies.Select(e => e.Id).ToList()
            }).ToList(),

            Families = world.Families.Select(f => new FamilyData
            {
                Id = f.Id,
                FatherId = f.Father.Id,
                MotherId = f.Mother.Id,
                ChildrenIds = f.Children.Select(c => c.Id).ToList(),
                DynastyId = f.Dynasty?.Id,
                FormedYear = f.FormedYear
            }).ToList(),

            Dynasties = world.Dynasties.Select(d => new DynastyData
            {
                Id = d.Id,
                Name = d.Name,
                MemberIds = d.Members.Select(c => c.Id).ToList(),
                FamilyIds = d.Families.Select(f => f.Id).ToList(),
                FounderId = d.Founder.Id,
                FoundedYear = d.FoundedYear,
                Reputation = d.Reputation
            }).ToList(),

            Settlements = world.Settlements.Select(s => new SettlementData
            {
                Id = s.Id,
                Name = s.Name,
                FoodStock = s.FoodStock,
                MaterialStocks = s.MaterialStocks,
                Houses = s.Houses,
                Hospitals = s.Hospitals,
                Workshops = s.Workshops,
                Schools = s.Schools,
                Walls = s.Walls,
                Gold = s.Gold,
                X = s.X,
                Y = s.Y,
                SiegeYears = s.SiegeYears,
                TruceUntilYear = s.TruceUntilYear,
                MemberIds = s.Members.Select(c => c.Id).ToList(),
                CultureId = s.Culture?.Id,
                ReligionId = s.Religion?.Id,
                LegendCount = s.LegendCount
            }).ToList(),

            Cultures = world.Cultures.Select(c => new CultureData
            {
                Id = c.Id,
                Name = c.Name,
                PreferredCategory = c.PreferredCategory
            }).ToList(),

            Religions = world.Religions.Select(r => new ReligionData
            {
                Id = r.Id,
                Name = r.Name
            }).ToList(),

            Kingdoms = world.Kingdoms.Select(k => new KingdomData
            {
                Id = k.Id,
                Name = k.Name,
                DynastyId = k.Dynasty.Id,
                RulerId = k.Ruler.Id,
                FoundedYear = k.FoundedYear,
                FallenYear = k.FallenYear,
                SettlementIds = k.Settlements.Select(s => s.Id).ToList(),
                AlliedKingdomIds = k.AlliedKingdoms.Select(ak => ak.Id).ToList(),
                SuzerainId = k.Suzerain?.Id,
                FoodTreasury = k.FoodTreasury,
                MaterialTreasury = k.MaterialTreasury,
                GoldTreasury = k.GoldTreasury
            }).ToList(),

            TradeRoutes = world.TradeRoutes.Select(r => new TradeRouteData
            {
                SettlementAId = r.A.Id,
                SettlementBId = r.B.Id,
                Years = r.Years
            }).ToList()
        };
    }

    private static World FromSaveData(SaveData data)
    {
        // Сначала создаём "пустые" объекты для каждого Id, потом связываем их
        // между собой — иначе персонаж может ссылаться на семью, ещё не созданную
        var dynastiesById = data.Dynasties.ToDictionary(
            d => d.Id,
            d => new Dynasty { Id = d.Id, Name = d.Name, FoundedYear = d.FoundedYear, Reputation = d.Reputation });

        var culturesById = data.Cultures.ToDictionary(
            c => c.Id,
            c => new Culture { Id = c.Id, Name = c.Name, PreferredCategory = c.PreferredCategory });

        var religionsById = data.Religions.ToDictionary(
            r => r.Id,
            r => new Religion { Id = r.Id, Name = r.Name });

        var settlementsById = data.Settlements.ToDictionary(
            s => s.Id,
            s => new Settlement { Id = s.Id, Name = s.Name, FoodStock = s.FoodStock, MaterialStocks = s.MaterialStocks, Houses = s.Houses, Hospitals = s.Hospitals, Workshops = s.Workshops, Schools = s.Schools, Walls = s.Walls, Gold = s.Gold, X = s.X, Y = s.Y, SiegeYears = s.SiegeYears, TruceUntilYear = s.TruceUntilYear, LegendCount = s.LegendCount });

        var familiesById = data.Families.ToDictionary(
            f => f.Id,
            f => new Family { Id = f.Id, FormedYear = f.FormedYear });

        var charactersById = data.Characters.ToDictionary(
            c => c.Id,
            c => new Character
            {
                Id = c.Id,
                Name = c.Name,
                LastName = c.LastName,
                Age = c.Age,
                Profession = c.Profession,
                Gender = c.Gender,
                Alive = c.Alive,
                DeathReason = c.DeathReason,
                LifeStage = c.LifeStage,
                BirthYear = c.BirthYear,
                DeathYear = c.DeathYear,
                Traits = c.Traits.ToHashSet()
            });

        foreach (var c in data.Characters)
        {
            var character = charactersById[c.Id];

            character.Mother = c.MotherId.HasValue ? charactersById[c.MotherId.Value] : null;
            character.Father = c.FatherId.HasValue ? charactersById[c.FatherId.Value] : null;
            character.Guardian = c.GuardianId.HasValue ? charactersById[c.GuardianId.Value] : null;
            character.BirthFamily = c.BirthFamilyId.HasValue ? familiesById[c.BirthFamilyId.Value] : null;
            character.CurrentFamily = c.CurrentFamilyId.HasValue ? familiesById[c.CurrentFamilyId.Value] : null;
            character.Dynasty = c.DynastyId.HasValue ? dynastiesById[c.DynastyId.Value] : null;
            character.Settlement = c.SettlementId.HasValue ? settlementsById[c.SettlementId.Value] : null;
            character.Enemies = c.EnemyIds.Select(id => charactersById[id]).ToHashSet();
        }

        foreach (var f in data.Families)
        {
            var family = familiesById[f.Id];

            family.Father = charactersById[f.FatherId];
            family.Mother = charactersById[f.MotherId];
            family.Children = f.ChildrenIds.Select(id => charactersById[id]).ToList();
            family.Dynasty = f.DynastyId.HasValue ? dynastiesById[f.DynastyId.Value] : null;
        }

        foreach (var d in data.Dynasties)
        {
            var dynasty = dynastiesById[d.Id];

            dynasty.Members = d.MemberIds.Select(id => charactersById[id]).ToList();
            dynasty.Families = d.FamilyIds.Select(id => familiesById[id]).ToList();
            dynasty.Founder = charactersById[d.FounderId];
        }

        foreach (var s in data.Settlements)
        {
            var settlement = settlementsById[s.Id];

            settlement.Members = s.MemberIds.Select(id => charactersById[id]).ToList();
            settlement.Culture = s.CultureId.HasValue ? culturesById[s.CultureId.Value] : null;
            settlement.Religion = s.ReligionId.HasValue ? religionsById[s.ReligionId.Value] : null;
        }

        // Dynasty/Character/Settlement уже полностью связаны на этом этапе, но
        // Kingdom ссылается сам на себя через AlliedKingdoms — сначала заготовки,
        // потом отдельный проход простановки союзов (по тому же принципу, что выше)
        var kingdomsById = data.Kingdoms.ToDictionary(
            k => k.Id,
            k => new Kingdom
            {
                Id = k.Id,
                Name = k.Name,
                FoundedYear = k.FoundedYear,
                FallenYear = k.FallenYear,
                Dynasty = dynastiesById[k.DynastyId],
                Ruler = charactersById[k.RulerId],
                Settlements = k.SettlementIds.Select(id => settlementsById[id]).ToList(),
                FoodTreasury = k.FoodTreasury,
                MaterialTreasury = k.MaterialTreasury,
                GoldTreasury = k.GoldTreasury
            });

        foreach (var k in data.Kingdoms)
        {
            kingdomsById[k.Id].AlliedKingdoms = k.AlliedKingdomIds.Select(id => kingdomsById[id]).ToList();
            kingdomsById[k.Id].Suzerain = k.SuzerainId.HasValue ? kingdomsById[k.SuzerainId.Value] : null;
        }

        var world = new World
        {
            CurrentYear = data.CurrentYear,
            TotalBirths = data.TotalBirths,
            TotalDeaths = data.TotalDeaths,
            AliveCount = data.AliveCount,
            Settings = data.Settings,
            Events = data.Events,
            Characters = data.Characters.Select(c => charactersById[c.Id]).ToList(),
            Families = data.Families.Select(f => familiesById[f.Id]).ToList(),
            Dynasties = data.Dynasties.Select(d => dynastiesById[d.Id]).ToList(),
            Settlements = data.Settlements.Select(s => settlementsById[s.Id]).ToList(),
            Cultures = data.Cultures.Select(c => culturesById[c.Id]).ToList(),
            Religions = data.Religions.Select(r => religionsById[r.Id]).ToList(),
            Kingdoms = data.Kingdoms.Select(k => kingdomsById[k.Id]).ToList(),
            TradeRoutes = data.TradeRoutes.Select(r => new TradeRoute
            {
                A = settlementsById[r.SettlementAId],
                B = settlementsById[r.SettlementBId],
                Years = r.Years
            }).ToList()
        };

        // Продолжаем нумерацию Id с того места, где остановилось сохранение,
        // иначе новые персонажи/семьи/династии/поселения/культуры/королевства начнут конфликтовать со старыми
        CharacterGenerator.SetNextId(NextId(data.Characters.Select(c => c.Id)));
        FamilySystem.SetNextFamilyId(NextId(data.Families.Select(f => f.Id)));
        DynastySystem.SetNextDynastyId(NextId(data.Dynasties.Select(d => d.Id)));
        SettlementGenerator.SetNextId(NextId(data.Settlements.Select(s => s.Id)));
        CultureGenerator.SetNextId(NextId(data.Cultures.Select(c => c.Id)));
        ReligionGenerator.SetNextId(NextId(data.Religions.Select(r => r.Id)));
        KingdomSystem.SetNextId(NextId(data.Kingdoms.Select(k => k.Id)));

        return world;
    }

    private static int NextId(IEnumerable<int> ids)
    {
        var maxId = 0;

        foreach (var id in ids)
        {
            if (id > maxId)
            {
                maxId = id;
            }
        }

        return maxId + 1;
    }
}
