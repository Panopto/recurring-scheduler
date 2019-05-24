using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Utilities;
using Utilities.RemoteRecorderManagement42;

namespace Scheduler
{
    public static class ConsoleUI
    {
        /// <summary>
        /// Prints the usage of this program, then exits.
        /// </summary>
        public static void Usage()
        {
            System.Console.WriteLine("Usage: Scheduler <userName> <password> <filepath> [startDate] [endDate] [term] [--check]");
            System.Environment.ExitCode = 1;
            System.Environment.Exit(System.Environment.ExitCode);
        }

        /// <summary>
        /// Prints an error message regarding this program's invalid configuration, then exits.
        /// </summary>
        public static void NoSiteName()
        {
            Console.WriteLine("siteName was not configured, please set it and retry.");
            System.Environment.ExitCode = 1;
            System.Environment.Exit(System.Environment.ExitCode);
        }

        /// <summary>
        /// Prints an error message for bad date arguments, then exits.
        /// </summary>
        public static void BadDates()
        {
            Console.WriteLine("Dates given were unable to be parsed, please retry.");
            System.Environment.ExitCode = 1;
            System.Environment.Exit(System.Environment.ExitCode);
        }

        /// <summary>
        /// Prints the start message.
        /// </summary>
        public static void PrintStartMessage()
        {
            Console.WriteLine("---------------------------------------------------------");
            Console.WriteLine("Trying to parse the file...");
            Console.WriteLine("---------------------------------------------------------");
        }

        /// <summary>
        /// Prints the given <paramref name="schedule"/>.
        /// </summary>
        /// <param name="schedule">The schedule to print.</param>
        public static void PrintSchedule(List<ScheduleRecording> schedule)
        {
            for (int i = 0; i < schedule.Count; i++)
            {
                // Print the information of each schedule recording successfully parsed with no conflicts.
                ConsoleUI.PrintScheduleRecordingInfo(schedule[i], Console.Out);
                Console.WriteLine("---------------------------------------------------------");
            }
        }

        /// <summary>
        /// Prints the information of <paramref name="sr"/>.
        /// </summary>
        /// <param name="sr">The ScheduleRecording object of which to print the information of.</param>
        private static void PrintScheduleRecordingInfo(ScheduleRecording sr, TextWriter writer)
        {
            writer.WriteLine("SessionName:               " + sr.SessionName);
            writer.WriteLine("Recorder ID:               " + sr.RecorderID);
            writer.WriteLine("Recorder Name:             " + sr.RecorderName);
            writer.WriteLine("Folder ID:                 " + sr.FolderID);
            writer.WriteLine("Folder Name:               " + sr.FolderName);
            writer.WriteLine("Start Date:                " + sr.StartDate.ToLocalTime());
            writer.WriteLine("Duration:                  " + sr.Duration);
            writer.WriteLine("Presenter:                 " + sr.Presenter);
            writer.WriteLine("Broadcast:                 " + sr.IsBroadcast);
        }
        /// <summary>
        /// Prints the <paramref name="lineNumber"/> of the bad recording that was parser with <paramref name="error"/>
        /// </summary>
        /// <param name="lineNumber">The line number where this ScheduleRecording object was originally parsed.</param>
        /// <param name="errors">A flag for which additional error information can be printed if needed.</param>
        private static void PrintBadScheduleRecordingInfo(int lineNumber, RecordingValidityCode error, TextWriter writer)
        {
            writer.WriteLine("CSV Line Number:           " + lineNumber);
            writer.WriteLine("Error: Recording has conflicts");
            // This will get the name of the Enum value, for example, Valid or BadFolderID
            writer.WriteLine("Validity Status:           " + Enum.GetName(typeof(RecordingValidityCode), error));
            switch (error)
            {
                case RecordingValidityCode.BadCadence:
                    writer.WriteLine("Cadence provided was invalid.");
                    break;
                case RecordingValidityCode.BadDuration:
                    writer.WriteLine("Duration provided was invalid.");
                    break;
                case RecordingValidityCode.BadEndDate:
                    writer.WriteLine("End date provided was invalid.");
                    break;
                case RecordingValidityCode.BadFolderID:
                    writer.WriteLine("Folder name provided or valid accessible folder could not be found.");
                    break;
                case RecordingValidityCode.BadPresenter:
                    writer.WriteLine("Presenter provided was invalid.");
                    break;
                case RecordingValidityCode.BadRecorderID:
                    writer.WriteLine("Recorder name provided or accessible recorder could not be found.");
                    break;
                case RecordingValidityCode.BadSessionID:
                    writer.WriteLine("Session ID provided was invalid.");
                    break;
                case RecordingValidityCode.BadSessionName:
                    writer.WriteLine("Session name provided was invalid");
                    break;
                case RecordingValidityCode.BadStartDate:
                    writer.WriteLine("Start date provided was invalid.");
                    break;
                case RecordingValidityCode.ParseError:
                    writer.WriteLine("Line could not be parsed.");
                    break;
                case RecordingValidityCode.TimeConflict:
                    writer.WriteLine("This line has a time conflict with another recording.");
                    break;
                case RecordingValidityCode.Valid:
                default:
                    break;
            }
            if (lineNumber == -1)
            {
                // if line number is -1, then give message of filetype not supported.
                writer.WriteLine("Filetype not supported.");
            }
        }

