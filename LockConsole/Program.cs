using Microsoft.Win32;
using System;

namespace LockConsole
{
    class Program
    {
        static bool isLocked { get; set; } = false;
        static bool isInActive { get; set; } = false;
        static DateTime lastActivity { get; set; } = DateTime.Now;
        static void Main(string[] args)
        {
            ConsoleKeyInfo keyInput;
            SystemEvents.SessionSwitch += new SessionSwitchEventHandler(SystemEvents_SessionSwitch);

            Console.WriteLine("Hello World!");
            do
            {
                keyInput = Console.ReadKey();
                Console.WriteLine("Checking for ESC");
            } while (keyInput.Key != ConsoleKey.Escape);

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
}
