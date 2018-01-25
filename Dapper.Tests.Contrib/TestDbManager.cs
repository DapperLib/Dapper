using System;
using Dapper.Contrib.Extensions;
using MySql.Data.MySqlClient;
using Xunit;

namespace Dapper.Tests.Contrib
{
    enum Gender
    {
        Male = 0,
        Female
    }

    [Table("person")]
    class Human
    {
        public long Id { get; set; }
        protected string Region { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public string Address { get; set; }
        public bool Enabled { get; set; }
        public Gender Gender { get; set; }
        public DateTime? CreatedDate { get; set; }
    }

    public class TestDbManager
    {
        const string conStr = "server=LocalHost;User Id=root;Pwd=greedyint;database=test";
        Random rand = new Random();

        [Fact]
        public void TestSaveChanges()
        {
            var mgr = new DbManager(() => { return new MySqlConnection(conStr); });

            var person = new Human()
            {
                Id = 11,
                Name = "TestDbManager",
                Age = rand.Next(1, 100),
                Address = "TestInsertStrongClassInstanceAddress",
                Enabled = true
            };
            mgr.Insert(person);
            mgr.SaveChanges();

            var queryPerson = mgr.Get<Human>(person.Id);
            Assert.Equal(person.Age, queryPerson.Age);
        }
    }
}
