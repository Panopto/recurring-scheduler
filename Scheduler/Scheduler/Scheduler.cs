using CsvHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceModel;
using Utilities;
using Utilities.RemoteRecorderManagement42;
using Utilities.SessionManagement46;
using System.ComponentModel;

namespace Scheduler
{
    public class Scheduler
    {
        /// <summary>
        /// Sets up the access to the server.
        /// </summary>
        /// <param name="userName">The username of the account that has access to schedule recordings.</param>
        /// <param name="password">The password associated with the account <paramref name="userName"/>.</param>
        /// <param name="rrMgr">The client in which holds the information about the recorders available.</param>
        /// <param name="rrAuth">The authentication information for which to access the information from <paramref name="rrMgr"/>.</param>
        /// <param name="sessionMgr">The client in which holds the information about the sessions.</param>
        /// <param name="sessionAuth">The authentication information for which to access the information from <paramref name="sessionMgr"/>.</param>
        public static void SetupSiteAccess(string userName,
                                           string password,
                                           out IRemoteRecorderManagement rrMgr,
                                           out Utilities.RemoteRecorderManagement42.AuthenticationInfo rrAuth,
                                           out ISessionManagement sessionMgr,
                                           out Utilities.SessionManagement46.AuthenticationInfo sessionAuth)
        {
            SetupSiteAccess(userName: userName,
                            password: password,
                            siteName: Properties.Settings.Default.sitename,
                            rrMgr: out rrMgr,
                            rrAuth: out rrAuth,
                            sessionMgr: out sessionMgr,
                            sessionAuth: out sessionAuth);
        }

        /// <summary>
        /// Sets up the access to the server.
        /// </summary>
        /// <param name="userName">The username of the account that has access to schedule recordings.</param>
        /// <param name="password">The password associated with the account <paramref name="userName"/>.</param>
        /// <param name="siteName">The name of the site in which to gain access to.</param>
        /// <param name="rrMgr">The client in which holds the information about the recorders available.</param>
        /// <param name="rrAuth">The authentication information for which to access the information from <paramref name="rrMgr"/>.</param>
        /// <param name="sessionMgr">The client in which holds the information about the sessions.</param>
        /// <param name="sessionAuth">The authentication information for which to access the information from <paramref name="sessionMgr"/>.</param>
        public static void SetupSiteAccess(string userName,
                                           string password,
                                           string siteName,
                                           out IRemoteRecorderManagement rrMgr,
                                           out Utilities.RemoteRecorderManagement42.AuthenticationInfo rrAuth,
                                           out ISessionManagement sessionMgr,
                                           out Utilities.SessionManagement46.AuthenticationInfo sessionAuth)
        {
            if (siteName == "")
            {
                // sitename was not configured
                throw new System.Configuration.ConfigurationErrorsException("Sitename was not configured.");
            }
            // rr manager setup
            rrMgr = new RemoteRecorderManagementClient(
                new BasicHttpBinding(BasicHttpSecurityMode.Transport)
                {
                    MaxReceivedMessageSize = Properties.Settings.Default.HttpBindingMessageSize,
                    SendTimeout = Properties.Settings.Default.HttpBindingTimeout,
                    ReceiveTimeout = Properties.Settings.Default.HttpBindingTimeout
                },
                new EndpointAddress("https://" + siteName + "/Panopto/PublicAPI/4.2/RemoteRecorderManagement.svc")
            );

            // rr auth info setup
            rrAuth = new Utilities.RemoteRecorderManagement42.AuthenticationInfo();
            rrAuth.UserKey = userName;
            rrAuth.Password = password;

            // session manager setup
            sessionMgr = new SessionManagementClient(
                new BasicHttpBinding(BasicHttpSecurityMode.Transport)
                {
                    MaxReceivedMessageSize = Properties.Settings.Default.HttpBindingMessageSize,
                    SendTimeout = Properties.Settings.Default.HttpBindingTimeout,
                    ReceiveTimeout = Properties.Settings.Default.HttpBindingTimeout
                },
                new EndpointAddress("https://" + siteName + "/Panopto/PublicAPI/4.6/SessionManagement.svc")
            );

            // session auth info
            sessionAuth = new Utilities.SessionManagement46.AuthenticationInfo();
            sessionAuth.UserKey = userName;
            sessionAuth.Password = password;
        }

