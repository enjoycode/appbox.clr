using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Models;
using appbox.Serialization;
using appbox.Server;

namespace appbox.Store
{
    /// <summary>
    /// 模型存储相关Api
    /// </summary>
    internal static class ModelStore
    {
        private const int APP_DATA_OFFSET = 9;
        private static HostStoreApi HostApi => (HostStoreApi)StoreApi.Api;

        #region ====模型相关操作====
        //TODO: 获取分区对应的RaftGroupId时考虑在Native层缓存

        /// <summary>
        /// Serializes the model.
        /// </summary>
        /// <returns>在主进程内返回的是NativeString，在子进程内返回NativeBytes</returns>
        internal static unsafe IntPtr SerializeModel(object obj, out int size, int offset = 0)
        {
            //TODO:暂使用内存Copy
            byte[] data = null;
            using (MemoryStream ms = new MemoryStream(1024))
            {
                BinSerializer cf = new BinSerializer(ms);
                try { cf.Serialize(obj); }
                catch (Exception) { throw; }
                finally { cf.Clear(); }

                ms.Close();
                data = ms.ToArray();
            }

            size = data.Length + offset;
            IntPtr nativeDataPtr = IntPtr.Zero;
            byte* dp;
            if (Runtime.RuntimeContext.Current.RuntimeId == 0)
            {
                nativeDataPtr = NativeApi.NewNativeString(size, out dp);
            }
            else
            {
                nativeDataPtr = NativeBytes.MakeRaw(size);
                dp = (byte*)nativeDataPtr + 4;
            }

            //Log.Debug($"序列化模型: {obj.GetType().Name} arraySize={size} stringSize={NativeString.GetSize(dataString)}");
            fixed (byte* pv = data)
            {
                for (int i = 0; i < offset; i++)
                {
                    dp[i] = 0;
                }
                Buffer.MemoryCopy(pv, dp + offset, data.Length, data.Length);
            }

            return nativeDataPtr;
        }

        internal static unsafe object DeserializeModel(IntPtr dataPtr, int dataSize, int offset = 0)
        {
            byte* dp = (byte*)dataPtr.ToPointer();

            object result = null;
            var stream = new UnmanagedMemoryStream(dp + offset, dataSize - offset);
            BinSerializer cf = new BinSerializer(stream);
            try { result = cf.Deserialize(); }
            catch (Exception) { throw; }
            finally { cf.Clear(); }

            stream.Close();

            if (result != null && result is ApplicationModel && offset == APP_DATA_OFFSET)
            {
                var app = (ApplicationModel)result;
                app.StoreId = dp[0];
                uint* devIdCounterPtr = (uint*)(dp + 1);
                //uint* usrIdCounterPtr = (uint*)(dp + 5);
                app.DevModelIdSeq = *devIdCounterPtr;
            }
            return result;
        }

        internal static async ValueTask CreateApplicationAsync(ApplicationModel app)
        {
            IntPtr dataPtr = SerializeModel(app, out _, APP_DATA_OFFSET);
            IntPtr keyPtr;
            unsafe
            {
                byte* kPtr = stackalloc byte[KeyUtil.APP_KEY_SIZE];
                KeyUtil.WriteAppKey(kPtr, app.Id);
                keyPtr = new IntPtr(kPtr);
            }

            var appId = await HostApi.CreateApplicationAsync(keyPtr, KeyUtil.APP_KEY_SIZE, dataPtr);
            app.StoreId = appId;
        }

        internal static async ValueTask<ulong> GenModelIdAsync(uint appId, ModelType type, ModelLayer layer)
        {
            if (layer == ModelLayer.SYS) //不允许SYS Layer
                throw new ArgumentException(nameof(layer));

            var seq = await HostApi.MetaGenModelIdAsync(appId, layer == ModelLayer.DEV);

            var nid = (ulong)appId << IdUtil.MODELID_APPID_OFFSET;
            nid |= (ulong)type << IdUtil.MODELID_TYPE_OFFSET;
            nid |= (ulong)seq << IdUtil.MODELID_SEQ_OFFSET;
            nid |= (ulong)layer;
            return nid;
        }

