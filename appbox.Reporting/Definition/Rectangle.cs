using System;
using System.Xml;

namespace appbox.Reporting.RDL
{
	///<summary>
	/// Represent the rectangle report item.
	///</summary>
	[Serializable]
	internal class Rectangle : ReportItem
	{
		/// <summary>
		/// Report items contained within the bounds of the rectangle.
		/// </summary>
		internal ReportItems ReportItems { get; set; }

		/// <summary>
		/// Indicates the report should page break at the start of the rectangle.
		/// </summary>
		internal bool PageBreakAtStart { get; set; }

		/// <summary>
		/// Indicates the report should page break at the end of the rectangle.
		/// </summary>
		internal bool PageBreakAtEnd { get; set; }

		// constructor that doesn't process syntax
		internal Rectangle(ReportDefn r, ReportLink p, XmlNode xNode, bool bNoLoop):base(r,p,xNode)
		{
			ReportItems=null;
			PageBreakAtStart=false;
			PageBreakAtEnd=false;
		}

		internal Rectangle(ReportDefn r, ReportLink p, XmlNode xNode):base(r,p,xNode)
		{
			ReportItems=null;
			PageBreakAtStart=false;
			PageBreakAtEnd=false;

			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name)
				{
					case "ReportItems":
						ReportItems = new ReportItems(r, this, xNodeLoop);
						break;
					case "PageBreakAtStart":
						PageBreakAtStart = XmlUtil.Boolean(xNodeLoop.InnerText, OwnerReport.rl);
						break;
					case "PageBreakAtEnd":
						PageBreakAtEnd = XmlUtil.Boolean(xNodeLoop.InnerText, OwnerReport.rl);
						break;
					default:	
						if (ReportItemElement(xNodeLoop))	// try at ReportItem level
							break;
						// don't know this element - log it
						OwnerReport.rl.LogError(4, "Unknown Rectangle element " + xNodeLoop.Name + " ignored.");
						break;
				}
			}
		}
 
		override internal void FinalPass()
		{
			base.FinalPass();

			if (ReportItems != null)
				ReportItems.FinalPass();

			return;
		}
 
		override internal void Run(IPresent ip, Row row)
		{
			base.Run(ip, row);

			if (ReportItems == null)
				return;

			if (ip.RectangleStart(this, row))
			{
				ReportItems.Run(ip, row);
				ip.RectangleEnd(this, row);
			}
		}

		override internal void RunPage(Pages pgs, Row row)
		{
			Report r = pgs.Report;
            bool bHidden = IsHidden(r, row);

			SetPagePositionBegin(pgs);

            // Handle page breaking at start
            if (this.PageBreakAtStart && !IsTableOrMatrixCell(r) && !pgs.CurrentPage.IsEmpty() && !bHidden)
            {	// force page break at beginning of dataregion
                pgs.NextOrNew();
                pgs.CurrentPage.YOffset = OwnerReport.TopOfPage;
            }

			PageRectangle pr = new PageRectangle();
			SetPagePositionAndStyle(r, pr, row);
			if (pr.SI.BackgroundImage != null)
				pr.SI.BackgroundImage.H = pr.H;		//   and in the background image

            if (!bHidden)
            {
                Page p = pgs.CurrentPage;
                p.AddObject(pr);

                if (ReportItems != null)
                {
                    float saveY = p.YOffset;
       //             p.YOffset += (Top == null ? 0 : this.Top.Points);
                    p.YOffset = pr.Y;       // top of rectangle is base for contained report items
                    ReportItems.RunPage(pgs, row, GetOffsetCalc(pgs.Report) + LeftCalc(r));
                    p.YOffset = saveY;
                }

                // Handle page breaking at end
                if (this.PageBreakAtEnd && !IsTableOrMatrixCell(r) && !pgs.CurrentPage.IsEmpty())
                {	// force page break at beginning of dataregion
                    pgs.NextOrNew();
                    pgs.CurrentPage.YOffset = OwnerReport.TopOfPage;
                }
            }
//			SetPagePositionEnd(pgs, pgs.CurrentPage.YOffset);
            SetPagePositionEnd(pgs, pr.Y + pr.H);
        }

        internal override void RemoveWC(Report rpt)
        {
            base.RemoveWC(rpt);

            if (this.ReportItems == null)
                return;

            foreach (ReportItem ri in this.ReportItems.Items)
            {
                ri.RemoveWC(rpt);
            }
        }

    }
}
