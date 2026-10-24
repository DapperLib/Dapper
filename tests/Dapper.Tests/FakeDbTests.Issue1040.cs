#if !NET481
using System.Collections.Generic;
using pengdows.crud.fakeDb;
using Xunit;

namespace Dapper.Tests
{
    public sealed class FakeDbIssue1040Tests
    {
        private static string Run(string sql, object args)
        {
            using var conn = new fakeDbConnection(new FakeDataStore());
            conn.EnqueueNonQueryResult(1);
            conn.Open();
            conn.Execute(sql, args);
            return Assert.Single(conn.ExecutedNonQueryTexts);
        }

        [Fact]
        public void ListExpansionDoesNotCollideWithLongerListName()
        {
            var text = Run(
                "select 1 where Value in @values1 or Value in @values11",
                new { values1 = new[] { 1, 2 }, values11 = new[] { 3, 4 } });
            Assert.Equal("select 1 where Value in (@values11,@values12) or Value in (@values111,@values112)", text);
        }

        [Fact]
        public void ListExpansionIsIndependentOfDeclarationOrder()
        {
            var text = Run(
                "select 1 where Value in @values11 or Value in @values1",
                new { values11 = new[] { 3, 4 }, values1 = new[] { 1, 2 } });
            Assert.Equal("select 1 where Value in (@values111,@values112) or Value in (@values11,@values12)", text);
        }

        [Fact]
        public void ThreeOverlappingListNamesExpandIndependently()
        {
            var text = Run(
                "select 1 where a in @v or b in @v1 or c in @v11",
                new { v = new[] { 1, 2 }, v1 = new[] { 3, 4 }, v11 = new[] { 5, 6 } });
            Assert.Equal("select 1 where a in (@v1,@v2) or b in (@v11,@v12) or c in (@v111,@v112)", text);
        }

        [Fact]
        public void DynamicParametersListExpansionDoesNotCollide()
        {
            var args = new DynamicParameters();
            args.Add("values1", new[] { 1, 2 });
            args.Add("values11", new[] { 3, 4 });
            var text = Run("select 1 where Value in @values1 or Value in @values11", args);
            Assert.Equal("select 1 where Value in (@values11,@values12) or Value in (@values111,@values112)", text);
        }

        [Fact]
        public void DictionaryListExpansionDoesNotCollide()
        {
            var args = new Dictionary<string, object>
            {
                ["values1"] = new[] { 1, 2 },
                ["values11"] = new[] { 3, 4 },
            };
            var text = Run("select 1 where Value in @values1 or Value in @values11", args);
            Assert.Equal("select 1 where Value in (@values11,@values12) or Value in (@values111,@values112)", text);
        }

        [Fact]
        public void UnrelatedListNamesKeepOrderAndExpandNormally()
        {
            var text = Run(
                "select 1 where a in @ids or b in @codes",
                new { ids = new[] { 1, 2 }, codes = new[] { 3 } });
            Assert.Equal("select 1 where a in (@ids1,@ids2) or b in (@codes1)", text);
        }
    }
}
#endif