        /// <summary>
        /// 用于运行时加载单个ApplicationModel
        /// </summary>
        internal static async ValueTask<ApplicationModel> LoadApplicationAsync(uint appId)
        {
            IntPtr keyPtr;
            unsafe
            {
                byte* pk = stackalloc byte[KeyUtil.APP_KEY_SIZE];
                KeyUtil.WriteAppKey(pk, appId);
                keyPtr = new IntPtr(pk);
            }

            var res = await StoreApi.Api.ReadIndexByGetAsync(KeyUtil.META_RAFTGROUP_ID, keyPtr, KeyUtil.APP_KEY_SIZE);
            if (res == null)
                return null;

            ApplicationModel app = null;
            try { app = (ApplicationModel)DeserializeModel(res.DataPtr, (int)res.Size, APP_DATA_OFFSET); }
            catch (Exception) { throw; }
            finally { res.Dispose(); }

            return app;
        }

        /// <summary>
        /// 用于设计时加载所有ApplicationModel
        /// </summary>
        internal static async ValueTask<ApplicationModel[]> LoadAllApplicationAsync()
        {
            byte appKey = KeyUtil.METACF_APP_PREFIX;
            IntPtr appKeyPtr;
            unsafe { appKeyPtr = new IntPtr(&appKey); }

            var req = new ClrScanRequire
            {
                RaftGroupId = KeyUtil.META_RAFTGROUP_ID,
                BeginKeyPtr = appKeyPtr,
                BeginKeySize = new IntPtr(1),
                EndKeyPtr = IntPtr.Zero,
                EndKeySize = IntPtr.Zero,
                FilterPtr = IntPtr.Zero,
                Skip = 0,
                Take = uint.MaxValue,
                DataCF = -1
            };
            IntPtr reqPtr;
            unsafe { reqPtr = new IntPtr(&req); }
            var res = await HostApi.ReadIndexByScanAsync(reqPtr);
            var apps = new ApplicationModel[res.Length];
            try
            {
                int index = 0;
                res.ForEachRow((kp, ks, vp, vs) =>
                {
                    apps[index] = (ApplicationModel)DeserializeModel(vp, vs, APP_DATA_OFFSET);
                    index++;
                });
            }
            catch (Exception) { throw; }
            finally { res.Dispose(); }

            return apps;
        }

        /// <summary>
        /// 用于设计时加载所有Model
        /// </summary>
        internal static async ValueTask<ModelBase[]> LoadAllModelAsync()
        {
            byte key = KeyUtil.METACF_MODEL_PREFIX;
            IntPtr keyPtr;
            unsafe { keyPtr = new IntPtr(&key); }

            var req = new ClrScanRequire
            {
                RaftGroupId = KeyUtil.META_RAFTGROUP_ID,
                BeginKeyPtr = keyPtr,
                BeginKeySize = new IntPtr(1),
                EndKeyPtr = IntPtr.Zero,
                EndKeySize = IntPtr.Zero,
                FilterPtr = IntPtr.Zero,
                Skip = 0,
                Take = uint.MaxValue,
                DataCF = -1
            };
            IntPtr reqPtr;
            unsafe { reqPtr = new IntPtr(&req); }
            var res = await HostApi.ReadIndexByScanAsync(reqPtr);
            var models = new ModelBase[res.Length];
            try
            {
                int index = 0;
                res.ForEachRow((kp, ks, vp, vs) =>
                {
                    models[index] = (ModelBase)DeserializeModel(vp, vs);
                    models[index].AcceptChanges(); //TODO:check是否在这里AcceptChanges
                    index++;
                });
            }
            catch (Exception) { throw; }
            finally { res.Dispose(); }

            return models;
        }

        /// <summary>
        /// 用于设计时加载所有Folder
        /// </summary>
        internal static async ValueTask<ModelFolder[]> LoadAllFolderAsync()
        {
            byte key = KeyUtil.METACF_FOLDER_PREFIX;
            IntPtr keyPtr;
            unsafe { keyPtr = new IntPtr(&key); }

            var req = new ClrScanRequire
            {
                RaftGroupId = KeyUtil.META_RAFTGROUP_ID,
                BeginKeyPtr = keyPtr,
                BeginKeySize = new IntPtr(1),
                EndKeyPtr = IntPtr.Zero,
                EndKeySize = IntPtr.Zero,
                FilterPtr = IntPtr.Zero,
                Skip = 0,
                Take = uint.MaxValue,
                DataCF = -1
            };
            IntPtr reqPtr;
            unsafe { reqPtr = new IntPtr(&req); }
            var res = await HostApi.ReadIndexByScanAsync(reqPtr);
            var folders = new ModelFolder[res.Length];
            try
            {
                int index = 0;
                res.ForEachRow((kp, ks, vp, vs) =>
                {
                    folders[index] = (ModelFolder)DeserializeModel(vp, vs);
                    index++;
                });
            }
            catch (Exception) { throw; }
            finally { res.Dispose(); }

            return folders;
        }

