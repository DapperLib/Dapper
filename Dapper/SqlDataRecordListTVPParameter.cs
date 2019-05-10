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
#if !NETSTANDARD1_3
        where T : IDataRecord
#endif
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
            StructuredHelper.ConfigureStructured(parameter, typeName);
        }
    }
    static class StructuredHelper
    {
        private static readonly Hashtable s_perTypeHelpers = new Hashtable();
        private static Action<IDbDataParameter, string> GetHelper(Type type)
            => (Action<IDbDataParameter, string>)s_perTypeHelpers[type] ?? SlowGetHelper(type);
        static Action<IDbDataParameter, string> SlowGetHelper(Type type)
        {
            lock(s_perTypeHelpers)
            {
                var helper = (Action<IDbDataParameter, string>)s_perTypeHelpers[type];
                if (helper == null)
                {
                    helper = CreateFor(type);
                    s_perTypeHelpers.Add(type, helper);
                }
                return helper;
            }
        }
        static Action<IDbDataParameter, string> CreateFor(Type type)
        {
            var name = type.GetProperty("TypeName", BindingFlags.Public | BindingFlags.Instance);
            if (!name.CanWrite) name = null;
            if (name == null) return (p, n) => { };

            var dbType = type.GetProperty("SqlDbType", BindingFlags.Public | BindingFlags.Instance);
            if (!dbType.CanWrite) dbType = null;

            var dm = new DynamicMethod(nameof(CreateFor) + "_" + type.Name, null,
                new[] { typeof(IDbDataParameter), typeof(string) }, true);
            var il = dm.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Castclass, type);

            if (dbType != null) il.Emit(OpCodes.Dup);

            il.Emit(OpCodes.Ldarg_1);
            il.EmitCall(OpCodes.Callvirt, name.GetSetMethod(), null);

            if (dbType != null)
            {
                il.Emit(OpCodes.Ldind_I4, (int)SqlDbType.Structured);
                il.EmitCall(OpCodes.Callvirt, dbType.GetSetMethod(), null);
            }

            il.Emit(OpCodes.Ret);
            return (Action<IDbDataParameter, string>)dm.CreateDelegate(typeof(Action<IDbDataParameter, string>));

        }

        // this needs to be done per-provider; "dynamic" doesn't work well on all runtimes, although that
        // would be a fair option otherwise
        internal static void ConfigureStructured(IDbDataParameter parameter, string typeName)
            => GetHelper(parameter.GetType())(parameter, typeName);
    }
}
