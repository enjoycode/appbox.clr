using System;
using System.Collections;

namespace appbox.Reporting.RDL
{
    ///<summary>
    /// A report expression: includes original source, parsed expression and type information.
    ///</summary>
    [Serializable]
    internal class DynamicExpression : IExpr
    {

        /// <summary>
        /// source of expression
        /// </summary>
        internal string Source { get; }

        /// <summary>
        /// expression after parse
        /// </summary>
        internal IExpr Expr { get; private set; }

        internal TypeCode Type { get; }

        private readonly ReportLink _rl;

        internal DynamicExpression(Report rpt, ReportLink p, string expr, Row row)
        {
            Source = expr;
            Expr = null;
            _rl = p;
            Type = DoParse(rpt);
        }

        internal TypeCode DoParse(Report rpt)
        {
            // optimization: avoid expression overhead if this isn't really an expression
            if (Source == null)
            {
                Expr = new Constant("");
                return Expr.GetTypeCode();
            }
            else if (Source == string.Empty ||          // empty expression
                Source[0] != '=')   // if 1st char not '='
            {
                Expr = new Constant(Source);	//   this is a constant value
                return Expr.GetTypeCode();
            }

            Parser p = new Parser(new System.Collections.Generic.List<ICacheData>());

            // find the fields that are part of the DataRegion (if there is one)
            IDictionary fields = null;
            ReportLink dr = _rl.Parent;
            Grouping grp = null;        // remember if in a table group or detail group or list group
            Matrix m = null;

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

            NameLookup lu = new NameLookup(fields, rpt.ReportDefinition.LUReportParameters,
                rpt.ReportDefinition.LUReportItems, rpt.ReportDefinition.LUGlobals,
                rpt.ReportDefinition.LUUser, rpt.ReportDefinition.LUAggrScope,
                grp, m, rpt.ReportDefinition.CodeModules, rpt.ReportDefinition.Classes, rpt.ReportDefinition.DataSetsDefn,
                rpt.ReportDefinition.CodeType);

            try
            {
                Expr = p.Parse(lu, Source);
            }
            catch (Exception e)
            {
                Expr = new ConstantError(e.Message);
                // Invalid expression
                rpt.rl.LogError(8, ErrorText(e.Message));
            }

            // Optimize removing any expression that always result in a constant
            try
            {
                Expr = Expr.ConstantOptimization();
            }
            catch (Exception ex)
            {
                rpt.rl.LogError(4, "Expression:" + Source + "\r\nConstant Optimization exception:\r\n" + ex.Message + "\r\nStack trace:\r\n" + ex.StackTrace);
            }

            return Expr.GetTypeCode();
        }

        private string ErrorText(string msg)
        {
            ReportLink rl = _rl.Parent;
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
            rpt.rl.LogError(severity, err);
        }

        #region IExpr Members
        public TypeCode GetTypeCode() => Expr.GetTypeCode();

        public bool IsConstant() => Expr.IsConstant();

        public IExpr ConstantOptimization() => this;

        public object Evaluate(Report rpt, Row row)
        {
            try
            {
                return Expr.Evaluate(rpt, row);
            }
            catch (Exception e)
            {
                string err;
                if (e.InnerException != null)
                    err = string.Format("Exception evaluating {0}.  {1}.  {2}", Source, e.Message, e.InnerException.Message);
                else
                    err = string.Format("Exception evaluating {0}.  {1}", Source, e.Message);

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
                string err = string.Format("Exception evaluating {0}.  {1}", Source, e.Message);
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
                string err = string.Format("Exception evaluating {0}.  {1}", Source, e.Message);
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
                string err = string.Format("Exception evaluating {0}.  {1}", Source, e.Message);
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
                string err = string.Format("Exception evaluating {0}.  {1}", Source, e.Message);
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
                string err = string.Format("Exception evaluating {0}.  {1}", Source, e.Message);
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
                string err = string.Format("Exception evaluating {0}.  {1}", Source, e.Message);
                ReportError(rpt, 4, err);
                return false;
            }
        }

        #endregion
    }
}
