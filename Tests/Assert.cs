using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if DNXCORE50
using ApplicationException = global::System.InvalidOperationException;
#endif

namespace SqlMapper
{
    static class Assert
    {

        public static void IsEqualTo<T>(this T obj, T other)
        {
            if (!Equals(obj, other))
            {
                throw new ApplicationException(string.Format("{0} should be equals to {1}", obj, other));
            }
        }

        public static void IsSequenceEqualTo<T>(this IEnumerable<T> obj, IEnumerable<T> other)
        {
            if (!(obj ?? new T[0]).SequenceEqual(other ?? new T[0]))
            {
                throw new ApplicationException(string.Format("{0} should be equals to {1}", obj, other));
            }
        }

        public static void Fail()
        {
            throw new ApplicationException("Expectation failed");
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
