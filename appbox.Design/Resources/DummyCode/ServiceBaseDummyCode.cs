using System;
using System.Data.Common;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reflection;
using sys;

[RealType("appbox.Log")]
public static class Log
{
    public static void Debug(string message) { }
    public static void Info(string message) { }
    public static void Warn(string message) { }
    public static void Error(string message) { }
}

[RealType("appbox.SimplePerfTest")]
public static class SimplePerfTest
{
    public static Task<string> Run(int taskCount, int loopCount, Func<int, int, ValueTask> action)
    { throw new Exception(); }
}

#region ====DbFuncs && Extensions====
public static class DbFuncs
{
    public static int Sum(int field) { return 0; }

    public static long Sum(long field) { return 0; }

    public static int Avg(int field) { return 0; }

    public static long Avg(long field) { return 0; }

    public static bool In<T>(this T source, IEnumerable<T> list) { return true; }

    public static bool NotIn<T>(this T source, IEnumerable<T> list) { return true; }
}
#endregion

#region ====Entity Store====
[RealType("appbox.Store.Transaction")]
public sealed class Transaction : IDisposable
{
    private Transaction() { }

    public static ValueTask<Transaction> BeginAsync() { return new ValueTask<Transaction>(new Transaction()); }
    public ValueTask CommitAsync() { return new ValueTask(); }
    public void Rollback() { }
    /// <summary>
    /// 仅用于测试，模拟事务所在的节点crash
    /// </summary>
    public void Abort() { }
    public void Dispose() { }
}

/// <summary>
/// 系统默认存储
/// </summary>
[RealType("appbox.Store.EntityStore")]
public static class EntityStore
{
    public static ValueTask SaveAsync(sys.SysEntityBase entity) { return new ValueTask(); }
    public static ValueTask SaveAsync(sys.SysEntityBase entity, Transaction txn) { return new ValueTask(); }
    public static ValueTask DeleteAsync(sys.SysEntityBase entity) { return new ValueTask(); }
    public static ValueTask DeleteAsync(sys.SysEntityBase entity, Transaction txn) { return new ValueTask(); }
    [InvocationInterceptor("DeleteEntity")]
    public static ValueTask DeleteAsync<T>(Guid id) where T : sys.SysEntityBase { return new ValueTask(); }
    [InvocationInterceptor("DeleteEntity")]
    public static ValueTask DeleteAsync<T>(Guid id, Transaction txn) where T : sys.SysEntityBase { return new ValueTask(); }
    [InvocationInterceptor("LoadEntity")]
    public static ValueTask<T> LoadAsync<T>(Guid id) where T : sys.SysEntityBase { throw new Exception(); }
    [InvocationInterceptor("LoadEntitySet")]
    public static ValueTask<EntityList<TResult>> LoadEntitySetAsync<TSource, TResult>(Guid id,
        Func<TSource, EntityList<TResult>> entitySet) where TSource : sys.SysEntityBase where TResult : sys.SysEntityBase
    {
        throw new Exception();
    }
}

public interface IIncluder<out T> { }

public interface IIncludable<out TEntity, out TProperty> : IIncluder<TEntity> { }

public static class IIncluderExts
{
    [InvocationInterceptor("Include")]
    public static IIncludable<TEntity, TProperty> ThenInclude<TEntity, TPreviousProperty, TProperty>(
        this IIncludable<TEntity, TPreviousProperty> source,
        Func<TPreviousProperty, TProperty> navigationPropertyPath)
        where TEntity : class
    { return null; }

    [InvocationInterceptor("Include")]
    public static IIncludable<TEntity, TProperty> ThenInclude<TEntity, TPreviousProperty, TProperty>(
        this IIncludable<TEntity, IEnumerable<TPreviousProperty>> source,
        Func<TPreviousProperty, TProperty> navigationPropertyPath)
        where TEntity : class
    { return null; }

    [InvocationInterceptor("Include")]
    public static IIncludable<TEntity, TProperty> Include<TEntity, TProperty>(
        this IIncluder<TEntity> source,
        Func<TEntity, TProperty> navigationPropertyPath)
        where TEntity : class
    { return null; }
}
#endregion

