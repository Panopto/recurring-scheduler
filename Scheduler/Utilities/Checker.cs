using System;
using System.Collections.Generic;
using System.Linq;
using Utilities.SessionManagement46;
using Utilities.RemoteRecorderManagement42;

namespace Utilities
{
    public class Checker
    {
        public static Dictionary<string, Guid> recorderIds = new Dictionary<string, Guid>();
        /// <summary>
        /// Checks if there are any scheduling conflicts in <paramref name="schedule"/>. If conflicts exist, a mapping is added to
        /// <paramref name="badSchedules"/> from line number to the error code.
        /// </summary>
        /// <param name="schedule">The schedule to check conflicts on.</param>
        /// <param name="recordingToLine">The mappings from recording to line number.</param>
        /// <param name="badSchedules">The mappings from line number to error code to be populated, if needed.</param>
        /// <returns>All the recordings that conflicted with an earlier recording or are invalid.</returns>
        public static List<ScheduleRecording> CheckConflicts(List<ScheduleRecording> schedule,
                                                             Dictionary<ScheduleRecording, int> recordingToLine,
                                                             Dictionary<int, RecordingValidityCode> badSchedules)
        {
            List<ScheduleRecording> conflictingRecordings = new List<ScheduleRecording>();
            // Update lastEndTime only if a valid recording doesn't conflict with already added valid recordings
            // Only finds conflicts with the first recording that it conflicts with, not all recordings added.
            Dictionary<Guid, List<ScheduleRecording>> recorderToSortedSchedules = new Dictionary<Guid, List<ScheduleRecording>>();
            for (int i = 0; i < schedule.Count; i++)
            {
                if (!recorderToSortedSchedules.ContainsKey(schedule[i].RecorderID))
                {
                    // New recorder list is needed.
                    recorderToSortedSchedules[schedule[i].RecorderID] = new List<ScheduleRecording>();
                }
                // add the schedule to the list.
                recorderToSortedSchedules[schedule[i].RecorderID].Add(schedule[i]);
            }
            foreach (List<ScheduleRecording> sortedSchedule in recorderToSortedSchedules.Values)
            {
                List<List<ScheduleRecording>> weekSchedule = new List<List<ScheduleRecording>>();
                if (sortedSchedule[0] as RecurringRecording != null) {
                    for (int i = 0; i < 7; i++)
                    {
                        weekSchedule.Add(new List<ScheduleRecording>());
                    }
                    foreach (ScheduleRecording sr in sortedSchedule)
                    {
                        foreach (Days day in Enum.GetValues(typeof(Days)))
                        {
                            switch ((sr as RecurringRecording).Cadence & day)
                            {
                                case Days.Monday:
                                    weekSchedule[0].Add(sr);
                                    break;
                                case Days.Tuesday:
                                    weekSchedule[1].Add(sr);
                                    break;
                                case Days.Wednesday:
                                    weekSchedule[2].Add(sr);
                                    break;
                                case Days.Thursday:
                                    weekSchedule[3].Add(sr);
                                    break;
                                case Days.Friday:
                                    weekSchedule[4].Add(sr);
                                    break;
                                case Days.Saturday:
                                    weekSchedule[5].Add(sr);
                                    break;
                                case Days.Sunday:
                                    weekSchedule[6].Add(sr);
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
                foreach (List<ScheduleRecording> daySchedule in weekSchedule)
                {
                    // Sort by the start date
                    daySchedule.Sort((a, b) => a.StartDate.CompareTo(b.StartDate));
                    DateTime lastEndTime = DateTime.MinValue;
                    foreach (ScheduleRecording sr in daySchedule)
                    {
                        if (sr.CheckValidity() != RecordingValidityCode.Valid)
                        {
                            // this recording is not valid so don't check it
                            conflictingRecordings.Add(sr);
                            continue;
                        }
                        if (sr.StartDate < lastEndTime)
                        {
                            conflictingRecordings.Add(sr);
                            // Map the line number to the TimeConflict error code
                            badSchedules.Add(recordingToLine[sr], RecordingValidityCode.TimeConflict);
                        }
                        else
                        {
                            lastEndTime = sr.StartDate + sr.Duration;
                        }
                    }
                }
            }
            return conflictingRecordings;
        }

        /// <summary>
        /// Attempts to get any remote recorder with the provided <paramref name="rrAuth"/> credentials.
        /// </summary>
        /// <param name="rrMgr">The client in which holds the information about the recorders</param>
        /// <param name="rrAuth">The authentication information for which to access the information from <paramref name="rrMgr"/>.</param>
        /// <returns>A code that determines the result of the attempted login.</returns>
        public static LoginResults TryLoginGetRecorder(IRemoteRecorderManagement rrMgr, RemoteRecorderManagement42.AuthenticationInfo rrAuth)
        {
            try
            {
                var pagination = new RemoteRecorderManagement42.Pagination { MaxNumberResults = 1, PageNumber = 0 };
                var recorderListResponse = rrMgr.ListRecorders(rrAuth, pagination, RecorderSortField.Name);

                // no Remote Recorders found
                if (recorderListResponse.TotalResultCount < 1)
                    return LoginResults.NoAccess;
            }
            catch
            {
                // ListRecorders throws an exception if authentication fails either incorrect credentials or not admin.
                return LoginResults.Failure;
            }
            return LoginResults.Success;
        }

        /// <summary>
        /// Gets the unique ID of the recorder specified by <paramref name="recorderName"/>.
        /// </summary>
        /// <param name="recorderName">Name of the recorder to get the ID for.</param>
        /// <param name="rrMgr">The client in which holds the information about the recorder <paramref name="recorderName"/>.</param>
        /// <param name="rrAuth">The authentication information for which to access the information from <paramref name="rrMgr"/>.</param>
        /// <returns>Returns the unique ID, if found. Else, will return Guid.Empty.</returns>
        public static Guid GetRecorderID(string recorderName, IRemoteRecorderManagement rrMgr, RemoteRecorderManagement42.AuthenticationInfo rrAuth)
        {
            Guid rrID = Guid.Empty;
            // Handle no recorder name here
            if (recorderName.Equals(""))
            {
                return rrID;
            }

            // set the recorderName to lowercase for insensitive lookup
            recorderName = recorderName.ToLowerInvariant();

            if(recorderIds.ContainsKey(recorderName))
            {
                return recorderIds[recorderName];
            }

            if(recorderIds.Count > 0)
            {
                return Guid.Empty;
            }
            
            bool lastPage = false;
            int resultsPerPage = 5;
            int pageNumber = 0;
            while (!lastPage)
            {
                RemoteRecorderManagement42.Pagination rrPage = new RemoteRecorderManagement42.Pagination()
                {
                    MaxNumberResults = resultsPerPage,
                    PageNumber = pageNumber
                };

                ListRecordersResponse rrResponse = rrMgr.ListRecorders(rrAuth, rrPage, RecorderSortField.Name);
                if (resultsPerPage * (pageNumber + 1) >= rrResponse.TotalResultCount)
                {
                    lastPage = true;
                }
                // Populate the lookup map with the lowercase'd names of the recorders
                foreach (RemoteRecorder rr in rrResponse.PagedResults)
                {
                    recorderIds[rr.Name.ToLowerInvariant()] = rr.Id;
                }
                // keep looking
                pageNumber++;
            }
            // If rrID stays empty then remote recorder wasn't found or handle the case in which not found
            return GetRecorderID(recorderName, rrMgr, rrAuth);
        }

        /// <summary>
        /// Gets the unique ID of the folder specified by <paramref name="folderName"/>.
        /// </summary>
        /// <param name="folderName">name fo the folder to the ID for.</param>
        /// <param name="rrMgr">The client in which holds the information about the recorder <paramref name="recorderName"/>.</param>
        /// <param name="rrAuth">The authentication information for which to access the information from <paramref name="rrMgr"/>.</param>
        /// <param name="rrID">The unique ID of a recorder.</param>
        /// <param name="sessionMgr">The client in which holds the information about the folder <paramref name="folderName"/>.</param>
        /// <param name="sessionAuth">The authentication information for which to access the information from <paramref name="sessionMgr"/></param>
        /// <returns>Returns the unique ID, if found. Else, returns Guid.Empty</returns>
        public static Guid GetFolderID(string folderName,
                                       ISessionManagement sessionMgr,
                                       SessionManagement46.AuthenticationInfo sessionAuth)
        {
            if (folderName != "")
            {
                // if the foldername is a guid, trust it literally
                Guid folderId;
                if (Guid.TryParse(folderName, out folderId))
                {
                    return folderId;
                }

                // otherwise use the sessionmanagement api
                SessionManagement46.Pagination pagination = new SessionManagement46.Pagination()
                {
                    MaxNumberResults = 10,
                    PageNumber = 0
                };
                ListFoldersRequest request = new ListFoldersRequest()
                {
                    Pagination = pagination,
                    ParentFolderId = Guid.Empty,
                    SortBy = FolderSortField.Relavance
                };
                // try to get the folder up to 3 times
                ListFoldersResponse response = null;
                for (int retries = 0; retries < 3; retries++)
                {
                    try
                    {
                        // Add quotes here to search for exact folder name
                        response = sessionMgr.GetFoldersList(sessionAuth, request, "\"" + folderName + "\"");
                        break;
                    }
                    catch
                    {
                        // catch the FaultException if the request times out.
                    }
                }
                // if the response is null, then the GetFoldersList request has failed all retry attempts.
                // By returning Guid.Empty, the validation process of a ScheduleRecording will catch it as a generic
                // RecordingValidity.BadFolderID error code. A more descriptive error code can be considered such as
                // RecordingValidityCode.TimeOutError, and this would have to throw an exception or return a special
                // Guid to differentiate it from a regular RecordingValidityCode.BadFolderID (ie a Guid with all 1s)
                if (response == null)
                {
                    // This is the timeout case, maybe have a special reserved Guid to reflect this.
                    return Guid.Empty;
                }
                // we check the number of results and if there is more than 1 result/match then
                // folder name is ambiguous (not unique) and so return Guid.Empty
                if (response.Results.Length == 1)
                {
                    return response.Results[0].Id;
                }
                else
                {
                    Console.WriteLine(
                        "{0} results for {1}: {2}",
                        response.Results.Length,
                        folderName,
                            response.Results.Length > 0
                                ? string.Join("\n\t", response.Results.Select(f => f.Name))
                                : "");
                    return Guid.Empty;
                }
            }
            return Guid.Empty;
        }

        /// <summary>
        /// Categorizes the <paramref name="result"/> of scheduling recordings,
        /// returned through <paramref name="scheduleSuccesses"/> and <paramref name="scheduleConflicts"/>.
        /// </summary>
        /// <param name="result">The results of attempting to schedule recordings.</param>
        /// <param name="scheduleSuccesses">Returns the recordings that were successfully scheduled.</param>
        /// <param name="scheduleConflicts">Returns the recordings that were unsuccessfully scheduled.</param>
        public static void GetSuccessesAndConflicts(Dictionary<ScheduleRecording, ScheduledRecordingResult> result,
                                                    out List<Guid> scheduleSuccesses,
                                                    out Dictionary<ScheduleRecording, ScheduledRecordingInfo[]> scheduleConflicts)
        {
            scheduleSuccesses = new List<Guid>();
            scheduleConflicts = new Dictionary<ScheduleRecording, ScheduledRecordingInfo[]>();
            foreach (ScheduleRecording sr in result.Keys)
            {
                ScheduledRecordingResult scheduleResult = result[sr];
                if (!scheduleResult.ConflictsExist)
                {
                    foreach (Guid id in scheduleResult.SessionIDs)
                    {
                        scheduleSuccesses.Add(id);
                    }
                }
                else
                {
                    scheduleConflicts.Add(sr, scheduleResult.ConflictingSessions);
                }
            }
        }
    }
}
