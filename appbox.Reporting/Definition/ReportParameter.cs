using System;
using System.Xml;
using System.Collections;
using System.Globalization;

namespace appbox.Reporting.RDL
{
    ///<summary>
    /// Represent a report parameter (name, default value, runtime value,
    ///</summary>
    [Serializable]
    internal class ReportParameter : ReportLink
    {
        private readonly bool _NumericType = false;  // true if _dt is a numeric type

        /// <summary>
        /// The data type of the parameter
        /// </summary>
        internal TypeCode dt { get; set; }

        /// <summary>
        /// Indicates the value for this parameter is allowed to be Null.
        /// </summary>
        internal bool Nullable { get; set; }

        /// <summary>
        /// indicates parameter should not be showed to user
        /// </summary>
        internal bool Hidden { get; set; } = false;

        /// <summary>
        /// indicates parameter is a collection - expressed as 0 based arrays Parameters!p1.Value(0)
        /// </summary>
        internal bool MultiValue { get; set; } = false;

        /// <summary>
        /// Default value to use for the parameter (if not provided by the user)
        /// If no value is provided as a part of the definition or by the user,
        /// the value is null.
        /// Required if there is no Prompt and either Nullable is False or a ValidValues
        /// list is provided that does not contain Null (an omitted Value).
        /// </summary>
        internal DefaultValue DefaultValue { get; set; }

        /// <summary>
        /// Indicates the value for this parameter is allowed to be the empty string.
        /// Ignored if DataType is not string.
        /// </summary>
        internal bool AllowBlank { get; set; }

        /// <summary>
        /// The user prompt to display when asking for parameter values
        /// If omitted, the user should not be prompted for a value for this parameter.
        /// </summary>
        internal string Prompt { get; set; }

        /// <summary>
        /// Possible values for the parameter (for an end-user prompting interface)
        /// </summary>
        internal ValidValues ValidValues { get; set; }

        /// <summary>
        /// Enum True | False | Auto (default)
        ///	Indicates whether or not the parameter is used in a query in the report.
        ///	This is needed to determine if the queries need to be re-executed if the parameter
        ///	changes. Auto indicates the UsedInQuery setting should be
        ///	autodetected as follows: True if the parameter is referenced in any query value expression.		
        /// </summary>
        internal TrueFalseAutoEnum UsedInQuery { get; set; }

        internal ReportParameter(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
        {
            Name = null;
            dt = TypeCode.Object;
            Nullable = false;
            DefaultValue = null;
            AllowBlank = false;
            Prompt = null;
            ValidValues = null;
            UsedInQuery = TrueFalseAutoEnum.Auto;
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
                    case "DataType":
                        dt = DataType.GetStyle(xNodeLoop.InnerText, OwnerReport);
                        _NumericType = DataType.IsNumeric(dt);
                        break;
                    case "Nullable":
                        Nullable = XmlUtil.Boolean(xNodeLoop.InnerText, OwnerReport.rl);
                        break;
                    case "DefaultValue":
                        DefaultValue = new DefaultValue(r, this, xNodeLoop);
                        break;
                    case "AllowBlank":
                        AllowBlank = XmlUtil.Boolean(xNodeLoop.InnerText, OwnerReport.rl);
                        break;
                    case "Prompt":
                        Prompt = xNodeLoop.InnerText;
                        break;
                    case "Hidden":
                        Hidden = XmlUtil.Boolean(xNodeLoop.InnerText, OwnerReport.rl);
                        OwnerReport.rl.LogError(4, "ReportParameter element Hidden is currently ignored."); // TODO
                        break;
                    case "MultiValue":
                        MultiValue = XmlUtil.Boolean(xNodeLoop.InnerText, OwnerReport.rl);
                        break;
                    case "ValidValues":
                        ValidValues = new ValidValues(r, this, xNodeLoop);
                        break;
                    case "UsedInQuery":
                        UsedInQuery = TrueFalseAuto.GetStyle(xNodeLoop.InnerText, OwnerReport.rl);
                        break;
                    default:
                        // don't know this element - log it
                        OwnerReport.rl.LogError(4, $"Unknown ReportParameter element '{xNodeLoop.Name}' ignored.");
                        break;
                }
            }
            if (Name == null)
                OwnerReport.rl.LogError(8, "ReportParameter name is required but not specified.");

            if (dt == TypeCode.Object)
                OwnerReport.rl.LogError(8, string.Format("ReportParameter DataType is required but not specified or invalid for {0}.", Name == null ? "<unknown name>" : Name.Nm));
        }

        override internal void FinalPass()
        {
            if (DefaultValue != null)
                DefaultValue.FinalPass();
            if (ValidValues != null)
                ValidValues.FinalPass();
            return;
        }