        /// <summary>
        /// 加载单个Model，用于运行时或设计时重新加载
        /// </summary>
        internal static async ValueTask<ModelBase> LoadModelAsync(ulong modelId)
        {
            IntPtr keyPtr;
            unsafe
            {
                byte* pk = stackalloc byte[KeyUtil.MODEL_KEY_SIZE];
                KeyUtil.WriteModelKey(pk, modelId);
                keyPtr = new IntPtr(pk);
            }

            var res = await StoreApi.Api.ReadIndexByGetAsync(KeyUtil.META_RAFTGROUP_ID, keyPtr, KeyUtil.MODEL_KEY_SIZE);
            if (res == null)
                return null;

            ModelBase model = null;
            try { model = (ModelBase)DeserializeModel(res.DataPtr, (int)res.Size); } //TODO: change to runtime mode
            catch (Exception) { throw; }
            finally { res.Dispose(); }

            model.AcceptChanges(); //TODO:同上
            return model;
        }

        internal static async ValueTask InsertModelAsync(ModelBase model, Transaction txn)
        {
            IntPtr keyPtr;
            IntPtr dataPtr = SerializeModel(model, out int dataSize);

            unsafe
            {
                byte* pk = stackalloc byte[KeyUtil.MODEL_KEY_SIZE];
                KeyUtil.WriteModelKey(pk, model.Id);
                keyPtr = new IntPtr(pk);
            }

            var req = new ClrInsertRequire
            {
                RaftGroupId = KeyUtil.META_RAFTGROUP_ID,
                KeyPtr = keyPtr,
                KeySize = new IntPtr(KeyUtil.MODEL_KEY_SIZE),
                DataPtr = dataPtr,
                SchemaVersion = 0,
                OverrideIfExists = false,
                DataCF = -1
            };
            IntPtr reqPtr;
            unsafe { reqPtr = new IntPtr(&req); }
            await HostApi.ExecKVInsertAsync(txn.Handle, reqPtr);
        }

