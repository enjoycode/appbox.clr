using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using appbox.Data;
using appbox.Store;
using appbox.Runtime;
using appbox.Server.Channel;

namespace appbox.Server.WebHost.Controllers
{

    [Route("api/[controller]/[action]")]
    public class LoginController : Controller
    {

        /// <summary>
        /// 内部用户通过用户名、密码登录
        /// </summary>
        /// <remarks>POST api/login</remarks>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] LoginRequire require)
        {
            if (string.IsNullOrEmpty(require.User) || string.IsNullOrEmpty(require.Password))
                return Ok(new { Succeed = false, Error = "User accout or password is null" });

#if FUTURE
            //TODO:以下逻辑合并至ServerMessageDispatcher.ProcessLoginRequire

            //根据账号索引查询
            var q = new IndexGet(appbox.Consts.SYS_EMPLOEE_MODEL_ID, appbox.Consts.EMPLOEE_UI_ACCOUNT_ID);
            q.Where(new KeyPredicate(appbox.Consts.EMPLOEE_ACCOUNT_ID, KeyPredicateType.Equal, require.User));
            var res = await q.ToIndexRowAsync();
            if (res.IsEmpty)
                return Ok(new { Succeed = false, Error = "User account not exists" });
            Guid emploeeID = res.TargetEntityId;
            byte[] passData = res.ValueTuple.GetBytes(appbox.Consts.EMPLOEE_PASSWORD_ID);
            res.Dispose();
            //验证密码
            if (!RuntimeContext.PasswordHasher.VerifyHashedPassword(passData, require.Password))
                return Ok(new { Succeed = false, Error = "Password not match" });

            //TODO:****暂全表扫描获取Emploee对应的OrgUnits，待用Include EntitySet实现
            var q1 = new TableScan(appbox.Consts.SYS_ORGUNIT_MODEL_ID);
            q1.Filter(q1.GetGuid(appbox.Consts.ORGUNIT_BASEID_ID) == emploeeID);
            var ous = await q1.ToListAsync();
            if (ous == null || ous.Count == 0)
                return Ok(new { Succeed = false, Error = "User must mapped to OrgUnit" });

            TreeNodePath path = await EntityStore.LoadTreeNodePathAsync(
                appbox.Consts.SYS_ORGUNIT_MODEL_ID, ous[0].Id, appbox.Consts.ORGUNIT_PARENT_ID, appbox.Consts.ORGUNIT_NAME_ID);

#else
            //查找账号并验证密码
            var q = new SqlQuery(appbox.Consts.SYS_EMPLOEE_MODEL_ID);
            q.Where(q.T["Account"] == require.User);
            var emp = await q.ToSingleAsync();
            if (emp == null)
                return Ok(new { Succeed = false, Error = "User account not exists" });
            Guid emploeeID = emp.GetGuid(StoreInitiator.PK_Member_Id);

            byte[] passData = emp.GetBytes(appbox.Consts.EMPLOEE_PASSWORD_ID);
            if (passData == null)
                return Ok(new { Succeed = false, Error = "User password not exists" });

            if (!RuntimeContext.PasswordHasher.VerifyHashedPassword(passData, require.Password))
                return Ok(new { Succeed = false, Error = "Password not match" });
            //查找对应的OrgUnits
            var q1 = new SqlQuery(appbox.Consts.SYS_ORGUNIT_MODEL_ID);
            q1.Where(q1.T["BaseId"] == emploeeID);
            var ous = await q1.ToListAsync();
            if (ous == null || ous.Count == 0)
                return Ok(new { Succeed = false, Error = "User must mapped to OrgUnit" });

            var q2 = new SqlQuery(appbox.Consts.SYS_ORGUNIT_MODEL_ID);
            q2.Where(q2.T["Id"] == ous[0].GetGuid(StoreInitiator.PK_Member_Id));
            TreeNodePath path = await q2.ToTreeNodePathAsync(q2.T["Parent"], q2.T["Name"]);
#endif

            object returnUserInfo = new { ous[0].Id, Name = path[0].Text, Account = require.User };

            //注册会话
            var id = (ulong)StringHelper.GetHashCode(require.User); //TODO:***** 暂简单hash
            var session = new WebSession(id, path, emploeeID, null /*TODO:tag暂null*/);
            HttpContext.Session.SaveWebSession(session);

            //返回登录成功
            Log.Debug($"用户[{session.GetFullName()}]登录.");
            return Ok(new { Succeed = true, UserInfo = returnUserInfo });
        }

