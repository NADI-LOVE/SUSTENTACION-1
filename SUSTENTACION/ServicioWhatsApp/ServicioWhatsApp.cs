using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using Npgsql;
using NpgsqlTypes;

namespace SUSTENTACION.ServicioWhatsApp
{
    internal class ServicioWhatsApp
    {
        // Configurar estas variables en Windows, nunca en el repositorio Git.
        private static string AccountSid => ObtenerConfiguracion("TWILIO_ACCOUNT_SID");
        private static string AuthToken => ObtenerConfiguracion("TWILIO_AUTH_TOKEN");
        private static string NumeroTwilio => ObtenerConfiguracion("TWILIO_WHATSAPP_FROM");
        private static string NumeroDestino => ObtenerConfiguracion("TWILIO_WHATSAPP_TO");
        private static string ContentSidSandbox => ObtenerConfiguracion("TWILIO_CONTENT_SID");

        private static string ObtenerConfiguracion(string nombre)
        {
            string? valor = Environment.GetEnvironmentVariable(nombre);
            if (string.IsNullOrWhiteSpace(valor))
                throw new InvalidOperationException($"Falta configurar la variable de entorno {nombre}.");
            return valor.Trim();
        }

        public static async Task EnviarReporteInventarioAsync()
        {
            try
            {
                int totalUnidades = 0;
                int equiposEnEvento = 0;
                int unidadesDisponibles = 0;
                int devuelvenHoy = 0;

                Conexion conexionDB = new Conexion();
                using (NpgsqlConnection conn = conexionDB.ObtenerConexion())
                {
                    if (conn.State != ConnectionState.Open)
                        conn.Open();

                    const string query = @"
                        SELECT
                            COUNT(*) AS Total,
                            SUM(CASE WHEN estado = 'En Evento' THEN 1 ELSE 0 END) AS EnEvento,
                            SUM(CASE WHEN estado = 'Disponible' THEN 1 ELSE 0 END) AS Disponibles,
                            SUM(CASE WHEN estado = 'Devuelto' THEN 1 ELSE 0 END) AS DevuelvenHoy
                        FROM equipos;";

                    using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                    using (NpgsqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            totalUnidades = reader["Total"] != DBNull.Value ? Convert.ToInt32(reader["Total"]) : 0;
                            equiposEnEvento = reader["EnEvento"] != DBNull.Value ? Convert.ToInt32(reader["EnEvento"]) : 0;
                            unidadesDisponibles = reader["Disponibles"] != DBNull.Value ? Convert.ToInt32(reader["Disponibles"]) : 0;
                            devuelvenHoy = reader["DevuelvenHoy"] != DBNull.Value ? Convert.ToInt32(reader["DevuelvenHoy"]) : 0;
                        }
                    }
                }

                // La plantilla Sandbox espera las variables {{1}} y {{2}}.
                string variable1 = "INVENTARIO";
                string variable2 = $"{totalUnidades} (Eventos: {equiposEnEvento}, Disponibles: {unidadesDisponibles}, Devueltos: {devuelvenHoy})";
                await EnviarMensajeTwilioHttpAsync(variable1, variable2);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo generar o enviar el reporte: {ex.Message}",
                    "Reporte de inventario", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private static async Task EnviarMensajeTwilioHttpAsync(string var1, string var2)
        {
            string accountSid = AccountSid;
            string authToken = AuthToken;

            using (var client = new HttpClient())
            {
                string url = $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Messages.json";
                byte[] authBytes = Encoding.ASCII.GetBytes($"{accountSid}:{authToken}");
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));

                string jsonVariables = JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    ["1"] = var1,
                    ["2"] = var2
                });

                using (var content = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("From", NumeroTwilio),
                    new KeyValuePair<string, string>("To", NumeroDestino),
                    new KeyValuePair<string, string>("ContentSid", ContentSidSandbox),
                    new KeyValuePair<string, string>("ContentVariables", jsonVariables)
                }))
                using (HttpResponseMessage response = await client.PostAsync(url, content))
                {
                    string responseString = await response.Content.ReadAsStringAsync();
                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("Reporte enviado correctamente a WhatsApp.",
                            "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        // No mostrar el cuerpo completo de la respuesta por precaución.
                        MessageBox.Show($"Twilio rechazó el envío. Código HTTP: {(int)response.StatusCode} ({response.ReasonPhrase}).",
                            "Error Twilio", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }
    }
}
