using System;

namespace appbox.Design.Tests.Resources.Code
{
	public class SqlORM示例
	{
		/// <summary>
		/// EntityRef自动Join
		/// </summary>
		/// <returns></returns>
		public async Task<object> Query1()
		{
			var q = new SqlQuery<Entities.Customer>();
			q.Where(t => t.City.Name == "无锡");
			return await q.ToListAsync(t => new { t.Code, t.Name, CityName = t.City.Name });
		}

		/// <summary>
		/// 手动Join，适用于没有导航属性的实体
		/// </summary>
		/// <returns></returns>
		public async Task<object> Query2()
		{
			var q = new SqlQuery<Entities.Customer>();
			var j = new SqlQueryJoin<Entities.City>();
			q.LeftJoin(j, (cus, city) => cus.CityCode == city.Code);
			q.Where(j, (cus, city) => city.Name == "无锡");
			return await q.ToListAsync(j, (cus, city) => new { cus.Code, cus.Name, CityName = city.Name });
		}

		/// <summary>
		/// Eager loading
		/// </summary>
		/// <remarks>wrk -c100 -t4 -d5s -s post.lua http://10.211.55.3:5000/api/Invoke 7500/秒</remarks>
		public async Task<object> QueryWithInclude()
		{
			var q = new SqlQuery<Entities.Order>();
			q.Include(order => order.Customer)
				.ThenInclude(customer => customer.City);
			return await q.ToListAsync();
		}

		public async Task<object> QueryWithInclude2()
		{
			var q = new SqlQuery<Entities.Order>();
			q.Include(order => order.Customer)
				.ThenInclude(customer => customer.City)
			 .Include(order => order.Items)
				.ThenInclude(item => item.Product);
			return await q.ToSingleAsync();
		}

		/// <summary>
		/// Explicit loading
		/// </summary>
		public async Task<object> Test()
		{
			var order = await Entities.Order.LoadAsync(1);
			// await order.Include(o => o.Customer)
			//                 .ThenInclude(c => c.City).LoadAsync();
			//await DataStore.DemoDB.LoadAsync(order, t => t.Include(o => o.Customer)
			//                                                    .ThenInclude(o => o.City));
			return order;
		}

        /// <summary>
		/// GroupBy Query
		/// </summary>
		public async Task<object> Group()
		{
			var q = new SqlQuery<Entities.OrderItem>();
			q.GroupBy(t => t.ProductCode)
				.Having(t => DbFuncs.Sum(t.Quantity) > 0);
			return await q.ToListAsync(t => new { t.ProductCode, Amount = DbFuncs.Sum(t.Quantity) });
		}

        /// <summary>
		/// SubQuery
		/// </summary>
		public async Task<object> SubQuery()
		{
			var q = new SqlQuery<Entities.OrderItem>();
			var sq = new SqlQuery<Entities.Product>();

			q.Where(t => t.ProductCode.In(
				sq.Where(p => p.Name.Contains("15")).AsSubQuery(p => p.Code)
				));
			return await q.ToListAsync();
		}
	}
}
