using System;
using Xunit.Sdk;

namespace Dapper.Tests
{
    /// <summary>
    /// <para>Override for <see cref="Xunit.FactAttribute"/> that truncates our DisplayName down.</para>
    /// <para>
    /// Attribute that is applied to a method to indicate that it is a fact that should
    /// be run by the test runner. It can also be extended to support a customized definition
    /// of a test method.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    [XunitTestCaseDiscoverer("Dapper.Tests.FactDiscoverer", "Dapper.Tests.Contrib")]
    public class FactAttribute : Xunit.FactAttribute
    {
    }

    /// <summary>
    /// <para>Override for <see cref="Xunit.TheoryAttribute"/> that truncates our DisplayName down.</para>
    /// <para>
    /// Marks a test method as being a data theory. Data theories are tests which are
    /// fed various bits of data from a data source, mapping to parameters on the test
    /// method. If the data source contains multiple rows, then the test method is executed
    /// multiple times (once with each data row). Data is provided by attributes which
    /// derive from Xunit.Sdk.DataAttribute (notably, Xunit.InlineDataAttribute and Xunit.MemberDataAttribute).
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    [XunitTestCaseDiscoverer("Dapper.Tests.TheoryDiscoverer", "Dapper.Tests.Contrib")]
    public class TheoryAttribute : Xunit.TheoryAttribute { }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class FactLongRunningAttribute : FactAttribute
    {
        public FactLongRunningAttribute()
        {
#if !LONG_RUNNING
            Skip = "Long running";
#endif
        }

        public string Url { get; private set; }
    }
}
