using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using OmniSharp;
using appbox.Models;
using System.Linq;
using appbox.Caching;
using appbox.Design.ServiceInterceptors;

namespace appbox.Design
{
    /// <summary>
    /// 用于生成各模型的虚拟代码
    /// </summary>
    static class CodeGenService //TODO: rename to CodeGenerator
    {
        #region ====Generate Base Dummy Code Methods====
        public static string GenBaseDummyCode()
        {
            return Resources.LoadStringResource("Resources.DummyCode.BaseDummyCode.cs");
        }

        public static string GenBaseWFDummyCode()
        {
            return Resources.LoadStringResource("Resources.DummyCode.BaseWFDummyCode.cs");
        }

        public static string GenServiceBaseDummyCode()
        {
            return Resources.LoadStringResource("Resources.DummyCode.ServiceBaseDummyCode.cs");
        }

        public static string GenSyncSysServiceDummyCode()
        {
            return Resources.LoadStringResource("Resources.DummyCode.SyncSysServiceDummyCode.cs");
        }

        internal static string GenDataStoreDummyCode(DataStoreModel model)
        {
            var sb = StringBuilderCache.Acquire();
            sb.Append("public static partial class DataStore { ");
            sb.Append($"public static {model.Kind.ToString()}Store {model.Name} {{get{{return null;}}}} }}");
            return StringBuilderCache.GetStringAndRelease(sb);
        }
        #endregion

        #region ====Generate Model Dummy Code Methods====
        /// <summary>
        /// 根据枚举模型生成虚拟代码
        /// </summary>
        public static string GenEnumDummyCode(EnumModel model)
        {
            throw ExceptionHelper.NotImplemented();
            //StringBuilder sb = new StringBuilder();

            //sb.AppendFormat("namespace {0}.Enums {{ \r\n ", model.AppID);
            ////注意：暂去掉RealType特性 sb.AppendFormat("\t[{0}(),{1}(\"AppBox.Core.EnumValue\")] {2}", TypeHelper.EnumModelAttribute
            ////                 , TypeHelper.RealTypeAttribute, Environment.NewLine);
            //sb.AppendFormat("\t[{0}()]{1}", TypeHelper.EnumModelAttribute, Environment.NewLine);
            //sb.AppendFormat("\tpublic enum {0} {{ \r\n", model.ID.Split('.')[1]);
            //foreach (EnumModelItem item in model.Items)
            //{
            //    sb.AppendFormat("\t\t[{0}(\"EnumItem\")]{1} = {2},", TypeHelper.MemberAccessInterceptorAttribute
            //                    , item.Name, item.Value.ToString());
            //}
            //if (model.Items.Count > 0)
            //    sb.Remove(sb.Length - 1, 1);
            //sb.Append("\t} \r\n}");

            //return sb.ToString();
        }

