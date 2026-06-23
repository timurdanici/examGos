using System;
using System.Data;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using Microsoft.Reporting.WinForms;

namespace gosExam
{
    public partial class ReportForm : Form
    {
        string connStr = "Server=localhost;Database=TrainingCenter;Uid=root;Pwd=root;";

        public ReportForm()
        {
            InitializeComponent();
            this.WindowState = FormWindowState.Maximized;
            this.Text = "Final Participation & Payments Report";
        }

        private void ReportForm_Load(object sender, EventArgs e)
        {
            LoadReport();
        }

        private void LoadReport()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    // SQL Query for the report (Requirement 13 & Bonus: Sort by TotalSum Desc)
                    string sql = @"
                        SELECT 
                            CONCAT(s.FirstName, ' ', s.LastName) as FullName, 
                            COUNT(e.EnrollmentId) as EnrollmentCount, 
                            IFNULL(SUM(c.Price), 0) as TotalSum
                        FROM Student s
                        LEFT JOIN Enrollment e ON s.StudentId = e.StudentId
                        LEFT JOIN Course c ON e.CourseId = c.CourseId
                        GROUP BY s.StudentId
                        ORDER BY TotalSum DESC";

                    MySqlDataAdapter da = new MySqlDataAdapter(sql, conn);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    // Setup ReportViewer
                    reportViewer1.LocalReport.DataSources.Clear();

                    // Name "DataSet1" must EXACTLY match the name in the .rdlc file
                    ReportDataSource rds = new ReportDataSource("DataSet1", dt);
                    reportViewer1.LocalReport.DataSources.Add(rds);

                    // Point to the RDLC file
                    reportViewer1.LocalReport.ReportPath = "ReportPayments.rdlc";

                    // Bonus: Adding global statistics as Report Parameters (Optional)
                    // If you want to show total revenue at the top/bottom
                    AddReportStatistics(conn);

                    reportViewer1.RefreshReport();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading report: " + ex.Message);
            }
        }

        // Method for Bonus Requirements (Total Revenue, Avg payment, etc.)
        private void AddReportStatistics(MySqlConnection conn)
        {
            // This is how you can get single values for the report summary
            string statsSql = @"
                SELECT 
                    COUNT(DISTINCT StudentId) as TotalStudents,
                    IFNULL(SUM(Price), 0) as TotalRevenue
                FROM Enrollment e
                JOIN Course c ON e.CourseId = c.CourseId";

            MySqlCommand cmd = new MySqlCommand(statsSql, conn);
            conn.Open();
            using (MySqlDataReader dr = cmd.ExecuteReader())
            {
                if (dr.Read())
                {
                    // To use these, you must add 'Report Parameters' in your RDLC file
                    // Example: ReportParameter p1 = new ReportParameter("TotalRev", dr["TotalRevenue"].ToString());
                    // reportViewer1.LocalReport.SetParameters(new ReportParameter[] { p1 });
                }
            }
            conn.Close();
        }

		private void ReportForm_Load_1(object sender, EventArgs e)
		{

			this.reportViewer1.RefreshReport();
		}
	}
}