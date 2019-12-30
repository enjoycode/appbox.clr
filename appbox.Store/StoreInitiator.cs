using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using appbox.Data;
using appbox.Models;
using System.Diagnostics;

namespace appbox.Store
{
    /// <summary>
    /// 存储初始化器，仅用于启动集群第一节点时初始化存储
    /// </summary>
    static class StoreInitiator
    {
#if !FUTURE
        private const ushort PK_Member_Id = 0; //暂为0
#endif

        internal static async Task InitAsync(
#if !FUTURE
            System.Data.Common.DbTransaction txn
#endif
            )
        {
            //TODO:判断是否已初始化
            //新建sys ApplicationModel
            var app = new ApplicationModel("appbox", Consts.SYS);
#if FUTURE
            await ModelStore.CreateApplicationAsync(app);
#else
            await ModelStore.CreateApplicationAsync(app, txn);
#endif
            //新建默认文件夹
            var entityRootFolder = new ModelFolder(app.Id, ModelType.Entity);
            var entityOrgUnitsFolder = new ModelFolder(entityRootFolder, "OrgUnits");
            var entityDesignFolder = new ModelFolder(entityRootFolder, "Design");
            var viewRootFolder = new ModelFolder(app.Id, ModelType.View);
            var viewOrgUnitsFolder = new ModelFolder(viewRootFolder, "OrgUnits");
            var viewOperationFolder = new ModelFolder(viewRootFolder, "Operations");
            var viewMetricsFolder = new ModelFolder(viewOperationFolder, "Metrics");
            var viewClusterFolder = new ModelFolder(viewOperationFolder, "Cluster");

            //新建EntityModel
            var emploee = CreateEmploeeModel(app);
            emploee.FolderId = entityOrgUnitsFolder.Id;
            var enterprise = CreateEnterpriseModel(app);
            enterprise.FolderId = entityOrgUnitsFolder.Id;
            var workgroup = CreateWorkgroupModel(app);
            workgroup.FolderId = entityOrgUnitsFolder.Id;
            var orgunit = CreateOrgUnitModel(app);
            orgunit.FolderId = entityOrgUnitsFolder.Id;
            var staged = CreateStagedModel(app);
            staged.FolderId = entityDesignFolder.Id;
            var checkout = CreateCheckoutModel(app);
            checkout.FolderId = entityDesignFolder.Id;

            //新建默认组织

            var defaultEnterprise = new Entity(enterprise);
#if !FUTURE
            defaultEnterprise.SetGuid(PK_Member_Id, Guid.NewGuid());
#endif
            defaultEnterprise.SetString(Consts.ENTERPRISE_NAME_ID, "Future Studio");

            //新建默认系统管理员及测试账号
            var admin = new Entity(emploee);
#if !FUTURE
            admin.SetGuid(PK_Member_Id, Guid.NewGuid());
#endif
            admin.SetString(Consts.EMPLOEE_NAME_ID, "Admin");
            admin.SetString(Consts.EMPLOEE_ACCOUNT_ID, "Admin");
            admin.SetBytes(Consts.EMPLOEE_PASSWORD_ID, Runtime.RuntimeContext.PasswordHasher.HashPassword("760wb"));
            admin.SetBoolean(Consts.EMPLOEE_MALE_ID, true);
            admin.SetDateTime(Consts.EMPLOEE_BIRTHDAY_ID, new DateTime(1977, 3, 16));


            var test = new Entity(emploee);
#if !FUTURE
            test.SetGuid(PK_Member_Id, Guid.NewGuid());
#endif
            test.SetString(Consts.EMPLOEE_NAME_ID, "Test");
            test.SetString(Consts.EMPLOEE_ACCOUNT_ID, "Test");
            test.SetBytes(Consts.EMPLOEE_PASSWORD_ID, Runtime.RuntimeContext.PasswordHasher.HashPassword("la581"));
            test.SetBoolean(Consts.EMPLOEE_MALE_ID, false);
            test.SetDateTime(Consts.EMPLOEE_BIRTHDAY_ID, new DateTime(1979, 1, 2));

            var itdept = new Entity(workgroup);
#if !FUTURE
            itdept.SetGuid(PK_Member_Id, Guid.NewGuid());
#endif
            itdept.SetString(Consts.WORKGROUP_NAME_ID, "IT Dept");

            //新建默认组织单元
            var entou = new Entity(orgunit);
#if !FUTURE
            entou.SetGuid(PK_Member_Id, Guid.NewGuid());
#endif
            entou.SetString(Consts.ORGUNIT_NAME_ID, defaultEnterprise.GetString(Consts.ENTERPRISE_NAME_ID));
            entou.SetUInt64(Consts.ORGUNIT_BASETYPE_ID, Consts.SYS_ENTERPRISE_MODEL_ID);
#if FUTURE
            entou.SetEntityId(Consts.ORGUNIT_BASEID_ID, defaultEnterprise.Id);
#else
            entou.SetGuid(Consts.ORGUNIT_BASEID_ID, defaultEnterprise.GetGuid(PK_Member_Id));
#endif

            var itdeptou = new Entity(orgunit);
#if !FUTURE
            itdeptou.SetGuid(PK_Member_Id, Guid.NewGuid());
#endif
            itdeptou.SetString(Consts.ORGUNIT_NAME_ID, itdept.GetString(Consts.WORKGROUP_NAME_ID));
            itdeptou.SetUInt64(Consts.ORGUNIT_BASETYPE_ID, Consts.SYS_WORKGROUP_MODEL_ID);
#if FUTURE
            itdeptou.SetEntityId(Consts.ORGUNIT_BASEID_ID, itdept.Id);
            itdeptou.SetEntityId(Consts.ORGUNIT_PARENTID_ID, entou.Id);
#else
            itdeptou.SetGuid(Consts.ORGUNIT_BASEID_ID, itdept.GetGuid(PK_Member_Id));
            itdeptou.SetGuid(Consts.ORGUNIT_PARENTID_ID, entou.GetGuid(PK_Member_Id));
#endif

            var adminou = new Entity(orgunit);
#if !FUTURE
            adminou.SetGuid(PK_Member_Id, Guid.NewGuid());
#endif
            adminou.SetString(Consts.ORGUNIT_NAME_ID, admin.GetString(Consts.EMPLOEE_NAME_ID));
            adminou.SetUInt64(Consts.ORGUNIT_BASETYPE_ID, Consts.SYS_EMPLOEE_MODEL_ID);
#if FUTURE
            adminou.SetEntityId(Consts.ORGUNIT_BASEID_ID, admin.Id);
            adminou.SetEntityId(Consts.ORGUNIT_PARENTID_ID, itdeptou.Id);
#else
            adminou.SetGuid(Consts.ORGUNIT_BASEID_ID, admin.GetGuid(PK_Member_Id));
            adminou.SetGuid(Consts.ORGUNIT_PARENTID_ID, itdeptou.GetGuid(PK_Member_Id));
#endif

            var testou = new Entity(orgunit);
#if !FUTURE
            testou.SetGuid(PK_Member_Id, Guid.NewGuid());
#endif
            testou.SetString(Consts.ORGUNIT_NAME_ID, test.GetString(Consts.EMPLOEE_NAME_ID));
            testou.SetUInt64(Consts.ORGUNIT_BASETYPE_ID, Consts.SYS_EMPLOEE_MODEL_ID);
#if FUTURE
            testou.SetEntityId(Consts.ORGUNIT_BASEID_ID, test.Id);
            testou.SetEntityId(Consts.ORGUNIT_PARENTID_ID, itdeptou.Id);
#else
            testou.SetGuid(Consts.ORGUNIT_BASEID_ID, test.GetGuid(PK_Member_Id));
            testou.SetGuid(Consts.ORGUNIT_PARENTID_ID, itdeptou.GetGuid(PK_Member_Id));
#endif

            //事务保存
#if FUTURE
            var txn = await Transaction.BeginAsync();
#endif
            await ModelStore.UpsertFolderAsync(entityRootFolder, txn);
            await ModelStore.UpsertFolderAsync(viewRootFolder, txn);

            await ModelStore.InsertModelAsync(emploee, txn);
            await ModelStore.InsertModelAsync(enterprise, txn);
            await ModelStore.InsertModelAsync(workgroup, txn);
            await ModelStore.InsertModelAsync(orgunit, txn);
            await ModelStore.InsertModelAsync(staged, txn);
            await ModelStore.InsertModelAsync(checkout, txn);

            await CreateServiceModel("OrgUnitService", 1, null, txn);
            await CreateServiceModel("MetricService", 2, null, txn, new List<string> { "Newtonsoft.Json", "System.Private.Uri", "System.Net.Http" });

            await CreateViewModel("Home", 1, null, txn);
            await CreateViewModel("EnterpriseView", 2, viewOrgUnitsFolder.Id, txn);
            await CreateViewModel("WorkgroupView", 3, viewOrgUnitsFolder.Id, txn);
            await CreateViewModel("EmploeeView", 4, viewOrgUnitsFolder.Id, txn);
            await CreateViewModel("PermissionTree", 5, viewOrgUnitsFolder.Id, txn);
            await CreateViewModel("OrgUnits", 6, viewOrgUnitsFolder.Id, txn);

            await CreateViewModel("CpuUsages", 7, viewMetricsFolder.Id, txn);
            await CreateViewModel("MemUsages", 8, viewMetricsFolder.Id, txn);
            await CreateViewModel("NetTraffic", 9, viewMetricsFolder.Id, txn);
            await CreateViewModel("DiskIO", 10, viewMetricsFolder.Id, txn);
            await CreateViewModel("NodeMetrics", 11, viewMetricsFolder.Id, txn);
            await CreateViewModel("InvokeMetrics", 12, viewMetricsFolder.Id, txn);

            await CreateViewModel("GaugeCard", 13, viewClusterFolder.Id, txn);
            await CreateViewModel("NodesListView", 14, viewClusterFolder.Id, txn);
            await CreateViewModel("PartsListView", 15, viewClusterFolder.Id, txn);
            await CreateViewModel("ClusterHome", 16, viewClusterFolder.Id, txn);

            await CreateViewModel("OpsLogin", 17, viewOperationFolder.Id, txn, "ops");
            await CreateViewModel("OpsHome", 18, viewOperationFolder.Id, txn);

            //插入数据前先设置模型缓存，以防止找不到
            var runtime = (Runtime.IHostRuntimeContext)Runtime.RuntimeContext.Current;
            runtime.AddModelCache(emploee);
            runtime.AddModelCache(enterprise);
            runtime.AddModelCache(workgroup);
            runtime.AddModelCache(orgunit);
            runtime.AddModelCache(staged);
            runtime.AddModelCache(checkout);

#if FUTURE
            await EntityStore.InsertEntityAsync(defaultEnterprise, txn);
            await EntityStore.InsertEntityAsync(itdept, txn);
            await EntityStore.InsertEntityAsync(admin, txn);
            await EntityStore.InsertEntityAsync(test, txn);
            await EntityStore.InsertEntityAsync(entou, txn);
            await EntityStore.InsertEntityAsync(itdeptou, txn);
            await EntityStore.InsertEntityAsync(adminou, txn);
            await EntityStore.InsertEntityAsync(testou, txn);
#else
            var ctx = new InitDesignContext(app);
            ctx.AddEntityModel(emploee);
            ctx.AddEntityModel(enterprise);
            ctx.AddEntityModel(workgroup);
            ctx.AddEntityModel(orgunit);
            ctx.AddEntityModel(staged);
            ctx.AddEntityModel(checkout);

            await SqlStore.Default.CreateTableAsync(emploee, txn, ctx);
            await SqlStore.Default.CreateTableAsync(enterprise, txn, ctx);
            await SqlStore.Default.CreateTableAsync(workgroup, txn, ctx);
            await SqlStore.Default.CreateTableAsync(orgunit, txn, ctx);
            await SqlStore.Default.CreateTableAsync(staged, txn, ctx);
            await SqlStore.Default.CreateTableAsync(checkout, txn, ctx);

            await SqlStore.Default.InsertAsync(defaultEnterprise, txn);
            await SqlStore.Default.InsertAsync(itdept, txn);
            await SqlStore.Default.InsertAsync(admin, txn);
            await SqlStore.Default.InsertAsync(test, txn);
            await SqlStore.Default.InsertAsync(entou, txn);
            await SqlStore.Default.InsertAsync(itdeptou, txn);
            await SqlStore.Default.InsertAsync(adminou, txn);
            await SqlStore.Default.InsertAsync(testou, txn);
#endif

            //添加权限模型在保存OU实例之后
            var admin_permission = new PermissionModel(Consts.SYS_PERMISSION_ADMIN_ID, "Admin");
            admin_permission.Remark = "System administrator";
#if FUTURE
            admin_permission.OrgUnits.Add(adminou.Id);
#else
            admin_permission.OrgUnits.Add(adminou.GetGuid(PK_Member_Id));
#endif
            var developer_permission = new PermissionModel(Consts.SYS_PERMISSION_DEVELOPER_ID, "Developer");
            developer_permission.Remark = "System developer";
#if FUTURE
            developer_permission.OrgUnits.Add(itdeptou.Id);
#else
            developer_permission.OrgUnits.Add(itdeptou.GetGuid(PK_Member_Id));
#endif
            await ModelStore.InsertModelAsync(admin_permission, txn);
            await ModelStore.InsertModelAsync(developer_permission, txn);

#if FUTURE
            await txn.CommitAsync();
#endif
        }

