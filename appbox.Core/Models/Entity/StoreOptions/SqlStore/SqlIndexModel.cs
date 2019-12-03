using System;

namespace appbox.Models
{
    public sealed class SqlIndexModel : IndexModelBase
    {
        //暂没有特定定义

        #region ====Ctor====
        internal SqlIndexModel() { }

        internal SqlIndexModel(EntityModel owner, string name, bool unique,
            FieldWithOrder[] fields, ushort[] storingFields = null)
            : base(owner, name, unique, fields, storingFields) { }
        #endregion
    }
}
