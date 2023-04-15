#if NETCOREAPP3_1_OR_GREATER

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using ClickHouse.Client.ADO;
using Xunit;

namespace Dapper.Tests.Providers
{
    public class ClickHouseProvider : DatabaseProvider
    {
        public override DbProviderFactory Factory => Npgsql.NpgsqlFactory.Instance;
        public override string GetConnectionString() =>
            GetConnectionString("ClickHouseConnectionString", "Server=localhost;Port=8123;Username=default;Database=test");
    }
    public class ClickHouseTests : TestBase<ClickHouseProvider>
    {
        private ClickHouseConnection GetOpenNpgsqlConnection() => (ClickHouseConnection)Provider.GetOpenConnection();

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

        [FactClickHouse]
        public void TestClickHouseArrayParameters()
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

        [FactClickHouse]
        public void TestClickHouseListParameters()
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

        [FactClickHouse]
        public void TestClickHouseChar()
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

        [FactClickHouse]
        public void TestClickHouseSelectArray()
        {
            using (var conn = GetOpenNpgsqlConnection())
            {
                var r = conn.Query<int[]>("select array[1,2,3]").ToList();
                Assert.Single(r);
                Assert.Equal(new[] { 1, 2, 3 }, r.Single());
            }
        }

        [FactClickHouse]
        public void TestClickHouseDateTimeUsage()
        {
            using (var conn = GetOpenNpgsqlConnection())
            {
                var now = DateTime.UtcNow;
                DateTime? nilA = now, nilB = null;
                _ = conn.ExecuteScalar("SELECT @now, @nilA, @nilB::timestamp", new { now, nilA, nilB });
            }
        }

        [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
        public class FactClickHouseAttribute : FactAttribute
        {
            public override string Skip
            {
                get { return unavailable ?? base.Skip; }
                set { base.Skip = value; }
            }

            private static readonly string unavailable;

            static FactClickHouseAttribute()
            {
                try
                {
                    using (DatabaseProvider<ClickHouseProvider>.Instance.GetOpenConnection()) { /* just trying to see if it works */ }
                }
                catch (Exception ex)
                {
                    unavailable = $"ClickHouse is unavailable: {ex.Message}";
                }
            }
        }
    }
}

#endif