        internal static async ValueTask UpdateModelAsync(ModelBase model, Transaction txn, Func<uint, ApplicationModel> getApp)
        {
            //TODO:考虑先处理变更项但不提议变更命令，再保存AcceptChanges后的模型数据，最后事务提议变更命令
            model.Version += 1; //注意：模型版本号+1

            IntPtr keyPtr;
            IntPtr dataPtr = SerializeModel(model, out int dataSize);
            unsafe
            {
                byte* pk = stackalloc byte[KeyUtil.MODEL_KEY_SIZE];
                KeyUtil.WriteModelKey(pk, model.Id);
                keyPtr = new IntPtr(pk);
            }
            var req = new ClrUpdateRequire()
            {
                RaftGroupId = KeyUtil.META_RAFTGROUP_ID,
                KeyPtr = keyPtr,
                KeySize = new IntPtr(KeyUtil.MODEL_KEY_SIZE),
                DataPtr = dataPtr,
                SchemaVersion = 0,
                DataCF = -1,
                Merge = false,
                ReturnExists = false
            };
            IntPtr reqPtr;
            unsafe { reqPtr = new IntPtr(&req); }
            await HostApi.ExecKVUpdateAsync(txn.Handle, reqPtr);

            //EntityModel特殊处理
            if (model.ModelType == ModelType.Entity)
            {
                EntityModel em = (EntityModel)model;
                //system entity model's schema has changed
                if (em.SysStoreOptions != null && em.SysStoreOptions.OldSchemaVersion != em.SysStoreOptions.SchemaVersion)
                {
                    Log.Debug($"Entity[{em.Name}] schema changed, {em.SysStoreOptions.OldSchemaVersion} -> {em.SysStoreOptions.SchemaVersion}");
                    var app = getApp(em.AppId);
                    var alterCmdPtr = NativeApi.NewAlterTable(em.TableId | ((uint)app.StoreId << 24), em.SysStoreOptions.SchemaVersion);
                    //开始处理成员变更项
                    foreach (var m in em.Members)
                    {
                        if (m.PersistentState == PersistentState.Detached && !m.AllowNull) //仅新的非空的成员
                        {
                            var defaultValue = new EntityMember();
                            if (m.Type == EntityMemberType.DataField) //TODO:other
                                defaultValue = ((DataFieldModel)m).DefaultValue.Value;

                            unsafe
                            {
                                int* varSizes = stackalloc int[1];
                                var valueSize = EntityStoreWriter.CalcMemberSize(ref defaultValue, varSizes, false);
                                byte* valueData = stackalloc byte[valueSize];
                                var writer = new EntityStoreWriter(valueData, 0);
                                writer.WriteMember(ref defaultValue, varSizes, false, false);

                                NativeApi.AlterTableAddColumn(alterCmdPtr, new IntPtr(valueData), valueSize);
                            }
                        }
                        else if (m.PersistentState == PersistentState.Deleted)
                        {
                            //TODO:考虑底层实现合并多个删除的成员
                            if (m.Type == EntityMemberType.DataField)
                            {
                                //TODO:引用外键特殊处理
                                NativeApi.AlterTableDropColumn(alterCmdPtr, m.MemberId);
                            }
                            else
                            {
                                throw ExceptionHelper.NotImplemented();
                            }
                        }
                    }

                    //开始处理索引变更项
                    if (em.SysStoreOptions.HasIndexes)
                    {
                        foreach (var index in em.SysStoreOptions.Indexes)
                        {
                            if (index.PersistentState == PersistentState.Detached)
                            {
                                IntPtr fieldsPtr = IntPtr.Zero;
                                int fieldsSize = index.Fields.Length * 2;
                                IntPtr storingPtr = IntPtr.Zero;
                                int storingSize = index.HasStoringFields ? index.StoringFields.Length * 2 : 0;
                                unsafe
                                {
                                    byte* fPtr = stackalloc byte[fieldsSize];
                                    fieldsPtr = new IntPtr(fPtr);
                                    ushort* mPtr = (ushort*)fPtr;
                                    for (int i = 0; i < index.Fields.Length; i++)
                                    {
                                        //注意：传入底层的MemberId包含OrderFlag
                                        int orderFlag = index.Fields[i].OrderByDesc ? 1 : 0;
                                        mPtr[i] = (ushort)(index.Fields[i].MemberId | (orderFlag << IdUtil.MEMBERID_ORDER_OFFSET));
                                    }

                                    if (storingSize > 0)
                                    {
                                        byte* sPtr = stackalloc byte[storingSize];
                                        storingPtr = new IntPtr(sPtr);
                                        mPtr = (ushort*)sPtr;
                                        for (int i = 0; i < index.StoringFields.Length; i++)
                                        {
                                            mPtr[i] = index.StoringFields[i];
                                        }
                                    }
                                }
                                NativeApi.AlterTableAddIndex(alterCmdPtr, index.IndexId, index.Global,
                                    fieldsPtr, fieldsSize, storingPtr, storingSize);
                            }
                            else if (index.PersistentState == PersistentState.Deleted)
                            {
                                NativeApi.AlterTableDropIndex(alterCmdPtr, index.IndexId);
                            }
                        }
                    }

                    //递交AlterTable任务
                    await HostApi.ExecMetaAlterTableAsync(txn.Handle, alterCmdPtr);
                }
            }
        }

