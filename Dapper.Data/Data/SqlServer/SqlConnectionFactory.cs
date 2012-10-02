using System.Data;
using System.Data.Common;

namespace Dapper.Data.SqlClient
{
	/// <summary>
	/// Default implementation of SQL Server Connection Factory 
	/// </summary>
	public class SqlConnectionFactory : IDbConnectionFactory
	{
		private SqlConnectionFactory(SqlConnectionBuilder builder)
		{
			ConnectionString = builder.ToString();
		}

		public SqlConnectionFactory(string serverName, string databaseName)
			: this(SqlConnectionBuilder.Instance(serverName, databaseName))
		{}

		public SqlConnectionFactory(string serverName, string databaseName, string userId, string password)
			: this(SqlConnectionBuilder.Instance(serverName, databaseName, userId, password))
		{ }

		public string ConnectionString { get; private set; }

		public IDbConnection Create()
		{
			return new System.Data.SqlClient.SqlConnection(ConnectionString);
		}

		public IDbConnection CreateAndOpen()
		{
			var con = Create();
			con.Open();
			return con;
		}

		protected class SqlConnectionBuilder : ConnectionStringBuilder
		{
			private SqlConnectionBuilder()
			{
				SetConnectionTimeout(0);
			}

			public new SqlConnectionBuilder AddProperty(string name, string value)
			{
				return (SqlConnectionBuilder)base.AddProperty(name, value);
			}

			public new SqlConnectionBuilder AddProperty(string name, object value)
			{
				return (SqlConnectionBuilder)base.AddProperty(name, value);
			}

			public SqlConnectionBuilder SetServer(string serverName)
			{
				return AddProperty("Data Source", serverName);
			}

			public SqlConnectionBuilder SetDatabase(string databaseName)
			{
				return AddProperty("Initial Catalog", databaseName);
			}

			private SqlConnectionBuilder SetUserId(string userId)
			{
				return AddProperty("User Id", userId);
			}

			private SqlConnectionBuilder SetPassword(string password)
			{
				return AddProperty("Password", password);
			}

			private SqlConnectionBuilder SetIntegratedSecurity()
			{
				return AddProperty("Integrated Security", "SSPI");
			}

			public SqlConnectionBuilder SetConnectionTimeout(int seconds)
			{
				return AddProperty("Connection Timeout", seconds);
			}

			public static SqlConnectionBuilder Instance(string serverName, string databaseName)
			{
				return new SqlConnectionBuilder().SetIntegratedSecurity().SetServer(serverName).SetDatabase(databaseName);
			}

			public static SqlConnectionBuilder Instance(string serverName, string databaseName, string userId, string password)
			{
				return new SqlConnectionBuilder().SetServer(serverName).SetDatabase(databaseName).SetPassword(password).SetUserId(userId);
			}
		}
	}
}
