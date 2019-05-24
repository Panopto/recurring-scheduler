using System;
using CsvHelper.Configuration;
using Utilities;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Parsers
{
    sealed class RecurringRecordingMap : ClassMap<RecurringRecording>
    {
        public RecurringRecordingMap()
        {
            Map(m => m.RecorderName).ConvertUsing(row =>
            {
                // RR names are some combination of building and room number
                // temporary map to translate abbreviations to rr name
                Dictionary<string, string> abbreviationMap = new Dictionary<string, string>()
                {
                    { "LAW", "LAW MCD" },
                    { "HTNG", "LAW HOTUNG" }
                };
                string building = row.GetField(2);
                if (building == "LAW" || building == "HTNG")
                {
                    building = abbreviationMap[row.GetField(2)];
                }
                string roomNumFull = row.GetField(3);
                string pattern = @"(?<RoomPrefix>[a-zA-Z]?)(?<RoomNumber>[0-9]+)";
                Regex regex = new Regex(pattern);
                var match = regex.Match(roomNumFull);
                string roomNumber = match.Groups["RoomNumber"].Value;
                return string.Format("{0} {1}", building, roomNumber);
            });
            // Currently need a format for session name, use the course Title for now
            Map(m => m.SessionName).Index(4);
            Map(m => m.Presenter).Index(5);
            Map(m => m.StartDate).Constant(DateTime.MinValue); // Start date will be taken in as args
            Map(m => m.EndDate).Constant(DateTime.MinValue); // End Date will be taken in as args
            Map(m => m.Duration).Constant(TimeSpan.Zero);
            Map(m => m.Cadence).ConvertUsing(row =>
            {
                string cadence = row.GetField(8).ToUpper();
                Days result = Days.None;
                foreach (char ch in cadence)
                {
                    switch (ch)
                    {
                        case 'M': { result = result | Days.Monday; break; }
                        case 'T': { result = result | Days.Tuesday; break; }
                        case 'W': { result = result | Days.Wednesday; break; }
                        case 'R': { result = result | Days.Thursday; break; }
                        case 'F': { result = result | Days.Friday; break; }
                        case 'S': { result = result | Days.Saturday; break; }
                        case 'U': { result = result | Days.Sunday; break; }
                        default: { /*There was a letter not recognized as a day of the week, set a recoginizable value
                                so the error code for BadCadence can be caught with CheckValidity.*/
                                return Days.None; }
                    }
                }
                return result;
            });
            Map(m => m.FolderName).ConvertUsing(row =>
            {
                string courseID = row.GetField(10);
                string section = row.GetField(11);
                // This pattern should split Course IDs into Department and Course number
                // This pattern splits on the basis that the Department is all alphabetic characters
                // while the course number is all numeric characters.
                string pattern = @"(?<Department>[a-zA-Z]+?)(?<CourseNumber>[0-9]+)";
                Regex regex = new Regex(pattern);
                var match = regex.Match(courseID);
                string department = match.Groups["Department"].Value;
                string courseNumber = match.Groups["CourseNumber"].Value;
                // need term which can maybe be parsed from the file itself.
                return string.Format("{0}-{1}-{2}.", department, courseNumber, section);
            });
            // These properties can be set properly later so set a temporary value at parse time
            // Since these need to be set later, it is ok to not handle empty RecorderName and FolderName here
            Map(m => m.IsBroadcast).Constant(false);
            Map(m => m.RecorderID).Constant(Guid.Empty);
            Map(m => m.FolderID).Constant(Guid.Empty);
        }
    }
}
