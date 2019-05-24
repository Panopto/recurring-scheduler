using System;
using System.IO;
using System.Collections.Generic;
using Utilities.SessionManagement46;
using Utilities.RemoteRecorderManagement42;
using CsvHelper;
using System.Linq;
using System.Globalization;
using Utilities;

namespace Parsers
{
    /// <summary>
    /// This is a utility class that can help parse a recording schedule, a date and time, and grab unique identifiers for a folder and/or recorder.
    /// </summary>
    public class Parser
    {
        protected static readonly Dictionary<SupportedFileType, string[]> Formats = new Dictionary<SupportedFileType, string[]>()
            { { SupportedFileType.NotSupported, new string[] { } },
              { SupportedFileType.Legacy, new string[] { "sessionName", "recorderName", "recordingDate", "startTime", "endTime", "presenterName", "folderName" } },
              { SupportedFileType.Banner, new string[] { "Seats", "Enr", "Building", "Room", "Title", "Instructor", "Begin Time", "End Time", "Meeting Days", "Meeting Type", "Course ID", "Section" } },
              { SupportedFileType.Georgetown, new string[] { "Date Start", "Date End", "Building", "Room", "Remote Recorder", "Title", "Instructor", "Begin Time", "End Time", "Meeting Days", "Course ID", "Section", "Recording Option" } },
              { SupportedFileType.Mediasite, new string[] { "Day", "Class of", "Cohort", "Date", "Start", "End", "Room", "Alias", "Alternate Session Title", "Notes" } }
            };

