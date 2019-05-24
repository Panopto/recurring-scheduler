namespace Utilities
{
    public enum RecordingValidityCode
    {
        Valid = 0,
        ParseError = 1,
        TimeConflict = 2,
        BadRecorderID = 3,
        BadFolderID = 4,
        BadSessionID = 5,
        BadSessionName = 6,
        BadPresenter = 7,
        BadStartDate = 8,
        BadDuration = 9,
        BadEndDate = 10,
        BadCadence = 11
    }
}
