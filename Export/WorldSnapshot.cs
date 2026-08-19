namespace Aeternum.WorldGen.Export;

// Плоский снимок мира для отрисовки. Это не сохранение: SaveSystem пишет всё,
// что нужно, чтобы симуляцию можно было продолжить, — со ссылками по Id,
// служебными счётчиками и полной родословной каждого когда-либо жившего.
// Клиенту, который рисует карту и таймлайн, нужно ровно обратное: немного
// готовых к показу чисел и подписей, уже собранных воедино, без обходов графа
// на стороне отрисовки и без всего, что не видно на экране.
//
// Отдельный слой нужен ещё и потому, что у этих двух форматов разные причины
// меняться: сохранение обязано следовать за моделью, а снимок — за тем, что
// клиент показывает
public record WorldSnapshot(
    int Year,
    string Era,
    double Knowledge,
    int Population,
    int TotalBirths,
    int TotalDeaths,
    double MapSize,
    List<SettlementSnapshot> Settlements,
    List<KingdomSnapshot> Kingdoms,
    List<TradeRouteSnapshot> TradeRoutes,
    List<EventSnapshot> Timeline);

// Всё, что нужно, чтобы поставить точку на карте и показать карточку по клику
public record SettlementSnapshot(
    int Id,
    string Name,
    double X,
    double Y,
    int Population,
    double Fertility,
    string? Culture,
    string? Religion,
    int Houses,
    int Hospitals,
    int Schools,
    int Walls,
    int Legends,
    bool IsUnderSiege,
    bool IsRebelling,
    string? RulingKingdom);

// Государство как политический слой поверх карты: какие точки закрасить и чем подписать
public record KingdomSnapshot(
    int Id,
    string Name,
    string Ruler,
    string Dynasty,
    int FoundedYear,
    int? FallenYear,
    double Reputation,
    string? Suzerain,
    List<string> Allies,
    List<int> SettlementIds);

// Линия между двумя точками карты, толщина которой говорит о возрасте связи
public record TradeRouteSnapshot(int FromSettlementId, int ToSettlementId, int Years);

// Строка таймлайна. Тип отдаётся отдельно от текста, чтобы клиент мог
// фильтровать и раскрашивать события, не разбирая русские фразы
public record EventSnapshot(int Year, string Type, string Description);
