using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LockConsole
{
    public class ConfigurationObject
    {
        public string userID { get; set; }
        public string location { get; set; }
        public string BeginTime { get; set; }
        public string EndTime { get; set; }
        public string InActivityThreshold { get; set; }
        public string dataPostURL { get; set; }
        public List<string> conferencePrograms { get; set; }
        public bool dryRunMode { get; set; }

        /// <summary>
        /// Creates a configuration file if not found
        /// </summary>
        public static void createConfigurationFile()
        {
            File.Create("configuration.json").Close();
            List<string> standardMeetingProcesses = new List<string>() { "Teams", "Zoom", "Skype" };
            ConfigurationObject standardConfiguration = new ConfigurationObject { userID = "testUser", location = "home", BeginTime = "08:00:00", EndTime = "17:00:00", InActivityThreshold = "00:01:00", dataPostURL = "http://127.0.0.1:8000/lockObject", conferencePrograms = standardMeetingProcesses, dryRunMode = true };
            using (StreamWriter file = new StreamWriter("configuration.json", false))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, standardConfiguration);

            }
        }

        /// <summary>
        /// Reads configuration file and return it
        /// </summary>
        /// <returns>Settingsobject with configuration from file</returns>
        public static ConfigurationObject getConfiguration()
        {
            ConfigurationObject configuration;
            if (!FileManager.checkIfFileExcists("configuration.json"))
            {
                createConfigurationFile();
            }

            using (StreamReader reader = new StreamReader("configuration.json"))
            {
                string json = reader.ReadToEnd();
                configuration = JsonConvert.DeserializeObject<ConfigurationObject>(json);
            }

            return configuration;
        }

    }
}
