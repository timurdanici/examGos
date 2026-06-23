using System;
using System.Data;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using System.Text.RegularExpressions;

namespace gosExam
{
    public partial class Form1 : Form
    {
        // Connection string - adjust credentials if needed
        string connStr = "Server=localhost;Database=TrainingCenter;Uid=root;Pwd=root;";
        // Track selected IDs (0 means "New mode")
        int selectedStudentId = 0;
        int selectedCourseId = 0;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            numPrice.Maximum = 1000000;
            numDuration.Maximum = 10000;
            numFilterDuration.Maximum = 10000;

            StyleDataGridView(dgvStudents);
            StyleDataGridView(dgvCourses);
            StyleDataGridView(dgvEnrollments);

            LoadAllData();
            LoadTrainerFilter();
        }

        // --- GLOBAL DATA REFRESH ---
        private void LoadAllData()
        {
            try
            {
                RefreshGrid("SELECT * FROM Student", dgvStudents);
                RefreshGrid("SELECT * FROM Course", dgvCourses);
                RefreshGrid(@"SELECT e.EnrollmentId, s.LastName, c.Title, e.EnrollmentDate, e.PaymentStatus 
                          FROM Enrollment e 
                          JOIN Student s ON e.StudentId = s.StudentId 
                          JOIN Course c ON e.CourseId = c.CourseId", dgvEnrollments);

                // Fill Combos for Enrollment tab
                FillCombo("SELECT StudentId, CONCAT(FirstName, ' ', LastName) as Display FROM Student", cmbStudent);
                FillCombo("SELECT CourseId, Title as Display FROM Course", cmbCourse);
                FillCombo("SELECT StudentId, CONCAT(FirstName, ' ', LastName) as Display FROM Student", cmbViewByStudent);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Database Error: " + ex.Message);
            }
        }

        // --- TAB 1: STUDENT MANAGEMENT ---

        private void btnSaveStudent_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFirstName.Text) || string.IsNullOrWhiteSpace(txtLastName.Text) || string.IsNullOrWhiteSpace(txtEmail.Text))
            {
                MessageBox.Show("Please fill in all mandatory fields (First Name, Last Name, Email).", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!IsOnlyLetters(txtFirstName.Text) || !IsOnlyLetters(txtLastName.Text))
            {
                MessageBox.Show("Names should contain only letters.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!string.IsNullOrWhiteSpace(txtPhone.Text) && !IsOnlyDigits(txtPhone.Text))
            {
                MessageBox.Show("Phone number must contain only digits.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!IsValidEmail(txtEmail.Text))
            {
                MessageBox.Show("Please enter a valid email address (e.g., user@example.com).", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!IsEmailUnique(txtEmail.Text, selectedStudentId))
            {
                MessageBox.Show("This email is already registered to another student.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (MySqlConnection conn = new MySqlConnection(connStr))
            {
                string sql = selectedStudentId == 0
                    ? "INSERT INTO Student (FirstName, LastName, Phone, Email) VALUES (@fn, @ln, @p, @e)"
                    : "UPDATE Student SET FirstName=@fn, LastName=@ln, Phone=@p, Email=@e WHERE StudentId=@id";

                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@fn", txtFirstName.Text);
                cmd.Parameters.AddWithValue("@ln", txtLastName.Text);
                cmd.Parameters.AddWithValue("@p", txtPhone.Text);
                cmd.Parameters.AddWithValue("@e", txtEmail.Text);
                if (selectedStudentId > 0) cmd.Parameters.AddWithValue("@id", selectedStudentId);

                conn.Open();
                cmd.ExecuteNonQuery();
                ClearStudentFields();
                LoadAllData();
                MessageBox.Show("Student saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void btnDeleteStudent_Click(object sender, EventArgs e)
        {
            if (selectedStudentId == 0) { MessageBox.Show("Please select a student first!"); return; }

            string checkSql = "SELECT COUNT(*) FROM Enrollment WHERE StudentId = @id";
            if (HasRecords(checkSql, new MySqlParameter("@id", selectedStudentId)))
            {
                MessageBox.Show("Cannot delete this student because they are enrolled in courses.", "Dependency Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var result = MessageBox.Show("Are you sure?", "Confirm", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    MySqlCommand cmd = new MySqlCommand("DELETE FROM Student WHERE StudentId = @id", conn);
                    cmd.Parameters.AddWithValue("@id", selectedStudentId);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                    ClearStudentFields();
                    LoadAllData();
                }
            }
        }

        private void txtSearchStudent_TextChanged(object sender, EventArgs e)
        {
            string sql = $"SELECT * FROM Student WHERE LastName LIKE '%{txtSearchStudent.Text}%' OR Email LIKE '%{txtSearchStudent.Text}%'";
            RefreshGrid(sql, dgvStudents);
        }

        private void dgvStudents_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var row = dgvStudents.Rows[e.RowIndex];
                selectedStudentId = Convert.ToInt32(row.Cells["StudentId"].Value); // ID still exists in the background
                txtFirstName.Text = row.Cells["FirstName"].Value.ToString();
                txtLastName.Text = row.Cells["LastName"].Value.ToString();
                txtPhone.Text = row.Cells["Phone"].Value.ToString();
                txtEmail.Text = row.Cells["Email"].Value.ToString();
                btnSaveStudent.Text = "Update Student";
            }
        }

        private void ClearStudentFields()
        {
            selectedStudentId = 0;
            txtFirstName.Clear(); txtLastName.Clear(); txtEmail.Clear(); txtPhone.Clear();
            btnSaveStudent.Text = "Save Student";
        }

        private void btnClearStudent_Click(object sender, EventArgs e) => ClearStudentFields();

        // --- TAB 2: COURSE MANAGEMENT ---

        private void btnSaveCourse_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTitle.Text) || string.IsNullOrWhiteSpace(txtTrainer.Text))
            {
                MessageBox.Show("Course Title and Trainer name are required.", "Validation Error");
                return;
            }

            if (!IsOnlyLetters(txtTrainer.Text))
            {
                MessageBox.Show("Trainer name should contain only letters.", "Validation Error");
                return;
            }

            if (numPrice.Value <= 0)
            {
                MessageBox.Show("Price must be greater than 0!", "Validation Error");
                return;
            }

            using (MySqlConnection conn = new MySqlConnection(connStr))
            {
                string sql = selectedCourseId == 0
                    ? "INSERT INTO Course (Title, Trainer, Price, DurationDays) VALUES (@t, @tr, @p, @d)"
                    : "UPDATE Course SET Title=@t, Trainer=@tr, Price=@p, DurationDays=@d WHERE CourseId=@id";

                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@t", txtTitle.Text);
                cmd.Parameters.AddWithValue("@tr", txtTrainer.Text);
                cmd.Parameters.AddWithValue("@p", numPrice.Value);
                cmd.Parameters.AddWithValue("@d", numDuration.Value);
                if (selectedCourseId > 0) cmd.Parameters.AddWithValue("@id", selectedCourseId);

                conn.Open();
                cmd.ExecuteNonQuery();
                ClearCourseFields();
                LoadAllData();
                LoadTrainerFilter();
                MessageBox.Show("Course saved successfully!");
            }
        }

        private void btnDeleteCourse_Click(object sender, EventArgs e)
        {
            if (selectedCourseId == 0) { MessageBox.Show("Please select a course first!"); return; }

            string checkSql = "SELECT COUNT(*) FROM Enrollment WHERE CourseId = @id";
            if (HasRecords(checkSql, new MySqlParameter("@id", selectedCourseId)))
            {
                MessageBox.Show("Cannot delete course with active enrollments.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (MessageBox.Show("Delete this course?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    MySqlCommand cmd = new MySqlCommand("DELETE FROM Course WHERE CourseId = @id", conn);
                    cmd.Parameters.AddWithValue("@id", selectedCourseId);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                    ClearCourseFields();
                    LoadAllData();
                    LoadTrainerFilter();
                }
            }
        }

        private void dgvCourses_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var row = dgvCourses.Rows[e.RowIndex];
                selectedCourseId = Convert.ToInt32(row.Cells["CourseId"].Value);
                txtTitle.Text = row.Cells["Title"].Value.ToString();
                txtTrainer.Text = row.Cells["Trainer"].Value.ToString();
                numPrice.Value = Convert.ToDecimal(row.Cells["Price"].Value);
                numDuration.Value = Convert.ToInt32(row.Cells["DurationDays"].Value);
                btnSaveCourse.Text = "Update";
            }
        }

        private void ClearCourseFields()
        {
            selectedCourseId = 0;
            txtTitle.Clear(); txtTrainer.Clear(); numPrice.Value = 1; numDuration.Value = 1;
            btnSaveCourse.Text = "Save";
        }

        private void btnClearCourse_Click(object sender, EventArgs e) => ClearCourseFields();

        private void LoadTrainerFilter()
        {
            using (MySqlConnection conn = new MySqlConnection(connStr))
            {
                string sql = "SELECT DISTINCT Trainer FROM Course WHERE Trainer IS NOT NULL";
                MySqlDataAdapter da = new MySqlDataAdapter(sql, conn);
                DataTable dt = new DataTable();
                da.Fill(dt);
                cmbFilterTrainer.Items.Clear();
                cmbFilterTrainer.Items.Add("All Trainers");
                foreach (DataRow row in dt.Rows) cmbFilterTrainer.Items.Add(row["Trainer"].ToString());
                cmbFilterTrainer.SelectedIndex = 0;
            }
        }

        private void FilterCourses_Changed(object sender, EventArgs e)
        {
            string sql = "SELECT * FROM Course WHERE 1=1";
            if (cmbFilterTrainer.SelectedIndex > 0) sql += $" AND Trainer = '{cmbFilterTrainer.Text}'";
            if (numFilterDuration.Value > 0) sql += $" AND DurationDays = {numFilterDuration.Value}";
            RefreshGrid(sql, dgvCourses);
        }

        // --- TAB 3: ENROLLMENT MANAGEMENT ---

        private void btnRegister_Click(object sender, EventArgs e)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    string sql = "INSERT INTO Enrollment (StudentId, CourseId, EnrollmentDate, PaymentStatus) VALUES (@sid, @cid, @date, @status)";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@sid", cmbStudent.SelectedValue);
                    cmd.Parameters.AddWithValue("@cid", cmbCourse.SelectedValue);
                    cmd.Parameters.AddWithValue("@date", dtpEnrollment.Value);
                    cmd.Parameters.AddWithValue("@status", cmbStatus.Text);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                    LoadAllData();
                }
            }
            catch { MessageBox.Show("Student already registered for this course!"); }
        }

        private void cmbViewByStudent_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbViewByStudent.SelectedValue is int sid)
            {
                string sql = $@"SELECT e.EnrollmentId, c.Title, e.EnrollmentDate, e.PaymentStatus 
                                FROM Enrollment e 
                                JOIN Course c ON e.CourseId = c.CourseId 
                                WHERE e.StudentId = {sid}";
                RefreshGrid(sql, dgvEnrollments);
            }
        }

        private void btnCancelEnrollment_Click(object sender, EventArgs e)
        {
            if (dgvEnrollments.CurrentRow == null) return;
            int id = Convert.ToInt32(dgvEnrollments.CurrentRow.Cells["EnrollmentId"].Value);

            if (MessageBox.Show("Cancel enrollment?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    MySqlCommand cmd = new MySqlCommand("DELETE FROM Enrollment WHERE EnrollmentId=@id", conn);
                    cmd.Parameters.AddWithValue("@id", id);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                    LoadAllData();
                }
            }
        }

        // --- HELPER METHODS ---

        private void RefreshGrid(string sql, DataGridView dgv)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    MySqlDataAdapter da = new MySqlDataAdapter(sql, conn);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgv.DataSource = dt;

                    // Hiding ID columns automatically
                    if (dgv.Columns.Contains("StudentId")) dgv.Columns["StudentId"].Visible = false;
                    if (dgv.Columns.Contains("CourseId")) dgv.Columns["CourseId"].Visible = false;
                    if (dgv.Columns.Contains("EnrollmentId")) dgv.Columns["EnrollmentId"].Visible = false;

                    dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void FillCombo(string sql, ComboBox cb)
        {
            using (MySqlConnection conn = new MySqlConnection(connStr))
            {
                MySqlDataAdapter da = new MySqlDataAdapter(sql, conn);
                DataTable dt = new DataTable();
                da.Fill(dt);
                cb.DataSource = dt;
                cb.DisplayMember = "Display";
                cb.ValueMember = dt.Columns[0].ColumnName;
            }
        }

        private bool HasRecords(string sql, params MySqlParameter[] parameters)
        {
            using (MySqlConnection conn = new MySqlConnection(connStr))
            {
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddRange(parameters);
                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        private void btnOpenReport_Click(object sender, EventArgs e)
        {
            new ReportForm().ShowDialog();
        }

        private void StyleDataGridView(DataGridView dgv)
        {
            dgv.ReadOnly = true;                 
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect; 
            dgv.MultiSelect = false;             
            dgv.AllowUserToAddRows = false;     
            dgv.AllowUserToResizeRows = false;   
            dgv.RowHeadersVisible = false;       
            dgv.BackgroundColor = System.Drawing.Color.White; 
            dgv.BorderStyle = BorderStyle.None;  

            dgv.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(51, 153, 255); 
            dgv.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.White;
            dgv.DefaultCellStyle.Font = new System.Drawing.Font("Segoe UI", 9); 

            dgv.EnableHeadersVisualStyles = false; 
            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = System.Drawing.Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font("Segoe UI Semibold", 10);
            dgv.ColumnHeadersHeight = 35; 

            dgv.AlternatingRowsDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(245, 245, 245);

            dgv.GridColor = System.Drawing.Color.FromArgb(230, 230, 230);
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill; 
        }

        private bool IsValidEmail(string email)
        {
            string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, pattern);
        }

        private bool IsOnlyLetters(string text)
        {
            return Regex.IsMatch(text, @"^[a-zA-Zа-яА-Я\s-]+$");
        }

        private bool IsOnlyDigits(string text)
        {
            return Regex.IsMatch(text, @"^[0-9]+$");
        }

        private bool IsEmailUnique(string email, int excludeId)
        {
            string sql = "SELECT COUNT(*) FROM Student WHERE Email = @e AND StudentId != @id";
            return !HasRecords(sql, new MySqlParameter("@e", email), new MySqlParameter("@id", excludeId));
        }

        private void OnlyLetters_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsLetter(e.KeyChar) && !char.IsControl(e.KeyChar) && e.KeyChar != ' ' && e.KeyChar != '-')
            {
                e.Handled = true;
            }
        }

        private void OnlyDigits_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar))
            {
                e.Handled = true;
            }
        }

    }


}