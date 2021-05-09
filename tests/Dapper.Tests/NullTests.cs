using Xunit;
using System.Linq;
namespace Dapper.Tests
{
    [Collection(NonParallelDefinition.Name)]
    public sealed class SystemSqlClientNullTests : NullTests<SystemSqlClientProvider> { }
#if MSSQLCLIENT
    [Collection(NonParallelDefinition.Name)]
    public sealed class MicrosoftSqlClientNullTests : NullTests<MicrosoftSqlClientProvider> { }
#endif

    public abstract class NullTests<TProvider> : TestBase<TProvider> where TProvider : DatabaseProvider
    {
        [Fact]
        public void TestNullableDefault()
        {
            TestNullable(false);
        }

        [Fact]
        public void TestNullableApplyNulls()
        {
            TestNullable(true);
        }

        private void TestNullable(bool applyNulls)
        {
            bool oldSetting = SqlMapper.Settings.ApplyNullValues;
            try
            {
                SqlMapper.Settings.ApplyNullValues = applyNulls;
                SqlMapper.PurgeQueryCache();

                var data = connection.Query<NullTestClass>(@"
declare @data table(Id int not null, A int null, B int null, C varchar(20), D int null, E int null)
insert @data (Id, A, B, C, D, E) values 
	(1,null,null,null,null,null),
	(2,42,42,'abc',2,2)
select * from @data").ToDictionary(_ => _.Id);

                var obj = data[2];

                Assert.Equal(2, obj.Id);
                Assert.Equal(42, obj.A);
                Assert.Equal(42, obj.B);
                Assert.Equal("abc", obj.C);
                Assert.Equal(AnEnum.A, obj.D);
                Assert.Equal(AnEnum.A, obj.E);

                obj = data[1];
                Assert.Equal(1, obj.Id);
                if (applyNulls)
                {
                    Assert.Equal(2, obj.A); // cannot be null
                    Assert.Null(obj.B);
                    Assert.Null(obj.C);
                    Assert.Equal(AnEnum.B, obj.D);
                    Assert.Null(obj.E);
                }
                else
                {
                    Assert.Equal(2, obj.A);
                    Assert.Equal(2, obj.B);
                    Assert.Equal("def", obj.C);
                    Assert.Equal(AnEnum.B, obj.D);
                    Assert.Equal(AnEnum.B, obj.E);
                }
            }
            finally
            {
                SqlMapper.Settings.ApplyNullValues = oldSetting;
            }
        }

        private class NullTestClass
        {
            public int Id { get; set; }
            public int A { get; set; }
            public int? B { get; set; }
            public string C { get; set; }
            public AnEnum D { get; set; }
            public AnEnum? E { get; set; }

            public NullTestClass()
            {
                A = 2;
                B = 2;
                C = "def";
                D = AnEnum.B;
                E = AnEnum.B;
            }
        }
    }
}
