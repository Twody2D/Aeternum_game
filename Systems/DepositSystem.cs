using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;

namespace Aeternum.WorldGen.Systems;

// Месторождения. До сих пор тип сырья решала только профессия (см.
// ProfessionSystem.GetMaterialProduction) — кузнец давал металл, столяр дерево,
// где бы они ни жили. Место при этом ничего не решало: кузница в горах и
// кузница в чистом поле ковали одинаково, хотя руда в чистом поле не берётся
// из ниоткуда.
//
// Рельеф уже отвечает на вопрос "где что есть" (см. TerrainSystem) — тем же
// приёмом, без единого нового поля: камень и металл берутся из земли, поэтому
// их даёт гористая местность, а не пашня; дерево и глина, наоборот, берутся
// с той земли, что годится под лес и берег, — низина, а не голый камень.
// Ткани ни с тем ни с другим не связаны — сырьё для них (лён, шерсть) с ремеслом,
// а не с рельефом, поэтому не тронуты. Роскошь (см. MaterialType.Luxury) —
// по той же причине: работа ювелира ценится мастерством (см. GuildSystem),
// а не тем, у подножия какой горы он родился
public static class DepositSystem
{
    private const double MountainOreBonus = 1.6; // Камень и металл — из-под земли
    private const double HillOreBonus = 1.3;
    private const double LowlandOrePenalty = 0.6; // На голой пашне ни жилы, ни карьера

    private const double LowlandTimberBonus = 1.4; // Дерево и глина — с леса и берега
    private const double MountainTimberPenalty = 0.6;

    private const double LowlandClayBonus = 1.3;
    private const double MountainClayPenalty = 0.7;

    // Множитель к добыче конкретного сырья в этом поселении (см. EconomySystem)
    public static double GetYieldMultiplier(Settlement settlement, MaterialType type, World world)
    {
        var relief = TerrainSystem.GetRelief(settlement, world);

        return (type, relief) switch
        {
            (MaterialType.Stone or MaterialType.Metal, Relief.Mountain) => MountainOreBonus,
            (MaterialType.Stone or MaterialType.Metal, Relief.Hill) => HillOreBonus,
            (MaterialType.Stone or MaterialType.Metal, Relief.Lowland) => LowlandOrePenalty,

            (MaterialType.Wood, Relief.Lowland) => LowlandTimberBonus,
            (MaterialType.Wood, Relief.Mountain) => MountainTimberPenalty,

            (MaterialType.Clay, Relief.Lowland) => LowlandClayBonus,
            (MaterialType.Clay, Relief.Mountain) => MountainClayPenalty,

            _ => 1.0
        };
    }
}
