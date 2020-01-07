using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace sys.ServiceLogic
{
	public class OrgUnitService
	{

		/// <summary>
		/// 获取组织结构树
		/// </summary>
		[InvokePermission(Permissions.Admin)]
		public async Task<IList<Entities.OrgUnit>> LoadTreeList()
		{
			var q = new SqlQuery<Entities.OrgUnit>();
			return await q.ToTreeListAsync(t => t.Childs);
		}

		[InvokePermission(Permissions.Admin)]
		public async Task<Entities.Enterprise> LoadEnterprise(Guid id)
		{
			return await Entities.Enterprise.LoadAsync(id);
		}

		[InvokePermission(Permissions.Admin)]
		public async Task<Entities.Workgroup> LoadWorkgroup(Guid id)
		{
			return await Entities.Workgroup.LoadAsync(id);
		}

		[InvokePermission(Permissions.Admin)]
		public async Task<Entities.Emploee> LoadEmploee(Guid id)
		{
			return await Entities.Emploee.LoadAsync(id);
		}

		/// <summary>
		/// 保存公司信息
		/// </summary>
		[InvokePermission(Permissions.Admin)]
		public async Task SaveEnterprise(Entities.Enterprise ent, Guid ouid)
		{
			var ou = await Entities.OrgUnit.LoadAsync(ouid);
			bool nameChanged = ou.Name != ent.Name;
			if (nameChanged) ou.Name = ent.Name;

			using var conn = await SqlStore.Default.OpenConnectionAsync();
			using var txn = conn.BeginTransaction();
			await SqlStore.Default.SaveAsync(ent, txn);
			if (nameChanged)
				await SqlStore.Default.SaveAsync(ou, txn);
			txn.Commit();
		}

		/// <summary>
		/// 保存工作组信息
		/// </summary>
		[InvokePermission(Permissions.Admin)]
		public async Task SaveWorkgroup(Entities.Workgroup group, Guid ouid)
		{
			var ou = await Entities.OrgUnit.LoadAsync(ouid);
			bool nameChanged = ou.Name != group.Name;
			if (nameChanged) ou.Name = group.Name;

			using var conn = await SqlStore.Default.OpenConnectionAsync();
			using var txn = conn.BeginTransaction();
			await SqlStore.Default.SaveAsync(group, txn);
			if (nameChanged)
				await SqlStore.Default.SaveAsync(ou, txn);
			txn.Commit();
		}

		/// <summary>
		/// 保存员工信息
		/// </summary>
		[InvokePermission(Permissions.Admin)]
		public async Task SaveEmploee(Entities.Emploee emp, Guid ouid)
		{
			//TODO:同步关联至相同员工的组织单元的名称
			var ou = await Entities.OrgUnit.LoadAsync(ouid);
			bool nameChanged = ou.Name != emp.Name;
			if (nameChanged) ou.Name = emp.Name;

			using var conn = await SqlStore.Default.OpenConnectionAsync();
			using var txn = conn.BeginTransaction();
			await SqlStore.Default.SaveAsync(emp, txn);
			if (nameChanged)
				await SqlStore.Default.SaveAsync(ou, txn);
			txn.Commit();
		}

		/// <summary>
		/// 新建员工节点并保存后返回
		/// </summary>
		[InvokePermission(Permissions.Admin)]
		public async Task<Entities.OrgUnit> NewEmploee(Guid parentID)
		{
			if (parentID == Guid.Empty)
				throw new Exception("新建员工时必须指定上级节点");

			//获取上级 
			var parent = await Entities.OrgUnit.LoadAsync(parentID);
			if (parent == null)
				throw new Exception("新建员工时无法获取上级节点");
			if (parent.BaseType == Entities.Emploee.TypeId)
				throw new Exception("无法在员工节点新建子节点");

			var emp = new Entities.Emploee(Guid.NewGuid());
			emp.Name = "新员工";
			var ou = new Entities.OrgUnit(Guid.NewGuid());
			ou.Name = emp.Name;
			ou.BaseType = Entities.Emploee.TypeId;
			ou.BaseId = emp.Id;
			ou.ParentId = parentID;

			//保存并返回
			using var conn = await SqlStore.Default.OpenConnectionAsync();
			using var txn = conn.BeginTransaction();
			await SqlStore.Default.SaveAsync(emp, txn);
			await SqlStore.Default.SaveAsync(ou, txn);
			txn.Commit();
			return ou;
		}

		/// <summary>
		/// 新建工作组节点并返回
		/// </summary>
		[InvokePermission(Permissions.Admin)]
		public async Task<Entities.OrgUnit> NewWorkgroup(Guid parentID)
		{
			if (parentID == Guid.Empty)
				throw new Exception("新建组时必须指定上级节点");

			//获取上级 
			var parent = await Entities.OrgUnit.LoadAsync(parentID);
			if (parent == null)
				throw new Exception("新建组时无法获取上级节点");
			if (parent.BaseType == Entities.Emploee.TypeId)
				throw new Exception("无法在员工节点新建子节点");

			var workgroup = new Entities.Workgroup(Guid.NewGuid());
			workgroup.Name = "新工作组";
			var ou = new Entities.OrgUnit(Guid.NewGuid());
			ou.Name = workgroup.Name;
			ou.BaseType = Entities.Workgroup.TypeId;
			ou.BaseId = workgroup.Id;
			ou.ParentId = parentID;

			//保存并返回
			using var conn = await SqlStore.Default.OpenConnectionAsync();
			using var txn = conn.BeginTransaction();
			await SqlStore.Default.SaveAsync(workgroup, txn);
			await SqlStore.Default.SaveAsync(ou, txn);
			txn.Commit();
			return ou;
		}

		/// <summary>
		/// 新建员工用户
		/// </summary>
		[InvokePermission(Permissions.Admin)]
		public async Task NewEmploeeUser(Entities.Emploee emp, string account, string password)
		{
			if (string.IsNullOrEmpty(account) || string.IsNullOrEmpty(password))
				throw new ArgumentException("账户或密码不能为空");

			emp.Account = account;
			emp.Password = sys.Runtime.RuntimeContext.PasswordHasher.HashPassword(password);
			await SqlStore.Default.SaveAsync(emp);
		}

		/// <summary>
		/// 删除员工用户
		/// </summary>
		[InvokePermission(Permissions.Admin)]
		public async Task DeleteEmploeeUser(Entities.Emploee emp)
		{
			emp.Account = null;
			emp.Password = null;
			await SqlStore.Default.SaveAsync(emp);
		}

		/// <summary>
		/// 重置员工密码
		/// </summary>
		[InvokePermission(Permissions.Admin)]
		public async Task ResetPassword(Entities.Emploee emp, string password)
		{
			if (string.IsNullOrEmpty(password))
				throw new ArgumentException("密码不能为空");

			emp.Password = sys.Runtime.RuntimeContext.PasswordHasher.HashPassword(password);
			await SqlStore.Default.SaveAsync(emp);
		}

		[InvokePermission(Permissions.Admin)]
		public Task DragEnded(Entities.OrgUnit source, Entities.OrgUnit target, int dropPosition)
		{
			throw new NotImplementedException();
			//TODO: 验证是否允许

			// if (dropPosition == (int)sys.Enums.DropPosition.AsChildNode)
			// {
			//     if (source.ID == target.ID)
			//         throw new System.Exception("节点相同");
			//     sys.DataStore.Default.Save(target); //保存目标节点
			// }
			// else
			// {
			//     if (source.Parent != null)
			//     {
			//         sys.DataStore.Default.Save(source.Parent);
			//     }
			//     else
			//     {
			//         //TODO：需要保存重新设置SortNumber的其他根级对象
			//         sys.DataStore.Default.Save(source);
			//     }
			// }
		}

		/// <summary>
		/// 删除指定的组织单元节点，并尝试删除其基类实例
		/// </summary>
		[InvokePermission(Permissions.Admin)]
		public async Task DeleteOrgUnit(Guid ouid)
		{
			if (ouid == Guid.Empty)
				throw new Exception("待删除的组织单元标识为空");

			var ou = await Entities.OrgUnit.LoadAsync(ouid);
			if (ou == null)
				throw new Exception("找不到待删除的组织单元");
			if (ou.ParentId == null)
				throw new Exception("不能删除根节点");

			//先删除组织单元
			await SqlStore.Default.DeleteAsync(ou);
			//再尝试删除orgunit的基类实例
			try
			{
				if (ou.BaseType == Entities.Emploee.TypeId)
				{
					var cmd = new SqlDeleteCommand<Entities.Emploee>();
					cmd.Where(t => t.Id == ou.BaseId);
					await SqlStore.Default.ExecCommandAsync(cmd);
				}
				else if (ou.BaseType == Entities.Workgroup.TypeId)
				{
					var cmd = new SqlDeleteCommand<Entities.Workgroup>();
					cmd.Where(t => t.Id == ou.BaseId);
					await SqlStore.Default.ExecCommandAsync(cmd);
				}
				else if (ou.BaseType == Entities.Enterprise.TypeId)
				{
					var cmd = new SqlDeleteCommand<Entities.Enterprise>();
					cmd.Where(t => t.Id == ou.BaseId);
					await SqlStore.Default.ExecCommandAsync(cmd);
				}
			}
			catch (Exception) { /*do nothing*/ }
		}
	}
}