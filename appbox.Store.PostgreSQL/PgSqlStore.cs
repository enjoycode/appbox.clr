using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using appbox.Models;
using Npgsql;

namespace appbox.Store
{
    public sealed partial class PgSqlStore : SqlStore
    {
        private readonly string _connectionString;

        public override string NameEscaper => "\"";

        public override bool IsAtomicUpsertSupported => true;

        public override bool UseReaderForOutput => true;

        public PgSqlStore(string settings)
        {
            //根据设置创建ConnectionString
            var s = Newtonsoft.Json.JsonConvert.DeserializeObject<Settings>(settings);
            _connectionString = $"Server={s.Host};Port={s.Port};Database={s.Database};Userid={s.User};Password={s.Password};Enlist=true;Pooling=true;MinPoolSize=1;MaxPoolSize=200;";
        }

        #region ====overrides Create Methods====
        public override DbConnection MakeConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }

        public override DbCommand MakeCommand()
        {
            return new NpgsqlCommand();
        }

        public override DbParameter MakeParameter()
        {
            return new NpgsqlParameter();
        }
        #endregion
    }

    struct Settings
    {
        public string Host { get; set; }
        public string Port { get; set; }
        public string Database { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
    }
}
