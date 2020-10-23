using Microsoft.Win32;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Globalization;

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
        static SettingsObject configuration;

        static void Main(string[] args)
        {
            configuration = getConfiguration();

            SystemEvents.SessionSwitch += new SessionSwitchEventHandler(SystemEvents_SessionSwitch);

            if (checkIfFileExcists(currentTime.ToString("d", CultureInfo.CreateSpecificCulture("nl-NL")) + ".json"))
            {
                if (JsonConvert.DeserializeObject<List<DataMessage>>(File.ReadAllText(@currentTime.ToString("d", CultureInfo.CreateSpecificCulture("nl-NL")) + ".json")) != null)
                {
                    logMessages = new List<DataMessage>(JsonConvert.DeserializeObject<List<DataMessage>>(File.ReadAllText(@currentTime.ToString("d", CultureInfo.CreateSpecificCulture("nl-NL")) + ".json")));
                }
            }
            else
            {
                createLogFile(currentTime.ToString("d", CultureInfo.CreateSpecificCulture("nl-NL")) + ".json");
            }

            Console.WriteLine("Hello World!");
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
            if (DateTime.Compare(lastActivity, fetchedTime) < 0)
            {
                lastActivity = fetchedTime;
                lastActivityWithThreshold = fetchedTime.Add(TimeSpan.Parse(configuration.InActivityThreshold));
                isInactiveAfterThreshold = false;
            }
        }

        // Check if set threshold has been past and create http request or write in log of http is not posible
        static void CheckInactivityThreshold()
        {
            if (DateTime.Compare(lastActivityWithThreshold, DateTime.Now) < 0)
            {
                if (isInactiveAfterThreshold != true)
                {
                    writeToLog(createMessage(DateTime.Now, isLocked, configuration.location, "threshold has been past", false));
                }
                isInactiveAfterThreshold = true;
            }
            else
            {
                isInactiveAfterThreshold = false;
            }
        }

        /// <summary>
        /// Writes message to file
        /// </summary>
        /// <param name="dataMessage">the dataMessage object</param>
        static void writeToLog(DataMessage dataMessage)
        {
            if (logMessages == null || !logMessages.Contains(dataMessage))
            {
                logMessages.Add(dataMessage);
                updateEventLog(logMessages);
            }

        }

        /// <summary>
        /// Read data from given filename
        /// </summary>
        /// <param name="fileName">The name of the file that needs to be read</param>
        static List<DataMessage> readLogFile(string fileName)
        {
            List<DataMessage> dataMessages;

            using (StreamReader reader = new StreamReader(fileName))
            {
                string json = reader.ReadToEnd();
                dataMessages = JsonConvert.DeserializeObject<List<DataMessage>>(json);
            }

            return dataMessages;
        }

        /// <summary>
        /// Create the log file
        /// </summary>
        /// <param name="fileName">The name of the file that needs to be created</param>
        /// <returns></returns>
        static bool createLogFile(string fileName)
        {
            File.Create(fileName).Close();
            return (checkIfFileExcists(fileName));
        }

        /// <summary>
        /// Check if log already excits
        /// </summary>
        /// <param name="fileName">The fileName of the log file that needs to be checked</param>
        static bool checkIfFileExcists(string fileName)
        {
            if (File.Exists(fileName))
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        /// <summary>
        /// Updates the logfile with the logMessages list
        /// </summary>
        static void updateEventLog(List<DataMessage> updatedLog)
        {
            string fileName = currentTime.ToString("d", CultureInfo.CreateSpecificCulture("nl-NL")) + ".json";
            if (!checkIfFileExcists(fileName))
            {
                if (createLogFile(fileName))
                {
                    Console.WriteLine("file succesfull created");
                }
                else
                {
                    throw new IOException("file not created");
                }

            }

            using (StreamWriter file = new StreamWriter(fileName, false))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, updatedLog);

            }
        }

        /// <summary>
        /// This function is used to synchronize the local event log with the connected database via the endpoint backend
        /// </summary>
        static void syncLogWithAPI()
        {
            List<DataMessage> dataMessagesInLog = readLogFile(currentTime.ToString("d", CultureInfo.CreateSpecificCulture("nl-NL")) + ".json");
            List<DataMessage> updatedLog = new List<DataMessage>();
            string url = "http://127.0.0.1:8000/lockObject";

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
                                var response = client.PostAsJsonAsync(url, dataMessage).Result;
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
                    updateEventLog(updatedLog);
                    logMessages = updatedLog;
                }

            }
        }

        static DataMessage createMessage(DateTime timeStamp, bool isLocked, string location, string message, bool APISucces)
        {
            return new DataMessage
            {
                userId = configuration.userID,
                timeStamp = timeStamp.ToString("yyyy-MM-dd HH:mm:ss"),
                locked = isLocked,
                location = location,
                message = message,
                APISucces = APISucces
            };
        }

        static void createConfigurationFile()
        {
            File.Create("configuration.json").Close();
            SettingsObject standardConfiguration = new SettingsObject { userID = "testUser", location = "home", BeginTime = "08:00:00", EndTime = "17:00:00", InActivityThreshold = "00:01:00", dryRunMode = true };
            using (StreamWriter file = new StreamWriter("configuration.json", false))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, standardConfiguration);

            }
        }

        static SettingsObject getConfiguration()
        {
            SettingsObject configuration;
            if (!checkIfFileExcists("configuration.json"))
            {
                createConfigurationFile();
            }

            using (StreamReader reader = new StreamReader("configuration.json"))
            {
                string json = reader.ReadToEnd();
                configuration = JsonConvert.DeserializeObject<SettingsObject>(json);
            }

            return configuration;
        }

        static void dryRunModeLogger()
        {
            Console.WriteLine("Current Time: " + currentTime);
            Console.WriteLine("Last Activity Timestamp: " + lastActivity);
            Console.WriteLine("Set Threshold: " + configuration.InActivityThreshold);
            Console.WriteLine("Last Activity with Threshold: " + lastActivityWithThreshold);
            Console.WriteLine("Lock Status: " + isLocked);

        }

    }

    public class DataMessage
    {
        public string userId { get; set; }
        public string timeStamp { get; set; }
        public bool locked { get; set; }
        public string location { get; set; }
        public string message { get; set; }
        public bool APISucces { get; set; }
    }

    public class SettingsObject
    {
        public string userID { get; set; }
        public string location { get; set; }
        public string BeginTime { get; set; }
        public string EndTime { get; set; }
        public string InActivityThreshold { get; set; }
        public bool dryRunMode { get; set; }

    }
}
