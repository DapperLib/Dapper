Dapper - a simple object mapper for .Net
========================================
[![Build status](https://ci.appveyor.com/api/projects/status/8rbgoxqio76ynj4h?svg=true)](https://ci.appveyor.com/project/StackExchange/dapper)

Release Notes
-------------

[Located at stackexchange.github.io/Dapper](https://stackexchange.github.io/Dapper/)


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
            
dog.Count()
    .IsEqualTo(1);

dog.First().Age
    .IsNull();

dog.First().Id
    .IsEqualTo(guid);
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

((int)rows[0].A)
   .IsEqualTo(1);

((int)rows[0].B)
   .IsEqualTo(2);

((int)rows[1].A)
   .IsEqualTo(3);

((int)rows[1].B)
    .IsEqualTo(4);
```

Execute a Command that returns no results
-----------------------------------------

```csharp
public static int Execute(this IDbConnection cnn, string sql, object param = null, SqlTransaction transaction = null)
```

Example usage:

```csharp
connection.Execute(@"
  set nocount on 
  create table #t(i int) 
  set nocount off 
  insert #t 
  select @a a union all select @b 
  set nocount on 
  drop table #t", new {a=1, b=2 })
   .IsEqualTo(2);
```

Execute a Command multiple times
--------------------------------

The same signature also allows you to conveniently and efficiently execute a command multiple times (for example to bulk-load data)

Example usage:

```csharp
connection.Execute(@"insert MyTable(colA, colB) values (@a, @b)",
    new[] { new { a=1, b=1 }, new { a=2, b=2 }, new { a=3, b=3 } }
  ).IsEqualTo(3); // 3 rows inserted: "1,1", "2,2" and "3,3"
```
This works for any parameter that implements IEnumerable<T> for some T.

Performance
-----------

A key feature of Dapper is performance. The following metrics show how long it takes to execute 500 SELECT statements against a DB and map the data returned to objects.

The performance tests are broken in to 3 lists:

- POCO serialization for frameworks that support pulling static typed objects from the DB. Using raw SQL.
- Dynamic serialization for frameworks that support returning dynamic lists of objects.
- Typical framework usage. Often typical framework usage differs from the optimal usage performance wise. Often it will not involve writing SQL.

### Performance of SELECT mapping over 500 iterations - POCO serialization

<table>
  <tr>
  	<th>Method</th>
		<th>Duration</th>		
		<th>Remarks</th>
	</tr>
	<tr>
		<td>Hand coded (using a <code>SqlDataReader</code>)</td>
		<td>47ms</td>
		<td rowspan="9"><a href="http://www.toptensoftware.com/blog/posts/94-PetaPoco-More-Speed">Can be faster</a></td>
	</tr>
	<tr>
		<td>Dapper <code>ExecuteMapperQuery<Post></code></td>
		<td>49ms</td>
	</tr>
	<tr>
		<td><a href="https://github.com/ServiceStack/ServiceStack.OrmLite">ServiceStack.OrmLite</a> (QueryById)</td>
		<td>50ms</td>
	</tr>
	<tr>
		<td><a href="http://www.toptensoftware.com/petapoco/">PetaPoco</a></td>
		<td>52ms</td>
	</tr>
	<tr>
		<td>BLToolkit</td>
		<td>80ms</td>
	</tr>
	<tr>
		<td>SubSonic CodingHorror</td>
		<td>107ms</td>
	</tr>
	<tr>
		<td>NHibernate SQL</td>
		<td>104ms</td>
	</tr>
	<tr>
		<td>Linq 2 SQL <code>ExecuteQuery</code></td>
		<td>181ms</td>
	</tr>
	<tr>
		<td>Entity framework <code>ExecuteStoreQuery</code></td>
		<td>631ms</td>
	</tr>
</table>

### Performance of SELECT mapping over 500 iterations - dynamic serialization

<table>
	<tr>
		<th>Method</th>
		<th>Duration</th>		
		<th>Remarks</th>
	</tr>
	<tr>
		<td>Dapper <code>ExecuteMapperQuery</code> (dynamic)</td>
		<td>48ms</td>
		<td rowspan="3">&nbsp;</td>
	</tr>
	<tr>
		<td><a href="https://github.com/FransBouma/Massive">Massive</a></td>
		<td>52ms</td>
	</tr>
	<tr>
		<td><a href="https://github.com/markrendle/Simple.Data">Simple.Data</a></td>
		<td>95ms</td>
	</tr>
</table>


### Performance of SELECT mapping over 500 iterations - typical usage

<table>
	<tr>
		<th>Method</th>
		<th>Duration</th>		
		<th>Remarks</th>
	</tr>
	<tr>
		<td>Linq 2 SQL CompiledQuery</td>
		<td>81ms</td>
		<td>Not super typical involves complex code</td>
	</tr>
	<tr>
		<td>NHibernate HQL</td>
		<td>118ms</td>
		<td>&nbsp;</td>
	</tr>
	<tr>
		<td>Linq 2 SQL</td>
		<td>559ms</td>
		<td>&nbsp;</td>
	</tr>
	<tr>
		<td>Entity framework</td>
		<td>859ms</td>
		<td>&nbsp;</td>
	</tr>
	<tr>
		<td>SubSonic ActiveRecord.SingleOrDefault</td>
		<td>3619ms</td>
		<td>&nbsp;</td>
	</tr>
</table>

Performance benchmarks are available [here](https://github.com/StackExchange/dapper-dot-net/blob/master/Dapper.Tests.Performance/PerformanceTests.cs).

Feel free to submit patches that include other ORMs - when running benchmarks, be sure to compile in Release and not attach a debugger (ctrl F5).

Alternatively, you might prefer Frans Bouma's [RawDataAccessBencher](https://github.com/FransBouma/RawDataAccessBencher) test suite or [OrmBenchmark](https://github.com/InfoTechBridge/OrmBenchmark).

Parameterized queries
---------------------

Parameters are passed in as anonymous classes. This allow you to name your parameters easily and gives you the ability to simply cut-and-paste SQL snippets and run them in Query analyzer.

```csharp
new {A = 1, B = "b"} // A will be mapped to the param @A, B to the param @B 
```

List Support
------------
Dapper allows you to pass in IEnumerable<int> and will automatically parameterize your query.

For example:

```csharp
connection.Query<int>("select * from (select 1 as Id union all select 2 union all select 3) as X where Id in @Ids", new { Ids = new int[] { 1, 2, 3 } });
```

Will be translated to:

```csharp
select * from (select 1 as Id union all select 2 union all select 3) as X where Id in (@Ids1, @Ids2, @Ids3)" // @Ids1 = 1 , @Ids2 = 2 , @Ids2 = 3
```

Buffered vs Unbuffered readers
---------------------
Dapper's default behavior is to execute your sql and buffer the entire reader on return. This is ideal in most cases as it minimizes shared locks in the db and cuts down on db network time.

However when executing huge queries you may need to minimize memory footprint and only load objects as needed. To do so pass, buffered: false into the Query method.

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

This is the user case for multi mapping. You tell dapper that the query returns a `Post` and a `User` object and then give it a function describing what you want to do with each of the rows containing both a `Post` and a `User` object. In our case, we want to take the user object and put it inside the post object. So we write the function: 

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
 
post.Content.IsEqualTo("Sams Post1");
post.Id.IsEqualTo(1);
post.Owner.Name.IsEqualTo("Sam");
post.Owner.Id.IsEqualTo(99);
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

On SQL Server it is crucial to use the unicode when querying unicode and ansi when querying non unicode.

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

Limitations and caveats
---------------------
Dapper caches information about every query it runs, this allow it to materialize objects quickly and process parameters quickly. The current implementation caches this information in a ConcurrentDictionary object. The objects it stores are never flushed. If you are generating SQL strings on the fly without using parameters it is possible you will hit memory issues. We may convert the dictionaries to an LRU Cache.

Dapper's simplicity means that many feature that ORMs ship with are stripped out. It worries  about the 95% scenario, and gives you the tools you need most of the time. It doesn't attempt to solve every problem.

Will Dapper work with my DB provider?
---------------------
Dapper has no DB specific implementation details, it works across all .NET ADO providers including [SQLite](http://www.sqlite.org/), SQL CE, Firebird, Oracle, MySQL, PostgreSQL and SQL Server.

Do you have a comprehensive list of examples?
---------------------
Dapper has a comprehensive test suite in the [test project](https://github.com/StackExchange/dapper-dot-net/blob/master/Dapper.Tests)

Who is using this?
---------------------
Dapper is in production use at:

[Stack Overflow](https://stackoverflow.com/), [helpdesk](https://www.jitbit.com/web-helpdesk/)

(if you would like to be listed here let me know)
