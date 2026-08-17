namespace Aeternum.WorldGen.Models;


// Один житель мира. Связи с родителями/семьёй/династией — прямые ссылки на объекты
public class Character
{
    public int Id { get; set; } //Уникальный номер персонажа

    public string Name { get; set; } = ""; //Имя

    public string LastName { get; set; } = ""; //Текущая фамилия (меняется при браке)

    public int Age { get; set; } //Возраст
    public string? Profession { get; set; }  //Профессия

    public Gender Gender { get; set; }
    public Family? BirthFamily { get; set; } //Семья, в которой родился (не меняется)
    public Family? CurrentFamily { get; set; } //Семья, созданная браком; null — персонаж не женат/не замужем
    public Dynasty? Dynasty { get; set; } //Ссылка на династию персонажа
    public Character? Mother { get; set; } //Ссылка на мать персонажа
    public Character? Father { get; set; } //Ссылка на отца персонажа

    public bool Alive { get; set; } = true; //Жив ли

    public DeathReason DeathReason { get; set; } = DeathReason.None; //Причина смерти

    public LifeStage LifeStage { get; set; } //Этап жизни (младенец/ребёнок/ученик/взрослый/старик)

}
