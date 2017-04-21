using System.Collections.Generic;
using System.Linq;
using Xunit;
using Child = Dapper.Tests.MultiMapTests.Child;
using Parent = Dapper.Tests.MultiMapTests.Parent;

namespace Dapper.Tests
{
    public class MultiMapTupleTests : TestBase
    {
        // note: implementation is not optimized yet - just basic reflection, no IL crazy
        // intent: to explore the API, allowing horizontally partitioned data to be fetched
        // more convenienty by expressing them as tuples; this is similar to the (hard to use)
        // pre-existing multi-generic Query<...> API that does the same, i.e.
        // similar to connection.Query<Parent,Child,Parent>(...) - which folks find hard to grok

        // here are 3 possible ideas for expressing that
        [Fact]
        public void GetRawTuples_Manual() // here we use an explicit map function that returns the entire row, and
                                          // in doing so provides the contextual name metadata
        {
            var tuples = connection.Query(
                @"select 1 as [Id], 1 as [Id] union all select 1,2 union all select 2,3 union all select 1,4 union all select 3,5",
                ((Parent parent, Child child)row) => row
            ).AsList();

            tuples.Count.IsEqualTo(5);

            string.Join(",", tuples.Select(x => $"({x.parent.Id},{x.child.Id})")).IsEqualTo(
                "(1,1),(1,2),(2,3),(1,4),(3,5)");
        }

        [Fact]
        public void GetRawTuples_Passthru() // here we use a declarative map function, so all we provide is the T to Map<T>
        {
            var tuples = connection.Query(
                @"select 1 as [Id], 1 as [Id] union all select 1,2 union all select 2,3 union all select 1,4 union all select 3,5",
                SqlMapper.Map<(Parent parent, Child child)>()
            ).AsList();

            tuples.Count.IsEqualTo(5);

            string.Join(",", tuples.Select(x => $"({x.parent.Id},{x.child.Id})")).IsEqualTo(
                "(1,1),(1,2),(2,3),(1,4),(3,5)");
        }

        [Fact]
        public void GetRawTuples_Split() // here we provide the tuple metadata via the T in QuerySplit<T> - note the name
                                         // is different to avoid ambiguity with the primary Query<T> which does something else
        {
            var tuples = connection.QuerySplit<(Parent parent, Child child)>(
                @"select 1 as [Id], 1 as [Id] union all select 1,2 union all select 2,3 union all select 1,4 union all select 3,5"
            ).AsList();

            tuples.Count.IsEqualTo(5);

            string.Join(",", tuples.Select(x => $"({x.parent.Id},{x.child.Id})")).IsEqualTo(
                "(1,1),(1,2),(2,3),(1,4),(3,5)");
        }

        // these are more complex examples that make use of a non-trivial mapping function to play with the horizontal partitions *before*
        // yielding them - for example, to take parent/child data and stitch it together such that the children are attached to the parents

        // compare and contrast: MultiMapTests.ParentChildIdentityAssociations
        [Fact]
        public void ParentChildIdentityAssociations()
        {
            var lookup = new Dictionary<int, Parent>();
            var parents = connection.Query(@"select 1 as [Id], 1 as [Id] union all select 1,2 union all select 2,3 union all select 1,4 union all select 3,5",
                ((Parent parent, Child child) row) =>
                {
                    if (!lookup.TryGetValue(row.parent.Id, out Parent found))
                    {
                        lookup.Add(row.parent.Id, found = row.parent);
                    }
                    found.Children.Add(row.child);
                    return found;
                }).Distinct().ToDictionary(p => p.Id);
            parents.Count.IsEqualTo(3);
            parents[1].Children.Select(c => c.Id).SequenceEqual(new[] { 1, 2, 4 }).IsTrue();
            parents[2].Children.Select(c => c.Id).SequenceEqual(new[] { 3 }).IsTrue();
            parents[3].Children.Select(c => c.Id).SequenceEqual(new[] { 5 }).IsTrue();
        }

        // compare and contrast: MultiMapTests.TestMultiMap
        [Fact]
        public void TestMultiMap()
        {
            const string createSql = @"
                create table #Users (Id int, Name varchar(20))
                create table #Posts (Id int, OwnerId int, Content varchar(20))

                insert #Users values(99, 'Sam')
                insert #Users values(2, 'I am')

                insert #Posts values(1, 99, 'Sams Post1')
                insert #Posts values(2, 99, 'Sams Post2')
                insert #Posts values(3, null, 'no ones post')
";
            connection.Execute(createSql);
            try
            {
                const string sql =
    @"select * from #Posts p 
left join #Users u on u.Id = p.OwnerId 
Order by p.Id";

                var data = connection.Query(sql, ((Post post, User user) row) => { row.post.Owner = row.user; return row.post; }).ToList();
                var p = data.First();

                p.Content.IsEqualTo("Sams Post1");
                p.Id.IsEqualTo(1);
                p.Owner.Name.IsEqualTo("Sam");
                p.Owner.Id.IsEqualTo(99);

                data[2].Owner.IsNull();
            }
            finally
            {
                connection.Execute("drop table #Users drop table #Posts");
            }
        }




    }
}
