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
            this.Text = "Student Participation Report";
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

                    reportViewer1.LocalReport.DataSources.Clear();

                    ReportDataSource rds = new ReportDataSource("DataSet", dt);
                    reportViewer1.LocalReport.DataSources.Add(rds);

                    reportViewer1.LocalReport.ReportPath = "ReportPayments.rdlc";

                    reportViewer1.RefreshReport();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading report: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}