        /// <summary>
        /// 通过第三方Token登录，如微信openid
        /// </summary>
        [HttpPost]
        public IActionResult LoginByToken([FromBody] LoginByTokenRequire require)
        {
            if (string.IsNullOrEmpty(require.Token) || string.IsNullOrEmpty(require.Validator))
                return Ok(new { Succeed = false, Error = "Token或验证服务为空" });

            //调用验证服务
            //object validateResult = null;
            //try
            //{
            //    validateResult = AppBox.Core.RuntimeContext.Default.Invoke(require.Validator, new InvokeArgs().Add(require.Token));
            //}
            //catch (Exception ex)
            //{
            //    Log.Warn($"Token验证服务内部异常: [{ex.GetType()}]{ex.Message}");
            //    return Ok(new { Succeed = false, Error = "Token验证服务内部异常" });
            //}

            ////判断返回的结果是否有效
            //if (validateResult == null)
            //    return Ok(new { Succeed = false, Error = "Token验证失败" });
            //var res = validateResult as Entity;
            //if (res == null)
            //    return Ok(new { Succeed = false, Error = "Token验证结果无效" });

            ////判断是否外部用户
            //TreeNodePath path = null;
            object returnUserInfo = null;
            //Guid? emploeeID = null;
            //string tag = null;
            //string user = null;
            //if (res.ModelID == "sys.Emploee")
            //{
            //    var q3 = new Query("sys.OrgUnit");
            //    q3.Where(q3.T["BaseType"] == res.Model.UID & q3.T["BaseID"] == res.ID);
            //    path = q3.ToTreeNodePath(q3.T["Parent"]); //todo: check path==null ?

            //    emploeeID = res.ID;
            //    tag = res.GetStringValue("SessionTag");
            //    user = res.GetStringValue("Account");
            //    returnUserInfo = new
            //    {
            //        ID = path[path.Level - 1].ID,
            //        Name = res.GetStringValue("Name"),
            //        Account = user
            //    };
            //}
            //else
            //{
            //    //查询组织路径
            //    Query q2 = new Query("sys.OrgUnit");
            //    var orgUnitGroupID = res.GetGuidValue("OrgUnitGroupID");
            //    q2.Where(q2.T["ID"] == orgUnitGroupID);
            //    path = q2.ToTreeNodePath(q2.T["Parent"]);
            //    if (path == null)
            //        return Ok(new { Succeed = false, Error = "外部用户未指定对应的组织单元组" });
            //    //重新构建TreeNodePath
            //    var ns = new List<TreeNodeInfo>();
            //    ns.Add(new TreeNodeInfo() { ID = res.ID, Text = res.GetStringValue("Name") });
            //    for (int i = 0; i < path.Level; i++)
            //    {
            //        ns.Add(path[i]);
            //    }
            //    path = new TreeNodePath(ns);
            //    tag = res.GetStringValue("SessionTag");
            //    user = res.GetStringValue("Account");

            //    returnUserInfo = new
            //    {
            //        ID = res.ID,
            //        Name = res.GetStringValue("Name"),
            //        Account = user
            //    };
            //}

            ////注册会话
            //var id = (ulong)StringHelper.GetHashCode(user); //todo:***** 暂简单hash
            //var session = new WebSession(id, path, emploeeID, tag);
            //HttpContext.Session.SetWebSession(session);

            //返回登录成功
            return Ok(new { Succeed = true, UserInfo = returnUserInfo });
        }

        /// <summary>
        /// 退出登录
        /// </summary>
        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return Ok();
        }

    }

    public struct LoginRequire
    {
        public string User { get; set; }
        public string Password { get; set; }
        /// <summary>
        /// 外部用户对应的实体模型标识，可以是映射至表存储的模型，但必须包含sys.External相应的成员
        /// </summary>
        public string ExternalModelID { get; set; } //TODO:remove 不再支持
    }

    public struct LoginByTokenRequire
    {
        /// <summary>
        /// 第三方Token, 如:微信OpenID
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// 验证第三方Token的服务, 如: SERP.WXService.LoginByWX
        /// </summary>
        public string Validator { get; set; }
    }

}