        /// <summary>
        /// Takes the recording client and authentication, and attempts to schedule sessions provided in <paramref name="schedule"/>.
        /// </summary>
        /// <param name="rrMgr">The client in which holds the information about the recorders available.</param>
        /// <param name="rrAuth">The authentication information for which to access the information from <paramref name="rrMgr"/>.</param>
        /// <param name="schedule">The recording schedule.</param>
        /// <returns>The results of the scheduling attempt.</returns>
        public static Dictionary<ScheduleRecording, ScheduledRecordingResult> ScheduleRecordings(
            IRemoteRecorderManagement rrMgr,
            Utilities.RemoteRecorderManagement42.AuthenticationInfo rrAuth,
            List<ScheduleRecording> schedule)
        {
            return ScheduleRecordings(rrMgr, rrAuth, schedule, sender: null);
        }

        /// <summary>
        /// Takes the recording client and authentication, and attempts to schedule sessions provided in <paramref name="schedule"/>.
        /// </summary>
        /// <param name="rrMgr">The client in which holds the information about the recorders available.</param>
        /// <param name="rrAuth">The authentication information for which to access the information from <paramref name="rrMgr"/>.</param>
        /// <param name="schedule">The recording schedule.</param>
        /// <param name="sender">The BackgroundWorker to make this method asynchronous.</param>
        /// <returns>The results of the scheduling attempt.</returns>
        public static Dictionary<ScheduleRecording, ScheduledRecordingResult> ScheduleRecordings(
            IRemoteRecorderManagement rrMgr,
            Utilities.RemoteRecorderManagement42.AuthenticationInfo rrAuth,
            List<ScheduleRecording> schedule,
            BackgroundWorker sender)
        {
            Dictionary<ScheduleRecording, ScheduledRecordingResult> result = new Dictionary<ScheduleRecording, ScheduledRecordingResult>();
            for (int i = 0; i < schedule.Count; i++)
            {
                if (sender != null)
                {
                    sender.ReportProgress(i + 1);
                }
                else
                {
                    Console.WriteLine("Scheduling recording " + (i + 1) + "/" + schedule.Count + "...");
                }
                ScheduleRecording scheduleRecording = schedule[i];
                try
                {
                    // Currently only concerned with 1 RR for each session
                    RecorderSettings rs = new RecorderSettings() { RecorderId = scheduleRecording.RecorderID };
                    RecurringRecording recurring = null;
                    // need to check to make sure start date is on cadence and adjust as needed if recurring.
                    if ((scheduleRecording as RecurringRecording) != null)
                    {
                        scheduleRecording.StartDate = AdjustDateOntoCadence(
                            scheduleRecording.StartDate,
                            ((RecurringRecording)scheduleRecording).Cadence);
                        // since we need to schedule the first occurrence, this is for accurate success/conflict counting
                        recurring = new RecurringRecording((RecurringRecording)scheduleRecording);
                    }
                    ScheduledRecordingResult scheduledResult = rrMgr.ScheduleRecording(
                        rrAuth,
                        scheduleRecording.SessionName,
                        scheduleRecording.FolderID,
                        scheduleRecording.IsBroadcast || Properties.Settings.Default.ScheduleBroadcasts,
                        scheduleRecording.StartDate,
                        scheduleRecording.StartDate + scheduleRecording.Duration,
                        new RecorderSettings[] { rs });
                    result.Add(scheduleRecording, scheduledResult);
                    if ((scheduleRecording as RecurringRecording) != null && !result[scheduleRecording].ConflictsExist)
                    {
                        // need to transform recurring.Cadence to a DayOfWeek[]
                        DayOfWeek[] days = CadenceToArray(recurring.Cadence);
                        // update the dictionary since this is recurring recording.
                        scheduledResult = rrMgr.ScheduleRecurringRecording(rrAuth,
                                                                           result[scheduleRecording].SessionIDs[0],
                                                                           days,
                                                                           recurring.EndDate);
                        result.Add(recurring, scheduledResult);
                    }
                }
                catch
                {
                    // Since something went wrong with scheduling, still log it in result to report it later.
                    result.Add(scheduleRecording, new ScheduledRecordingResult() { ConflictsExist = true });
                }
            }
            return result;
        }

