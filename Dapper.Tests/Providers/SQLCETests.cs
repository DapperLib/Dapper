#if SQL_CE
using System.Data.SqlServerCe;
using System.IO;
using System.Linq;
using Xunit;

namespace Dapper.Tests
{
    public class SQLCETests : TestBase
    {
        [Fact]
        public void MultiRSSqlCE()
        {
            if (File.Exists("Test.DB.sdf"))
                File.Delete("Test.DB.sdf");

            const string cnnStr = "Data Source = Test.DB.sdf;";
            var engine = new SqlCeEngine(cnnStr);
            engine.CreateDatabase();

            using (var cnn = new SqlCeConnection(cnnStr))
            {
                cnn.Open();

                cnn.Execute("create table Posts (ID int, Title nvarchar(50), Body nvarchar(50), AuthorID int)");
                cnn.Execute("create table Authors (ID int, Name nvarchar(50))");

                cnn.Execute("insert Posts values (1,'title','body',1)");
                cnn.Execute("insert Posts values(2,'title2','body2',null)");
                cnn.Execute("insert Authors values(1,'sam')");

                var data = cnn.Query<PostCE, AuthorCE, PostCE>("select * from Posts p left join Authors a on a.ID = p.AuthorID", (post, author) => { post.Author = author; return post; }).ToList();
                var firstPost = data[0];
                Assert.Equal("title", firstPost.Title);
                Assert.Equal("sam", firstPost.Author.Name);
                Assert.Null(data[1].Author);
            }
        }

        public class PostCE
        {
            public int ID { get; set; }
            public string Title { get; set; }
            public string Body { get; set; }

            public AuthorCE Author { get; set; }
        }

        public class AuthorCE
        {
            public int ID { get; set; }
            public string Name { get; set; }
        }
    }
}
#endif
