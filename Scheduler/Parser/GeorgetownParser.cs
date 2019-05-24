using CsvHelper;
using System;
using System.Collections.Generic;
using Utilities;
using Utilities.RemoteRecorderManagement42;
using Utilities.SessionManagement46;

namespace Parsers
{
    class GeorgetownParser : Parser
    {
        private static DateTime SetDate(DateTime desiredDate, DateTime dateToSet)
        {
            return DateTime.SpecifyKind(
                new DateTime(
                    year: desiredDate.Year,
                    month: desiredDate.Month,
                    day: desiredDate.Day,
                    hour: dateToSet.Hour,
                    minute: dateToSet.Minute,
                    second: dateToSet.Second),
                DateTimeKind.Utc);
        }

        public static void Parse(CsvReader csvReader,
                                 IRemoteRecorderManagement rrMgr,
                                 Utilities.RemoteRecorderManagement42.AuthenticationInfo rrAuth,
                                 ISessionManagement sessionMgr,
                                 Utilities.SessionManagement46.AuthenticationInfo sessionAuth,
                                 DateTime startDate,
                                 DateTime endDate,
                                 int lineNumber,
                                 string term,
                                 List<ScheduleRecording> schedule,
                                 Dictionary<int, RecordingValidityCode> badSchedules,
                                 Dictionary<ScheduleRecording, int> recordingToLine)
        {
            while (csvReader.Read())
            {
                // maybe problem due to inconsistent header lengths (with/without exclusion date header) or can just ignore and continue
                // and the Sunday sheet example where there is nothing, header check might catch and ignore this case though
                if (csvReader.Context.Record.Length != Formats[SupportedFileType.Georgetown].Length)
                {
                    // if the number of fields is not good, then continue to the next record
                    badSchedules.Add(lineNumber, RecordingValidityCode.ParseError);
                }
                else if (csvReader.Context.Record[1] == "")
                {
                    // skip it: this line was probably scheduled manually
                }
                else
                {
                    RecurringRecording record = csvReader.GetRecord<RecurringRecording>();
                    record.FolderName += "." + term;
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
                if(lineNumber % 10 == 0)
                {
                    Console.WriteLine(lineNumber);
                }
            }
        }
    }
}
