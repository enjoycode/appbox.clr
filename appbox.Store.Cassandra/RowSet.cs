using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using appbox.Data;
using appbox.Models;

namespace appbox.Store
{
    internal struct RowSet : IRowSet
    {
        private readonly Cassandra.RowSet rawRowSet;

        public RowSet(Cassandra.RowSet rawRowSet)
        {
            this.rawRowSet = rawRowSet;
        }

        public List<Entity> ToEntityList(ulong modelId)
        {
            var model = Runtime.RuntimeContext.Current.GetModelAsync<EntityModel>(modelId).Result;
            return ToEntityList(model);
        }

        public List<Entity> ToEntityList(EntityModel model)
        {
            var list = new List<Entity>();
            foreach (var row in this)
            {
                list.Add(row.FetchToEntity(model));
            }
            return list;
        }

        public List<T> ToList<T>(Func<IRow, T> selector)
        {
            var list = new List<T>();
            foreach (var row in this)
            {
                list.Add(selector(row));
            }
            return list;
        }

        public T ToScalar<T>()
        {
            return rawRowSet.First().GetValue<T>(0);
        }

        #region ====IEnumerable====
        public IEnumerator<IRow> GetEnumerator()
        {
            foreach (var row in rawRowSet)
            {
                yield return new Row(row);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        #endregion

    }
}
