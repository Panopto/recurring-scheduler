using CsvHelper;
using System;
using System.Collections.Generic;
using Utilities;
using Utilities.RemoteRecorderManagement42;
using Utilities.SessionManagement46;

namespace Parsers
{
    class BannerParser : Parser
    {
        private static readonly string BannerTimeFormat = "HHmm";

        public static void Parse(CsvReader csvReader,
                                 IRemoteRecorderManagement rrMgr,
                                 Utilities.RemoteRecorderManagement42.AuthenticationInfo rrAuth,
                                 ISessionManagement sessionMgr,
                                 Utilities.SessionManagement46.AuthenticationInfo sessionAuth,
                                 int lineNumber,
                                 DateTime startDate,
                                 DateTime endDate,
                                 string term,
                                 List<ScheduleRecording> schedule,
                                 Dictionary<int, RecordingValidityCode> badSchedules,
                                 Dictionary<ScheduleRecording, int> recordingToLine)
        {
            while (csvReader.Read())
            {
                if (csvReader.Context.Record.Length != Formats[SupportedFileType.Banner].Length)
                {
                    // if the number of fields is not good, then continue to the next record
                    badSchedules.Add(lineNumber, RecordingValidityCode.ParseError);
                }
                else
                {
                    RecurringRecording record = csvReader.GetRecord<RecurringRecording>();
                    // Put in start date and end date record index 6 and 7 for times
                    // Don't adjust the start date onto the cadence here, adjust at schedule time.
                    record.StartDate = ParseTime(startDate, csvReader.Context.Record[6], BannerTimeFormat);
                    record.EndDate = ParseTime(endDate, csvReader.Context.Record[7], BannerTimeFormat);
                    // Use local time to calculate recording duration due to UTC conversion causing negative durations
                    // with the use of TimeOfDay. Alternate solutions include subtracting days from end and start so that they are
                    // on the same day but that introduces lots of overhead and more calculations.
                    record.Duration = record.EndDate.ToLocalTime().TimeOfDay - record.StartDate.ToLocalTime().TimeOfDay;
                    record.FolderName += term;
                    ParseCommon(rrMgr,
                                rrAuth,
                                sessionMgr,
                                sessionAuth,
                                record,
                                lineNumber,
                                schedule,
                                badSchedules,
                                recordingToLine);
                }
                lineNumber++;
            }
        }
    }
}
