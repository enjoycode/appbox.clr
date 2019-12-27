using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Models;
using appbox.Runtime;
using appbox.Server;
using Microsoft.AspNetCore.Mvc;

namespace appbox.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {

        private static readonly ApplicationModel app;
        private static readonly EntityModel emploee;

        private static readonly AsyncLocal<int> sessionId = new AsyncLocal<int>();

        public static int GetSessionId() => sessionId.Value;

        static TestController()
        {
            app = new ApplicationModel("appbox", Consts.SYS);

            emploee = new EntityModel(Consts.SYS_EMPLOEE_MODEL_ID, Consts.EMPLOEE, EntityStoreType.StoreWithMvcc);
            var name = new DataFieldModel(emploee, Consts.NAME, EntityFieldType.String);
            emploee.AddSysMember(name, Consts.EMPLOEE_NAME_ID);
            var account = new DataFieldModel(emploee, Consts.ACCOUNT, EntityFieldType.String);
            account.AllowNull = true;
            emploee.AddSysMember(account, Consts.EMPLOEE_ACCOUNT_ID);
            var password = new DataFieldModel(emploee, Consts.PASSWORD, EntityFieldType.Binary);
            password.AllowNull = true;
            emploee.AddSysMember(password, Consts.EMPLOEE_PASSWORD_ID);
        }

        [HttpGet]
        public Task<string> Get()
        {
            return Task.FromResult("Hello Future!");
        }

        [HttpGet("Channel")]
        public Task<string> Channel()
        {
            return Task.FromResult(Host.ChildProcess.AppContainer.Channel.GetDebugInfo());
        }

        [HttpGet("AppModel")]
        public async Task<ActionResult<string>> AppModel()
        {
            var appModel = await Store.ModelStore.LoadApplicationAsync(Consts.SYS_APP_ID);
            return appModel == null ? "Null" : $"{appModel.Owner}.{appModel.Name}";
        }

        [HttpGet("EmpModel")]
        public async Task<ActionResult<string>> EmpModel()
        {
            var modelId = emploee.Id;

            //var empModel = await Store.EntityStore.LoadModelAsync(ModelType.Entity, modelId);
            var empModel = await RuntimeContext.Current.GetModelAsync<EntityModel>(modelId);
            return empModel == null ? "Null" : $"{empModel.Name}";
        }

#if FUTURE
        private static int nameIndex;
        private async ValueTask InsertEmploeeAsync()
        {
            var index = Interlocked.Increment(ref nameIndex);
            string name = $"AAAAA{index}";
            var txn = await Store.Transaction.BeginAsync();
            try
            {
                var model = await RuntimeContext.Current.GetModelAsync<EntityModel>(Consts.SYS_EMPLOEE_MODEL_ID);
                var emp1 = new Entity(model);
                emp1.SetString(Consts.EMPLOEE_NAME_ID, name);
                emp1.SetString(Consts.EMPLOEE_ACCOUNT_ID, name);
                await Store.EntityStore.InsertEntityAsync(emp1, txn);
                await txn.CommitAsync();
            }
            catch (Exception ex)
            {
                Log.Warn(ExceptionHelper.GetExceptionDetailInfo(ex));
                txn.Rollback();
            }
        }

        [HttpGet("InsertPerf/{tasks}/{loop}")]
        public async Task<ActionResult<string>> InsertPerf(int tasks, int loop)
        {
            if (tasks <= 0 || loop <= 0) throw new ArgumentOutOfRangeException();

            return await SimplePerfTest.Run(tasks, loop, async (i, j) =>
            {
                await InsertEmploeeAsync();
            });
        }

        [HttpGet("FKInsertPerf")]
        public async Task<ActionResult<string>> FKInsertPerf()
        {
            var q = new Store.TableScan(Consts.SYS_EMPLOEE_MODEL_ID);
            q.Filter(q.GetString(Consts.EMPLOEE_NAME_ID) == "Admin");
            var emps = await q.ToListAsync();

            var model = await RuntimeContext.Current.GetModelAsync<EntityModel>(Consts.SYS_ORGUNIT_MODEL_ID);

            return await SimplePerfTest.Run(64, 1000, async (i, j) =>
            {
                var ou = new Entity(model);
                ou.SetString(1 << IdUtil.MEMBERID_SEQ_OFFSET, "OUName");
                ou.SetEntityId(2 << IdUtil.MEMBERID_SEQ_OFFSET, emps[0].Id);
                var txn = await Store.Transaction.BeginAsync();
                try
                {
                    await Store.EntityStore.InsertEntityAsync(ou, txn);
                    await txn.CommitAsync();
                }
                catch (Exception ex)
                {
                    txn.Rollback();
                    Console.WriteLine($"Insert orgunit error: {ex.Message}");
                }
            });
        }

