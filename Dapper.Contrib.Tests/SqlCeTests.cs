using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlServerCe;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Dapper.Contrib.Tests
{
    class SqlCeTests : Tests
    {
        private readonly string _assemblyLocation;
        private readonly string _projFolder;
        private const string DatabaseFilename = "Test.sdf";
        private bool _setUp;

        public SqlCeTests()
        {
            _assemblyLocation = Assembly.GetAssembly(GetType()).Location;
            _projFolder = Path.GetDirectoryName(_assemblyLocation);
        }

        protected override IDbConnection GetOpenConnection()
        {
            var connection = new SqlCeConnection(ConnectionString);
            connection.Open();
            return connection;
        }

        protected override string ConnectionString
        {
            get
            {
                return string.Format("Data Source = {0}\\{1};", _projFolder, DatabaseFilename);
            }
        }

        public override void SetUpTests()
        {
            if (_setUp)
                return;

            string fullPath = Path.Combine(_projFolder, DatabaseFilename);
            if (File.Exists(fullPath))
                File.Delete(fullPath);
            var connectionString = ConnectionString;
            var engine = new SqlCeEngine(connectionString);
            engine.CreateDatabase();

            using (var connection = new SqlCeConnection(connectionString))
            {
                connection.Open();
                connection.Execute(@" create table Users (Id int IDENTITY(1,1) not null, Name nvarchar(100) not null, Age int not null) ");
                connection.Execute(@" create table Automobiles (Id int IDENTITY(1,1) not null, Name nvarchar(100) not null) ");
            }
            _setUp = true;
        }
    }
}
