using System;
using appbox.Reporting.RDL;


namespace appbox.Reporting.RDL
{
	/// <summary>
	/// If function caches data then this must be implemented and constructor must place in 
	///    master report report
	/// </summary>
	internal interface ICacheData
	{
		void ClearCache(Report rpt);			// clear out cache of data: new data is coming
	}
}
