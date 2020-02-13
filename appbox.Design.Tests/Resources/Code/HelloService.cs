using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace sys.ServiceLogic
{
    public class HelloService
    {
		//public async Task<EntityList<Entities.Emploee>> SayHello(string name, int[] age, Guid id, DateTime birthday)
		//{
		//    //var vs = new Entities.Order();
		//    //vs.CreateById = Guid.Empty;
		//    //await EntityStore.SaveAsync(vs);
		//    return null;
		//}

		//[InvokePermission(Permissions.Admin || Permissions.Developer)]
		//public async Task SayHello(Entities.Emploee emp)
		//{
		//    await EntityStore.SaveAsync(emp);
		//}

		//public async Task<EntityBase> LoadEmploee(Guid id)
		//{
		//    return await EntityStore.LoadAsync<Entities.Emploee>(id);
		//}

		//public Task<bool> TypeIdTest()
		//{
		//    ulong baseType = 0;
		//    var res = baseType == Entities.Emploee.TypeId;
		//    return Task.FromResult(res);
		//}

		//public async Task DeleteTest()
		//{
		//    await EntityStore.DeleteAsync<Entities.Emploee>(Guid.Empty);
		//}

		//public async Task<object> Test()
		//{
		//    var q = new TableScan<Entities.OrgUnit>();
		//    q.Include(o => o.Parent.Name);
		//    return await q.ToListAsync();
		//}

		//public async Task<object> Test()
		//{
		//    var id = Guid.Empty;
		//    var childs = await EntityStore.LoadEntitySetAsync<Entities.OrgUnit, Entities.OrgUnit>(id, t => t.Childs);
		//    return childs;
		//}

		//public async Task<object> Test()
		//{
		//    var q = new TableScan<Entities.VehicleState>();
		//    q.Partitions.Equal(t => t.VehicleId, 1);
		//    //q.Partitions.Equal(t => t.CreateTime, new DateTime(2019, 1, 1));
		//    return await q.ToListAsync();
		//}

		//public async Task<object> Test()
		//{
		//	var q = new TableScan<Entities.VehicleState>();
		//	q.Filter(t => t.Lng > 12f && t.Lat > 20f);
		//	return await q.ToListAsync();
		//}

		//public async Task<object> Test(DateTime date)
		//{
		//    var q = new IndexScan<Entities.Emploee, Entities.Emploee.UI_Account_Password>();
		//    q.Keys.Equal(t => t.Account, "Admin");
		//    return await q.ToListAsync();
		//}

		//      public async Task Test()
		//{
		//	var obj = new Entities.City(214000);
		//	obj.Name = "Wuxi";
		//	await DataStore.DemoDB.SaveAsync(obj);
		//}

		//      public async Task<object> Test()
		//{
		//	var q = new SqlQuery<Entities.City>();
		//	q.Where(t => t.Code > 1 && t.Code < 10);
		//	return await q.ToListAsync();
		//}

		//      public async Task Test()
		//{
		//	var cmd = new SqlUpdateCommand<Entities.City>();
		//	cmd.Update(t => t.Code = t.Code + 1);
		//	cmd.Output(t => t.Code);
		//	cmd.Where(t => t.Code == 1);
		//	await DataStore.DemoDB.ExecCommandAsync(cmd);
		//}

		//      public async Task<object> Test()
		//{
		//	return await Entities.City.LoadAsync(214000);
		//}

		//public async Task<object> SayHello()
		//{
		//	var q = new SqlQuery<Entities.Order>();
		//	q.Include(order => order.Customer)
		//		.ThenInclude(customer => customer.City);
		//	return await q.ToListAsync();
		//}

        //调用其他服务
        public async Task<object> CallService()
		{
			//await Services.TestService.Test("Rick");
			//var res1 = await sys.Services.TestService.Test1("hello");
			//var res2 = await sys.Services.TestService.Test2(128);
			//var res3 = await sys.Services.TestService.Test3(DateTime.Now);
			//return $"{res1} {res2} {res3}";
			var res = await sys.Services.TestService.Test4();
			return res[0].Name;
		}

	}
}
