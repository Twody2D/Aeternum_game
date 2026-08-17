namespace Aeternum.WorldGen.Models;

// Категория профессии — определяет вклад персонажа в производство еды (см. EconomySystem)
public enum ProfessionCategory
{
    FoodProducer, // Фермер, рыбак, охотник и т.п. — основной источник еды
    General,      // Разнорабочие без узкой специализации
    Craft,        // Ремесленники и строители
    Trade,        // Торговля
    Knowledge,    // Учёные, лекари, музыканты и т.п.
    Military      // Воины, солдаты, матросы
}
