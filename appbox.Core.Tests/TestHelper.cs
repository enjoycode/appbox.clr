using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Models;
using appbox.Runtime;

namespace appbox.Core.Tests
{
    public static class TestHelper
    {

        public static ApplicationModel SysAppModel { get; private set; }

        public static EntityModel EmploeeModel { get; private set; }
        public static EntityModel VehicleStateModel { get; private set; }
        public static EntityModel OrgUnitModel { get; private set; }

        public static DataStoreModel SqlStoreModel { get; private set; }

        public static EntityModel CityModel { get; private set; }
        public static EntityModel CustomerModel { get; private set; }
        public static EntityModel OrderModel { get; private set; }

        public static PermissionModel AdminPermissionModel { get; private set; }
        public static PermissionModel DeveloperPermissionModel { get; private set; }

        static TestHelper()
        {
            SysAppModel = new ApplicationModel("appbox", Consts.SYS, Consts.SYS_APP_ID);

            AdminPermissionModel = new PermissionModel(Consts.SYS_PERMISSION_ADMIN_ID, "Admin");
            DeveloperPermissionModel = new PermissionModel(Consts.SYS_PERMISSION_DEVELOPER_ID, "Developer");

            EmploeeModel = new EntityModel(Consts.SYS_EMPLOEE_MODEL_ID, Consts.EMPLOEE, EntityStoreType.StoreWithMvcc);
            var name = new DataFieldModel(EmploeeModel, Consts.NAME, EntityFieldType.String);
            EmploeeModel.AddSysMember(name, Consts.EMPLOEE_NAME_ID);
            var account = new DataFieldModel(EmploeeModel, Consts.ACCOUNT, EntityFieldType.String);
            account.AllowNull = true;
            EmploeeModel.AddSysMember(account, Consts.EMPLOEE_ACCOUNT_ID);
            var password = new DataFieldModel(EmploeeModel, Consts.PASSWORD, EntityFieldType.Binary);
            password.AllowNull = true;
            EmploeeModel.AddSysMember(password, Consts.EMPLOEE_PASSWORD_ID);

            //Add indexes
            var ui_account_pass = new EntityIndexModel(EmploeeModel, "UI_Account_Password", true,
                                                       new FieldWithOrder[] { new FieldWithOrder(Consts.EMPLOEE_ACCOUNT_ID) },
                                                       new ushort[] { Consts.EMPLOEE_PASSWORD_ID });
            EmploeeModel.SysStoreOptions.AddSysIndex(EmploeeModel, ui_account_pass, Consts.EMPLOEE_UI_ACCOUNT_ID);

            //测试用分区表
            VehicleStateModel = new EntityModel(((ulong)Consts.SYS_APP_ID << 32) | 18, "VehicleState", EntityStoreType.StoreWithMvcc);
            var vid = new DataFieldModel(VehicleStateModel, "VehicleId", EntityFieldType.Int32);
            VehicleStateModel.AddMember(vid);
            var lng = new DataFieldModel(VehicleStateModel, "Lng", EntityFieldType.Float);
            VehicleStateModel.AddMember(lng);
            var lat = new DataFieldModel(VehicleStateModel, "Lat", EntityFieldType.Float);
            VehicleStateModel.AddMember(lat);
            var pks = new PartitionKey[1];
            pks[0] = new PartitionKey() { MemberId = vid.MemberId, OrderByDesc = false };
            VehicleStateModel.SysStoreOptions.SetPartitionKeys(VehicleStateModel, pks);

            //测试树状结构
            ulong orgUnitModelId = ((ulong)Consts.SYS_APP_ID << 32) | 19;
            OrgUnitModel = new EntityModel(orgUnitModelId, "OrgUnit", EntityStoreType.StoreWithMvcc);
            var ouName = new DataFieldModel(OrgUnitModel, "Name", EntityFieldType.String);
            OrgUnitModel.AddSysMember(ouName, 1 << IdUtil.MEMBERID_SEQ_OFFSET);
            var parentId = new DataFieldModel(OrgUnitModel, "ParentId", EntityFieldType.EntityId, true);
            parentId.AllowNull = true;
            OrgUnitModel.AddSysMember(parentId, 2 << IdUtil.MEMBERID_SEQ_OFFSET);
            var parent = new EntityRefModel(OrgUnitModel, "Parent", orgUnitModelId, new ushort[] { parentId.MemberId });
            parent.AllowNull = true;
            OrgUnitModel.AddSysMember(parent, 3 << IdUtil.MEMBERID_SEQ_OFFSET);
            var childs = new EntitySetModel(OrgUnitModel, "Childs", orgUnitModelId, parent.MemberId);
            OrgUnitModel.AddSysMember(childs, 4 << IdUtil.MEMBERID_SEQ_OFFSET);

            //----以下测试映射至SqlStore的实体---
            SqlStoreModel = new DataStoreModel(DataStoreKind.Sql, "appbox.Store.PostgreSQL;appbox.Store.PgSqlStore", "DemoDB");

            ulong cityModelId = ((ulong)Consts.SYS_APP_ID << 32) | 25;
            CityModel = new EntityModel(cityModelId, "City", new SqlStoreOptions(SqlStoreModel.Id));
            var cityCode = new DataFieldModel(CityModel, "Code", EntityFieldType.Int32);
            CityModel.AddMember(cityCode);
            var cityName = new DataFieldModel(CityModel, "Name", EntityFieldType.String);
            CityModel.AddMember(cityName);
            var cityPk = new List<FieldWithOrder>();
            cityPk.Add(new FieldWithOrder { MemberId = cityCode.MemberId, OrderByDesc = false });
            CityModel.SqlStoreOptions.SetPrimaryKeys(CityModel, cityPk);

            ulong customerModelId = ((ulong)Consts.SYS_APP_ID << 32) | 26;
            CustomerModel = new EntityModel(customerModelId, "Customer", new SqlStoreOptions(SqlStoreModel.Id));
            var customerId = new DataFieldModel(CustomerModel, "Id", EntityFieldType.Int32);
            CustomerModel.AddMember(customerId);
            var customerName = new DataFieldModel(CustomerModel, "Name", EntityFieldType.String);
            CustomerModel.AddMember(customerName);
            var customerCityId = new DataFieldModel(CustomerModel, "CityId", EntityFieldType.Int32, true);
            CustomerModel.AddMember(customerCityId);
            var customerCity = new EntityRefModel(CustomerModel, "City", cityModelId, new ushort[] { customerCityId.MemberId });
            CustomerModel.AddMember(customerCity);
            var customerPk = new List<FieldWithOrder>();
            customerPk.Add(new FieldWithOrder { MemberId = customerId.MemberId, OrderByDesc = false });
            CustomerModel.SqlStoreOptions.SetPrimaryKeys(CustomerModel, customerPk);

            ulong orderModelId = ((ulong)Consts.SYS_APP_ID << 32) | 27;
            OrderModel = new EntityModel(orderModelId, "Order", new SqlStoreOptions(SqlStoreModel.Id));
            var orderId = new DataFieldModel(OrderModel, "Id", EntityFieldType.Int32);
            OrderModel.AddMember(orderId);
            var orderCustomerId = new DataFieldModel(OrderModel, "CustomerId", EntityFieldType.Int32, true);
            OrderModel.AddMember(orderCustomerId);
            var orderCustomer = new EntityRefModel(OrderModel, "Customer", customerModelId, new ushort[] { orderCustomerId.MemberId });
            OrderModel.AddMember(orderCustomer);
            var orderPk = new List<FieldWithOrder>();
            orderPk.Add(new FieldWithOrder { MemberId = orderId.MemberId, OrderByDesc = false });
            OrderModel.SqlStoreOptions.SetPrimaryKeys(OrderModel, orderPk);
        }

