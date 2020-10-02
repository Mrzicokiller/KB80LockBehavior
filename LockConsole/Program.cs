using Microsoft.Win32;
using System;
using System.IO;
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
            writeToLog(DateTime.Now, "Test message 2");
            readLogFile("2-10-2020.txt");

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

        /// <summary>
        /// Write message to correct file
        /// </summary>
        /// <param name="logFileDate">The date of the day (file date name)</param>
        /// <param name="message">The message that needs to be written to the file</param>
        static void writeToLog(DateTime logFileDate, string message)
        {
            string fileName = logFileDate.ToShortDateString() + ".txt";
            if(checkIfLogExcists(fileName))
            {
                using (StreamWriter file = new StreamWriter(fileName, true))
                {
                    file.WriteLine(message);
                }
            }
            else
            {
                if (createLogFile(fileName))
                {
                    Console.WriteLine("file succesfull created");
                }
                else
                {
                    Console.WriteLine("file not created");
                }
            }

            //Console.WriteLine("FileName: " + fileName);
            //Console.WriteLine("Message: " + message);

        }

        /// <summary>
        /// Read data from given filename
        /// </summary>
        /// <param name="fileName">The name of the file that needs to be read</param>
        static void readLogFile(string fileName)
        {
            string[] fileData = File.ReadAllLines(fileName);
            foreach(string dataLine in fileData)
            {
                Console.WriteLine(dataLine);
            }
        }

        /// <summary>
        /// Create the log file
        /// </summary>
        /// <param name="fileName">The name of the file that needs to be created</param>
        /// <returns></returns>
        static bool createLogFile(string fileName)
        {
            File.Create(fileName);
            return (checkIfLogExcists(fileName));
        }

        /// <summary>
        /// Check if log already excits
        /// </summary>
        /// <param name="fileName">The fileName of the log file that needs to be checked</param>
        static bool checkIfLogExcists(string fileName)
        {
            Console.WriteLine(fileName);
            if (File.Exists(fileName))
            {
                Console.WriteLine("found");
                return true;
            }
            else
            {
                Console.WriteLine("not found");
                return false;
            }

        }

    }
}
