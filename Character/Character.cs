namespace Aeternum.WorldGen.Characters;


public class Character
{
    public string Name { get; set; } = ""; //Имя

    public int Age { get; set; } //Возраст
    public string? Profession { get; set; }  //Профессия

    public Gender Gender { get; set; }
    public Character? Mother { get; set; } //Ссылка на мать персонажа
    public Character? Father { get; set; } //Ссылка на отца персонажа

    public bool Alive { get; set; } = true; //Жив ли

    public DeathReason DeathReason { get; set; } = DeathReason.None; //Причина смерти

    public LifeStage LifeStage { get; set; } //Этап жизни

}