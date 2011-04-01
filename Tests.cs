using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;

namespace SqlMapper
{
    class Tests
    {
        void AssertEquals(object a, object b)
        {
            if (!a.Equals(b))
            {
                throw new ApplicationException(string.Format("{0} should be equals to {1}",a,b));
            }
        }


        SqlConnection connection = Program.GetOpenConnection();

        public void SelectListInt()
        {
            var items = connection.ExecuteMapperQuery<int>("select 1 union all select 2 union all select 3").ToList();

            AssertEquals(items[0], 1);
            AssertEquals(items[1], 2);
            AssertEquals(items[2], 3);
        }

        public void PassInIntArray()
        {
            var items = connection.ExecuteMapperQuery<int>("select * from @Ids", new {Ids = new int[] {1,2,3} }).ToList();

            AssertEquals(items[0], 1);
            AssertEquals(items[1], 2);
            AssertEquals(items[2], 3);
        }
    }
}
