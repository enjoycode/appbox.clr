using System;

namespace appbox.Serialization
{
    public enum PayloadType : byte
    {
        //Null reference
        Null = 0,
        //DBNull
        DBNull = 1, //todo: remove it
        //----Primitive Types----
        Boolean = 2,
        Byte = 3,
        Char = 4,
        Decimal = 5,
        Float = 6,
        Double = 7,
        Int16 = 8,
        Int32 = 9,
        Int64 = 10,
        UInt16 = 12,
        UInt32 = 13,
        UInt64 = 14,
        DateTime = 15,
        String = 16,
        Guid = 17,

        // Collections
        Dictionary = 18,
        Array = 19,
        List = 20,

        /// <summary>
        /// 扩展类型
        /// </summary>
        ExtKnownType = 21,

        /// <summary>
        /// 对象引用
        /// </summary>
        ObjectRef = 22,

        /// <summary>
        /// 专用于Json序列化
        /// </summary>
        UnknownType = 23,

        //----Known Object Types----
        Resource = 24,
        ApplicationModel = 25,
        ApplicationAssembly = 83,
        ModelFolder = 26,
        MenuFolder = 27,
        EnumModel = 28,
        EnumModelItem = 29,
        PermissionModel = 30,
        EventModel = 31,
        ResourceModel = 32,
        ResourceValue = 33,
        TreeNodePath = 34,
        PermissionNode = 35,
        WorkflowModel = 37,
        StartActivityModel = 38,
        SingleHumanActivityModel = 39,
        DecisionActivityModel = 40,
        AutomationActivityModel = 41,
        MultiHumanActivityModel = 42,
        ModelBase = 43, //特殊占位
        ActivityViewInfo = 44,
        DataStoreModel = 45,
        ViewModel = 46,
        ReportModel = 47,
        FormModel = 48,
        ServiceModel = 49,
        //----实体模型及成员模型----
        EntityModel = 50,
        //InheritEntityModel = 51,
        //ImageRefModel = 52,
        EntityIndexModel = 53,
        //AggregateModel = 54,
        //AggregationRefModel = 55,
        //AutoNumberModel = 56,
        DataFieldModel = 57,
        EntityRefModel = 58,
        EntitySetModel = 59,
        FormulaModel = 60,
        TrackerModel = 61,
        //FieldSetModel = 85,
        SysStoreOptions = 84,
        SqlStoreOptions = 86,
        //----表达式----
        LambdaExpression = 36,
        AggregationRefFieldExpression = 62,
        BinaryExpression = 63,
        PrimitiveExpression = 64,
        EntityExpression = 65,
        EntitySetExpression = 66,
        EnumItemExpression = 67,
        FieldExpression = 68,
        GroupExpression = 69,
        TypeReference = 70,
        IdentifierExpression = 71,
        InvokeServiceExpression = 72,
        InvokeServiceAsyncExpression = 73,
        InvokeSysFuncExpression = 74,
        FormCreationExpression = 75,
        ArrayCreationExpression = 76,
        //WorkflowCreationExpression = 77,
        KVFieldExpression = 78,
        //EntityDeleteAction = 79,
        //EntityActionPermissionRule = 80,
        //EntityActionEditRule = 81,
        //EntityActionValidateRule = 82,
        //----查询相关----
        // 87 - 89

        AppPackage = 100,

        //----实体实例相关----
        Entity = 90,
        EntityList = 91,
        //MAggRef = 92,
        //MAutoNo = 93,
        //MBinary = 94,
        //MBool = 95,
        //MByte = 96,
        //MDate = 97,
        //MDecimal = 98,
        //MEntityRef = 99,
        //MFormula = 101,
        //MGuid = 102,
        //MInt = 103,
        //MString = 104,
        //MTracker = 105,
        FileContentResult = 106,
        DataTable = 107,
        JsonResult = 108,
        ObjectArray = 109,
        //MEntityRefDisplayText = 110,

        //----消息相关----
        LoginRequire = 112,
        LoginResponse = 113,
        InvokeRequire = 114,
        InvokeResponse = 115,
        LogoutRequire = 116,
        LogoutResponse = 117,
        //EventMessage = 118,
        //SysException = 119,
        //CommandMessage = 120,
        //AppContainerOnline = 121,
        InvalidModelsCache = 122,
        MetricRequire = 123,
        //GetPublicKeyResponse = 124,

        //----后来补充的表达式----
        EventAction = 140,
        MemberAccessExpression = 141,
        BlockExpression = 142,
        AssignmentExpression = 143,
        IfStatementExpression = 144,
        InvokeGuiFuncExpression = 145,
        InvokeDynamicExpression = 146,
        LocalDeclarationExpression = 147,
        TypeExpression = 148,

        //----Drawing及FormModel相关----
        //Point = 150,
        //Rectangle = 151,
        //Color = 152,
        RootViewModel = 153,
        ButtonModel = 154,
        LabelModel = 155,
        TextBoxModel = 156,
        ControlBindingInfo = 157,
        EntityDataSourceModel = 158,
        EntitySetDataSourceModel = 159,
        ServiceDataSourceModel = 160,
        GridViewModel = 161,
        GridViewColumnModel = 162,
        GridViewTextBoxColumnModel = 163,
        //164-169
        PageViewModel = 170,
        PageViewPageModel = 171,
        TreeViewModel = 172,
        ResourceImageSource = 173,
        EntityViewPanelModel = 174,
        EntityPickerModel = 175,

        //Store Api Message
        NativeMessage = 200,
        KVGetRequire = 201,
        KVScanRequire = 202,
        BeginTranRequire = 203,
        CommitTranRequire = 204,
        RollbackTranRequire = 205,
        GenPartitionRequire = 206,
        KVInsertRequire = 207,
        KVAddRefRequire = 208,
        KVDeleteRequire = 209,
        KVUpdateRequire = 210,

        //工作流运行时相关
        HumanActionResult = 249,
        StartActivity = 250,
        DecisionActivity = 251,
        AutomationActivity = 252,
        SingleHumanActivity = 253,
        MultiHumanActivity = 254,
        Bookmark = 255

        //注意：只能继续添加不能修改上述任一编号
    }
}

