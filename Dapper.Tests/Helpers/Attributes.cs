﻿using System;
using System.Data.SqlClient;
using Xunit;

namespace Dapper.Tests
{
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
            using (var conn = TestBase.GetOpenConnection())
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
            using (var conn = TestBase.GetOpenConnection())
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
