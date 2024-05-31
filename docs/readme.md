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

Sponsors
--------

Dapper was originally developed for and by Stack Overflow, but is F/OSS. Sponsorship is welcome and invited - see the sponsor link at the top of the page.
A huge thanks to everyone (individuals or organisations) who have sponsored Dapper, but a massive thanks in particular to:

- [Dapper Plus](https://dapper-plus.net/) is a major sponsor and is proud to contribute to the development of Dapper ([read more](https://dapperlib.github.io/Dapper/dapperplus))
- [AWS](https://github.com/aws) who sponsored Dapper from Oct 2023 via the [.NET on AWS Open Source Software Fund](https://github.com/aws/dotnet-foss)

[![Dapper Plus logo](https://raw.githubusercontent.com/DapperLib/Dapper/main/docs/dapper-sponsor.png)](https://dapper-plus.net/)
