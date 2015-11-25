using System;
using System.Reflection;

namespace Dapper
{
    /// <summary>
    /// Represents simple member map for one of target parameter or property or field to source DataReader column
    /// </summary>
    sealed class SimpleMemberMap : SqlMapper.IMemberMap
    {
        /// <summary>
        /// Creates instance for simple property mapping
        /// </summary>
        /// <param name="columnName">DataReader column name</param>
        /// <param name="property">Target property</param>
        public SimpleMemberMap(string columnName, PropertyInfo property)
        {
            if (columnName == null)
                throw new ArgumentNullException(nameof(columnName));

            if (property == null)
                throw new ArgumentNullException(nameof(property));

            ColumnName = columnName;
            Property = property;
        }

        /// <summary>
        /// Creates instance for simple field mapping
        /// </summary>
        /// <param name="columnName">DataReader column name</param>
        /// <param name="field">Target property</param>
        public SimpleMemberMap(string columnName, FieldInfo field)
        {
            if (columnName == null)
                throw new ArgumentNullException(nameof(columnName));

            if (field == null)
                throw new ArgumentNullException(nameof(field));

            ColumnName = columnName;
            Field = field;
        }

        /// <summary>
        /// Creates instance for simple constructor parameter mapping
        /// </summary>
        /// <param name="columnName">DataReader column name</param>
        /// <param name="parameter">Target constructor parameter</param>
        public SimpleMemberMap(string columnName, ParameterInfo parameter)
        {
            if (columnName == null)
                throw new ArgumentNullException(nameof(columnName));

            if (parameter == null)
                throw new ArgumentNullException(nameof(parameter));

            ColumnName = columnName;
            Parameter = parameter;
        }

        /// <summary>
        /// DataReader column name
        /// </summary>
        public string ColumnName { get; }

        /// <summary>
        /// Target member type
        /// </summary>
        public Type MemberType => Field?.FieldType ?? Property?.PropertyType ?? Parameter?.ParameterType;

        /// <summary>
        /// Target property
        /// </summary>
        public PropertyInfo Property { get; }

        /// <summary>
        /// Target field
        /// </summary>
        public FieldInfo Field { get; }

        /// <summary>
        /// Target constructor parameter
        /// </summary>
        public ParameterInfo Parameter { get; }
    }
}
