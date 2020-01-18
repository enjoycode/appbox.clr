using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using appbox.Data;

namespace appbox.Store
{
    public struct CqlBatch
    {
        private readonly CqlStore store;

        public List<CqlCommand> Commands { get; }

        public CqlBatch(CqlStore store)
        {
            this.store = store;
            Commands = new List<CqlCommand>();
        }

        public Task ExecuteAsync()
        {
            return store.ExecuteAsync(ref this);
        }

        #region ====辅助方法====
        public void Insert(Entity entity, bool ifNotExists = false)
        {
            Commands.Add(new CqlCommand(CqlCommandType.Insert, entity, ifNotExists));
        }

        public void Update(Entity entity, bool ifNotExists = false)
        {
            Commands.Add(new CqlCommand(CqlCommandType.Update, entity, ifNotExists));
        }

        public void Delete(Entity entity, bool ifNotExits = false)
        {
            Commands.Add(new CqlCommand(CqlCommandType.Delete, entity, ifNotExits));
        }
        #endregion

    }
}
