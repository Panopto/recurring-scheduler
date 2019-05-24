using CsvHelper;
using System;
using System.Collections.Generic;
using Utilities;
using Utilities.RemoteRecorderManagement42;
using Utilities.SessionManagement46;

namespace Parsers
{
    class MediasiteParser : Parser
    {
        public static void Parse(CsvReader csvReader,
                                 IRemoteRecorderManagement rrMgr,
                                 Utilities.RemoteRecorderManagement42.AuthenticationInfo rrAuth,
                                 ISessionManagement sessionMgr,
                                 Utilities.SessionManagement46.AuthenticationInfo sessionAuth,
                                 Guid? folderId,
                                 int lineNumber,
                                 List<ScheduleRecording> schedule,
                                 Dictionary<int, RecordingValidityCode> badSchedules,
                                 Dictionary<ScheduleRecording, int> recordingToLine)
        {
            while (csvReader.Read())
            {
                if (csvReader.Context.Record.Length != Formats[SupportedFileType.Mediasite].Length)
                {
                    // if the number of fields is not good, then continue to the next record
                    badSchedules.Add(lineNumber, RecordingValidityCode.ParseError);
                }
                else
                {
                    ScheduleRecording record = csvReader.GetRecord<ScheduleRecording>();
                    record.Presenter = string.Empty;
                    if (folderId.HasValue)
                    {
                        record.FolderID = folderId.Value;
                    }
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
