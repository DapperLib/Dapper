Dapper - a simple object mapper for .Net
========================================
[![Build status](https://ci.appveyor.com/api/projects/status/8rbgoxqio76ynj4h?svg=true)](https://ci.appveyor.com/project/StackExchange/dapper)

Release Notes
-------------
Located at [stackexchange.github.io/Dapper](https://stackexchange.github.io/Dapper/)

Packages
--------

MyGet Pre-release feed: https://www.myget.org/gallery/dapper

| Package | NuGet Stable | NuGet Pre-release | Downloads | MyGet |
| ------- | ------------ | ----------------- | --------- | ----- |
| [Dapper](https://www.nuget.org/packages/Dapper/) | [![Dapper](https://img.shields.io/nuget/v/Dapper.svg)](https://www.nuget.org/packages/Dapper/) | [![Dapper](https://img.shields.io/nuget/vpre/Dapper.svg)](https://www.nuget.org/packages/Dapper/) | [![Dapper](https://img.shields.io/nuget/dt/Dapper.svg)](https://www.nuget.org/packages/Dapper/) | [![Dapper MyGet](https://img.shields.io/myget/dapper/vpre/Dapper.svg)](https://www.myget.org/feed/dapper/package/nuget/Dapper) |
| [Dapper.Contrib](https://www.nuget.org/packages/Dapper.Contrib/) | [![Dapper.Contrib](https://img.shields.io/nuget/v/Dapper.Contrib.svg)](https://www.nuget.org/packages/Dapper.Contrib/) | [![Dapper.Contrib](https://img.shields.io/nuget/vpre/Dapper.Contrib.svg)](https://www.nuget.org/packages/Dapper.Contrib/) | [![Dapper.Contrib](https://img.shields.io/nuget/dt/Dapper.Contrib.svg)](https://www.nuget.org/packages/Dapper.Contrib/) | [![Dapper.Contrib MyGet](https://img.shields.io/myget/dapper/vpre/Dapper.Contrib.svg)](https://www.myget.org/feed/dapper/package/nuget/Dapper.Contrib) |
| [Dapper.EntityFramework](https://www.nuget.org/packages/Dapper.EntityFramework/) | [![Dapper.EntityFramework](https://img.shields.io/nuget/v/Dapper.EntityFramework.svg)](https://www.nuget.org/packages/Dapper.EntityFramework/) | [![Dapper.EntityFramework](https://img.shields.io/nuget/vpre/Dapper.EntityFramework.svg)](https://www.nuget.org/packages/Dapper.EntityFramework/) | [![Dapper.EntityFramework](https://img.shields.io/nuget/dt/Dapper.EntityFramework.svg)](https://www.nuget.org/packages/Dapper.EntityFramework/) | [![Dapper.EntityFramework MyGet](https://img.shields.io/myget/dapper/vpre/Dapper.EntityFramework.svg)](https://www.myget.org/feed/dapper/package/nuget/Dapper.EntityFramework) |
| [Dapper.EntityFramework.StrongName](https://www.nuget.org/packages/Dapper.EntityFramework.StrongName/) | [![Dapper.EntityFramework.StrongName](https://img.shields.io/nuget/v/Dapper.EntityFramework.StrongName.svg)](https://www.nuget.org/packages/Dapper.EntityFramework.StrongName/) | [![Dapper.EntityFramework.StrongName](https://img.shields.io/nuget/vpre/Dapper.EntityFramework.StrongName.svg)](https://www.nuget.org/packages/Dapper.EntityFramework.StrongName/) | [![Dapper.EntityFramework.StrongName](https://img.shields.io/nuget/dt/Dapper.EntityFramework.StrongName.svg)](https://www.nuget.org/packages/Dapper.EntityFramework.StrongName/) | [![Dapper.EntityFramework.StrongName MyGet](https://img.shields.io/myget/dapper/vpre/Dapper.EntityFramework.StrongName.svg)](https://www.myget.org/feed/dapper/package/nuget/Dapper.EntityFramework.StrongName) |
| [Dapper.Rainbow](https://www.nuget.org/packages/Dapper.Rainbow/) | [![Dapper.Rainbow](https://img.shields.io/nuget/v/Dapper.Rainbow.svg)](https://www.nuget.org/packages/Dapper.Rainbow/) | [![Dapper.Rainbow](https://img.shields.io/nuget/vpre/Dapper.Rainbow.svg)](https://www.nuget.org/packages/Dapper.Rainbow/) | [![Dapper.Rainbow](https://img.shields.io/nuget/dt/Dapper.Rainbow.svg)](https://www.nuget.org/packages/Dapper.Rainbow/) | [![Dapper.Rainbow MyGet](https://img.shields.io/myget/dapper/vpre/Dapper.Rainbow.svg)](https://www.myget.org/feed/dapper/package/nuget/Dapper.Rainbow) |
| [Dapper.SqlBuilder](https://www.nuget.org/packages/Dapper.SqlBuilder/) | [![Dapper.SqlBuilder](https://img.shields.io/nuget/v/Dapper.SqlBuilder.svg)](https://www.nuget.org/packages/Dapper.SqlBuilder/) | [![Dapper.SqlBuilder](https://img.shields.io/nuget/vpre/Dapper.SqlBuilder.svg)](https://www.nuget.org/packages/Dapper.SqlBuilder/) | [![Dapper.SqlBuilder](https://img.shields.io/nuget/dt/Dapper.SqlBuilder.svg)](https://www.nuget.org/packages/Dapper.SqlBuilder/) | [![Dapper.SqlBuilder MyGet](https://img.shields.io/myget/dapper/vpre/Dapper.SqlBuilder.svg)](https://www.myget.org/feed/dapper/package/nuget/Dapper.SqlBuilder) |
| [Dapper.StrongName](https://www.nuget.org/packages/Dapper.StrongName/) | [![Dapper.StrongName](https://img.shields.io/nuget/v/Dapper.StrongName.svg)](https://www.nuget.org/packages/Dapper.StrongName/) | [![Dapper.StrongName](https://img.shields.io/nuget/vpre/Dapper.StrongName.svg)](https://www.nuget.org/packages/Dapper.StrongName/) | [![Dapper.StrongName](https://img.shields.io/nuget/dt/Dapper.StrongName.svg)](https://www.nuget.org/packages/Dapper.StrongName/) | [![Dapper.StrongName MyGet](https://img.shields.io/myget/dapper/vpre/Dapper.StrongName.svg)](https://www.myget.org/feed/dapper/package/nuget/Dapper.StrongName) |

Features
--------
Dapper is a [NuGet library](https://www.nuget.org/packages/Dapper) that you can add in to your project that will extend your `IDbConnection` interface.

It provides 3 helpers:

Execute a query and map the results to a strongly typed List
------------------------------------------------------------

```csharp
public static IEnumerable<T> Query<T>(this IDbConnection cnn, string sql, object param = null, SqlTransaction transaction = null, bool buffered = true)
```
Example usage:

```csharp
public class Dog
{
    public int? Age { get; set; }
    public Guid Id { get; set; }
    public string Name { get; set; }
    public float? Weight { get; set; }

    public int IgnoredProperty { get { return 1; } }
}

var guid = Guid.NewGuid();
var dog = connection.Query<Dog>("select Age = @Age, Id = @Id", new { Age = (int?)null, Id = guid });

Assert.Equal(1,dog.Count());
Assert.Null(dog.First().Age);
Assert.Equal(guid, dog.First().Id);
```

Execute a query and map it to a list of dynamic objects
-------------------------------------------------------

```csharp
public static IEnumerable<dynamic> Query (this IDbConnection cnn, string sql, object param = null, SqlTransaction transaction = null, bool buffered = true)
```
This method will execute SQL and return a dynamic list.

Example usage:

```csharp
var rows = connection.Query("select 1 A, 2 B union all select 3, 4");

Assert.Equal(1, (int)rows[0].A);
Assert.Equal(2, (int)rows[0].B);
Assert.Equal(3, (int)rows[1].A);
Assert.Equal(4, (int)rows[1].B);
```

Execute a Command that returns no results
-----------------------------------------

```csharp
public static int Execute(this IDbConnection cnn, string sql, object param = null, SqlTransaction transaction = null)
```

Example usage:

```csharp
var count = connection.Execute(@"
  set nocount on
  create table #t(i int)
  set nocount off
  insert #t
  select @a a union all select @b
  set nocount on
  drop table #t", new {a=1, b=2 });
Assert.Equal(2, count);
```

Execute a Command multiple times
--------------------------------

The same signature also allows you to conveniently and efficiently execute a command multiple times (for example to bulk-load data)

Example usage:

```csharp
var count = connection.Execute(@"insert MyTable(colA, colB) values (@a, @b)",
    new[] { new { a=1, b=1 }, new { a=2, b=2 }, new { a=3, b=3 } }
  );
Assert.Equal(3, count); // 3 rows inserted: "1,1", "2,2" and "3,3"
```
This works for any parameter that implements IEnumerable<T> for some T.

Performance
-----------

A key feature of Dapper is performance. The following metrics show how long it takes to execute a `SELECT` statement against a DB (in various config, each labeled) and map the data returned to objects.

The benchmarks can be found in [Dapper.Tests.Performance](https://github.com/StackExchange/Dapper/tree/master/Dapper.Tests.Performance) (contributions welcome!) and can be run once compiled via:
```
Dapper.Tests.Performance.exe -f * --join
```
Output from the latest run is:
``` ini
BenchmarkDotNet=v0.11.1, OS=Windows 10.0.17134.254 (1803/April2018Update/Redstone4)
Intel Core i7-7700HQ CPU 2.80GHz (Kaby Lake), 1 CPU, 8 logical and 4 physical cores
Frequency=2742188 Hz, Resolution=364.6723 ns, Timer=TSC
  [Host]   : .NET Framework 4.7.2 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.3163.0
  ShortRun : .NET Framework 4.7.2 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.3163.0

```
|          ORM |                        Method |  Return |        Mean |   Gen 0 |  Gen 1 |  Gen 2 | Allocated |
|------------- |------------------------------ |-------- |------------:|--------:|-------:|-------:|----------:|
|   LINQ to DB |            &#39;First (Compiled)&#39; |    Post |    78.75 us |  0.7500 |      - |      - |   2.66 KB |
|   LINQ to DB |                      Query&lt;T&gt; |    Post |    80.38 us |  2.1250 |      - |      - |   6.87 KB |
|   Hand Coded |                    SqlCommand |    Post |    87.16 us |  2.5000 | 1.0000 | 0.2500 |  12.24 KB |
|       Dapper |  QueryFirstOrDefault&lt;dynamic&gt; | dynamic |    87.80 us |  4.3750 |      - |      - |   13.5 KB |
|     Belgrade |                 ExecuteReader |    Post |    87.85 us |  3.6250 | 0.7500 |      - |  11.27 KB |
|       Dapper |        QueryFirstOrDefault&lt;T&gt; |    Post |    91.51 us |  2.8750 | 0.8750 | 0.2500 |  13.46 KB |
|   Hand Coded |                     DataTable | dynamic |    91.74 us |  2.2500 | 0.6250 |      - |  12.45 KB |
|       Dapper |         &#39;Query&lt;T&gt; (buffered)&#39; |    Post |    94.05 us |  2.8750 | 0.8750 | 0.2500 |  13.79 KB |
|       Dapper |   &#39;Query&lt;dynamic&gt; (buffered)&#39; | dynamic |    95.25 us |  2.5000 | 1.0000 | 0.2500 |  13.87 KB |
|      Massive |             &#39;Query (dynamic)&#39; | dynamic |    96.18 us |  3.2500 | 0.8750 | 0.3750 |  14.19 KB |
|     PetaPoco |             &#39;Fetch&lt;T&gt; (Fast)&#39; |    Post |    96.57 us |  2.7500 | 0.8750 | 0.2500 |  13.65 KB |
|     PetaPoco |                      Fetch&lt;T&gt; |    Post |    97.62 us |  2.8750 | 0.8750 | 0.2500 |  14.59 KB |
|       Dapper |              &#39;Contrib Get&lt;T&gt;&#39; |    Post |    98.85 us |  2.8750 | 1.0000 | 0.2500 |  14.45 KB |
| ServiceStack |                 SingleById&lt;T&gt; |    Post |   102.39 us |  3.1250 | 0.8750 | 0.3750 |  17.52 KB |
|   LINQ to DB |                         First |    Post |   103.54 us |  1.7500 |      - |      - |   5.51 KB |
|      Susanoo |         &#39;Execute&lt;T&gt; (Static)&#39; |    Post |   105.07 us |  2.8750 | 0.8750 | 0.2500 |  14.98 KB |
|      Dashing |                           Get |    Post |   105.80 us |  3.1250 | 0.8750 | 0.3750 |  14.82 KB |
|      Susanoo |    &#39;Execut&lt;dynamic&gt; (Static)&#39; | dynamic |   109.26 us |  3.1250 | 0.8750 | 0.2500 |  14.97 KB |
|  LINQ to SQL |            &#39;First (Compiled)&#39; |    Post |   114.62 us |  3.1250 |      - |      - |   9.82 KB |
|       Dapper |       &#39;Query&lt;T&gt; (unbuffered)&#39; |    Post |   119.72 us |  3.1250 | 0.8750 | 0.2500 |  13.83 KB |
|      Susanoo |    &#39;Execute&lt;dynamic&gt; (Cache)&#39; | dynamic |   124.02 us |  3.6250 | 1.0000 | 0.5000 |   20.4 KB |
|      Susanoo |          &#39;Execute&lt;T&gt; (Cache)&#39; |    Post |   126.92 us |  4.2500 | 1.0000 | 0.5000 |  20.88 KB |
|       Dapper | &#39;Query&lt;dynamic&gt; (unbuffered)&#39; | dynamic |   139.89 us |  2.5000 | 1.0000 | 0.2500 |  13.87 KB |
|         EF 6 |                      SqlQuery |    Post |   143.86 us |  5.2500 | 0.7500 |      - |  27.86 KB |
|      EF Core |            &#39;First (Compiled)&#39; |    Post |   148.42 us |  5.0000 |      - |      - |  16.08 KB |
|   NHibernate |                        Get&lt;T&gt; |    Post |   196.88 us |  5.7500 | 1.0000 |      - |   32.5 KB |
|      EF Core |                         First |    Post |   197.91 us |  6.5000 |      - |      - |  20.25 KB |
|   NHibernate |                           HQL |    Post |   207.84 us |  6.0000 | 0.7500 |      - |     35 KB |
|      EF Core |         &#39;First (No Tracking)&#39; |    Post |   213.58 us |  4.2500 | 0.7500 | 0.2500 |  21.36 KB |
|      EF Core |                      SqlQuery |    Post |   247.25 us |  6.5000 |      - |      - |  20.56 KB |
|         EF 6 |                         First |    Post |   247.53 us | 15.5000 |      - |      - |  48.29 KB |
|   NHibernate |                      Criteria |    Post |   253.30 us | 13.2500 | 1.2500 | 0.2500 |  65.32 KB |
|         EF 6 |         &#39;First (No Tracking)&#39; |    Post |   265.80 us | 10.5000 | 1.0000 |      - |  55.09 KB |
|  LINQ to SQL |                  ExecuteQuery |    Post |   284.74 us |  7.0000 | 1.0000 | 0.5000 |  42.33 KB |
|   NHibernate |                           SQL |    Post |   313.85 us | 26.5000 | 1.0000 |      - | 101.01 KB |
|  LINQ to SQL |                         First |    Post |   968.14 us |  4.0000 | 1.0000 |      - |  14.68 KB |
|   NHibernate |                          LINQ |    Post | 1,062.16 us | 11.0000 | 2.0000 |      - |  62.37 KB |


Feel free to submit patches that include other ORMs - when running benchmarks, be sure to compile in Release and not attach a debugger (<kbd>Ctrl</kbd>+<kbd>F5</kbd>).

Alternatively, you might prefer Frans Bouma's [RawDataAccessBencher](https://github.com/FransBouma/RawDataAccessBencher) test suite or [OrmBenchmark](https://github.com/InfoTechBridge/OrmBenchmark).

Parameterized queries
---------------------

Parameters are passed in as anonymous classes. This allow you to name your parameters easily and gives you the ability to simply cut-and-paste SQL snippets and run them in your db platform's Query analyzer.

```csharp
new {A = 1, B = "b"} // A will be mapped to the param @A, B to the param @B
```

List Support
------------
Dapper allows you to pass in `IEnumerable<int>` and will automatically parameterize your query.

For example:

```csharp
connection.Query<int>("select * from (select 1 as Id union all select 2 union all select 3) as X where Id in @Ids", new { Ids = new int[] { 1, 2, 3 } });
```

Will be translated to:

```csharp
select * from (select 1 as Id union all select 2 union all select 3) as X where Id in (@Ids1, @Ids2, @Ids3)" // @Ids1 = 1 , @Ids2 = 2 , @Ids2 = 3
```

Literal replacements
------------
Dapper supports literal replacements for bool and numeric types.

```csharp
connection.Query("select * from User where UserTypeId = {=Admin}", new { UserTypeId.Admin }));
```

The literal replacement is not sent as a parameter; this allows better plans and filtered index usage but should usually be used sparingly and after testing. This feature is particularly useful when the value being injected
is actually a fixed value (for example, a fixed "category id", "status code" or "region" that is specific to the query). For *live* data where you are considering literals, you might *also* want to consider and test provider-specific query hints like [`OPTIMIZE FOR UNKNOWN`](https://blogs.msdn.microsoft.com/sqlprogrammability/2008/11/26/optimize-for-unknown-a-little-known-sql-server-2008-feature/) with regular parameters.

Buffered vs Unbuffered readers
---------------------
Dapper's default behavior is to execute your SQL and buffer the entire reader on return. This is ideal in most cases as it minimizes shared locks in the db and cuts down on db network time.

However when executing huge queries you may need to minimize memory footprint and only load objects as needed. To do so pass, `buffered: false` into the `Query` method.

Multi Mapping
---------------------
Dapper allows you to map a single row to multiple objects. This is a key feature if you want to avoid extraneous querying and eager load associations.

Example:

Consider 2 classes: `Post` and `User`

```csharp
class Post
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public User Owner { get; set; }
}

class User
{
    public int Id { get; set; }
    public string Name { get; set; }
}
```

Now let us say that we want to map a query that joins both the posts and the users table. Until now if we needed to combine the result of 2 queries, we'd need a new object to express it but it makes more sense in this case to put the `User` object inside the `Post` object.

This is the use case for multi mapping. You tell dapper that the query returns a `Post` and a `User` object and then give it a function describing what you want to do with each of the rows containing both a `Post` and a `User` object. In our case, we want to take the user object and put it inside the post object. So we write the function:

```csharp
(post, user) => { post.Owner = user; return post; }
```

The 3 type arguments to the `Query` method specify what objects dapper should use to deserialize the row and what is going to be returned. We're going to interpret both rows as a combination of `Post` and `User` and we're returning back a `Post` object. Hence the type declaration becomes

```csharp
<Post, User, Post>
```

Everything put together, looks like this:

```csharp
var sql =
@"select * from #Posts p
left join #Users u on u.Id = p.OwnerId
Order by p.Id";

var data = connection.Query<Post, User, Post>(sql, (post, user) => { post.Owner = user; return post;});
var post = data.First();

Assert.Equal("Sams Post1", post.Content);
Assert.Equal(1, post.Id);
Assert.Equal("Sam", post.Owner.Name);
Assert.Equal(99, post.Owner.Id);
```

Dapper is able to split the returned row by making an assumption that your Id columns are named `Id` or `id`. If your primary key is different or you would like to split the row at a point other than `Id`, use the optional `splitOn` parameter.

Multiple Results
---------------------
Dapper allows you to process multiple result grids in a single query.

Example:

```csharp
var sql =
@"
select * from Customers where CustomerId = @id
select * from Orders where CustomerId = @id
select * from Returns where CustomerId = @id";

using (var multi = connection.QueryMultiple(sql, new {id=selectedId}))
{
   var customer = multi.Read<Customer>().Single();
   var orders = multi.Read<Order>().ToList();
   var returns = multi.Read<Return>().ToList();
   ...
}
```

Stored Procedures
---------------------
Dapper fully supports stored procs:

```csharp
var user = cnn.Query<User>("spGetUser", new {Id = 1},
        commandType: CommandType.StoredProcedure).SingleOrDefault();
```

If you want something more fancy, you can do:

```csharp
var p = new DynamicParameters();
p.Add("@a", 11);
p.Add("@b", dbType: DbType.Int32, direction: ParameterDirection.Output);
p.Add("@c", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);

cnn.Execute("spMagicProc", p, commandType: CommandType.StoredProcedure);

int b = p.Get<int>("@b");
int c = p.Get<int>("@c");
```

Ansi Strings and varchar
---------------------
Dapper supports varchar params, if you are executing a where clause on a varchar column using a param be sure to pass it in this way:

```csharp
Query<Thing>("select * from Thing where Name = @Name", new {Name = new DbString { Value = "abcde", IsFixedLength = true, Length = 10, IsAnsi = true });
```

On SQL Server it is crucial to use the unicode when querying unicode and ANSI when querying non unicode.

Type Switching Per Row
---------------------

Usually you'll want to treat all rows from a given table as the same data type. However, there are some circumstances where it's useful to be able to parse different rows as different data types. This is where `IDataReader.GetRowParser` comes in handy.

Imagine you have a database table named "Shapes" with the columns: `Id`, `Type`, and `Data`, and you want to parse its rows into `Circle`, `Square`, or `Triangle` objects based on the value of the Type column.

```csharp
var shapes = new List<IShape>();
using (var reader = connection.ExecuteReader("select * from Shapes"))
{
    // Generate a row parser for each type you expect.
    // The generic type <IShape> is what the parser will return.
    // The argument (typeof(*)) is the concrete type to parse.
    var circleParser = reader.GetRowParser<IShape>(typeof(Circle));
    var squareParser = reader.GetRowParser<IShape>(typeof(Square));
    var triangleParser = reader.GetRowParser<IShape>(typeof(Triangle));

    var typeColumnIndex = reader.GetOrdinal("Type");

    while (reader.Read())
    {
        IShape shape;
        var type = (ShapeType)reader.GetInt32(typeColumnIndex);
        switch (type)
        {
            case ShapeType.Circle:
            	shape = circleParser(reader);
            	break;
            case ShapeType.Square:
            	shape = squareParser(reader);
            	break;
            case ShapeType.Triangle:
            	shape = triangleParser(reader);
            	break;
            default:
            	throw new NotImplementedException();
        }

      	shapes.Add(shape);
    }
}
```

User Defined Variables in MySQL
---------------------
In order to use Non-parameter SQL variables with MySql Connector, you have to add the following option to your connection string:

`Allow User Variables=True`

Make sure you don't provide Dapper with a property to map.

Limitations and caveats
---------------------
Dapper caches information about every query it runs, this allows it to materialize objects quickly and process parameters quickly. The current implementation caches this information in a `ConcurrentDictionary` object. Statements that are only used once are routinely flushed from this cache. Still, if you are generating SQL strings on the fly without using parameters it is possible you may hit memory issues.

Dapper's simplicity means that many feature that ORMs ship with are stripped out. It worries about the 95% scenario, and gives you the tools you need most of the time. It doesn't attempt to solve every problem.

Will Dapper work with my DB provider?
---------------------
Dapper has no DB specific implementation details, it works across all .NET ADO providers including [SQLite](https://www.sqlite.org/), SQL CE, Firebird, Oracle, MySQL, PostgreSQL and SQL Server.

Do you have a comprehensive list of examples?
---------------------
Dapper has a comprehensive test suite in the [test project](https://github.com/StackExchange/Dapper/tree/master/Dapper.Tests).

Who is using this?
---------------------
Dapper is in production use at [Stack Overflow](https://stackoverflow.com/).
