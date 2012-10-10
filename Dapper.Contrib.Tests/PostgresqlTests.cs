using System.Data;
using Npgsql;

namespace Dapper.Contrib.Tests
{
    public class PostgresqlTests : Tests
    {
        private bool _setUp;
        protected override IDbConnection GetOpenConnection()
        {
            var conn = new NpgsqlConnection(ConnectionString);
            conn.Open();
            return conn;
        }

        protected override string ConnectionString
        {
            get { return "Server=127.0.0.1;Port=5432;User Id=test;Password=testpass;Database=test;"; }
        }

        public override void SetUpTests()
        {
            if (_setUp)
                return;

            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                conn.Open();

                conn.Execute("DROP TABLE IF EXISTS Users");
                conn.Execute("DROP TABLE IF EXISTS Automobiles");
                conn.Execute("DROP TABLE IF EXISTS Houses");

                conn.Execute("CREATE TABLE Users (id SERIAL NOT NULL, name text NOT NULL, age integer NOT NULL)");
                conn.Execute("CREATE TABLE Automobiles (id SERIAL NOT NULL, name text NOT NULL)");
                conn.Execute("CREATE TABLE Houses (id integer NOT NULL, number integer NOT NULL, road text NOT NULL)");
            }
            _setUp = true;
        }
    }
}