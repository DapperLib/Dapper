using System;
using System.Data;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

#if DNXCORE50
using IDbDataParameter = global::System.Data.Common.DbParameter;
using IDataParameter = global::System.Data.Common.DbParameter;
using IDbTransaction = global::System.Data.Common.DbTransaction;
using IDbConnection = global::System.Data.Common.DbConnection;
using IDbCommand = global::System.Data.Common.DbCommand;
using IDataReader = global::System.Data.Common.DbDataReader;
using IDataRecord = global::System.Data.Common.DbDataReader;
using IDataParameterCollection = global::System.Data.Common.DbParameterCollection;
using DataException = global::System.InvalidOperationException;
using ApplicationException = global::System.InvalidOperationException;
#endif

namespace Dapper
{    /// <summary>
     /// Represents the key aspects of a sql operation
     /// </summary>
    public struct CommandDefinition
    {
        internal static CommandDefinition ForCallback(object parameters)
        {
            if (parameters is DynamicParameters)
            {
                return new CommandDefinition(parameters);
            }
            else
            {
                return default(CommandDefinition);
            }
        }
        private readonly string commandText;
        private readonly object parameters;
        private readonly IDbTransaction transaction;
        private readonly int? commandTimeout;
        private readonly CommandType? commandType;
        private readonly CommandFlags flags;


        internal void OnCompleted()
        {
            if (parameters is SqlMapper.IParameterCallbacks)
            {
                ((SqlMapper.IParameterCallbacks)parameters).OnCompleted();
            }
        }
        /// <summary>
        /// The command (sql or a stored-procedure name) to execute
        /// </summary>
        public string CommandText { get { return commandText; } }
        /// <summary>
        /// The parameters associated with the command
        /// </summary>
        public object Parameters { get { return parameters; } }
        /// <summary>
        /// The active transaction for the command
        /// </summary>
        public IDbTransaction Transaction { get { return transaction; } }
        /// <summary>
        /// The effective timeout for the command
        /// </summary>
        public int? CommandTimeout { get { return commandTimeout; } }
        /// <summary>
        /// The type of command that the command-text represents
        /// </summary>
        public CommandType? CommandType { get { return commandType; } }

        /// <summary>
        /// Should data be buffered before returning?
        /// </summary>
        public bool Buffered { get { return (flags & CommandFlags.Buffered) != 0; } }

        /// <summary>
        /// Should the plan for this query be cached?
        /// </summary>
        internal bool AddToCache { get { return (flags & CommandFlags.NoCache) == 0; } }

        /// <summary>
        /// Additional state flags against this command
        /// </summary>
        public CommandFlags Flags { get { return flags; } }

        /// <summary>
        /// Can async queries be pipelined?
        /// </summary>
        public bool Pipelined { get { return (flags & CommandFlags.Pipelined) != 0; } }

        /// <summary>
        /// Initialize the command definition
        /// </summary>
        public CommandDefinition(string commandText, object parameters = null, IDbTransaction transaction = null, int? commandTimeout = null,
            CommandType? commandType = null, CommandFlags flags = CommandFlags.Buffered
#if ASYNC
            , CancellationToken cancellationToken = default(CancellationToken)
#endif
            )
        {
            this.commandText = commandText;
            this.parameters = parameters;
            this.transaction = transaction;
            this.commandTimeout = commandTimeout;
            this.commandType = commandType;
            this.flags = flags;
#if ASYNC
            this.cancellationToken = cancellationToken;
#endif
        }

        private CommandDefinition(object parameters) : this()
        {
            this.parameters = parameters;
        }

#if ASYNC
        private readonly CancellationToken cancellationToken;
        /// <summary>
        /// For asynchronous operations, the cancellation-token
        /// </summary>
        public CancellationToken CancellationToken { get { return cancellationToken; } }
#endif

        internal IDbCommand SetupCommand(IDbConnection cnn, Action<IDbCommand, object> paramReader)
        {
            var cmd = cnn.CreateCommand();
            var init = GetInit(cmd.GetType());
            if (init != null) init(cmd);
            if (transaction != null)
                cmd.Transaction = transaction;
            cmd.CommandText = commandText;
            if (commandTimeout.HasValue)
            {
                cmd.CommandTimeout = commandTimeout.Value;
            }
            else if (SqlMapper.Settings.CommandTimeout.HasValue)
            {
                cmd.CommandTimeout = SqlMapper.Settings.CommandTimeout.Value;
            }
            if (commandType.HasValue)
                cmd.CommandType = commandType.Value;
            if (paramReader != null)
            {
                paramReader(cmd, parameters);
            }
            return cmd;
        }

        static SqlMapper.Link<Type, Action<IDbCommand>> commandInitCache;
        static Action<IDbCommand> GetInit(Type commandType)
        {
            if (commandType == null) return null; // GIGO
            Action<IDbCommand> action;
            if (SqlMapper.Link<Type, Action<IDbCommand>>.TryGet(commandInitCache, commandType, out action))
            {
                return action;
            }
            var bindByName = GetBasicPropertySetter(commandType, "BindByName", typeof(bool));
            var initialLongFetchSize = GetBasicPropertySetter(commandType, "InitialLONGFetchSize", typeof(int));

            action = null;
            if (bindByName != null || initialLongFetchSize != null)
            {
                var method = new DynamicMethod(commandType.Name + "_init", null, new Type[] { typeof(IDbCommand) });
                var il = method.GetILGenerator();

                if (bindByName != null)
                {
                    // .BindByName = true
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Castclass, commandType);
                    il.Emit(OpCodes.Ldc_I4_1);
                    il.EmitCall(OpCodes.Callvirt, bindByName, null);
                }
                if (initialLongFetchSize != null)
                {
                    // .InitialLONGFetchSize = -1
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Castclass, commandType);
                    il.Emit(OpCodes.Ldc_I4_M1);
                    il.EmitCall(OpCodes.Callvirt, initialLongFetchSize, null);
                }
                il.Emit(OpCodes.Ret);
                action = (Action<IDbCommand>)method.CreateDelegate(typeof(Action<IDbCommand>));
            }
            // cache it            
            SqlMapper.Link<Type, Action<IDbCommand>>.TryAdd(ref commandInitCache, commandType, ref action);
            return action;
        }
        static MethodInfo GetBasicPropertySetter(Type declaringType, string name, Type expectedType)
        {
            var prop = declaringType.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            ParameterInfo[] indexers;
            if (prop != null && prop.CanWrite && prop.PropertyType == expectedType
                && ((indexers = prop.GetIndexParameters()) == null || indexers.Length == 0))
            {
                return prop.GetSetMethod();
            }
            return null;
        }
    }

}
