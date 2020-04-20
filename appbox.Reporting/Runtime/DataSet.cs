using System;
using System.Xml;
using System.Collections;
using System.Data;

namespace appbox.Reporting.RDL
{
	///<summary>
	/// Runtime Information about a set of data; public interface to the definition
	///</summary>
	[Serializable]
	public class DataSet
	{
        private readonly Report _rpt;       //	the runtime report
        private readonly DataSetDefn _dsd;	//  the true definition of the DataSet
	
		internal DataSet(Report rpt, DataSetDefn dsd)
		{
			_rpt = rpt;
			_dsd = dsd;
		}

		public void SetData(IDataReader dr)
		{
			_dsd.Query.SetData(_rpt, dr, _dsd.Fields, _dsd.Filters);		// get the data (and apply the filters
		}

		public void SetData(DataTable dt)
		{
			_dsd.Query.SetData(_rpt, dt, _dsd.Fields, _dsd.Filters);
		}

		public void SetData(XmlDocument xmlDoc)
		{
			_dsd.Query.SetData(_rpt, xmlDoc, _dsd.Fields, _dsd.Filters);
		}

        /// <summary>
        /// Sets the data in the dataset from an IEnumerable. The content of the IEnumerable
        /// depends on the flag collection. If collection is false it will contain classes whose fields
        /// or properties will be matched to the dataset field names. If collection is true it may 
        /// contain IDictionary(s) that will be matched by key with the field name or IEnumerable(s) 
        /// that will be matched by column number. It is possible to have a mix of IDictionary and 
        /// IEnumerable when collection is true.
        /// </summary>
        /// <param name="ie"></param>
        /// <param name="collection"></param>
		public void SetData(IEnumerable ie, bool collection = false)
		{
			_dsd.Query.SetData(_rpt, ie, _dsd.Fields, _dsd.Filters, collection);
		}

        public void SetSource(string sql)
        {
            _dsd.Query.CommandText.SetSource(sql);
        }

	}
}
