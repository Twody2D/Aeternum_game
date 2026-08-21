namespace Aeternum.WorldGen.Models;

// Тип материала — привязан к конкретной ремесленной профессии (см. ProfessionSystem.MaterialTypeByProfession)
public enum MaterialType
{
    Wood,
    Stone,
    Metal,
    Textile,
    Clay,
    Luxury // Товар редкости, не сырьё для построек — только на продажу (см. MarketSystem)
}