        /// <summary>
        /// 根据实体模型生成虚拟代码
        /// </summary>
        public static string GenEntityDummyCode(EntityModel model, string appName, DesignTree designTree)
        {
            var sb = StringBuilderCache.Acquire();
            //sb.Append("using System;\n");
            sb.Append($"namespace {appName}.Entities {{\n");

            //TODO:生成注释
            //sb.Append("/// <summary>\n");
            //sb.AppendFormat("/// {0}\n", model.Name);
            //sb.Append("/// </summary>\n");
            sb.Append($"public class {model.Name}");
            //根据映射存储不同生成不同的继承
            if (model.SysStoreOptions != null)
                sb.Append($": {TypeHelper.Type_SysEntityBase}");
            else if (model.SqlStoreOptions != null)
                sb.Append($": {TypeHelper.Type_SqlEntityBase}");
            else if (model.CqlStoreOptions != null)
                sb.Append($": {TypeHelper.Type_CqlEntityBase}");
            else
                sb.Append($": {TypeHelper.Type_EntityBase}");
            sb.Append(" {\n");

            //生成静态TypeId属性
            sb.AppendFormat("[{0}(\"TypeId\")]\n", TypeHelper.MemberAccessInterceptorAttribute);
            sb.AppendLine("public const ulong TypeId = 0;");

            //生成成员属性
            EntityMemberModel[] ls = model.Members.ToArray();
            for (int i = 0; i < ls.Length; i++)
            {
                EntityMemberModel mm = ls[i];
                if (!string.IsNullOrEmpty(mm.Comment))
                {
                    sb.Append("/// <summary>\n");
                    sb.AppendFormat("/// {0}\n", mm.Comment);
                    sb.Append("/// </summary>\n");
                }

                string typeString = "object";
                bool readOnly = false;
                GetEntityMemberTypeStringAndReadOnly(mm, ref typeString, ref readOnly, designTree);

                sb.Append($"public {typeString} {mm.Name} {{get;");
                if (!readOnly)
                    sb.Append("set;");
                sb.Append("}\n");
            }

            //如果是分区表,生成带分区参数的构造方法
            if (model.SysStoreOptions != null && model.SysStoreOptions.HasPartitionKeys)
            {
                sb.Append($"public {model.Name}(");
                bool hasPreArg = false;
                for (int i = 0; i < model.SysStoreOptions.PartitionKeys.Length; i++)
                {
                    if (model.SysStoreOptions.PartitionKeys[i].MemberId != 0) //排除特殊分区键
                    {
                        if (hasPreArg) sb.Append(',');
                        var member = model.GetMember(model.SysStoreOptions.PartitionKeys[i].MemberId, true);
                        string typeString = "object";
                        bool readOnly = false;
                        GetEntityMemberTypeStringAndReadOnly(member, ref typeString, ref readOnly, designTree);
                        sb.Append($"{typeString} {member.Name}");
                        hasPreArg = true;
                    }
                }
                sb.Append("){}");
            }
            //如果是SqlStore且具备主键生成主键参数构造，同时生成静态LoadAsync方法 //TODO:考虑排除自增主键
            if (model.SqlStoreOptions != null && model.SqlStoreOptions.HasPrimaryKeys)
            {
                var ctorsb = StringBuilderCache.Acquire();
                ctorsb.Append($"public {model.Name}(");
                sb.AppendFormat("[{0}(\"{1}\")]\n",
                    TypeHelper.InvocationInterceptorAttribute, LoadEntityInterceptor.Name);
                sb.Append($"public static System.Threading.Tasks.Task<{model.Name}> LoadAsync(");
                for (int i = 0; i < model.SqlStoreOptions.PrimaryKeys.Count; i++)
                {
                    var mm = model.GetMember(model.SqlStoreOptions.PrimaryKeys[i].MemberId, true);
                    string typeString = "object";
                    bool readOnly = false;
                    GetEntityMemberTypeStringAndReadOnly(mm, ref typeString, ref readOnly, designTree);
                    if (i != 0) { sb.Append(","); ctorsb.Append(","); }
                    sb.Append($"{typeString} {mm.Name}"); //TODO:mm.Name转为小驼峰
                    ctorsb.Append($"{typeString} {mm.Name}");
                }
                sb.Append(") {return null;}\n");
                ctorsb.Append("){}");
                sb.Append(StringBuilderCache.GetStringAndRelease(ctorsb));
            }
            //如果是CqlStore生成主键参数构造, 同时生成静态LoadAsync方法
            if (model.CqlStoreOptions != null)
            {
                var ctorsb = StringBuilderCache.Acquire();
                ctorsb.Append($"public {model.Name}(");
                //sb.AppendFormat("[{0}(\"LoadEntity\")]\n", TypeHelper.InvocationInterceptorAttribute);
                //sb.Append($"public static System.Threading.Tasks.Task<{model.Name}> LoadAsync(");
                var pks = model.CqlStoreOptions.PrimaryKey.GetAllPKs();
                for (int i = 0; i < pks.Length; i++)
                {
                    var mm = model.GetMember(pks[i], true);
                    string typeString = "object";
                    bool readOnly = false;
                    GetEntityMemberTypeStringAndReadOnly(mm, ref typeString, ref readOnly, designTree);
                    if (i != 0) { sb.Append(","); ctorsb.Append(","); }
                    //sb.Append($"{typeString} {mm.Name}"); //TODO:mm.Name转为小驼峰
                    ctorsb.Append($"{typeString} {mm.Name}");
                }
                //sb.Append(") {return null;}\n");
                ctorsb.Append("){}");
                sb.Append(StringBuilderCache.GetStringAndRelease(ctorsb));
            }

            //系统存储生成索引接口
            if (model.SysStoreOptions != null && model.SysStoreOptions.HasIndexes)
            {
                foreach (var index in model.SysStoreOptions.Indexes)
                {
                    sb.Append("public interface ");
                    sb.Append(index.Name);
                    sb.Append(" :IEntityIndex<");
                    sb.Append(model.Name);
                    sb.Append("> {");
                    for (int i = 0; i < index.Fields.Length; i++)
                    {
                        sb.Append("/// <summary>\n");
                        sb.AppendFormat("/// [{0}] {1}\n", i, index.Name); //TODO:OrderBy注释
                        sb.Append("/// </summary>\n");
                        var mm = model.GetMember(index.Fields[i].MemberId, true);
                        string typeString = "object";
                        bool readOnly = false;
                        GetEntityMemberTypeStringAndReadOnly(mm, ref typeString, ref readOnly, designTree);
                        sb.AppendFormat("{0} {1} {{get;}}", typeString, mm.Name);
                    }
                    sb.Append("}");
                }
            }

            //生成FetchEntitySet方法
            //sb.AppendFormat("\t\t[{0}(\"FetchEntitySetMethod\")]{1}", TypeHelper.InvocationInterceptorAttribute, Environment.NewLine);
            //sb.AppendFormat("\t\tpublic void FetchEntitySet<T>(System.Func<{0}.Entities.{1},EntityList<T>> selector) where T: EntityBase {{}}{2}"
            //                , model.AppID, model.Name, Environment.NewLine);
            //sb.AppendFormat("\t\t[{0}(\"FetchEntitySetMethod\")]{1}", TypeHelper.InvocationInterceptorAttribute, Environment.NewLine);
            //sb.AppendFormat("\t\tpublic void FetchEntitySet<T>(System.Func<{0}.Entities.{1},EntityList<T>> selector, System.Func<T, EntityBase[]> includes) where T: EntityBase {{}}{2}"
            //, model.AppID, model.Name, Environment.NewLine);

            sb.Append("}\n}");
            return StringBuilderCache.GetStringAndRelease(sb);
        }

