using Aeternum.WorldGen.Models;

namespace Aeternum.WorldGen.Systems;

// Наследование престола. Раньше правило было одно на весь мир — корона всегда
// уходила старейшему живому члену дома. Теперь обычай принадлежит культуре
// правящего дома (Culture.SuccessionLaw): один народ чтит старшинство, другой —
// первородство, третий выбирает достойнейшего.
//
// Разница обычаев не остаётся косметической: KingdomSystem считает кризисом
// наследования переход трона не к прямому потомку — а значит, при первородстве
// распри вспыхивают реже, чем при старшинстве или выборности, где корона
// сплошь и рядом уходит в боковую ветвь
public static class SuccessionSystem
{
    // Кто наследует корону среди живых членов дома. Список гарантированно непуст:
    // вызывающий код (KingdomSystem) отдельно обрабатывает угасший дом
    public static Character PickHeir(List<Character> aliveMembers, Kingdom kingdom, Character previousRuler)
    {
        var law = kingdom.Ruler.Settlement?.Culture?.SuccessionLaw ?? SuccessionLaw.Seniority;

        return law switch
        {
            SuccessionLaw.Primogeniture => PickFirstborn(aliveMembers, previousRuler),
            SuccessionLaw.Election => PickWorthiest(aliveMembers),
            _ => PickSenior(aliveMembers)
        };
    }

    // Старейший живой член дома — исходный обычай мира
    public static Character PickSenior(List<Character> aliveMembers)
    {
        return aliveMembers.OrderBy(m => m.BirthYear).First();
    }

    // Старший из детей покойного правителя; если своих детей не осталось —
    // обычай молчит, и корона уходит по старшинству
    private static Character PickFirstborn(List<Character> aliveMembers, Character previousRuler)
    {
        var children = aliveMembers
            .Where(m => m.Mother == previousRuler || m.Father == previousRuler)
            .OrderBy(m => m.BirthYear)
            .ToList();

        return children.Count > 0 ? children[0] : PickSenior(aliveMembers);
    }

    // Достойнейший: дом смотрит не на возраст, а на вес человека — сколько за ним
    // друзей и союзов (Character.Friends) и насколько он не нажил себе врагов
    // внутри дома (Character.Enemies). Оба поля уже есть в мире и заведены не
    // ради этого — сюда они ложатся сами
    private static Character PickWorthiest(List<Character> aliveMembers)
    {
        return aliveMembers
            .OrderByDescending(m => m.Friends.Count(f => f.Alive) - m.Enemies.Count(e => e.Alive))
            .ThenBy(m => m.BirthYear)
            .First();
    }
}
