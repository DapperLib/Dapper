# Using Dapper.Rainbow in C# for CRUD Operations

This guide outlines how to use `Dapper.Rainbow` in C# for CRUD operations.

## 1. Setting Up

Add Dapper and Dapper.Rainbow to your project via NuGet:

```powershell
Install-Package Dapper -Version x.x.x
Install-Package Dapper.Rainbow -Version x.x.x
```

*Replace `x.x.x` with the latest version numbers.*

## 2. Database Setup and Requirements

For `Dapper.Rainbow` to function correctly, ensure each table has a primary key column named `Id`.

Example `Users` table schema:

```sql
CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name VARCHAR(100),
    Email VARCHAR(100)
);
```

## 3. Establishing Database Connection

Open a connection to your database:

```csharp
using System.Data.SqlClient;

var connectionString = "your_connection_string_here";
using var connection = new SqlConnection(connectionString);
connection.Open(); // Open the connection
```

## 4. Defining Your Database Context

Define a class for your database context:

```csharp
using Dapper;
using System.Data;

public class MyDatabase : Database<MyDatabase>
{
    public Table<User> Users { get; set; }
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}
```

## 5. Performing CRUD Operations

### Insert

```csharp
var db = new MyDatabase { Connection = connection };
var newUser = new User { Name = "John Doe", Email = "john.doe@example.com" };
var insertedUser = db.Users.Insert(newUser);
```

### Select

Fetch users by ID or all users:

```csharp
var user = db.Users.Get(id); // Single user by ID
var users = connection.Query<User>("SELECT * FROM Users"); // All users
```

### Update

```csharp
var userToUpdate = db.Users.Get(id);
userToUpdate.Email = "new.email@example.com";
db.Users.Update(userToUpdate);
```

### Delete

```csharp
db.Users.Delete(id);
```

## 6. Working with Foreign Keys

Example schema for a `Posts` table with a foreign key to `Users`:

```sql
CREATE TABLE Posts (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT,
    Content VARCHAR(255),
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);
```

Inserting a parent (`User`) and a child (`Post`) row:

```csharp
var newUser = new User { Name = "Jane Doe", Email = "jane.doe@example.com" };
var userId = db.Users.Insert(newUser);

var newPost = new Post { UserId = userId, Content = "Hello, World!" };
db.Connection.Insert(newPost); // Using Dapper for the child table
```

