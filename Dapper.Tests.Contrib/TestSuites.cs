using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using Xunit;
using Xunit.Sdk;
using Oracle.ManagedDataAccess.Client;
#if COREFX
using Microsoft.Data.Sqlite;
using IDbConnection = System.Data.Common.DbConnection;
#else
using System.Data.SQLite;
using System.Data.SqlServerCe;
using MySql.Data.MySqlClient;
using SqliteConnection = System.Data.SQLite.SQLiteConnection;
#endif

namespace Dapper.Tests.Contrib
{
    // The test suites here implement TestSuiteBase so that each provider runs
    // the entire set of tests without declarations per method
    // If we want to support a new provider, they need only be added here - not in multiple places

#if XUNIT2
    [XunitTestCaseDiscoverer("Dapper.Tests.SkippableFactDiscoverer", "Dapper.Tests.Contrib")]
    public class SkippableFactAttribute : FactAttribute { }
#endif

    public class SqlServerTestSuite : TestSuite
    {
        const string DbName = "tempdb";
        public static string ConnectionString =>
            IsAppVeyor
                ? @"Server=(local)\SQL2014;Database=tempdb;User ID=sa;Password=Password12!"
                : $"Data Source=.;Initial Catalog={DbName};Integrated Security=True";
        public override IDbConnection GetConnection() => new SqlConnection(ConnectionString);

        static SqlServerTestSuite()
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                // ReSharper disable once AccessToDisposedClosure
                Action<string> dropTable = name => connection.Execute($@"IF OBJECT_ID('{name}', 'U') IS NOT NULL DROP TABLE [{name}]; ");
                connection.Open();
                dropTable("Stuff");
                connection.Execute(@"CREATE TABLE Stuff (TheId int IDENTITY(1,1) not null, Name nvarchar(100) not null, Created DateTime null);");
                dropTable("People");
                connection.Execute(@"CREATE TABLE People (Id int IDENTITY(1,1) not null, Name nvarchar(100) not null);");
                dropTable("Users");
                connection.Execute(@"CREATE TABLE Users (Id int IDENTITY(1,1) not null, Name nvarchar(100) not null, Age int not null);");
                dropTable("Automobiles");
                connection.Execute(@"CREATE TABLE Automobiles (Id int IDENTITY(1,1) not null, Name nvarchar(100) not null);");
                dropTable("Results");
                connection.Execute(@"CREATE TABLE Results (Id int IDENTITY(1,1) not null, Name nvarchar(100) not null, [Order] int not null);");
                dropTable("ObjectX");
                connection.Execute(@"CREATE TABLE ObjectX (ObjectXId nvarchar(100) not null, Name nvarchar(100) not null);");
                dropTable("ObjectY");
                connection.Execute(@"CREATE TABLE ObjectY (ObjectYId int not null, Name nvarchar(100) not null);");
                dropTable("ObjectZ");
                connection.Execute(@"CREATE TABLE ObjectZ (Id int not null, Name nvarchar(100) not null);");
            }
        }
    }

#if !COREFX
    public class MySqlServerTestSuite : TestSuite
    {
        const string DbName = "DapperContribTests";

        public static string ConnectionString =>
            IsAppVeyor
                ? @"Server=localhost;Uid=root;Pwd=Password12!;"
                : $"Server=localhost;Uid=root;Pwd=Password12!;";

        public override IDbConnection GetConnection()
        {
            if (_skip) throw new SkipTestException("Skipping MySQL Tests - no server.");
            return new MySqlConnection(ConnectionString);
        }

        private static readonly bool _skip;

        static MySqlServerTestSuite()
        {
            try
            {
                using (var connection = new MySqlConnection(ConnectionString))
                {
                    // ReSharper disable once AccessToDisposedClosure
                    Action<string> dropTable = name => connection.Execute($@"DROP TABLE IF EXISTS `{name}`;");
                    connection.Open();
                    connection.Execute($@"DROP DATABASE IF EXISTS {DbName}; CREATE DATABASE {DbName}; USE {DbName};");
                    dropTable("Stuff");
                    connection.Execute(@"CREATE TABLE Stuff (TheId int not null AUTO_INCREMENT PRIMARY KEY, Name nvarchar(100) not null, Created DateTime null);");
                    dropTable("People");
                    connection.Execute(@"CREATE TABLE People (Id int not null AUTO_INCREMENT PRIMARY KEY, Name nvarchar(100) not null);");
                    dropTable("Users");
                    connection.Execute(@"CREATE TABLE Users (Id int not null AUTO_INCREMENT PRIMARY KEY, Name nvarchar(100) not null, Age int not null);");
                    dropTable("Automobiles");
                    connection.Execute(@"CREATE TABLE Automobiles (Id int not null AUTO_INCREMENT PRIMARY KEY, Name nvarchar(100) not null);");
                    dropTable("Results");
                    connection.Execute(@"CREATE TABLE Results (Id int not null AUTO_INCREMENT PRIMARY KEY, Name nvarchar(100) not null, `Order` int not null);");
                    dropTable("ObjectX");
                    connection.Execute(@"CREATE TABLE ObjectX (ObjectXId nvarchar(100) not null, Name nvarchar(100) not null);");
                    dropTable("ObjectY");
                    connection.Execute(@"CREATE TABLE ObjectY (ObjectYId int not null, Name nvarchar(100) not null);");
                    dropTable("ObjectZ");
                    connection.Execute(@"CREATE TABLE ObjectZ (Id int not null, Name nvarchar(100) not null);");
                }
            }
            catch (MySqlException e)
            {
                if (e.Message.Contains("Unable to connect"))
                    _skip = true;
                else
                    throw;
            }
        }
    }
