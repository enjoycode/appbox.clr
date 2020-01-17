using System;
using appbox.Data;
using appbox.Models;

namespace appbox.Store
{
    public interface IRow
    {

        Entity FetchToEntity(ulong modelId);

        Entity FetchToEntity(EntityModel model);

        T Fetch<T>(Func<IRow, T> selector);

        T GetValue<T>(string name);

        T GetValue<T>(int ordinal);

    }
}