        /// <summary>
        /// Writes the successfully scheduled recordings to an output CSV file.
        /// </summary>
        /// <param name="sessionMgr">The client in which holds the information about the sessions.</param>
        /// <param name="sessionAuth">The authentication information for which to access the information from <paramref name="sessionMgr"/>.</param>
        /// <param name="scheduleSuccesses">The list of unique identifiers for each successfully scheduled recording.</param>
        public static void WriteSuccessFile(ISessionManagement sessionMgr,
                                           Utilities.SessionManagement46.AuthenticationInfo sessionAuth,
                                           List<Guid> scheduleSuccesses)
        {
            WriteSuccessFile(sessionMgr, sessionAuth, scheduleSuccesses, sender: null);
        }

        /// <summary>
        /// Writes the successfully scheduled recordings to an output CSV file.
        /// </summary>
        /// <param name="sessionMgr">The client in which holds the information about the sessions.</param>
        /// <param name="sessionAuth">The authentication information for which to access the information from <paramref name="sessionMgr"/>.</param>
        /// <param name="scheduleSuccesses">The list of unique identifiers for each successfully scheduled recording.</param>
        /// <param name="sender">The BackgroundWorker to make this method asynchronous.</param>
        public static void WriteSuccessFile(ISessionManagement sessionMgr,
                                           Utilities.SessionManagement46.AuthenticationInfo sessionAuth,
                                           List<Guid> scheduleSuccesses,
                                           BackgroundWorker sender)
        {
            // Get the information for each scheduled session and put it into a ScheduleResult object.
            List<ScheduleResult> scheduled = new List<ScheduleResult>();
            Session[] session = sessionMgr.GetSessionsById(sessionAuth, scheduleSuccesses.ToArray());
            for (int j = 0; j < session.Length; j++)
            {
                if (sender != null)
                {
                    // NOTE: This method runs so quickly even at many lines, that this progress report may not be necessary
                    // to update the UI. Still good to have in the event this stalls here though.
                    sender.ReportProgress(j);
                }
                // Use 0 for RemoteRecorderIds since currently only dealing with 1 remote recorder.
                ScheduleResult sr = new ScheduleResult(session[j].RemoteRecorderIds[0],
                                                       session[j].FolderId,
                                                       session[j].Id,
                                                       session[j].Name,
                                                       session[j].StartTime?.ToLocalTime(),
                                                       (session[j].StartTime + TimeSpan.FromSeconds(session[j].Duration.GetValueOrDefault()))?.ToLocalTime());
                scheduled.Add(sr);
            }
            // write what was successfully scheduled to output.csv
            // output.csv will only have a header if there were no session scheduled.
            using (TextWriter writer = new StreamWriter(Properties.Settings.Default.LogsLocation + DateTime.Now.ToString("yyyyMMdd_HHmmss") + @"_success_output.csv"))
            {
                if (sender != null)
                {
                    sender.ReportProgress(session.Length);
                }
                var csv = new CsvWriter(writer);
                csv.WriteRecords(scheduled);
            }
        }