        private static void GetEntityMemberTypeStringAndReadOnly(EntityMemberModel mm,
            ref string typeString, ref bool readOnly, DesignTree designTree)
        {
            switch (mm.Type)
            {
                case EntityMemberType.DataField:
                    //判断是否是枚举
                    DataFieldModel dmm = mm as DataFieldModel;
                    //if (dmm.DataType == EntityFieldType.Enum)
                    //{
                    //    if (string.IsNullOrEmpty(dmm.EnumModelID))
                    //        typeString = "int";
                    //    else
                    //    {
                    //        string[] sr = dmm.EnumModelID.Split('.');
                    //        typeString = sr[0] + ".Enums." + sr[1];
                    //    }
                    //}
                    if (dmm.DataType == EntityFieldType.EntityId)
                    {
                        typeString = "EntityId";
                    }
                    else
                    {
                        typeString = dmm.DataType.GetValueType().FullName; //TODO:简化类型名称
                    }

                    //系统存储分区键与sql存储的主键为只读
                    readOnly |= dmm.IsPartitionKey || dmm.IsPrimaryKey;

                    if (dmm.AllowNull && (dmm.DataType != EntityFieldType.String &&
                            dmm.DataType != EntityFieldType.EntityId &&
                            dmm.DataType != EntityFieldType.Binary && typeString != "object"))
                        typeString += "?";
                    break;
                case EntityMemberType.EntityRef:
                    EntityRefModel rm = (EntityRefModel)mm;
                    if (rm.IsAggregationRef)
                    {
                        typeString = TypeHelper.Type_EntityBase;
                    }
                    else
                    {
                        if (rm.RefModelIds.Count == 0) //Todo:待移除，因误删除模型引用项导致异常
                        {
                            typeString = TypeHelper.Type_EntityBase;
                        }
                        else
                        {
                            var targetModelNode = designTree.FindModelNode(ModelType.Entity, rm.RefModelIds[0]);
                            typeString = $"{targetModelNode.AppNode.Model.Name}.Entities.{targetModelNode.Model.Name}";
                        }
                    }
                    break;
                case EntityMemberType.EntitySet:
                    {
                        EntitySetModel sm = (EntitySetModel)mm;
                        var targetModelNode = designTree.FindModelNode(ModelType.Entity, sm.RefModelId);
                        typeString = $"EntityList<{targetModelNode.AppNode.Model.Name}.Entities.{targetModelNode.Model.Name}>";
                        readOnly = true;
                    }
                    break;
                case EntityMemberType.AggregationRefField:
                    typeString = "object";
                    readOnly = true;
                    break;
                //case EntityMemberType.Formula:
                //case EntityMemberType.Aggregate:
                //FormulaModel fmm = mm as FormulaModel;
                //typeString = TypeService.GetEntityFieldValueType(fmm.DataType).FullName;
                //readOnly = true;
                //break;
                case EntityMemberType.Tracker:
                    throw ExceptionHelper.NotImplemented();
                //GetEntityMemberTypeStringAndReadOnly(
                //    (mm as TrackerModel).TargetMember, ref typeString, ref readOnly);
                //readOnly = true;
                //break;
                case EntityMemberType.AutoNumber:
                    typeString = "string";
                    readOnly = true;
                    break;
                //case EntityMemberType.ImageRef:
                //typeString = TypeHelper.Type_IImageSource;
                //readOnly = false;
                //break;
                default:
                    typeString = "object";
                    break;
            }
        }

