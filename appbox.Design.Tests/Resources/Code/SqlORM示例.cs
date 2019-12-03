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
		/// 手动Join
		/// </summary>
		/// <returns></returns>
		public async Task<object> Query2()
		{
			var j = new SqlQueryJoin<Entities.City>();
			var q = new SqlQuery<Entities.Customer>();
			q.LeftJoin(j, (cus, city) => cus.CityCode == city.Code);
			q.Where(j, (cus, city) => city.Name == "无锡");
			return await q.ToListAsync(j, (cus, city) => new { cus.Code, cus.Name, CityName = city.Name });
		}
	}
}
