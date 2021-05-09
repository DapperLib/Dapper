namespace Dapper.Tests
{
    public class Person
    {
        public int PersonId { get; set; }
        public string Name { get; set; }
        public string Occupation { get; private set; }
        public int NumberOfLegs = 2;
        public Address Address { get; set; }
    }
}
