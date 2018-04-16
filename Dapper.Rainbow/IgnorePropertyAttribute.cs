using System;

namespace Dapper
{
    /// <summary>
    /// Specifies whether a property should be ignored for database operations.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class IgnorePropertyAttribute : Attribute
    {
        /// <summary>
        /// Specifies whether a property should be ignored for database operations.
        /// </summary>
        /// <param name="ignore">Whether to ignore this property.</param>
        public IgnorePropertyAttribute(bool ignore)
        {
            Value = ignore;
        }

        /// <summary>
        /// Whether to ignore this property.
        /// </summary>
        public bool Value { get; set; }
    }
}