        /// <summary>
        /// Returns true if the <paramref name="header"/> is of the expected <paramref name="format"/>.
        /// </summary>
        /// <param name="format">The expected format of the <paramref name="header"/>.</param>
        /// <param name="header">The header to check against the expected <paramref name="header"/>.</param>
        /// <returns>Returns true if the <paramref name="header"/> is of the expected <paramref name="format"/>, false otherwise.</returns>
        public static bool IsHeader(string[] format, string[] header)
        {
            // SequenceEqual compares the ordering, or sequence, as well as the elements of any IEnumerable.
            return header.SequenceEqual(format, StringComparer.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Parses the given <paramref name="filepath"/> and returns the recording schedule.
        /// </summary>
        /// <param name="filepath">The path at which the file is located.</param>
        /// <param name="rrMgr">The client in which holds the information about the recorders available.</param>
        /// <param name="rrAuth">The authentication information for which to access the information from <paramref name="rrMgr"/>.</param>
        /// <param name="sessionMgr">The client in which holds the information about the sessions.</param>
        /// <param name="sessionAuth">The authentication information for which to access the information from <paramref name="sessionMgr"/>.</param>
        /// <param name="badSchedules">The mappings from line number to error code to be populated, if needed.</param>
        /// <param name="conflicts">All the recordings that conflicted with an earlier recording or are invalid.</param>
        /// <returns>The recording schedule, as well as the output parameters <paramref name="badSchedules"/> and <paramref name="conflicts"/>.</returns>
        public static List<ScheduleRecording> ParseFile(string filepath,
                                                        IRemoteRecorderManagement rrMgr,
                                                        Utilities.RemoteRecorderManagement42.AuthenticationInfo rrAuth,
                                                        ISessionManagement sessionMgr,
                                                        Utilities.SessionManagement46.AuthenticationInfo sessionAuth,
                                                        out Dictionary<int, RecordingValidityCode> badSchedules,
                                                        out List<ScheduleRecording> conflicts)
        {
            return ParseFile(filepath: filepath,
                             rrMgr: rrMgr,
                             rrAuth: rrAuth,
                             sessionMgr: sessionMgr,
                             sessionAuth: sessionAuth,
                             startDate: DateTime.MinValue,
                             endDate: DateTime.MinValue,
                             term: null,
                             folderId: null,
                             badSchedules: out badSchedules,
                             conflicts: out conflicts);
        }

        /// <summary>
        /// Parses the given <paramref name="filepath"/> and returns the recording schedule.
        /// </summary>
        /// <param name="filepath">The path at which the file is located.</param>
        /// <param name="rrMgr">The client in which holds the information about the recorders available.</param>
        /// <param name="rrAuth">The authentication information for which to access the information from <paramref name="rrMgr"/>.</param>
        /// <param name="sessionMgr">The client in which holds the information about the sessions.</param>
        /// <param name="sessionAuth">The authentication information for which to access the information from <paramref name="sessionMgr"/>.</param>
        /// <param name="startDate">The start date of the term.</param>
        /// <param name="endDate">The end date of the term.</param>
        /// <param name="term">The string representation of the term.</param>
        /// <param name="badSchedules">The mappings from line number to error code to be populated, if needed.</param>
        /// <param name="conflicts">All the recordings that conflicted with an earlier recording or are invalid.</param>
        /// <returns>The recording schedule, as well as the output parameters <paramref name="badSchedules"/> and <paramref name="conflicts"/>.</returns>
        public static List<ScheduleRecording> ParseFile(string filepath,
                                                        IRemoteRecorderManagement rrMgr,
                                                        Utilities.RemoteRecorderManagement42.AuthenticationInfo rrAuth,
                                                        ISessionManagement sessionMgr,
                                                        Utilities.SessionManagement46.AuthenticationInfo sessionAuth,
                                                        string term,
                                                        out Dictionary<int, RecordingValidityCode> badSchedules,
                                                        out List<ScheduleRecording> conflicts)
        {
            return ParseFile(filepath: filepath,
                             rrMgr: rrMgr,
                             rrAuth: rrAuth,
                             sessionMgr: sessionMgr,
                             sessionAuth: sessionAuth,
                             startDate: DateTime.MinValue,
                             endDate: DateTime.MinValue,
                             term: term,
                             folderId: null,
                             badSchedules: out badSchedules,
                             conflicts: out conflicts);
        }

        /// <summary>
        /// Parses the given <paramref name="filepath"/> and returns the recording schedule.
        /// </summary>
        /// <param name="filepath">The path at which the file is located.</param>
        /// <param name="rrMgr">The client in which holds the information about the recorders available.</param>
        /// <param name="rrAuth">The authentication information for which to access the information from <paramref name="rrMgr"/>.</param>
        /// <param name="sessionMgr">The client in which holds the information about the sessions.</param>
        /// <param name="sessionAuth">The authentication information for which to access the information from <paramref name="sessionMgr"/>.</param>
        /// <param name="startDate">The start date of the term.</param>
        /// <param name="endDate">The end date of the term.</param>
        /// <param name="term">The string representation of the term.</param>
        /// <param name="badSchedules">The mappings from line number to error code to be populated, if needed.</param>
        /// <param name="conflicts">All the recordings that conflicted with an earlier recording or are invalid.</param>
        /// <returns>The recording schedule, as well as the output parameters <paramref name="badSchedules"/> and <paramref name="conflicts"/>.</returns>
        public static List<ScheduleRecording> ParseFile(string filepath,
                                                        IRemoteRecorderManagement rrMgr,
                                                        Utilities.RemoteRecorderManagement42.AuthenticationInfo rrAuth,
                                                        ISessionManagement sessionMgr,
                                                        Utilities.SessionManagement46.AuthenticationInfo sessionAuth,
                                                        DateTime startDate,
                                                        DateTime endDate,
                                                        string term,
                                                        Guid? folderId,
                                                        out Dictionary<int, RecordingValidityCode> badSchedules,
                                                        out List<ScheduleRecording> conflicts)
        {
            List<ScheduleRecording> result = new List<ScheduleRecording>();
            // Handle CSV style case
            using (TextReader reader = File.OpenText(filepath))
            {
                // Create the CsvReader
                CsvReader csvReader = new CsvReader(reader);
                // Ignore missing fields since we don't know the format yet
                // The corresponding classmap class will and should handle those missing fields
                csvReader.Configuration.MissingFieldFound = null;
                // Now read the header to determine its format.
                badSchedules = new Dictionary<int, RecordingValidityCode>();
                conflicts = null;
                Dictionary<ScheduleRecording, int> recordingToLine = new Dictionary<ScheduleRecording, int>();
                List<ScheduleRecording> schedule = new List<ScheduleRecording>();
                int lineNumber = 1;
                SupportedFileType fileType = SupportedFileType.NotSupported;
                bool foundHeader = false;
                while (csvReader.Read())
                {
                    if (!foundHeader)
                    {
                        csvReader.ReadHeader();
                        // see if this line is a header we recognize
                        foreach (SupportedFileType sft in Enum.GetValues(typeof(SupportedFileType)))
                        {
                            string[] format = Formats[sft];
                            foundHeader = IsHeader(format, csvReader.Context.Record);
                            if (foundHeader)
                            {
                                // set the filetype and the appropriate map.
                                fileType = sft;
                                switch (fileType)
                                {
                                    // Call appropriate parser here, with lineNumber + 1
                                    case SupportedFileType.Legacy:
                                        csvReader.Configuration.RegisterClassMap<ScheduleRecordingMap>();
                                        LegacyParser.Parse(csvReader,
                                                           rrMgr,
                                                           rrAuth,
                                                           sessionMgr,
                                                           sessionAuth,
                                                           lineNumber + 1,
                                                           schedule,
                                                           badSchedules,
                                                           recordingToLine);
                                        // Need to check the schedule for internal conflicts
                                        // done parsing so check the schedule
                                        conflicts = Checker.CheckConflicts(schedule, recordingToLine, badSchedules);
                                        break;
                                    case SupportedFileType.Banner:
                                        csvReader.Configuration.RegisterClassMap<RecurringRecordingMap>();
                                        BannerParser.Parse(csvReader,
                                                           rrMgr,
                                                           rrAuth,
                                                           sessionMgr,
                                                           sessionAuth,
                                                           lineNumber + 1,
                                                           startDate,
                                                           endDate,
                                                           term,
                                                           schedule,
                                                           badSchedules,
                                                           recordingToLine);
                                        // We trust that the Banner format is internally consistent so set conflicts to new empty list.
                                        conflicts = new List<ScheduleRecording>();
                                        break;
                                    case SupportedFileType.Georgetown:
                                        csvReader.Configuration.RegisterClassMap<GeorgetownMap>();
                                        GeorgetownParser.Parse(csvReader,
                                                               rrMgr,
                                                               rrAuth,
                                                               sessionMgr,
                                                               sessionAuth,
                                                               startDate,
                                                               endDate,
                                                               lineNumber + 1,
                                                               term,
                                                               schedule,
                                                               badSchedules,
                                                               recordingToLine);
                                        // We trust that Georgetown has scrubbed their file so that there are no conflicts
                                        conflicts = new List<ScheduleRecording>();
                                        break;
                                    case SupportedFileType.Mediasite:
                                        csvReader.Configuration.RegisterClassMap<MediasiteMap>();
                                        MediasiteParser.Parse(csvReader,
                                                           rrMgr,
                                                           rrAuth,
                                                           sessionMgr,
                                                           sessionAuth,
                                                           folderId,
                                                           lineNumber + 1,
                                                           schedule,
                                                           badSchedules,
                                                           recordingToLine);
                                        // Need to check the schedule for internal conflicts
                                        // done parsing so check the schedule
                                        conflicts = Checker.CheckConflicts(schedule, recordingToLine, badSchedules);
                                        break;
                                    default:
                                        break;
                                }
                                break;
                            }
                        }
                    }
                    lineNumber++;
                }
                // This step can still be done for all formats
                for (int i = 0; i < schedule.Count; i++)
                {
                    if (!badSchedules.ContainsKey(recordingToLine[schedule[i]]) && schedule[i].CheckValidity() == RecordingValidityCode.Valid)
                    {
                        result.Add(schedule[i]);
                    }
                }
                // we are done reading but no SupportedFileType detected.
                if (!foundHeader)
                {
                    // give a special line number to badSchedules
                    badSchedules.Add(-1, RecordingValidityCode.ParseError);
                    conflicts = new List<ScheduleRecording>();
                }
            }
            return result;
        }

        /// <summary>
        /// Parses the given <paramref name="date"/> and <paramref name="time"/> into a DateTime object
        /// </summary>
        /// <param name="date">The date for the object</param>
        /// <param name="time">The time for the object</param>
        /// <returns>A DateTime object from the requested <paramref name="date"/> and <paramref name="time"/>.
        /// If the given parameters are not parsable, returns DateTime.Min</returns>
        public static DateTime ParseDateTime(string date, string time)
        {
            // NOTE: Currently machine date format dependent (time ok) if year is not format of (yyyy)
            DateTime result;
            try
            {
                result = DateTime.Parse(date + " " + time).ToUniversalTime();
            }
            catch (FormatException)
            {
                // returning DateTime.Min as an error flag
                result = DateTime.MinValue;
            }
            return result;
        }

        /// <summary>
        /// Parses the given <paramref name="date"/> and <paramref name="time"/> into a DateTime object
        /// </summary>
        /// <param name="date">The date for the object</param>
        /// <param name="time">The time for the object</param>
        /// <param name="timeFormat">The format of which <paramref name="time"/> will be parsed with.</param>
        /// <returns>A DateTime object from the requested <paramref name="date"/> and <paramref name="time"/>.
        /// If the given parameters are not parsable, returns DateTime.Min</returns>
        public static DateTime ParseDateTime(string date, string time, string timeFormat)
        {
            // NOTE: Currently machine date format dependent (time ok) if year is not format of (yyyy)
            DateTime datePart;
            DateTime.TryParse(date, out datePart);
            DateTime result;
            try
            {
                // ParseTime will return as UTC so no need to convert here
                result = ParseTime(datePart, time, timeFormat);
            }
            catch (FormatException)
            {
                // returning DateTime.Min as an error flag
                result = DateTime.MinValue;
            }
            return result;
        }

        /// <summary>
        /// Parses the given <paramref name="time"/> with the given <paramref name="timeFormat"/> and the given <paramref name="date"/>.
        /// </summary>
        /// <param name="date">The date of which to be parsed with.</param>
        /// <param name="time">The time of which to be parsed.</param>
        /// <param name="timeFormat">The format of which <paramref name="time"/> will be parsed with.</param>
        /// <returns>A DateTime object from the requested <paramref name="date"/> and <paramref name="time"/>.
        /// If the given parameters are not parsable, returns DateTime.Min</returns>
        public static DateTime ParseTime(DateTime date, string time, string timeFormat)
        {
            // NOTE: Currently machine date format dependent (time ok) if year is not format of (yyyy)
            DateTime result;
            try
            {
                result = date.Add(DateTime.ParseExact(time, timeFormat, CultureInfo.InvariantCulture).TimeOfDay);
            }
            catch (FormatException)
            {
                // returning DateTime.Min as an error flag
                result = DateTime.MinValue;
            }
            return result.ToUniversalTime();
        }

        /// <summary>
        /// Common method calls an actions for all parsers.
        /// </summary>
        /// <param name="rrMgr">The client in which holds the information about the recorders available.</param>
        /// <param name="rrAuth">The authentication information for which to access the information from <paramref name="rrMgr"/>.</param>
        /// <param name="sessionMgr">The client in which holds the information about the sessions.</param>
        /// <param name="sessionAuth">The authentication information for which to access the information from <paramref name="sessionMgr"/>.</param>
        /// <param name="record">The line currently being parsed.</param>
        /// <param name="lineNumber">The line number on the original file.</param>
        /// <param name="schedule">The schedule to update after <paramref name="record"/> is parsed.</param>
        /// <param name="badSchedules">The mappings from line number to error code to be populated, if needed.</param>
        /// <param name="recordingToLine">The mappings from recording to line number.</param>
        public static void ParseCommon(IRemoteRecorderManagement rrMgr,
                                       Utilities.RemoteRecorderManagement42.AuthenticationInfo rrAuth,
                                       ISessionManagement sessionMgr,
                                       Utilities.SessionManagement46.AuthenticationInfo sessionAuth,
                                       ScheduleRecording record,
                                       int lineNumber,
                                       List<ScheduleRecording> schedule,
                                       Dictionary<int, RecordingValidityCode> badSchedules,
                                       Dictionary<ScheduleRecording, int> recordingToLine)
        {
            // Now grab the IDs if possible and set them in the object
            record.RecorderID = Checker.GetRecorderID(record.RecorderName, rrMgr, rrAuth);
            if (record.FolderName != "" && record.FolderID == Guid.Empty)
            {
                // a specific folder was specified so try to look it up
                record.FolderID = Checker.GetFolderID(record.FolderName, sessionMgr, sessionAuth);
            }
            else if (record.FolderName == "" && record.RecorderID != Guid.Empty)
            {
                // case for a default folder/unspecified folder
                // Each RR has their own default folder, so call that and directly get folderID from that
                record.FolderID = rrMgr.GetDefaultFolderForRecorder(rrAuth, record.RecorderID);
                // Need to update the FolderName if we used the remote recorder's default folder
                Folder[] folder = sessionMgr.GetFoldersById(sessionAuth, new Guid[] { record.FolderID });
                record.FolderName = folder[0].Name;
            }
            if (record.CheckValidity() != RecordingValidityCode.Valid)
            {
                badSchedules.Add(lineNumber, record.CheckValidity());
            }
            recordingToLine.Add(record, lineNumber);
            schedule.Add(record);
        }
    }
}
