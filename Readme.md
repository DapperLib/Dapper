Dapper - a simple object mapper for .Net
========================================
[![Build status](https://ci.appveyor.com/api/projects/status/8rbgoxqio76ynj4h?svg=true)](https://ci.appveyor.com/project/StackExchange/dapper)

Release Notes
-------------
Located at [https://github.com/DapperLib/Dapper/releases](https://github.com/DapperLib/Dapper/releases/)

Packages
--------

MyGet Pre-release feed: https://www.myget.org/gallery/dapper

| Package | NuGet Stable | NuGet Pre-release | Downloads | MyGet |
| ------- | ------------ | ----------------- | --------- | ----- |
| [Dapper](https://www.nuget.org/packages/Dapper/) | [![Dapper](https://img.shields.io/nuget/v/Dapper.svg)](https://www.nuget.org/packages/Dapper/) | [![Dapper](https://img.shields.io/nuget/vpre/Dapper.svg)](https://www.nuget.org/packages/Dapper/) | [![Dapper](https://img.shields.io/nuget/dt/Dapper.svg)](https://www.nuget.org/packages/Dapper/) | [![Dapper MyGet](https://img.shields.io/myget/dapper/vpre/Dapper.svg)](https://www.myget.org/feed/dapper/package/nuget/Dapper) |
| [Dapper.EntityFramework](https://www.nuget.org/packages/Dapper.EntityFramework/) | [![Dapper.EntityFramework](https://img.shields.io/nuget/v/Dapper.EntityFramework.svg)](https://www.nuget.org/packages/Dapper.EntityFramework/) | [![Dapper.EntityFramework](https://img.shields.io/nuget/vpre/Dapper.EntityFramework.svg)](https://www.nuget.org/packages/Dapper.EntityFramework/) | [![Dapper.EntityFramework](https://img.shields.io/nuget/dt/Dapper.EntityFramework.svg)](https://www.nuget.org/packages/Dapper.EntityFramework/) | [![Dapper.EntityFramework MyGet](https://img.shields.io/myget/dapper/vpre/Dapper.EntityFramework.svg)](https://www.myget.org/feed/dapper/package/nuget/Dapper.EntityFramework) |
| [Dapper.EntityFramework.StrongName](https://www.nuget.org/packages/Dapper.EntityFramework.StrongName/) | [![Dapper.EntityFramework.StrongName](https://img.shields.io/nuget/v/Dapper.EntityFramework.StrongName.svg)](https://www.nuget.org/packages/Dapper.EntityFramework.StrongName/) | [![Dapper.EntityFramework.StrongName](https://img.shields.io/nuget/vpre/Dapper.EntityFramework.StrongName.svg)](https://www.nuget.org/packages/Dapper.EntityFramework.StrongName/) | [![Dapper.EntityFramework.StrongName](https://img.shields.io/nuget/dt/Dapper.EntityFramework.StrongName.svg)](https://www.nuget.org/packages/Dapper.EntityFramework.StrongName/) | [![Dapper.EntityFramework.StrongName MyGet](https://img.shields.io/myget/dapper/vpre/Dapper.EntityFramework.StrongName.svg)](https://www.myget.org/feed/dapper/package/nuget/Dapper.EntityFramework.StrongName) |
| [Dapper.Rainbow](https://www.nuget.org/packages/Dapper.Rainbow/) | [![Dapper.Rainbow](https://img.shields.io/nuget/v/Dapper.Rainbow.svg)](https://www.nuget.org/packages/Dapper.Rainbow/) | [![Dapper.Rainbow](https://img.shields.io/nuget/vpre/Dapper.Rainbow.svg)](https://www.nuget.org/packages/Dapper.Rainbow/) | [![Dapper.Rainbow](https://img.shields.io/nuget/dt/Dapper.Rainbow.svg)](https://www.nuget.org/packages/Dapper.Rainbow/) | [![Dapper.Rainbow MyGet](https://img.shields.io/myget/dapper/vpre/Dapper.Rainbow.svg)](https://www.myget.org/feed/dapper/package/nuget/Dapper.Rainbow) |
| [Dapper.SqlBuilder](https://www.nuget.org/packages/Dapper.SqlBuilder/) | [![Dapper.SqlBuilder](https://img.shields.io/nuget/v/Dapper.SqlBuilder.svg)](https://www.nuget.org/packages/Dapper.SqlBuilder/) | [![Dapper.SqlBuilder](https://img.shields.io/nuget/vpre/Dapper.SqlBuilder.svg)](https://www.nuget.org/packages/Dapper.SqlBuilder/) | [![Dapper.SqlBuilder](https://img.shields.io/nuget/dt/Dapper.SqlBuilder.svg)](https://www.nuget.org/packages/Dapper.SqlBuilder/) | [![Dapper.SqlBuilder MyGet](https://img.shields.io/myget/dapper/vpre/Dapper.SqlBuilder.svg)](https://www.myget.org/feed/dapper/package/nuget/Dapper.SqlBuilder) |
| [Dapper.StrongName](https://www.nuget.org/packages/Dapper.StrongName/) | [![Dapper.StrongName](https://img.shields.io/nuget/v/Dapper.StrongName.svg)](https://www.nuget.org/packages/Dapper.StrongName/) | [![Dapper.StrongName](https://img.shields.io/nuget/vpre/Dapper.StrongName.svg)](https://www.nuget.org/packages/Dapper.StrongName/) | [![Dapper.StrongName](https://img.shields.io/nuget/dt/Dapper.StrongName.svg)](https://www.nuget.org/packages/Dapper.StrongName/) | [![Dapper.StrongName MyGet](https://img.shields.io/myget/dapper/vpre/Dapper.StrongName.svg)](https://www.myget.org/feed/dapper/package/nuget/Dapper.StrongName) |

Package Purposes:
* Dapper
  * The core library
* Dapper.EntityFramework
  * Extension handlers for EntityFramework
* Dapper.EntityFramework.StrongName
  * Extension handlers for EntityFramework
* Dapper.Rainbow
  * Micro-ORM implemented on Dapper, provides CRUD helpers
* Dapper.SqlBuilder
  * Component for building SQL queries dynamically and composably

Sponsors
--------

Dapper was originally developed for and by Stack Overflow, but is F/OSS. Sponsorship is welcome and invited - see the sponsor link at the top of the page.
A huge thanks to everyone (individuals or organisations) who have sponsored Dapper, but a massive thanks in particular to:

- [AWS](https://github.com/aws) who sponsored Dapper from Oct 2023 via the [.NET on AWS Open Source Software Fund](https://github.com/aws/dotnet-foss)

Features
--------
Dapper is a [NuGet library](https://www.nuget.org/packages/Dapper) that you can add in to your project that will enhance your ADO.NET connections via
extension methods on your `DbConnection` instance. This provides a simple and efficient API for invoking SQL, with support for both synchronous and
asynchronous data access, and allows both buffered and non-buffered queries.

It provides multiple helpers, but the key APIs are:

``` csharp
// insert/update/delete etc
var count  = connection.Execute(sql [, args]);

// multi-row query
IEnumerable<T> rows = connection.Query<T>(sql [, args]);

// single-row query ({Single|First}[OrDefault])
T row = connection.QuerySingle<T>(sql [, args]);
```

where `args` can be (among other things):

- a simple POCO (including anonyomous types) for named parameters
- a `Dictionary<string,object>`
- a `DynamicParameters` instance

Execute a query and map it to a list of typed objects
-------------------------------------------------------

``` csharp
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

This method will execute SQL and return a dynamic list.

Example usage:

```csharp
var rows = connection.Query("select 1 A, 2 B union all select 3, 4").AsList();

Assert.Equal(1, (int)rows[0].A);
Assert.Equal(2, (int)rows[0].B);
Assert.Equal(3, (int)rows[1].A);
Assert.Equal(4, (int)rows[1].B);
```

Execute a Command that returns no results
-----------------------------------------

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

Another example usage when you _already_ have an existing collection:
```csharp
var foos = new List<Foo>
{
    { new Foo { A = 1, B = 1 } }
    { new Foo { A = 2, B = 2 } }
    { new Foo { A = 3, B = 3 } }
};

var count = connection.Execute(@"insert MyTable(colA, colB) values (@a, @b)", foos);
Assert.Equal(foos.Count, count);
```

This works for any parameter that implements `IEnumerable<T>` for some T.

Performance
-----------

A key feature of Dapper is performance. The following metrics show how long it takes to execute a `SELECT` statement against a DB (in various config, each labeled) and map the data returned to objects.

The benchmarks can be found in [Dapper.Tests.Performance](https://github.com/DapperLib/Dapper/tree/main/benchmarks/Dapper.Tests.Performance) (contributions welcome!) and can be run via:
```bash
dotnet run --project .\benchmarks\Dapper.Tests.Performance\ -c Release -f net8.0 -- -f * --join
```
Output from the latest run is:
``` ini
BenchmarkDotNet v0.13.7, Windows 10 (10.0.19045.3693/22H2/2022Update)
Intel Core i7-3630QM CPU 2.40GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
.NET SDK 8.0.100
  [Host]   : .NET 8.0.0 (8.0.23.53103), X64 RyuJIT AVX
  ShortRun : .NET 8.0.0 (8.0.23.53103), X64 RyuJIT AVX

```
|                 ORM |                         Method |       Return |     Mean |    StdDev |     Error |    Gen0 |   Gen1 |   Gen2 | Allocated |
|-------------------- |------------------------------- |------------- |---------:|----------:|----------:|--------:|-------:|-------:|----------:|
| Dapper cache impact |     QueryFirstParameters_Cache |         Void | 104.0 us |   0.64 us |   0.97 us |  0.8750 |      - |      - |    2824 B |
| Dapper cache impact |        ExecuteParameters_Cache |         Void | 106.6 us |   1.24 us |   2.08 us |  0.6250 |      - |      - |    2184 B |
|          Hand Coded |                     SqlCommand |         Post | 129.1 us |   0.60 us |   0.91 us |  1.2500 | 1.0000 |      - |    7584 B |
|          Hand Coded |                      DataTable |      dynamic | 133.4 us |   1.55 us |   2.34 us |  3.0000 |      - |      - |    9576 B |
|          SqlMarshal |                     SqlCommand |         Post | 141.9 us |   2.56 us |   3.88 us |  1.7500 | 1.5000 |      - |   11529 B |
|          LINQ to DB |                       Query<T> |         Post | 144.3 us |   2.14 us |   3.24 us |  1.7500 | 1.2500 |      - |   10904 B |
|              Mighty |             SingleFromQuery<T> |         Post | 144.5 us |   1.30 us |   2.19 us |  2.2500 | 1.2500 |      - |   12201 B |
|              Mighty |       SingleFromQuery<dynamic> |      dynamic | 145.5 us |   2.50 us |   3.79 us |  2.0000 | 1.7500 |      - |   12710 B |
|              Mighty |                 Query<dynamic> |      dynamic | 146.3 us |   1.38 us |   2.32 us |  2.0000 | 1.7500 |      - |   12710 B |
|              Dapper |    'Query<dynamic> (buffered)' |      dynamic | 148.4 us |   1.19 us |   2.00 us |  2.0000 | 1.5000 |      - |   11968 B |
|              Mighty |                       Query<T> |         Post | 149.1 us |   2.63 us |   3.98 us |  1.7500 | 1.5000 |      - |   12201 B |
|          LINQ to DB |             'First (Compiled)' |         Post | 150.1 us |   1.97 us |   3.31 us |  2.0000 | 1.5000 |      - |   12128 B |
|              Dapper |   QueryFirstOrDefault<dynamic> |      dynamic | 151.0 us |   5.48 us |   8.29 us |  3.5000 |      - |      - |   11648 B |
|        ServiceStack |                  SingleById<T> |         Post | 154.1 us |   2.24 us |   3.38 us |  2.5000 | 1.2500 | 0.2500 |   15248 B |
|              Dapper |         QueryFirstOrDefault<T> |         Post | 154.9 us |   4.10 us |   6.20 us |  1.7500 | 1.5000 |      - |   11608 B |
|             Massive |              'Query (dynamic)' |      dynamic | 156.9 us |   4.05 us |   6.12 us |  2.0000 | 1.5000 |      - |   12342 B |
|                Norm |              'Read<> (tuples)' | ValueTuple`8 | 157.2 us |   3.16 us |   5.31 us |  2.0000 | 1.7500 |      - |   12745 B |
|              Dapper |               'Contrib Get<T>' |         Post | 157.9 us |   3.14 us |   4.75 us |  2.0000 | 1.5000 |      - |   12440 B |
|              Dapper |          'Query<T> (buffered)' |         Post | 158.0 us |   4.28 us |   6.47 us |  2.0000 | 1.0000 |      - |   11888 B |
|                Norm |      'Read<()> (named tuples)' | ValueTuple`8 | 163.3 us |   1.03 us |   1.56 us |  2.2500 | 2.0000 | 1.2500 |   14562 B |
| Dapper cache impact |      ExecuteNoParameters_Cache |         Void | 177.0 us |   6.37 us |   9.64 us |       - |      - |      - |     760 B |
| Dapper cache impact | QueryFirstNoParameters_NoCache |         Void | 177.6 us |   2.62 us |   3.96 us |  0.5000 |      - |      - |    1576 B |
|      DevExpress.XPO |                  FindObject<T> |         Post | 182.0 us |   3.41 us |   5.73 us |  8.5000 |      - |      - |   28099 B |
|          LINQ to DB |                          First |         Post | 183.3 us |   5.28 us |   7.98 us |  2.5000 | 1.5000 |      - |   14041 B |
|                EF 6 |                       SqlQuery |         Post | 185.9 us |   3.08 us |   4.66 us |  4.5000 | 0.5000 |      - |   24210 B |
| Dapper cache impact |    ExecuteNoParameters_NoCache |         Void | 187.5 us |   3.17 us |   4.80 us |       - |      - |      - |     760 B |
| Dapper cache impact |   QueryFirstNoParameters_Cache |         Void | 187.6 us |   7.54 us |  11.40 us |       - |      - |      - |    1520 B |
|      DevExpress.XPO |              GetObjectByKey<T> |         Post | 188.0 us |   2.66 us |   4.46 us |  4.5000 | 1.0000 |      - |   30114 B |
|                Norm |               'Read<> (class)' |         Post | 199.9 us |   1.98 us |   3.79 us |  3.0000 | 0.5000 |      - |   17579 B |
|              Dapper |  'Query<dynamic> (unbuffered)' |      dynamic | 204.3 us |   2.79 us |   4.22 us |  1.5000 | 1.0000 |      - |   12048 B |
|      DevExpress.XPO |                       Query<T> |         Post | 213.9 us |   7.82 us |  13.15 us | 10.0000 |      - |      - |   32083 B |
|              Dapper |        'Query<T> (unbuffered)' |         Post | 217.6 us |   5.62 us |   8.50 us |  1.5000 | 1.0000 |      - |   12008 B |
|             EF Core |             'First (Compiled)' |         Post | 228.1 us |   5.69 us |   8.60 us |  2.0000 |      - |      - |    7521 B |
|          NHibernate |                            HQL |         Post | 239.4 us |   6.76 us |  10.21 us |  6.5000 | 0.5000 |      - |   31885 B |
|          NHibernate |                         Get<T> |         Post | 247.1 us |   6.55 us |   9.91 us |  6.0000 | 0.5000 |      - |   29885 B |
|                EF 6 |                          First |         Post | 297.6 us |  28.83 us |  48.45 us | 13.0000 |      - |      - |   43309 B |
|          NHibernate |                       Criteria |         Post | 300.1 us |  13.37 us |  25.57 us | 14.0000 | 1.0000 |      - |   57562 B |
|             EF Core |          'First (No Tracking)' |         Post | 314.2 us |  43.80 us |  73.60 us |  3.0000 | 1.0000 |      - |   17987 B |
|             EF Core |                          First |         Post | 319.8 us |  32.72 us |  54.99 us |  3.0000 |      - |      - |   11306 B |
|                EF 6 |          'First (No Tracking)' |         Post | 327.2 us |  33.05 us |  55.54 us | 12.0000 | 1.0000 |      - |   50237 B |
|          NHibernate |                            SQL |         Post | 327.7 us |  16.66 us |  28.00 us | 21.0000 | 1.0000 |      - |   80007 B |
|             EF Core |                       SqlQuery |         Post | 334.2 us |  33.86 us |  56.90 us |  5.0000 |      - |      - |   18195 B |
|            Belgrade |                 FirstOrDefault |       Task`1 | 384.4 us |  20.30 us |  34.11 us |  4.0000 | 1.0000 |      - |   20556 B |
| Dapper cache impact |   QueryFirstParameters_NoCache |         Void | 677.3 us |   5.55 us |   8.40 us |  3.0000 | 2.0000 |      - |   10640 B |
|          NHibernate |                           LINQ |         Post | 936.0 us | 168.35 us | 254.52 us | 10.0000 |      - |      - |   46181 B |
| Dapper cache impact |      ExecuteParameters_NoCache |         Void | 986.8 us | 198.13 us | 299.55 us |  3.0000 | 2.0000 |      - |   10001 B |

Feel free to submit patches that include other ORMs - when running benchmarks, be sure to compile in Release and not attach a debugger (<kbd>Ctrl</kbd>+<kbd>F5</kbd>).

Alternatively, you might prefer Frans Bouma's [RawDataAccessBencher](https://github.com/FransBouma/RawDataAccessBencher) test suite or [OrmBenchmark](https://github.com/InfoTechBridge/OrmBenchmark).

Parameterized queries
---------------------

Parameters are usually passed in as anonymous classes. This allows you to name your parameters easily and gives you the ability to simply cut-and-paste SQL snippets and run them in your db platform's Query analyzer.

```csharp
new {A = 1, B = "b"} // A will be mapped to the param @A, B to the param @B
```
Parameters can also be built up dynamically using the DynamicParameters class. This allows for building a dynamic SQL statement while still using parameters for safety and performance.

```csharp
    var sqlPredicates = new List<string>();
    var queryParams = new DynamicParameters();
    if (boolExpression)
    {
        sqlPredicates.Add("column1 = @param1");
        queryParams.Add("param1", dynamicValue1, System.Data.DbType.Guid);
    } else {
        sqlPredicates.Add("column2 = @param2");
        queryParams.Add("param2", dynamicValue2, System.Data.DbType.String);
    }
```

DynamicParameters also supports copying multiple parameters from existing objects of different types.
    
```csharp
    var queryParams = new DynamicParameters(objectOfType1);
    queryParams.AddDynamicParams(objectOfType2);
```
    
When an object that implements the `IDynamicParameters` interface passed into `Execute` or `Query` functions, parameter values will be extracted via this interface. Obviously, the most likely object class to use for this purpose would be the built-in `DynamicParameters` class.
    
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
connection.Query("select * from User where UserTypeId = {=Admin}", new { UserTypeId.Admin });
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
Query<Thing>("select * from Thing where Name = @Name", new {Name = new DbString { Value = "abcde", IsFixedLength = true, Length = 10, IsAnsi = true }});
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

Dapper's simplicity means that many features that ORMs ship with are stripped out. It worries about the 95% scenario, and gives you the tools you need most of the time. It doesn't attempt to solve every problem.

Will Dapper work with my DB provider?
---------------------
Dapper has no DB specific implementation details, it works across all .NET ADO providers including [SQLite](https://www.sqlite.org/), SQL CE, Firebird, Oracle, MySQL, PostgreSQL and SQL Server.

Do you have a comprehensive list of examples?
---------------------
Dapper has a comprehensive test suite in the [test project](https://github.com/DapperLib/Dapper/tree/main/tests/Dapper.Tests).

Who is using this?
---------------------
Dapper is in production use at [Stack Overflow](https://stackoverflow.com/).
