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
        static bool isInactiveAfterThreshold { get; set; } = false;
        static DateTime lastActivity { get; set; } = DateTime.Now;
        static DateTime lastActivityWithThreshold { get; set; }
        static DateTime currentTime = DateTime.Now;
        static void Main(string[] args)
        {
            SystemEvents.SessionSwitch += new SessionSwitchEventHandler(SystemEvents_SessionSwitch);

            Console.WriteLine("Hello World!");

            do
            {
                GetInactivityTime();
                //Console.WriteLine("last activity: " + lastActivity);
                //Console.WriteLine("last activity with treshold: " + lastActivityWithThreshold);
                CheckInactivityThreshold();

                /*keyInput = Console.ReadKey();
                Console.WriteLine("Checking for ESC");
                if (keyInput.Key == ConsoleKey.Escape)
                {
                    Environment.Exit(0);
                }*/

                currentTime = DateTime.Now;
                Thread.Sleep(2000);
            } while (currentTime < DateTime.Parse("18:00:00"));
            


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

        // Get last input timestamp and set in global variable and threshold time
        static void GetInactivityTime()
        {
            LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
            lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);
            GetLastInputInfo(ref lastInputInfo);
            DateTime fetchedTime = DateTime.Now.AddMilliseconds(-(Environment.TickCount - lastInputInfo.dwTime));
            if(lastActivity != fetchedTime)
            {
                lastActivity = fetchedTime;
                lastActivityWithThreshold = fetchedTime.Add(new TimeSpan(00,01,00));
            }
        }

        // Check if set threshold has been past and create http request or write in log of http is not posible
        static void CheckInactivityThreshold()
        {
            if(lastActivityWithThreshold < DateTime.Now)
            {
                Console.WriteLine("Threshold has been past");
                isInactiveAfterThreshold = true;
            }
            else
            {
                isInactiveAfterThreshold = false;
            }
        }

    }
}
