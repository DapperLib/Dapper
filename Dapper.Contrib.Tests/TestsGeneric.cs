using System.Data;
using System.Data.SqlServerCe;
using System.IO;
using System.Linq;
using System.Reflection;
using Dapper.Contrib.Extensions;
using System.Collections.Generic;
using System;
using Dapper;


namespace Dapper.Contrib.Tests
{
    public interface IPerson<TKey>
    {
        [Key]
        TKey Id { get; set; }
        string Name { get; set; }
        int Age { get; set; }
    }

    public class Person<TKey> : IPerson<TKey>
    {
        [Key]
        public TKey Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
    }
    
    [Table("Persons")]
    public class Person : IPerson<Int64>
    {
        [Key(false)]
        public Int64 Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
    }

    [Table("Managers")]
    public class Manager : IPerson<Guid>
    {
        public Manager()
        {
            this.Id = Guid.NewGuid();
        }
        [Key(true)]
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
    }

    public class TestsGeneric
    {
        private IDbConnection GetOpenConnection()
        {
            var projLoc = Assembly.GetAssembly(GetType()).Location;
            var projFolder = Path.GetDirectoryName(projLoc);

            var connection = new SqlCeConnection("Data Source = " + projFolder + "\\Test.sdf;");
            connection.Open();
            return connection;
        }
        
        public void TestSimpleGet()
        {
            using (var connection = GetOpenConnection())
            {
                var id = connection.Insert<Person,Int64>(new Person { Name = "Adama", Age = 10 });
                var user = connection.Get<Person>(id);
                user.Id.IsEqualTo((Int64)id);
                user.Name.IsEqualTo("Adama");
                connection.Delete(user);
            }
        }

        public void TestSimpleGetGuid()
        {
            using (var connection = GetOpenConnection())
            {
                Guid id = Guid.Empty;
                Manager manager = new Manager { Name = "Adama", Age = 10 };
                id = manager.Id;
                connection.Insert<Manager, String>(manager);
                manager.Id.IsEqualTo(id);
                Manager manager1 = connection.Get<Manager>(id);
                manager1.Id.IsEqualTo(id);
                manager1.Name.IsEqualTo("Adama");
                connection.Delete(manager1);
            }
        }

        public void InsertGetUpdate()
        {
            using (var connection = GetOpenConnection())
            {
                connection.Get<Person>(3).IsNull();

                var id = connection.Insert(new Person { Name = "Adam", Age = 10 });

                //get a user with "isdirty" tracking
                var user = connection.Get<IPerson<Int64>>(id);
                user.Name.IsEqualTo("Adam");
                connection.Update(user).IsEqualTo(false);    //returns false if not updated, based on tracking
                user.Name = "Bob";
                connection.Update(user).IsEqualTo(true);    //returns true if updated, based on tracking
                user = connection.Get<IPerson<Int64>>(id);
                user.Name.IsEqualTo("Bob");

                //get a user with no tracking
                var notrackedUser = connection.Get<Person>(id);
                notrackedUser.Name.IsEqualTo("Bob");
                connection.Update(notrackedUser).IsEqualTo(true);   //returns true, even though user was not changed
                notrackedUser.Name = "Cecil";
                connection.Update(notrackedUser).IsEqualTo(true);
                connection.Get<Person>(id).Name.IsEqualTo("Cecil");

                connection.Query<Person>("select * from Persons").Count().IsEqualTo(1);
                connection.Delete(user).IsEqualTo(true);
                connection.Query<Person>("select * from Persons").Count().IsEqualTo(0);

                connection.Update(notrackedUser).IsEqualTo(false);   //returns false, user not found
            }
        }

        public void InsertCheckKey()
        {
            using (var connection = GetOpenConnection())
            {
                connection.Get<IPerson<Int64>>(3).IsNull();
                Person user = new Person { Name = "Adamb", Age = 10 };
                Int64 id = (Int64)connection.Insert<Person,Int64>(user);
                user.Id.IsEqualTo(id);
            }
        }

        public void BuilderSelectClause()
        {
            using (var connection = GetOpenConnection())
            {
                var rand = new Random(8675309);
                var data = new List<Person>();
                for (int i = 0; i < 100; i++)
                {
                    var nU = new Person { Age = rand.Next(70), Id = i, Name = Guid.NewGuid().ToString() };
                    data.Add(nU);
                    nU.Id = (Int64)connection.Insert<Person,Int64>(nU);
                }

                var builder = new SqlBuilder();
                var justId = builder.AddTemplate("SELECT /**select**/ FROM Persons");
                var all = builder.AddTemplate("SELECT Name, /**select**/, Age FROM Persons");

                builder.Select("Id");

                var ids = connection.Query<Int64>(justId.RawSql, justId.Parameters);
                var users = connection.Query<Person>(all.RawSql, all.Parameters);

                foreach (var u in data)
                {
                    if (!ids.Any(i => u.Id == i)) throw new Exception("Missing ids in select");
                    if (!users.Any(a => a.Id == u.Id && a.Name == u.Name && a.Age == u.Age)) throw new Exception("Missing Persons in select");
                }
            }
        }

        public void BuilderTemplateWOComposition()
        {
            var builder = new SqlBuilder();
            var template = builder.AddTemplate("SELECT COUNT(*) FROM Persons WHERE Age = @age", new { age = 5 });

            if (template.RawSql == null) throw new Exception("RawSql null");
            if (template.Parameters == null) throw new Exception("Parameters null");

            using (var connection = GetOpenConnection())
            {
                connection.Insert(new Person { Age = 5, Name = "Testy McTestington" });

                if (connection.Query<int>(template.RawSql, template.Parameters).Single() != 1)
                    throw new Exception("Query failed");
            }
        }
    }
}
