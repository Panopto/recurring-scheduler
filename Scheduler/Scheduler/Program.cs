using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Utilities;
using Utilities.RemoteRecorderManagement42;
using Utilities.SessionManagement46;
using Parsers;

namespace Scheduler
{
    /// <summary>
    /// This program will attempt to schedule recordings provided in a CSV file.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 3 || args.Length > 7 || args.Length == 5)
            {
                ConsoleUI.Usage();
            }
            string siteName = Properties.Settings.Default.sitename;
            if (siteName == "")
            {
                ConsoleUI.NoSiteName();
            }
            string userName = args[0];
            string password = args[1];
            string filepath = args[2];
            bool dateParseSuccess;
            DateTime startDate = DateTime.MinValue;
            DateTime endDate = DateTime.MinValue;
            string term = null;
            bool check = args[args.Length - 1].Equals("--check", StringComparison.InvariantCultureIgnoreCase);
            if (args.Length > (3 + (check ? 1 : 0)))
            {
                dateParseSuccess = DateTime.TryParse(args[3], out startDate) && DateTime.TryParse(args[4], out endDate);
                if (!dateParseSuccess)
                {
                    ConsoleUI.BadDates();
                }
                term = args[5];
            }
            if(check)
            {
                Console.WriteLine("---- Check is specified, no scheduling will be performed but schedule consistency will be validated.");
            }
            ISessionManagement sessionMgr;
            Utilities.SessionManagement46.AuthenticationInfo sessionAuth;
            IRemoteRecorderManagement rrMgr;
            Utilities.RemoteRecorderManagement42.AuthenticationInfo rrAuth;
            Scheduler.SetupSiteAccess(userName, password, out rrMgr, out rrAuth, out sessionMgr, out sessionAuth);

            ConsoleUI.PrintStartMessage();
            
            // Prepare the variables to store the information from ParseFile
            Dictionary<int, RecordingValidityCode> badSchedules;
            List<ScheduleRecording> conflicts;

            Guid? folderId = null;
            Guid mediasiteFolderId;
            if (Guid.TryParse(Properties.Settings.Default.MediasiteFolderId, out mediasiteFolderId))
            {
                folderId = mediasiteFolderId;
            }

            // File path is passed in as an argument, parse the file
            var schedule = Parser.ParseFile(filepath: filepath,
                                            rrMgr: rrMgr,
                                            rrAuth: rrAuth,
                                            sessionMgr: sessionMgr,
                                            sessionAuth: sessionAuth,
                                            startDate: startDate,
                                            endDate: endDate,
                                            term: term,
                                            folderId: folderId,
                                            badSchedules: out badSchedules,
                                            conflicts: out conflicts);
            ConsoleUI.PrintSchedule(schedule);
            // Print the internal scheduling conflicts in the provided file.
            ConsoleUI.PrintInternalScheduleConflictInfo(conflicts, badSchedules, Console.Out);
            if (badSchedules.Any())
            {
                Console.WriteLine("{0} bad schedules", badSchedules.Count());
            }

            Console.WriteLine(schedule.Count + " recordings ready to schedule.");
            if(Properties.Settings.Default.ScheduleBroadcasts)
            {
                Console.WriteLine("ScheduleBroadcasts is set in config so all sessions will be scheduled as broadcasts.");
            }

            // don't proceed if -check flag was passed
            if (check)
            {
                Console.WriteLine("Check complete, exiting.");
            }
            else
            {
                // Try to schedule the recordings
                Console.WriteLine("---------------------------------------------------------");
                Console.WriteLine("Proceed? Ctrl-C now to exit.");
                Console.ReadKey();
                Dictionary<ScheduleRecording, ScheduledRecordingResult> result = Scheduler.ScheduleRecordings(rrMgr, rrAuth, schedule);

                // Categorize and print the results
                List<Guid> scheduleSuccesses;
                Dictionary<ScheduleRecording, ScheduledRecordingInfo[]> scheduleConflicts;
                Checker.GetSuccessesAndConflicts(result, out scheduleSuccesses, out scheduleConflicts);
                ConsoleUI.PrintScheduleRecordingResults(scheduleSuccesses, scheduleConflicts, Console.Out);
                using (TextWriter writer = new StreamWriter(
                    Properties.Settings.Default.LogsLocation + DateTime.Now.ToString("yyyyMMdd_HHmmss") + @"_full_log.csv"))
                {
                    ConsoleUI.PrintScheduleRecordingResults(scheduleSuccesses, scheduleConflicts, writer);
                }

                // Write the output file
                Console.WriteLine("Writing output file...");
                Scheduler.WriteSuccessFile(sessionMgr, sessionAuth, scheduleSuccesses);
            }
            System.Environment.ExitCode = 0;
            System.Environment.Exit(System.Environment.ExitCode);
        }
    }
}