#region ====Other Store====
/// <summary>
/// 第三方关系数据库存储
/// </summary>
public sealed class SqlStore
{
    private SqlStore() { }

    public static SqlStore Default => null;

    public Task<DbConnection> OpenConnectionAsync() { return null; }

    public Task SaveAsync(SqlEntityBase entity, DbTransaction txn = null) { return null; }

    public Task<int> DeleteAsync(SqlEntityBase entity, DbTransaction txn = null) { return null; }

    //public Task LoadAsync<T>(T entity, Action<ISqlIncluder<T>> includer) { return null; }

    public Task ExecCommandAsync<TSource>(SqlUpdateCommand<TSource> cmd) where TSource : SqlEntityBase { return null; }

    public Task ExecCommandAsync<TSource>(SqlDeleteCommand<TSource> cmd) where TSource : SqlEntityBase { return null; }
}

public sealed class CqlStore
{
    private CqlStore() { }
}
#endregion

#region ====Entity Query====
[RealType("appbox.Store.PartitionPredicates")]
public sealed class PartitionPredicates<T> where T : sys.SysEntityBase
{
    private PartitionPredicates() { }
    [InvocationInterceptor("PartitionPredicate")]
    public void Equal<TResult>(Func<T, TResult> key, TResult value) { }
}

[RealType("appbox.Store.IndexPredicates")]
public sealed class IndexPredicates<T> where T : IEntityIndex<sys.SysEntityBase>
{
    private IndexPredicates() { }
    [InvocationInterceptor("IndexPredicate")]
    public void Equal<TResult>(Func<T, TResult> pk, TResult value) { }
}

[RealType("appbox.Store.TableScan")]
[GenericCreate()]
[QueriableClass(true)]
public sealed class TableScan<T> : IIncluder<T> where T : sys.SysEntityBase
{
    /// <summary>
    /// Partition key predicates for scan from which partitions, only for partitioned table.
    /// </summary>
    public PartitionPredicates<T> Partitions { get; }

    public TableScan<T> Skip(uint skip) { return this; }
    public TableScan<T> Take(uint take) { return this; }

    [QueryMethod]
    public TableScan<T> Filter(Func<T, bool> condition) { return this; }

    public ValueTask<IList<T>> ToListAsync() { throw new Exception(); }

    [InvocationInterceptor("ToTreeList")]
    public ValueTask<EntityList<T>> ToTreeListAsync<TResult>(Func<T, EntityList<TResult>> entitySet)
        where TResult : sys.SysEntityBase
    { throw new Exception(); }
}

[RealType("appbox.Store.IndexScan")]
[GenericCreate()]
public sealed class IndexScan<TEntity, TIndex> where TEntity : sys.SysEntityBase where TIndex : IEntityIndex<TEntity>
{
    /// <summary>
    /// Partition key predicates for scan from which partitions, only for partitioned local indexes.
    /// </summary>
    public PartitionPredicates<TEntity> Partitions { get; }
    /// <summary>
    /// Index key predicates
    /// </summary>
    public IndexPredicates<TIndex> Keys { get; }

    public IndexScan<TEntity, TIndex> Skip(uint skip) { return this; }
    public IndexScan<TEntity, TIndex> Take(uint take) { return this; }

    public ValueTask<IList<TEntity>> ToListAsync() { throw new Exception(); }
}
#endregion

#region ====Sql Query====
[RealType("appbox.Store.ISqlQueryJoin")]
public interface ISqlQueryJoin<TSource> //where TSource : class
{ }

public interface ISqlIncluder<out T> { }

public interface ISqlIncludable<out TEntity, out TProperty> : ISqlIncluder<TEntity> { }

[QueriableClass(false)]
public static class ISqlIncluderExts
{
    [QueryMethod()]
    public static ISqlIncludable<TEntity, TProperty> ThenInclude<TEntity, TPreviousProperty, TProperty>(
        this ISqlIncludable<TEntity, TPreviousProperty> source,
        Func<TPreviousProperty, TProperty> navigationPropertyPath)
        where TEntity : class
    { return null; }