        private static EntityModel CreateEmploeeModel(ApplicationModel app)
        {
#if FUTURE
            var emploee = new EntityModel(Consts.SYS_EMPLOEE_MODEL_ID, Consts.EMPLOEE, EntityStoreType.StoreWithMvcc);
#else
            var emploee = new EntityModel(Consts.SYS_EMPLOEE_MODEL_ID, Consts.EMPLOEE, SqlStore.DefaultSqlStoreId);
            var id = new DataFieldModel(emploee, "Id", EntityFieldType.Guid);
            emploee.AddSysMember(id, PK_Member_Id);
            //add pk
            emploee.SqlStoreOptions.SetPrimaryKeys(emploee, new List<FieldWithOrder> { new FieldWithOrder { MemberId = id.MemberId } });
#endif
            //Add members
            var name = new DataFieldModel(emploee, Consts.NAME, EntityFieldType.String);
#if !FUTURE
            name.Length = 20;
#endif
            emploee.AddSysMember(name, Consts.EMPLOEE_NAME_ID);

            var male = new DataFieldModel(emploee, "Male", EntityFieldType.Boolean);
            emploee.AddSysMember(male, Consts.EMPLOEE_MALE_ID);

            var birthday = new DataFieldModel(emploee, "Birthday", EntityFieldType.DateTime);
            emploee.AddSysMember(birthday, Consts.EMPLOEE_BIRTHDAY_ID);
            birthday.SetDefaultValue("1977-03-16");

            var account = new DataFieldModel(emploee, Consts.ACCOUNT, EntityFieldType.String);
            account.AllowNull = true;
            emploee.AddSysMember(account, Consts.EMPLOEE_ACCOUNT_ID);

            var password = new DataFieldModel(emploee, Consts.PASSWORD, EntityFieldType.Binary);
            password.AllowNull = true;
            emploee.AddSysMember(password, Consts.EMPLOEE_PASSWORD_ID);

            var orgunits = new EntitySetModel(emploee, "OrgUnits", Consts.SYS_ORGUNIT_MODEL_ID, Consts.ORGUNIT_BASE_ID);
            emploee.AddSysMember(orgunits, Consts.EMPLOEE_ORGUNITS_ID);

            //Add indexes
#if FUTURE
            var ui_account = new EntityIndexModel(emploee, "UI_Account", true,
                                                       new FieldWithOrder[] { new FieldWithOrder(Consts.EMPLOEE_ACCOUNT_ID) },
                                                       new ushort[] { Consts.EMPLOEE_PASSWORD_ID });
            emploee.SysStoreOptions.AddSysIndex(emploee, ui_account, Consts.EMPLOEE_UI_ACCOUNT_ID);
#else
            var ui_account = new SqlIndexModel(emploee, "UI_Account", true,
                                                        new FieldWithOrder[] { new FieldWithOrder(Consts.EMPLOEE_ACCOUNT_ID) },
                                                        new ushort[] { Consts.EMPLOEE_PASSWORD_ID });
            emploee.SqlStoreOptions.AddIndex(emploee, ui_account);
#endif

            return emploee;
        }

