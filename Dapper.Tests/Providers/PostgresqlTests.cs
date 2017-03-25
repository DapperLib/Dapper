#if POSTGRESQL
using System;
using System.Data;
using System.Linq;
using Xunit;

namespace Dapper.Tests
{
    public class PostcresqlTests : TestBase
    {
        private static Npgsql.NpgsqlConnection GetOpenNpgsqlConnection()
        {
            string cs = IsAppVeyor
                ? "Server=localhost;Port=5432;User Id=postgres;Password=Password12!;Database=test"
                : "Server=localhost;Port=5432;User Id=dappertest;Password=dapperpass;Database=dappertest"; // ;Encoding = UNICODE
            var conn = new Npgsql.NpgsqlConnection(cs);
            conn.Open();
            return conn;
        }

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
                conn.Execute("insert into tcat(breed, name) values(:breed, :name) ", Cats);

                var r = conn.Query<Cat>("select * from tcat where id=any(:catids)", new { catids = new[] { 1, 3, 5 } });
                r.Count().IsEqualTo(3);
                r.Count(c => c.Id == 1).IsEqualTo(1);
                r.Count(c => c.Id == 3).IsEqualTo(1);
                r.Count(c => c.Id == 5).IsEqualTo(1);
                transaction.Rollback();
            }
        }

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
                    using (GetOpenNpgsqlConnection()) { }
                }
                catch (Exception ex)
                {
                    unavailable = $"Postgresql is unavailable: {ex.Message}";
                }
            }
        }
    }
}
#endif