        /// <summary>
        /// Writes the information of the provided conflicts to a log file.
        /// </summary>
        /// <param name="conflicts">All the recordings that conflicted with an earlier recording or are invalid.</param>
        /// <param name="badSchedules">The mappings from line number to error code.</param>
        /// <param name="scheduleSuccesses">The recordings that were successfully scheduled.</param>
        /// <param name="scheduleConflicts">The recordings that were unsuccessfully scheduled.</param>
        public static void WriteConflictLogFile(List<ScheduleRecording> conflicts,
                                                Dictionary<int, RecordingValidityCode> badSchedules,
                                                List<Guid> scheduleSuccesses,
                                                Dictionary<ScheduleRecording, ScheduledRecordingInfo[]> scheduleConflicts,
                                                TextWriter writer)
        {
            if (conflicts != null && badSchedules != null)
            {
                ConsoleUI.PrintInternalScheduleConflictInfo(conflicts, badSchedules, writer);
            }
            else if (scheduleSuccesses != null && scheduleConflicts != null)
            {
                ConsoleUI.PrintScheduleRecordingResults(scheduleSuccesses, scheduleConflicts, writer);
            }
        }

        /// <summary>
        /// Changes a Days enum to a DayOfWeek enum array.
        /// </summary>
        /// <param name="cadence">The Days enum to parse.</param>
        /// <returns>An array of DayOfWeek enum values.</returns>
        public static DayOfWeek[] CadenceToArray(Days cadence)
        {
            List<DayOfWeek> days = new List<DayOfWeek>();
            foreach (Days day in Enum.GetValues(cadence.GetType()))
            {
                if (cadence.HasFlag(day))
                {
                    switch (day)
                    {
                        case Days.Monday: { days.Add(DayOfWeek.Monday); break; }
                        case Days.Tuesday: { days.Add(DayOfWeek.Tuesday); break; }
                        case Days.Wednesday: { days.Add(DayOfWeek.Wednesday); break; }
                        case Days.Thursday: { days.Add(DayOfWeek.Thursday); break; }
                        case Days.Friday: { days.Add(DayOfWeek.Friday); break; }
                        case Days.Saturday: { days.Add(DayOfWeek.Saturday); break; }
                        case Days.Sunday: { days.Add(DayOfWeek.Sunday); break; }
                    }
                }
            }
            return days.ToArray();
        }

        /// <summary>
        /// Checks if the <paramref name="startDate"/> occurs on the specified <paramref name="cadence"/>.
        /// </summary>
        /// <param name="startDate">The date to check.</param>
        /// <param name="cadence">The cadence to check on.</param>
        /// <returns><paramref name="startDate"/> if it aligns with the <paramref name="cadence"/>, else the next day
        /// that coincides with <paramref name="cadence"/>.</returns>
        public static DateTime AdjustDateOntoCadence(DateTime startDate, Days cadence)
        {
            if (startDate == DateTime.MinValue)
            {
                return startDate;
            }
            Days Everyday = Days.Monday
                            | Days.Tuesday
                            | Days.Wednesday
                            | Days.Thursday
                            | Days.Friday
                            | Days.Saturday
                            | Days.Sunday;
            if (cadence <= Days.None || cadence > Everyday)
            {
                throw new ArgumentException("Cadence provided is not valid.");
            }
            DateTime result = startDate;
            Dictionary<DayOfWeek, Days> transform = new Dictionary<DayOfWeek, Days>()
            { { DayOfWeek.Monday, Days.Monday },
              { DayOfWeek.Tuesday, Days.Tuesday },
              { DayOfWeek.Wednesday, Days.Wednesday },
              { DayOfWeek.Thursday, Days.Thursday },
              { DayOfWeek.Friday, Days.Friday },
              { DayOfWeek.Saturday, Days.Saturday },
              { DayOfWeek.Sunday, Days.Sunday }
            };
            DayOfWeek start = result.ToLocalTime().DayOfWeek;
            // if the start date is not on the cadence, a bitwise & should reveal Days.None (all zeros)
            while ((cadence & transform[start]) == Days.None)
            {
                // We need to adjust the start date.
                result = result.AddDays(1);
                start = result.ToLocalTime().DayOfWeek;
            }
            return result;
        }
    }
}
