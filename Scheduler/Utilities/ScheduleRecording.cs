using System;

namespace Utilities
{
    /// <summary>
    /// This class represents a recording to be scheduled.
    /// </summary>
    public class ScheduleRecording
    {
        public Guid RecorderID { get; set; } // unique ID of the recorder
        public string RecorderName { get; set; } // name of the recorder
        public Guid FolderID { get; set; } // unique ID of the folder
        public string FolderName { get; set; } // name of the recorder
        public string SessionName { get; set; } // name of the session
        public bool IsBroadcast { get; set; } // flag for IsBroadcast status
        public DateTime StartDate { get; set; } // date the recording starts
        public TimeSpan Duration { get; set; } // Duration of the recording
        public string Presenter { get; set; } // Presenter of the session

        /// <summary>
        /// Empty default constructor. Required for CsvHelper to instantiate a Mapping from parsed data to this object.
        /// </summary>
        public ScheduleRecording() { }

        /// <summary>
        /// Determines if this ScheduleRecording object is valid.
        /// </summary>
        /// <returns>Returns RecordingValidityCode.Valid if this ScheduleRecording object is valid.</returns>
        public RecordingValidityCode CheckValidity()
        {
            if (this.RecorderID == Guid.Empty)
            {
                // recorder ID must be specified.
                return RecordingValidityCode.BadRecorderID;
            }
            if (this.FolderID == Guid.Empty)
            {
                // folder ID must be specified.
                return RecordingValidityCode.BadFolderID;
            }
            if (string.IsNullOrEmpty(this.SessionName))
            {
                // session name cannot be null or empty.
                return RecordingValidityCode.BadSessionName;
            }
            if (this.Presenter == null)
            {
                // presenter cannot be null.
                return RecordingValidityCode.BadPresenter;
            }
            if (this.StartDate == DateTime.MinValue || this.StartDate < DateTime.UtcNow)
            {
                // start date must be specified and start time cannot be in the past.
                return RecordingValidityCode.BadStartDate;
            }
            if (this.Duration <= TimeSpan.Zero)
            {   // Negative or zero duration case
                // end time must be later than start time.
                return RecordingValidityCode.BadDuration;
            }
            return RecordingValidityCode.Valid;
        }
    }
}
