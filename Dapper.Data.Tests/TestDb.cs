using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dapper.Data.Tests
{
	sealed class TestDb : DbContext
	{
		private const string ConnectionName = "DefaultConnection";
		private static readonly IDbContext Db  = new TestDb();

		private TestDb()
			: base(ConnectionName)
		{ }

		public static IDbContext Instance()
		{
			return Db;
		}
	}
}
