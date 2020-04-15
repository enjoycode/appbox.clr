using System;
using System.Xml;
using System.Collections.Generic;

namespace appbox.Reporting.RDL
{
    ///<summary>
    /// Grouping definition: expressions forming group, paging forced when group changes, ...
    ///</summary>
    [Serializable]
    internal class Grouping : ReportLink
    {
        private string _DataElementName;
        private string _DataCollectionName;
        private List<Textbox> _HideDuplicates;  // holds any textboxes that use this as a hideduplicate scope

        /// <summary>
        /// true if grouping is in a matrix
        /// </summary>
        internal bool InMatrix { get; private set; }

        /// <summary>
        /// Name of the Grouping (for use in RunningValue and RowNumber)
        /// No two grouping elements may have the same name. No grouping element may
        /// have the same name as a data set or a data region
        /// </summary>
        internal Name Name { get; set; }

        /// <summary>
        /// (string) A label to identify an instance of the group
        /// within the client UI (to provide a userfriendly
        /// label for searching). See ReportItem.Label
        /// </summary>
        internal Expression Label { get; set; }

        /// <summary>
        /// The expressions to group the data by
        /// </summary>
        internal GroupExpressions GroupExpressions { get; set; }

        /// <summary>
        /// Indicates the report should page break at the start of the group.
        /// Not valid for column groupings in Matrix regions.
        /// </summary>
        internal bool PageBreakAtStart { get; set; }

        /// <summary>
        /// Indicates the report should page break at the end of the group.
        /// Not valid for column groupings in Matrix regions.
        /// </summary>
        internal bool PageBreakAtEnd { get; set; }

        /// <summary>
        /// Custom information to be passed to the report output component.
        /// </summary>
        internal Custom Custom { get; set; }

        /// <summary>
        /// Filters to apply to each instance of the group.
        /// </summary>
        internal Filters Filters { get; set; }

        /// <summary>
        /// (Variant) An expression that identifies the parent
        /// group in a recursive hierarchy.
        /// Only allowed if the group has exactly one group expression.
        /// Indicates the following:
        /// 1. Groups should be sorted according to the recursive hierarchy (Sort is still used to sort peer groups).
        /// 2. Labels (in the document map) should be placed/indented according to the recursive hierarchy.
        /// 3. Intra-group show/hide should toggle items according to the recursive hierarchy (see ToggleItem)
        /// If filters on the group eliminate a group instance's parent, it is instead treated as a
        /// child of the parent's parent.
        /// </summary>
        internal Expression ParentGroup { get; set; }

        /// <summary>
        /// The name to use for the data element for instances of this group.
        /// Default: Name of the group
        /// </summary>
        internal string DataElementName
        {
            get
            {
                if (_DataElementName == null)
                {
                    if (Name != null)
                        return Name.Nm;
                }
                return _DataElementName;
            }
            set { _DataElementName = value; }
        }

        /// <summary>
        /// The name to use for the data element for
        /// </summary>
        internal string DataCollectionName
        {
            get
            {
                if (_DataCollectionName == null)
                    return DataElementName + "_Collection";
                return _DataCollectionName;
            }
            set { _DataCollectionName = value; }
        }

        /// <summary>
        /// Indicates whether the group should appear in a data rendering.
        /// Default: Output
        /// </summary>
        internal DataElementOutputEnum DataElementOutput { get; set; }

