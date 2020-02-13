using System;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Security;
using appbox.Models;

namespace appbox.Runtime
{
    public static class RuntimeContext
    {
        public static IRuntimeContext Current { get; private set; }

        public static ISessionInfo Session => Current?.CurrentSession;

        public static IPasswordHasher PasswordHasher { get; } = new PasswordHasher();

        internal static void Init(IRuntimeContext context, ushort peerId)
        {
            if (Current != null)
                throw new Exception("RuntimeContext has initialized.");

            Current = context ?? throw new ArgumentNullException();
            PeerId = peerId;
        }

        public static ushort PeerId { get; private set; }

        //TODO:暂放在这里
        /// <summary>
        /// 检查当前运行时内的当前用户是否具备指定的PermissionModelId的授权
        /// </summary>
        public static bool HasPermission(ulong permissionModelId)
        {
            if (Current == null) return false;

            var pm = Current.GetModelAsync<PermissionModel>(permissionModelId).Result; //TODO: cache it
            if (pm == null) return false;

            var curSession = Current.CurrentSession;
            if (curSession == null) return false;

            if (pm.HasOrgUnits)
            {
                for (int i = 0; i < pm.OrgUnits.Count; i++)
                {
                    var startIndex = curSession.IsExternal ? 1 : 0; //注意：外部会话忽略第0级的External信息
                    for (int j = startIndex; j < curSession.Levels; j++)
                    {
                        if (curSession[j].ID == pm.OrgUnits[i])
                            return true;
                    }
                }
            }

            return false;
        }

        #region ====Shortpath for service invoke====
        //仅用于服务模型虚拟代码转换为运行时代码的调用

        public static async ValueTask<bool> InvokeBooleanAsync(string service, InvokeArgs args)
        {
            var res = await Current.InvokeAsync(service, args);
            return res.BooleanValue;
        }

        public static async ValueTask<byte> InvokeByteAsync(string service, InvokeArgs args)
        {
            var res = await Current.InvokeAsync(service, args);
            return res.ByteValue;
        }

        public static async ValueTask<ushort> InvokeUInt16Async(string service, InvokeArgs args)
        {
            var res = await Current.InvokeAsync(service, args);
            return res.UInt16Value;
        }

        public static async ValueTask<short> InvokeInt16Async(string service, InvokeArgs args)
        {
            var res = await Current.InvokeAsync(service, args);
            return res.Int16Value;
        }

        public static async ValueTask<uint> InvokeUInt32Async(string service, InvokeArgs args)
        {
            var res = await Current.InvokeAsync(service, args);
            return res.UInt32Value;
        }

        public static async ValueTask<int> InvokeInt32Async(string service, InvokeArgs args)
        {
            var res = await Current.InvokeAsync(service, args);
            return res.Int32Value;
        }

        public static async ValueTask<ulong> InvokeUInt64Async(string service, InvokeArgs args)
        {
            var res = await Current.InvokeAsync(service, args);
            return res.UInt64Value;
        }

        public static async ValueTask<long> InvokeInt64Async(string service, InvokeArgs args)
        {
            var res = await Current.InvokeAsync(service, args);
            return res.Int64Value;
        }

        public static async ValueTask<float> InvokeFloatAsync(string service, InvokeArgs args)
        {
            var res = await Current.InvokeAsync(service, args);
            return res.FloatValue;
        }

        public static async ValueTask<double> InvokeDoubleAsync(string service, InvokeArgs args)
        {
            var res = await Current.InvokeAsync(service, args);
            return res.DoubleValue;
        }

        public static async ValueTask<DateTime> InvokeDateTimeAsync(string service, InvokeArgs args)
        {
            var res = await Current.InvokeAsync(service, args);
            return res.DateTimeValue;
        }

        public static async ValueTask<Guid> InvokeGuidAsync(string service, InvokeArgs args)
        {
            var res = await Current.InvokeAsync(service, args);
            return res.GuidValue;
        }

        public static async ValueTask<decimal> InvokeDecimalAsync(string service, InvokeArgs args)
        {
            var res = await Current.InvokeAsync(service, args);
            return res.DecimalValue;
        }

        public static async ValueTask<TResult> InvokeAsync<TResult>(string service, InvokeArgs args)
        {
            var res = await Current.InvokeAsync(service, args);
            return (TResult)res.ObjectValue;
        }
        #endregion

    }
}
