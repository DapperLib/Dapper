using System.Data.SqlClient;
using System.Windows.Forms;
using Dapper;

namespace UIBindingTest
{
    public partial class BindingForm : Form
    {
        public BindingForm()
        {
            InitializeComponent();

            SuspendLayout();
            using (var conn = new SqlConnection("Data Source=.;Initial Catalog=master;Integrated Security=SSPI"))
            {
                mainGrid.DataSource = conn.Query("select * from sys.objects").AsList();
            }
            ResumeLayout();
        }
    }
}
