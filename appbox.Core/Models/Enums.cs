using System;
using appbox.Data;

namespace appbox.Models
{

    public enum ModelLayer : byte
    {
        /// <summary>
        /// 系统层
        /// </summary>
        SYS = 0,
        /// <summary>
        /// 开发层
        /// </summary>
        DEV = 1,
        /// <summary>
        /// 用户层
        /// </summary>
        USR = 2
    }

    /// <summary>
    /// 模型类型
    /// </summary>
    public enum ModelType : byte
    {
        Application = 0,
        Enum = 1,
        Entity = 2,
        Event = 3,
        Service = 4,
        View = 5,
        Workflow = 6,
        Report = 7,
        Folder = 8,
        Permission = 9,
        DataStore = 10,
    }

    /// <summary>
    /// 实体模型的存储方式
    /// </summary>
    public enum EntityStoreType : byte
    {
        StoreWithMvcc = 0,
        StoreWithoutMvcc = 1,
    }

    /// <summary>
    /// 实体成员类型
    /// </summary>
    public enum EntityMemberType : byte
    {
        //数据字段
        DataField = 0,
        //引用对象的显示文本，用于不加载对应的引用实例的界面绑定
        EntityRefDisplayText = 1,
        //引用对象
        EntityRef = 2,
        //子对象集
        EntitySet = 3,
        //聚合引用字段
        AggregationRefField = 4,
        //自动编号
        AutoNumber = 7,
        //原始值跟踪
        Tracker = 8,
        //图片源
        ImageSource = 10,
    }

    public enum EntityFieldType : byte
    {
        EntityId = 0,
        String,
        DateTime,
        UInt16,
        Int16,
        UInt32,
        Int32,
        UInt64,
        Int64,
        Decimal,
        Boolean,
        Guid,
        Byte,
        Binary,
        Enum,
        Float,
        Double,
    }

    public static class EntityFieldTypeHelper
    {
        public static Type GetValueType(this EntityFieldType fieldType)
        {
            //TODO: fix others
            switch (fieldType)
            {
                case EntityFieldType.EntityId:
                    return typeof(EntityId);
                case EntityFieldType.String:
                    return typeof(string);
                case EntityFieldType.DateTime:
                    return typeof(DateTime);
                case EntityFieldType.Int32:
                    return typeof(int);
                case EntityFieldType.Int64:
                    return typeof(long);
                case EntityFieldType.UInt64:
                    return typeof(ulong);
                case EntityFieldType.Decimal:
                    return typeof(decimal);
                case EntityFieldType.Float:
                    return typeof(float);
                case EntityFieldType.Double:
                    return typeof(double);
                case EntityFieldType.Boolean:
                    return typeof(bool);
                case EntityFieldType.Guid:
                    return typeof(Guid);
                case EntityFieldType.Byte:
                    return typeof(byte);
                case EntityFieldType.Binary:
                    return typeof(byte[]);
                case EntityFieldType.Enum:
                    return typeof(int);
                default:
                    return typeof(object);
            }
        }
    }
}
