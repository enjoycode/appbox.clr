using System;
using System.IO;
using System.Collections.Generic;
using Xunit;
using appbox.Design;
using appbox.Runtime;
using appbox.Models;
using appbox.Serialization;
using System.Threading.Tasks;
using System.Text;
using Xunit.Abstractions;

namespace appbox.Design.Tests
{
    /// <summary>
    /// 模型包导入导出测试
    /// </summary>
    public class PackageTest
    {

        private readonly ITestOutputHelper output;
        private const string StoreSettings =
            "{\"Host\":\"10.211.55.2\",\"Port\":5432,\"DataBase\":\"ABStore\",\"User\":\"lushuaijun\",\"Password\":\"\"}";

        public PackageTest(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public async Task ExportTest()
        {
            Store.SqlStore.SetDefaultSqlStore(new Store.PgSqlStore(StoreSettings));

            var appId = 3250279307u; //env, sys=2660935927
            var appName = "env";

            var pkg = new AppPackage();
            await Store.ModelStore.LoadToAppPackage(appId, appName, pkg);

            //序列化
            using var wfs = File.OpenWrite($"{appName}.apk");
            var wbs = new BinSerializer(wfs);
            wbs.Serialize(pkg);
            wfs.Close();
            //反序列化
            using var rfs = File.OpenRead($"{appName}.apk");
            var rbs = new BinSerializer(rfs);
            var res = (AppPackage)rbs.Deserialize();
            foreach (var model in res.Models)
            {
                output.WriteLine($"{model.Name} {model.ModelType}");
            }
        }

    }
}
