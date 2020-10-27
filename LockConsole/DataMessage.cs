using System;
using System.Collections.Generic;
using System.Text;

namespace LockConsole
{
    public class DataMessage
    {
        public string userId { get; set; }
        public string timeStamp { get; set; }
        public bool locked { get; set; }
        public string location { get; set; }
        public string message { get; set; }
        public bool activeConferenceProgram { get; set; }
        public bool APISucces { get; set; }
        public bool dryRunMode { get; set; }

        /// <summary>
        /// Creates the message for the log and API
        /// </summary>
        /// <param name="userId">The userID from the configuration file</param>
        /// <param name="timeStamp">Timestamp for the message</param>
        /// <param name="isLocked">Lockstatus</param>
        /// <param name="location">location from configuration</param>
        /// <param name="message">A extra message</param>
        /// <param name="APISucces">API sync status</param>
        /// <param name="activeConferenceProgram">A boolean for active conference programs</param>
        /// <param name="dryRunMode">Boolean for dry run mode from configuratio</param>
        /// <returns>A datamessage object ready for the log and API</returns>
        public static DataMessage createMessage(string userId, DateTime timeStamp, bool isLocked, string location, string message, bool APISucces, bool activeConferenceProgram, bool dryRunMode)
        {
            return new DataMessage
            {
                userId = userId,
                timeStamp = timeStamp.ToString("yyyy-MM-dd HH:mm:ss"),
                locked = isLocked,
                location = location,
                message = message,
                activeConferenceProgram = activeConferenceProgram,
                APISucces = APISucces,
                dryRunMode = dryRunMode
            };
        }
    }
}
