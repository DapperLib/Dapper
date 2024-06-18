namespace Dapper.Tests
{
    /// <summary>
    /// If Docker Desktop is installed, run the following command to start a container suitable for the tests.
    /// <code>
    /// docker run -d -p 3306:3306 --name Dapper.Tests.MariaDB -e MARIADB_DATABASE=tests -e MARIADB_USER=test -e MARIADB_PASSWORD=pass -e MARIADB_ROOT_PASSWORD=pass mariadb
    /// </code>
    /// </summary>
    public sealed class MariaDBProvider : MySqlProvider
    {
        public override string GetConnectionString() =>
            GetConnectionString("MariaDBConnectionString", "Server=localhost;Database=tests;Uid=test;Pwd=pass;");
    }
}