        private static int keyIndex;
        private ValueTask KVInsertAsync(IntPtr txnPtr)
        {
            IntPtr keyPtr;
            IntPtr valuePtr;
            unsafe
            {
                var keyData = stackalloc byte[19];
                var idx = Interlocked.Increment(ref keyIndex);
                int* ptr = (int*)keyData;
                *ptr = idx;
                keyPtr = new IntPtr(keyData);
                valuePtr = NativeApi.NewNativeString(50, out byte* dataPtr);
            }

            var req = new ClrInsertRequire
            {
                RaftGroupId = 0,
                KeyPtr = keyPtr,
                KeySize = new IntPtr(19),
                DataPtr = valuePtr,
                SchemaVersion = 0,
                OverrideIfExists = true,
                DataCF = -1
            };
            IntPtr reqPtr;
            unsafe { reqPtr = new IntPtr(&req); }
            return Store.StoreApi.Api.ExecKVInsertAsync(txnPtr, reqPtr);
        }

        /// <summary>
        /// 测试纯KV插入性能
        /// </summary>
        [HttpGet("KVInsertPerf")]
        public async Task<ActionResult<string>> KVInsertPerf()
        {
            return await SimplePerfTest.Run(64, 1000, async (i, j) =>
            {
                var txn = await Store.Transaction.BeginAsync();
                await KVInsertAsync(txn.Handle);
                await KVInsertAsync(txn.Handle);
                await txn.CommitAsync();
            });
        }

        /// <summary>
        /// 惟一索引冲突
        /// </summary>
        [HttpGet("UIPerf")]
        public async Task<ActionResult<string>> UIPerf()
        {
            var model = await RuntimeContext.Current.GetModelAsync<EntityModel>(Consts.SYS_EMPLOEE_MODEL_ID);

            return await SimplePerfTest.Run(8, 2000, async (i, j) =>
            {
                var txn = await Store.Transaction.BeginAsync();
                var emp = new Entity(model);
                emp.SetString(Consts.EMPLOEE_NAME_ID, "Admin");
                emp.SetString(Consts.EMPLOEE_ACCOUNT_ID, "Admin");
                try
                {
                    await Store.EntityStore.InsertEntityAsync(emp, txn);
                    await txn.CommitAsync();
                }
                catch (Exception)
                {
                    txn.Rollback();
                }
            });
        }

        [HttpGet("IndexGet")]
        public async Task<string> IndexGet()
        {
            sessionId.Value = 12345678;
            Log.Debug($"1.ThreadId: {Thread.CurrentThread.ManagedThreadId} {Thread.CurrentThread.IsThreadPoolThread} SessionId: {sessionId.Value}");
            var q = new Store.IndexGet(Consts.SYS_EMPLOEE_MODEL_ID, Consts.EMPLOEE_UI_ACCOUNT_ID);
            q.Where(new Store.KeyPredicate(Consts.EMPLOEE_ACCOUNT_ID, Store.KeyPredicateType.Equal, "Admin"));
            var res = await q.ToIndexRowAsync();
            Log.Debug($"2.ThreadId: {Thread.CurrentThread.ManagedThreadId} {Thread.CurrentThread.IsThreadPoolThread} SessionId: {sessionId.Value}");
            return res.IsEmpty ? "Null\n" : "Ok\n";
        }

        [HttpGet("IndexGetPerf")]
        public async Task<string> IndexGetPerf()
        {
            return await SimplePerfTest.Run(64, 2000, async (i, j) =>
            {
                var q = new Store.IndexGet(Consts.SYS_EMPLOEE_MODEL_ID, Consts.EMPLOEE_UI_ACCOUNT_ID);
                q.Where(new Store.KeyPredicate(Consts.EMPLOEE_ACCOUNT_ID, Store.KeyPredicateType.Equal, "Admin"));
                var _ = await q.ToIndexRowAsync();
            });
        }

