using Xunit;
using System.Linq;
namespace Dapper.Tests
{
    public partial class TestSuite
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

                obj.Id.IsEqualTo(2);
                obj.A.IsEqualTo(42);
                obj.B.IsEqualTo(42);
                obj.C.IsEqualTo("abc");
                obj.D.IsEqualTo(AnEnum.A);
                obj.E.IsEqualTo(AnEnum.A);

                obj = data[1];
                obj.Id.IsEqualTo(1);
                if (applyNulls)
                {
                    obj.A.IsEqualTo(2); // cannot be null
                    obj.B.IsEqualTo(null);
                    obj.C.IsEqualTo(null);
                    obj.D.IsEqualTo(AnEnum.B);
                    obj.E.IsEqualTo(null);
                }
				else
                {
                    obj.A.IsEqualTo(2);
                    obj.B.IsEqualTo(2);
                    obj.C.IsEqualTo("def");
                    obj.D.IsEqualTo(AnEnum.B);
                    obj.E.IsEqualTo(AnEnum.B);
                }
            } finally
            {
                SqlMapper.Settings.ApplyNullValues = oldSetting;
            }
        }

		class NullTestClass
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
