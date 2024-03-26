using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using Xunit;

namespace Dapper.Tests
{
    public class CockroachDBProvider : DatabaseProvider
    {
        public override DbProviderFactory Factory => Npgsql.NpgsqlFactory.Instance;
        public override string GetConnectionString() =>
            GetConnectionString("CockroachDBConnectionString", "Server=localhost;Port=26257;User Id=root;Database=defaultdb");
    }
    public class CockroachDBTests : TestBase<CockroachDBProvider>
    {
        private Npgsql.NpgsqlConnection GetOpenNpgsqlConnection() => (Npgsql.NpgsqlConnection)Provider.GetOpenConnection();

        private class Cat
        {
            public Int64 Id { get; set; }
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

        [FactCockroachDB]
        public void TestCockroachDBArrayParameters()
        {
            using (var conn = GetOpenNpgsqlConnection())
            {
                IDbTransaction transaction = conn.BeginTransaction();
                conn.Execute("create table tcat ( id serial not null, breed character varying(20) not null, name character varying (20) not null);");
                conn.Execute("insert into tcat(breed, name) values(:Breed, :Name) ", Cats);
                var r = conn.Query<Cat>("select * from tcat;");
                Assert.Equal(10, r.Count());
                transaction.Rollback();
            }
        }

        [FactCockroachDB]
        public void TestCockroachDBListParameters()
        {
            using (var conn = GetOpenNpgsqlConnection())
            {
                IDbTransaction transaction = conn.BeginTransaction();
                conn.Execute("create table tcat ( id serial not null, breed character varying(20) not null, name character varying (20) not null);");
                conn.Execute("insert into tcat(breed, name) values(:Breed, :Name) ", new List<Cat>(Cats));

                var r = conn.Query<Cat>("select * from tcat;");
                Assert.Equal(10, r.Count());
                transaction.Rollback();
            }
        }

        private class CharTable
        {
            public Int64 Id { get; set; }
            public char CharColumn { get; set; }
        }

        [FactCockroachDB]
        public void TestCockroachDBChar()
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

        [FactCockroachDB]
        public void TestCockroachDBSelectArray()
        {
            using (var conn = GetOpenNpgsqlConnection())
            {
                var r = conn.Query<int[]>("select array[1,2,3]").ToList();
                Assert.Single(r);
                Assert.Equal(new[] { 1, 2, 3 }, r.Single());
            }
        }

        [FactCockroachDB]
        public void TestCockroachDBDateTimeUsage()
        {
            using (var conn = GetOpenNpgsqlConnection())
            {
                DateTime now = DateTime.UtcNow;
                DateTime? nilA = now, nilB = null;
                _ = conn.ExecuteScalar("SELECT @now, @nilA, @nilB::timestamp", new { now, nilA, nilB });
            }
        }

        [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
        public class FactCockroachDBAttribute : FactAttribute
        {
            public override string Skip
            {
                get { return unavailable ?? base.Skip; }
                set { base.Skip = value; }
            }

            private static readonly string unavailable;

            static FactCockroachDBAttribute()
            {
                try
                {
                    using (DatabaseProvider<CockroachDBProvider>.Instance.GetOpenConnection()) { /* just trying to see if it works */ }
                }
                catch (Exception ex)
                {
                    unavailable = $"CockroachDB is unavailable: {ex.Message}";
                }
            }
        }
    }
}
