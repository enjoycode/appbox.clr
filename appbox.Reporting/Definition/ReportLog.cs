
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using appbox.Reporting.Resources;


namespace appbox.Reporting.RDL
{
	///<summary>
	/// Error logging (parse and runtime) within report.
	///</summary>
	[Serializable]
	internal class ReportLog
	{
		/// <summary>
		/// list of report items
		/// </summary>
		internal List<string> ErrorItems { get; private set; }

		/// <summary>
		/// maximum severity encountered
		/// </summary>
		internal int MaxSeverity { get; set; }

		internal ReportLog() { }

		internal ReportLog(ReportLog rl)
		{
			if (rl != null && rl.ErrorItems != null)
			{
				MaxSeverity = rl.MaxSeverity;
                ErrorItems = new List<string>(rl.ErrorItems);
			}
		}

		internal void LogError(ReportLog rl)
		{
			if (rl.ErrorItems.Count == 0)
				return;
			LogError(rl.MaxSeverity, rl.ErrorItems);
		}

		internal void LogError(int severity, string item)
		{
			if (ErrorItems == null)			// create log if first time
                ErrorItems = new List<string>();

			if (severity > MaxSeverity)
				MaxSeverity = severity;

			var msg = Strings.ReportLog_Error_Severity + ": " + Convert.ToString(severity) + " - " + item;

			ErrorItems.Add(msg);

			if (severity >= 12)		
				throw new Exception(msg);		// terminate the processing
		}

		internal void LogError(int severity, List<string> list)
		{
			if (ErrorItems == null)			// create log if first time
                ErrorItems = new List<string>();

            if (severity > MaxSeverity)
				MaxSeverity = severity;

			ErrorItems.AddRange(list);
		}

		internal void Reset()
		{
			ErrorItems=null;
			if (MaxSeverity < 8)    // we keep the severity to indicate we can't run report
				MaxSeverity=0;
		}
    }
}
