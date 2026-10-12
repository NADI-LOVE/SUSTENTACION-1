
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using MySql.Data.MySqlClient;

namespace SUSTENTACION.PanelTrabajador
{
    public class FormSalidaEquipo : Form
    {
        // ==========================================
        // COLORES DEL SISTEMA - DARK PREMIUM
        // ==========================================

        private readonly Color fondo = Color.FromArgb(17, 19, 32);
        private readonly Color tarjeta = Color.FromArgb(26, 29, 46);
        private readonly Color campo = Color.FromArgb(37, 41, 62);
        private readonly Color morado = Color.FromArgb(100, 88, 245);
        private readonly Color blanco = Color.FromArgb(245, 246, 255);
        private readonly Color gris = Color.FromArgb(158, 164, 188);
        private readonly Color verde = Color.FromArgb(22, 177, 127);

        // ==========================================
        // DATOS RECIBIDOS
        // ==========================================

        private readonly int idTrabajador;
        private readonly string nombreTrabajador;
        private readonly DateTime fechaSeleccionada;

        // ==========================================
        // COMPONENTES
        // ==========================================

        private TextBox txtCodigoBarras;
        private TextBox txtEncargado;
        private TextBox txtUbicacion;
        private TextBox txtDetalles;

        private ListBox lstEquiposAgregados;
        private Label lblCantidad;
        private Label lblEstadoMapa;

        private Button btnGuardar;
        private WebView2 webMapa;

        private readonly List<int> equiposIds = new List<int>();

        private bool mapaListo = false;
        private bool guardando = false;

        // ==========================================
        // CONSTRUCTOR
        // ==========================================

        public FormSalidaEquipo(
            int idTrabajador,
            string nombreTrabajador,
            DateTime fecha)
        {
            this.idTrabajador = idTrabajador;
            this.nombreTrabajador = nombreTrabajador;
            this.fechaSeleccionada = fecha;

            CrearInterfaz();

            this.Shown += async (s, e) =>
            {
                await InicializarMapaAsync();
            };
        }

        // ==========================================
        // CONTROLES DE DISEÑO
        // ==========================================