        /// <summary>
        /// Prints a summary of ScheduleRecording <paramref name="conflicts"/> encountered when trying to schedule recordings.
        /// </summary>
        /// <param name="conflicts">The ScheduleRecording objects that had a conflict within itself or another ScheduleRecording object.</param>
        /// <param name="badSchedules">The mappings from line number to error code.</param>
        public static void PrintInternalScheduleConflictInfo(List<ScheduleRecording> conflicts, Dictionary<int, RecordingValidityCode> badSchedules, TextWriter writer)
        {
            writer.WriteLine(string.Format("{0} conflict{1} found.", badSchedules.Count, badSchedules.Count != 1 ? "s" : ""));
            writer.WriteLine("---------------------------------------------------------");
            foreach (int i in badSchedules.Keys)
            {
                ConsoleUI.PrintBadScheduleRecordingInfo(i, badSchedules[i], writer);
                writer.WriteLine("---------------------------------------------------------");
            }
            if (!badSchedules.Any())
            { 
                // Parsing was successful, print appropriate message.
                writer.WriteLine("Parse successful.");
                writer.WriteLine("---------------------------------------------------------");
            }
        }

        /// <summary>
        /// Prints the information of <paramref name="conflict"/>.
        /// </summary>
        /// <param name="conflict"></param>
        private static void PrintExistingScheduleConflictInfo(ScheduledRecordingInfo conflict, TextWriter writer)
        {
            writer.WriteLine("    SessionName:               " + conflict.SessionName);
            writer.WriteLine("    Session ID:                " + conflict.SessionID);
            writer.WriteLine("    Start Time:                " + conflict.StartTime.ToLocalTime());
            writer.WriteLine("    End Time:                  " + conflict.EndTime.ToLocalTime());
            writer.WriteLine("-------------------------------------------------------------");
        }

        /// <summary>
        /// Prints a summary of which ScheduleRecording objects could not be successfully scheduled.
        /// </summary>
        /// <param name="scheduleSuccesses">The recordings that were successfully scheduled.</param>
        /// <param name="scheduleConflicts">The recordings that were unsuccessfully scheduled.</param>
        public static void PrintScheduleRecordingResults(List<Guid> scheduleSuccesses,
                                                         Dictionary<ScheduleRecording, ScheduledRecordingInfo[]> scheduleConflicts,
                                                         TextWriter writer)
        {
            try
            {
                writer.WriteLine("Successfully scheduled " + scheduleSuccesses.Count + "/" + (scheduleSuccesses.Count + scheduleConflicts.Count) + " sessions.");
                writer.WriteLine("---------------------------------------------------------");
                if (scheduleConflicts.Count != 0)
                {
                    writer.WriteLine("There were already scheduled recordings at the times requested.");
                    writer.WriteLine("---------------------------------------------------------");
                    writer.WriteLine("Currently scheduled recordings that conflict with requested recordings:");
                    writer.WriteLine("---------------------------------------------------------");
                    foreach (ScheduleRecording sr in scheduleConflicts.Keys)
                    {
                        try
                        {
                            writer.WriteLine("The recording \"{0}\" from {1} to {2} conflicts with the following existing recordings:",
                                             sr.SessionName,
                                             sr.StartDate.ToLocalTime(),
                                             (sr.StartDate + sr.Duration).ToLocalTime());
                            for (int j = 0; j < scheduleConflicts[sr].Length; j++)
                            {
                                ConsoleUI.PrintExistingScheduleConflictInfo(scheduleConflicts[sr].ElementAt(j), writer);
                            }
                        }
                        catch (Exception e)
                        {
                            writer.WriteLine(e);
                        }
                    }
                }
                else
                {
                    writer.WriteLine("All sessions scheduled successfully!");
                }
            }catch(Exception e)
            {
                writer.WriteLine(e);
            }
        }
    }
}
