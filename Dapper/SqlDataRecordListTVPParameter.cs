using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Dapper
{
    /// <summary>
    /// Used to pass a IEnumerable&lt;SqlDataRecord&gt; as a SqlDataRecordListTVPParameter
    /// </summary>
    internal sealed class SqlDataRecordListTVPParameter<T> : SqlMapper.ICustomQueryParameter
        where T : IDataRecord
    {
        private readonly IEnumerable<T> data;
        private readonly string typeName;
        /// <summary>
        /// Create a new instance of <see cref="SqlDataRecordListTVPParameter&lt;T&gt;"/>.
        /// </summary>
        /// <param name="data">The data records to convert into TVPs.</param>
        /// <param name="typeName">The parameter type name.</param>
        public SqlDataRecordListTVPParameter(IEnumerable<T> data, string typeName)
        {
            this.data = data;
            this.typeName = typeName;
        }

        void SqlMapper.ICustomQueryParameter.AddParameter(IDbCommand command, string name)
        {
            var param = command.CreateParameter();
            param.ParameterName = name;
            Set(param, data, typeName);
            command.Parameters.Add(param);
        }

        internal static void Set(IDbDataParameter parameter, IEnumerable<T> data, string typeName)
        {
            parameter.Value = data != null && data.Any() ? data : null;
            StructuredHelper.ConfigureTVP(parameter, typeName);
        }
    }
    static class StructuredHelper
    {
        private static readonly Hashtable s_udt = new Hashtable(), s_tvp = new Hashtable();

        private static Action<IDbDataParameter, string> GetUDT(Type type)
            => (Action<IDbDataParameter, string>)s_udt[type] ?? SlowGetHelper(type, s_udt, "UdtTypeName", 29); // 29 = SqlDbType.Udt (avoiding ref)
        private static Action<IDbDataParameter, string> GetTVP(Type type)
            => (Action<IDbDataParameter, string>)s_tvp[type] ?? SlowGetHelper(type, s_tvp, "TypeName", 30); // 30 = SqlDbType.Structured (avoiding ref)

        static Action<IDbDataParameter, string> SlowGetHelper(Type type, Hashtable hashtable, string nameProperty, int sqlDbType)
        {
            lock (hashtable)
            {
                var helper = (Action<IDbDataParameter, string>)hashtable[type];
                if (helper == null)
                {
                    helper = CreateFor(type, nameProperty, sqlDbType);
                    hashtable.Add(type, helper);
                }
                return helper;
            }
        }

        static Action<IDbDataParameter, string> CreateFor(Type type, string nameProperty, int sqlDbType)
        {
            var name = type.GetProperty(nameProperty, BindingFlags.Public | BindingFlags.Instance);
            if (name == null || !name.CanWrite)
            {
                return (p, n) => { };
            }

            var dbType = type.GetProperty("SqlDbType", BindingFlags.Public | BindingFlags.Instance);
            if (dbType != null && !dbType.CanWrite) dbType = null;

            var dm = new DynamicMethod(nameof(CreateFor) + "_" + type.Name, null,
                new[] { typeof(IDbDataParameter), typeof(string) }, true);
            var il = dm.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Castclass, type);
            il.Emit(OpCodes.Ldarg_1);
            il.EmitCall(OpCodes.Callvirt, name.GetSetMethod(), null);

            if (dbType != null)
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Castclass, type);
                il.Emit(OpCodes.Ldc_I4, sqlDbType);
                il.EmitCall(OpCodes.Callvirt, dbType.GetSetMethod(), null);
            }

            il.Emit(OpCodes.Ret);
            return (Action<IDbDataParameter, string>)dm.CreateDelegate(typeof(Action<IDbDataParameter, string>));

        }

        // this needs to be done per-provider; "dynamic" doesn't work well on all runtimes, although that
        // would be a fair option otherwise
        internal static void ConfigureUDT(IDbDataParameter parameter, string typeName)
            => GetUDT(parameter.GetType())(parameter, typeName);
        internal static void ConfigureTVP(IDbDataParameter parameter, string typeName)
            => GetTVP(parameter.GetType())(parameter, typeName);
    }
}
