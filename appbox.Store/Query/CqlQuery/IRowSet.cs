using System;
using System.Collections.Generic;
using appbox.Data;
using appbox.Models;

namespace appbox.Store
{
    public interface IRowSet : IEnumerable<IRow>
    {

        T ToScalar<T>();

        List<Entity> ToEntityList(ulong modelId);
        List<Entity> ToEntityList(EntityModel model);

        List<T> ToList<T>(Func<IRow, T> selector);

    }
}
