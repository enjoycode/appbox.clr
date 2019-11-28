using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using appbox.Data;
using appbox.Runtime;
using appbox.Serialization;
using appbox.Design;

namespace appbox.Server.Channel
{

    /// <summary>
    /// WebSocket或Ajax通道的会话
    /// </summary>
    public sealed class WebSession : IDeveloperSession, IDisposable
    {
        internal const string UserSessionKey = "User";

        private readonly Guid? _emploeeID;

        public Guid EmploeeID => _emploeeID ?? Guid.Empty;

        internal TreeNodePath TreeNodePath { get; }

        public ulong SessionID { get; }

        public string Tag { get; }

        int ISessionInfo.Levels => TreeNodePath.Level;

        public bool IsExternal => !_emploeeID.HasValue;

        Guid ISessionInfo.LeafOrgUnitID => _emploeeID.HasValue ? TreeNodePath[0].ID : TreeNodePath[1].ID;

        Guid ISessionInfo.ExternalID => _emploeeID.HasValue ? Guid.Empty : TreeNodePath[0].ID;

        public TreeNodeInfo this[int index] => TreeNodePath[index];

        ulong ISessionInfo.SessionID => SessionID;

        string ISessionInfo.Name => TreeNodePath[0].Text;

        string ISessionInfo.FullName => this.GetFullName();

        /// <summary>
        /// 用于判断是否当前实例被WebSocket通道拥有
        /// </summary>
        internal WebSocketClient Owner;

        #region ====Ctor====
        public WebSession(ulong id, TreeNodePath path, Guid? empID, string tag)
        {
            SessionID = id;
            _emploeeID = empID;
            TreeNodePath = path;
            Tag = tag;
        }
        #endregion

        #region ====设计时相关====
        private DesignHub designHub;
        public DesignHub GetDesignHub()
        {
            if (designHub == null)
            {
                lock (this)
                {
                    if (designHub == null)
                    {
                        //创建DesignHub实例前，判断当前用户是否具备开发者权限
                        //TODO: fix
                        //if (!AppBox.Core.PermissionService.HasPermission(RuntimeContext.Default, "sys.Developer"))
                            //throw new Exception("当前会话不具备开发人员权限");
                        //尝试从WebSocketManager内获取缓存的WebSession，主要用于Ajax上传通道以指向相同的DesignHub,而不是重新创建一个DesignHub实例
                        if (Owner != null)
                        {
                            designHub = new DesignHub(this);
                        }
                        else
                        {
                            var websocketSession = WebSocketManager.GetSessionByID(SessionID);
                            if (websocketSession == null)
                                throw new Exception("非WebSocket通道无法创建DesignHub");
                            designHub = websocketSession.GetDesignHub();
                        }
                    }
                }
            }
            return designHub;
        }

        //string IDesignSession.CultureName
        //{
        //    get { return GetDesignHub().CultureName; }
        //    set { GetDesignHub().CultureName = value; }
        //}

        ///// <summary>
        ///// DesighHub未实始化返回null
        ///// </summary>
        //IDesignTimeModelContainer IDesignSession.DesignTimeModelContainer
        //{
        //    get
        //    {
        //        if (designHub == null)
        //            return null; //throw new Exception("GetDesignTimeModelContainer when DesignHub not init.");

        //        if (designHub.PublishModels == null)
        //            return designHub;
        //        else
        //            return designHub.PublishModels;
        //    }
        //}

        void IDeveloperSession.SendEvent(int source, string body)
        {
            if (Owner == null)
            {
                Log.Warn("Cannot SendEvent without owner");
                return;
            }

            Owner.SendEvent(source, body);
        }
        #endregion

        #region ====IDisposable Support====
        private bool disposedValue = false; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (designHub != null)
                    {
                        designHub.Dispose();
                        designHub = null;
                    }
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

    }

    #region ====Extensions====
    public static class SessionExtensions
    {

        public static void SaveWebSession(this ISession session, WebSession value)
        {
            byte[] data = null;
            using (var ms = new MemoryStream())
            {
                var bs = new BinSerializer(ms); //TODO:使用ThreadCache或参照RoutedSesseionWriter
                bs.Write(value.SessionID);
                bs.Serialize(value.TreeNodePath);
                bs.Write(value.IsExternal);
                if (!value.IsExternal)
                    bs.Write(value.EmploeeID);
                bs.Write(value.Tag);
                bs.Clear();
                data = ms.ToArray();
            }

            session.Set(WebSession.UserSessionKey, data);
        }

        public static WebSession LoadWebSession(this ISession session)
        {
            var data = session.Get(WebSession.UserSessionKey);
            if (data == null)
                return null;

            ulong id;
            Guid? empID = null;
            TreeNodePath path;
            string tag = null;
            using (var ms = new MemoryStream(data))
            {
                var bs = new BinSerializer(ms); //TODO:使用ThreadCache或参照RoutedSessionReader
                id = bs.ReadUInt64();
                path = (TreeNodePath)bs.Deserialize();
                bool isExternal = bs.ReadBoolean();
                if (!isExternal)
                    empID = bs.ReadGuid();
                tag = bs.ReadString();
                bs.Clear();
            }

            return new WebSession(id, path, empID, tag);
        }
    }
    #endregion

}