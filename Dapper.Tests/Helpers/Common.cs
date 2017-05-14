﻿using System;
using System.Data;
using System.Data.Common;

namespace Dapper.Tests
{
    public static class Common
    {
        public static Type GetSomeType() => typeof(SomeType);

        public static void DapperEnumValue(IDbConnection connection)
        {
            // test passing as AsEnum, reading as int
            var v = (AnEnum)connection.QuerySingle<int>("select @v, @y, @z", new { v = AnEnum.B, y = (AnEnum?)AnEnum.B, z = (AnEnum?)null });
            v.IsEqualTo(AnEnum.B);

            var args = new DynamicParameters();
            args.Add("v", AnEnum.B);
            args.Add("y", AnEnum.B);
            args.Add("z", null);
            v = (AnEnum)connection.QuerySingle<int>("select @v, @y, @z", args);
            v.IsEqualTo(AnEnum.B);

            // test passing as int, reading as AnEnum
            var k = (int)connection.QuerySingle<AnEnum>("select @v, @y, @z", new { v = (int)AnEnum.B, y = (int?)(int)AnEnum.B, z = (int?)null });
            k.IsEqualTo((int)AnEnum.B);

            args = new DynamicParameters();
            args.Add("v", (int)AnEnum.B);
            args.Add("y", (int)AnEnum.B);
            args.Add("z", null);
            k = (int)connection.QuerySingle<AnEnum>("select @v, @y, @z", args);
            k.IsEqualTo((int)AnEnum.B);
        }

        public static void TestDateTime(DbConnection connection)
        {
            DateTime? now = DateTime.UtcNow;
            try { connection.Execute("DROP TABLE Persons"); } catch { /* don't care */ }
            connection.Execute(@"CREATE TABLE Persons (id int not null, dob datetime null)");
            connection.Execute(@"INSERT Persons (id, dob) values (@id, @dob)",
                 new { id = 7, dob = (DateTime?)null });
            connection.Execute(@"INSERT Persons (id, dob) values (@id, @dob)",
                 new { id = 42, dob = now });

            var row = connection.QueryFirstOrDefault<NullableDatePerson>(
                "SELECT id, dob, dob as dob2 FROM Persons WHERE id=@id", new { id = 7 });
            row.IsNotNull();
            row.Id.IsEqualTo(7);
            row.DoB.IsNull();
            row.DoB2.IsNull();

            row = connection.QueryFirstOrDefault<NullableDatePerson>(
                "SELECT id, dob FROM Persons WHERE id=@id", new { id = 42 });
            row.IsNotNull();
            row.Id.IsEqualTo(42);
            row.DoB.Equals(now);
            row.DoB2.Equals(now);
        }

        private class NullableDatePerson
        {
            public int Id { get; set; }
            public DateTime? DoB { get; set; }
            public DateTime? DoB2 { get; set; }
        }
    }
}
