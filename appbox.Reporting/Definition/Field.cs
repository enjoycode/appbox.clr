using System;
using System.Xml;

namespace appbox.Reporting.RDL
{
    ///<summary>
    /// Definition of a field within a DataSet.   
    ///</summary>
    [Serializable]
    internal class Field : ReportLink
    {
        /// <summary>
        /// Name to use for the field within the report
        /// Note: Field names need only be unique within the containing Fields collection
        /// Note: Either _DataField or _Value must be specified but not both
        /// </summary>
        internal Name Name { get; set; }

        /// <summary>
        /// Name of the field in the query
        /// Note: DataField names do not need to be unique.
        /// Multiple fields can refer to the same data field.
        /// </summary>
        internal string DataField { get; set; }

        internal int ColumnNumber { get; set; }

        private TypeCode _Type;
        /// <summary>
        /// The data type of the field
        /// </summary>
        internal TypeCode Type
        {
            get
            {
                if (Value == null || Value.Expr == null)        // expression?
                    return _Type;              //  no just return the type
                return Value.Expr.GetTypeCode();
            }
            set { _Type = value; }
        }

        /// <summary>
        /// Query column: resolved from the query SQL
        /// </summary>
        internal QueryColumn qColumn { get; private set; }

        internal TypeCode RunType => qColumn != null ? qColumn.colType : Type;

        /// <summary>
        /// (Variant) An expression that evaluates to the value of this field.
        /// For example, =Fields!Price.Value+Fields!Tax.Value
        /// The expression cannot contain aggregates or references to report items.	
        /// </summary>
        internal Expression Value { get; set; }

        internal Field(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
        {
            Name = null;
            DataField = null;
            Value = null;
            ColumnNumber = -1;
            _Type = TypeCode.String;
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
                    case "DataField":
                        DataField = xNodeLoop.InnerText;
                        break;
                    case "TypeName":        // Extension !!!!!!!!!!!!!!!!!
                    case "rd:TypeName":     // Microsoft Designer uses this extension
                        _Type = DataType.GetStyle(xNodeLoop.InnerText, this.OwnerReport);
                        break;
                    case "Value":
                        Value = new Expression(r, this, xNodeLoop, ExpressionType.Variant);
                        break;
                    default:
                        // don't know this element - log it
                        OwnerReport.rl.LogError(4, "Unknown Field element '" + xNodeLoop.Name + "' ignored.");
                        break;
                }
            }
            if (DataField != null && Value != null)
                OwnerReport.rl.LogError(8, "Only DataField or Value may be specified in a Field element, not both.");
            else if (DataField == null && Value == null)
                OwnerReport.rl.LogError(8, "Either DataField or Value must be specified in a Field element.");
        }

        // Handle parsing of function in final pass
        override internal void FinalPass()
        {
            if (Value != null)
                Value.FinalPass();

            // Resolve the field if specified
            if (DataField != null)
            {
                Fields f = (Fields)Parent;
                DataSetDefn ds = (DataSetDefn)f.Parent;
                Query q = ds.Query;
                if (q != null && q.Columns != null)
                {
                    qColumn = (QueryColumn)q.Columns[DataField];
                    if (qColumn == null)
                    {   // couldn't find the data field
                        OwnerReport.rl.LogError(8, "DataField '" + DataField + "' not part of query.");
                    }
                }
            }

            return;
        }

    }
}
