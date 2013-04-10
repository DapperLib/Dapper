using System.Data;
using System.Data.SqlServerCe;
using System.IO;
using System.Linq;
using System.Reflection;
using Dapper.Contrib.Extensions;
using System.Collections.Generic;
using System;
using Dapper;


namespace Dapper.Data.Tests
{

	public class User : SqlMapper.IDynamicParameters
    {
        public string Name { get; set; }
        public int Age { get; set; }

		public void AddParameters(IDbCommand command, SqlMapper.Identity identity)
		{
			throw new NotImplementedException();
		}

        public void AddParameters(System.Data.IDbCommand command, SqlMapper.Identity identity)
        {
            throw new NotImplementedException();
        }
    }

    public class Car
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class Tests
    {
        private IDbContext Db
        {
			get { return TestDb.Instance(); }
            
        }

        public void InsertAndSelect()
        {
			var rand = new Random(8675309);
			var data = new List<User>();
			// reusing singe connection
	        Db.Batch(s =>
		    {
			    for (int i = 0; i < 100; i++)
			    {
				    var user = new User {Age = rand.Next(70), Name = Guid.NewGuid().ToString()};
				    data.Add(user);
					s.Execute("insert into Users (Age, Name) values (@Age, @Name)", user);
			    }
		    });

			var builder = new SqlBuilder();
			var justId = builder.AddTemplate("SELECT /**select**/ FROM Users");
			var all = builder.AddTemplate("SELECT Name, /**select**/, Age FROM Users");

			builder.Select("Id");

			var ids = Db.Query<int>(justId.RawSql, justId.Parameters);
			var users = Db.Query<User>(all.RawSql, all.Parameters);
			ids.Count().IsEqualTo(data.Count);
			users.Select(u=>u.Name).IsSequenceEqualTo(data.Select(u=>u.Name));
        }

        public void BuilderTemplateWOComposition()
        {
			var builder = new SqlBuilder();
			var template = builder.AddTemplate("SELECT COUNT(*) FROM Users WHERE Age = @age", new {age = 5});

			if (template.RawSql == null) throw new Exception("RawSql null");
			if (template.Parameters == null) throw new Exception("Parameters null");

			Db.Execute("insert into Users (Age, Name) values (@Age, @Name)", new User { Age = 5, Name = "Testy McTestington" });
			if (Db.Query<int>(template.RawSql, template.Parameters).Single() != 1)
			{ throw new Exception("Query failed"); }
        }
    }
}
