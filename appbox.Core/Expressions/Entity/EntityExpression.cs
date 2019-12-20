using System;
using System.Collections.Generic;
using System.Text;
using appbox.Models;
using appbox.Serialization;

namespace appbox.Expressions
{
    public sealed class EntityExpression : MemberExpression
    {

        #region ====Fields & Properties====

        public override ExpressionType Type => ExpressionType.EntityExpression;

        /// <summary>
        /// 用于查询时的别名，不用序列化
        /// </summary>
        public string AliasName { get; set; }

        private object _user;
        public object User
        {
            get
            {
                if (Equals(null, Owner))
                    return _user;
                return Owner.User;
            }
            set
            {
                if (Equals(null, Owner))
                    _user = value;
                else
                    throw new NotSupportedException();
            }
        }

        public ulong ModelID { get; private set; }

        //TODO:考虑实现AddToCache，用于下属成员反序列化时自动加入Cache内
        private Dictionary<string, MemberExpression> _cache;
        private Dictionary<string, MemberExpression> Cache
        {
            get
            {
                if (_cache == null)
                    _cache = new Dictionary<string, MemberExpression>();
                return _cache;
            }
        }

        #endregion

        #region ====Default Property====
        public override MemberExpression this[string name]
        {
            get
            {
                MemberExpression exp = null;
                if (Cache.TryGetValue(name, out exp))
                    return exp;

                EntityModel model = Runtime.RuntimeContext.Current.GetModelAsync<EntityModel>(ModelID).Result;
                EntityMemberModel m = model.GetMember(name, false);
                if (m != null)
                {
                    switch (m.Type)
                    {
                        case EntityMemberType.DataField:
                            //case EntityMemberType.Formula:
                            //case EntityMemberType.Aggregate:
                            //case EntityMemberType.AutoNumber:
                            exp = new FieldExpression(name, this);
                            break;
                        case EntityMemberType.EntityRef:
                            var rm = (EntityRefModel)m;
                            if (!rm.IsAggregationRef)
                                exp = new EntityExpression(name, rm.RefModelIds[0], this);
                            else
                                throw new NotImplementedException("尚未实现聚合引用对象的表达式");
                            break;
                        case EntityMemberType.EntitySet:
                            var sm = (EntitySetModel)m;
                            //EntityRefModel erm = esm.RefModel[esm.RefMemberName] as EntityRefModel;
                            exp = new EntitySetExpression(name, this, sm.RefModelId);
                            break;
                        //case EntityMemberType.AggregationRefField:
                        //    exp = new AggregationRefFieldExpression(name, this);
                        //    break;
                        default:
                            throw new NotSupportedException($"EntityExpression.DefaultIndex[]: Not Supported MemberType [{m.Type.ToString()}].");
                    }
                    Cache.Add(name, exp);
                    return exp;
                }
                //如果不包含判断是否继承，或EntityRef's DisplayText
                //if (name.EndsWith("DisplayText", StringComparison.Ordinal)) //TODO: 暂简单判断
                //{
                //    exp = new FieldExpression(name, this);
                //    Cache.Add(name, exp);
                //    return exp;
                //}
                throw new Exception($"Can not find member [{name}] in [{model.Name}].");
            }
        }
        #endregion

        #region ====Ctor====
        /// <summary>
        /// Ctor for Serialization
        /// </summary>
        internal EntityExpression() { }

        /// <summary>
        /// New Root EntityExpression
        /// </summary>
        public EntityExpression(ulong modelID, object user)
            : base(null, null)
        {
            ModelID = modelID;
            _user = user;
        }

        /// <summary>
        /// New EntityRefModel's EntityExpression
        /// </summary>
        internal EntityExpression(string name, ulong modelID, EntityExpression owner)
            : base(name, owner)
        {
            ModelID = modelID;
        }
        #endregion

        #region ====Overrides Methods====
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            EntityExpression target = obj as EntityExpression;
            if (Equals(null, target))
                return false;

            return target.ModelID == ModelID
            && target.User == User && Equals(target.Owner, Owner);
        }

        public override void ToCode(StringBuilder sb, string preTabs)
        {
            if (Equals(Owner, null))
            {
                if (string.IsNullOrEmpty(AliasName))
                    sb.Append("t");
                else
                    sb.Append(AliasName);
            }
            else
            {
                Owner.ToCode(sb, preTabs);
                sb.AppendFormat(".{0}", Name);
            }
        }

        public override System.Linq.Expressions.Expression ToLinqExpression(IExpressionContext ctx)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region ====Serialization Methods====
        public override void WriteObject(BinSerializer cf)
        {
            base.WriteObject(cf);

            cf.Write(ModelID, 2);
            if (_user != null)
                cf.Serialize(_user, 3);

            cf.Write((uint)0);
        }

        public override void ReadObject(BinSerializer cf)
        {
            base.ReadObject(cf);

            uint propIndex;
            do
            {
                propIndex = cf.ReadUInt32();
                switch (propIndex)
                {
                    case 1: cf.ReadBoolean(); break; //todo: 待全部转换完后移除
                    case 2: ModelID = cf.ReadUInt64(); break;
                    case 3: _user = cf.Deserialize(); break;
                    case 0: break;
                    default: throw new Exception("Deserialize_ObjectUnknownFieldIndex: " + GetType().Name);
                }
            } while (propIndex != 0);
        }
        #endregion

    }
}