    [QueryMethod()]
    public static ISqlIncludable<TEntity, TProperty> ThenInclude<TEntity, TPreviousProperty, TProperty>(
        this ISqlIncludable<TEntity, IEnumerable<TPreviousProperty>> source,
        Func<TPreviousProperty, TProperty> navigationPropertyPath)
        where TEntity : class
    { return null; }

    [QueryMethod()]
    public static ISqlIncludable<TEntity, TProperty> Include<TEntity, TProperty>(
        this ISqlIncluder<TEntity> source,
        Func<TEntity, TProperty> navigationPropertyPath)
        where TEntity : class
    { return null; }
}

[RealType("appbox.Store.SqlQuery")]
[GenericCreate()]
[QueriableClass(false)]
public class SqlQuery<TSource> : ISqlQueryJoin<TSource>, ISqlIncluder<TSource> where TSource : SqlEntityBase
{
    #region ----Properties----
    /// <summary>
    /// 是否忽略实体查询的过滤器
    /// </summary>
    //public bool IgnoreQueryFilter
    //{ get { return false; } }

    public bool Distinct { get; set; }
    #endregion

    #region ----OrderBy Methods----
    /// <summary>
    /// 正排序
    /// </summary>
    [QueryMethod()]
    public SqlQuery<TSource> OrderBy<TResult>(Func<TSource, TResult> selector)
    { return this; }

    /// <summary>
    /// 倒排序
    /// </summary>
    [QueryMethod()]
    public SqlQuery<TSource> OrderByDesc<TResult>(Func<TSource, TResult> selector)
    { return this; }
    #endregion

    #region ----Where Methods----
    [QueryMethod()]
    public SqlQuery<TSource> Where(Func<TSource, bool> condition) { return this; }

    [QueryMethod()]
    public SqlQuery<TSource> Where<TJoin>(ISqlQueryJoin<TJoin> j, Func<TSource, TJoin, bool> condition) where TJoin : class
    { return this; }

    [QueryMethod()]
    public SqlQuery<TSource> Where<TJoin1, TJoin2>(ISqlQueryJoin<TJoin1> j1, ISqlQueryJoin<TJoin2> j2,
        Func<TSource, TJoin1, TJoin2, bool> condition)
        where TJoin1 : class
        where TJoin2 : class
    { return this; }

    [QueryMethod()]
    public SqlQuery<TSource> AndWhere(Func<TSource, bool> condition) { return this; }

    [QueryMethod()]
    public SqlQuery<TSource> AndWhere<TJoin>(ISqlQueryJoin<TJoin> j, Func<TSource, TJoin, bool> condition) where TJoin : class
    { return this; }

    [QueryMethod()]
    public SqlQuery<TSource> AndWhere<TJoin1, TJoin2>(ISqlQueryJoin<TJoin1> j1, ISqlQueryJoin<TJoin2> j2,
        Func<TSource, TJoin1, TJoin2, bool> condition)
        where TJoin1 : class
        where TJoin2 : class
    { return this; }

    [QueryMethod()]
    public SqlQuery<TSource> OrWhere(Func<TSource, bool> condition) { return this; }

    [QueryMethod()]
    public SqlQuery<TSource> OrWhere<TJoin>(ISqlQueryJoin<TJoin> j, Func<TSource, TJoin, bool> condition) where TJoin : class
    { return this; }

    [QueryMethod()]
    public SqlQuery<TSource> OrWhere<TJoin1, TJoin2>(ISqlQueryJoin<TJoin1> j1, ISqlQueryJoin<TJoin2> j2,
        Func<TSource, TJoin1, TJoin2, bool> condition)
        where TJoin1 : class
        where TJoin2 : class
    { return this; }
    #endregion

    #region ----Top & Page Methods----
    public SqlQuery<TSource> Skip(int rows) { return this; }

    public SqlQuery<TSource> Take(int rows) { return this; }

    public SqlQuery<TSource> Page(int pageSize, int pageIndex) { return this; } //TODO: remove it
    #endregion

    #region ----Join Methods-----
    [QueryMethod()]
    public ISqlQueryJoin<TJoin> LeftJoin<TJoin>(ISqlQueryJoin<TJoin> join, Func<TSource, TJoin, bool> condition)
    { return join; }
    #endregion

