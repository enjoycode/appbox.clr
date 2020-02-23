using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using appbox.Runtime;
using appbox.Data;
using appbox.Store;
using appbox.Models;

namespace appbox.Design
{
    static class CheckoutService
    {

        /// <summary>
        /// 签出指定节点
        /// </summary>
        internal static async Task<CheckoutResult> CheckoutAsync(List<CheckoutInfo> checkoutInfos)
        {
            if (checkoutInfos == null || checkoutInfos.Count == 0)
                return null;

            //尝试向存储插入签出信息
            var model = await RuntimeContext.Current.GetModelAsync<EntityModel>(Consts.SYS_CHECKOUT_MODEL_ID);
#if FUTURE
            var txn = await Transaction.BeginAsync();
#else
            using var conn = await SqlStore.Default.OpenConnectionAsync();
            using var txn = conn.BeginTransaction();
#endif
            try
            {
                for (int i = 0; i < checkoutInfos.Count; i++)
                {
                    var info = checkoutInfos[i];
                    var obj = new Entity(model);
                    obj.SetByte(Consts.CHECKOUT_NODETYPE_ID, (byte)info.NodeType);
                    obj.SetString(Consts.CHECKOUT_TARGETID_ID, info.TargetID);
                    obj.SetGuid(Consts.CHECKOUT_DEVELOPERID_ID, info.DeveloperOuid);
                    obj.SetString(Consts.CHECKOUT_DEVELOPERNAME_ID, info.DeveloperName);
                    obj.SetInt32(Consts.CHECKOUT_VERSION_ID, (int)info.Version);

#if FUTURE
                    await EntityStore.InsertEntityAsync(obj, txn);
                    await txn.CommitAsync();
#else
                    await SqlStore.Default.InsertAsync(obj, txn);
                    txn.Commit();
#endif
                }
            }
            catch (Exception)
            {
                txn.Rollback();
                return new CheckoutResult(false);
            }

            //检查签出单个模型时，存储有无新版本
            CheckoutResult result = new CheckoutResult(true);
            if (checkoutInfos[0].IsSingleModel)
            {
                var storedModel = await ModelStore.LoadModelAsync(ulong.Parse(checkoutInfos[0].TargetID));
                if (storedModel.Version != checkoutInfos[0].Version)
                {
                    result.ModelWithNewVersion = storedModel;
                }
            }
            return result;
        }

        /// <summary>
        /// 用于DesignTree加载时
        /// </summary>
        internal static async Task<Dictionary<string, CheckoutInfo>> LoadAllAsync()
        {
            var list = new Dictionary<string, CheckoutInfo>();
#if FUTURE
            var q = new TableScan(Consts.SYS_CHECKOUT_MODEL_ID);
#else
            var q = new SqlQuery(Consts.SYS_CHECKOUT_MODEL_ID);
#endif
            var res = await q.ToListAsync();
            if (res != null)
            {
                for (int i = 0; i < res.Count; i++)
                {
                    var info = new CheckoutInfo((DesignNodeType)res[i].GetByte(Consts.CHECKOUT_NODETYPE_ID),
                                                res[i].GetString(Consts.CHECKOUT_TARGETID_ID),
                                                (uint)res[i].GetInt32(Consts.CHECKOUT_VERSION_ID),
                                                res[i].GetString(Consts.CHECKOUT_DEVELOPERNAME_ID),
                                                res[i].GetGuid(Consts.CHECKOUT_DEVELOPERID_ID));
                    list.Add(info.GetKey(), info);
                }
            }

            return list;
        }

        /// <summary>
        /// 签入当前用户所有已签出项
        /// </summary>
        internal static async Task CheckinAsync(
#if FUTURE
            Transaction txn
#else
            System.Data.Common.DbTransaction txn
#endif
            )
        {
            var devId = RuntimeContext.Current.CurrentSession.LeafOrgUnitID;
            var model = await RuntimeContext.Current.GetModelAsync<EntityModel>(Consts.SYS_CHECKOUT_MODEL_ID);

            //TODO:***** Use DeleteCommand(join txn), 暂临时使用查询再删除
#if FUTURE
            var q = new TableScan(Consts.SYS_CHECKOUT_MODEL_ID);
            q.Filter(q.GetGuid(Consts.CHECKOUT_DEVELOPERID_ID) == devId);
#else
            var q = new SqlQuery(Consts.SYS_CHECKOUT_MODEL_ID);
            q.Where(q.T["DeveloperId"] == devId);
#endif

            var list = await q.ToListAsync();
            if (list != null)
            {
                for (int i = 0; i < list.Count; i++)
                {
#if FUTURE
                    await EntityStore.DeleteEntityAsync(model, list[i].Id, txn);
#else
                    await SqlStore.Default.DeleteAsync(list[i], txn);
#endif
                }
            }
        }

    }
}
