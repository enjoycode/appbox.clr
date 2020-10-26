using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Models;
using OmniSharp.Mef;

namespace appbox.Design
{
    /// <summary>
    /// 改变实体模型的成员的属性
    /// </summary>
    sealed class ChangeEntityMember : IRequestHandler
    {
        public Task<object> Handle(DesignHub hub, InvokeArgs args)
        {
            var modelID = args.GetString();
            var memberName = args.GetString();
            var propertyName = args.GetString();
            // var propertyValue = args.GetString();

            var modelNode = hub.DesignTree.FindModelNode(ModelType.Entity, ulong.Parse(modelID));
            if (modelNode == null)
                throw new Exception($"Can't find Entity model: {modelID}");
            var model = (EntityModel)modelNode.Model;
            var member = model.GetMember(memberName, true);
            //TODO:如果改变DataField数据类型预先检查兼容性

            PropertyInfo[] dms = null;
            switch (member.Type)
            {
                case EntityMemberType.DataField:
                    dms = typeof(DataFieldModel).GetProperties(); break;
                case EntityMemberType.EntityRef:
                    dms = typeof(EntityRefModel).GetProperties(); break;
                case EntityMemberType.EntitySet:
                    dms = typeof(EntitySetModel).GetProperties(); break;
                default:
                    throw new NotImplementedException();
            }

            var dm = dms.FirstOrDefault(t => t.Name == propertyName);
            if (dm == null)
                throw new Exception($"Can't find EntityMemberModel's property: {propertyName}");
            if (dm.PropertyType.IsEnum)
				dm.SetValue(member, args.GetByte()); //Convert.ToByte(propertyValue));
            else if (dm.PropertyType == typeof(decimal))
				dm.SetValue(member, args.GetDecimal()); // Convert.ToDecimal(propertyValue));
            else if (dm.PropertyType == typeof(DateTime))
				dm.SetValue(member, args.GetDateTime()); // Convert.ToDateTime(propertyValue));
            else if (dm.PropertyType == typeof(uint))
				dm.SetValue(member, args.GetUInt32()); // Convert.ToUInt32(propertyValue));
			else if (dm.PropertyType == typeof(ulong))
				dm.SetValue(member, args.GetUInt64()); // Convert.ToUInt64(propertyValue));
            else if (dm.PropertyType == typeof(int))
				dm.SetValue(member, args.GetInt32()); // Convert.ToInt32(propertyValue));
            else if (dm.PropertyType == typeof(bool))
				dm.SetValue(member, args.GetBoolean()); // Convert.ToBoolean(propertyValue));
            else
				dm.SetValue(member, args.GetString()); // propertyValue);

            if (member.Type == EntityMemberType.DataField)
            {
                var dfm = (DataFieldModel)member;
                if (propertyName == "DataType" || propertyName == "Length"
                    || propertyName == "Decimals" || propertyName == "DefaultValue"
                    || propertyName == "AllowNull")
                    dfm.OnDataTypeChanged();
            }

            return Task.FromResult<object>(1);
        }
    }
}