        internal static async ValueTask DeleteModelAsync(ModelBase model, Transaction txn, Func<uint, ApplicationModel> getApp)
        {
            if (model.ModelType == ModelType.Entity && ((EntityModel)model).SysStoreOptions != null)
            {
                //TODO:考虑先保存删除状态的实体模型，存储层异步任务完成后会删除相应的实体模型
                var app = getApp(model.AppId);
                EntityModel em = (EntityModel)model;
                var tableId = em.TableId | ((uint)app.StoreId << 24);
                //TODO:全局索引入参处理
                await HostApi.ExecMetaDropTableAsync(txn.Handle, tableId, model.Id, IntPtr.Zero, IntPtr.Zero, false);
            }
            else //----以下非系统存储的实体模型直接删除原数据----
            {
                IntPtr keyPtr;
                unsafe
                {
                    byte* pk = stackalloc byte[KeyUtil.MODEL_KEY_SIZE];
                    KeyUtil.WriteModelKey(pk, model.Id);
                    keyPtr = new IntPtr(pk);
                }
                var req = new ClrDeleteRequire
                {
                    RaftGroupId = KeyUtil.META_RAFTGROUP_ID,
                    KeyPtr = keyPtr,
                    KeySize = new IntPtr(KeyUtil.MODEL_KEY_SIZE),
                    SchemaVersion = 0,
                    ReturnExists = false,
                    DataCF = -1
                };
                IntPtr reqPtr;
                unsafe { reqPtr = new IntPtr(&req); }
                await HostApi.ExecKVDeleteAsync(txn.Handle, reqPtr);
            }
        }

        /// <summary>
        /// 插入或更新文件夹
        /// </summary>
        internal static async ValueTask UpsertFolderAsync(ModelFolder folder, Transaction txn)
        {
            if (folder.Parent != null)
                throw new InvalidOperationException("Can't save none root folder.");

            //TODO:check need add version

            IntPtr keyPtr;
            IntPtr dataPtr = SerializeModel(folder, out _);
            unsafe
            {
                byte* pk = stackalloc byte[KeyUtil.FOLDER_KEY_SIZE];
                KeyUtil.WriteFolderKey(pk, folder.AppId, folder.TargetModelType);
                keyPtr = new IntPtr(pk);
            }

            var req = new ClrInsertRequire
            {
                RaftGroupId = KeyUtil.META_RAFTGROUP_ID,
                KeyPtr = keyPtr,
                KeySize = new IntPtr(KeyUtil.FOLDER_KEY_SIZE),
                DataPtr = dataPtr,
                SchemaVersion = 0,
                OverrideIfExists = true,
                DataCF = -1
            };
            IntPtr reqPtr;
            unsafe { reqPtr = new IntPtr(&req); }
            await HostApi.ExecKVInsertAsync(txn.Handle, reqPtr);
        }

        /// <summary>
        /// 用于设计时加载变更添加的索引的构建状态
        /// </summary>
        internal static async ValueTask LoadIndexBuildingStatesAsync(ApplicationModel app, EntityModel model)
        {
            if (!model.SysStoreOptions.HasIndexes || model.SysStoreOptions.SchemaVersion == 0) return;
            var addedIndexes = model.SysStoreOptions.Indexes.Where(t => t.State == EntityIndexState.Building);
            if (!addedIndexes.Any()) return;
            //先初始化为Ready状态
            foreach (var item in addedIndexes)
            {
                item.State = EntityIndexState.Ready;
            }

            IntPtr bkPtr;
            unsafe
            {
                byte* bk = stackalloc byte[5];
                bk[0] = KeyUtil.METACF_INDEX_STATE_PREFIX;
                uint* tiPtr = (uint*)(bk + 1);
                *tiPtr = KeyUtil.EncodeTableId(app.StoreId, model.TableId);
                bkPtr = new IntPtr(bk);
            }

            var req = new ClrScanRequire
            {
                RaftGroupId = KeyUtil.META_RAFTGROUP_ID,
                BeginKeyPtr = bkPtr,
                BeginKeySize = new IntPtr(5),
                EndKeyPtr = IntPtr.Zero,
                EndKeySize = IntPtr.Zero,
                FilterPtr = IntPtr.Zero,
                Skip = 0,
                Take = uint.MaxValue,
                DataCF = -1
            };
            IntPtr reqPtr;
            unsafe { reqPtr = new IntPtr(&req); }
            var res = await HostApi.ReadIndexByScanAsync(reqPtr);
            if (res == null || res.Length == 0) return;

            byte indexId = 0;
            unsafe
            {
                res.ForEachRow((kp, ks, vp, vs) =>
                {
                    var keyPtr = (byte*)kp.ToPointer();
                    var valPtr = (byte*)vp.ToPointer();
                    indexId = keyPtr[5];
                    var index = model.SysStoreOptions.Indexes.SingleOrDefault/*Single*/(t => t.IndexId == indexId);
                    if (index != null)
                        index.State = (EntityIndexState)valPtr[0];
                    else
                        Log.Warn($"查询索引构建状态发现不存在的索引{indexId}");
                });
            }

            res.Dispose();
        }
        #endregion

