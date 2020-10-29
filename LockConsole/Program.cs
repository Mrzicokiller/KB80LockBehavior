using Microsoft.Win32;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Globalization;
using System.Text;
using System.Linq;

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
        static DateTime lastActivity { get; set; }
        static DateTime lastActivityWithThreshold { get; set; }
        static DateTime currentTime = DateTime.Now;
        static List<DataMessage> logMessages { get; set; } = new List<DataMessage>();
        static ConfigurationObject configuration;

        static void Main(string[] args)
        {
            configuration = ConfigurationObject.getConfiguration();

            SystemEvents.SessionSwitch += new SessionSwitchEventHandler(SystemEvents_SessionSwitch);

            if (FileManager.checkIfFileExcists("lockEventLog.json"))
            {
                if (JsonConvert.DeserializeObject<List<DataMessage>>(File.ReadAllText(@"lockEventLog.json")) != null)
                {
                    logMessages = new List<DataMessage>(JsonConvert.DeserializeObject<List<DataMessage>>(File.ReadAllText(@"lockEventLog.json")));
                }
            }
            else
            {
                FileManager.createLogFile("lockEventLog.json");
            }

            Console.WriteLine("Program succesfully started");
            if(configuration.dryRunMode)
            {
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine("Dry run mode is enabled");
                Console.BackgroundColor = ConsoleColor.Black;
            }

            do
            {
                GetInactivityTime();
                CheckInactivityThreshold();

                currentTime = DateTime.Now;
                syncLogWithAPI();

                if(configuration.dryRunMode)
                {
                    dryRunModeLogger();
                }

                Thread.Sleep(2000);
            } while (currentTime < DateTime.Parse(configuration.EndTime));
        }

        /// <summary>
        /// Observer: When a SystemEvent.SessionSwitch is fired checks if SessionLock event is fired
        /// </summary>
        static void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            if (e.Reason == SessionSwitchReason.SessionLock)
            {
                if(configuration.dryRunMode)
                {
                    Console.BackgroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("locked");
                    Console.BackgroundColor = ConsoleColor.Black;
                }
                isLocked = true;
            }
            else if (e.Reason == SessionSwitchReason.SessionUnlock)
            {
                if(configuration.dryRunMode)
                {
                    Console.BackgroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("unlocked");
                    Console.BackgroundColor = ConsoleColor.Black;
                }
                isLocked = false;
            }
        }

        /// <summary>
        /// Get last input timestamp and set in global variable and threshold time
        /// </summary>
        static void GetInactivityTime()
        {
            LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
            lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);
            GetLastInputInfo(ref lastInputInfo);
            DateTime fetchedTime = DateTime.Now.AddMilliseconds(-(Environment.TickCount - lastInputInfo.dwTime));
            if (DateTime.Compare(lastActivity, fetchedTime) < 0)
            {
                lastActivity = fetchedTime;
                lastActivityWithThreshold = fetchedTime.Add(TimeSpan.Parse(configuration.InActivityThreshold));
                isInactiveAfterThreshold = false;
            }
        }

        /// <summary>
        /// Check if set threshold has been past and set the boolean isInactiveAfterThreshold and creates message and writes to the log
        /// </summary>
        static void CheckInactivityThreshold()
        {
            if (DateTime.Compare(lastActivityWithThreshold, DateTime.Now) < 0)
            {
                if (isInactiveAfterThreshold != true)
                {
                    FileManager.writeToLog(DataMessage.createMessage(configuration.userID, DateTime.Now, isLocked, configuration.location, "threshold has been past", false, ConferencePrograms.checkForActiveConferenceProgram(configuration.conferencePrograms), configuration.dryRunMode), logMessages, "lockEventLog.json"); ;
                    isInactiveAfterThreshold = true;
                }
            }
            else
            {
                isInactiveAfterThreshold = false;
            }
        }

        /// <summary>
        /// This function is used to synchronize the local event log with the connected database via the endpoint backend
        /// </summary>
        static void syncLogWithAPI()
        {
            List<DataMessage> dataMessagesInLog = FileManager.readLogFile("lockEventLog.json");
            List<DataMessage> updatedLog = new List<DataMessage>();

            if (dataMessagesInLog != null)
            {
                using (var client = new HttpClient())
                {
                    dataMessagesInLog.ForEach(delegate (DataMessage dataMessage)
                    {
                        if (!dataMessage.APISucces)
                        {
                            try
                            {
                                if (configuration.dryRunMode)
                                {
                                    Console.BackgroundColor = ConsoleColor.DarkGreen;
                                    Console.WriteLine("Posting one message...");
                                    Console.BackgroundColor = ConsoleColor.Black;
                                }
                                var postContent = new StringContent(JsonConvert.SerializeObject(dataMessage), Encoding.UTF8, "application/json");
                                var response = client.PostAsync(configuration.dataPostURL, postContent).Result;

                                if(configuration.dryRunMode)
                                {
                                    Console.BackgroundColor = ConsoleColor.DarkGreen;
                                    Console.WriteLine(response.Content.ReadAsStringAsync().Result);
                                    Console.BackgroundColor = ConsoleColor.Black;
                                }

                                if (response.IsSuccessStatusCode)
                                {
                                    dataMessage.APISucces = true;
                                }
                                updatedLog.Add(dataMessage);
                            }
                            catch
                            {
                                updatedLog.Add(dataMessage);
                            }
                            
                        }
                        else
                        {
                            updatedLog.Add(dataMessage);
                        }
                    });
                    FileManager.updateEventLog(updatedLog, "lockEventLog.json");
                    logMessages = updatedLog;
                }

            }
        }

        /// <summary>
        /// logs data while in dry run mode
        /// </summary>
        static void dryRunModeLogger()
        {
            Console.WriteLine("Current Time: " + currentTime);
            Console.WriteLine("Last Activity Timestamp: " + lastActivity);
            Console.WriteLine("Set Threshold: " + configuration.InActivityThreshold);
            Console.WriteLine("Last Activity with Threshold: " + lastActivityWithThreshold);
            Console.WriteLine("Lock Status: " + isLocked);
            Console.WriteLine("Active Conference Programs:" + String.Join(',' , ConferencePrograms.getActiveConferencePrograms(configuration.conferencePrograms).Distinct()));

        }

    }
}
