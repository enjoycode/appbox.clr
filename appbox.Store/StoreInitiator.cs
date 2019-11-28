using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using appbox.Data;
using appbox.Models;

namespace appbox.Store
{
    /// <summary>
    /// 存储初始化器，仅用于启动集群第一节点时初始化存储
    /// </summary>
    static class StoreInitiator
    {
        internal static async Task InitAsync()
        {
            //TODO:判断是否已初始化
            //新建sys ApplicationModel
            var app = new ApplicationModel("appbox", Consts.SYS);
            await ModelStore.CreateApplicationAsync(app);

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
            defaultEnterprise.SetString(Consts.ENTERPRISE_NAME_ID, "Future Studio");

            //新建默认系统管理员及测试账号
            var admin = new Entity(emploee);
            admin.SetString(Consts.EMPLOEE_NAME_ID, "Admin");
            admin.SetString(Consts.EMPLOEE_ACCOUNT_ID, "Admin");
            admin.SetBytes(Consts.EMPLOEE_PASSWORD_ID, Runtime.RuntimeContext.PasswordHasher.HashPassword("760wb"));
            admin.SetBoolean(Consts.EMPLOEE_MALE_ID, true);
            admin.SetDateTime(Consts.EMPLOEE_BIRTHDAY_ID, new DateTime(1977, 3, 16));

            var test = new Entity(emploee);
            test.SetString(Consts.EMPLOEE_NAME_ID, "Test");
            test.SetString(Consts.EMPLOEE_ACCOUNT_ID, "Test");
            test.SetBytes(Consts.EMPLOEE_PASSWORD_ID, Runtime.RuntimeContext.PasswordHasher.HashPassword("la581"));
            test.SetBoolean(Consts.EMPLOEE_MALE_ID, false);
            test.SetDateTime(Consts.EMPLOEE_BIRTHDAY_ID, new DateTime(1979, 1, 2));

            var itdept = new Entity(workgroup);
            itdept.SetString(Consts.WORKGROUP_NAME_ID, "IT Dept");

            //新建默认组织单元
            var entou = new Entity(orgunit);
            entou.SetString(Consts.ORGUNIT_NAME_ID, defaultEnterprise.GetString(Consts.ENTERPRISE_NAME_ID));
            entou.SetUInt64(Consts.ORGUNIT_BASETYPE_ID, Consts.SYS_ENTERPRISE_MODEL_ID);
            entou.SetEntityId(Consts.ORGUNIT_BASEID_ID, defaultEnterprise.Id);

            var itdeptou = new Entity(orgunit);
            itdeptou.SetString(Consts.ORGUNIT_NAME_ID, itdept.GetString(Consts.WORKGROUP_NAME_ID));
            itdeptou.SetUInt64(Consts.ORGUNIT_BASETYPE_ID, Consts.SYS_WORKGROUP_MODEL_ID);
            itdeptou.SetEntityId(Consts.ORGUNIT_BASEID_ID, itdept.Id);
            itdeptou.SetEntityId(Consts.ORGUNIT_PARENTID_ID, entou.Id);

            var adminou = new Entity(orgunit);
            adminou.SetString(Consts.ORGUNIT_NAME_ID, admin.GetString(Consts.EMPLOEE_NAME_ID));
            adminou.SetUInt64(Consts.ORGUNIT_BASETYPE_ID, Consts.SYS_EMPLOEE_MODEL_ID);
            adminou.SetEntityId(Consts.ORGUNIT_BASEID_ID, admin.Id);
            adminou.SetEntityId(Consts.ORGUNIT_PARENTID_ID, itdeptou.Id);

            var testou = new Entity(orgunit);
            testou.SetString(Consts.ORGUNIT_NAME_ID, test.GetString(Consts.EMPLOEE_NAME_ID));
            testou.SetUInt64(Consts.ORGUNIT_BASETYPE_ID, Consts.SYS_EMPLOEE_MODEL_ID);
            testou.SetEntityId(Consts.ORGUNIT_BASEID_ID, test.Id);
            testou.SetEntityId(Consts.ORGUNIT_PARENTID_ID, itdeptou.Id);

            //事务保存
            var txn = await Transaction.BeginAsync();
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

            await EntityStore.InsertEntityAsync(defaultEnterprise, txn);
            await EntityStore.InsertEntityAsync(itdept, txn);
            await EntityStore.InsertEntityAsync(admin, txn);
            await EntityStore.InsertEntityAsync(test, txn);
            await EntityStore.InsertEntityAsync(entou, txn);
            await EntityStore.InsertEntityAsync(itdeptou, txn);
            await EntityStore.InsertEntityAsync(adminou, txn);
            await EntityStore.InsertEntityAsync(testou, txn);

            //添加权限模型在保存OU实例之后
            var admin_permission = new PermissionModel(Consts.SYS_PERMISSION_ADMIN_ID, "Admin");
            admin_permission.Remark = "System administrator";
            admin_permission.OrgUnits.Add(adminou.Id);
            var developer_permission = new PermissionModel(Consts.SYS_PERMISSION_DEVELOPER_ID, "Developer");
            developer_permission.Remark = "System developer";
            developer_permission.OrgUnits.Add(itdeptou.Id);
            await ModelStore.InsertModelAsync(admin_permission, txn);
            await ModelStore.InsertModelAsync(developer_permission, txn);

            await txn.CommitAsync();
        }

