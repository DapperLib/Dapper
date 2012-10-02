using System.Configuration;

namespace System.Data.Common
{
	public interface IDbConnectionFactory
	{
		IDbConnection Create();
		IDbConnection CreateAndOpen();
	}

	/// <summary>
	/// Default implementation of ConnectionFactory
	/// used by DbContext
	/// </summary>
	public class DbConnectionFactory : IDbConnectionFactory
	{
		readonly DbProviderFactory _factory;
		readonly string _conectionString;

		/// <summary>
		/// connectionStringName: 
		///		connection string name defined under connections section of the config file.
		/// The connection element must have propper DbProviderFactory defined
		/// </summary>
		public DbConnectionFactory(string connectionName)
			: this(ConfigurationManager.ConnectionStrings.GetByName(connectionName))
		{}

		protected DbConnectionFactory(ConnectionStringSettings connectionSetting)
		{
			_factory = connectionSetting.CreatDbProviderFactory();
			_conectionString = connectionSetting.ConnectionString;
		}

		public virtual IDbConnection Create()
		{
			return _factory.CreateConnection(_conectionString);
		}

		public virtual IDbConnection CreateAndOpen()
		{
			var con = Create();
			con.Open();
			return con;
		}
	}

	static class DbConnectionFactoryHelpers
	{
		/// <summary>
		/// Creates connection
		/// </summary>
		internal static IDbConnection CreateConnection(this DbProviderFactory sender, string connectionString)
		{
			var connection = sender.CreateConnection();
			connection.ConnectionString = connectionString;
			return connection;
		}

		/// <summary>
		/// Creates and opens connection
		/// </summary>
		internal static IDbConnection CreateOpenedConnection(this DbProviderFactory sender, string connectionString)
		{
			var connection = sender.CreateConnection();
			connection.ConnectionString = connectionString;
			connection.Open();
			return connection;
		}

		/// <summary>
		/// Get connection by name form Connection Configuration Collection
		/// </summary>
		public static ConnectionStringSettings GetByName(this ConnectionStringSettingsCollection sender, string connectionStringName)
		{
			var connectionSetting = sender[connectionStringName];
			if (connectionSetting == null)
			{ throw new InvalidOperationException(string.Format("Can't find a connection string with the name '{0}'", connectionStringName)); }
			return connectionSetting;
		}

		/// <summary>
		/// Creates an instance of DbProviderFactory defined in config file in connection setting
		/// </summary>
		public static DbProviderFactory CreatDbProviderFactory(this ConnectionStringSettings sender)
		{
			try
			{
				return DbProviderFactories.GetFactory(sender.ProviderName);
			}
			catch (Exception ex)
			{
				throw new ApplicationException(sender.ProviderName, ex);
			}
		}
	}
}