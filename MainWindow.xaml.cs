using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MySql.Data.MySqlClient;

namespace Angajati
{
    public partial class MainWindow : Window
    {
        private int _selectedAngajatID = -1;
        private int _selectedDeptID = -1;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (!DatabaseHelper.TestConnection())
            {
                TxtDbStatus.Text = "● Deconectat";
                TxtDbStatus.Foreground = (SolidColorBrush)FindResource("Br_Danger");
                MessageBox.Show(
                    "Nu s-a putut conecta la baza de date.\nVerificati ca XAMPP ruleaza si baza de date 'companie' exista.",
                    "Eroare conexiune", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            LoadAngajati();
            LoadDepartamente();
            LoadComboDepartamente();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
        private void BtnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
            => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

        private void ShowPage(int page)
        {
            PageAngajati.Visibility = page == 0 ? Visibility.Visible : Visibility.Collapsed;
            PageDept.Visibility     = page == 1 ? Visibility.Visible : Visibility.Collapsed;
            PageFilter.Visibility   = page == 2 ? Visibility.Visible : Visibility.Collapsed;
            BtnNavAngajati.Style = (Style)FindResource(page == 0 ? "Btn_NavActive" : "Btn_Nav");
            BtnNavDept.Style     = (Style)FindResource(page == 1 ? "Btn_NavActive" : "Btn_Nav");
            BtnNavFilter.Style   = (Style)FindResource(page == 2 ? "Btn_NavActive" : "Btn_Nav");
        }

        private void NavAngajati_Click(object sender, RoutedEventArgs e) { ShowPage(0); LoadAngajati(); }
        private void NavDept_Click(object sender, RoutedEventArgs e) { ShowPage(1); LoadDepartamente(); }
        private void NavFilter_Click(object sender, RoutedEventArgs e) { ShowPage(2); LoadComboDepartamenteFilter(); }

        private void ShowErr(TextBlock tb, bool show)
            => tb.Visibility = show ? Visibility.Visible : Visibility.Collapsed;

        private void ClearErrs(params TextBlock[] tbs)
        {
            foreach (var tb in tbs) tb.Visibility = Visibility.Collapsed;
        }

        private int GetComboTag(ComboBox cmb)
        {
            if (cmb.SelectedItem is ComboBoxItem cbi && cbi.Tag != null)
                return (int)cbi.Tag;
            return 0;
        }

        // ======== ANGAJATI ========

        private void LoadAngajati()
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    var adapter = new MySqlDataAdapter("SELECT * FROM vw_AngajatiDepartamente ORDER BY Nume, Prenume", conn);
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    DgAngajati.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Eroare la incarcare angajati:\n" + ex.Message, "Eroare", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadComboDepartamente()
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    var cmd = new MySqlCommand("SELECT DepartamentID, DenumireDepartament FROM Departamente ORDER BY DenumireDepartament", conn);
                    var reader = cmd.ExecuteReader();
                    CmbDepartament.Items.Clear();
                    CmbDepartament.Items.Add(new ComboBoxItem { Content = "— selecteaza —", Tag = 0 });
                    while (reader.Read())
                        CmbDepartament.Items.Add(new ComboBoxItem { Content = reader.GetString(1), Tag = reader.GetInt32(0) });
                    reader.Close();
                    CmbDepartament.SelectedIndex = 0;
                }
            }
            catch { }
        }