    #region ----ToXXXX Methods----
    public Task<int> CountAsync() { return null; }

    //[QueryMethod()]
    //public TResult ToScalar<TResult>(Func<TSource, TResult> selector) { return default(TResult); }

    public Task<TSource> ToSingleAsync() { return null; }

    public Task<IList<TSource>> ToListAsync() { return null; }

    [QueryMethod()]
    public Task<IList<TSource>> ToTreeListAsync<TResult>(Func<TSource, TResult> selector) { return null; }

    [QueryMethod()]
    public Task<IList<TResult>> ToListAsync<TResult>(Func<TSource, TResult> selector)
    { return null; }

    [QueryMethod()]
    public Task<IList<TResult>> ToListAsync<TJoin, TResult>(ISqlQueryJoin<TJoin> join,
        Func<TSource, TJoin, TResult> selector)
    { return null; }

    [QueryMethod()]
    public Task<IList<TResult>> ToListAsync<TJoin1, TJoin2, TResult>(ISqlQueryJoin<TJoin1> join1,
        ISqlQueryJoin<TJoin2> join2, Func<TSource, TJoin1, TJoin2, TResult> selector)
    { return null; }

    [QueryMethod()]
    public Task<IList<TResult>> ToListAsync<TJoin1, TJoin2, TJoin3, TResult>(ISqlQueryJoin<TJoin1> join1,
        ISqlQueryJoin<TJoin2> join2, ISqlQueryJoin<TJoin3> join3,
        Func<TSource, TJoin1, TJoin2, TJoin3, TResult> selector)
    { return null; }
    #endregion

    #region ----AsXXXX Methods----
    [QueryMethod()]
    public SqlSubQuery<TResult> AsSubQuery<TResult>(Func<TSource, TResult> selector)
    { return null; }
    #endregion

    #region ----GroupBy Methods----
    [QueryMethod()]
    public SqlQuery<TSource> GroupBy<TResult>(Func<TSource, TResult> keySelector)
    { return this; }

    [QueryMethod()]
    public SqlQuery<TSource> Having(Func<TSource, bool> condition) { return this; }
    #endregion
}

[RealType("appbox.Store.SqlQueryJoin")]
[GenericCreate()]
[QueriableClass(false)]
public class SqlQueryJoin<TSource> : ISqlQueryJoin<TSource> where TSource : SqlEntityBase
{

    [QueryMethod()]
    public ISqlQueryJoin<TJoin> LeftJoin<TJoin>(ISqlQueryJoin<TJoin> join, Func<TSource, TJoin, bool> condition)
    { return join; }

}

[RealType("appbox.Store.SqlSubQuery")]
public class SqlSubQuery<TSource> : ISqlQueryJoin<TSource>, /*伪*/IEnumerable<TSource>
{
    private SqlSubQuery() { }
}

[RealType("appbox.Store.SqlUpdateCommand")]
[GenericCreate()]
[QueriableClass(false)]
public sealed class SqlUpdateCommand<TSource> where TSource : SqlEntityBase
{
    /// <summary>
    /// 输出的值集合
    /// </summary>
    public object[] OutputValues { get { return null; } } //TODO:同步改为支持多条记录返回字段

    [QueryMethod()]
    public SqlUpdateCommand<TSource> Where(Func<TSource, bool> condition) { return this; }

    [QueryMethod()]
    public SqlUpdateCommand<TSource> Where<TJoin>(ISqlQueryJoin<TJoin> j, Func<TSource, TJoin, bool> condition) where TJoin : class
    { return this; }

    [QueryMethod()]
    public SqlUpdateCommand<TSource> Where<TJoin1, TJoin2>(ISqlQueryJoin<TJoin1> j1, ISqlQueryJoin<TJoin2> j2,
        Func<TSource, TJoin1, TJoin2, bool> condition)
        where TJoin1 : class
        where TJoin2 : class
    { return this; }

    [QueryMethod()]
    public SqlUpdateCommand<TSource> Update(Action<TSource> setter) { return this; }

