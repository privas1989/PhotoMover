using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.SqlClient;

namespace PhotoMover
{
    public class BadgePassDB
    {
        private string conn;
        private SqlConnection sql;

        public BadgePassDB(string conn)
        {
            this.conn = conn;
            sql = new SqlConnection(this.conn);
        }

        public string Translate(string guid)
        {
            this.sql.Open();
            string translation = null;

            string query = "SELECT TOP(1) [idNumber] FROM [BadgePass].[dbo].[bbts_employee_lookup] WHERE [path] LIKE '%" + guid + "%'";
            SqlCommand cmd = new SqlCommand(query, this.sql);
            SqlDataReader rd = cmd.ExecuteReader();

            while (rd.Read())
            {
                translation = rd.GetString(0);
            }

            this.sql.Close();
            return translation;
        }
    }
}