        private static EntityModel CreateEnterpriseModel(ApplicationModel app)
        {
#if FUTURE
            var model = new EntityModel(Consts.SYS_ENTERPRISE_MODEL_ID, Consts.ENTERPRISE, EntityStoreType.StoreWithMvcc);
#else
            var model = new EntityModel(Consts.SYS_ENTERPRISE_MODEL_ID, Consts.ENTERPRISE, SqlStore.DefaultSqlStoreId);
            var id = new DataFieldModel(model, "Id", EntityFieldType.Guid);
            model.AddSysMember(id, PK_Member_Id);
            //add pk
            model.SqlStoreOptions.SetPrimaryKeys(model, new List<FieldWithOrder> { new FieldWithOrder { MemberId = id.MemberId } });
#endif

            var name = new DataFieldModel(model, Consts.NAME, EntityFieldType.String);
#if !FUTURE
            name.Length = 100;
#endif
            model.AddSysMember(name, Consts.ENTERPRISE_NAME_ID);

            var address = new DataFieldModel(model, "Address", EntityFieldType.String);
            address.AllowNull = true;
            model.AddSysMember(address, Consts.ENTERPRISE_ADDRESS_ID);

            return model;
        }

        private static EntityModel CreateWorkgroupModel(ApplicationModel app)
        {
#if FUTURE
            var model = new EntityModel(Consts.SYS_WORKGROUP_MODEL_ID, Consts.WORKGROUP, EntityStoreType.StoreWithMvcc);
#else
            var model = new EntityModel(Consts.SYS_WORKGROUP_MODEL_ID, Consts.WORKGROUP, SqlStore.DefaultSqlStoreId);
            var id = new DataFieldModel(model, "Id", EntityFieldType.Guid);
            model.AddSysMember(id, PK_Member_Id);
            //add pk
            model.SqlStoreOptions.SetPrimaryKeys(model, new List<FieldWithOrder> { new FieldWithOrder { MemberId = id.MemberId } });
#endif

            var name = new DataFieldModel(model, Consts.NAME, EntityFieldType.String);
#if !FUTURE
            name.Length = 50;
#endif
            model.AddSysMember(name, Consts.WORKGROUP_NAME_ID);

            return model;
        }

