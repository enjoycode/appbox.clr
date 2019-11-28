using System;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;
using Xunit;

namespace appbox.Core.Tests
{
    class EntityBase
    {
        public Guid Id { get; private set; }
        public DateTime CreateTime { get; private set; }
    }

    interface IEntityIndex<out T> where T : EntityBase
    {
        //IIndexPredicates<IIndex<T>> Keys { get; }
    }

    //interface IIndexPredicates<out T> where T : IIndex<EntityBase>
    //{
    //    void Equal<TResult>(Func<T, TResult> key, TResult value);
    //}

    class IndexPredicates<T> where T : IEntityIndex<EntityBase>
    {
        public void Equal<TResult>(Func<T, TResult> pk, TResult value) { }
    }

    class PartitionPredicates<T> where T : EntityBase
    {
        public void Equal<TResult>(Func<T, TResult> pk, TResult value) { }
    }

    class TableScan<T> where T : EntityBase
    {

        public PartitionPredicates<T> Partitions { get; }

        //public void PartitionEqual<TResult>(Func<T, TResult> pk, TResult value) { }

        //public void PartitionGreaterThan<TResult>(Func<T, TResult> pk, TResult value) { }

        //public void KeyInRange<TResult>(Func<T, TResult> key, TResult begin, TResult end) { }

        public EntityList<T> ToTreeList<TResult>(Func<T, EntityList<TResult>> entitySet) where TResult : EntityBase
        {
            return null;
        }
    }

    //class IndexScan<TEntity, TIndex> where  TEntity: EntityBase where TIndex : IndexBase<TEntity>
    //{

    //}

    //class IndexScan<TIndex> where TIndex: IIndex<EntityBase>
    //{
    //    //public PartitionPredicates<TIndex> Partitons { get; }
    //    public IIndexPredicates<TIndex> Keys { get; }
    //}

    class IndexScan<TEntity, TIndex> where TEntity : EntityBase where TIndex : IEntityIndex<TEntity>
    {
        public PartitionPredicates<TEntity> Partitions { get; }
        //public IIndexPredicates<IIndex<T>> Keys { get; }
        public IndexPredicates<TIndex> Keys { get; }
    }


    interface IIncluder<out T> { }

    interface IIncludable<out TEntity, out TProperty> : IIncluder<TEntity> //IQueryable<TEntity> 
    { }

    static class Exts
    {
        public static IIncludable<TEntity, TProperty> ThenInclude<TEntity, TPreviousProperty, TProperty>(
            this IIncludable<TEntity, TPreviousProperty> source,
            Expression<Func<TPreviousProperty, TProperty>> navigationPropertyPath)
            where TEntity : class
        {
            return null;
        }

        public static IIncludable<TEntity, TProperty> ThenInclude<TEntity, TPreviousProperty, TProperty>(
            this IIncludable<TEntity, IEnumerable<TPreviousProperty>> source,
            Expression<Func<TPreviousProperty, TProperty>> navigationPropertyPath)
            where TEntity : class
        {
            return null;
        }

        public static IIncludable<TEntity, TProperty> Include<TEntity, TProperty>(
            this IIncluder<TEntity> source,
            Expression<Func<TEntity, TProperty>> navigationPropertyPath)
            where TEntity : class
        {
            return null;
        }
    }

    class EntityList<T> : List<T> where T : EntityBase { }

    class Includer<T> : IIncluder<T> where T : class //EntityBase
    {
    }

    static class EntityStore
    {
        public static TEntity Fetch<TEntity>(Guid id, Action<IIncluder<TEntity>> includes) where TEntity : EntityBase
        {
            return null;
        }

        public static EntityList<TResult> LoadEntitySet<TSource, TResult>(Func<TSource, EntityList<TResult>> entitySet)
            where TSource : EntityBase
            where TResult : EntityBase
        {
            throw new Exception();
        }
    }

    class Region : EntityBase
    {
        public string Name { get; set; }
    }

    class Customer : EntityBase
    {
        public string Name { get; set; }
        public Region Region { get; set; }
        public string Address { get; set; }
        public int Level { get; set; }

        public interface UI_Name : IEntityIndex<Customer>
        {
            /// <summary>
            /// [0] Name, OrderByASC
            /// </summary>
            string Name { get; }
        }

        //public static class Indexes
        //{
        //    public static readonly IIndex<Customer> UIName;
        //}
    }

    class ProductCategory : EntityBase
    {
        public string Name { get; set; }
    }

    class Product : EntityBase
    {
        public string Name { get; set; }
        public ProductCategory Category { get; set; }

        public interface UI_Name : IEntityIndex<Product>
        {
            string Name { get; }
        }
    }

    class Order : EntityBase
    {
        public Customer Customer { get; set; }
        public EntityList<OrderDetail> Details { get; }
    }

    class OrderDetail : EntityBase
    {
        public Product Product { get; set; }
        public int Amount { get; set; }
    }

    public class OrmNavigatePropertyTest
    {
        [Fact]
        public void Test()
        {
            var q = new Includer<Order>();
            q.Include(o => o.Customer).ThenInclude(c => c.Region)
                .Include(o => o.Customer.Region).ThenInclude(r => r.Name) //无效
                .Include(o => o.Customer.Level)
                .Include(o => o.Customer).ThenInclude(c => c.Region.Name)
                .Include(o => o.Details).ThenInclude(d => d.Product)
                .Include(o => o.Details[0].Product) //无效
                .Include(o => o.Details).ThenInclude(d => d.Product.Name)
                .Include(o => o.Details).ThenInclude(d => d.Product.Category.Name)
                ;
        }

        [Fact]
        public void Test2()
        {
            var q = new TableScan<Order>();
            q.Partitions.Equal(o => o.CreateTime, new DateTime(2012, 2, 2));
            //q.PartitionEqual(o => o.CreateTime, new DateTime(2012, 2, 2));
            var list = q.ToTreeList(o => o.Details);
        }

        [Fact]
        public void Test3()
        {
            var res = EntityStore.LoadEntitySet<Order, OrderDetail>(t => t.Details);
        }

        [Fact]
        public void Test4()
        {
            //方案一
            var q = new IndexScan<Customer, Customer.UI_Name>();
            q.Partitions.Equal(t => t.Level, 1);
            q.Keys.Equal(t => t.Name, "aa");

            //方案二,删除索引无法查找引用项
            //var q = new IndexScan<Customer>(Customer.Indexes.UIName);
            //q.Partitions.Equal(t => t.Level, 1);
            //q.Keys.Equal(t => t.Name, "aa");
            

        }
    }
}