        private static EntityModel CreateEmploeeModel(ApplicationModel app)
        {
            var emploee = new EntityModel(Consts.SYS_EMPLOEE_MODEL_ID, Consts.EMPLOEE, EntityStoreType.StoreWithMvcc);
            //Add members
            var name = new DataFieldModel(emploee, Consts.NAME, EntityFieldType.String);
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
            var ui_account = new EntityIndexModel(emploee, "UI_Account", true,
                                                       new EntityIndexField[] { new EntityIndexField(Consts.EMPLOEE_ACCOUNT_ID) },
                                                       new ushort[] { Consts.EMPLOEE_PASSWORD_ID });
            emploee.SysStoreOptions.AddSysIndex(emploee, ui_account, Consts.EMPLOEE_UI_ACCOUNT_ID);

            return emploee;
        }

        private static EntityModel CreateEnterpriseModel(ApplicationModel app)
        {
            var model = new EntityModel(Consts.SYS_ENTERPRISE_MODEL_ID, Consts.ENTERPRISE, EntityStoreType.StoreWithMvcc);

            var name = new DataFieldModel(model, Consts.NAME, EntityFieldType.String);
            model.AddSysMember(name, Consts.ENTERPRISE_NAME_ID);

            var address = new DataFieldModel(model, "Address", EntityFieldType.String);
            address.AllowNull = true;
            model.AddSysMember(address, Consts.ENTERPRISE_ADDRESS_ID);

            return model;
        }

        private static EntityModel CreateWorkgroupModel(ApplicationModel app)
        {
            var model = new EntityModel(Consts.SYS_WORKGROUP_MODEL_ID, Consts.WORKGROUP, EntityStoreType.StoreWithMvcc);

            var name = new DataFieldModel(model, Consts.NAME, EntityFieldType.String);
            model.AddSysMember(name, Consts.WORKGROUP_NAME_ID);

            return model;
        }