        private static EntityModel CreateOrgUnitModel(ApplicationModel app)
        {
            EntityFieldType fkType;
#if FUTURE
            var model = new EntityModel(Consts.SYS_ORGUNIT_MODEL_ID, Consts.ORGUNIT, EntityStoreType.StoreWithMvcc);
            fkType = EntityFieldType.EntityId;
#else
            fkType = EntityFieldType.Guid;
            var model = new EntityModel(Consts.SYS_ORGUNIT_MODEL_ID, Consts.ORGUNIT, SqlStore.DefaultSqlStoreId);
            var id = new DataFieldModel(model, "Id", EntityFieldType.Guid);
            model.AddSysMember(id, PK_Member_Id);
            //add pk
            model.SqlStoreOptions.SetPrimaryKeys(model, new List<FieldWithOrder> { new FieldWithOrder { MemberId = id.MemberId } });
#endif

            var name = new DataFieldModel(model, Consts.NAME, EntityFieldType.String);
#if !FUTURE
            name.Length = 100;
#endif
            model.AddSysMember(name, Consts.ORGUNIT_NAME_ID);

            var baseId = new DataFieldModel(model, "BaseId", fkType, true);
            model.AddSysMember(baseId, Consts.ORGUNIT_BASEID_ID);
            var baseType = new DataFieldModel(model, "BaseType", EntityFieldType.UInt64, true);
            model.AddSysMember(baseType, Consts.ORGUNIT_BASETYPE_ID);
            var Base = new EntityRefModel(model, "Base",
                new List<ulong>() { Consts.SYS_ENTERPRISE_MODEL_ID, Consts.SYS_WORKGROUP_MODEL_ID, Consts.SYS_EMPLOEE_MODEL_ID },
                new ushort[] { baseId.MemberId }, baseType.MemberId);
            model.AddSysMember(Base, Consts.ORGUNIT_BASE_ID);

            var parentId = new DataFieldModel(model, "ParentId", fkType, true);
            parentId.AllowNull = true;
            model.AddSysMember(parentId, Consts.ORGUNIT_PARENTID_ID);
            var parent = new EntityRefModel(model, "Parent", Consts.SYS_ORGUNIT_MODEL_ID, new ushort[] { parentId.MemberId });
            parent.AllowNull = true;
            model.AddSysMember(parent, Consts.ORGUNIT_PARENT_ID);

            var childs = new EntitySetModel(model, "Childs", Consts.SYS_ORGUNIT_MODEL_ID, parent.MemberId);
            model.AddSysMember(childs, Consts.ORGUNIT_CHILDS_ID);

            return model;
        }

