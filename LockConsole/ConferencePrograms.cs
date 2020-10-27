using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace LockConsole
{
    public class ConferencePrograms
    {
        /// <summary>
        /// Gets all active conference programs
        /// </summary>
        /// ///<param name="conferencePrograms">The list of conference programs from the configuration</param>
        /// <returns>A string list with all active confernce programs names</returns>
        public static List<string> getActiveConferencePrograms(List<string> conferencePrograms)
        {
            Process[] activeProcesses = Process.GetProcesses();
            List<string> activeConferenceProcesses = new List<string>();

            foreach (Process process in activeProcesses)
            {
                if (conferencePrograms.Contains(process.ProcessName))
                {
                    activeConferenceProcesses.Add(process.ProcessName);
                }
            }

            return activeConferenceProcesses;
        }

        /// <summary>
        /// Check if there are active conference programs like teams, skype and zoom
        /// </summary>
        /// <param name="conferencePrograms">The list of conference programs from the configuration</param>
        /// <returns>a bool with true if there are active programs and false if there are no active programs</returns>
        public static bool checkForActiveConferenceProgram(List<string> conferencePrograms)
        {
            if (getActiveConferencePrograms(conferencePrograms).Count > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
