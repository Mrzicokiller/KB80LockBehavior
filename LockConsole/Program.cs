using Microsoft.Win32;
using System;

namespace LockConsole
{
    class Program
    {
        static bool isLocked { get; set; } = false;
        static bool isInActive { get; set; } = false;
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            SystemEvents.SessionSwitch += new SessionSwitchEventHandler(SystemEvents_SessionSwitch);
            var key = Console.ReadKey();
            while(key.Key != ConsoleKey.Escape)
            if (key.Key == ConsoleKey.Escape)
            {
                Environment.Exit(0);
            }

        }

        // Observer: When a SystemEvent.SessionSwitch is fired checks if SessionLock event is fired
        static void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            if (e.Reason == SessionSwitchReason.SessionLock)
            {
                Console.WriteLine("locked");
                isLocked = true;
            }
            else if (e.Reason == SessionSwitchReason.SessionUnlock)
            {
                Console.WriteLine("unlocked");
                isLocked = false;
            }
        }

    }
    /*
     Test code:
     for(int i = 0; i < 10; i++)
     {
        Console.WriteLine("Boolean isLocked: " + isLocked);
        Thread.Sleep(500);
     }
     */
}
