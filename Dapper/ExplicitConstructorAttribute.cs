using System;

namespace Dapper
{
    /// <summary>
    /// Tell Dapper to use an explicit constructor, passing nulls or 0s for all parameters
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class ExplicitConstructorAttribute : Attribute
    {
    }
}
