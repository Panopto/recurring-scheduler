using System;
using CsvHelper.Configuration;
using Utilities;
using System.Text.RegularExpressions;

namespace Parsers
{
    sealed class GeorgetownMap : ClassMap<RecurringRecording>
    {
        private static readonly string Option2Header = "Option 2";
        private static readonly string Option2Modifier = "-LA";

        public GeorgetownMap()
        {
            Map(m => m.StartDate).ConvertUsing(row =>
            {
                string startDate = row.GetField(0).Trim();
                string startTime = row.GetField(7).Trim().PadLeft(4, '0');
                return Parser.ParseDateTime(startDate, startTime, "HHmm");
            });
            Map(m => m.EndDate).ConvertUsing(row =>
            {
                string endDate = row.GetField(1).Trim();
                string endTime = row.GetField(8).Trim().PadLeft(4, '0');
                return Parser.ParseDateTime(endDate, endTime, "HHmm");
            });
            Map(m => m.Duration).ConvertUsing(row =>
            {
                string startDate = row.GetField(0).Trim();
                string startTime = row.GetField(7).Trim().PadLeft(4, '0');
                string endDate = row.GetField(1).Trim();
                string endTime = row.GetField(8).Trim().PadLeft(4, '0');
                return Parser.ParseDateTime(
                    endDate, endTime, "HHmm").ToLocalTime().TimeOfDay -
                    Parser.ParseDateTime(startDate, startTime, "HHmm").ToLocalTime().TimeOfDay;
            });
            Map(m => m.RecorderName).ConvertUsing(row =>
            {
                string building = row.GetField(2).Trim();
                string roomNum = row.GetField(3).Trim();
                return string.Format("{0} {1}", building, roomNum);
            });
            Map(m => m.SessionName).Index(5);
            Map(m => m.Presenter).Index(6);
            Map(m => m.Cadence).ConvertUsing(row =>
            {
                string cadence = row.GetField(9).ToUpper().Trim();
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
                        default:
                            { /*There was a letter not recognized as a day of the week, set a recoginizable value
                                so the error code for BadCadence can be caught with CheckValidity.*/
                                return Days.None;
                            }
                    }
                }
                return result;
            });
            Map(m => m.FolderName).ConvertUsing(row =>
            {
                string courseID = row.GetField(10).Trim();
                string section = row.GetField(11).Trim();
                if (section.Length <= 2)
                {
                    // width of 2 characters
                    section = section.PadLeft(2, '0');
                }
                else if (section.Length >= 4)
                {
                    // width of 5 characters
                    section = section.PadLeft(5, '0');
                }
                string foldernameModifier = "";
                if(row.GetField(12).Equals(GeorgetownMap.Option2Header, StringComparison.InvariantCultureIgnoreCase))
                {
                    foldernameModifier = GeorgetownMap.Option2Modifier;
                }
                // This pattern should split Course IDs into Department and Course number
                // This pattern splits on the basis that the Department is all alphabetic characters
                // while the course number is all numeric characters.
                string pattern = @"(?<Department>[a-zA-Z]+?)(?<CourseNumber>[0-9]+)";
                Regex regex = new Regex(pattern);
                var match = regex.Match(courseID);
                string department = match.Groups["Department"].Value;
                string courseNumber = match.Groups["CourseNumber"].Value;
                // need term which can be inputted from program arguments.
                return string.Format("{0}-{1}-{2}{3}", department, courseNumber, section, foldernameModifier);
            });
            // These properties can be set properly later so set a temporary value at parse time
            Map(m => m.IsBroadcast).Constant(false);
            Map(m => m.RecorderID).Constant(Guid.Empty);
            Map(m => m.FolderID).Constant(Guid.Empty);
        }
    }
}
