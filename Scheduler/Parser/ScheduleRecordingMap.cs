using System;
using CsvHelper.Configuration;
using Utilities;

namespace Parsers
{
    /// <summary>
    /// A utility class to use with CsvHelper to map a CSV file to a ScheduleRecording object. Before using CsvHelper to parse and read,
    /// this Map class needs to be registered to the CsvReader so that it can insert the CSV values into an instance of T where T is
    /// specified by ClassMap<T> (in this case, a ScheduleRecording).
    /// 
    /// Reference to CsvHelper's documentation on Mapping can be found here: https://joshclose.github.io/CsvHelper/mapping.
    /// 
    /// Since the currently supported CSV format does not map one-to-one to a ScheduleRecording object, a Map class like this is required to bypass
    /// CsvHelper's Auto Mapping feature. The "m" here refers to an instance of T which a CSV record line is being parsed into by this Map class
    /// and by CsvHelper's Read and GetRecord methods.
    /// </summary>
    sealed class ScheduleRecordingMap : ClassMap<ScheduleRecording>
    {
        public ScheduleRecordingMap()
        {
            // Try and grab the first CSV field and assign that to m.SessionName. Otherwise, use the start time of
            // this recording if possible, else use the current local time.
            Map(m => m.SessionName).ConvertUsing(row =>
            {
                // If session name is not provided, default to the time of the recording, else the current local time.
                string sessionName = row.GetField(0);
                if (string.IsNullOrEmpty(sessionName))
                {
                    // Get the start time of this recording
                    string date = row.GetField(2);
                    string time = row.GetField(3);
                    if (!string.IsNullOrEmpty(date) && !string.IsNullOrEmpty(time))
                    {
                        // Use the start time of this recording
                        sessionName = Parser.ParseDateTime(date, time).ToLocalTime().ToString();
                        // Add presenter if possible
                        string presenter = row.GetField(5);
                        if (!string.IsNullOrEmpty(presenter))
                        {
                            sessionName += " By " + presenter;
                        }
                    }
                    else
                    {
                        // Use current local time
                        sessionName = DateTime.Now.ToString();
                        // Add presenter if possible
                        string presenter = row.GetField(5);
                        if (!string.IsNullOrEmpty(presenter))
                        {
                            sessionName += " By " + presenter;
                        }
                    }
                }
                return sessionName;
            });
            // Get the second CSV field and assign that as m.RecorderName
            Map(m => m.RecorderName).Index(1);
            // Get the third (Date of recording) and fourth (start time of the recording) CSV fields and
            // try to parse those as a combined DateTime for m.StartTime
            Map(m => m.StartDate).ConvertUsing( row =>
            {
                string date = row.GetField(2);
                string time = row.GetField(3);
                if (string.IsNullOrEmpty(date) || string.IsNullOrEmpty(time))
                {
                    return DateTime.MinValue;
                }
                return Parser.ParseDateTime(date, time);
            });
            // Get the third (Date of recording), fourth (start time of the recording) and fifth (end time of the recording)
            // CSV fields and try to parse those as DateTime's then subtract them to get a TimeSpan for m.Duration
            Map(m => m.Duration).ConvertUsing(row =>
            {
                string date = row.GetField(2);
                string startTime = row.GetField(3);
                string endTime = row.GetField(4);
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
            // Get the sixth CSV field and assign that as m.Presenter
            Map(m => m.Presenter).Index(5);
            // Get the seventh CSV field and assign that as m.FolderName
            Map(m => m.FolderName).Index(6);
            // These properties can be set properly later so set a temporary value at parse time
            // Since these need to be set later, it is ok to not handle empty RecorderName and FolderName here
            Map(m => m.IsBroadcast).Constant(false);
            Map(m => m.RecorderID).Constant(Guid.Empty);
            Map(m => m.FolderID).Constant(Guid.Empty);
        }
    }
}
