#if LINQ2SQL
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Dapper.Tests
{
    public class Linq2SqlTests : TestBase
    {
        [Fact]
        public void TestLinqBinaryToClass()
        {
            byte[] orig = new byte[20];
            new Random(123456).NextBytes(orig);
            var input = new System.Data.Linq.Binary(orig);

            var output = connection.Query<WithBinary>("select @input as [Value]", new { input }).First().Value;

            Assert.Equal(orig, output.ToArray());
        }

        [Fact]
        public void TestLinqBinaryRaw()
        {
            byte[] orig = new byte[20];
            new Random(123456).NextBytes(orig);
            var input = new System.Data.Linq.Binary(orig);

            var output = connection.Query<System.Data.Linq.Binary>("select @input as [Value]", new { input }).First();

            Assert.Equal(orig, output.ToArray());
        }

        private class WithBinary
        {
            public System.Data.Linq.Binary Value { get; set; }
        }

        private class NoDefaultConstructorWithBinary
        {
            public System.Data.Linq.Binary Value { get; set; }
            public int Ynt { get; set; }
            public NoDefaultConstructorWithBinary(System.Data.Linq.Binary val)
            {
                Value = val;
            }
        }

        [Fact]
        public void TestNoDefaultConstructorBinary()
        {
            byte[] orig = new byte[20];
            new Random(123456).NextBytes(orig);
            var input = new System.Data.Linq.Binary(orig);
            var output = connection.Query<NoDefaultConstructorWithBinary>("select @input as val", new { input }).First().Value;
            Assert.Equal(orig, output.ToArray());
        }
    }
}
#endif
