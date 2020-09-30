using Microsoft.Win32;
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace LockConsole
{
    class Program
    {
        [DllImport("User32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);
        internal struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }
        static bool isLocked { get; set; } = false;
        static bool isInActive { get; set; } = false;
        static DateTime lastActivity { get; set; } = DateTime.Now;
        static DateTime currentTime = DateTime.Now;
        static void Main(string[] args)
        {
            SystemEvents.SessionSwitch += new SessionSwitchEventHandler(SystemEvents_SessionSwitch);
            
            Console.WriteLine("Hello World!");

            do
            {
                CheckInActivity();
                Console.WriteLine(lastActivity);

                /*keyInput = Console.ReadKey();
                Console.WriteLine("Checking for ESC");
                if (keyInput.Key == ConsoleKey.Escape)
                {
                    Environment.Exit(0);
                }*/

                currentTime = DateTime.Now;
                Thread.Sleep(2000);
            } while (currentTime < DateTime.Parse("14:21:00"));
            


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

        static void CheckInActivity()
        {
            LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
            lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);
            GetLastInputInfo(ref lastInputInfo);
            DateTime fetchedTime = DateTime.Now.AddMilliseconds(-(Environment.TickCount - lastInputInfo.dwTime));
            if(lastActivity != fetchedTime)
            {
                lastActivity = fetchedTime;
            }
        }

    }
}
