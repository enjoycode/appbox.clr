using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using appbox.Data;
using appbox.Models;

namespace appbox.Core.Tests
{
    public static class TestHelper
    {

        public static ApplicationModel SysAppModel { get; private set; }

        public static EntityModel EmploeeModel { get; private set; }
        public static EntityModel VehicleStateModel { get; private set; }
        public static EntityModel OrgUnitModel { get; private set; }

        public static DataStoreModel SqlStoreModel { get; private set; }
        /// <summary>
        /// 映射至SqlStore
        /// </summary>
        public static EntityModel CityModel { get; private set; }

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
                                                       new EntityIndexField[] { new EntityIndexField(Consts.EMPLOEE_ACCOUNT_ID) },
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
            var parentId = new DataFieldModel(OrgUnitModel, "ParentId", EntityFieldType.EntityId);
            parentId.AllowNull = true;
            OrgUnitModel.AddSysMember(parentId, 2 << IdUtil.MEMBERID_SEQ_OFFSET);
            var parent = new EntityRefModel(OrgUnitModel, "Parent", orgUnitModelId, parentId.MemberId);
            parent.AllowNull = true;
            OrgUnitModel.AddSysMember(parent, 3 << IdUtil.MEMBERID_SEQ_OFFSET);
            var childs = new EntitySetModel(OrgUnitModel, "Childs", orgUnitModelId, parent.MemberId);
            OrgUnitModel.AddSysMember(childs, 4 << IdUtil.MEMBERID_SEQ_OFFSET);

            SqlStoreModel = new DataStoreModel(DataStoreKind.Sql, "appbox.Store.PostgreSQL;appbox.Store.PgSqlStore", "DemoDB");
            //测试映射至SqlStore的实体
            ulong sqlModelId = ((ulong)Consts.SYS_APP_ID << 32) | 25;
            CityModel = new EntityModel(sqlModelId, "City", SqlStoreModel.Id);
            var cityCode = new DataFieldModel(CityModel, "Code", EntityFieldType.Int32);
            CityModel.AddMember(cityCode);
            var cityName = new DataFieldModel(CityModel, "Name", EntityFieldType.String);
            CityModel.AddMember(cityName);
            var cityPk = new List<FieldWithOrder>();
            cityPk.Add(new FieldWithOrder() { MemberId = cityCode.MemberId, OrderByDesc = false });
            CityModel.SqlStoreOptions.SetPrimaryKeys(CityModel, cityPk);
        }

        public static Expressions.IExpressionContext GetMockExpressionContext()
        {
            return new MockExpressionContext();
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
