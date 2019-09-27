using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace Dapper
{
    /// <summary>
    /// A bag of parameters that can be passed to the Dapper Query and Execute methods
    /// </summary>
    public partial class DynamicParameters : SqlMapper.IDynamicParameters, SqlMapper.IParameterLookup, SqlMapper.IParameterCallbacks
    {
        internal const DbType EnumerableMultiParameter = (DbType)(-1);
        private static readonly Dictionary<SqlMapper.Identity, Action<IDbCommand, object>> paramReaderCache = new Dictionary<SqlMapper.Identity, Action<IDbCommand, object>>();
        private readonly Dictionary<string, ParamInfo> parameters = new Dictionary<string, ParamInfo>();
        private List<object> templates;

        object SqlMapper.IParameterLookup.this[string name] =>
            parameters.TryGetValue(name, out ParamInfo param) ? param.Value : null;

        /// <summary>
        /// construct a dynamic parameter bag
        /// </summary>
        public DynamicParameters()
        {
            RemoveUnused = true;
        }

        /// <summary>
        /// construct a dynamic parameter bag
        /// </summary>
        /// <param name="template">can be an anonymous type or a DynamicParameters bag</param>
        public DynamicParameters(object template)
        {
            RemoveUnused = true;
            AddDynamicParams(template);
        }

        /// <summary>
        /// Append a whole object full of params to the dynamic
        /// EG: AddDynamicParams(new {A = 1, B = 2}) // will add property A and B to the dynamic
        /// </summary>
        /// <param name="param"></param>
        public void AddDynamicParams(object param)
        {
            var obj = param;
            if (obj != null)
            {
                var subDynamic = obj as DynamicParameters;
                if (subDynamic == null)
                {
                    var dictionary = obj as IEnumerable<KeyValuePair<string, object>>;
                    if (dictionary == null)
                    {
                        templates = templates ?? new List<object>();
                        templates.Add(obj);
                    }
                    else
                    {
                        foreach (var kvp in dictionary)
                        {
                            Add(kvp.Key, kvp.Value, null, null, null);
                        }
                    }
                }
                else
                {
                    if (subDynamic.parameters != null)
                    {
                        foreach (var kvp in subDynamic.parameters)
                        {
                            parameters.Add(kvp.Key, kvp.Value);
                        }
                    }

                    if (subDynamic.templates != null)
                    {
                        templates = templates ?? new List<object>();
                        foreach (var t in subDynamic.templates)
                        {
                            templates.Add(t);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Add a parameter to this dynamic parameter list.
        /// </summary>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="value">The value of the parameter.</param>
        /// <param name="dbType">The type of the parameter.</param>
        /// <param name="direction">The in or out direction of the parameter.</param>
        /// <param name="size">The size of the parameter.</param>
        public void Add(string name, object value, DbType? dbType, ParameterDirection? direction, int? size)
        {
            parameters[Clean(name)] = new ParamInfo
            {
                Name = name,
                Value = value,
                ParameterDirection = direction ?? ParameterDirection.Input,
                DbType = dbType,
                Size = size
            };
        }

        /// <summary>
        /// Add a parameter to this dynamic parameter list.
        /// </summary>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="value">The value of the parameter.</param>
        /// <param name="dbType">The type of the parameter.</param>
        /// <param name="direction">The in or out direction of the parameter.</param>
        /// <param name="size">The size of the parameter.</param>
        /// <param name="precision">The precision of the parameter.</param>
        /// <param name="scale">The scale of the parameter.</param>
        public void Add(string name, object value = null, DbType? dbType = null, ParameterDirection? direction = null, int? size = null, byte? precision = null, byte? scale = null)
        {
            parameters[Clean(name)] = new ParamInfo
            {
                Name = name,
                Value = value,
                ParameterDirection = direction ?? ParameterDirection.Input,
                DbType = dbType,
                Size = size,
                Precision = precision,
                Scale = scale
            };
        }

        private static string Clean(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                switch (name[0])
                {
                    case '@':
                    case ':':
                    case '?':
                        return name.Substring(1);
                }
            }
            return name;
        }

        void SqlMapper.IDynamicParameters.AddParameters(IDbCommand command, SqlMapper.Identity identity)
        {
            AddParameters(command, identity);
        }

        /// <summary>
        /// If true, the command-text is inspected and only values that are clearly used are included on the connection
        /// </summary>
        public bool RemoveUnused { get; set; }

        /// <summary>
        /// Add all the parameters needed to the command just before it executes
        /// </summary>
        /// <param name="command">The raw command prior to execution</param>
        /// <param name="identity">Information about the query</param>
        protected void AddParameters(IDbCommand command, SqlMapper.Identity identity)
        {
            var literals = SqlMapper.GetLiteralTokens(identity.sql);

            if (templates != null)
            {
                foreach (var template in templates)
                {
                    var newIdent = identity.ForDynamicParameters(template.GetType());
                    Action<IDbCommand, object> appender;

                    lock (paramReaderCache)
                    {
                        if (!paramReaderCache.TryGetValue(newIdent, out appender))
                        {
                            appender = SqlMapper.CreateParamInfoGenerator(newIdent, true, RemoveUnused, literals);
                            paramReaderCache[newIdent] = appender;
                        }
                    }

                    appender(command, template);
                }

                // The parameters were added to the command, but not the
                // DynamicParameters until now.
                foreach (IDbDataParameter param in command.Parameters)
                {
                    // If someone makes a DynamicParameters with a template,
                    // then explicitly adds a parameter of a matching name,
                    // it will already exist in 'parameters'.
                    if (!parameters.ContainsKey(param.ParameterName))
                    {
                        parameters.Add(param.ParameterName, new ParamInfo
                        {
                            AttachedParam = param,
                            CameFromTemplate = true,
                            DbType = param.DbType,
                            Name = param.ParameterName,
                            ParameterDirection = param.Direction,
                            Size = param.Size,
                            Value = param.Value
                        });
                    }
                }

                // Now that the parameters are added to the command, let's place our output callbacks
                var tmp = outputCallbacks;
                if (tmp != null)
                {
                    foreach (var generator in tmp)
                    {
                        generator();
                    }
                }
            }

            foreach (var param in parameters.Values)
            {
                if (param.CameFromTemplate) continue;

                var dbType = param.DbType;
                var val = param.Value;
                string name = Clean(param.Name);
                var isCustomQueryParameter = val is SqlMapper.ICustomQueryParameter;

                SqlMapper.ITypeHandler handler = null;
                if (dbType == null && val != null && !isCustomQueryParameter)
                {
#pragma warning disable 618
                    dbType = SqlMapper.LookupDbType(val.GetType(), name, true, out handler);
#pragma warning disable 618
                }
                if (isCustomQueryParameter)
                {
                    ((SqlMapper.ICustomQueryParameter)val).AddParameter(command, name);
                }
                else if (dbType == EnumerableMultiParameter)
                {
#pragma warning disable 612, 618
                    SqlMapper.PackListParameters(command, name, val);
#pragma warning restore 612, 618
                }
                else
                {
                    bool add = !command.Parameters.Contains(name);
                    IDbDataParameter p;
                    if (add)
                    {
                        p = command.CreateParameter();
                        p.ParameterName = name;
                    }
                    else
                    {
                        p = (IDbDataParameter)command.Parameters[name];
                    }

                    p.Direction = param.ParameterDirection;
                    if (handler == null)
                    {
#pragma warning disable 0618
                        p.Value = SqlMapper.SanitizeParameterValue(val);
#pragma warning restore 0618
                        if (dbType != null && p.DbType != dbType)
                        {
                            p.DbType = dbType.Value;
                        }
                        var s = val as string;
                        if (s?.Length <= DbString.DefaultLength)
                        {
                            p.Size = DbString.DefaultLength;
                        }
                        if (param.Size != null) p.Size = param.Size.Value;
                        if (param.Precision != null) p.Precision = param.Precision.Value;
                        if (param.Scale != null) p.Scale = param.Scale.Value;
                    }
                    else
                    {
                        if (dbType != null) p.DbType = dbType.Value;
                        if (param.Size != null) p.Size = param.Size.Value;
                        if (param.Precision != null) p.Precision = param.Precision.Value;
                        if (param.Scale != null) p.Scale = param.Scale.Value;
                        handler.SetValue(p, val ?? DBNull.Value);
                    }

                    if (add)
                    {
                        command.Parameters.Add(p);
                    }
                    param.AttachedParam = p;
                }
            }

            // note: most non-priveleged implementations would use: this.ReplaceLiterals(command);
            if (literals.Count != 0) SqlMapper.ReplaceLiterals(this, command, literals);
        }

        /// <summary>
        /// All the names of the param in the bag, use Get to yank them out
        /// </summary>
        public IEnumerable<string> ParameterNames => parameters.Select(p => p.Key);

        /// <summary>
        /// Get the value of a parameter
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns>The value, note DBNull.Value is not returned, instead the value is returned as null</returns>
        public T Get<T>(string name)
        {
            var paramInfo = parameters[Clean(name)];
            var attachedParam = paramInfo.AttachedParam;
            object val = attachedParam == null ? paramInfo.Value : attachedParam.Value;
            if (val == DBNull.Value)
            {
                if (default(T) != null)
                {
                    throw new ApplicationException("Attempting to cast a DBNull to a non nullable type! Note that out/return parameters will not have updated values until the data stream completes (after the 'foreach' for Query(..., buffered: false), or after the GridReader has been disposed for QueryMultiple)");
                }
                return default(T);
            }
            return (T)val;
        }

        /// <summary>
        /// Allows you to automatically populate a target property/field from output parameters. It actually
        /// creates an InputOutput parameter, so you can still pass data in.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="target">The object whose property/field you wish to populate.</param>
        /// <param name="expression">A MemberExpression targeting a property/field of the target (or descendant thereof.)</param>
        /// <param name="dbType"></param>
        /// <param name="size">The size to set on the parameter. Defaults to 0, or DbString.DefaultLength in case of strings.</param>
        /// <returns>The DynamicParameters instance</returns>
        public DynamicParameters Output<T>(T target, Expression<Func<T, object>> expression, DbType? dbType = null, int? size = null)
        {
            var failMessage = "Expression must be a property/field chain off of a(n) {0} instance";
            failMessage = string.Format(failMessage, typeof(T).Name);
            Action @throw = () => throw new InvalidOperationException(failMessage);

            // Is it even a MemberExpression?
            var lastMemberAccess = expression.Body as MemberExpression;

            if (lastMemberAccess == null
                || (!(lastMemberAccess.Member is PropertyInfo)
                    && !(lastMemberAccess.Member is FieldInfo)))
            {
                if (expression.Body.NodeType == ExpressionType.Convert
                    && expression.Body.Type == typeof(object)
                    && ((UnaryExpression)expression.Body).Operand is MemberExpression)
                {
                    // It's got to be unboxed
                    lastMemberAccess = (MemberExpression)((UnaryExpression)expression.Body).Operand;
                }
                else
                {
                    @throw();
                }
            }

            // Does the chain consist of MemberExpressions leading to a ParameterExpression of type T?
            MemberExpression diving = lastMemberAccess;
            // Retain a list of member names and the member expressions so we can rebuild the chain.
            List<string> names = new List<string>();
            List<MemberExpression> chain = new List<MemberExpression>();

            do
            {
                // Insert the names in the right order so expression
                // "Post.Author.Name" becomes parameter "PostAuthorName"
                names.Insert(0, diving?.Member.Name);
                chain.Insert(0, diving);

                var constant = diving?.Expression as ParameterExpression;
                diving = diving?.Expression as MemberExpression;

                if (constant != null && constant.Type == typeof(T))
                {
                    break;
                }
                else if (diving == null
                    || (!(diving.Member is PropertyInfo)
                        && !(diving.Member is FieldInfo)))
                {
                    @throw();
                }
            }
            while (diving != null);

            var dynamicParamName = string.Concat(names.ToArray());

            // Before we get all emitty...
            var lookup = string.Join("|", names.ToArray());

            var cache = CachedOutputSetters<T>.Cache;
            var setter = (Action<object, DynamicParameters>)cache[lookup];
            if (setter != null) goto MAKECALLBACK;

            // Come on let's build a method, let's build it, let's build it now!
            var dm = new DynamicMethod("ExpressionParam" + Guid.NewGuid().ToString(), null, new[] { typeof(object), GetType() }, true);
            var il = dm.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0); // [object]
            il.Emit(OpCodes.Castclass, typeof(T));    // [T]

            // Count - 1 to skip the last member access
            for (var i = 0; i < chain.Count - 1; i++)
            {
                var member = chain[i].Member;

                if (member is PropertyInfo)
                {
                    var get = ((PropertyInfo)member).GetGetMethod(true);
                    il.Emit(OpCodes.Callvirt, get); // [Member{i}]
                }
                else // Else it must be a field!
                {
                    il.Emit(OpCodes.Ldfld, (FieldInfo)member); // [Member{i}]
                }
            }

            var paramGetter = GetType().GetMethod("Get", new Type[] { typeof(string) }).MakeGenericMethod(lastMemberAccess.Type);

            il.Emit(OpCodes.Ldarg_1); // [target] [DynamicParameters]
            il.Emit(OpCodes.Ldstr, dynamicParamName); // [target] [DynamicParameters] [ParamName]
            il.Emit(OpCodes.Callvirt, paramGetter); // [target] [value], it's already typed thanks to generic method

            // GET READY
            var lastMember = lastMemberAccess.Member;
            if (lastMember is PropertyInfo)
            {
                var set = ((PropertyInfo)lastMember).GetSetMethod(true);
                il.Emit(OpCodes.Callvirt, set); // SET
            }
            else
            {
                il.Emit(OpCodes.Stfld, (FieldInfo)lastMember); // SET
            }

            il.Emit(OpCodes.Ret); // GO

            setter = (Action<object, DynamicParameters>)dm.CreateDelegate(typeof(Action<object, DynamicParameters>));
            lock (cache)
            {
                cache[lookup] = setter;
            }

            // Queue the preparation to be fired off when adding parameters to the DbCommand
            MAKECALLBACK:
            (outputCallbacks ?? (outputCallbacks = new List<Action>())).Add(() =>
            {
                // Finally, prep the parameter and attach the callback to it
                var targetMemberType = lastMemberAccess?.Type;
                int sizeToSet = (!size.HasValue && targetMemberType == typeof(string)) ? DbString.DefaultLength : size ?? 0;

                if (parameters.TryGetValue(dynamicParamName, out ParamInfo parameter))
                {
                    parameter.ParameterDirection = parameter.AttachedParam.Direction = ParameterDirection.InputOutput;

                    if (parameter.AttachedParam.Size == 0)
                    {
                        parameter.Size = parameter.AttachedParam.Size = sizeToSet;
                    }
                }
                else
                {
                    dbType = (!dbType.HasValue)
#pragma warning disable 618
                    ? SqlMapper.LookupDbType(targetMemberType, targetMemberType?.Name, true, out SqlMapper.ITypeHandler handler)
#pragma warning restore 618
                    : dbType;

                    // CameFromTemplate property would not apply here because this new param
                    // Still needs to be added to the command
                    Add(dynamicParamName, expression.Compile().Invoke(target), null, ParameterDirection.InputOutput, sizeToSet);
                }

                parameter = parameters[dynamicParamName];
                parameter.OutputCallback = setter;
                parameter.OutputTarget = target;
            });

            return this;
        }

        private List<Action> outputCallbacks;

        void SqlMapper.IParameterCallbacks.OnCompleted()
        {
            foreach (var param in from p in parameters select p.Value)
            {
                param.OutputCallback?.Invoke(param.OutputTarget, this);
            }
        }
    }
}
