using System;
using System.Linq;
using appbox.Reporting.RDL;

namespace RdlEngine.Render.ExcelConverter
{
	internal class ExcelTable
	{
		public Table Table { get; set; }
		public float GrowedBottomPosition { get; set; }
		public float OriginalBottomPosition { get; set; }

        public float TableWidth => Table.TableColumns.Items.Sum(x => x.Width.Points);
    }
}
