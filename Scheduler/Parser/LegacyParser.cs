using CsvHelper;
using System.Collections.Generic;
using Utilities;
using Utilities.RemoteRecorderManagement42;
using Utilities.SessionManagement46;

namespace Parsers
{
    class LegacyParser : Parser
    {
        public static void Parse(CsvReader csvReader,
                                 IRemoteRecorderManagement rrMgr,
                                 Utilities.RemoteRecorderManagement42.AuthenticationInfo rrAuth,
                                 ISessionManagement sessionMgr,
                                 Utilities.SessionManagement46.AuthenticationInfo sessionAuth,
                                 int lineNumber,
                                 List<ScheduleRecording> schedule,
                                 Dictionary<int, RecordingValidityCode> badSchedules,
                                 Dictionary<ScheduleRecording, int> recordingToLine)
        {
            while (csvReader.Read())
            {
                if (csvReader.Context.Record.Length != Formats[SupportedFileType.Legacy].Length)
                {
                    // if the number of fields is not good, then continue to the next record
                    badSchedules.Add(lineNumber, RecordingValidityCode.ParseError);
                }
                else
                {
                    ScheduleRecording record = csvReader.GetRecord<ScheduleRecording>();
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