        /// <summary>
        /// Name of the parameter 
        /// Note: Parameter names need only be
        /// unique within the containing Parameters collection
        /// </summary>
        internal Name Name { get; set; }

        internal object GetRuntimeValue(Report rpt)
        {
            object rtv = rpt == null ? null :
                rpt.Cache.Get(this, "runtimevalue");

            if (rtv != null)
                return rtv;
            if (DefaultValue == null)
                return null;

            object[] result = DefaultValue.GetValue(rpt);
            if (result == null)
                return null;
            object v = result[0];
            if (v is string && _NumericType)
                v = ConvertStringToNumber((string)v);

            rtv = Convert.ChangeType(v, dt);
            if (rpt != null)
                rpt.Cache.Add(this, "runtimevalue", rtv);

            return rtv;
        }

        internal ArrayList GetRuntimeValues(Report rpt)
        {
            ArrayList rtv = rpt == null ? null :
                (ArrayList)rpt.Cache.Get(this, "rtvs");

            if (rtv != null)
                return rtv;

            if (DefaultValue == null)
                return null;

            object[] result = DefaultValue.GetValue(rpt);
            if (result == null)
                return null;

            ArrayList ar = new ArrayList(result.Length);
            foreach (object v in result)
            {
                object nv = v;
                if (nv is string && _NumericType)
                    nv = ConvertStringToNumber((string)nv);

                ar.Add(Convert.ChangeType(nv, dt));
            }

            if (rpt != null)
                rpt.Cache.Add(this, "rtvs", ar);

            return ar;
        }

        internal void SetRuntimeValue(Report rpt, object v)
        {
            if (MultiValue)
            {   // ok to only set one parameter of multiValue;  but we still save as MultiValue
                ArrayList ar;
                if (v is string)
                {   // when the value is a string we parse it looking for multiple arguments
                    ParameterLexer pl = new ParameterLexer(v as string);
                    ar = pl.Lex();
                }
                else if (v is ICollection)
                {   // when collection put it in local array
                    ar = new ArrayList(v as ICollection);
                }
                else
                {
                    ar = new ArrayList(1);
                    ar.Add(v);
                }

                SetRuntimeValues(rpt, ar);
                return;
            }

            object rtv;
            if (v is Guid)
            { //Added from Forum, User: solidstore
                v = ((Guid)v).ToString("B");
            }
            if (!AllowBlank && dt == TypeCode.String && (string)v == "")
                throw new ArgumentException(string.Format("Empty string isn't allowed for {0}.", Name.Nm));
            try
            {
                if (v is String && _NumericType)
                    v = ConvertStringToNumber((string)v);
                rtv = Convert.ChangeType(v, dt);
            }
            catch (Exception e)
            {
                // illegal parameter passed
                string err = "Illegal parameter value for '" + Name.Nm + "' provided.  Value =" + v.ToString();
                if (rpt == null)
                    OwnerReport.rl.LogError(4, err);
                else
                    rpt.rl.LogError(4, err);
                throw new ArgumentException(string.Format("Unable to convert '{0}' to {1} for {2}", v, dt, Name.Nm), e);
            }
            rpt.Cache.AddReplace(this, "runtimevalue", rtv);
        }

        internal void SetRuntimeValues(Report rpt, ArrayList vs)
        {
            if (!this.MultiValue)
                throw new ArgumentException(string.Format("{0} is not a MultiValue parameter. SetRuntimeValues only valid for MultiValue parameters", this.Name.Nm));

            ArrayList ar = new ArrayList(vs.Count);
            foreach (object v in vs)
            {
                object rtv;
                if (!AllowBlank && dt == TypeCode.String && v.ToString() == "")
                {
                    string err = string.Format("Empty string isn't allowed for {0}.", Name.Nm);
                    if (rpt == null)
                        OwnerReport.rl.LogError(4, err);
                    else
                        rpt.rl.LogError(4, err);
                    throw new ArgumentException(err);
                }
                try
                {
                    object nv = v;
                    if (nv is String && _NumericType)
                        nv = ConvertStringToNumber((string)nv);
                    rtv = Convert.ChangeType(nv, dt);
                    ar.Add(rtv);
                }
                catch (Exception e)
                {
                    // illegal parameter passed
                    string err = "Illegal parameter value for '" + Name.Nm + "' provided.  Value =" + v.ToString();
                    if (rpt == null)
                        OwnerReport.rl.LogError(4, err);
                    else
                        rpt.rl.LogError(4, err);
                    throw new ArgumentException(string.Format("Unable to convert '{0}' to {1} for {2}", v, dt, Name.Nm), e);
                }
            }
            rpt.Cache.AddReplace(this, "rtvs", ar);
        }

