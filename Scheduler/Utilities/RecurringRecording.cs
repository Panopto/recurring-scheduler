using System;

namespace Utilities
{
    /// <summary>
    /// This class represents a recording recurs at a regular frequency until an end date.
    /// </summary>
    public class RecurringRecording : ScheduleRecording
    {
        private static readonly Days Everyday = Days.Monday
                                                | Days.Tuesday
                                                | Days.Wednesday
                                                | Days.Thursday
                                                | Days.Friday
                                                | Days.Saturday
                                                | Days.Sunday;
        public DateTime EndDate { get; set; }
        public Days Cadence { get; set; }
        public RecurringRecording() : base()
        {
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="copy">The RecurringRecording to copy.</param>
        public RecurringRecording(RecurringRecording copy)
        {
            RecorderID = copy.RecorderID;
            RecorderName = copy.RecorderName;
            FolderID = copy.FolderID;
            FolderName = copy.FolderName;
            SessionName = copy.SessionName;
            IsBroadcast = copy.IsBroadcast;
            StartDate = copy.StartDate;
            Duration = copy.Duration;
            Presenter = copy.Presenter;
            EndDate = copy.EndDate;
            Cadence = copy.Cadence;
        }

        /// <summary>
        /// Determines if this RecurringRecording object is valid.
        /// </summary>
        /// <returns>Returns RecordingValidityCode.Valid if this RecurringRecording object is valid.</returns>
        public new RecordingValidityCode CheckValidity()
        {
            if (this.EndDate == DateTime.MinValue || this.EndDate < this.StartDate)
            {
                // end date must be specified.
                // end date must be later than or equal to start date.
                return RecordingValidityCode.BadEndDate;
            }
            if (this.Cadence <= Days.None || this.Cadence > Everyday)
            {
                // repeating days must be specified.
                // invalid number of days in a week.
                return RecordingValidityCode.BadCadence;
            }
            return base.CheckValidity();
        }
    }
}
