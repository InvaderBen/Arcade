using System;

namespace SpaceInvaders
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new SpaceInvadersGame())
                game.Run();
        }
    }
}