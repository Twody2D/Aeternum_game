using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;
using Aeternum.WorldGen.Events;

namespace Aeternum.WorldGen.Systems;

// Языковой барьер. Культуры в мире были, а понимать друг друга это никак
// не мешало: чужой народ отличался от своего только предпочтениями в ремесле.
//
// Язык — не второе имя культуры: на одном наречии говорят несколько
// родственных народов (см. LanguageGenerator), поэтому языковая граница
// проходит не там, где культурная, и государство вполне может оказаться
// разноязыким.
//
// Барьер поставлен там, где до сих пор его не было вовсе: в обмене товаром
// между поселениями (см. TradeSystem) и в дипломатии (см. AllianceSystem).
// В брак он не добавлен намеренно — там уже есть и вера, и обычай, и третий
// штраф просто добил бы межпоселенческие свадьбы.
//
// Ассимиляция идёт по соседству: речь перенимают у тех, с кем живут бок о бок.
// Считается живое население в округе, а не число поселений, — поэтому крупный
// сосед навязывает наречие, а хутор рядом с большим городом теряет своё.
//
// Сначала это было сделано через наезженные торговые пути (TradeRoute), но
// замер показал, что путей в мире всего 0-2: обмен возникает только при
// дефиците, а склады и рынок закрывают его раньше. Механика на таком
// основании была бы мертва
public static class LanguageSystem
{
    private const double ForeignTradeFactor = 0.7; // Насколько хуже идёт обмен через языковую границу
    private const double SharedLanguageAllianceBonus = 2.0; // Во столько раз охотнее сговариваются понимающие друг друга

    private const double NeighbourDistance = 200; // Округа, речь которой слышна каждый день
    private const double AssimilationMargin = 1.5; // Во сколько раз чужих должно быть больше своих, чтобы своё наречие дрогнуло
    private const double AssimilationChance = 0.02; // Шанс в год перенять наречие соседей, когда их перевес устоялся

    public static void Process(World world)
    {
        foreach (var settlement in world.Settlements)
        {
            if (settlement.Language == null || settlement.Members.All(m => !m.Alive))
            {
                continue;
            }

            var dominant = FindDominantNeighbourLanguage(settlement, world);

            if (dominant == null || Rng.NextDouble() >= AssimilationChance)
            {
                continue;
            }

            var previous = settlement.Language;
            settlement.Language = dominant;

            world.Events.Add(new WorldEvent
            {
                Year = world.CurrentYear,
                Type = EventType.Assimilation,
                Description = $"{settlement.Name}: {previous.Name} вытеснено — теперь здесь говорят на {dominant.Name.ToLower()}"
            });
        }
    }

    // Наречие округи, перевесившее собственное. Меряется живыми людьми,
    // а не поселениями: чей голос слышнее, того речь и берут
    private static Language? FindDominantNeighbourLanguage(Settlement settlement, World world)
    {
        var speakersByLanguage = new Dictionary<Language, int>();

        foreach (var other in world.Settlements)
        {
            if (other == settlement || other.Language == null || GetDistance(settlement, other) > NeighbourDistance)
            {
                continue;
            }

            var speakers = other.Members.Count(m => m.Alive);

            if (speakers == 0)
            {
                continue;
            }

            speakersByLanguage[other.Language] = speakersByLanguage.GetValueOrDefault(other.Language) + speakers;
        }

        if (speakersByLanguage.Count == 0)
        {
            return null;
        }

        // Свои — это и собственные жители, и единоязычные соседи
        var own = settlement.Members.Count(m => m.Alive) + speakersByLanguage.GetValueOrDefault(settlement.Language!);

        var strongest = speakersByLanguage
            .Where(kv => kv.Key != settlement.Language)
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key.Id)
            .ToList();

        return strongest.Count > 0 && strongest[0].Value > own * AssimilationMargin ? strongest[0].Key : null;
    }

    private static double GetDistance(Settlement a, Settlement b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;

        return Math.Sqrt(dx * dx + dy * dy);
    }

    public static bool SharesLanguage(Settlement? a, Settlement? b)
    {
        return a?.Language != null && a.Language == b?.Language;
    }

    // Множитель к обмену товаром: через языковую границу договариваться труднее
    public static double GetTradeFactor(Settlement a, Settlement b)
    {
        return SharesLanguage(a, b) ? 1.0 : ForeignTradeFactor;
    }

    // Множитель к шансу союза: понимающие друг друга сговариваются охотнее
    public static double GetDiplomacyFactor(Kingdom a, Kingdom b)
    {
        return SharesLanguage(a.Ruler.Settlement, b.Ruler.Settlement) ? SharedLanguageAllianceBonus : 1.0;
    }
}
