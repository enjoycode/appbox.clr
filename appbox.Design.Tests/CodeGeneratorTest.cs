using System;
using System.Collections.Generic;
using Xunit;
using appbox.Design;
using appbox.Runtime;
using appbox.Models;
using System.Threading.Tasks;
using System.Text;

namespace appbox.Design.Tests
{
    public class CodeGeneratorTest
    {
        [Fact]
        public void GenEntityModelTest()
        {
            var model = Core.Tests.TestHelper.EmploeeModel;
            var code = CodeGenService.GenEntityDummyCode(model, Consts.SYS, null);
            Console.WriteLine(code);
        }

        [Fact]
        public async Task GenServiceCodeTest()
        {
            RuntimeContext.Init(new MockRuntimContext(), 10410);

            var session = new MockDeveloperSession();
            var ctx = new DesignHub(session);
            var apps = new List<ApplicationModel>() { Core.Tests.TestHelper.SysAppModel };
            var models = new List<ModelBase>
            {
                Core.Tests.TestHelper.EmploeeModel,
                Core.Tests.TestHelper.VehicleStateModel,
                Core.Tests.TestHelper.OrgUnitModel,
                Core.Tests.TestHelper.CityModel,
                Core.Tests.TestHelper.AdminPermissionModel,
                Core.Tests.TestHelper.DeveloperPermissionModel
            };
            await ctx.DesignTree.LoadForTest(apps, models);

            //模拟添加存储模型，参照NewDataStore Handler
            var storeNode = ctx.DesignTree.StoreRootNode.AddModel(Core.Tests.TestHelper.SqlStoreModel, ctx);
            ctx.TypeSystem.CreateStoreDocument(storeNode);

            //模拟添加服务模型, 参照NewServiceModel Handler
            var serviceRootNode = ctx.DesignTree.FindModelRootNode(Consts.SYS_APP_ID, ModelType.Service);
            var parentNode = serviceRootNode;
            var modelId = (ulong)Consts.SYS_APP_ID << 32;
            modelId |= (ulong)ModelType.Service << 24;
            modelId |= (ulong)1 << 3;
            modelId |= (ulong)ModelLayer.DEV << 1;

            var model = new ServiceModel(modelId, "HelloService");
            var node = new ModelNode(model, ctx);
            parentNode.Nodes.Add(node);
            serviceRootNode.AddModelIndex(node);
            node.CheckoutInfo = new CheckoutInfo(node.NodeType, node.CheckoutInfoTargetID, model.Version,
                                                 ctx.Session.Name, ctx.Session.LeafOrgUnitID);

            var souceCode = Resources.LoadStringResource("Resources.Code.HelloService.cs");
            await ctx.TypeSystem.CreateModelDocumentAsync(node, souceCode);

            //生成服务代码
            var data = await PublishService.CompileServiceAsync(ctx, model);
            Assert.NotNull(data);
        }

        /// <summary>
        /// 测试生成服务模型用于前端TypeScript的声明代码
        /// </summary>
        [Fact]
        public async Task GenServiceDeclareTest()
        {
            RuntimeContext.Init(new MockRuntimContext(), 10410);

            var session = new MockDeveloperSession();
            var ctx = new DesignHub(session);
            var apps = new List<ApplicationModel>() { Core.Tests.TestHelper.SysAppModel };
            var models = new List<ModelBase>
            {
                Core.Tests.TestHelper.EmploeeModel,
                Core.Tests.TestHelper.VehicleStateModel,
                Core.Tests.TestHelper.OrgUnitModel
            };
            await ctx.DesignTree.LoadForTest(apps, models);

            //模拟添加, 参照NewServiceModel Handler
            var rootNode = ctx.DesignTree.FindModelRootNode(Consts.SYS_APP_ID, ModelType.Service);
            var parentNode = rootNode;
            var modelId = (ulong)Consts.SYS_APP_ID << 32;
            modelId |= (ulong)ModelType.Service << 24;
            modelId |= (ulong)1 << 3;
            modelId |= (ulong)ModelLayer.DEV << 1;
            var model = new ServiceModel(modelId, "HelloService");
            var node = new ModelNode(model, ctx);
            parentNode.Nodes.Add(node);
            rootNode.AddModelIndex(node);
            node.CheckoutInfo = new CheckoutInfo(node.NodeType, node.CheckoutInfoTargetID, model.Version,
                                                 ctx.Session.Name, ctx.Session.LeafOrgUnitID);

            var sourceCode = Resources.LoadStringResource("Resources.Code.HelloService.cs");
            await ctx.TypeSystem.CreateModelDocumentAsync(node, sourceCode);

            //生成服务声明代码
            var appName = node.AppNode.Model.Name;
            var doc = ctx.TypeSystem.Workspace.CurrentSolution.GetDocument(node.RoslynDocumentId);
            var semanticModel = await doc.GetSemanticModelAsync();
            //TODO: 检测虚拟代码错误
            var codegen = new ServiceDeclareGenerator(ctx, appName, semanticModel, (ServiceModel)node.Model);
            codegen.Visit(semanticModel.SyntaxTree.GetRoot());
            var declare = codegen.GetDeclare();
            Console.WriteLine(declare);
        }
    }
}
