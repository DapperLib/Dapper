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
                return "Data Source = " + _projFolder + "\\Test.sdf;";
            }
        }

        public override void SetUpTests()
        {
            if (_setUp)
                return;

            if (File.Exists(_projFolder + "\\Test.sdf"))
                File.Delete(_projFolder + "\\Test.sdf");
            var connectionString = "Data Source = " + _projFolder + "\\Test.sdf;";
            var engine = new SqlCeEngine(connectionString);
            engine.CreateDatabase();

            using (var connection = new SqlCeConnection(connectionString))
            {
                connection.Open();
                connection.Execute(@" create table Users (Id int IDENTITY(1,1) not null, Name nvarchar(100) not null, Age int not null) ");
                connection.Execute(@" create table Automobiles (Id int IDENTITY(1,1) not null, Name nvarchar(100) not null) ");
                connection.Execute(@" create table Houses (Id int not null, Number int not null, Road nvarchar(100) not null) ");
            }
            _setUp = true;
        }
    }
}
