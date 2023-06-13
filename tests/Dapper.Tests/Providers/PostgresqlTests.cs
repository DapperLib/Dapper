using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using Xunit;

namespace Dapper.Tests
{
    /// <summary>
    /// If Docker Desktop is installed, run the following command to start a container suitable for the tests.
    /// <code>
    /// docker run -d -p 5432:5432 --name Dapper.Tests.PostgreSQL -e POSTGRES_DB=dappertest -e POSTGRES_USER=dappertest -e POSTGRES_PASSWORD=dapperpass postgres
    /// </code>
    /// </summary>
    public class PostgresProvider : DatabaseProvider
    {
        public override DbProviderFactory Factory => Npgsql.NpgsqlFactory.Instance;
        public override string GetConnectionString() =>
            GetConnectionString("PostgesConnectionString", "Server=localhost;Port=5432;User Id=dappertest;Password=dapperpass;Database=dappertest");
    }
    public class PostgresqlTests : TestBase<PostgresProvider>
    {
        private Npgsql.NpgsqlConnection GetOpenNpgsqlConnection() => (Npgsql.NpgsqlConnection)Provider.GetOpenConnection();

        private class Cat
        {
            public int Id { get; set; }
            public string Breed { get; set; }
            public string Name { get; set; }
        }

        private readonly Cat[] Cats =
        {
            new Cat() { Breed = "Abyssinian", Name="KACTUS"},
            new Cat() { Breed = "Aegean cat", Name="KADAFFI"},
            new Cat() { Breed = "American Bobtail", Name="KANJI"},
            new Cat() { Breed = "Balinese", Name="MACARONI"},
            new Cat() { Breed = "Bombay", Name="MACAULAY"},
            new Cat() { Breed = "Burmese", Name="MACBETH"},
            new Cat() { Breed = "Chartreux", Name="MACGYVER"},
            new Cat() { Breed = "German Rex", Name="MACKENZIE"},
            new Cat() { Breed = "Javanese", Name="MADISON"},
            new Cat() { Breed = "Persian", Name="MAGNA"}
        };

        [FactPostgresql]
        public void TestPostgresqlArrayParameters()
        {
            using (var conn = GetOpenNpgsqlConnection())
            {
                IDbTransaction transaction = conn.BeginTransaction();
                conn.Execute("create table tcat ( id serial not null, breed character varying(20) not null, name character varying (20) not null);");
                conn.Execute("insert into tcat(breed, name) values(:Breed, :Name) ", Cats);

                var r = conn.Query<Cat>("select * from tcat where id=any(:catids)", new { catids = new[] { 1, 3, 5 } });
                Assert.Equal(3, r.Count());
                Assert.Equal(1, r.Count(c => c.Id == 1));
                Assert.Equal(1, r.Count(c => c.Id == 3));
                Assert.Equal(1, r.Count(c => c.Id == 5));
                transaction.Rollback();
            }
        }

        [FactPostgresql]
        public void TestPostgresqlListParameters()
        {
            using (var conn = GetOpenNpgsqlConnection())
            {
                IDbTransaction transaction = conn.BeginTransaction();
                conn.Execute("create table tcat ( id serial not null, breed character varying(20) not null, name character varying (20) not null);");
                conn.Execute("insert into tcat(breed, name) values(:Breed, :Name) ", new List<Cat>(Cats));

                var r = conn.Query<Cat>("select * from tcat where id=any(:catids)", new { catids = new List<int> { 1, 3, 5 } });
                Assert.Equal(3, r.Count());
                Assert.Equal(1, r.Count(c => c.Id == 1));
                Assert.Equal(1, r.Count(c => c.Id == 3));
                Assert.Equal(1, r.Count(c => c.Id == 5));
                transaction.Rollback();
            }
        }

        private class CharTable
        {
            public int Id { get; set; }
            public char CharColumn { get; set; }
        }

        [FactPostgresql]
        public void TestPostgresqlChar()
        {
            using (var conn = GetOpenNpgsqlConnection())
            {
                var transaction = conn.BeginTransaction();
                conn.Execute("create table chartable (id serial not null, charcolumn \"char\" not null);");
                conn.Execute("insert into chartable(charcolumn) values('a');");

                var r = conn.Query<CharTable>("select * from chartable");
                Assert.Single(r);
                Assert.Equal('a', r.Single().CharColumn);
                transaction.Rollback();
            }
        }

        [FactPostgresql]
        public void TestPostgresqlSelectArray()
        {
            using (var conn = GetOpenNpgsqlConnection())
            {
                var r = conn.Query<int[]>("select array[1,2,3]").ToList();
                Assert.Single(r);
                Assert.Equal(new[] { 1, 2, 3 }, r.Single());
            }
        }

        [FactPostgresql]
        public void TestPostgresqlDateTimeUsage()
        {
            using (var conn = GetOpenNpgsqlConnection())
            {
                DateTime now = DateTime.UtcNow;
                DateTime? nilA = now, nilB = null;
                _ = conn.ExecuteScalar("SELECT @now, @nilA, @nilB::timestamp", new { now, nilA, nilB });
            }
        }

        [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
        public class FactPostgresqlAttribute : FactAttribute
        {
            public override string Skip
            {
                get { return unavailable ?? base.Skip; }
                set { base.Skip = value; }
            }

            private static readonly string unavailable;

            static FactPostgresqlAttribute()
            {
                try
                {
                    using (DatabaseProvider<PostgresProvider>.Instance.GetOpenConnection()) { /* just trying to see if it works */ }
                }
                catch (Exception ex)
                {
                    unavailable = $"Postgresql is unavailable: {ex.Message}";
                }
            }
        }
    }
}
