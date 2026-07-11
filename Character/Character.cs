namespace Aeternum.WorldGen.Characters;


public class Character
{
    public string Name { get; set; } = ""; //Имя

    public int Age { get; set; } //Возраст

    public bool Alive { get; set; } = true; //Жив ли

    public string? Profession { get; set; }  //Профессия
    public LifeStage LifeStage { get; set; } //Этап жизни
}