        /// <summary>
        /// 生成PermissionModel的虚拟代码
        /// </summary>
        public static string GenPermissionDummyCode(PermissionModel model, string appName)
        {
            var sb = StringBuilderCache.Acquire();
            sb.Append("namespace ");
            sb.Append(appName);
            sb.Append("{public partial class Permissions {");
            sb.Append("[");
            sb.Append(TypeHelper.MemberAccessInterceptorAttribute);
            sb.Append("(\"");
            sb.Append(ServiceInterceptors.PermissionAccessInterceptor.Name);
            sb.Append("\")]");
            sb.AppendFormat("public const bool {0} = false;", model.Name);
            sb.Append("}}");

            return StringBuilderCache.GetStringAndRelease(sb);
        }

        public static string GenWorkflowDummyCode(WorkflowModel model)
        {
            throw ExceptionHelper.NotImplemented();
            //StringBuilder sb = new StringBuilder();

            //sb.AppendFormat("namespace {0}.Workflows {{ {1} ", model.AppID, Environment.NewLine);
            ////sb.AppendFormat("[{0}(\"AppBox.Core.WorkflowInstanceInfo\")]{1}", RealTypeAttribute, Environment.NewLine);
            //sb.AppendFormat("\tpublic partial class {0} : wf.WorkflowInstance {{ {1}", model.ID.Split('.')[1], Environment.NewLine);

            ////生成参数
            //for (int i = 0; i < model.Parameters.Count; i++)
            //{
            //    var type = GetWorkflowParameterTypeString(model.Parameters[i].Type);
            //    sb.AppendFormat("public {0} {1} {{ get; set; }} {2}"
            //                    , type, model.Parameters[i].Name, Environment.NewLine);
            //}

            ////todo:生成Activity

            //sb.AppendFormat("\t}} {0}}}", Environment.NewLine);

            //return sb.ToString();
        }

        //private static string GetWorkflowParameterTypeString(WorkflowParameterType type)
        //{
        //    switch (type)
        //    {
        //        case WorkflowParameterType.Guid:
        //            return "System.Guid";
        //        case WorkflowParameterType.String:
        //            return "string";
        //        case WorkflowParameterType.ObjectArray:
        //            return "object[]";
        //        default:
        //            return "object";
        //    }
        //}
        #endregion

        #region ====Generate Expression Dummy Code Methods====

        //internal static string GenExressionDummyCode(Expression expression, string nameSpace,
        //                                             string className, string methodName,
        //                                             string returnType, string[] arguments)
        //{
        //    var rType = string.IsNullOrEmpty(returnType) ? "void" : returnType;
        //    var tabs = "";

