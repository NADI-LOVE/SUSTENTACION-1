using Npgsql;
using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SUSTENTACION.PanelTrabajador
{
    public class NotificacionesPanel : UserControl
    {
        // ═══════════════════════════════════════════
        // PALETA
        // ═══════════════════════════════════════════
        private readonly Color bgMain = Color.FromArgb(15, 15, 20);
        private readonly Color bgCard = Color.FromArgb(25, 25, 35);
        private readonly Color bgRow = Color.FromArgb(32, 32, 45);
        private readonly Color accentPurple = Color.FromArgb(124, 92, 255);
        private readonly Color accentGreen = Color.FromArgb(46, 204, 148);
        private readonly Color accentYellow = Color.FromArgb(255, 193, 7);
        private readonly Color accentRed = Color.FromArgb(240, 90, 110);
        private readonly Color accentCyan = Color.FromArgb(0, 188, 212);
        private readonly Color textWhite = Color.White;
        private readonly Color textMuted = Color.FromArgb(150, 150, 160);

        private int idTrabajador;
        private string nombreTrabajador;

        // Tabs
        private Button btnTabSolicitudes;
        private Button btnTabNotificaciones;
        private Panel pnlContenido;
        private FlowLayoutPanel flpItems;

        private string tabActual = "solicitudes";

        public NotificacionesPanel(int idTrabajador, string nombreTrabajador)
        {
            this.idTrabajador = idTrabajador;
            this.nombreTrabajador = nombreTrabajador;

            InitializeComponentes();
            CargarSolicitudes();
        }

        private void InitializeComponentes()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = bgMain;
            this.Padding = new Padding(24, 20, 24, 20);

            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.Transparent
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            // ─── HEADER ───
            Panel headerPanel = new Panel { Dock = DockStyle.Fill, BackColor = bgCard, Padding = new Padding(20) };
            AplicarBordesRedondeados(headerPanel, 10);

            headerPanel.Controls.Add(new Label
            {
                Text = "🔔  Notificaciones y solicitudes",
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = textWhite,
                Location = new Point(20, 14),
                AutoSize = true
            });

            headerPanel.Controls.Add(new Label
            {
                Text = "Gestiona tus solicitudes de cambio y mensajes del administrador",
                Font = new Font("Segoe UI", 9f),
                ForeColor = textMuted,
                Location = new Point(22, 42),
                AutoSize = true
            });

            mainLayout.Controls.Add(headerPanel, 0, 0);

            // ─── TABS ───
            FlowLayoutPanel tabsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 8, 0, 0)
            };

            btnTabSolicitudes = CrearTabButton("📩  Mis solicitudes", true);
            btnTabNotificaciones = CrearTabButton("🔔  Notificaciones", false);

            btnTabSolicitudes.Click += (s, e) =>
            {
                tabActual = "solicitudes";
                ActualizarTabs();
                CargarSolicitudes();
            };

            btnTabNotificaciones.Click += (s, e) =>
            {
                tabActual = "notificaciones";
                ActualizarTabs();
                CargarNotificaciones();
            };

            tabsPanel.Controls.Add(btnTabSolicitudes);
            tabsPanel.Controls.Add(btnTabNotificaciones);

            mainLayout.Controls.Add(tabsPanel, 0, 1);

            // ─── CONTENIDO ───
            pnlContenido = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = bgCard,
                Padding = new Padding(20),
                Margin = new Padding(0, 10, 0, 0)
            };
            AplicarBordesRedondeados(pnlContenido, 10);

            Label lblInfoContenido = new Label
            {
                Text = "📋  Contenido",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = textWhite,
                Dock = DockStyle.Top,
                Height = 32,
                Padding = new Padding(2, 4, 0, 0),
                Name = "lblTituloContenido"
            };
            pnlContenido.Controls.Add(lblInfoContenido);

            flpItems = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Color.Transparent
            };
            pnlContenido.Controls.Add(flpItems);
            flpItems.BringToFront();

            mainLayout.Controls.Add(pnlContenido, 0, 2);

            this.Controls.Add(mainLayout);
        }

        private Button CrearTabButton(string texto, bool activo)
        {
            Button btn = new Button
            {
                Text = texto,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = activo ? textWhite : textMuted,
                BackColor = activo ? accentPurple : bgCard,
                FlatStyle = FlatStyle.Flat,
                Height = 36,
                Width = 200,
                Margin = new Padding(0, 0, 10, 0),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            AplicarBordesRedondeados(btn, 8);
            return btn;
        }

        private void ActualizarTabs()
        {
            bool esSol = tabActual == "solicitudes";
            btnTabSolicitudes.BackColor = esSol ? accentPurple : bgCard;
            btnTabSolicitudes.ForeColor = esSol ? textWhite : textMuted;
            btnTabNotificaciones.BackColor = !esSol ? accentPurple : bgCard;
            btnTabNotificaciones.ForeColor = !esSol ? textWhite : textMuted;
        }

        // ═══════════════════════════════════════════
        // CARGAR SOLICITUDES
        // ═══════════════════════════════════════════
        private void CargarSolicitudes()
        {
            var lbl = pnlContenido.Controls["lblTituloContenido"] as Label;
            if (lbl != null) lbl.Text = "📩  Mis solicitudes de cambio";

            flpItems.Controls.Clear();

            Conexion conexionDB = new Conexion();
            try
            {
                NpgsqlConnection con = conexionDB.ObtenerConexion();

                string sql = @"
                    SELECT id_solicitud, tipo_cambio, descripcion, estado, 
                           fecha_solicitud, respuesta_admin
                    FROM solicitudes_cambio
                    WHERE id_trabajador = @id
                    ORDER BY fecha_solicitud DESC
                    LIMIT 30";

                using (NpgsqlCommand cmd = new NpgsqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@id", idTrabajador);

                    using (NpgsqlDataReader rd = cmd.ExecuteReader())
                    {
                        bool hay = false;
                        while (rd.Read())
                        {
                            hay = true;
                            int idSolicitud = Convert.ToInt32(rd["id_solicitud"]);
                            string tipo = rd["tipo_cambio"].ToString();
                            string desc = rd["descripcion"].ToString();
                            string estado = rd["estado"].ToString();
                            DateTime fecha = Convert.ToDateTime(rd["fecha_solicitud"]);
                            string respuestaAdmin = rd["respuesta_admin"]?.ToString() ?? "";

                            flpItems.Controls.Add(
                                CrearItemSolicitudConHilo(idSolicitud, tipo, desc, estado, fecha, respuestaAdmin));
                        }

                        if (!hay)
                            MostrarVacio("📭  No tienes solicitudes pendientes",
                                "Crea una desde tu perfil haciendo click en '🔑 Cambiar'");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error solicitudes: " + ex.Message);
                MostrarVacio("⚠  Error al cargar",
                    "Verifica que existan las tablas solicitudes_cambio y respuestas_solicitud");
            }
            finally
            {
                conexionDB.CerrarConexion();
            }
        }

        private void CargarNotificaciones()
        {
            var lbl = pnlContenido.Controls["lblTituloContenido"] as Label;
            if (lbl != null) lbl.Text = "🔔  Notificaciones";

            flpItems.Controls.Clear();

            MostrarVacio("🔔  No tienes notificaciones",
                "Cuando el administrador o un compañero te escriba, aparecerá aquí");
        }

        private void MostrarVacio(string titulo, string subtitulo)
        {
            Panel item = new Panel
            {
                Width = 700,
                Height = 100,
                Margin = new Padding(0, 20, 0, 0),
                BackColor = bgRow
            };
            AplicarBordesRedondeados(item, 10);

            item.Controls.Add(new Label
            {
                Text = titulo,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = textMuted,
                Dock = DockStyle.Top,
                Height = 50,
                TextAlign = ContentAlignment.MiddleCenter
            });

            item.Controls.Add(new Label
            {
                Text = subtitulo,
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(100, 100, 120),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.TopCenter
            });

            flpItems.Controls.Add(item);
        }

        // ═══════════════════════════════════════════
        // ITEM DE SOLICITUD CON HILO
        // ═══════════════════════════════════════════
        private Panel CrearItemSolicitudConHilo(int idSolicitud, string tipo, string desc,
            string estado, DateTime fecha, string respuestaAdmin)
        {
            // Contenedor principal
            Panel item = new Panel
            {
                Width = 850,
                BackColor = bgRow,
                Margin = new Padding(0, 6, 0, 6),
                Padding = new Padding(0)
            };
            AplicarBordesRedondeados(item, 10);

            // Layout en 3 filas: header / hilo / responder
            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60f));

            // ── Color según estado ──
            string estadoLower = estado.ToLower().Trim();

            Color colorEstado = accentYellow;
            if (estadoLower.Contains("aprob") || estadoLower.Contains("autoriz"))
                colorEstado = accentGreen;
            else if (estadoLower.Contains("rech") || estadoLower.Contains("deneg"))
                colorEstado = accentRed;

            // ═══════════════════════════════════
            // FILA 0: CABECERA
            // ═══════════════════════════════════
            Panel headerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = bgRow,
                Margin = new Padding(0)
            };

            Panel barra = new Panel { Width = 4, Dock = DockStyle.Left, BackColor = colorEstado };
            headerPanel.Controls.Add(barra);

            headerPanel.Controls.Add(new Label
            {
                Text = "📝",
                Font = new Font("Segoe UI", 20f),
                ForeColor = accentPurple,
                Location = new Point(20, 18),
                AutoSize = true,
                BackColor = Color.Transparent
            });

            headerPanel.Controls.Add(new Label
            {
                Text = tipo,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = textWhite,
                Location = new Point(65, 14),
                AutoSize = true,
                BackColor = Color.Transparent
            });

            headerPanel.Controls.Add(new Label
            {
                Text = desc,
                Font = new Font("Segoe UI", 9f),
                ForeColor = textMuted,
                Location = new Point(65, 38),
                Size = new Size(600, 20),
                AutoEllipsis = true,
                BackColor = Color.Transparent
            });

            headerPanel.Controls.Add(new Label
            {
                Text = "🕒  " + fecha.ToString("dd/MM/yyyy HH:mm"),
                Font = new Font("Segoe UI", 8f),
                ForeColor = Color.FromArgb(100, 100, 120),
                Location = new Point(65, 58),
                AutoSize = true,
                BackColor = Color.Transparent
            });

            // ═══════════════════════════════════
            // BADGE DE ESTADO (arriba a la derecha)
            // ═══════════════════════════════════
            Panel badge = new Panel
            {
                Size = new Size(120, 28),
                BackColor = colorEstado,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            AplicarBordesRedondeados(badge, 14);

            string icono = "⏳";
            string textoBadge = "PENDIENTE";
            if (estadoLower.Contains("aprob") || estadoLower.Contains("autoriz"))
            {
                icono = "✅";
                textoBadge = "APROBADA";
            }
            else if (estadoLower.Contains("rech") || estadoLower.Contains("deneg"))
            {
                icono = "❌";
                textoBadge = "RECHAZADA";
            }
            else if (estadoLower.Contains("visto") || estadoLower.Contains("revis"))
            {
                icono = "👁";
                textoBadge = "VISTO";
            }

            badge.Controls.Add(new Label
            {
                Text = icono + "  " + textoBadge,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            });

            Action posicionarBadge = () =>
            {
                if (headerPanel.Width > 200)
                    badge.Location = new Point(headerPanel.Width - badge.Width - 20, 22);
            };
            headerPanel.Resize += (s, e) => posicionarBadge();
            posicionarBadge();

            headerPanel.Controls.Add(badge);

            // ═══════════════════════════════════
            // FILA 1: HILO
            // ═══════════════════════════════════
            FlowLayoutPanel flpHilo = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = false,
                BackColor = Color.Transparent,
                Padding = new Padding(60, 8, 60, 8),
                Name = "flpHilo_" + idSolicitud
            };

            // 1. Respuesta legacy del admin
            if (!string.IsNullOrWhiteSpace(respuestaAdmin))
            {
                flpHilo.Controls.Add(CrearMensajeHilo("Admin", respuestaAdmin, true));
            }

            // 2. Mensajes de la tabla respuestas_solicitud
            CargarMensajesHilo(flpHilo, idSolicitud);

            if (flpHilo.Controls.Count == 0)
            {
                flpHilo.Controls.Add(new Label
                {
                    Text = "Sin mensajes aún",
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                    ForeColor = Color.FromArgb(100, 100, 120),
                    AutoSize = true,
                    Margin = new Padding(20, 10, 0, 0)
                });
            }

            // ═══════════════════════════════════
            // FILA 2: CAJA DE RESPUESTA / AVISO
            // ═══════════════════════════════════
            // 🔥 Solo se puede responder cuando el admin YA tomó una decisión
            bool permiteResponder =
                estadoLower.Contains("aprob") ||
                estadoLower.Contains("autoriz") ||
                estadoLower.Contains("rechaz") ||
                estadoLower.Contains("deneg");

            // 🚫 Bloquear si el estado es "pendiente", "visto" o "en revisión"
            if (estadoLower.Contains("pendiente") ||
                estadoLower.Contains("visto") ||
                estadoLower.Contains("revis"))
            {
                permiteResponder = false;
            }

            if (permiteResponder)
            {
                Panel pnlResp = CrearCajaRespuesta(idSolicitud);
                pnlResp.Dock = DockStyle.Fill;
                layout.Controls.Add(pnlResp, 0, 2);
            }
            else
            {
                // 🔥 Mostrar aviso de "esperando respuesta"
                Panel pnlAviso = CrearAvisoEsperandoRespuesta();
                pnlAviso.Dock = DockStyle.Fill;
                layout.Controls.Add(pnlAviso, 0, 2);
            }

            layout.Controls.Add(headerPanel, 0, 0);
            layout.Controls.Add(flpHilo, 0, 1);

            item.Controls.Add(layout);

            // 🔥 Calcular alto según contenido
            int altoHeader = 80;
            int altoResponder = 60;   // siempre hay fila 2 (responder o aviso)

            int altoHilo = 24;
            foreach (Control c in flpHilo.Controls)
                altoHilo += c.Height + c.Margin.Top + c.Margin.Bottom;
            if (altoHilo < 50) altoHilo = 50;

            item.Height = altoHeader + altoHilo + altoResponder + 12;

            return item;
        }

        // ═══════════════════════════════════
        // Aviso "esperando respuesta del admin"
        // ═══════════════════════════════════
        private Panel CrearAvisoEsperandoRespuesta()
        {
            Panel pnl = new Panel
            {
                Height = 60,
                BackColor = Color.FromArgb(30, 25, 15),   // tono amarillo oscuro
                Padding = new Padding(20, 10, 20, 10),
                Dock = DockStyle.Fill
            };
            AplicarBordesRedondeados(pnl, 8);

            Panel barra = new Panel
            {
                Width = 3,
                Dock = DockStyle.Left,
                BackColor = accentYellow
            };
            pnl.Controls.Add(barra);

            pnl.Controls.Add(new Label
            {
                Text = "⏳  Esperando respuesta del administrador...",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = accentYellow,
                Location = new Point(18, 12),
                AutoSize = true,
                BackColor = Color.Transparent
            });

            pnl.Controls.Add(new Label
            {
                Text = "Podrás responder cuando el admin revise tu solicitud.",
                Font = new Font("Segoe UI", 8f, FontStyle.Italic),
                ForeColor = Color.FromArgb(180, 160, 110),
                Location = new Point(20, 32),
                AutoSize = true,
                BackColor = Color.Transparent
            });

            return pnl;
        }

        // ═══════════════════════════════════
        // Cargar mensajes del hilo desde la BD
        // ═══════════════════════════════════
        private void CargarMensajesHilo(FlowLayoutPanel flpHilo, int idSolicitud)
        {
            Conexion conexionDB = new Conexion();
            try
            {
                NpgsqlConnection con = conexionDB.ObtenerConexion();

                string sql = @"
                    SELECT tipo_autor, nombre_autor, mensaje, fecha
                    FROM respuestas_solicitud
                    WHERE id_solicitud = @id
                    ORDER BY fecha ASC";

                using (NpgsqlCommand cmd = new NpgsqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@id", idSolicitud);

                    using (NpgsqlDataReader rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            string tipo = rd["tipo_autor"].ToString();
                            string nombre = rd["nombre_autor"]?.ToString() ?? "Usuario";
                            string mensaje = rd["mensaje"].ToString();
                            DateTime fecha = Convert.ToDateTime(rd["fecha"]);

                            bool esAdmin = tipo == "admin";
                            flpHilo.Controls.Add(CrearMensajeHilo(nombre, mensaje, esAdmin, fecha));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error cargar hilo: " + ex.Message);
            }
            finally
            {
                conexionDB.CerrarConexion();
            }
        }

        // ═══════════════════════════════════
        // Burbuja de mensaje tipo chat
        // ═══════════════════════════════════
        private Panel CrearMensajeHilo(string autor, string mensaje, bool esAdmin, DateTime? fecha = null)
        {
            Panel wrapper = new Panel
            {
                Width = 720,
                Height = 10,
                Margin = new Padding(0, 3, 0, 3),
                BackColor = Color.Transparent
            };

            Panel bubble = new Panel
            {
                Padding = new Padding(0),
                BackColor = esAdmin
                    ? Color.FromArgb(45, 35, 75)
                    : Color.FromArgb(30, 50, 75)
            };
            AplicarBordesRedondeados(bubble, 10);

            Panel barra = new Panel
            {
                Width = 3,
                Dock = DockStyle.Left,
                BackColor = esAdmin ? accentPurple : accentCyan
            };
            bubble.Controls.Add(barra);

            Label lblAutor = new Label
            {
                Text = (esAdmin ? "👨‍💼 " : "👤 ") + autor,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = esAdmin ? accentPurple : accentCyan,
                Location = new Point(14, 8),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            bubble.Controls.Add(lblAutor);

            Label lblFecha = null;
            if (fecha.HasValue)
            {
                lblFecha = new Label
                {
                    Text = fecha.Value.ToString("dd/MM HH:mm"),
                    Font = new Font("Segoe UI", 7f),
                    ForeColor = Color.FromArgb(140, 140, 170),
                    AutoSize = true,
                    BackColor = Color.Transparent
                };
                bubble.Controls.Add(lblFecha);
            }

            Label lblMsg = new Label
            {
                Text = mensaje,
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = textWhite,
                Location = new Point(14, 28),
                MaximumSize = new Size(460, 0),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            bubble.Controls.Add(lblMsg);

            int anchoMsg = lblMsg.PreferredWidth;
            if (anchoMsg > 460) anchoMsg = 460;

            int anchoBurbuja = Math.Max(200, anchoMsg + 40);
            int altoBurbuja = 28 + lblMsg.PreferredHeight + 12;

            bubble.Size = new Size(anchoBurbuja, altoBurbuja);

            if (lblFecha != null)
            {
                lblFecha.Location = new Point(
                    bubble.Width - lblFecha.PreferredWidth - 12,
                    10);
            }

            if (esAdmin)
                bubble.Location = new Point(0, 0);
            else
                bubble.Location = new Point(wrapper.Width - anchoBurbuja, 0);

            wrapper.Height = altoBurbuja;
            wrapper.Controls.Add(bubble);

            return wrapper;
        }

        // ═══════════════════════════════════
        // Caja de respuesta (input + enviar)
        // ═══════════════════════════════════
        private Panel CrearCajaRespuesta(int idSolicitud)
        {
            Panel pnl = new Panel
            {
                Height = 60,
                BackColor = Color.FromArgb(20, 20, 30),
                Padding = new Padding(12, 10, 12, 10),
                Dock = DockStyle.Fill
            };

            Button btn = new Button
            {
                Text = "➤ Enviar",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = textWhite,
                BackColor = accentPurple,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(110, 36),
                Dock = DockStyle.Right,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(155, 125, 255);
            AplicarBordesRedondeados(btn, 8);
            pnl.Controls.Add(btn);

            Panel panelTxt = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(35, 35, 50),
                Padding = new Padding(14, 0, 14, 0),
                Margin = new Padding(0, 0, 10, 0)
            };
            AplicarBordesRedondeados(panelTxt, 8);
            pnl.Controls.Add(panelTxt);
            panelTxt.BringToFront();

            TextBox txt = new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(35, 35, 50),
                ForeColor = textWhite,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10f),
                PlaceholderText = "Escribe una respuesta al admin..."
            };
            panelTxt.Controls.Add(txt);

            btn.Margin = new Padding(10, 0, 0, 0);

            Action enviar = () =>
            {
                string msg = txt.Text.Trim();
                if (string.IsNullOrWhiteSpace(msg))
                {
                    MessageBox.Show("Escribe un mensaje.", "Vacío",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (EnviarRespuesta(idSolicitud, msg))
                {
                    txt.Clear();
                    CargarSolicitudes();
                }
            };

            btn.Click += (s, e) => enviar();
            txt.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && !e.Shift)
                {
                    e.SuppressKeyPress = true;
                    enviar();
                }
            };

            return pnl;
        }

        // ═══════════════════════════════════
        // Insertar respuesta en la BD
        // ═══════════════════════════════════
        private bool EnviarRespuesta(int idSolicitud, string mensaje)
        {
            Conexion conexionDB = new Conexion();
            try
            {
                NpgsqlConnection con = conexionDB.ObtenerConexion();

                string sql = @"
                    INSERT INTO respuestas_solicitud 
                        (id_solicitud, id_autor, tipo_autor, nombre_autor, mensaje, fecha)
                    VALUES 
                        (@idSol, @idAutor, 'trabajador', @nombre, @mensaje, NOW())";

                using (NpgsqlCommand cmd = new NpgsqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@idSol", idSolicitud);
                    cmd.Parameters.AddWithValue("@idAutor", idTrabajador);
                    cmd.Parameters.AddWithValue("@nombre", nombreTrabajador);
                    cmd.Parameters.AddWithValue("@mensaje", mensaje);

                    cmd.ExecuteNonQuery();
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al enviar mensaje:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            finally
            {
                conexionDB.CerrarConexion();
            }
        }

        // ═══════════════════════════════════════════
        // UTILIDAD
        // ═══════════════════════════════════════════
        private void AplicarBordesRedondeados(Control control, int radio)
        {
            Action recalcularRegion = () =>
            {
                if (control.Width <= 0 || control.Height <= 0) return;
                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddArc(0, 0, radio, radio, 180, 90);
                    path.AddArc(control.Width - radio, 0, radio, radio, 270, 90);
                    path.AddArc(control.Width - radio, control.Height - radio, radio, radio, 0, 90);
                    path.AddArc(0, control.Height - radio, radio, radio, 90, 90);
                    path.CloseAllFigures();
                    control.Region = new Region(path);
                }
            };
            control.Resize += (s, e) => recalcularRegion();
            recalcularRegion();
        }
    }
}