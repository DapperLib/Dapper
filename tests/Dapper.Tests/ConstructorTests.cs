using System;
using System.Linq;
using Xunit;

namespace Dapper.Tests
{
    [Collection("ConstructorTests")]
    public sealed class SystemSqlClientConstructorTests : ConstructorTests<SystemSqlClientProvider> { }
#if MSSQLCLIENT
    [Collection("ConstructorTests")]
    public sealed class MicrosoftSqlClientConstructorTests : ConstructorTests<MicrosoftSqlClientProvider> { }
#endif

    public abstract class ConstructorTests<TProvider> : TestBase<TProvider> where TProvider : DatabaseProvider
    {
        [Fact]
        public void TestAbstractInheritance()
        {
            var order = connection.Query<AbstractInheritance.ConcreteOrder>("select 1 Internal,2 Protected,3 [Public],4 Concrete").First();

            Assert.Equal(1, order.Internal);
            Assert.Equal(2, order.ProtectedVal);
            Assert.Equal(3, order.Public);
            Assert.Equal(4, order.Concrete);
        }

        [Fact]
        public void TestMultipleConstructors()
        {
            MultipleConstructors mult = connection.Query<MultipleConstructors>("select 0 A, 'Dapper' b").First();
            Assert.Equal(0, mult.A);
            Assert.Equal("Dapper", mult.B);
        }

        [Fact]
        public void TestConstructorsWithAccessModifiers()
        {
            ConstructorsWithAccessModifiers value = connection.Query<ConstructorsWithAccessModifiers>("select 0 A, 'Dapper' b").First();
            Assert.Equal(1, value.A);
            Assert.Equal("Dapper!", value.B);
        }

        [Fact]
        public void TestNoDefaultConstructor()
        {
            var guid = Guid.NewGuid();
            NoDefaultConstructor nodef = connection.Query<NoDefaultConstructor>("select CAST(NULL AS integer) A1,  CAST(NULL AS integer) b1, CAST(NULL AS real) f1, 'Dapper' s1, G1 = @id", new { id = guid }).First();
            Assert.Equal(0, nodef.A);
            Assert.Null(nodef.B);
            Assert.Equal(0, nodef.F);
            Assert.Equal("Dapper", nodef.S);
            Assert.Equal(nodef.G, guid);
        }

        [Fact]
        public void TestNoDefaultConstructorWithChar()
        {
            const char c1 = 'ą';
            const char c3 = 'ó';
            NoDefaultConstructorWithChar nodef = connection.Query<NoDefaultConstructorWithChar>("select @c1 c1, @c2 c2, @c3 c3", new { c1 = c1, c2 = (char?)null, c3 = c3 }).First();
            Assert.Equal(c1, nodef.Char1);
            Assert.Null(nodef.Char2);
            Assert.Equal(c3, nodef.Char3);
        }

        [Fact]
        public void TestNoDefaultConstructorWithEnum()
        {
            NoDefaultConstructorWithEnum nodef = connection.Query<NoDefaultConstructorWithEnum>("select cast(2 as smallint) E1, cast(5 as smallint) n1, cast(null as smallint) n2").First();
            Assert.Equal(ShortEnum.Two, nodef.E);
            Assert.Equal(ShortEnum.Five, nodef.NE1);
            Assert.Null(nodef.NE2);
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

            Assert.Single(rows);
            Assert.Equal(1, rows[0].Field);
            Assert.Equal(1, rows[0].Field_1);
            Assert.True(rows[0].GetWentThroughProperConstructor());
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
                B = default!;
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
            public string? B { get; set; }
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
            Assert.Equal(1, output.Foo);
        }

        [Fact]
        public void CtorWithUnderscores()
        {
            var obj = connection.QueryFirst<Type_ParamsWithUnderscores>("select 'abc' as FIRST_NAME, 'def' as LAST_NAME");
            Assert.NotNull(obj);
            Assert.Equal("abc", obj.FirstName);
            Assert.Equal("def", obj.LastName);
        }

        [Fact]
        public void CtorWithoutUnderscores()
        {
            DefaultTypeMap.MatchNamesWithUnderscores = true;
            var obj = connection.QueryFirst<Type_ParamsWithoutUnderscores>("select 'abc' as FIRST_NAME, 'def' as LAST_NAME");
            Assert.NotNull(obj);
            Assert.Equal("abc", obj.FirstName);
            Assert.Equal("def", obj.LastName);
        }

        [Fact]
        public void Issue1993_PreferPropertyOverField() // https://github.com/DapperLib/Dapper/issues/1993
        {
            var oldValue = DefaultTypeMap.MatchNamesWithUnderscores;
            try
            {
                DefaultTypeMap.MatchNamesWithUnderscores = true;

                var map = new DefaultTypeMap(typeof(ShowIssue1993));
                var first = map.GetMember("field_first");
                Assert.NotNull(first);
                Assert.Null(first.Field);
                Assert.Equal(nameof(ShowIssue1993.FieldFirst), first.Property?.Name);

                var last = map.GetMember("field_last");
                Assert.NotNull(last);
                Assert.Null(last.Field);
                Assert.Equal(nameof(ShowIssue1993.FieldLast), last.Property?.Name);
            }
            finally
            {
                DefaultTypeMap.MatchNamesWithUnderscores = oldValue;
            }
        }

        [Fact]
        public void Issue1993_Query()
        {
            var oldValue = DefaultTypeMap.MatchNamesWithUnderscores;
            try
            {
                DefaultTypeMap.MatchNamesWithUnderscores = true;

                var obj = connection.QueryFirst<ShowIssue1993>("select 'abc' as field_first, 'def' as field_last");
                Assert.Equal("abc", obj.FieldFirst);
                Assert.Equal("def", obj.FieldLast);

                Assert.Equal("abc", obj.AltFieldFirst);
                Assert.Equal("def", obj.AltFieldLast);
            }
            finally
            {
                DefaultTypeMap.MatchNamesWithUnderscores = oldValue;
            }
        }

        public class ShowIssue1993
        {
            private string _fieldFirst { get; set; } = null!; // not actually a field
            public string FieldFirst
            {
                get => _fieldFirst;
                set => _fieldFirst = AltFieldFirst = value;
            }

            public string FieldLast
            {
                get => _fieldLast;
                set => _fieldLast = AltFieldLast = value;
            }
            private string _fieldLast { get; set; } = null!;// not actually a field

            public string AltFieldFirst { get; set; } = null!;
            public string AltFieldLast { get; set; } = null!;
        }

        class Type_ParamsWithUnderscores
        {
            public string FirstName { get; }
            public string LastName { get; }
            public Type_ParamsWithUnderscores(string first_name, string last_name)
            {
                FirstName = first_name;
                LastName = last_name;
            }
        }
        class Type_ParamsWithoutUnderscores
        {
            public string FirstName { get; }
            public string LastName { get; }
            public Type_ParamsWithoutUnderscores(string firstName, string lastName)
            {
                FirstName = firstName;
                LastName = lastName;
            }
        }
    }
}
