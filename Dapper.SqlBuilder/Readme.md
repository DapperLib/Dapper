Dapper.SqlBuilder - a simple sql formatter for .Net
========================================
[![Build status](https://ci.appveyor.com/api/projects/status/1w448i6nfxd14w75?svg=true)](https://ci.appveyor.com/project/StackExchange/dapper-SqlBuilder)

Packages
--------

MyGet Pre-release feed: https://www.myget.org/gallery/dapper

| Package                                                      | NuGet Stable                                                 | NuGet Pre-release                                            | Downloads                                                    | MyGet                                                        |
| ------------------------------------------------------------ | ------------------------------------------------------------ | ------------------------------------------------------------ | ------------------------------------------------------------ | ------------------------------------------------------------ |
| [Dapper.SqlBuilder](https://www.nuget.org/packages/Dapper.SqlBuilder/) | [![Dapper.SqlBuilder](https://img.shields.io/nuget/v/Dapper.SqlBuilder.svg)](https://www.nuget.org/packages/Dapper.SqlBuilder/) | [![Dapper.SqlBuilder](https://img.shields.io/nuget/vpre/Dapper.SqlBuilder.svg)](https://www.nuget.org/packages/Dapper.SqlBuilder/) | [![Dapper.SqlBuilder](https://img.shields.io/nuget/dt/Dapper.SqlBuilder.svg)](https://www.nuget.org/packages/Dapper.SqlBuilder/) | [![Dapper.SqlBuilder MyGet](https://img.shields.io/myget/dapper/vpre/Dapper.SqlBuilder.svg)](https://www.myget.org/feed/dapper/package/nuget/Dapper.SqlBuilder) |

Features
--------

Dapper.SqlBuilder contains a number of helper methods for generating sql. 

The list of extension methods in Dapper.SqlBuilder right now are:

```csharp
SqlBuilder AddParameters(dynamic parameters);
SqlBuilder Select(string sql, dynamic parameters = null);
SqlBuilder Where(string sql, dynamic parameters = null);
SqlBuilder OrWhere(string sql, dynamic parameters = null);
SqlBuilder OrderBy(string sql, dynamic parameters = null);
SqlBuilder GroupBy(string sql, dynamic parameters = null);
SqlBuilder Having(string sql, dynamic parameters = null);
SqlBuilder Set(string sql, dynamic parameters = null);
SqlBuilder Join(string sql, dynamic parameters = null);
SqlBuilder InnerJoin(string sql, dynamic parameters = null);
SqlBuilder LeftJoin(string sql, dynamic parameters = null);
SqlBuilder RightJoin(string sql, dynamic parameters = null);
SqlBuilder Intersect(string sql, dynamic parameters = null);
```


Template
--------

SqlBuilder allows you to generate N SQL templates from a composed query, it can easily format sql when you are attaching parameters and how, e.g:  
```csharp
var builder = new SqlBuilder()
    .Where("a = @a", new { a = 1 })
    .Where("b = @b", new { b = 2 })
    .OrderBy("a")
    .OrderBy("b");
var counter = builder.AddTemplate("select count(*) from table /**where**/");
var selector = builder.AddTemplate("select * from table /**where**/ /**orderby**/");
var count = cnn.Query(counter.RawSql, counter.Parameters).Single();
var rows = cnn.Query(selector.RawSql, selector.Parameters);
```

it's same as 
```csharp
var count = cnn.Query("select count(*) from table where a = @a and b = @b", new { a = 1, b = 1 });
var rows = cnn.Query("select * from table where a = @a and b = @b order by a, b", new { a = 1, b = 1 });
```

Dynamic Filter Paging Example
----------

```csharp
var builder = new SqlBuilder();
var selectTemplate = builder.AddTemplate(@"select X.* from (
        select us.*, ROW_NUMBER() OVER (/**orderby**/) AS RowNumber 
        from Users us 
        /**where**/
    ) as X 
    where RowNumber between @start and @finish", new { start, finish });
var countTemplate = builder.AddTemplate(@"select count(*) from Users /**where**/");

if (userId.HasValue())
    builder.Where($"t.userId = @{nameof(userId)}", new { userId });
if (isCancel)
    builder.Where($"t.isCancel = @{nameof(isCancel)}", new { isCancel });

builder.OrderBy(string.Format("t.id {0}", orderDesc ? "desc" : "asc"));

var users = conn.Query<User>(selectTemplate.RawSql, selectTemplate.Parameters);
var count = conn.ExecuteScalar<int>(countTemplate.RawSql, countTemplate.Parameters);
//..etc..
```

Limitations and caveats
--------

OrWhere use `and` not `or` to concat sql problem

[Issue 647](https://github.com/DapperLib/Dapper/issues/647) 

```csharp
sql.Where("a = @a1");
sql.OrWhere("b = @b1");
sql.Where("a = @a2");
sql.OrWhere("b = @b2");
```

SqlBuilder will generate sql
```sql=
a = @a1 AND b = @b1 AND a = @a2 AND b = @b2
```

not
```sql
a = @a1 OR b = @b1 AND a = @a2 OR b = @b2
```
