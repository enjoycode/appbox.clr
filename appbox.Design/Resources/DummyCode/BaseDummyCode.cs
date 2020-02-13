using System;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace System.Reflection
{
    #region ====Attributes====
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class MemberAccessInterceptorAttribute : Attribute
    {
        public MemberAccessInterceptorAttribute(string name) { }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class InvocationInterceptorAttribute : Attribute
    {
        public InvocationInterceptorAttribute(string name) { }
    }

    /// <summary>
    /// 系统特殊范型创建，必须与RealTypeAttribute一起使用
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class GenericCreateAttribute : Attribute { }

    /// <summary>
    /// 表示可查询或者需要转换表达式的类
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class QueriableClassAttribute : Attribute
    {
        public QueriableClassAttribute(bool sysQuery) { }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class QueryMethodAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Enum)]
    public class EnumModelAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Class)]
    public class ResourceModelAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface)]
    public class RealTypeAttribute : Attribute
    {
        public RealTypeAttribute(string realTypeFullName) { }
    }
    #endregion
}

namespace sys
{
    #region ====Attributes====
    /// <summary>
    /// 控制服务方法的调用权限
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class InvokePermissionAttribute : Attribute
    {
        public InvokePermissionAttribute(bool permission) { }
    }
    #endregion

    #region ====Data====
    //todo: 将该region的代码移至sys.Data命名空间下

    public interface IImageSource { }

    [RealType("appbox.Data.EntityId")]
    public sealed class EntityId
    {
        private EntityId() { }
        public static implicit operator Guid(EntityId id) { return Guid.Empty; }
        public static implicit operator EntityId(Guid guid) { return new EntityId(); }
    }

    [RealType("appbox.Data.Entity")]
    public abstract class EntityBase
    {
    }

    /// <summary>
    /// 映射至系统存储的实体基类
    /// </summary>
    [RealType("appbox.Data.Entity")]
    public abstract class SysEntityBase : EntityBase
    {
        public EntityId Id { get; }

        public DateTime CreateTime { get; }

        public void AcceptChanges() { }

        public void MarkDeleted() { }

        public PersistentState PersistentState { get { return PersistentState.Detached; } }
    }

    /// <summary>
    /// 映射至SqlStore的实体基类
    /// </summary>
    [RealType("appbox.Data.Entity")]
    public abstract class SqlEntityBase : EntityBase
    {
        public void AcceptChanges() { }

        public void MarkDeleted() { }

        public PersistentState PersistentState { get { return PersistentState.Detached; } }
    }

    /// <summary>
	/// 映射至CqlStore的实体基类
	/// </summary>
	[RealType("appbox.Data.Entity")]
    public abstract class CqlEntityBase : EntityBase, ICqlEntityOrView { }

    public interface ICqlEntityOrView { }

    public interface IEntityIndex<out T> where T : EntityBase { }

    [RealType("appbox.Data.PersistentState")]
    public enum PersistentState : byte
    {
        Detached = 0,
        Unchanged = 1,
        Modified = 2,
        Deleted = 3,
    }

    [RealType("appbox.Data.EntityList")]
    [GenericCreate()]
    public sealed class EntityList<T> : Collection<T> where T : EntityBase
    {
        private EntityList() { }

        //public List<T> DeletedItems { get { return null; } }
    }

    //[RealType("AppBox.Core.PermissionNode")]
    //public sealed class PermissionNode
    //{
    //    public string Name { get; set; }

    //    public bool IsFolder { get; }

    //    //public bool IsInherit {get;}
    //    //public bool HasPermission {get;}
    //    public bool HasChanged { get; }

    //    public bool SetCurrentOrgUnit(sys.Entities.OrgUnit ou) { return true; }

    //    public object ChangePermission(bool owns) { return null; }

    //    public void AcceptChanges(object state) { }

    //    public bool CancelChanges(object state) { return true; }
    //}

    //[RealType("AppBox.Core.DataTable")]
    //public sealed class DataTable : System.Data.DataTable
    //{
    //}
    #endregion

    #region ====Funcs====

    /// <summary>
    /// 系统内置表达式函数
    /// </summary>
    public static class Funcs
    {
        ///// <summary>
        ///// 生成顺序Guid
        ///// </summary>
        //[InvocationInterceptor("SystemFunc")]
        //public static Guid NewSequenceGuid() { return Guid.Empty; }

        /// <summary>
        /// 获取当前用户会话信息
        /// </summary>
        [InvocationInterceptor("SystemFunc")]
        public static Runtime.ISessionInfo GetCurrentSession() { return null; }

