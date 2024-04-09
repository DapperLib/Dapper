using System;

namespace Dapper
{
    /// <summary>
    /// Tell Dapper to use an explicit constructor, passing nulls or 0s for all parameters
    /// </summary>
    /// <remarks>
    /// Usage on methods is limited to the usage with Dapper.AOT (https://github.com/DapperLib/DapperAOT)
    /// </remarks>
    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class ExplicitConstructorAttribute : Attribute
    {
    }
}
