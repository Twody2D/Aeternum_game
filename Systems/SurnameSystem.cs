using Aeternum.WorldGen.Models;

namespace Aeternum.WorldGen.Systems;

// Character.LastName хранит фамилию в канонической (мужской) форме — так проще
// сравнивать родство и вести родословную. Для вывода женщинам нужна склонённая
// форма (Комаров -> Комарова), этим занимается только этот класс
public static class SurnameSystem
{
    // Женская форма фамилии для отображения. Покрывает фамилии на -ов/-ев/-ёв/-ин
    // (весь текущий пул), для прочих типов (несклоняемые, на -ой/-ский и т.п.)
    // форма не меняется — правило можно расширить, когда появятся такие фамилии
    public static string GetDisplaySurname(Character character)
    {
        if (character.Gender != Gender.Female || character.LastName.Length == 0)
        {
            return character.LastName;
        }

        var lastChar = character.LastName[^1];

        return lastChar is 'в' or 'н'
            ? character.LastName + "а"
            : character.LastName;
    }

    // "Имя Фамилия" с фамилией в правильной для персонажа форме
    public static string GetDisplayFullName(Character character)
    {
        return $"{character.Name} {GetDisplaySurname(character)}";
    }
}
