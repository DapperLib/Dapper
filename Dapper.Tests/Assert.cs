using System.Collections.Generic;

namespace Dapper.Tests
{
    public static class Assert
    {
        public static void IsEqualTo<T>(this T actual, T expected)
        {
            Xunit.Assert.Equal(expected, actual);
        }

        public static void IsSequenceEqualTo<T>(this IEnumerable<T> actual, IEnumerable<T> expected)
        {
            Xunit.Assert.Equal(expected, actual ?? new T[0]);
        }
        public static void IsMoreThan(this int a, int b)
        {
            Xunit.Assert.True(a > b, $"{a} should be larger than {b}");
        }
 
        public static void IsMoreThan(this long a, int b)
        {
            Xunit.Assert.True(a > b, $"{a} should be larger than {b}");
        }

        public static void Fail(string message = null)
        {
            Xunit.Assert.True(false, message ?? "Expectation failed");
        }
        public static void IsFalse(this bool b)
        {
            Xunit.Assert.False(b);
        }

        public static void IsTrue(this bool b)
        {
            Xunit.Assert.True(b);
        }

        public static void IsNull(this object obj)
        {
            Xunit.Assert.Null(obj);
        }

        public static void IsNotNull(this object obj)
        {
            Xunit.Assert.NotNull(obj);
        }
    }
}
