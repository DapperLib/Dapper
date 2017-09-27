using System;

namespace Dapper.Contrib.Extensions
{
    /// <summary>
    /// Specifies that this is a row version column, used during optimistic concurrency checks.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class RowVersionAttribute : Attribute
    {
    }
}