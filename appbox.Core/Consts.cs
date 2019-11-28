using System;

namespace appbox
{
    public static class Consts
    {
        public const string SYS = "sys";

        //====系统内置表及成员名称====
        //public const string ID = "Id";
        //public const string BASE = "Base";
        public const string NAME = "Name";

        public const string ENTERPRISE = "Enterprise";
        public const string WORKGROUP = "Workgroup";
        public const string EMPLOEE = "Emploee";
        public const string EXTUSER = "ExtUser";
        public const string ORGUNIT = "OrgUnit";

        public const string ACCOUNT = "Account";
        public const string PASSWORD = "Password";

        internal const uint SYS_APP_ID = 0x9E9AA8F7;
        private const ulong SYS_ENTITY_MODEL_ID = ((ulong)SYS_APP_ID << IdUtil.MODELID_APPID_OFFSET) | ((ulong)Models.ModelType.Entity << IdUtil.MODELID_TYPE_OFFSET);
        private const ulong SYS_PERMISSION_MODEL_ID = ((ulong)SYS_APP_ID << IdUtil.MODELID_APPID_OFFSET) | ((ulong)Models.ModelType.Permission << IdUtil.MODELID_TYPE_OFFSET);
        internal const ulong SYS_EMPLOEE_MODEL_ID = SYS_ENTITY_MODEL_ID | (1 << IdUtil.MODELID_SEQ_OFFSET);
        internal const ulong SYS_ENTERPRISE_MODEL_ID = SYS_ENTITY_MODEL_ID | (2 << IdUtil.MODELID_SEQ_OFFSET);
        internal const ulong SYS_WORKGROUP_MODEL_ID = SYS_ENTITY_MODEL_ID | (3 << IdUtil.MODELID_SEQ_OFFSET);
        internal const ulong SYS_ORGUNIT_MODEL_ID = SYS_ENTITY_MODEL_ID | (4 << IdUtil.MODELID_SEQ_OFFSET);
        internal const ulong SYS_STAGED_MODEL_ID = SYS_ENTITY_MODEL_ID | (5 << IdUtil.MODELID_SEQ_OFFSET);
        internal const ulong SYS_CHECKOUT_MODEL_ID = SYS_ENTITY_MODEL_ID | (6 << IdUtil.MODELID_SEQ_OFFSET);

        internal const ulong SYS_PERMISSION_ADMIN_ID = SYS_PERMISSION_MODEL_ID | (1 << IdUtil.MODELID_SEQ_OFFSET);
        internal const ulong SYS_PERMISSION_DEVELOPER_ID = SYS_PERMISSION_MODEL_ID | (2 << IdUtil.MODELID_SEQ_OFFSET);

        internal const ushort ENTERPRISE_NAME_ID = 1 << IdUtil.MEMBERID_SEQ_OFFSET;
        internal const ushort ENTERPRISE_ADDRESS_ID = 2 << IdUtil.MEMBERID_SEQ_OFFSET;

        internal const ushort WORKGROUP_NAME_ID = 1 << IdUtil.MEMBERID_SEQ_OFFSET;

        internal const ushort EMPLOEE_NAME_ID = 1 << IdUtil.MEMBERID_SEQ_OFFSET;
        internal const ushort EMPLOEE_MALE_ID = 2 << IdUtil.MEMBERID_SEQ_OFFSET;
        internal const ushort EMPLOEE_BIRTHDAY_ID = 3 << IdUtil.MEMBERID_SEQ_OFFSET;
        internal const ushort EMPLOEE_ACCOUNT_ID = 4 << IdUtil.MEMBERID_SEQ_OFFSET;
        internal const ushort EMPLOEE_PASSWORD_ID = 5 << IdUtil.MEMBERID_SEQ_OFFSET;
        internal const ushort EMPLOEE_ORGUNITS_ID = 6 << IdUtil.MEMBERID_SEQ_OFFSET;
        internal const byte EMPLOEE_UI_ACCOUNT_ID = (1 << IdUtil.INDEXID_UNIQUE_OFFSET) | (1 << 2);

        internal const ushort ORGUNIT_NAME_ID = 1 << IdUtil.MEMBERID_SEQ_OFFSET;
        internal const ushort ORGUNIT_BASEID_ID = 2 << IdUtil.MEMBERID_SEQ_OFFSET;
        internal const ushort ORGUNIT_BASETYPE_ID = 3 << IdUtil.MEMBERID_SEQ_OFFSET;
        internal const ushort ORGUNIT_BASE_ID = 4 << IdUtil.MEMBERID_SEQ_OFFSET;
        internal const ushort ORGUNIT_PARENTID_ID = 5 << IdUtil.MEMBERID_SEQ_OFFSET;
        internal const ushort ORGUNIT_PARENT_ID = 6 << IdUtil.MEMBERID_SEQ_OFFSET;
        internal const ushort ORGUNIT_CHILDS_ID = 7 << IdUtil.MEMBERID_SEQ_OFFSET;

        internal const ushort STAGED_TYPE_ID = 1 << IdUtil.MEMBERID_SEQ_OFFSET;
        internal const ushort STAGED_MODELID_ID = 2 << IdUtil.MEMBERID_SEQ_OFFSET;
        internal const ushort STAGED_DEVELOPERID_ID = 3 << IdUtil.MEMBERID_SEQ_OFFSET;
        internal const ushort STAGED_DATA_ID = 4 << IdUtil.MEMBERID_SEQ_OFFSET;

        internal const ushort CHECKOUT_NODETYPE_ID = 1 << IdUtil.MEMBERID_SEQ_OFFSET;
        internal const ushort CHECKOUT_TARGETID_ID = 2 << IdUtil.MEMBERID_SEQ_OFFSET;
        internal const ushort CHECKOUT_DEVELOPERID_ID = 3 << IdUtil.MEMBERID_SEQ_OFFSET;
        internal const ushort CHECKOUT_DEVELOPERNAME_ID = 4 << IdUtil.MEMBERID_SEQ_OFFSET;
        internal const ushort CHECKOUT_VERSION_ID = 5 << IdUtil.MEMBERID_SEQ_OFFSET;
        internal const byte CHECKOUT_UI_NODETYPE_TARGETID_ID = (1 << IdUtil.INDEXID_UNIQUE_OFFSET) | (1 << 2);

    }
}
