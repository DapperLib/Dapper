Dapper.Contrib - more extensions for dapper
===========================================

Features
--------
Dapper.Contrib contains a number of helper methods for inserting, getting, updating and deleting files.

As with dapper, all extension methods assume the connection is already open, they will fail if the 
connection is closed. The full list of extension methods in Dapper.Contrib right now are:

```csharp
T Get<T>(id);
IEnumerable<T> GetAll<T>();
int Insert<T>(T obj);
int Insert<T>(Enumerable<T> list);
bool Update<T>(T obj);
bool Update<T>(Enumerable<T> list);
bool Delete<T>(T obj);
bool Delete<T>(Enumerable<T> list);
bool DeleteAll<T>();
```

For these extensions to work, the entity in question _MUST_ have a key-property, a property named "id" or decorated with 
a [Key] attribute.

```csharp
public class Car
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class User
{
    [Key]
    int TheId { get; set; }
    string Name { get; set; }
    int Age { get; set; }
}
   
```


Gets
-------
Get one specific entity based on id, or a list of all entities in the table.

```csharp
var car = connection.Get<Car>(1);

var cars = connection.GetAll<Car>();
```

Inserts
-------
Insert one entity or a list of entities.

```csharp
connection.Insert(new Car { Name = "Volvo" });

connection.Insert(cars);
```



Updates
-------
Update one specific entity or update a list of entities.

```csharp
connection.Update(new Car() { Id = 1, Name = "Saab" });

connection.Update(cars);
```

Deletes
-------
Delete one specific entity, a list of entities, or _ALL_ entities in the table.

```csharp
connection.Delete(new Car() { Id = 1 });

connection.Delete(cars);

connection.DeleteAll<Car>();
```

Special Attributes
----------
Dapper.Contrib makes use of some optional attributes:

* Table("Tablename") - use another table name instead of the name of the class
* Key - this property is the identity/key (unless it is named "Id")
* Write(true/false) -  this property is (not) writeable
* Computed - this property is computed and should not be part of updates
