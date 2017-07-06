﻿using System;
using System.Data;
using System.Linq;
using Xunit;

namespace Dapper.Tests
{
    public class ConstructorTests : TestBase
    {
        [Fact]
        public void TestAbstractInheritance()
        {
            var order = connection.Query<AbstractInheritance.ConcreteOrder>("select 1 Internal,2 Protected,3 [Public],4 Concrete").First();

            order.Internal.IsEqualTo(1);
            order.ProtectedVal.IsEqualTo(2);
            order.Public.IsEqualTo(3);
            order.Concrete.IsEqualTo(4);
        }

        [Fact]
        public void TestMultipleConstructors()
        {
            MultipleConstructors mult = connection.Query<MultipleConstructors>("select 0 A, 'Dapper' b").First();
            mult.A.IsEqualTo(0);
            mult.B.IsEqualTo("Dapper");
        }

        [Fact]
        public void TestConstructorsWithAccessModifiers()
        {
            ConstructorsWithAccessModifiers value = connection.Query<ConstructorsWithAccessModifiers>("select 0 A, 'Dapper' b").First();
            value.A.IsEqualTo(1);
            value.B.IsEqualTo("Dapper!");
        }

        [Fact]
        public void TestNoDefaultConstructor()
        {
            var guid = Guid.NewGuid();
            NoDefaultConstructor nodef = connection.Query<NoDefaultConstructor>("select CAST(NULL AS integer) A1,  CAST(NULL AS integer) b1, CAST(NULL AS real) f1, 'Dapper' s1, G1 = @id", new { id = guid }).First();
            nodef.A.IsEqualTo(0);
            nodef.B.IsEqualTo(null);
            nodef.F.IsEqualTo(0);
            nodef.S.IsEqualTo("Dapper");
            nodef.G.IsEqualTo(guid);
        }

        [Fact]
        public void TestNoDefaultConstructorWithChar()
        {
            const char c1 = 'ą';
            const char c3 = 'ó';
            NoDefaultConstructorWithChar nodef = connection.Query<NoDefaultConstructorWithChar>("select @c1 c1, @c2 c2, @c3 c3", new { c1 = c1, c2 = (char?)null, c3 = c3 }).First();
            nodef.Char1.IsEqualTo(c1);
            nodef.Char2.IsEqualTo(null);
            nodef.Char3.IsEqualTo(c3);
        }

        [Fact]
        public void TestNoDefaultConstructorWithEnum()
        {
            NoDefaultConstructorWithEnum nodef = connection.Query<NoDefaultConstructorWithEnum>("select cast(2 as smallint) E1, cast(5 as smallint) n1, cast(null as smallint) n2").First();
            nodef.E.IsEqualTo(ShortEnum.Two);
            nodef.NE1.IsEqualTo(ShortEnum.Five);
            nodef.NE2.IsEqualTo(null);
        }

        [Fact]
        public void ExplicitConstructors()
        {
            var rows = connection.Query<_ExplicitConstructors>(@"
declare @ExplicitConstructors table (
    Field INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    Field_1 INT NOT NULL);
insert @ExplicitConstructors(Field_1) values (1);
SELECT * FROM @ExplicitConstructors"
).ToList();

            rows.Count.IsEqualTo(1);
            rows[0].Field.IsEqualTo(1);
            rows[0].Field_1.IsEqualTo(1);
            rows[0].GetWentThroughProperConstructor().IsTrue();
        }

        private class _ExplicitConstructors
        {
            public int Field { get; set; }
            public int Field_1 { get; set; }

            private readonly bool WentThroughProperConstructor;

            public _ExplicitConstructors() { /* yep */ }

            [ExplicitConstructor]
            public _ExplicitConstructors(string foo, int bar)
            {
                WentThroughProperConstructor = true;
            }

            public bool GetWentThroughProperConstructor()
            {
                return WentThroughProperConstructor;
            }
        }

        public static class AbstractInheritance
        {
            public abstract class Order
            {
                internal int Internal { get; set; }
                protected int Protected { get; set; }
                public int Public { get; set; }

                public int ProtectedVal => Protected;
            }

            public class ConcreteOrder : Order
            {
                public int Concrete { get; set; }
            }
        }

        private class MultipleConstructors
        {
            public MultipleConstructors()
            {
            }

            public MultipleConstructors(int a, string b)
            {
                A = a + 1;
                B = b + "!";
            }

            public int A { get; set; }
            public string B { get; set; }
        }

        private class ConstructorsWithAccessModifiers
        {
            private ConstructorsWithAccessModifiers()
            {
            }

            public ConstructorsWithAccessModifiers(int a, string b)
            {
                A = a + 1;
                B = b + "!";
            }

            public int A { get; set; }
            public string B { get; set; }
        }

        private class NoDefaultConstructor
        {
            public NoDefaultConstructor(int a1, int? b1, float f1, string s1, Guid G1)
            {
                A = a1;
                B = b1;
                F = f1;
                S = s1;
                G = G1;
            }

            public int A { get; set; }
            public int? B { get; set; }
            public float F { get; set; }
            public string S { get; set; }
            public Guid G { get; set; }
        }

        private class NoDefaultConstructorWithChar
        {
            public NoDefaultConstructorWithChar(char c1, char? c2, char? c3)
            {
                Char1 = c1;
                Char2 = c2;
                Char3 = c3;
            }

            public char Char1 { get; set; }
            public char? Char2 { get; set; }
            public char? Char3 { get; set; }
        }

        private class NoDefaultConstructorWithEnum
        {
            public NoDefaultConstructorWithEnum(ShortEnum e1, ShortEnum? n1, ShortEnum? n2)
            {
                E = e1;
                NE1 = n1;
                NE2 = n2;
            }

            public ShortEnum E { get; set; }
            public ShortEnum? NE1 { get; set; }
            public ShortEnum? NE2 { get; set; }
        }

        private class WithPrivateConstructor
        {
            public int Foo { get; set; }
            private WithPrivateConstructor()
            {
            }
        }

        [Fact]
        public void TestWithNonPublicConstructor()
        {
            var output = connection.Query<WithPrivateConstructor>("select 1 as Foo").First();
            output.Foo.IsEqualTo(1);
        }
    }
}
