using System;
using System.Data.SqlClient;
using Xunit;

namespace Dapper.Tests
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class FactUnlessCoreCLRAttribute : FactAttribute
    {
        public FactUnlessCoreCLRAttribute(string url)
        {
#if COREFX
            Skip = $"CoreFX: {url}";
#endif
            this.Url = url;
        }
        public string Url { get; private set; }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class FactLongRunningAttribute : FactAttribute
    {
        public FactLongRunningAttribute()
        {
#if !LONG_RUNNING
            Skip = $"Long running";
#endif
        }
        public string Url { get; private set; }
    }
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
            using (var conn = TestSuite.GetOpenConnection())
            {
                try
                {
                    conn.Execute("declare @i int; set @I = 1;");
                }
                catch (SqlException s)
                {
                    if (s.Number == 137)
                        IsCaseSensitive = true;
                    else
                        throw;
                }
            }
        }
    }
}