        private Label CrearLabel(string texto, int x, int y,
            int ancho = 440, int tamano = 9, bool negrita = false)
        {
            return new Label
            {
                Text = texto,
                Location = new Point(x, y),
                Size = new Size(ancho, 22),
                Font = new Font("Segoe UI", tamano,
                    negrita ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = negrita ? blanco : gris,
                BackColor = Color.Transparent
            };
        }

        private TextBox CrearCampo(int x, int y, int ancho,
            bool multilinea = false)
        {
            return new TextBox
            {
                Location = new Point(x, y),
                Width = ancho,
                Height = multilinea ? 65 : 34,
                Multiline = multilinea,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = campo,
                ForeColor = blanco,
                Font = new Font("Segoe UI", 10),
                ScrollBars = multilinea
                    ? ScrollBars.Vertical
                    : ScrollBars.None
            };
        }

        private Button CrearBoton(string texto, Color color,
            int ancho, int alto)
        {
            Button boton = new Button
            {
                Text = texto,
                Width = ancho,
                Height = alto,
                FlatStyle = FlatStyle.Flat,
                BackColor = color,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            boton.FlatAppearance.BorderSize = 0;
            boton.FlatAppearance.MouseOverBackColor =
                ControlPaint.Light(color, 0.10f);

            return boton;
        }

        // ==========================================
        // INTERFAZ PRINCIPAL
        // ==========================================

        private void CrearInterfaz()
        {
            this.Text = "ARTEMUSA | Registrar salida audiovisual";
            this.Size = new Size(1250, 830);
            this.MinimumSize = new Size(1080, 730);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = fondo;
            this.Font = new Font("Segoe UI", 9);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;

            // Estructura general
            TableLayoutPanel estructura = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = fondo,
                ColumnCount = 1,
                RowCount = 3,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            estructura.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 88));
            estructura.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));
            estructura.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 78));

            this.Controls.Add(estructura);

            // ======================================
            // CABECERA
            // ======================================

            Panel cabecera = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = fondo,
                Padding = new Padding(24, 14, 24, 10)
            };

            Label titulo = CrearLabel(
                "REGISTRAR SALIDA DE EQUIPOS AUDIOVISUALES",
                24, 15, 850, 16, true);

            titulo.Height = 32;

            Label subtitulo = CrearLabel(
                "Control de asignación y destino de equipos audiovisuales",
                26, 52, 850, 9);

            cabecera.Controls.Add(titulo);
            cabecera.Controls.Add(subtitulo);
            estructura.Controls.Add(cabecera, 0, 0);

            // ======================================
            // CONTENIDO EN DOS COLUMNAS
            // ======================================

            TableLayoutPanel contenido = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = fondo,
                Padding = new Padding(16, 0, 16, 10)
            };

            contenido.ColumnStyles.Add(
                new ColumnStyle(SizeType.Absolute, 508));
            contenido.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100));

            contenido.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            estructura.Controls.Add(contenido, 0, 1);

            // ======================================
            // PANEL IZQUIERDO
            // ======================================

            Panel panelIzquierdo = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = tarjeta,
                Margin = new Padding(0, 0, 10, 0),
                AutoScroll = true
            };

            // Contenedor interno para el formulario.
            // Mantiene las posiciones y permite scroll.
            Panel formulario = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(480, 650),
                BackColor = tarjeta
            };

            panelIzquierdo.Controls.Add(formulario);

            contenido.Controls.Add(panelIzquierdo, 0, 0);

            formulario.Controls.Add(CrearLabel(
                "01  |  INFORMACIÓN DE LA SALIDA",
                20, 18, 420, 11, true));

            Label fecha = CrearLabel(
                "Fecha programada: " +
                fechaSeleccionada.ToString("dd/MM/yyyy"),
                20, 52, 430, 10);

            fecha.ForeColor = morado;
            formulario.Controls.Add(fecha);

            // ======================================
            // CÓDIGO DE BARRAS
            // ======================================

            formulario.Controls.Add(CrearLabel(
                "Código de barras del equipo audiovisual",
                20, 91, 445, 9, true));

            txtCodigoBarras = CrearCampo(20, 118, 435);

            txtCodigoBarras.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    AgregarEquipoPorCodigo(
                        txtCodigoBarras.Text.Trim());
                }
            };

            formulario.Controls.Add(txtCodigoBarras);

            Button btnAgregar = CrearBoton(
                "+  AGREGAR EQUIPO AUDIOVISUAL",
                morado, 435, 39);

            btnAgregar.Location = new Point(20, 162);

            btnAgregar.Click += (s, e) =>
            {
                AgregarEquipoPorCodigo(
                    txtCodigoBarras.Text.Trim());
            };

            formulario.Controls.Add(btnAgregar);

            // ======================================
            // LISTA DE EQUIPOS
            // ======================================

            formulario.Controls.Add(CrearLabel(
                "Equipos audiovisuales seleccionados",
                20, 217, 350, 9, true));

            lblCantidad = CrearLabel(
                "0 equipos", 350, 217, 105, 9);

            lblCantidad.TextAlign = ContentAlignment.MiddleRight;
            formulario.Controls.Add(lblCantidad);

            lstEquiposAgregados = new ListBox
            {
                Location = new Point(20, 246),
                Size = new Size(435, 103),
                BackColor = campo,
                ForeColor = blanco,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9.5f),
                IntegralHeight = false
            };

            formulario.Controls.Add(lstEquiposAgregados);

            Button btnQuitar = CrearBoton(
                "QUITAR EQUIPO SELECCIONADO",
                Color.FromArgb(73, 53, 78), 435, 30);

            btnQuitar.Location = new Point(20, 359);

            btnQuitar.Click += (s, e) =>
            {
                QuitarEquipo();
            };

            formulario.Controls.Add(btnQuitar);

            // ======================================
            // RESPONSABLE
            // ======================================

            formulario.Controls.Add(CrearLabel(
                "Encargado de los equipos audiovisuales",
                20, 407, 435, 9, true));

            txtEncargado = CrearCampo(20, 432, 435);
            txtEncargado.Text = nombreTrabajador;

            formulario.Controls.Add(txtEncargado);

            // ======================================
            // UBICACIÓN
            // ======================================

            formulario.Controls.Add(CrearLabel(
                "Ubicación o destino del evento",
                20, 480, 435, 9, true));

            txtUbicacion = CrearCampo(20, 506, 435);

            txtUbicacion.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    BuscarUbicacion();
                }
            };

            formulario.Controls.Add(txtUbicacion);

            // ======================================
            // DETALLES
            // ======================================

            formulario.Controls.Add(CrearLabel(
                "Detalles adicionales",
                20, 552, 435, 9, true));

            txtDetalles = CrearCampo(20, 579, 435, true);

            formulario.Controls.Add(txtDetalles);

            // ======================================
            // PANEL DERECHO - MAPA
            // ======================================

            Panel panelMapa = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = tarjeta,
                Margin = new Padding(0),
                Padding = new Padding(14)
            };

            contenido.Controls.Add(panelMapa, 1, 0);

            Panel mapaEncabezado = new Panel
            {
                Dock = DockStyle.Top,
                Height = 99,
                BackColor = tarjeta
            };

            Label tituloMapa = CrearLabel(
                "02  |  UBICACIÓN DEL EVENTO",
                8, 5, 450, 11, true);

            mapaEncabezado.Controls.Add(tituloMapa);

            lblEstadoMapa = CrearLabel(
                "Preparando mapa interactivo...",
                8, 32, 450, 9);

            mapaEncabezado.Controls.Add(lblEstadoMapa);

            Button btnBuscar = CrearBoton(
                "BUSCAR UBICACIÓN",
                morado, 175, 32);

            btnBuscar.Location = new Point(8, 61);

            btnBuscar.Click += (s, e) =>
            {
                BuscarUbicacion();
            };

            mapaEncabezado.Controls.Add(btnBuscar);

            Button btnAbrirMapa = CrearBoton(
                "ABRIR MAPA",
                campo, 155, 32);

            btnAbrirMapa.Location = new Point(193, 61);

            btnAbrirMapa.Click += (s, e) =>
            {
                AbrirMapaExterno();
            };

            mapaEncabezado.Controls.Add(btnAbrirMapa);

            Panel mapaPie = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 35,
                BackColor = tarjeta
            };

            Label indicacion = CrearLabel(
                "Puedes buscar una dirección o marcar un punto en el mapa.",
                8, 7, 580, 9);

            mapaPie.Controls.Add(indicacion);

            webMapa = new WebView2
            {
                Dock = DockStyle.Fill,
                DefaultBackgroundColor =
                    Color.FromArgb(31, 35, 51)
            };

            // El orden de acoplamiento deja el mapa
            // dentro de los encabezados y el pie.
            panelMapa.Controls.Add(webMapa);
            panelMapa.Controls.Add(mapaPie);
            panelMapa.Controls.Add(mapaEncabezado);

            // ======================================
            // PIE CON BOTONES
            // ======================================

            Panel pie = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = fondo
            };

            estructura.Controls.Add(pie, 0, 2);

            btnGuardar = CrearBoton(
                "GUARDAR SALIDA",
                verde, 230, 43);

            btnGuardar.Click += (s, e) =>
            {
                GuardarSalida();
            };

            Button btnCancelar = CrearBoton(
                "CANCELAR",
                Color.FromArgb(65, 68, 88),
                155, 43);

            btnCancelar.Click += (s, e) =>
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };

            // Botones alineados a la derecha.
            FlowLayoutPanel acciones = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                Width = 430,
                Height = 60,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Padding = new Padding(0, 7, 15, 0)
            };

            btnGuardar.Margin = new Padding(8, 0, 0, 0);
            btnCancelar.Margin = new Padding(8, 0, 0, 0);

            acciones.Controls.Add(btnGuardar);
            acciones.Controls.Add(btnCancelar);

            pie.Controls.Add(acciones);

            Label pieTexto = CrearLabel(
                "ARTEMUSA PERÚ S.R.L.  |  Control audiovisual",
                25, 22, 470, 9);

            pie.Controls.Add(pieTexto);
        }

        // ==========================================
        // INICIALIZAR MAPA HTML
        // ==========================================

        private async Task InicializarMapaAsync()
        {
            try
            {
                lblEstadoMapa.Text = "Cargando motor del mapa...";

                await webMapa.EnsureCoreWebView2Async();

                webMapa.CoreWebView2.WebMessageReceived +=
                    RecibirMensajeMapa;

                string rutaMapa = Path.Combine(
                    Application.StartupPath,
                    "PanelTrabajador",
                    "Mapas",
                    "mapa.html");

                // Compatibilidad si el archivo se copia
                // directamente a la carpeta Mapas.
                if (!File.Exists(rutaMapa))
                {
                    rutaMapa = Path.Combine(
                        Application.StartupPath,
                        "Mapas",
                        "mapa.html");
                }

                if (!File.Exists(rutaMapa))
                {
                    lblEstadoMapa.Text =
                        "No se encontró el archivo mapa.html.";

                    MessageBox.Show(
                        "No existe el archivo del mapa en:\n" +
                        rutaMapa +
                        "\n\nConfigura 'Copiar si es posterior' " +
                        "en las propiedades de mapa.html.",
                        "Archivo de mapa no encontrado",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return;
                }

                // El archivo HTML se carga después de
                // inicializar completamente WebView2.
                string carpetaMapa = Path.GetDirectoryName(rutaMapa);

                webMapa.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    "artemusa.local",
                    carpetaMapa,
                    CoreWebView2HostResourceAccessKind.Allow);

                webMapa.CoreWebView2.Navigate(
                    "https://artemusa.local/mapa.html");
            }
            catch (Exception ex)
            {
                lblEstadoMapa.Text =
                    "No se pudo iniciar el mapa.";

                MessageBox.Show(
                    "Error al iniciar WebView2:\n" + ex.Message,
                    "Error de mapa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // ==========================================
        // MENSAJES DESDE HTML HACIA C#
        // ==========================================

        private void RecibirMensajeMapa(
            object sender,
            CoreWebView2WebMessageReceivedEventArgs e)
        {
            string mensaje;

            try
            {
                mensaje = e.TryGetWebMessageAsString();
            }
            catch
            {
                return;
            }

            if (mensaje == "mapa_listo")
            {
                mapaListo = true;
                lblEstadoMapa.Text =
                    "Mapa interactivo disponible";
            }
            else if (mensaje.StartsWith("ubicacion|"))
            {
                string ubicacion =
                    mensaje.Substring("ubicacion|".Length);

                txtUbicacion.Text = ubicacion;

                lblEstadoMapa.Text =
                    "Ubicación seleccionada correctamente";
            }
            else if (mensaje.StartsWith("estado|"))
            {
                lblEstadoMapa.Text =
                    mensaje.Substring("estado|".Length);
            }
        }

        // ==========================================
        // BUSCAR DESTINO EN EL MAPA
        // ==========================================

        private void BuscarUbicacion()
        {
            string direccion = txtUbicacion.Text.Trim();

            if (string.IsNullOrWhiteSpace(direccion))
            {
                MessageBox.Show(
                    "Escribe una ubicación para buscar.",
                    "Ubicación requerida",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }

            if (!mapaListo || webMapa.CoreWebView2 == null)
            {
                MessageBox.Show(
                    "El mapa todavía no está disponible.",
                    "Mapa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }

            // PostWebMessageAsString evita concatenar
            // texto ingresado por el usuario como JavaScript.
            webMapa.CoreWebView2.PostWebMessageAsString(
                direccion);
        }

        private void AbrirMapaExterno()
        {
            string ubicacion =
                string.IsNullOrWhiteSpace(txtUbicacion.Text)
                ? "Juliaca, Puno, Perú"
                : txtUbicacion.Text.Trim();

            string url =
                "https://www.openstreetmap.org/search?query=" +
                Uri.EscapeDataString(ubicacion);

            try
            {
                ProcessStart(url);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "No se pudo abrir el navegador: " +
                    ex.Message);
            }
        }

        private void ProcessStart(string url)
        {
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
        }

        // ==========================================
        // AGREGAR EQUIPO POR CÓDIGO DE BARRAS
        // ==========================================

        private void AgregarEquipoPorCodigo(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
            {
                MessageBox.Show(
                    "Ingresa o escanea un código de barras.");
                txtCodigoBarras.Focus();
                return;
            }

            Conexion conexionDB = new Conexion();

            try
            {
                MySqlConnection con =
                    conexionDB.ObtenerConexion();

                string query =
                    "SELECT id, codigo, nombre " +
                    "FROM equipos WHERE codigo = @codigo";

                using (MySqlCommand cmd =
                    new MySqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue(
                        "@codigo", codigo);

                    using (MySqlDataReader reader =
                        cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int idEquipo =
                                Convert.ToInt32(reader["id"]);

                            string nombreEquipo =
                                reader["nombre"].ToString();

                            if (equiposIds.Contains(idEquipo))
                            {
                                MessageBox.Show(
                                    "Este equipo audiovisual " +
                                    "ya está agregado.");
                                return;
                            }

                            equiposIds.Add(idEquipo);

                            lstEquiposAgregados.Items.Add(
                                codigo + "  |  " + nombreEquipo);

                            lblCantidad.Text =
                                equiposIds.Count + " equipos";

                            txtCodigoBarras.Clear();
                            txtCodigoBarras.Focus();
                        }
                        else
                        {
                            MessageBox.Show(
                                "No se encontró un equipo " +
                                "audiovisual con ese código.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error al buscar equipo audiovisual:\n" +
                    ex.Message);
            }
            finally
            {
                conexionDB.CerrarConexion();
            }
        }

        // ==========================================
        // QUITAR EQUIPO DE LA LISTA
        // ==========================================

        private void QuitarEquipo()
        {
            int indice = lstEquiposAgregados.SelectedIndex;

            if (indice < 0)
            {
                MessageBox.Show(
                    "Selecciona un equipo audiovisual " +
                    "para quitarlo de la lista.");
                return;
            }

            equiposIds.RemoveAt(indice);
            lstEquiposAgregados.Items.RemoveAt(indice);

            lblCantidad.Text =
                equiposIds.Count + " equipos";
        }

        // ==========================================
        // REGISTRAR SALIDA EN MYSQL
        // ==========================================

        private void GuardarSalida()
        {
            if (guardando)
                return;

            if (equiposIds.Count == 0)
            {
                MessageBox.Show(
                    "Agrega al menos un equipo audiovisual.");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtEncargado.Text))
            {
                MessageBox.Show(
                    "Ingresa el nombre del encargado.");
                txtEncargado.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtUbicacion.Text))
            {
                MessageBox.Show(
                    "Ingresa la ubicación del evento.");
                txtUbicacion.Focus();
                return;
            }

            guardando = true;
            btnGuardar.Enabled = false;

            Conexion conexionDB = new Conexion();

            try
            {
                MySqlConnection con =
                    conexionDB.ObtenerConexion();

                // Una transacción mantiene unidos
                // el encabezado y los equipos.
                using (MySqlTransaction transaccion =
                    con.BeginTransaction())
                {
                    try
                    {
                        string sqlSalida = @"
                            INSERT INTO salidas
                            (
                                id_trabajador,
                                encargado,
                                fecha_salida,
                                ubicacion,
                                detalles,
                                estado
                            )
                            VALUES
                            (
                                @trabajador,
                                @encargado,
                                @fecha,
                                @ubicacion,
                                @detalles,
                                'Programada'
                            )";

                        long idSalida;

                        using (MySqlCommand cmd =
                            new MySqlCommand(
                                sqlSalida, con, transaccion))
                        {
                            cmd.Parameters.AddWithValue(
                                "@trabajador", idTrabajador);

                            cmd.Parameters.AddWithValue(
                                "@encargado",
                                txtEncargado.Text.Trim());

                            cmd.Parameters.AddWithValue(
                                "@fecha", fechaSeleccionada);

                            cmd.Parameters.AddWithValue(
                                "@ubicacion",
                                txtUbicacion.Text.Trim());

                            cmd.Parameters.AddWithValue(
                                "@detalles",
                                txtDetalles.Text.Trim());

                            cmd.ExecuteNonQuery();

                            idSalida = cmd.LastInsertedId;
                        }

                        string sqlEquipo = @"
                            INSERT INTO salida_equipos
                            (id_salida, id_equipo)
                            VALUES (@salida, @equipo)";

                        foreach (int idEquipo in equiposIds)
                        {
                            using (MySqlCommand cmdEquipo =
                                new MySqlCommand(
                                    sqlEquipo,
                                    con,
                                    transaccion))
                            {
                                cmdEquipo.Parameters.AddWithValue(
                                    "@salida", idSalida);

                                cmdEquipo.Parameters.AddWithValue(
                                    "@equipo", idEquipo);

                                cmdEquipo.ExecuteNonQuery();
                            }
                        }

                        transaccion.Commit();
                    }
                    catch
                    {
                        transaccion.Rollback();
                        throw;
                    }
                }

                MessageBox.Show(
                    "La salida de equipos audiovisuales " +
                    "se registró correctamente.",
                    "Registro exitoso",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "No se pudo registrar la salida:\n" +
                    ex.Message,
                    "Error de MySQL",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                guardando = false;

                if (!this.IsDisposed)
                    btnGuardar.Enabled = true;

                conexionDB.CerrarConexion();
            }
        }
    }
}
