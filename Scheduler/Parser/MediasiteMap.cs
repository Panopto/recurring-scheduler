using System;
using CsvHelper.Configuration;
using Utilities;

namespace Parsers
{
    sealed class MediasiteMap : ClassMap<ScheduleRecording>
    {
        private static DateTime GetStartTime(CsvHelper.IReaderRow row)
        {
            string date = row.GetField(3);
            string time = row.GetField(4);
            if (string.IsNullOrEmpty(date) || string.IsNullOrEmpty(time))
            {
                return DateTime.MinValue;
            }
            return Parser.ParseDateTime(date, time);
        }

        public MediasiteMap()
        {
            Map(m => m.StartDate).ConvertUsing(row =>
            {
                return MediasiteMap.GetStartTime(row);
            });
            Map(m => m.Duration).ConvertUsing(row =>
            {
                string date = row.GetField(3);
                string startTime = row.GetField(4);
                string endTime = row.GetField(5);
                DateTime end = Parser.ParseDateTime(date, endTime);
                DateTime start = Parser.ParseDateTime(date, startTime);
                if (string.IsNullOrEmpty(date)
                    || string.IsNullOrEmpty(startTime)
                    || string.IsNullOrEmpty(endTime)
                    || end == DateTime.MinValue
                    || start == DateTime.MinValue)
                {
                    return TimeSpan.Zero;
                }
                return end - start;
            });
            Map(m => m.RecorderName).Index(6);
            Map(m => m.SessionName).ConvertUsing(row =>
            {
                string alternateTitle = row.GetField(8).Trim();
                return string.Format(
                    "{0}{1} at {2}",
                    row.GetField(7),
                    !string.IsNullOrWhiteSpace(alternateTitle)
                        ? string.Format(" ({0})", alternateTitle)
                        : string.Empty,
                    MediasiteMap.GetStartTime(row).ToLocalTime());
            });
            // These properties can be set properly later so set a temporary value at parse time
            Map(m => m.IsBroadcast).Constant(false);
            Map(m => m.RecorderID).Constant(Guid.Empty);
            Map(m => m.FolderID).Constant(Guid.Empty);
        }
    }
}
