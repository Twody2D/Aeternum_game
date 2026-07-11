using Aeternum.WorldGen.Core;

public static class EventPrinter
{
    public static void Print(World world)
    {
        foreach(var e in world.Events)
        {
            Console.WriteLine(
                $"{e.Year}: {e.Description}"
            );
        }
    }
}