        // [InvocationInterceptor("SystemFunc")]
        // public static decimal Sum<T>(IList<T> list, Action<T> selector)
        // { return 0; }

        //[InvocationInterceptor("SystemFunc")]
        //public static long Count(object field) { return 0; }

        //[InvocationInterceptor("SystemFunc")]
        //public static T Sum<T>(T field) { return default; }

        //[InvocationInterceptor("SystemFunc")]
        //public static bool In<T>(T field, IList<T> list) { return false; }

        //[InvocationInterceptor("SystemFunc")]
        //public static bool NotIn<T>(T field, IList<T> list) { return false; }
    }

    #endregion

}

namespace sys.Security
{
    [RealType("appbox.Security.IPasswordHasher")]
    public interface IPasswordHasher
    {
        byte[] HashPassword(string password);
        bool VerifyHashedPassword(byte[] hashedPassword, string password);
    }
}

namespace sys.Runtime
{
    [RealType("appbox.Runtime.ISessionInfo")]
    public interface ISessionInfo
    {
        /// <summary>
        /// 是否外部用户会话
        /// </summary>
        bool IsExternal { get; }
        /// <summary>
        /// 内部会话对应的Emploee标识
        /// </summary>
        Guid EmploeeID { get; }
        /// <summary>
        /// 外部会话对应的External标识
        /// </summary>
        Guid ExternalID { get; }

        /// <summary>
        /// 附加会话标记，如SaaS外部用户的租户ID等
        /// </summary>
        string Tag { get; }

        /// <summary>
        /// 会话用户名称
        /// </summary>
        string Name { get; }
        /// <summary>
        /// 会话用户全路径
        /// </summary>
        string FullName { get; }
        /// <summary>
        /// 获取最后一级的组织单元标识, 如果是外部用户则返回上一级WorkGroup的组织单元标识
        /// </summary>
        Guid LeafOrgUnitID { get; }
    }

    [RealType("appbox.Runtime.RuntimeContext")]
    public static class RuntimeContext
    {
        /// <summary>
        /// 当前的会话信息
        /// </summary>
        public static ISessionInfo Session => null;

        /// <summary>
        /// 当前集群节点的标识号
        /// </summary>
        public static ushort PeerId { get; }

        public static Security.IPasswordHasher PasswordHasher => null;
    }
}

namespace sys.Data
{

    [RealType("appbox.Data.ObjectArray")]
    public sealed class ObjectArray : List<object>
    {
        public static explicit operator int[](ObjectArray value) { return null; }

        public static explicit operator string[](ObjectArray value) { return null; }

        public static explicit operator float[](ObjectArray value) { return null; }

        public static explicit operator double[](ObjectArray value) { return null; }

        public static explicit operator decimal[](ObjectArray value) { return null; }

        public static explicit operator Guid[](ObjectArray value) { return null; }

        public static explicit operator DateTime[](ObjectArray value) { return null; }
    }

    [RealType("AppBox.Server.Data.JsonResult")]
    public struct JsonResult
    {
        public JsonResult(object obj) { }
    }

    [RealType("AppBox.Server.Data.FileContentResult")]
    public struct FileContentResult
    {
        public FileContentResult(string fileName, byte[] content) { }
    }

    ///// <summary>
    ///// 映射至关系库的实体
    ///// </summary>
    //public interface ISqlEntity { }

    ///// <summary>
    ///// 表存储实体或物化视图
    ///// </summary>
    //public interface ITableOrView { }

    ///// <summary>
    ///// 映射至表存储的实体
    ///// </summary>
    //public interface ITableEntity : ITableOrView { }

    ///// <summary>
    ///// 表存储物化视图
    ///// </summary>
    //public interface IMaterializedView : ITableOrView { }

    //[RealType("AppBox.Core.FieldSet")]
    //public struct FieldSet<T>
    //{
    //    public FieldSet(params T[] values) { }

    //    public bool Contains(T item) { return false; }

    //    public bool Contains(IEnumerable<T> items) { return false; }

    //    public T[] ToArray() { return null; }

    //    public static FieldSet<T> operator +(FieldSet<T> left, FieldSet<T> right)
    //    {
    //        throw new NotSupportedException();
    //    }

    //    public static FieldSet<T> operator -(FieldSet<T> left, FieldSet<T> right)
    //    {
    //        throw new NotSupportedException();
    //    }
    //}
}

