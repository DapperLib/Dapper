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
    [XunitTestCaseDiscoverer("Dapper.Tests.FactDiscoverer", "Dapper.Tests")]
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
    [XunitTestCaseDiscoverer("Dapper.Tests.TheoryDiscoverer", "Dapper.Tests")]
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

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class FactRequiredCompatibilityLevelAttribute : FactAttribute
    {
        public FactRequiredCompatibilityLevelAttribute(int level) : base()
        {
            if (DetectedLevel < level)
            {
                Skip = $"Compatibility level {level} required; detected {DetectedLevel}";
            }
        }

        public const int SqlServer2016 = 130;
        public static readonly int DetectedLevel;
        static FactRequiredCompatibilityLevelAttribute()
        {
            using (var conn = DatabaseProvider<SystemSqlClientProvider>.Instance.GetOpenConnection())
            {
                try
                {
                    DetectedLevel = conn.QuerySingle<int>("SELECT compatibility_level FROM sys.databases where name = DB_NAME()");
                }
                catch { /* don't care */ }
            }
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class FactUnlessCaseSensitiveDatabaseAttribute : FactAttribute
    {
        public FactUnlessCaseSensitiveDatabaseAttribute() : base()
        {
            if (IsCaseSensitive)
            {
                Skip = "Case sensitive database";
            }
        }

        public static readonly bool IsCaseSensitive;
        static FactUnlessCaseSensitiveDatabaseAttribute()
        {
            using (var conn = DatabaseProvider<SystemSqlClientProvider>.Instance.GetOpenConnection())
            {
                try
                {
                    conn.Execute("declare @i int; set @I = 1;");
                }
                catch (Exception ex) when (ex.GetType().Name == "SqlException")
                {
                    int err = ((dynamic)ex).Number;
                    if (err == 137)
                        IsCaseSensitive = true;
                    else
                        throw;
                }
            }
        }
    }
}
