using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;

namespace Aeternum.WorldGen.Systems;

// Биография. Жизнь персонажа была рассыпана по миру: семья хранит только
// нынешний брак (Character.CurrentFamily), опекунство — только сегодняшнего
// подопечного (Character.Guardian), двор — только того, кто на месте прямо
// сейчас (Kingdom.Court). Прошлое при этом не стёрто, просто не собрано вместе:
// распавшийся брак остаётся в World.Families — при разводе или вдовстве
// свободными для нового брака становятся стороны, не сама семья (см.
// DivorceSystem, DeathSystem.Kill); бывший подопечный — обычный World.Characters
// с Guardian, всё ещё указывающим на того, кто его вырастил.
//
// BiographySystem ничего не хранит заново — только собирает уже существующие
// ссылки в одно место и в один порядок. Русский текст из них строит Program.cs,
// тем же разделением, что и у остальных отчётов (см. Models/Reports.cs)
public static class BiographySystem
{
    public static Biography Build(Character character, World world)
    {
        var ownFamilies = world.Families
            .Where(f => f.Father == character || f.Mother == character)
            .OrderBy(f => f.FormedYear)
            .ToList();

        var marriages = ownFamilies
            .Select(f =>
            {
                var spouse = f.Father == character ? f.Mother : f.Father;
                return new MarriageRecord(spouse, f.FormedYear, GetStatus(f, character, spouse));
            })
            .ToList();

        var children = ownFamilies
            .SelectMany(f => f.Children)
            .OrderBy(c => c.BirthYear)
            .ToList();

        var wards = world.Characters
            .Where(c => c.Guardian == character)
            .OrderBy(c => c.BirthYear)
            .ToList();

        var rulesKingdom = world.Kingdoms.FirstOrDefault(k => k.FallenYear == null && k.Ruler == character);

        var heldOffice = world.Kingdoms
            .Where(k => k.FallenYear == null)
            .SelectMany(k => k.Court.Where(kv => kv.Value == character).Select(kv => (Kingdom: k, Office: (CourtOffice?)kv.Key)))
            .FirstOrDefault();

        return new Biography(character, marriages, children, wards, rulesKingdom, heldOffice.Office, heldOffice.Kingdom);
    }

    // "Действующий" — оба ещё в этом браке и оба живы. Условие о живости не сводится
    // к сравнению CurrentFamily: при смерти освобождается для нового брака только
    // переживший супруг (см. DeathSystem.Kill), CurrentFamily умершего так и остаётся
    // указывать на последнюю семью. По факту сравнения CurrentFamily здесь бы уже
    // хватило (кто-то из двоих неизбежно оказывается развязан), но это чужой побочный
    // эффект — определение "действующего брака" через собственную живость обеих
    // сторон не зависит от того, как именно DeathSystem наводит порядок в ссылках
    private static MarriageStatus GetStatus(Family family, Character character, Character spouse)
    {
        var stillTogether = character.Alive && spouse.Alive &&
                             character.CurrentFamily == family && spouse.CurrentFamily == family;

        return stillTogether ? MarriageStatus.Current : MarriageStatus.Ended;
    }
}
