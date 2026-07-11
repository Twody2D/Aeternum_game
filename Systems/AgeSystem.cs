using Aeternum.WorldGen.Models;
using Aeternum.WorldGen.Core;

namespace Aeternum.WorldGen.Systems;


public static class AgeSystem
{
    public static void Process(World world)
    {
        foreach(var character in world.Characters)
        {
            if (!character.Alive)
            {
                continue;
            }


            character.Age++;


            LifeSystem.UpdateLifeStage(character);


            LifeSystem.AssignProfession(character);
        }
    }
}