        private object ConvertStringToNumber(string newv)
        {
            // remove any commas, currency symbols (internationalized)
            NumberFormatInfo nfi = NumberFormatInfo.CurrentInfo;
            if (!string.IsNullOrEmpty(nfi.NumberGroupSeparator))
                newv = newv.Replace(nfi.NumberGroupSeparator, "");
            if (!string.IsNullOrEmpty(nfi.CurrencySymbol))
                newv = newv.Replace(nfi.CurrencySymbol, "");
            return newv;
        }

    }

    /// <summary>
    /// Public class used to pass user provided report parameters.
    /// </summary>
    public class UserReportParameter
    {
        readonly Report _rpt;
        readonly ReportParameter _rp;
        object[] _DefaultValue;
        string[] _DisplayValues;
        object[] _DataValues;

        internal UserReportParameter(Report rpt, ReportParameter rp)
        {
            _rpt = rpt;
            _rp = rp;
        }
        /// <summary>
        /// Name of the report paramenter.
        /// </summary>
        public string Name
        {
            get { return _rp.Name.Nm; }
        }

        /// <summary>
        /// Type of the report parameter.
        /// </summary>
        public TypeCode dt
        {
            get { return _rp.dt; }
        }

        /// <summary>
        /// Is parameter allowed to be null.
        /// </summary>
        public bool Nullable
        {
            get { return _rp.Nullable; }
        }

        /// <summary>
        /// Default value(s) of the parameter.
        /// </summary>
        public object[] DefaultValue
        {
            get
            {
                if (_DefaultValue == null)
                {
                    if (_rp.DefaultValue != null)
                        _DefaultValue = _rp.DefaultValue.ValuesCalc(this._rpt);
                }
                return _DefaultValue;
            }
        }

        /// <summary>
        /// Is parameter allowed to be the empty string?
        /// </summary>
        public bool AllowBlank
        {
            get { return _rp.AllowBlank; }
        }
        /// <summary>
        /// Does parameters accept multiple values?
        /// </summary>
        public bool MultiValue
        {
            get { return _rp.MultiValue; }
        }

        /// <summary>
        /// Text used to prompt for the parameter.
        /// </summary>
        public string Prompt
        {
            get { return _rp.Prompt; }
        }

        /// <summary>
        /// The display values for the parameter.  These may differ from the data values.
        /// </summary>
        public string[] DisplayValues
        {
            get
            {
                if (_DisplayValues == null)
                {
                    if (_rp.ValidValues != null)
                        _DisplayValues = _rp.ValidValues.DisplayValues(_rpt);
                }
                return _DisplayValues;
            }
        }

        /// <summary>
        /// The data values of the parameter.
        /// </summary>
        public object[] DataValues
        {
            get
            {
                if (_DataValues == null)
                {
                    if (_rp.ValidValues != null)
                        _DataValues = _rp.ValidValues.DataValues(this._rpt);
                }
                return _DataValues;
            }
        }

        /// <summary>
        /// Obtain the data value from a (potentially) display value
        /// </summary>
        /// <param name="dvalue">Display value</param>
        /// <returns>The data value cooresponding to the display value.</returns>
        private object GetDataValueFromDisplay(object dvalue)
        {
            object val = dvalue;

            if (dvalue != null &&
                DisplayValues != null &&
                DataValues != null &&
                DisplayValues.Length == DataValues.Length)		// this should always be true
            {	// if display values are provided then we may need to 
                //  use the provided value with a display value and use
                //  the cooresponding data value
                string sval = dvalue.ToString();
                for (int index = 0; index < DisplayValues.Length; index++)
                {
                    if (DisplayValues[index].CompareTo(sval) == 0)
                    {
                        val = DataValues[index];
                        break;
                    }
                }
            }
            return val;
        }

        /// <summary>
        /// The runtime value of the parameter.
        /// </summary>
        public object Value
        {
            get { return _rp.GetRuntimeValue(this._rpt); }
            set
            {
                if (this.MultiValue && value is string)
                {   // treat this as a multiValue request
                    Values = ParseValue(value as string);
                    return;
                }

                object dvalue = GetDataValueFromDisplay(value);

                _rp.SetRuntimeValue(_rpt, dvalue);
            }
        }

        /// <summary>
        /// Take a string and parse it into multiple values
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        private ArrayList ParseValue(string v)
        {
            ParameterLexer pl = new ParameterLexer(v);
            return pl.Lex();
        }

        /// <summary>
        /// The runtime values of the parameter when MultiValue.
        /// </summary>
        public ArrayList Values
        {
            get { return _rp.GetRuntimeValues(this._rpt); }
            set
            {
                ArrayList ar = new ArrayList(value.Count);
                foreach (object v in value)
                {
                    ar.Add(GetDataValueFromDisplay(v));
                }
                _rp.SetRuntimeValues(_rpt, ar);
            }
        }
    }
}
