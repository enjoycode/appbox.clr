using System;
using System.Threading.Tasks;
using appbox.Models;

namespace appbox.Store
{
    public abstract class CqlStore
    {

        #region ====DDL Methods====
        public abstract Task CreateTableAsync(EntityModel model);

        public abstract Task AlterTableAsync(EntityModel model);

        public abstract Task DropTableAsync(EntityModel model);
        #endregion
    }
}