        private void DgAngajati_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DgAngajati.SelectedItem is not DataRowView row) return;
            _selectedAngajatID = Convert.ToInt32(row["AngajatID"]);
            TxtNume.Text = row["Nume"].ToString();
            TxtPrenume.Text = row["Prenume"].ToString();
            TxtFunctie.Text = row["Functie"].ToString();
            TxtSalariu.Text = row["Salariu"].ToString();
        }

        private bool ValidateAngajat()
        {
            ClearErrs(ErrNume, ErrPrenume, ErrFunctie, ErrSalariu, ErrDept);
            bool ok = true;
            if (string.IsNullOrWhiteSpace(TxtNume.Text)) { ShowErr(ErrNume, true); ok = false; }
            if (string.IsNullOrWhiteSpace(TxtPrenume.Text)) { ShowErr(ErrPrenume, true); ok = false; }
            if (string.IsNullOrWhiteSpace(TxtFunctie.Text)) { ShowErr(ErrFunctie, true); ok = false; }
            if (!decimal.TryParse(TxtSalariu.Text.Replace(",", "."), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out _))
            { ShowErr(ErrSalariu, true); ok = false; }
            if (GetComboTag(CmbDepartament) == 0) { ShowErr(ErrDept, true); ok = false; }
            return ok;
        }

        private void BtnAdaugaAngajat_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateAngajat()) return;
            try
            {
                decimal salariu = decimal.Parse(TxtSalariu.Text.Replace(",", "."),
                    System.Globalization.CultureInfo.InvariantCulture);
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    var cmd = new MySqlCommand(
                        "INSERT INTO Angajati (DepartamentID, Nume, Prenume, Functie, Salariu) VALUES (@d,@n,@p,@f,@s)", conn);
                    cmd.Parameters.AddWithValue("@d", GetComboTag(CmbDepartament));
                    cmd.Parameters.AddWithValue("@n", TxtNume.Text.Trim());
                    cmd.Parameters.AddWithValue("@p", TxtPrenume.Text.Trim());
                    cmd.Parameters.AddWithValue("@f", TxtFunctie.Text.Trim());
                    cmd.Parameters.AddWithValue("@s", salariu);
                    cmd.ExecuteNonQuery();
                }
                ClearAngajatForm();
                LoadAngajati();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Eroare la adaugare:\n" + ex.Message, "Eroare", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnStergeAngajat_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedAngajatID == -1) { MessageBox.Show("Selectati un angajat din tabel.", "Atentie", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (MessageBox.Show("Stergeti angajatul selectat?", "Confirmare", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    var cmd = new MySqlCommand("DELETE FROM Angajati WHERE AngajatID=@id", conn);
                    cmd.Parameters.AddWithValue("@id", _selectedAngajatID);
                    cmd.ExecuteNonQuery();
                }
                ClearAngajatForm();
                LoadAngajati();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Eroare la stergere:\n" + ex.Message, "Eroare", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearAngajatForm()
        {
            TxtNume.Clear(); TxtPrenume.Clear(); TxtFunctie.Clear(); TxtSalariu.Clear();
            CmbDepartament.SelectedIndex = 0;
            ClearErrs(ErrNume, ErrPrenume, ErrFunctie, ErrSalariu, ErrDept);
            _selectedAngajatID = -1;
            DgAngajati.SelectedItem = null;
        }

        // ======== DEPARTAMENTE ========

        private void LoadDepartamente()
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    var adapter = new MySqlDataAdapter("SELECT * FROM Departamente ORDER BY DenumireDepartament", conn);
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    DgDept.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Eroare la incarcare departamente:\n" + ex.Message, "Eroare", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DgDept_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DgDept.SelectedItem is not DataRowView row) return;
            _selectedDeptID = Convert.ToInt32(row["DepartamentID"]);
            TxtDenumireDept.Text = row["DenumireDepartament"].ToString();
        }

        private void BtnAdaugaDept_Click(object sender, RoutedEventArgs e)
        {
            ClearErrs(ErrDenumire);
            if (string.IsNullOrWhiteSpace(TxtDenumireDept.Text)) { ShowErr(ErrDenumire, true); return; }
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    var cmd = new MySqlCommand("INSERT INTO Departamente (DenumireDepartament) VALUES (@d)", conn);
                    cmd.Parameters.AddWithValue("@d", TxtDenumireDept.Text.Trim());
                    cmd.ExecuteNonQuery();
                }
                TxtDenumireDept.Clear();
                _selectedDeptID = -1;
                DgDept.SelectedItem = null;
                LoadDepartamente();
                LoadComboDepartamente();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Eroare la adaugare:\n" + ex.Message, "Eroare", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnStergeDept_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDeptID == -1) { MessageBox.Show("Selectati un departament din tabel.", "Atentie", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (MessageBox.Show("Stergeti departamentul selectat?\nSe vor sterge si angajatii asociati.",
                "Confirmare", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    var cmd = new MySqlCommand("DELETE FROM Departamente WHERE DepartamentID=@id", conn);
                    cmd.Parameters.AddWithValue("@id", _selectedDeptID);
                    cmd.ExecuteNonQuery();
                }
                TxtDenumireDept.Clear();
                _selectedDeptID = -1;
                DgDept.SelectedItem = null;
                LoadDepartamente();
                LoadComboDepartamente();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Eroare la stergere:\n" + ex.Message, "Eroare", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ======== FILTRARE CU PROCEDURA STOCATA ========

        private void LoadComboDepartamenteFilter()
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    var cmd = new MySqlCommand("SELECT DepartamentID, DenumireDepartament FROM Departamente ORDER BY DenumireDepartament", conn);
                    var reader = cmd.ExecuteReader();
                    CmbFilterDept.Items.Clear();
                    CmbFilterDept.Items.Add(new ComboBoxItem { Content = "— selecteaza departament —", Tag = 0 });
                    while (reader.Read())
                        CmbFilterDept.Items.Add(new ComboBoxItem { Content = reader.GetString(1), Tag = reader.GetInt32(0) });
                    reader.Close();
                    CmbFilterDept.SelectedIndex = 0;
                }
            }
            catch { }
        }

        private void CmbFilterDept_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (CmbFilterDept == null) return;
            int deptId = GetComboTag(CmbFilterDept);
            if (deptId == 0)
            {
                DgFilter.ItemsSource = null;
                return;
            }
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    var cmd = new MySqlCommand("sp_AngajatiDupaDepartament", conn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_DepartamentID", deptId);
                    var adapter = new MySqlDataAdapter(cmd);
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    DgFilter.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Eroare la apelarea procedurii stocate:\n" + ex.Message, "Eroare", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
