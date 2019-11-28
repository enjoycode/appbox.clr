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
    public class TypeSystemTest
    {

        [Fact]
        public async Task GetSymbolTest()
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
                Core.Tests.TestHelper.AdminPermissionModel,
                Core.Tests.TestHelper.DeveloperPermissionModel
            };
            await ctx.DesignTree.LoadForTest(apps, models);

            var symbol = await ctx.TypeSystem.GetEntityIndexSymbolAsync("sys", "Emploee", "UI_Account_Password");
            Assert.NotNull(symbol);
            symbol = await ctx.TypeSystem.GetModelSymbolAsync(ModelType.Entity, "sys", "Emploee");
            Assert.NotNull(symbol);
        }
    }
}
