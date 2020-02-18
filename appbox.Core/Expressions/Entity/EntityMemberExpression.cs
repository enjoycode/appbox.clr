using System;
using appbox.Serialization;

namespace appbox.Expressions
{
    public abstract class MemberExpression : Expression //TODO: rename to EntityMemberExpression
    {

        /// <summary>
        /// 名称
        /// 分以下几种情况：
        /// 1、如果为EntityExpression
        /// 1.1 如果为Root EntityExpression，Name及Owner属性皆为null
        /// 1.2 如果为Ref EntityExpression，Name即属性名称
        /// 2、如果为FieldExpression，Name为属性名称
        /// </summary>
        public string Name { get; internal set; }

        public EntityExpression Owner { get; private set; }

        public abstract MemberExpression this[string name] { get; }

        /// <summary>
        /// Ctor for Serialization
        /// </summary>
        internal MemberExpression() { }

        internal MemberExpression(string name, EntityExpression owner)
        {
            Name = name;
            Owner = owner;
        }

        /// <summary>
        /// eg: Customer.Name => CustomerName
        /// </summary>
        /// <returns></returns>
        internal string GetFieldAlias()
        {
            return Expression.IsNull(Owner) ? Name : $"{Owner.GetFieldAlias()}{Name}";
        }

        #region ====Serialization Methods====
        public override void WriteObject(BinSerializer writer)
        {
            base.WriteObject(writer);

            writer.Write(Name, 1);
            writer.Serialize(Owner, 2);

            writer.Write((uint)0);
        }

        public override void ReadObject(BinSerializer reader)
        {
            base.ReadObject(reader);

            uint propIndex;
            do
            {
                propIndex = reader.ReadUInt32();
                switch (propIndex)
                {
                    case 1: Name = reader.ReadString(); break;
                    case 2: Owner = (EntityExpression)reader.Deserialize(); break;
                    case 0: break;
                    default: throw new Exception("Deserialize_ObjectUnknownFieldIndex: " + GetType().Name);
                }
            } while (propIndex != 0);
        }
        #endregion

    }
}
