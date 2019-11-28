using System;
using System.Threading.Tasks;

namespace sys.ServiceLogic
{
    public class OrgUnitService
    {

        /// <summary>
        /// 获取组织结构树
        /// </summary>
        [InvokePermission(Permissions.Admin)]
        public async Task<EntityList<Entities.OrgUnit>> LoadTreeList()
        {
            var q = new TableScan<Entities.OrgUnit>();
            return await q.ToTreeListAsync(t => t.Childs);
        }

        [InvokePermission(Permissions.Admin)]
        public async Task<Entities.Enterprise> LoadEnterprise(Guid id)
        {
            return await EntityStore.LoadAsync<Entities.Enterprise>(id);
        }

        [InvokePermission(Permissions.Admin)]
        public async Task<Entities.Workgroup> LoadWorkgroup(Guid id)
        {
            return await EntityStore.LoadAsync<Entities.Workgroup>(id);
        }

        [InvokePermission(Permissions.Admin)]
        public async Task<Entities.Emploee> LoadEmploee(Guid id)
        {
            return await EntityStore.LoadAsync<Entities.Emploee>(id);
        }

        /// <summary>
        /// 保存公司信息
        /// </summary>
        [InvokePermission(Permissions.Admin)]
        public async Task SaveEnterprise(Entities.Enterprise ent, Guid ouid)
        {
            var ou = await EntityStore.LoadAsync<Entities.OrgUnit>(ouid);
            ou.Name = ent.Name;

            using (var txn = await Transaction.BeginAsync())
            {
                await EntityStore.SaveAsync(ent, txn);
                await EntityStore.SaveAsync(ou, txn);
                await txn.CommitAsync();
            }
        }

        /// <summary>
        /// 保存工作组信息
        /// </summary>
        [InvokePermission(Permissions.Admin)]
        public async Task SaveWorkgroup(Entities.Workgroup group, Guid ouid)
        {
            var ou = await EntityStore.LoadAsync<Entities.OrgUnit>(ouid);
            ou.Name = group.Name;

            using (var txn = await Transaction.BeginAsync())
            {
                await EntityStore.SaveAsync(group, txn);
                await EntityStore.SaveAsync(ou, txn);
                await txn.CommitAsync();
            }
        }

        /// <summary>
        /// 保存员工信息
        /// </summary>
        [InvokePermission(Permissions.Admin)]
        public async Task SaveEmploee(Entities.Emploee emp, Guid ouid)
        {
            //TODO:同步关联至相同员工的组织单元的名称
            var ou = await EntityStore.LoadAsync<Entities.OrgUnit>(ouid);
            ou.Name = emp.Name;

            using (var txn = await Transaction.BeginAsync())
            {
                await EntityStore.SaveAsync(emp, txn);
                await EntityStore.SaveAsync(ou, txn);
                await txn.CommitAsync();
            }
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
            var parent = await EntityStore.LoadAsync<Entities.OrgUnit>(parentID);
            if (parent == null)
                throw new Exception("新建员工时无法获取上级节点");
            if (parent.BaseType == Entities.Emploee.TypeId)
                throw new Exception("无法在员工节点新建子节点");

            var emp = new Entities.Emploee();
            emp.Name = "新员工";
            var ou = new Entities.OrgUnit();
            ou.Name = emp.Name;
            ou.BaseType = Entities.Emploee.TypeId;
            ou.BaseId = emp.Id;
            ou.ParentId = parentID;

            //保存并返回
            using (var txn = await Transaction.BeginAsync())
            {
                await EntityStore.SaveAsync(emp, txn);
                await EntityStore.SaveAsync(ou, txn);
                await txn.CommitAsync();
            }
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
            var parent = await EntityStore.LoadAsync<Entities.OrgUnit>(parentID);
            if (parent == null)
                throw new Exception("新建组时无法获取上级节点");
            if (parent.BaseType == Entities.Emploee.TypeId)
                throw new Exception("无法在员工节点新建子节点");

            var workgroup = new Entities.Workgroup();
            workgroup.Name = "新工作组";
            var ou = new Entities.OrgUnit();
            ou.Name = workgroup.Name;
            ou.BaseType = Entities.Workgroup.TypeId;
            ou.BaseId = workgroup.Id;
            ou.ParentId = parentID;

            //保存并返回
            using (var txn = await Transaction.BeginAsync())
            {
                await EntityStore.SaveAsync(workgroup, txn);
                await EntityStore.SaveAsync(ou, txn);
                await txn.CommitAsync();
            }
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
            await EntityStore.SaveAsync(emp);
        }

        /// <summary>
        /// 删除员工用户
        /// </summary>
        [InvokePermission(Permissions.Admin)]
        public async Task DeleteEmploeeUser(Entities.Emploee emp)
        {
            emp.Account = null;
            emp.Password = null;
            await EntityStore.SaveAsync(emp);
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
            await EntityStore.SaveAsync(emp);
        }

        //[InvokePermission(Permissions.Admin)]
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

            var ou = await EntityStore.LoadAsync<Entities.OrgUnit>(ouid);
            if (ou == null)
                throw new Exception("找不到待删除的组织单元");
            if (ou.ParentId == null)
                throw new Exception("不能删除根节点");

            //先删除组织单元
            await EntityStore.DeleteAsync(ou);
            //再尝试删除orgunit的基类实例
            try
            {
                if (ou.BaseType == Entities.Emploee.TypeId)
                    await EntityStore.DeleteAsync<Entities.Emploee>(ou.BaseId);
                else if (ou.BaseType == Entities.Workgroup.TypeId)
                    await EntityStore.DeleteAsync<Entities.Workgroup>(ou.BaseId);
                else if (ou.BaseType == Entities.Enterprise.TypeId)
                    await EntityStore.DeleteAsync<Entities.Enterprise>(ou.BaseId);
            }
            catch (Exception) { /*do nothing*/ }
        }
    }
}