#endif

#if !COREFX && !DNX451
    // This doesn't work on DNX right now due to:
    // In Visual Studio: Interop loads (works from console, though)
    // In general: parameter names, see https://github.com/StackExchange/dapper-dot-net/issues/375
    public class SQLiteTestSuite : TestSuite
    {
        const string FileName = "Test.DB.sqlite";
        public static string ConnectionString => $"Filename={FileName};";
        public override IDbConnection GetConnection() => new SqliteConnection(ConnectionString);

        static SQLiteTestSuite()
        {
            if (File.Exists(FileName))
            {
                File.Delete(FileName);
            }
            SqliteConnection.CreateFile(FileName);
            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();
                connection.Execute(@"CREATE TABLE Stuff (TheId integer primary key autoincrement not null, Name nvarchar(100) not null, Created DateTime null) ");
                connection.Execute(@"CREATE TABLE People (Id integer primary key autoincrement not null, Name nvarchar(100) not null) ");
                connection.Execute(@"CREATE TABLE Users (Id integer primary key autoincrement not null, Name nvarchar(100) not null, Age int not null) ");
                connection.Execute(@"CREATE TABLE Automobiles (Id integer primary key autoincrement not null, Name nvarchar(100) not null) ");
                connection.Execute(@"CREATE TABLE Results (Id integer primary key autoincrement not null, Name nvarchar(100) not null, [Order] int not null) ");
                connection.Execute(@"CREATE TABLE ObjectX (ObjectXId nvarchar(100) not null, Name nvarchar(100) not null) ");
                connection.Execute(@"CREATE TABLE ObjectY (ObjectYId integer not null, Name nvarchar(100) not null) ");
                connection.Execute(@"CREATE TABLE ObjectZ (Id integer not null, Name nvarchar(100) not null) ");
            }
        }
    }
#endif

#if !COREFX
    public class SqlCETestSuite : TestSuite
    {
        const string FileName = "Test.DB.sdf";
        public static string ConnectionString => $"Data Source={FileName};";
        public override IDbConnection GetConnection() => new SqlCeConnection(ConnectionString);
            
        static SqlCETestSuite()
        {
            if (File.Exists(FileName))
            {
                File.Delete(FileName);
            }
            var engine = new SqlCeEngine(ConnectionString);
            engine.CreateDatabase();
            using (var connection = new SqlCeConnection(ConnectionString))
            {
                connection.Open();
                connection.Execute(@"CREATE TABLE Stuff (TheId int IDENTITY(1,1) not null, Name nvarchar(100) not null, Created DateTime null) ");
                connection.Execute(@"CREATE TABLE People (Id int IDENTITY(1,1) not null, Name nvarchar(100) not null) ");
                connection.Execute(@"CREATE TABLE Users (Id int IDENTITY(1,1) not null, Name nvarchar(100) not null, Age int not null) ");
                connection.Execute(@"CREATE TABLE Automobiles (Id int IDENTITY(1,1) not null, Name nvarchar(100) not null) ");
                connection.Execute(@"CREATE TABLE Results (Id int IDENTITY(1,1) not null, Name nvarchar(100) not null, [Order] int not null) ");
                connection.Execute(@"CREATE TABLE ObjectX (ObjectXId nvarchar(100) not null, Name nvarchar(100) not null) ");
                connection.Execute(@"CREATE TABLE ObjectY (ObjectYId int not null, Name nvarchar(100) not null) ");
                connection.Execute(@"CREATE TABLE ObjectZ (Id int not null, Name nvarchar(100) not null) ");
            }
            Console.WriteLine("Created database");
        }
    }
#endif


