using System;
using System.Data;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace Dapper
{
    /// <summary>
    /// Represents the key aspects of a sql operation
    /// </summary>
    public readonly struct CommandDefinition
    {
        internal static CommandDefinition ForCallback(object? parameters)
        {
            if (parameters is DynamicParameters)
            {
                return new CommandDefinition(parameters);
            }
            else
            {
                return default;
            }
        }

        internal void OnCompleted()
        {
            (Parameters as SqlMapper.IParameterCallbacks)?.OnCompleted();
        }

        /// <summary>
        /// The command (sql or a stored-procedure name) to execute
        /// </summary>
        public string CommandText { get; }

        /// <summary>
        /// The parameters associated with the command
        /// </summary>
        public object? Parameters { get; }

        /// <summary>
        /// The active transaction for the command
        /// </summary>
        public IDbTransaction? Transaction { get; }

        /// <summary>
        /// The effective timeout for the command
        /// </summary>
        public int? CommandTimeout { get; }

        internal readonly CommandType CommandTypeDirect;

        /// <summary>
        /// The type of command that the command-text represents
        /// </summary>
#if DEBUG // prevent use in our own code
        [Obsolete("Prefer " + nameof(CommandTypeDirect), true)]
#endif
        public CommandType? CommandType => CommandTypeDirect;

        /// <summary>
        /// Should data be buffered before returning?
        /// </summary>
        public bool Buffered => (Flags & CommandFlags.Buffered) != 0;

        /// <summary>
        /// Should the plan for this query be cached?
        /// </summary>
        internal bool AddToCache => (Flags & CommandFlags.NoCache) == 0;

        /// <summary>
        /// Additional state flags against this command
        /// </summary>
        public CommandFlags Flags { get; }

        /// <summary>
        /// Can async queries be pipelined?
        /// </summary>
        public bool Pipelined => (Flags & CommandFlags.Pipelined) != 0;

        /// <summary>
        /// Initialize the command definition
        /// </summary>
        /// <param name="commandText">The text for this command.</param>
        /// <param name="parameters">The parameters for this command.</param>
        /// <param name="transaction">The transaction for this command to participate in.</param>
        /// <param name="commandTimeout">The timeout (in seconds) for this command.</param>
        /// <param name="commandType">The <see cref="CommandType"/> for this command.</param>
        /// <param name="flags">The behavior flags for this command.</param>
        /// <param name="cancellationToken">The cancellation token for this command.</param>
        public CommandDefinition(string commandText, object? parameters = null, IDbTransaction? transaction = null, int? commandTimeout = null,
                                 CommandType? commandType = null, CommandFlags flags = CommandFlags.Buffered
                                 , CancellationToken cancellationToken = default
            )
        {
            CommandText = commandText;
            Parameters = parameters;
            Transaction = transaction;
            CommandTimeout = commandTimeout;
            CommandTypeDirect = commandType ?? InferCommandType(commandText);
            Flags = flags;
            CancellationToken = cancellationToken;
        }

        internal static CommandType InferCommandType(string sql)
        {
            // if the sql contains any whitespace character (space/tab/cr/lf/etc - via unicode),
            // has operators, comments, semi-colon, or a known exception: interpret as ad-hoc;
            // otherwise, simple names like "SomeName" should be treated as a stored-proc
            // (note TableDirect would need to be specified explicitly, but in reality providers don't usually support TableDirect anyway)

            if (sql is null || CompiledRegex.WhitespaceOrReserved.IsMatch(sql)) return System.Data.CommandType.Text;
            return System.Data.CommandType.StoredProcedure;
        }

        private CommandDefinition(object? parameters) : this()
        {
            Parameters = parameters;
            CommandText = "";
        }

        /// <summary>
        /// For asynchronous operations, the cancellation-token
        /// </summary>
        public CancellationToken CancellationToken { get; }

        internal IDbCommand SetupCommand(IDbConnection cnn, Action<IDbCommand, object?>? paramReader)
        {
            var cmd = cnn.CreateCommand();
            var init = GetInit(cmd.GetType());
            init?.Invoke(cmd);
            if (Transaction is not null)
                cmd.Transaction = Transaction;
            cmd.CommandText = CommandText;
            if (CommandTimeout.HasValue)
            {
                cmd.CommandTimeout = CommandTimeout.Value;
            }
            else if (SqlMapper.Settings.CommandTimeout.HasValue)
            {
                cmd.CommandTimeout = SqlMapper.Settings.CommandTimeout.Value;
            }
            cmd.CommandType = CommandTypeDirect;
            paramReader?.Invoke(cmd, Parameters);
            return cmd;
        }

        private static SqlMapper.Link<Type, Action<IDbCommand>>? commandInitCache;

        internal static void ResetCommandInitCache()
            => SqlMapper.Link<Type, Action<IDbCommand>>.Clear(ref commandInitCache);

        private static Action<IDbCommand>? GetInit(Type commandType)
        {
            if (commandType is null)
                return null; // GIGO
            if (SqlMapper.Link<Type, Action<IDbCommand>>.TryGet(commandInitCache, commandType, out Action<IDbCommand>? action))
            {
                return action;
            }
            var bindByName = GetBasicPropertySetter(commandType, "BindByName", typeof(bool));
            var initialLongFetchSize = GetBasicPropertySetter(commandType, "InitialLONGFetchSize", typeof(int));
            var fetchSize = GetBasicPropertySetter(commandType, "FetchSize", typeof(long));

            action = null;
            if (bindByName is not null || initialLongFetchSize is not null || fetchSize is not null)
            {
                var method = new DynamicMethod(commandType.Name + "_init", null, new Type[] { typeof(IDbCommand) });
                var il = method.GetILGenerator();

                if (bindByName is not null)
                {
                    // .BindByName = true
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Castclass, commandType);
                    il.Emit(OpCodes.Ldc_I4_1);
                    il.EmitCall(OpCodes.Callvirt, bindByName, null);
                }
                if (initialLongFetchSize is not null)
                {
                    // .InitialLONGFetchSize = -1
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Castclass, commandType);
                    il.Emit(OpCodes.Ldc_I4_M1);
                    il.EmitCall(OpCodes.Callvirt, initialLongFetchSize, null);
                }
                if (fetchSize is not null)
                {
                    var snapshot = SqlMapper.Settings.FetchSize;
                    if (snapshot >= 0)
                    {
                        // .FetchSize = {withValue}
                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Castclass, commandType);
                        il.Emit(OpCodes.Ldc_I8, snapshot); // bake it as a constant
                        il.EmitCall(OpCodes.Callvirt, fetchSize, null);
                    }
                }
                il.Emit(OpCodes.Ret);
                action = (Action<IDbCommand>)method.CreateDelegate(typeof(Action<IDbCommand>));
            }
            // cache it
            SqlMapper.Link<Type, Action<IDbCommand>>.TryAdd(ref commandInitCache, commandType, ref action!);
            return action;
        }

        private static MethodInfo? GetBasicPropertySetter(Type declaringType, string name, Type expectedType)
        {
            var prop = declaringType.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (prop?.CanWrite == true && prop.PropertyType == expectedType && prop.GetIndexParameters().Length == 0)
            {
                return prop.GetSetMethod();
            }
            return null;
        }
    }
}
