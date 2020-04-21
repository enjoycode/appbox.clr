
using System;
using System.Xml;
using System.Collections;
using System.Collections.Specialized;
using System.Threading;
using appbox.Reporting.RDL;

namespace appbox.Reporting.RDL
{
    ///<summary>
    /// A report expression: includes original source, parsed expression and type information.
    ///</summary>
    [Serializable]
    internal class Expression : ReportLink, IExpr
    {
        /// <summary>
        /// unique name of expression; not always created
        /// </summary>
        internal string UniqueName { get; set; }

        /// <summary>
        /// source of expression
        /// </summary>
        internal string Source { get; private set; }

        /// <summary>
        /// expression after parse
        /// </summary>
        internal IExpr Expr { get; private set; }

        /// <summary>
        /// type of expression; only available after parsed
        /// </summary>
        internal TypeCode Type { get; private set; }

        /// <summary>
        /// expected type of expression
        /// </summary>
        internal ExpressionType ExpectedType { get; }

        internal Expression(ReportDefn r, ReportLink p, XmlNode xNode, ExpressionType et)
            : this(r, p, xNode.InnerText, et) { }

        internal Expression(ReportDefn r, ReportLink p, String xNode, ExpressionType et) : base(r, p)
        {
            Source = xNode;
            Type = TypeCode.Empty;
            ExpectedType = et;
            Expr = null;
        }

        override internal void FinalPass()
        {
            // optimization: avoid expression overhead if this isn't really an expression
            if (Source == null)
            {
                Expr = new Constant("");
                return;
            }
            else if (Source == "" ||           // empty expression
                Source[0] != '=')  // if 1st char not '='
            {
                Expr = new Constant(Source);  //   this is a constant value
                return;
            }

            Parser p = new Parser(OwnerReport.DataCache);

            // find the fields that are part of the DataRegion (if there is one)
            IDictionary fields = null;
            ReportLink dr = Parent;
            Grouping grp = null;        // remember if in a table group or detail group or list group
            Matrix m = null;
            ReportLink phpf = null;
            while (dr != null)
            {
                if (dr is Grouping)
                    p.NoAggregateFunctions = true;
                else if (dr is TableGroup)
                    grp = ((TableGroup)dr).Grouping;
                else if (dr is Matrix)
                {
                    m = (Matrix)dr;     // if matrix we need to pass special
                    break;
                }
                else if (dr is Details)
                {
                    grp = ((Details)dr).Grouping;
                }
                else if (dr is List)
                {
                    grp = ((List)dr).Grouping;
                    break;
                }
                else if (dr is PageHeader || dr is PageFooter)
                {
                    phpf = dr;
                }
                else if (dr is DataRegion || dr is DataSetDefn)
                    break;
                dr = dr.Parent;
            }
            if (dr != null)
            {
                if (dr is DataSetDefn)
                {
                    DataSetDefn d = (DataSetDefn)dr;
                    if (d.Fields != null)
                        fields = d.Fields.Items;
                }
                else    // must be a DataRegion
                {
                    DataRegion d = (DataRegion)dr;
                    if (d.DataSetDefn != null &&
                        d.DataSetDefn.Fields != null)
                        fields = d.DataSetDefn.Fields.Items;
                }
            }

            NameLookup lu = new NameLookup(fields, OwnerReport.LUReportParameters,
                OwnerReport.LUReportItems, OwnerReport.LUGlobals,
                OwnerReport.LUUser, OwnerReport.LUAggrScope,
                grp, m, OwnerReport.CodeModules, OwnerReport.Classes, OwnerReport.DataSetsDefn,
                OwnerReport.CodeType);

            if (phpf != null)
            {
                // Non-null when expression is in PageHeader or PageFooter; 
                //   Expression name needed for dynamic lookup of ReportItems on a page.
                lu.PageFooterHeader = phpf;
                lu.ExpressionName = UniqueName = "xn_" + Interlocked.Increment(ref Parser.Counter).ToString();
            }

            try
            {
                Expr = p.Parse(lu, Source);
            }
            catch (Exception e)
            {
                Expr = new ConstantError(e.Message);
                // Invalid expression
                OwnerReport.rl.LogError(8, ErrorText(e.Message));
            }

            // Optimize removing any expression that always result in a constant
            try
            {
                Expr = Expr.ConstantOptimization();
            }
            catch (Exception ex)
            {
                OwnerReport.rl.LogError(4, "Expression:" + Source + "\r\nConstant Optimization exception:\r\n" + ex.Message + "\r\nStack trace:\r\n" + ex.StackTrace);
            }
            Type = Expr.GetTypeCode();
        }

