using System;
using System.Text;
using appbox.Serialization;

namespace appbox.Expressions
{
    /// <summary>
    /// 用于描述ControlModel某一Event的执行逻辑
    /// </summary>
    public sealed class EventAction : Expression
    {

        //todo:考虑加入权限属性

        public BlockExpression Body { get; private set; }

        /// <summary>
        /// Ctor for serialization
        /// </summary>
        internal EventAction() { }

        /// <summary>
        /// 仅用于设计时
        /// </summary>
        public EventAction(BlockExpression body)
        {
            this.Body = body;
        }

        public override ExpressionType Type => ExpressionType.EventAction;

        public override string ToString()
        {
            return "EventAction";
        }

        public override void ToCode(StringBuilder sb, string preTabs)
        {
            sb.Append("EventAction"); //Statements.ToFriendlyString();
        }

		public override System.Linq.Expressions.Expression ToLinqExpression(IExpressionContext ctx)
		{
			return Body.ToLinqExpression(ctx);
		}

        #region ====Serialization====
        public override void WriteObject(BinSerializer bs)
        {
            base.WriteObject(bs);

            bs.Serialize(this.Body, 1);
            bs.Write((uint)0);
        }

        public override void ReadObject(BinSerializer bs)
        {
            base.ReadObject(bs);

            uint propIndex = 0;
            do
            {
                propIndex = bs.ReadUInt32();
                switch (propIndex)
                {
                    case 1: this.Body = (BlockExpression)bs.Deserialize(); break;
                    case 0: break;
                    default: throw new Exception("Deserialize_ObjectUnknownFieldIndex: " + this.GetType().Name);
                }
            } while (propIndex != 0);
        }
        #endregion
    }

}