        private static EntityModel CreateOrgUnitModel(ApplicationModel app)
        {
            var model = new EntityModel(Consts.SYS_ORGUNIT_MODEL_ID, Consts.ORGUNIT, EntityStoreType.StoreWithMvcc);

            var name = new DataFieldModel(model, Consts.NAME, EntityFieldType.String);
            model.AddSysMember(name, Consts.ORGUNIT_NAME_ID);

            var baseId = new DataFieldModel(model, "BaseId", EntityFieldType.EntityId);
            model.AddSysMember(baseId, Consts.ORGUNIT_BASEID_ID);
            var baseType = new DataFieldModel(model, "BaseType", EntityFieldType.UInt64);
            model.AddSysMember(baseType, Consts.ORGUNIT_BASETYPE_ID);
            var Base = new EntityRefModel(model, "Base",
                new List<ulong>() { Consts.SYS_ENTERPRISE_MODEL_ID, Consts.SYS_WORKGROUP_MODEL_ID, Consts.SYS_EMPLOEE_MODEL_ID },
                baseId.MemberId, baseType.MemberId);
            model.AddSysMember(Base, Consts.ORGUNIT_BASE_ID);

            var parentId = new DataFieldModel(model, "ParentId", EntityFieldType.EntityId);
            parentId.AllowNull = true;
            model.AddSysMember(parentId, Consts.ORGUNIT_PARENTID_ID);
            var parent = new EntityRefModel(model, "Parent", Consts.SYS_ORGUNIT_MODEL_ID, parentId.MemberId);
            parent.AllowNull = true;
            model.AddSysMember(parent, Consts.ORGUNIT_PARENT_ID);

            var childs = new EntitySetModel(model, "Childs", Consts.SYS_ORGUNIT_MODEL_ID, parent.MemberId);
            model.AddSysMember(childs, Consts.ORGUNIT_CHILDS_ID);

            return model;
        }

        private static EntityModel CreateStagedModel(ApplicationModel app)
        {
            var model = new EntityModel(Consts.SYS_STAGED_MODEL_ID, "StagedModel", EntityStoreType.StoreWithoutMvcc);

            var type = new DataFieldModel(model, "Type", EntityFieldType.Byte);
            model.AddSysMember(type, Consts.STAGED_TYPE_ID);

            var modelId = new DataFieldModel(model, "ModelId", EntityFieldType.String); //暂用String
            model.AddSysMember(modelId, Consts.STAGED_MODELID_ID);

            var devId = new DataFieldModel(model, "DeveloperId", EntityFieldType.Guid);
            model.AddSysMember(devId, Consts.STAGED_DEVELOPERID_ID);

            var data = new DataFieldModel(model, "Data", EntityFieldType.Binary);
            model.AddSysMember(data, Consts.STAGED_DATA_ID);

            return model;
        }

        private static EntityModel CreateCheckoutModel(ApplicationModel app)
        {
            var model = new EntityModel(Consts.SYS_CHECKOUT_MODEL_ID, "Checkout", EntityStoreType.StoreWithoutMvcc);

            var nodeType = new DataFieldModel(model, "NodeType", EntityFieldType.Byte);
            model.AddSysMember(nodeType, Consts.CHECKOUT_NODETYPE_ID);

            var targetId = new DataFieldModel(model, "TargetId", EntityFieldType.String);
            model.AddSysMember(targetId, Consts.CHECKOUT_TARGETID_ID);

            var devId = new DataFieldModel(model, "DeveloperId", EntityFieldType.Guid);
            model.AddSysMember(devId, Consts.CHECKOUT_DEVELOPERID_ID);

            var devName = new DataFieldModel(model, "DeveloperName", EntityFieldType.String);
            model.AddSysMember(devName, Consts.CHECKOUT_DEVELOPERNAME_ID);

            var version = new DataFieldModel(model, "Version", EntityFieldType.Int32); //TODO:UInt32
            model.AddSysMember(version, Consts.CHECKOUT_VERSION_ID);

            //Add indexes
            var ui_nodeType_targetId = new EntityIndexModel(model, "UI_NodeType_TargetId", true,
                                                            new EntityIndexField[]
            {
                new EntityIndexField(Consts.CHECKOUT_NODETYPE_ID),
                new EntityIndexField(Consts.CHECKOUT_TARGETID_ID)
            });
            model.SysStoreOptions.AddSysIndex(model, ui_nodeType_targetId, Consts.CHECKOUT_UI_NODETYPE_TARGETID_ID);

            return model;
        }

        private static async Task CreateServiceModel(string name, ulong idIndex, Guid? folderId, Transaction txn, List<string> references = null)
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

        private static async Task CreateViewModel(string name, ulong idIndex, Guid? folderId, Transaction txn, string routePath = null)
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
}