        //    var sb = new StringBuilder();
        //    if (!string.IsNullOrEmpty(nameSpace))
        //    {
        //        sb.AppendFormat("namespace {0} {1}{{{1}", nameSpace, Environment.NewLine);
        //        tabs = "\t";
        //    }

        //    sb.Append(tabs);
        //    sb.AppendFormat("public partial class {0}{1}", className, Environment.NewLine);
        //    sb.Append(tabs);
        //    sb.AppendLine("{");

        //    tabs += "\t";
        //    sb.Append(tabs);
        //    sb.AppendFormat("public {0} {1}(", rType, methodName);
        //    if (arguments != null && arguments.Length > 0)
        //    {
        //        for (int i = 0; i < arguments.Length; i++)
        //        {
        //            sb.Append(arguments[i]);
        //            if (i != arguments.Length - 1)
        //                sb.Append(", ");
        //        }
        //    }
        //    sb.AppendLine(")");
        //    sb.Append(tabs);
        //    sb.AppendLine("{");

        //    tabs += "\t";
        //    if (rType != "void")
        //        sb.AppendFormat("{0}return ", tabs);
        //    if (!object.Equals(null, expression))
        //    {
        //        if (expression is EventAction) //多行模式
        //        {
        //            ((EventAction)expression).Body.ToCode(sb, tabs);
        //        }
        //        else if (expression is BlockExpression)
        //        {
        //            expression.ToCode(sb, tabs);
        //        }
        //        else //其他单行模式
        //        {
        //            if (rType == "void")
        //                sb.Append(tabs);
        //            expression.ToCode(sb, null);
        //            sb.Append(";");
        //        }
        //    }
        //    sb.AppendLine();
        //    tabs = tabs.Remove(0, 1);
        //    sb.Append(tabs);
        //    sb.AppendLine("}");
        //    tabs = tabs.Remove(0, 1);
        //    sb.Append(tabs);
        //    sb.AppendLine("}");
        //    if (!string.IsNullOrEmpty(nameSpace))
        //        sb.AppendLine("}");

        //    return sb.ToString();
        //}

        #endregion

        #region ====Generate Service Proxy Code====
        /// <summary>
        /// 生成服务模型的异步代理，用于服务间相互调用
        /// </summary>
        internal static async Task<string> GenProxyCode(Document document, string appName, ServiceModel model)
        {
            var rootNode = await document.GetSyntaxRootAsync();

            //先导入using
            var sb = StringBuilderCache.Acquire();
            var usings = rootNode.DescendantNodes().OfType<UsingDirectiveSyntax>().ToArray();
            for (int i = 0; i < usings.Length; i++)
            {
                sb.Append(usings[i]);
            }

            sb.Append("namespace ");
            sb.AppendFormat("{0}.Services", appName);
            sb.Append("{ public static class ");
            sb.Append(model.Name);
            sb.Append("{ ");

            var methods = rootNode
                        .DescendantNodes().OfType<ClassDeclarationSyntax>().First()
                        .DescendantNodes().OfType<MethodDeclarationSyntax>().ToList();

            foreach (var method in methods)
            {
                bool isPublic = false;
                foreach (var modifier in method.Modifiers)
                {
                    if (modifier.ValueText == "public")
                    {
                        isPublic = true;
                        break;
                    }
                }

                if (isPublic)
                {
                    sb.AppendFormat("[{0}(\"{1}\")]public static {2} {3}(",
                                    TypeHelper.InvocationInterceptorAttribute,
                                    CallServiceInterceptor.Name,
                                    method.ReturnType,
                                    method.Identifier.ValueText);

                    for (int i = 0; i < method.ParameterList.Parameters.Count; i++)
                    {
                        if (i != 0) sb.Append(',');
                        sb.Append(method.ParameterList.Parameters[i].ToString());
                    }
                    sb.Append("){throw new Exception();}");
                }
            }

            sb.Append("}}");
            return StringBuilderCache.GetStringAndRelease(sb);
        }
        #endregion
    }

}
