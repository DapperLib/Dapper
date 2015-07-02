using System;
using System.Collections.Generic;
using System.Linq;

namespace Dapper.Contrib.Tests
{
    /// <summary>
    /// Assert extensions borrowed from Sam's code in DapperTests
    /// </summary>
    static class Assert
    {
        public static void IsEqualTo<T>(this T obj, T other)
        {
            if (!obj.Equals(other))
            {
                throw new ApplicationException(string.Format("{0} should be equals to {1}", obj, other));
            }
        }

        public static void IsMoreThan(this int obj, int other)
        {
            if (obj < other)
            {
                throw new ApplicationException(string.Format("{0} should be larger than {1}", obj, other));
            }
        }

        public static void IsMoreThan(this long obj, int other)
        {
            if (obj < other)
            {
                throw new ApplicationException(string.Format("{0} should be larger than {1}", obj, other));
            }
        }

        public static void IsSequenceEqualTo<T>(this IEnumerable<T> obj, IEnumerable<T> other)
        {
            if (!obj.SequenceEqual(other))
            {
                throw new ApplicationException(string.Format("{0} should be equals to {1}", obj, other));
            }
        }

        public static void IsFalse(this bool b)
        {
            if (b)
            {
                throw new ApplicationException("Expected false");
            }
        }

        public static void IsTrue(this bool b)
        {
            if (!b)
            {
                throw new ApplicationException("Expected true");
            }
        }

        public static void IsNull(this object obj)
        {
            if (obj != null)
            {
                throw new ApplicationException("Expected null");
            }
        }
        public static void IsNotNull(this object obj)
        {
            if (obj == null)
            {
                throw new ApplicationException("Expected not null");
            }
        }

    }
}