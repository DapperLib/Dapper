namespace Dapper.Tests
{
    public class Post
    {
        public int Id { get; set; }
        public User Owner { get; set; }
        public string Content { get; set; }
        public Comment Comment { get; set; }
    }
}
