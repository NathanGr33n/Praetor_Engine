using System;

namespace Praetor_Engine
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new Game1())
                game.Run;
        }
    }
}