        internal Grouping(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
        {
            Name = null;
            Label = null;
            GroupExpressions = null;
            PageBreakAtStart = false;
            PageBreakAtEnd = false;
            Custom = null;
            Filters = null;
            ParentGroup = null;
            _DataElementName = null;
            _DataCollectionName = null;
            DataElementOutput = DataElementOutputEnum.Output;
            _HideDuplicates = null;
            // Run thru the attributes
            foreach (XmlAttribute xAttr in xNode.Attributes)
            {
                switch (xAttr.Name)
                {
                    case "Name":
                        Name = new Name(xAttr.Value);
                        break;
                }
            }
            // Loop thru all the child nodes
            foreach (XmlNode xNodeLoop in xNode.ChildNodes)
            {
                if (xNodeLoop.NodeType != XmlNodeType.Element)
                    continue;
                switch (xNodeLoop.Name)
                {
                    case "Label":
                        Label = new Expression(r, this, xNodeLoop, ExpressionType.String);
                        break;
                    case "GroupExpressions":
                        GroupExpressions = new GroupExpressions(r, this, xNodeLoop);
                        break;
                    case "PageBreakAtStart":
                        PageBreakAtStart = XmlUtil.Boolean(xNodeLoop.InnerText, OwnerReport.rl);
                        break;
                    case "PageBreakAtEnd":
                        PageBreakAtEnd = XmlUtil.Boolean(xNodeLoop.InnerText, OwnerReport.rl);
                        break;
                    case "Custom":
                        Custom = new Custom(r, this, xNodeLoop);
                        break;
                    case "Filters":
                        Filters = new Filters(r, this, xNodeLoop);
                        break;
                    case "Parent":
                        ParentGroup = new Expression(r, this, xNodeLoop, ExpressionType.Variant);
                        break;
                    case "DataElementName":
                        _DataElementName = xNodeLoop.InnerText;
                        break;
                    case "DataCollectionName":
                        _DataCollectionName = xNodeLoop.InnerText;
                        break;
                    case "DataElementOutput":
                        DataElementOutput = RDL.DataElementOutput.GetStyle(xNodeLoop.InnerText, OwnerReport.rl);
                        break;
                    default:
                        // don't know this element - log it
                        OwnerReport.rl.LogError(4, "Unknown Grouping element '" + xNodeLoop.Name + "' ignored.");
                        break;
                }
            }
            if (this.Name != null)
            {
                try
                {
                    OwnerReport.LUAggrScope.Add(this.Name.Nm, this);        // add to referenceable Grouping's
                }
                catch   // wish duplicate had its own exception
                {
                    OwnerReport.rl.LogError(8, "Duplicate Grouping name '" + this.Name.Nm + "'.");
                }
            }
            if (GroupExpressions == null)
                OwnerReport.rl.LogError(8, "Group Expressions are required within group '" + (this.Name == null ? "unnamed" : this.Name.Nm) + "'.");
        }

        // Handle parsing of function in final pass
        override internal void FinalPass()
        {
            if (Label != null)
                Label.FinalPass();
            if (GroupExpressions != null)
                GroupExpressions.FinalPass();
            if (Custom != null)
                Custom.FinalPass();
            if (Filters != null)
                Filters.FinalPass();
            if (ParentGroup != null)
                ParentGroup.FinalPass();

            // Determine if group is defined inside of a Matrix;  these get
            //   different runtime expression handling in FunctionAggr
            InMatrix = false;
            for (ReportLink rl = this.Parent; rl != null; rl = rl.Parent)
            {
                if (rl is Matrix)
                {
                    InMatrix = true;
                    break;
                }
                if (rl is Table || rl is List || rl is Chart)
                    break;
            }

            return;
        }

        internal void AddHideDuplicates(Textbox tb)
        {
            if (_HideDuplicates == null)
                _HideDuplicates = new List<Textbox>();
            _HideDuplicates.Add(tb);
        }

        internal void ResetHideDuplicates(Report rpt)
        {
            if (_HideDuplicates == null)
                return;

            foreach (Textbox tb in _HideDuplicates)
                tb.ResetPrevious(rpt);
        }

        internal int GetIndex(Report rpt)
        {
            WorkClass wc = GetValue(rpt);
            return wc.index;
        }

        internal void SetIndex(Report rpt, int i)
        {
            WorkClass wc = GetValue(rpt);
            wc.index = i;
            return;
        }

        internal Rows GetRows(Report rpt)
        {
            WorkClass wc = GetValue(rpt);
            return wc.rows;
        }

        internal void SetRows(Report rpt, Rows rows)
        {
            WorkClass wc = GetValue(rpt);
            wc.rows = rows;
            return;
        }

        private WorkClass GetValue(Report rpt)
        {
            WorkClass wc = rpt.Cache.Get(this, "wc") as WorkClass;
            if (wc == null)
            {
                wc = new WorkClass();
                rpt.Cache.Add(this, "wc", wc);
            }
            return wc;
        }

        class WorkClass
        {
            internal int index;         // used by tables (and others) to set grouping values
            internal Rows rows;         // used by matrixes to get/set grouping values
            internal WorkClass()
            {
                index = -1;
            }
        }

    }
}
