using System;
using System.Collections;
using System.Collections.Specialized;

namespace appbox.Reporting.RDL
{
    ///<summary>
    /// The sets of data (defined by DataSet) that are retrieved as part of the Report.
    ///</summary>
    [Serializable]
    public class DataSets : IEnumerable
    {
        private readonly Report _rpt;               // runtime report
        private readonly IDictionary _Items;         // list of report items
        public DataSet this[string name] => _Items[name] as DataSet;

        internal DataSets(Report rpt, DataSetsDefn dsn)
        {
            _rpt = rpt;

            if (dsn.Items.Count < 10)
                _Items = new ListDictionary();  // Hashtable is overkill for small lists
            else
                _Items = new Hashtable(dsn.Items.Count);

            // Loop thru all the child nodes
            foreach (DataSetDefn dsd in dsn.Items.Values)
            {
                DataSet ds = new DataSet(rpt, dsd);
                _Items.Add(dsd.Name.Nm, ds);
            }
        }

        #region IEnumerable Members
        public IEnumerator GetEnumerator() => _Items.Values.GetEnumerator();
        #endregion
    }
}