        private static EntityModel CreateStagedModel(ApplicationModel app)
        {
#if FUTURE
            var model = new EntityModel(Consts.SYS_STAGED_MODEL_ID, "StagedModel", EntityStoreType.StoreWithoutMvcc);
#else
            var model = new EntityModel(Consts.SYS_STAGED_MODEL_ID, "StagedModel", SqlStore.DefaultSqlStoreId);
#endif

            var type = new DataFieldModel(model, "Type", EntityFieldType.Byte);
            model.AddSysMember(type, Consts.STAGED_TYPE_ID);

            var modelId = new DataFieldModel(model, "ModelId", EntityFieldType.String); //暂用String
#if !FUTURE
            modelId.Length = 100;
#endif
            model.AddSysMember(modelId, Consts.STAGED_MODELID_ID);

            var devId = new DataFieldModel(model, "DeveloperId", EntityFieldType.Guid);
            model.AddSysMember(devId, Consts.STAGED_DEVELOPERID_ID);

            var data = new DataFieldModel(model, "Data", EntityFieldType.Binary);
            data.AllowNull = true;
            model.AddSysMember(data, Consts.STAGED_DATA_ID);

#if !FUTURE
            //add pk
            model.SqlStoreOptions.SetPrimaryKeys(model, new List<FieldWithOrder>
            {
                new FieldWithOrder { MemberId = devId.MemberId },
                new FieldWithOrder { MemberId = type.MemberId },
                new FieldWithOrder { MemberId = modelId.MemberId }
            });
#endif

            return model;
        }