    /// <summary>
    /// 输出
    /// </summary>
    [QueryMethod()]
    public SqlUpdateCommand<TSource> Output<TResult>(Func<TSource, TResult> selector) { return this; }
}

[RealType("appbox.Store.SqlDeleteCommand")]
[GenericCreate()]
[QueriableClass(false)]
public sealed class SqlDeleteCommand<TSource> where TSource : SqlEntityBase
{
    [QueryMethod()]
    public SqlDeleteCommand<TSource> Where(Func<TSource, bool> condition) { return this; }

    [QueryMethod()]
    public SqlDeleteCommand<TSource> Where<TJoin>(ISqlQueryJoin<TJoin> j, Func<TSource, TJoin, bool> condition) where TJoin : class
    { return this; }

    [QueryMethod()]
    public SqlDeleteCommand<TSource> Where<TJoin1, TJoin2>(ISqlQueryJoin<TJoin1> j1, ISqlQueryJoin<TJoin2> j2,
        Func<TSource, TJoin1, TJoin2, bool> condition)
        where TJoin1 : class
        where TJoin2 : class
    { return this; }
}
#endregion

#region ====Cql Query====
[RealType("appbox.Store.IRow")]
public interface IRow
{
    [InvocationInterceptor("GenericTypeToArg")]
    T FetchToEntity<T>() where T : ITableOrView;

    T Fetch<T>(Func<IRow, T> selector);

    T GetValue<T>(string name);
}

[RealType("appbox.Store.IRowSet")]
public interface IRowSet : IEnumerable<IRow>
{
    [InvocationInterceptor("GenericTypeToArg")]
    List<T> ToEntityList<T>() where T : ITableOrView;

    List<T> ToList<T>(Func<IRow, T> selector);

    T ToScalar<T>();
}

[RealType("appbox.Store.CqlQuery")]
[GenericCreate()]
public struct CqlQuery<TSource> where TSource : ICqlEntityOrView
{

    public bool AllowFiltering;
    public int Limit;

    public long Token(object obj) { return 0; }
    public long Token(object obj1, object obj2) { return 0; }
    public long Token(object obj1, object obj2, object obj3) { return 0; }

    [InvocationInterceptor("CqlWhere")]
    public void Equals<TValue>(Func<TSource, TValue> selector, TValue value) { }
    [InvocationInterceptor("CqlWhere")]
    public void GreaterThan<TValue>(Func<TSource, TValue> selector, TValue value) { }
    [InvocationInterceptor("CqlWhere")]
    public void GreaterThanOrEqual<TValue>(Func<TSource, TValue> selector, TValue value) { }
    [InvocationInterceptor("CqlWhere")]
    public void LessThan<TValue>(Func<TSource, TValue> selector, TValue value) { }
    [InvocationInterceptor("CqlWhere")]
    public void LessThanOrEqual<TValue>(Func<TSource, TValue> selector, TValue value) { }
    [InvocationInterceptor("CqlWhere")]
    public void Like(Func<TSource, string> selector, string value) { }
    [InvocationInterceptor("CqlWhere")]
    public void In<TValue>(Func<TSource, TValue> selector, (TValue, TValue) values) { }
    [InvocationInterceptor("CqlWhere")]
    public void In<TValue>(Func<TSource, TValue> selector, (TValue, TValue, TValue) values) { }
    [InvocationInterceptor("CqlWhere")]
    public void In<TValue>(Func<TSource, TValue> selector, IEnumerable<TValue> values) { }

    public Task<IList<TSource>> ToListAsync() { return null; }

    [InvocationInterceptor("CqlToEntityList")]
    public Task<IList<TSource>> ToListByFilterAsync(Func<TSource, bool> filter, int skip = 0, int take = 0) { return null; }

    [InvocationInterceptor("CqlToList")]
    public Task<IList<TResult>> ToListAsync<TResult>(Func<TSource, TResult> selector) { return null; }
    [InvocationInterceptor("CqlToList")]
    public Task<IList<TResult>> ToListByFilterAsync<TResult>(Func<TSource, TResult> selector, Func<TResult, bool> filter, int skip = 0, int take = 0) { return null; }

    // public T ToScalar<T>() { return default(T); }
}
#endregion