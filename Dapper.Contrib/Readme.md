Dapper.Contrib - more extensions for dapper
===========================================

Features
--------
Dapper.Contrib contains a number of helper methods for inserting, getting, updating and deleting files.

The object you are working with must have a property named Id or a property marked with a [Key] attribute. As with dapper,
all extension methods assume the connection is already open, they will fail if the connection is closed.

Inserts
-------
```csharp
public static long Insert<T>(this IDbConnection connection, T entityToInsert, IDbTransaction transaction = null, int? commandTimeout = null)
```

```csharp
public class Car
{
    public int Id { get; set; }
    public string Name { get; set; }
}
    
connection.Insert(new Car { Name = "Volvo" });
```

Gets
-------
```csharp
public static T Get<T>(this IDbConnection connection, dynamic id, IDbTransaction transaction = null, int? commandTimeout = null)
```

```csharp
var car = connection.Get<Car>(1);
```

Updates
-------
```csharp
public static bool Update<T>(this IDbConnection connection, T entityToUpdate, IDbTransaction transaction = null, int? commandTimeout = null) 
```

```csharp
connection.Update(new Car() { Id = 1, Name = "Saab" });
```

Deletes
-------
```csharp
public static bool Delete<T>(this IDbConnection connection, T entityToDelete, IDbTransaction transaction = null, int? commandTimeout = null)
public static bool DeleteAll<T>(this IDbConnection connection, IDbTransaction transaction = null, int? commandTimeout = null)
```

```csharp
connection.Delete(new Car() { Id = 1 });
connection.DeleteAll<Car>();
```

Attributes
----------
Dapper.Contrib makes use of some optional attributes:

* Table("Tablename") - use another table name instead of the name of the class
* Key - this property is the identity/key (unless it is named "Id")
* Write(true/false) -  this property is (not) writeable
* Computed - this property is computed and should not be part of updates