        private static EntityModel CreateCheckoutModel(ApplicationModel app)
        {
#if FUTURE
            var model = new EntityModel(Consts.SYS_CHECKOUT_MODEL_ID, "Checkout", EntityStoreType.StoreWithoutMvcc);
#else
            var model = new EntityModel(Consts.SYS_CHECKOUT_MODEL_ID, "Checkout", SqlStore.DefaultSqlStoreId);
#endif

            var nodeType = new DataFieldModel(model, "NodeType", EntityFieldType.Byte);
            model.AddSysMember(nodeType, Consts.CHECKOUT_NODETYPE_ID);

            var targetId = new DataFieldModel(model, "TargetId", EntityFieldType.String);
#if !FUTURE
            targetId.Length = 100;
#endif
            model.AddSysMember(targetId, Consts.CHECKOUT_TARGETID_ID);

            var devId = new DataFieldModel(model, "DeveloperId", EntityFieldType.Guid);
            model.AddSysMember(devId, Consts.CHECKOUT_DEVELOPERID_ID);

            var devName = new DataFieldModel(model, "DeveloperName", EntityFieldType.String);
#if !FUTURE
            devName.Length = 100;
#endif
            model.AddSysMember(devName, Consts.CHECKOUT_DEVELOPERNAME_ID);

            var version = new DataFieldModel(model, "Version", EntityFieldType.Int32); //TODO:UInt32
            model.AddSysMember(version, Consts.CHECKOUT_VERSION_ID);

            //Add indexes
#if FUTURE
            var ui_nodeType_targetId = new EntityIndexModel(model, "UI_NodeType_TargetId", true,
                                                            new FieldWithOrder[]
            {
                new FieldWithOrder(Consts.CHECKOUT_NODETYPE_ID),
                new FieldWithOrder(Consts.CHECKOUT_TARGETID_ID)
            });
            model.SysStoreOptions.AddSysIndex(model, ui_nodeType_targetId, Consts.CHECKOUT_UI_NODETYPE_TARGETID_ID);
#else
            var ui_nodeType_targetId = new SqlIndexModel(model, "UI_NodeType_TargetId", true,
                                                            new FieldWithOrder[]
            {
                new FieldWithOrder(Consts.CHECKOUT_NODETYPE_ID),
                new FieldWithOrder(Consts.CHECKOUT_TARGETID_ID)
            });
            model.SqlStoreOptions.AddIndex(model, ui_nodeType_targetId);

            //add pk
            model.SqlStoreOptions.SetPrimaryKeys(model, new List<FieldWithOrder>
            {
                new FieldWithOrder { MemberId = devId.MemberId },
                new FieldWithOrder { MemberId = nodeType.MemberId },
                new FieldWithOrder { MemberId = targetId.MemberId }
            });
#endif
            return model;
        }

