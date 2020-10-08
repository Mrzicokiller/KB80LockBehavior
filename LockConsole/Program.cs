using Microsoft.Win32;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

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

        static void Main(string[] args)
        {
            SystemEvents.SessionSwitch += new SessionSwitchEventHandler(SystemEvents_SessionSwitch);
            if(checkIfLogExcists(DateTime.Now.ToShortDateString() + ".json"))
            {
                if(JsonConvert.DeserializeObject<List<DataMessage>>(File.ReadAllText(@DateTime.Now.ToShortDateString() + ".json")) != null)
                {
                    logMessages = new List<DataMessage>(JsonConvert.DeserializeObject<List<DataMessage>>(File.ReadAllText(@DateTime.Now.ToShortDateString() + ".json")));
                }
            }
            else
            {
                createLogFile(DateTime.Now.ToShortDateString() + ".json");
            }

            Console.WriteLine("Hello World!");

            do
            {
                GetInactivityTime();
                CheckInactivityThreshold();

                /*keyInput = Console.ReadKey();
                Console.WriteLine("Checking for ESC");
                if (keyInput.Key == ConsoleKey.Escape)
                {
                    Environment.Exit(0);
                }*/

                currentTime = DateTime.Now;
                syncLogWithAPI();
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
            if (DateTime.Compare(lastActivity, fetchedTime) < 0)
            {
                Console.WriteLine("Change time");
                lastActivity = fetchedTime;
                lastActivityWithThreshold = fetchedTime.Add(new TimeSpan(00, 01, 00));
                isInactiveAfterThreshold = false;
            }
        }

        // Check if set threshold has been past and create http request or write in log of http is not posible
        static void CheckInactivityThreshold()
        {
            if (DateTime.Compare(lastActivityWithThreshold, DateTime.Now) < 0)
            {
                if(isInactiveAfterThreshold != true)
                {
                    writeToLog(createMessage(DateTime.Now, isLocked, "settings", "threshold has been past", false));
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
                updateEventLog();
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

            //dataMessages.ForEach(delegate(DataMessage dataMessage){
            //    Console.WriteLine(dataMessage.message);
            //});

        }

        /// <summary>
        /// Create the log file
        /// </summary>
        /// <param name="fileName">The name of the file that needs to be created</param>
        /// <returns></returns>
        static bool createLogFile(string fileName)
        {
            File.Create(fileName).Close();
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

        /// <summary>
        /// Updates the logfile with the logMessages list
        /// </summary>
        static void updateEventLog()
        {
            string fileName = DateTime.Now.ToShortDateString() + ".json";
            if (!checkIfLogExcists(fileName))
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
                serializer.Serialize(file, logMessages);

            }
        }

        static void syncLogWithAPI()
        {
            List<DataMessage> dataMessagesInLog = readLogFile(DateTime.Now.ToShortDateString() + ".json");
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
                            var response = client.PostAsJsonAsync(url, dataMessage).Result;
                            var responseCode = response.StatusCode;
                            if(response.IsSuccessStatusCode)
                            {
                                dataMessage.APISucces = true;
                            }
                            updatedLog.Add(dataMessage);
                        }
                        else
                        {
                            updatedLog.Add(dataMessage);
                        }
                    });
                    updateLogAfterAPISync(updatedLog);
                    logMessages = updatedLog;
                }

            }
        }

        static void updateLogAfterAPISync(List<DataMessage> updatedLog)
        {
            string fileName = DateTime.Now.ToShortDateString() + ".json";
            using (StreamWriter file = new StreamWriter(fileName, false))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, updatedLog);
            }
        }

        static DataMessage createMessage(DateTime timeStamp, bool isLocked, string location, string message, bool APISucces)
        {
            return new DataMessage
            {
                timeStamp = timeStamp,
                locked = isLocked,
                location = location,
                message = message,
                APISucces = APISucces
            };
        }

    }

    public class DataMessage
    {
        //public int recordID { get; set; }
        public DateTime timeStamp { get; set; }
        public bool locked { get; set; }
        public string location { get; set; }
        public string message { get; set; }
        public bool APISucces { get; set; }
    }

}