        #region ====模型代码及Assembly相关操作====
        /// <summary>
        /// Insert or Update模型相关的代码，目前主要用于服务模型及视图模型
        /// </summary>
        /// <param name="codeData">已经压缩编码过</param>
        internal static async ValueTask UpsertModelCodeAsync(ulong modelId, byte[] codeData, Transaction txn)
        {
            IntPtr keyPtr;
            int keySize = 9;
            IntPtr dataPtr = IntPtr.Zero;

            unsafe
            {
                byte* pk = stackalloc byte[keySize];
                KeyUtil.WriteModelCodeKey(pk, modelId);
                keyPtr = new IntPtr(pk);

                dataPtr = NativeApi.NewNativeString(codeData.Length, out byte* destDataPtr);
                fixed (byte* srcDataPtr = codeData)
                {
                    Buffer.MemoryCopy(srcDataPtr, destDataPtr, codeData.Length, codeData.Length);
                }
            }

            var req = new ClrInsertRequire
            {
                RaftGroupId = KeyUtil.META_RAFTGROUP_ID,
                KeyPtr = keyPtr,
                KeySize = new IntPtr(keySize),
                DataPtr = dataPtr,
                SchemaVersion = 0,
                OverrideIfExists = true,
                DataCF = -1
            };
            IntPtr reqPtr;
            unsafe { reqPtr = new IntPtr(&req); }
            await HostApi.ExecKVInsertAsync(txn.Handle, reqPtr);
        }

        internal static async ValueTask DeleteModelCodeAsync(ulong modelId, Transaction txn)
        {
            IntPtr keyPtr;
            int keySize = 9;
            unsafe
            {
                byte* pk = stackalloc byte[keySize];
                KeyUtil.WriteModelCodeKey(pk, modelId);
                keyPtr = new IntPtr(pk);
            }
            var req = new ClrDeleteRequire
            {
                RaftGroupId = KeyUtil.META_RAFTGROUP_ID,
                KeyPtr = keyPtr,
                KeySize = new IntPtr(keySize),
                SchemaVersion = 0,
                ReturnExists = false,
                DataCF = -1
            };
            IntPtr reqPtr;
            unsafe { reqPtr = new IntPtr(&req); }
            await HostApi.ExecKVDeleteAsync(txn.Handle, reqPtr);
        }

        /// <summary>
        /// 仅用于加载服务模型的代码
        /// </summary>
        internal static async ValueTask<string> LoadServiceCodeAsync(ulong modelId)
        {
            var res = await LoadModelCodeAsync(modelId);
            if (res == null)
                return null;

            ModelCodeUtil.DecodeServiceCode(res.DataPtr, (int)res.Size, out string sourceCode, out string declareCode);
            res.Dispose();
            return sourceCode;
        }

        /// <summary>
        /// 仅用于加载视图模型的代码
        /// </summary>
        internal static async ValueTask<ValueTuple<string, string, string>> LoadViewCodeAsync(ulong modelId)
        {
            var res = await LoadModelCodeAsync(modelId);
            if (res == null)
                return ValueTuple.Create<string, string, string>(null, null, null);

            ModelCodeUtil.DecodeViewCode(res.DataPtr, (int)res.Size, out string templateCode, out string scriptCode, out string styleCode);
            res.Dispose();
            return ValueTuple.Create(templateCode, scriptCode, styleCode);
        }

        private static ValueTask<INativeData> LoadModelCodeAsync(ulong modelId)
        {
            IntPtr keyPtr;
            int keySize = 9;
            unsafe
            {
                byte* pk = stackalloc byte[keySize];
                KeyUtil.WriteModelCodeKey(pk, modelId);
                keyPtr = new IntPtr(pk);
            }

            return StoreApi.Api.ReadIndexByGetAsync(KeyUtil.META_RAFTGROUP_ID, keyPtr, (uint)keySize);
        }

