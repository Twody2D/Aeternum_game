namespace Aeternum.WorldGen.Models;

// Должность при дворе. Государство до сих пор состояло из одного человека —
// правителя; всё остальное делалось само собой. Эти четверо — те роли, для
// которых в мире уже есть и кандидаты, и работа (см. CourtSystem)
public enum CourtOffice
{
    Heir,       // Наследник — ясность преемства (см. KingdomSystem)
    Marshal,    // Воевода — походы против мятежных земель (см. RebellionSystem)
    Treasurer,  // Казначей — сбор дани (см. TributeSystem)
    Chancellor  // Советник — накопление знания (см. TechnologySystem)
}
