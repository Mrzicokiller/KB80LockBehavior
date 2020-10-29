using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace LockConsole
{
    public class FileManager
    {
        /// <summary>
        /// Check if log already excits
        /// </summary>
        /// <param name="fileName">The fileName of the log file that needs to be checked</param>
        public static bool checkIfFileExcists(string fileName)
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
        /// Create the log file
        /// </summary>
        /// <param name="fileName">The name of the file that needs to be created</param>
        /// <returns></returns>
        public static bool createLogFile(string fileName)
        {
            File.Create(fileName).Close();
            return (FileManager.checkIfFileExcists(fileName));
        }

        /// <summary>
        /// Read data from given filename
        /// </summary>
        /// <param name="fileName">The name of the file that needs to be read</param>
        public static List<DataMessage> readLogFile(string fileName)
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
        /// Writes message to file
        /// </summary>
        /// <param name="dataMessage">the dataMessage object</param>
        /// <param name="logMessages">Current logmessage</param>
        /// <param name="currentTime">Current time</param>
        public static void writeToLog(DataMessage dataMessage, List<DataMessage> logMessages, string fileName)
        {
            if (logMessages == null || !logMessages.Contains(dataMessage))
            {
                logMessages.Add(dataMessage);
                updateEventLog(logMessages, fileName);
            }

        }

        /// <summary>
        /// Updates the logfile with the logMessages list
        /// </summary>
        /// <param name="updatedLog">The updated message log list</param>
        /// <param name="currentTime">The current time</param>
        public static void updateEventLog(List<DataMessage> updatedLog, string fileName)
        {
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

    }
}