        /// <summary>
        /// 保存编译好的服务组件或视图运行时代码
        /// </summary>
        /// <param name="asmName">eg: sys.HelloService or sys.CustomerView</param>
        internal static ValueTask UpsertAssemblyAsync(bool isService, string asmName, byte[] asmData, Transaction txn)
        {
            IntPtr keyPtr;
            int keySize = EntityStoreWriter.CalcStringUtf8Size(asmName) + 1;
            IntPtr dataPtr = IntPtr.Zero;

            unsafe
            {
                byte* pk = stackalloc byte[keySize];
                KeyUtil.WriteAssemblyKey(isService, pk, asmName);
                keyPtr = new IntPtr(pk);

                dataPtr = NativeApi.NewNativeString(asmData.Length, out byte* destDataPtr);
                fixed (byte* srcDataPtr = asmData)
                {
                    Buffer.MemoryCopy(srcDataPtr, destDataPtr, asmData.Length, asmData.Length);
                }
            }

            var req = new ClrInsertRequire
            {
                RaftGroupId = KeyUtil.META_RAFTGROUP_ID,
                KeyPtr = keyPtr,
                KeySize = new IntPtr(keySize),
                DataPtr = dataPtr,
                SchemaVersion = 0,
                OverrideIfExists = true,
                DataCF = -1
            };
            IntPtr reqPtr;
            unsafe { reqPtr = new IntPtr(&req); }
            return HostApi.ExecKVInsertAsync(txn.Handle, reqPtr);
        }

        internal static async ValueTask DeleteAssemblyAsync(bool isService, string asmName, Transaction txn)
        {
            IntPtr keyPtr;
            int keySize = EntityStoreWriter.CalcStringUtf8Size(asmName) + 1;
            unsafe
            {
                byte* pk = stackalloc byte[keySize];
                KeyUtil.WriteAssemblyKey(isService, pk, asmName);
                keyPtr = new IntPtr(pk);
            }
            var req = new ClrDeleteRequire
            {
                RaftGroupId = KeyUtil.META_RAFTGROUP_ID,
                KeyPtr = keyPtr,
                KeySize = new IntPtr(keySize),
                SchemaVersion = 0,
                ReturnExists = false,
                DataCF = -1
            };
            IntPtr reqPtr;
            unsafe { reqPtr = new IntPtr(&req); }
            await HostApi.ExecKVDeleteAsync(txn.Handle, reqPtr);
        }

        /// <summary>
        /// 加载服务组件
        /// </summary>
        /// <param name="serviceName">eg: sys.HelloService</param>
        /// <returns></returns>
        internal static async ValueTask<byte[]> LoadServiceAssemblyAsync(string serviceName) //TODO:考虑保存至本地文件，返回路径
        {
#if !DEBUG
            //注意：只允许服务域调用
            //if (Runtime.RuntimeContext.Current.RuntimeId == 0)
            //    throw new NotSupportedException("不支持服务端主进程加载");
#endif
            var res = await LoadAssemblyAsync(true, serviceName);
            if (res == null)
                return null;

            var data = new byte[res.Size];
            unsafe
            {
                fixed (byte* pd = data)
                {
                    Buffer.MemoryCopy(res.DataPtr.ToPointer(), pd, (int)res.Size, (int)res.Size);
                }
            }
            res.Dispose();
            return data;
        }

        internal static async ValueTask<string> LoadViewAssemblyAsync(string viewName)
        {
            var res = await LoadAssemblyAsync(false, viewName);
            if (res == null)
                return null;

            ModelCodeUtil.DecodeViewRuntimeCode(res.DataPtr, (int)res.Size, out string runtimeCode);
            res.Dispose();
            return runtimeCode;
        }

        private static ValueTask<INativeData> LoadAssemblyAsync(bool isService, string asmName)
        {
            IntPtr keyPtr;
            int keySize = EntityStoreWriter.CalcStringUtf8Size(asmName) + 1;
            unsafe
            {
                byte* pk = stackalloc byte[keySize];
                KeyUtil.WriteAssemblyKey(isService, pk, asmName);
                keyPtr = new IntPtr(pk);
            }

            return StoreApi.Api.ReadIndexByGetAsync(KeyUtil.META_RAFTGROUP_ID, keyPtr, (uint)keySize);
        }
        #endregion

