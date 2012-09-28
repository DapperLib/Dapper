using System;
using System.Collections.Concurrent;
using System.Data.Common;

namespace Dapper.Data.Service
{
	public interface IDbServiceProvider
	{
		T For<T>() where T : IDbService;
	}

	/// <summary>
	/// Work in progress
	/// I have used old style dataset and templates [DatasetTransformer.tt] to create Poco classes
	/// I would like to update it to read queries from it and build them in to services where
	/// each table in dataset will reperesent a service and queries defined in it will represent
	/// actions that can be performed by the service
	/// Heavy usage of iterfaces enable me to use injection and substitution.
	/// </summary>
	public abstract class DbServiceProvider : DbContext, IDbServiceProvider
	{
		readonly ConcurrentDictionary<Type, IDbService> services
			= new ConcurrentDictionary<Type, IDbService>();

		protected DbServiceProvider(IDbConnectionFactory connectionFactory) : base(connectionFactory)
		{ }

		/// <summary>
		/// registeres new service
		/// </summary>
		protected void RegisterService<T>(T service) where T : IDbService
		{
			services.TryAdd(service.GetType(), service);
		}

		/// <summary>
		/// use this to retreave the service
		/// </summary>
		public T For<T>() where T : IDbService
		{
			return (T)services[typeof(T)];
		}
	}
}