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
            var txn = await Transaction.BeginAsync();
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

                    await EntityStore.InsertEntityAsync(obj, txn);
                    await txn.CommitAsync();
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

            var q = new TableScan(Consts.SYS_CHECKOUT_MODEL_ID);
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
        internal static async Task CheckinAsync(Transaction txn)
        {
            var devId = RuntimeContext.Current.CurrentSession.LeafOrgUnitID;
            var model = await RuntimeContext.Current.GetModelAsync<EntityModel>(Consts.SYS_CHECKOUT_MODEL_ID);

            //TODO:***** Use DeleteCommand(join txn), 暂临时使用查询再删除
            var q = new TableScan(Consts.SYS_CHECKOUT_MODEL_ID);
            q.Filter(q.GetGuid(Consts.CHECKOUT_DEVELOPERID_ID) == devId);
            var list = await q.ToListAsync();
            if (list != null)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    await EntityStore.DeleteEntityAsync(model, list[i].Id, txn);
                }
            }
        }

    }
}
