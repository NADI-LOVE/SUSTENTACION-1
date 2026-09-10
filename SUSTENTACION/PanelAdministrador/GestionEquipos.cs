using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SUSTENTACION.PanelAdministrador
{
    internal class GestionEquipos
    {
        private static readonly string phoneNumberId = "1284893474709394";
        private static readonly string accessToken = "EAAWNGZAd1wEkBSYaJLTDuWWkAEzxSNEUb8am3nAW4H3HyZAj3Naiux6TYDEsK7WyUV9soRDO9ZBdvZBJXaDZBfPmz35b1O8Wi4ZBwNmlmFevbvbMcgJYDgaAOEnXBHRcENoxSMppe8JdDVUxxerCbNj3SYnkd2O3n3kJClRAe6iGBsf67lBQawrGrPLSMG4NPzAlDUKl92nsGTBmTi8hR37teFoiVM2HRx8aVDiSEAX5lWErtdQf186POLCuCyTLZCk0vq95qUlOuvoVTra148uU5v2peaTNptqMz9Ndc2D";
        private static readonly string recipientPhone = "51961256566";

        public static async Task<bool> EnviarNotificacionWhatsAppAsync(string mensaje)
        {
            try
            {
                using var httpClient = new HttpClient();
                string url = $"https://graph.facebook.com/v25.0/{phoneNumberId}/messages";

                // Estructura de PLANTILLA (Template) para pasar la restricción de 24 horas
                var payload = new
                {
                    messaging_product = "whatsapp",
                    to = recipientPhone,
                    type = "template",
                    template = new
                    {
                        name = "hello_world", // Plantilla por defecto de Meta
                        language = new
                        {
                            code = "en_US"
                        }
                    }
                };

                string jsonPayload = JsonSerializer.Serialize(payload);

                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await httpClient.SendAsync(request);
                string responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    MessageBox.Show($"Respuesta de Meta:\n{responseContent}", "Detalle del Error Meta", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error de conexión: {ex.Message}", "Error de Excepción", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
    }
}