        public static Expressions.IExpressionContext GetMockExpressionContext()
        {
            return new MockExpressionContext();
        }
    }

    public sealed class MockRuntimeContext : IRuntimeContext
    {
        private readonly Dictionary<ulong, ModelBase> _entityModels = new Dictionary<ulong, ModelBase>();

        public void AddModel(ModelBase model)
        {
            _entityModels.Add(model.Id, model);
        }

#if FUTURE
        public string AppPath => "/Users/lushuaijun/Projects/AppBoxFuture/appbox/cmake-build-debug";
#else
        public string AppPath => "/Users/lushuaijun/Projects/AppBoxFuture/appbox.clr/build/bin";
#endif

        public bool IsMainDomain => throw new NotImplementedException();

        public ISessionInfo CurrentSession { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public ulong RuntimeId => throw new NotImplementedException();

        public ValueTask<ApplicationModel> GetApplicationModelAsync(uint appId)
        {
            throw new NotImplementedException();
        }

        public ValueTask<ApplicationModel> GetApplicationModelAsync(string appName)
        {
            throw new NotImplementedException();
        }

        public ValueTask<T> GetModelAsync<T>(ulong modelId) where T : ModelBase
        {
            if (_entityModels.TryGetValue(modelId, out ModelBase found))
            {
                return new ValueTask<T>((T)found);
            }
            return new ValueTask<T>(default(T));
        }

        public void InvalidModelsCache(string[] services, ulong[] others, bool byPublish)
        {
            throw new NotImplementedException();
        }

        public ValueTask<AnyValue> InvokeAsync(string servicePath, InvokeArgs args)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class MockExpressionContext : Expressions.IExpressionContext
    {

        private readonly ParameterExpression vp;
        private readonly ParameterExpression vs;
        private readonly ParameterExpression mv;
        private readonly ParameterExpression ts;

        public MockExpressionContext()
        {
            vp = Expression.Parameter(typeof(IntPtr), "vp");
            vs = Expression.Parameter(typeof(int), "vs");
            mv = Expression.Parameter(typeof(bool), "mv");
            ts = Expression.Parameter(typeof(ulong), "ts");
        }

        public ParameterExpression GetParameter(string paraName)
        {
            switch (paraName)
            {
                case "vp": return vp;
                case "vs": return vs;
                case "mv": return mv;
                case "ts": return ts;
                default: throw new Exception($"unknow parameter name: {paraName}");
            }
        }
    }
}