        private static async Task CreateServiceModel(string name, ulong idIndex, Guid? folderId,
#if FUTURE
            Transaction txn,
#else
            System.Data.Common.DbTransaction txn,
#endif
            List<string> references = null)
        {
            var modelId = ((ulong)Consts.SYS_APP_ID << IdUtil.MODELID_APPID_OFFSET)
                | ((ulong)ModelType.Service << IdUtil.MODELID_TYPE_OFFSET) | (idIndex << IdUtil.MODELID_SEQ_OFFSET);
            var model = new ServiceModel(modelId, name) { FolderId = folderId };
            if (references != null)
                model.References.AddRange(references);
            await ModelStore.InsertModelAsync(model, txn);

            var serviceCode = Resources.GetString($"Resources.Services.{name}.cs");
            var codeData = ModelCodeUtil.EncodeServiceCode(serviceCode, null);
            await ModelStore.UpsertModelCodeAsync(model.Id, codeData, txn);

            var asmData = Resources.GetBytes($"Resources.Services.{name}.dll");
            await ModelStore.UpsertAssemblyAsync(true, $"sys.{name}", asmData, txn);
        }

        private static async Task CreateViewModel(string name, ulong idIndex, Guid? folderId,
#if FUTURE
            Transaction txn,
#else
            System.Data.Common.DbTransaction txn,
#endif
            string routePath = null)
        {
            var modelId = ((ulong)Consts.SYS_APP_ID << IdUtil.MODELID_APPID_OFFSET)
                | ((ulong)ModelType.View << IdUtil.MODELID_TYPE_OFFSET) | (idIndex << IdUtil.MODELID_SEQ_OFFSET);
            var model = new ViewModel(modelId, name) { FolderId = folderId };
            if (!string.IsNullOrEmpty(routePath))
            {
                model.Flag = ViewModelFlag.ListInRouter;
                model.RoutePath = routePath;
                await ModelStore.UpsertViewRoute($"sys.{model.Name}", model.RoutePath, txn);
            }
            await ModelStore.InsertModelAsync(model, txn);

            var templateCode = Resources.GetString($"Resources.Views.{name}.html");
            var scriptCode = Resources.GetString($"Resources.Views.{name}.js");
            var styleCode = Resources.GetString($"Resources.Views.{name}.css");
            var codeData = ModelCodeUtil.EncodeViewCode(templateCode, scriptCode, styleCode);
            await ModelStore.UpsertModelCodeAsync(model.Id, codeData, txn);

            var runtimeCode = Resources.GetString($"Resources.Views.{name}.json");
            var runtimeCodeData = ModelCodeUtil.EncodeViewRuntimeCode(runtimeCode);
            await ModelStore.UpsertAssemblyAsync(false, $"sys.{name}", runtimeCodeData, txn);
        }
    }

#if !FUTURE
    /// <summary>
    /// 仅用于初始化默认存储
    /// </summary>
    sealed class InitDesignContext : Design.IDesignContext
    {
        private readonly ApplicationModel _sysApp;
        private readonly Dictionary<ulong, EntityModel> _models;

        public InitDesignContext(ApplicationModel app)
        {
            _sysApp = app;
            _models = new Dictionary<ulong, EntityModel>(8);
        }

        internal void AddEntityModel(EntityModel model)
        {
            _models.Add(model.Id, model);
        }

        public ApplicationModel GetApplicationModel(uint appId)
        {
            Debug.Assert(_sysApp.Id == appId);
            return _sysApp;
        }

        public EntityModel GetEntityModel(ulong modelID)
        {
            return _models[modelID];
        }
    }
#endif

}
