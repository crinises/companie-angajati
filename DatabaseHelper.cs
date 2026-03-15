using MySql.Data.MySqlClient;

namespace Angajati
{
    public static class DatabaseHelper
    {
        private static readonly string _conn =
            "Server=localhost;Port=3306;Database=companie;Uid=root;Pwd=;CharSet=utf8;";

        public static MySqlConnection GetConnection() => new MySqlConnection(_conn);

        public static bool TestConnection()
        {
            try { using var c = GetConnection(); c.Open(); return true; }
            catch { return false; }
        }
    }
}
