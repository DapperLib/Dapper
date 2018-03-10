Dapper.Contrib - DbManager for dapper
===========================================

Features
--------
DbManager is a light wrapped class that mixed up the part of coding experience as DbContext in EntityFramework and mapper-instance in MyBatis. It also contains a few number of helper methods for inserting, getting, updating and deleting records.

The full list of extension methods in Dapper.Contrib right now are:

```csharp
T Get<T>(id);
void Insert<T>(T obj);
void Insert<T>(Enumerable<T> list);
void Update<T>(T obj);
void Update<T>(Enumerable<T> list);
void Delete<T>(T obj);
void Delete<T>(Enumerable<T> list);
void SaveChanges();
T GetMapper<T>() where T : ISqlOperationMapper
```

For these extensions to work, the entity in question _MUST_ have a
key property. Dapper will automatically use a property named "`id`" 
(case-insensitive) as the key property, if one is present.

```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
}
```

`DbManager` construction method
-------
```csharp
var dbManager = new DbManager(() => { return new MySqlConnection(conStr); });
```

`Get` methods
-------

Get one specific entity based on id

```csharp
var car = dbManager.Get<User>(1);
``` 

`Insert` methods
-------

Insert one entity

```csharp
dbManager.Insert(new User { Name = "SuperMan", Age = 27 });
dbManager.SaveChanges();
```

or a list of entities.

```csharp
dbManager.Insert(users);
dbManager.SaveChanges();
```

> The update, delete methods also code as above...


Mapper Instances
-------

Define an interface inherited ISqlOperationMapper.

```csharp
public interface IUserMapper : ISqlOperationMapper
{
    [QuerySql("select name from user where age > @age")]
    string[] GetUserNames(int age);

    [ExcuteSql("update user set name = @name where age > @age")]
    int UpdateUsers(string name, int age);
}
```

Then you can get a mapper and invoke all its method that had any specific attribute.

```csharp
var userMapper = dbManager.GetMapper<IUserMapper>();
var names = userMapper.GetUserNames(21);
int count = userMapper.UpdateUsers("MapperUpdatedName", 25);
```



Special Attributes
----------
ISqlOperationMapper makes use of two optional attributes:

* `[QuerySql("")]` - use specific sql statement to query

* `[ExcuteSql("")]` - use specific sql statement to excute
    
```csharp
public interface IUserMapper : ISqlOperationMapper
{
    [QuerySql("select name from user where age>@age")]
    string[] GetUserNames(int age);

    [ExcuteSql("update user set name=@name where age>@age")]
    int UpdateUsers(string name, int age);
}
```