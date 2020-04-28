using System;
using System.Xml;

namespace appbox.Reporting.RDL
{
	///<summary>
	/// Handle a Matrix Row: i.e. height and matrix cells that make up the row.
	///</summary>
	[Serializable]
	internal class MatrixRow : ReportLink
	{
		/// <summary>
		/// Height of each detail cell in this row.
		/// </summary>
		internal RSize Height { get; set; }

		/// <summary>
		/// The set of cells in a row in the detail section of the Matrix.
		/// </summary>
		internal MatrixCells MatrixCells { get; set; }

		internal MatrixRow(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			Height=null;
			MatrixCells=null;

			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name)
				{
					case "Height":
						Height = new RSize(r, xNodeLoop);
						break;
					case "MatrixCells":
						MatrixCells = new MatrixCells(r, this, xNodeLoop);
						break;
					default:
						break;
				}
			}
			if (MatrixCells == null)
				OwnerReport.rl.LogError(8, "MatrixRow requires the MatrixCells element.");
		}
		
		override internal void FinalPass()
		{
			if (MatrixCells != null)
				MatrixCells.FinalPass();
			return;
		}

    }
}
