using System;
using System.Data;
using MySql.Data.MySqlClient;

namespace SUSTENTACION
{
    public class Conexion
    {
        // Parámetros de la base de datos
        private readonly string servidor = "localhost";
        private readonly string baseDatos = "artemusa_inventario";
        private readonly string usuario = "root";
        private readonly string password = "";
        private readonly string puerto = "3306";

        private readonly MySqlConnection conexion;

        public Conexion()
        {
            string cadenaConexion = $"Server={servidor};Port={puerto};Database={baseDatos};Uid={usuario};Pwd={password};";
            conexion = new MySqlConnection(cadenaConexion);
        }

        // Obtiene la conexión lista y abierta
        public MySqlConnection ObtenerConexion()
        {
            if (conexion.State == ConnectionState.Closed)
            {
                conexion.Open();
            }
            return conexion;
        }

        // Cierra la conexión de forma segura
        public void CerrarConexion()
        {
            if (conexion.State == ConnectionState.Open)
            {
                conexion.Close();
            }
        }
    }
}