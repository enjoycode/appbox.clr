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
        private readonly DataSetDefn _dsd;  //  the true definition of the DataSet

        internal DataSet(Report rpt, DataSetDefn dsd)
        {
            _rpt = rpt;
            _dsd = dsd;
        }

        public void SetData(IDataReader dr)
        {
            _dsd.Query.SetData(_rpt, dr, _dsd.Fields, _dsd.Filters);        // get the data (and apply the filters
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

        //====DesignTime Methods====
        public void MakePreviewData(int rows)
        {
            rows = Math.Min(rows, 128); //暂最多128行

            var dt = new DataTable();
            Field field;
            foreach (var col in _dsd.Fields)
            {
                field = (Field)col;
                dt.Columns.Add(field.Name.Nm, XmlUtil.GetTypeFromTypeCode(field.RunType));
            }

            //TODO:如果有DataRegion绑定且有分组则模拟分组，暂简单模拟分组

            for (int i = 0; i < rows; i++)
            {
                var row = dt.NewRow();
                int j = 0;
                int no;
                foreach (var col in _dsd.Fields)
                {
                    no = i % (j + 3);
                    field = (Field)col;
                    switch (field.RunType)
                    {
                        case TypeCode.Boolean:
                            row[j] = i % 2 == 0; break;
                        case TypeCode.Object:
                        case TypeCode.String:
                            row[j] = $"{field.Name.Nm}{no}";
                            break;
                        case TypeCode.Char:
                            row[j] = 'C'; break;
                        case TypeCode.DateTime:
                            row[j] = new DateTime(1977, 3, no + 1); break;
                        default: //left numbers
                            row[j] = Convert.ChangeType(no, field.RunType); break;
                    }
                    j++;
                }
                dt.Rows.Add(row);
            }

            SetData(dt);
        }
    }
}