        private string ErrorText(string msg)
        {
            ReportLink rl = this.Parent;
            while (rl != null)
            {
                if (rl is ReportItem)
                    break;
                rl = rl.Parent;
            }

            string prefix = "Expression";
            if (rl != null)
            {
                ReportItem ri = rl as ReportItem;
                if (ri.Name != null)
                    prefix = ri.Name.Nm + " expression";
            }
            return prefix + " '" + Source + "' failed to parse: " + msg;
        }

        private void ReportError(Report rpt, int severity, string err)
        {
            if (rpt == null)
                OwnerReport.rl.LogError(severity, err);
            else
                rpt.rl.LogError(severity, err);
        }

        #region ====IExpr Members====
        public TypeCode GetTypeCode()
        {
            if (Expr == null)
                return TypeCode.Object;      // we really don't know the type
            return Expr.GetTypeCode();
        }

        public bool IsConstant()
        {
            if (Expr == null)
            {
                this.FinalPass();           // expression hasn't been parsed yet -- let try to parse it.
                if (Expr == null)
                    return false;           // no luck; then don't treat as constant
            }
            return Expr.IsConstant();
        }

        public IExpr ConstantOptimization()
        {
            return this;
        }

        public object Evaluate(Report rpt, Row row)
        {
            try
            {
                // Check to see if we're evaluating an expression in a page header or footer;
                //   If that is the case the rows are cached by page.
                if (row == null && this.UniqueName != null)
                {
                    Rows rows = rpt.GetPageExpressionRows(UniqueName);
                    if (rows != null && rows.Data != null && rows.Data.Count > 0)
                        row = rows.Data[0];
                }

                return Expr.Evaluate(rpt, row);
            }
            catch (Exception e)
            {
                string err;
                if (e.InnerException != null)
                    err = $"Exception evaluating {Source}.  {e.Message}.  {e.InnerException.Message}";
                else
                    err = $"Exception evaluating {Source}.  {e.Message}";

                ReportError(rpt, 4, err);
                return null;
            }
        }

        public string EvaluateString(Report rpt, Row row)
        {
            try
            {
                return Expr.EvaluateString(rpt, row);
            }
            catch (Exception e)
            {
                string err = String.Format("Exception evaluating {0}.  {1}", Source, e.Message);
                ReportError(rpt, 4, err);
                return null;
            }
        }

        public double EvaluateDouble(Report rpt, Row row)
        {
            try
            {
                return Expr.EvaluateDouble(rpt, row);
            }
            catch (Exception e)
            {
                string err = String.Format("Exception evaluating {0}.  {1}", Source, e.Message);
                ReportError(rpt, 4, err);
                return double.NaN;
            }
        }

        public decimal EvaluateDecimal(Report rpt, Row row)
        {
            try
            {
                return Expr.EvaluateDecimal(rpt, row);
            }
            catch (Exception e)
            {
                string err = String.Format("Exception evaluating {0}.  {1}", Source, e.Message);
                ReportError(rpt, 4, err);
                return decimal.MinValue;
            }
        }

        public int EvaluateInt32(Report rpt, Row row)
        {
            try
            {
                return Expr.EvaluateInt32(rpt, row);
            }
            catch (Exception e)
            {
                string err = String.Format("Exception evaluating {0}.  {1}", Source, e.Message);
                ReportError(rpt, 4, err);
                return int.MinValue;
            }
        }

        public DateTime EvaluateDateTime(Report rpt, Row row)
        {
            try
            {
                return Expr.EvaluateDateTime(rpt, row);
            }
            catch (Exception e)
            {
                string err = String.Format("Exception evaluating {0}.  {1}", Source, e.Message);
                ReportError(rpt, 4, err);
                return DateTime.MinValue;
            }
        }

        public bool EvaluateBoolean(Report rpt, Row row)
        {
            try
            {
                return Expr.EvaluateBoolean(rpt, row);
            }
            catch (Exception e)
            {
                string err = String.Format("Exception evaluating {0}.  {1}", Source, e.Message);
                ReportError(rpt, 4, err);
                return false;
            }
        }
        #endregion

        public void SetSource(string sql)
        {
            this.Source = sql;
            FinalPass();
        }

    }
}
