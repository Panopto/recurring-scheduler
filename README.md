# recurring-scheduler

This is a tool for bulk scheduling Panopto sessions by reading a CSV specification.
Several formats are supported for easily importing files exported from class scheduling tools.

In particular, this tool supports a format to specify recurring class meetings
with a meeting time and place and the days of the week on which the meetings convene.

DETAILS
-------

This solution makes use of several Panopto APIs to schedule sessions against remote recorders.
For that reason the tool requires some configuration to exercise the APIs, including a password.

Build the solution, configure the Scheduler.exe.config file to point to the desired Panopto site,
and execute Scheduler.exe from the command line.
Usage: Scheduler.exe userName password filepath [startDate] [endDate] [term] [--check]

In practice, institutions have different needs and use different tools and conventions
to express when and where classes meet. This tool can be regarded as a minimal viable effort
to support a few example formats. Institutions should use it as a starting place for
authouring their own custom solution.

At a high level, the program workflow goes like this:
1. Read the file and detect its format by reading the CSV headers.
2. For each line, parse the meaning of the line:
	a. Determine overall start and end dates for the class.
	b. Parse the beginning and end times of the class.
	c. Parse the days of the week on which the class meets.
	d. Determine the Panopto folder the recordings should be committed to.
	e. Determine the Remote Recorder that should be used to capture the classes.
3. Once all the lines are parsed, check internally for consistency:
	a. Do all the folders and remote recorders exist?
	b. Are any remote recorders double-booked?
4. Schedule the sessions one at a time, noting success and failures.


THIS REPOSITORY IS UNSUPPORTED
------------------------------
This repository is provided as-is for general developer guidance.

The Panopto team does not support this repository.

License
-------

Copyright 2019 Panopto, Inc.

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
