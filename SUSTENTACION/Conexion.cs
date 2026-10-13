using System;
using System.Data;
using Npgsql;

namespace SUSTENTACION
{
    public class Conexion
    {
        private readonly string servidor =
            "aws-0-us-east-1.pooler.supabase.com";

        private readonly string baseDatos = "postgres";

        private readonly string usuario =
            "postgres.hrhpyafbugxnqmchpwjq";
        private readonly string password =
            "c#artemusaInventario";

        private readonly int puerto = 5432;

        private readonly NpgsqlConnection conexion;

        public Conexion()
        {
            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = servidor,
                Port = puerto,
                Database = baseDatos,
                Username = usuario,
                Password = password,
                SslMode = SslMode.Require,
                Timeout = 60
            };

            conexion = new NpgsqlConnection(
                builder.ConnectionString
            );
        }

        public NpgsqlConnection ObtenerConexion()
        {
            if (conexion.State != ConnectionState.Open)
            {
                conexion.Open();
            }

            return conexion;
        }

        public void CerrarConexion()
        {
            if (conexion.State == ConnectionState.Open)
            {
                conexion.Close();
            }
        }
    }
}