using System;
using System.Collections.Generic;

namespace SqlMapper
{
    static class Assert
    {

        public static void IsEqualTo<T>(this T obj, T other)
        {
            Xunit.Assert.Equal(obj, other);
        }

        public static void IsSequenceEqualTo<T>(this IEnumerable<T> obj, IEnumerable<T> other)
        {
            Xunit.Assert.Equal(obj ?? new T[0], other);
        }

        public static void Fail()
        {
            Xunit.Assert.True(false, "Expectation failed");
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
