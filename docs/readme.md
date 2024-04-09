# Dapper

Dapper is a simple micro-ORM used to simplify working with ADO.NET; if you like SQL but dislike the boilerplate of ADO.NET: Dapper is for you!

As a simple example:

``` c#
string region = ...
var customers = connection.Query<Customer>(
    "select * from Customers where Region = @region", // SQL
    new { region } // parameters
    ).AsList();
```

But all the execute/single-row/scalar/async/etc functionality you would expect: is there as extension methods on your `DbConnection`.

See [GitHub](https://github.com/DapperLib/Dapper) for more information and examples.