        [HttpGet("Scan")]
        public async Task<ActionResult<string>> Scan()
        {
            var q = new Store.TableScan(Consts.SYS_EMPLOEE_MODEL_ID);
            q.Filter(q.GetString(Consts.EMPLOEE_NAME_ID) == "Admin");
            var res = await q.ToListAsync();
            Guid id = Guid.Empty;
            string name = string.Empty;
            int count = 0;
            if (res != null && res.Count > 0)
            {
                count = res.Count;
                id = res[0].Id;
                name = res[0].GetString(Consts.EMPLOEE_NAME_ID);
            }
            return $"TableScan done. Rows={count} Id[0]={id} Name[0]={name}\n";
        }

        [HttpGet("ScanPerf/{tasks}/{loop}")]
        public async Task<ActionResult<string>> ScanPerf(int tasks, int loop)
        {
            if (tasks <= 0 || loop <= 0) throw new ArgumentOutOfRangeException();

            return await SimplePerfTest.Run(tasks, loop, async (i, j) =>
            {
                var q = new Store.TableScan(Consts.SYS_EMPLOEE_MODEL_ID);
                q.Filter(q.GetString(Consts.EMPLOEE_NAME_ID) == "Admin");
                await q.ToListAsync();
            });
        }

        [HttpGet("LoadEntitySet")]
        public async Task<object> LoadEntitySet()
        {
            var q = new Store.TableScan(Consts.SYS_ORGUNIT_MODEL_ID);
            q.Filter(q.GetString(Consts.ORGUNIT_NAME_ID) == "IT Dept");
            var parent = await q.ToListAsync();
            var parentId = parent[0].Id;

            var list = await Store.EntityStore.LoadEntitySetAsync(Consts.SYS_ORGUNIT_MODEL_ID, parentId, Consts.ORGUNIT_CHILDS_ID);
            if (list == null)
                return "Null";
            return list.Select(t => t.GetString(Consts.ORGUNIT_NAME_ID)).ToArray();
        }

        [HttpGet("LoadEntitySetPerf")]
        public async Task<string> LoadEntitySetPerf()
        {
            var q = new Store.TableScan(Consts.SYS_ORGUNIT_MODEL_ID);
            q.Filter(q.GetString(Consts.ORGUNIT_NAME_ID) == "IT Dept");
            var parent = await q.ToListAsync();
            var parentId = parent[0].Id;

            return await SimplePerfTest.Run(32, 4000, async (i, j) =>
            {
                await Store.EntityStore.LoadEntitySetAsync(Consts.SYS_ORGUNIT_MODEL_ID, parentId, Consts.ORGUNIT_CHILDS_ID);
            });
        }
#endif

        [HttpGet("GetServiceAsm/{app}/{service}")]
        public async Task<ActionResult> GetServiceAsm(string app, string service)
        {
            var fileName = $"{app}.{service}.dll";
            var data = await Store.ModelStore.LoadServiceAssemblyAsync($"{app}.{service}");
            //var ctg = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
            return File(data, "application/x-zip-compressed", fileName);
        }

        //[HttpGet("IndexScan")]
        //public async Task<object> IndexScan()
        //{
        //    var q = new Store.IndexScan(Consts.SYS_EMPLOEE_MODEL_ID, Consts.EMPLOEE_UI_ACCOUNT_ID);
        //    q.Take(100);
        //    return await q.ToListAsync();
        //}

        [HttpGet("SqlTest")]
        public async Task<object> SqlTest()
        {
            try
            {
                string storeName = "DemoDB";
                var sqlStore = Store.SqlStore.Get((ulong)StringHelper.GetHashCode(storeName));
                var conn = sqlStore.MakeConnection();
                conn.ConnectionString = "Server=10.211.55.2;Port=5432;Database=DpsStore;Userid=lushuaijun;Password=;Enlist=true;Pooling=true;MinPoolSize=1;MaxPoolSize=200;";
                await conn.OpenAsync();
                conn.Close();
                return "Done";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}