        #region ====视图模型路由相关====
        /// <summary>
        /// 保存视图模型路由表
        /// </summary>
        /// <param name="viewName">eg: sys.CustomerList</param>
        /// <param name="path">无自定义路由为空</param>
        internal static ValueTask UpsertViewRoute(string viewName, string path, Transaction txn)
        {
            //TODO:简化Key与Value编码,直接utf8,各减去3字节字符数标记
            IntPtr keyPtr;
            int keySize = EntityStoreWriter.CalcStringUtf8Size(viewName) + 1;
            IntPtr dataPtr = IntPtr.Zero;
            int valueSize = EntityStoreWriter.CalcStringUtf8Size(path);

            unsafe
            {
                byte* pk = stackalloc byte[keySize];
                KeyUtil.WriteViewRouteKey(pk, viewName);
                keyPtr = new IntPtr(pk);

                if (!string.IsNullOrEmpty(path))
                {
                    dataPtr = NativeApi.NewNativeString(valueSize, out byte* destDataPtr);
                    var sr = new EntityStoreWriter(destDataPtr, 0);
                    sr.WriteString(path, null);
                }
            }

            var req = new ClrInsertRequire
            {
                RaftGroupId = KeyUtil.META_RAFTGROUP_ID,
                KeyPtr = keyPtr,
                KeySize = new IntPtr(keySize),
                DataPtr = dataPtr,
                SchemaVersion = 0,
                OverrideIfExists = true,
                DataCF = -1
            };
            IntPtr reqPtr;
            unsafe { reqPtr = new IntPtr(&req); }
            return HostApi.ExecKVInsertAsync(txn.Handle, reqPtr);
        }

        internal static async ValueTask DeleteViewRoute(string viewName, Transaction txn)
        {
            IntPtr keyPtr;
            int keySize = EntityStoreWriter.CalcStringUtf8Size(viewName) + 1;

            unsafe
            {
                byte* pk = stackalloc byte[keySize];
                KeyUtil.WriteViewRouteKey(pk, viewName);
                keyPtr = new IntPtr(pk);
            }

            var req = new ClrDeleteRequire
            {
                RaftGroupId = KeyUtil.META_RAFTGROUP_ID,
                KeyPtr = keyPtr,
                KeySize = new IntPtr(keySize),
                SchemaVersion = 0,
                ReturnExists = false,
                DataCF = -1
            };
            IntPtr reqPtr;
            unsafe { reqPtr = new IntPtr(&req); }
            await HostApi.ExecKVDeleteAsync(txn.Handle, reqPtr);
        }

        internal static async ValueTask<ValueTuple<string, string>[]> LoadViewRoutes()
        {
            byte beginKey = KeyUtil.METACF_VIEW_ROUTER_PREFIX;
            IntPtr beginKeyPtr;
            unsafe { beginKeyPtr = new IntPtr(&beginKey); }

            var req = new ClrScanRequire
            {
                RaftGroupId = KeyUtil.META_RAFTGROUP_ID,
                BeginKeyPtr = beginKeyPtr,
                BeginKeySize = new IntPtr(1),
                EndKeyPtr = IntPtr.Zero,
                EndKeySize = IntPtr.Zero,
                FilterPtr = IntPtr.Zero,
                Skip = 0,
                Take = uint.MaxValue,
                DataCF = -1
            };
            IntPtr reqPtr;
            unsafe { reqPtr = new IntPtr(&req); }
            var scanRes = await HostApi.ReadIndexByScanAsync(reqPtr);
            if (scanRes == null || scanRes.Length == 0)
                return null;

            var routes = new ValueTuple<string, string>[scanRes.Length];
            var index = 0;
            scanRes.ForEachRow((kp, ks, vp, vs) =>
            {
                unsafe
                {
                    string key = new string((sbyte*)kp.ToPointer(), 1, ks - 1, System.Text.Encoding.UTF8);
                    string value = null;
                    if (vs > 0)
                        value = new string((sbyte*)vp.ToPointer(), 0, vs, System.Text.Encoding.UTF8);
                    routes[index] = ValueTuple.Create(key, value);
                }
                index++;
            });
            scanRes.Dispose();

            return routes;
        }
        #endregion
    }
}
