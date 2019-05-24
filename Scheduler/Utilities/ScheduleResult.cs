using System;

namespace Utilities
{
    /// <summary>
    /// This class represents a scheduled session.
    /// </summary>
    public class ScheduleResult
    {
        public Guid RecorderID { get; set; } // unique ID of the recorder
        public Guid FolderID { get; set; } // unique ID of the folder
        public Guid SessionID { get; set; } // unique ID of the session
        public string SessionName { get; set; } // name of the session
        public Nullable<DateTime> StartTime { get; set; } // date the recording starts
        public Nullable<DateTime> EndTime { get; set; } // Duration of the recording

        /// <summary>
        /// Creates a new instance of a ScheduleResult object.
        /// </summary>
        /// <param name="RecorderID">The unique ID of the remote recorder that will record this scheduled session.</param>
        /// <param name="FolderID">The unique ID of the folder that this schedule session will record into.</param>
        /// <param name="SessionID">The unique ID of this scheduled session.</param>
        /// <param name="SessionName">The name of this scheduled session.</param>
        /// <param name="StartTime">The start time of this scheduled session.</param>
        /// <param name="EndTime">The end time of this scheduled session.</param>
        public ScheduleResult(Guid RecorderID, Guid FolderID, Guid SessionID, string SessionName, DateTime? StartTime, DateTime? EndTime)
        {
            this.RecorderID = RecorderID;
            this.FolderID = FolderID;
            this.SessionID = SessionID;
            this.SessionName = SessionName;
            this.StartTime = StartTime;
            this.EndTime = EndTime;
        }

        /// <summary>
        /// Determines if this ScheduleResult object is valid.
        /// </summary>
        /// <returns>Returns RecordingValidityCode.Valid if this ScheduleResult object is valid.</returns>
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
            if (this.SessionID == Guid.Empty)
            {
                // folder ID must be specified.
                return RecordingValidityCode.BadSessionID;
            }
            if (string.IsNullOrEmpty(this.SessionName))
            {
                // session name cannot be null or empty.
                return RecordingValidityCode.BadSessionName;
            }
            if (this.StartTime == DateTime.MinValue
                || this.StartTime < DateTime.UtcNow
                || this.StartTime == null)
            {
                // start date must be specified and start time cannot be in the past.
                return RecordingValidityCode.BadStartDate;
            }
            if (this.EndTime == DateTime.MinValue
                || this.EndTime <= this.StartTime
                || this.EndTime == null)
            {   // Negative or zero duration case
                // end time must be later than start time.
                return RecordingValidityCode.BadEndDate;
            }
            return RecordingValidityCode.Valid;
        }
    }
}
