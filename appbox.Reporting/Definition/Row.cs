namespace appbox.Reporting.RDL
{
	///<summary>
	/// A Row in a data set.
	///</summary>
	internal class Row
	{
        /// <summary>
        /// Original row #
        /// </summary>
        internal int RowNumber { get; set; }

        /// <summary>
        /// Usually 0; set when row is part of group with ParentGroup (ie recursive hierarchy)
        /// </summary>
        internal int Level { get; set; }

        /// <summary>
        /// like level; 
        /// </summary>
        internal GroupEntry GroupEntry { get; set; }

        /// <summary>
        /// Owner of row collection
        /// </summary>
        internal Rows R { get; set; }

        /// <summary>
        /// Row of data
        /// </summary>
        internal object[] Data { get; set; }

        internal Row(Rows r, Row rd)			// Constructor that uses existing Row data
		{
			R = r;
			Data = rd.Data;
			Level = rd.Level;
		}

		internal Row(Rows r, int columnCount)
		{
			R = r;
			Data = new object[columnCount];
			Level=0;
		}
		
	}
}