#if !COREFX
    public class OracleTestSuite : TestSuite
    {
        // A usable version of Oracle for testing can be downloaded from 
        // http://www.oracle.com/technetwork/database/database-technologies/express-edition/downloads/index-083047.html
        

        public static string ConnectionString => "Data Source=MyOracleDB;User Id=myUsername;Password=myPassword;Integrated Security=no;";
        
        public override IDbConnection GetConnection() => new OracleConnection(ConnectionString);
        
        static OracleTestSuite()
        {
            using (var connection = new OracleConnection(ConnectionString))
            {
                connection.Open();
                const string schema = "testing";

                Action<string, string> DropTable = (table, sequence) =>
                {
                    //connection.Execute($@"EXECUTE IMMEDIATE '
                    //    DECLARE itemExists NUMBER;
                    //    BEGIN
                    //        itemExists := 0;

                    //        SELECT COUNT(CONSTRAINT_NAME) INTO itemExists
                    //            FROM ALL_CONSTRAINTS
                    //            WHERE UPPER(CONSTRAINT_NAME) = UPPER('{constraint}');

                    //        IF itemExists > 0 THEN
                    //            EXECUTE IMMEDIATE 'ALTER TABLE {table} DROP CONSTRAINT {constraint}';
                    //        END IF;
                    //    END; ';
                    //");
                    if (string.IsNullOrWhiteSpace(sequence))
                    {
                        connection.Execute($@"DROP SEQUENCE {sequence};");
                    }
                    connection.Execute($@"EXECUTE IMMEDIATE '
                        DECLARE itemExists NUMBER;
                        BEGIN
                            itemExists := 0;

                            SELECT COUNT(*) INTO itemExists
                                FROM ALL_TABLES
                                WHERE OWNER = UPPER('{schema}') AND TABLE_NAME = UPPER('{table}');
                            
                            IF itemExists > 0 THEN
                                EXECUTE IMMEDIATE 'DROP TABLE {schema}.{table} CASCADE CONSTRAINTS PURGE';
                            END IF
                        END';

                    ");
                };

                DropTable("Stuff", "Stuff_TheId_SEQ");
                connection.Execute($@"CREATE TABLE {schema}.Stuff (TheId number(10,0) not null, Name varchar2(100) not null, Created DATE null);");
                connection.Execute($@"ALTER TABLE {schema}.Stuff ADD CONSTRAINT Stuff_pk PRIMARY KEY (TheId);");
                connection.Execute(@"CREATE SEQUENCE Stuff_TheId_SEQ MINVALUE 1 START WITH 1 INCREMENT BY 1 CACHE 20;");

                DropTable("People", "People_Id_SEQ");
                connection.Execute($@"CREATE TABLE {schema}.People (Id number(10,0) not null, Name varchar2(100) not null);");
                connection.Execute($@"ALTER TABLE {schema}.People ADD CONSTRAINT People_pk PRIMARY KEY (Id);");
                connection.Execute(@"CREATE SEQUENCE People_Id_SEQ MINVALUE 1 START WITH 1 INCREMENT BY 1 CACHE 20;");

                DropTable("Users", "Users_Id_SEQ");
                connection.Execute($@"CREATE TABLE {schema}.Users (Id number(10,0) not null, Name varchar2(100) not null, Age number(10,0) not null);");
                connection.Execute($@"ALTER TABLE {schema}.Users ADD CONSTRAINT Users_pk PRIMARY KEY (Id);");
                connection.Execute(@"CREATE SEQUENCE Users_Id_SEQ MINVALUE 1 START WITH 1 INCREMENT BY 1 CACHE 20;");

                DropTable("Automobiles", "Automobiles_Id_SEQ");
                connection.Execute($@"CREATE TABLE {schema}.Automobiles (Id number(10,0) not null, Name varchar2(100) not null);");
                connection.Execute($@"ALTER TABLE {schema}.Automobiles ADD CONSTRAINT Automobiles_pk PRIMARY KEY (Id);");
                connection.Execute(@"CREATE SEQUENCE Automobiles_Id_SEQ MINVALUE 1 START WITH 1 INCREMENT BY 1 CACHE 20;");

                DropTable("Results", "Results_Id_SEQ");
                connection.Execute($@"CREATE TABLE {schema}.Results (Id number(10,0) not null, Name varchar2(100) not null, [Order] number(10,0) not null);");
                connection.Execute($@"ALTER {schema}.Results ADD CONSTRAINT Results_pk PRIMARY KEY (Id);");
                connection.Execute(@"CREATE SEQUENCE Results_Id_SEQ MINVALUE 1 START WITH 1 INCREMENT BY 1 CACHE 20;");

                DropTable("ObjectX", null);
                connection.Execute($@"CREATE TABLE {schema}.ObjectX (ObjectXId varchar2(100) not null, Name varchar2(100) not null);");

                DropTable("ObjectY", null);
                connection.Execute($@"CREATE TABLE {schema}.ObjectY (ObjectYId number(10,0) not null, Name varchar2(100) not null);");

                DropTable("ObjectZ", null);
                connection.Execute($@"CREATE TABLE {schema}.ObjectZ (Id number(10,0) not null, Name varchar2(100) not null);");
            }
        }
    }
#endif
}
