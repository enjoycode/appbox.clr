using System;
using Xunit;
using appbox.Models;
using System.Collections.Generic;
using Xunit.Abstractions;

namespace appbox.Store.Tests
{
    public class SqlStoreTests
    {

        private readonly ITestOutputHelper output;
        private const string StoreSettings =
            "{\"Host\":\"10.211.55.2\",\"Port\":5432,\"DataBase\":\"DpsStore\",\"User\":\"lushuaijun\",\"Password\":\"\"}";

        private EntityModel MakeTestSqlModel()
        {
            var model = new EntityModel(0x12345678ul, "Emploee", 0x1234ul);
            var m_code = new DataFieldModel(model, "Code", EntityFieldType.Int32);
            model.AddSysMember(m_code, 1);
            var m_name = new DataFieldModel(model, "Name", EntityFieldType.String);
            model.AddSysMember(m_name, 2);

            var pk = new List<SqlField>
            {
                new SqlField { MemberId = 1, OrderByDesc = false }
            };
            model.SqlStoreOptions.SetPrimaryKeys(model, pk);
            return model;
        }

        public SqlStoreTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void OpenConnectionTest()
        {
            var store = new PgSqlStore(StoreSettings);
            var conn = store.MakeConnection();
            conn.Open();
            conn.Close();
        }

        /// <summary>
        /// 测试DDL Create Table
        /// </summary>
        [Fact]
        public void CreateTableTest()
        {
            var model = MakeTestSqlModel();

            var store = new PgSqlStore(StoreSettings);
            var cmds = store.MakeCreateTable(model);
            Assert.True(cmds != null);
            output.WriteLine(cmds[0].CommandText);
        }

        [Fact]
        public void BuildQueryTest()
        {
            var model = MakeTestSqlModel();
            var mockRunntimeContext = new MockRuntimContext();
            Runtime.RuntimeContext.Init(mockRunntimeContext, 10410);
            mockRunntimeContext.AddModel(model);

            var q = new SqlQuery(model.Id);
            q.Where(q.T["Code"] >= 1 & q.T["Code"] < 10);
            SqlQuery.AddAllSelects(q, model, q.T, null); //Test only

            var store = new PgSqlStore(StoreSettings);
            var cmd = store.BuildQuery(q);
            Assert.True(cmd != null);
            output.WriteLine(cmd.CommandText);
        }
    }
}
