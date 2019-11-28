using System;
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

    }
}
