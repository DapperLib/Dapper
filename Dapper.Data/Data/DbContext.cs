using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Data;

namespace Dapper.Data
{
	/// <summary>
	/// Default behavior exposed by DbContext helps with injection
	/// </summary>
	public interface IDbContext
	{
		void Batch(Action<ISession> action);
		TResult Batch<TResult>(Func<ISession, TResult> func);

		int Execute(
			string sql,
			object param = null,
			CommandType? commandType = null
		);

		IEnumerable<T> Query<T>(
			string sql,
			object param = null,
			CommandType? commandType = null
		);

		IEnumerable<dynamic> Query(
			string sql,
			object param = null,
			CommandType? commandType = null
		);
	}

	/// <summary>
	/// Interface to help with transaction managment
	/// </summary>
	public interface ISession
	{
		void BeginTransaction();
        void BeginTransaction(IsolationLevel il);
        void CommitTransaction();
		void RollbackTransaction();

		int Execute(
			string sql,
			object param = null,
			CommandType? commandType = null
		);

		IEnumerable<T> Query<T>(
			string sql,
			object param = null,
			CommandType? commandType = null
		);

		IEnumerable<dynamic> Query(
			string sql,
			object param = null,
			CommandType? commandType = null
		);
	}

	/// <summary>
	/// Light weight DbContext implementation based on dapper
	/// Use it to create your own DbContext
	/// It will help manage connection life time and transactions
	/// </summary>
	public abstract class DbContext : IDbContext
	{

		protected DbContext(IDbConnectionFactory connectionFactory)
		{
			ConnectionFactory = connectionFactory;
		}

		public virtual IDbConnectionFactory ConnectionFactory
		{
			get;
			private set;
		}

		/// <summary>
		/// Enables execution of multiple statements and helps with
		/// transaction management
		/// </summary>
		public virtual void Batch(Action<ISession> action)
		{
			using (var con = ConnectionFactory.CreateAndOpen())
			{
				try
				{
					action(new Session(con));
				}
				finally
				{
					con.Close();
				}
			}
		}

		/// <summary>
		/// Enables execution of multiple statements and helps with
		/// transaction management
		/// </summary>
		public virtual TResult Batch<TResult>(Func<ISession, TResult> func)
		{
			using (var con = ConnectionFactory.CreateAndOpen())
			{
				try
				{
					return func(new Session(con));
				}
				finally
				{
					con.Close();
				}
			}
		}

		class Session : ISession
		{
            readonly IDbConnection _connection;
            IDbTransaction _transaction;

			public Session(IDbConnection connection)
			{
				_connection = connection;
				_transaction = null;
			}


			public void BeginTransaction()
			{
				if (_transaction == null)
				{ _transaction = _connection.BeginTransaction(); }
			}

            public void BeginTransaction(IsolationLevel il)
            {
                if (_transaction == null)
                { _transaction = _connection.BeginTransaction(il); }
            }

			public void CommitTransaction()
			{
				if (_transaction != null)
				{
					_transaction.Commit();
				}
				_transaction = null;
			}

			public void RollbackTransaction()
			{
				if (_transaction != null)
				{
					_transaction.Rollback();
				}
				_transaction = null;
			}

			public int Execute(string sql, object param = null, CommandType? commandType = null)
			{
				return _connection.Execute(sql, param, commandType, _transaction);
			}

			public IEnumerable<T> Query<T>(string sql, object param = null, CommandType? commandType = null)
			{
				return _connection.Query<T>(sql, param, commandType, _transaction);
			}


			public IEnumerable<dynamic> Query(string sql, object param = null, CommandType? commandType = null)
			{
				return Query<dynamic>(sql, param, commandType);
			}
		}

		public int Execute(string sql, object param = null, CommandType? commandType = null)
		{
			return Batch<int>(s => s.Execute(sql, param, commandType));
		}

		public IEnumerable<T> Query<T>(string sql, object param = null, CommandType? commandType = null)
		{
			return Batch(s => s.Query<T>(sql, param, commandType));
		}

		public IEnumerable<dynamic> Query(string sql, object param = null, CommandType? commandType = null)
		{
			return Batch(s => s.Query(sql, param, commandType));
		}
	}
}
