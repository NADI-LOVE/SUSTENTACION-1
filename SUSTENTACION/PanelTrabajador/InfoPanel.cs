using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Npgsql;

namespace SUSTENTACION.PanelTrabajador
{
    public class InfoPanel : UserControl
    {
        // ═══════════════════════════════════════════
        // PALETA
        // ═══════════════════════════════════════════
        private readonly Color bgMain = Color.FromArgb(15, 15, 20);
        private readonly Color bgCard = Color.FromArgb(25, 25, 35);
        private readonly Color bgRow = Color.FromArgb(32, 32, 45);
        private readonly Color accentOrange = Color.FromArgb(255, 87, 34);
        private readonly Color accentPurple = Color.FromArgb(156, 39, 176);
        private readonly Color accentCyan = Color.FromArgb(0, 188, 212);
        private readonly Color accentYellow = Color.FromArgb(255, 193, 7);
        private readonly Color accentGreen = Color.FromArgb(46, 204, 148);
        private readonly Color textWhite = Color.White;
        private readonly Color textMuted = Color.FromArgb(150, 150, 160);

        // ═══════════════════════════════════════════
        // DATOS
        // ═══════════════════════════════════════════
        private int idTrabajador;
        private string nombreTrabajador;
        private DateTime mesSeleccionado = DateTime.Now;

        private int salidasDelMes = 0;
        private int salidasRetornadas = 0;
        private int salidasActivas = 0;
        private int equiposAsignadosActivos = 0;
        private int equiposDisponibles = 0;
        private int equiposEnEvento = 0;
        private int totalEventosDelMes = 0;
        private int[] salidasPorMes = new int[12];

        // Paneles
        private Panel pnlLineChart;
        private Panel pnlDona1;
        private Panel pnlDona2;
        private Panel pnlBarras;
        private Panel pnlTarjeta;
        private FlowLayoutPanel pnlMeses;

        // KPIs (referencias para actualizar)
        private Label lblKpiSalidas, lblKpiActivas, lblKpiRetornadas, lblKpiEquipos;

        public InfoPanel(int idTrabajador = 1, string nombreTrabajador = "Trabajador")
        {
            this.idTrabajador = idTrabajador;
            this.nombreTrabajador = nombreTrabajador;

            InitializeComponentes();
            CargarDatosReales();
            RefrescarGraficos();
        }

        // ═══════════════════════════════════════════
        // INICIALIZACIÓN
        // ═══════════════════════════════════════════
        private void InitializeComponentes()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = bgMain;
            this.Padding = new Padding(15);

            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = Color.Transparent
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));   // Header
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));   // Meses
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100f));  // KPIs
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));   // Contenido

            // ─── HEADER ───
            Panel headerPanel = CrearHeader();
            mainLayout.Controls.Add(headerPanel, 0, 0);

            // ─── MESES ───
            pnlMeses = CrearPanelMeses();
            mainLayout.Controls.Add(pnlMeses, 0, 1);

            // ─── KPIs ───
            TableLayoutPanel kpiLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 5, 0, 10)
            };
            for (int i = 0; i < 4; i++)
                kpiLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

            kpiLayout.Controls.Add(CrearKPI("📤", "Salidas del mes", "0", accentOrange, out lblKpiSalidas), 0, 0);
            kpiLayout.Controls.Add(CrearKPI("🔄", "Activas", "0", accentYellow, out lblKpiActivas), 1, 0);
            kpiLayout.Controls.Add(CrearKPI("✅", "Retornadas", "0", accentGreen, out lblKpiRetornadas), 2, 0);
            kpiLayout.Controls.Add(CrearKPI("📦", "Equipos disponibles", "0", accentCyan, out lblKpiEquipos), 3, 0);

            mainLayout.Controls.Add(kpiLayout, 0, 2);

            // ─── CONTENIDO ───
            TableLayoutPanel contentLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f));
            contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));

            // ─── COLUMNA IZQUIERDA (gráficos) ───
            TableLayoutPanel leftColumn = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.Transparent
            };
            leftColumn.RowStyles.Add(new RowStyle(SizeType.Percent, 35f));
            leftColumn.RowStyles.Add(new RowStyle(SizeType.Percent, 35f));
            leftColumn.RowStyles.Add(new RowStyle(SizeType.Percent, 30f));

            pnlLineChart = CrearPanelGrafico("Salidas durante el año", "📈");
            pnlLineChart.Paint += (s, e) => DibujarGraficoLineas(e.Graphics, pnlLineChart.ClientRectangle);
            leftColumn.Controls.Add(pnlLineChart, 0, 0);

            TableLayoutPanel pnlDonaLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            pnlDonaLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            pnlDonaLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

            pnlDona1 = CrearPanelGrafico("Estado de salidas del mes", "🍩");
            pnlDona1.Paint += (s, e) => DibujarDona(
                e.Graphics, pnlDona1.ClientRectangle,
                salidasActivas, salidasRetornadas,
                accentPurple, accentGreen);

            pnlDona2 = CrearPanelGrafico("Equipos a mi cargo", "📊");
            pnlDona2.Paint += (s, e) => DibujarDona(
                e.Graphics, pnlDona2.ClientRectangle,
                equiposEnEvento, equiposDisponibles,
                accentOrange, accentCyan);

            pnlDonaLayout.Controls.Add(pnlDona1, 0, 0);
            pnlDonaLayout.Controls.Add(pnlDona2, 1, 0);
            leftColumn.Controls.Add(pnlDonaLayout, 0, 1);

            pnlBarras = CrearPanelGrafico("Actividad del mes", "🎯");
            pnlBarras.Paint += (s, e) => DibujarBarrasCirculares(
                e.Graphics, pnlBarras.ClientRectangle,
                salidasDelMes, totalEventosDelMes);
            leftColumn.Controls.Add(pnlBarras, 0, 2);

            contentLayout.Controls.Add(leftColumn, 0, 0);

            // ─── COLUMNA DERECHA (perfil + personal externo) ───
            TableLayoutPanel rightColumn = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent
            };
            rightColumn.RowStyles.Add(new RowStyle(SizeType.Percent, 60f));   // Perfil
            rightColumn.RowStyles.Add(new RowStyle(SizeType.Percent, 40f));   // Personal externo

            // ═══════════════════════════════════════════
            // PANEL DE PERFIL (datos de la cuenta)
            // ═══════════════════════════════════════════
            Panel pnlPerfil = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = bgCard,
                Margin = new Padding(5),
                Padding = new Padding(18)
            };
            AplicarBordesRedondeados(pnlPerfil, 10);

            Label lblPerfilTitle = new Label
            {
                Text = "👤  Mi cuenta",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = textWhite,
                Dock = DockStyle.Top,
                Height = 32,
                Padding = new Padding(2, 4, 0, 0)
            };
            pnlPerfil.Controls.Add(lblPerfilTitle);

            // Contenedor scrollable de campos
            FlowLayoutPanel flpPerfil = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Color.Transparent,
                Name = "flpPerfil"
            };
            pnlPerfil.Controls.Add(flpPerfil);
            flpPerfil.BringToFront();

            rightColumn.Controls.Add(pnlPerfil, 0, 0);

            // ═══════════════════════════════════════════
            // PANEL DE PERSONAL EXTERNO
            // ═══════════════════════════════════════════
            Panel pnlPersonalExt = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = bgCard,
                Margin = new Padding(5),
                Padding = new Padding(12, 8, 12, 12)
            };
            AplicarBordesRedondeados(pnlPersonalExt, 10);

            Label lblExtTitle = new Label
            {
                Text = "🧑‍🔧  Personal externo",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = textWhite,
                Dock = DockStyle.Top,
                Height = 32,
                Padding = new Padding(4, 4, 0, 0)
            };
            pnlPersonalExt.Controls.Add(lblExtTitle);

            FlowLayoutPanel flpPersonalExt = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Color.Transparent,
                Name = "flpPersonalExt"
            };
            pnlPersonalExt.Controls.Add(flpPersonalExt);
            flpPersonalExt.BringToFront();

            rightColumn.Controls.Add(pnlPersonalExt, 0, 1);

            contentLayout.Controls.Add(rightColumn, 1, 0);

            mainLayout.Controls.Add(contentLayout, 0, 3);

            this.Controls.Add(mainLayout);
        }

        // ═══════════════════════════════════════════
        // HEADER CON AVATAR
        // ═══════════════════════════════════════════
        private Panel CrearHeader()
        {
            Panel headerPanel = new Panel { Dock = DockStyle.Fill, BackColor = bgCard };
            AplicarBordesRedondeados(headerPanel, 8);

            Panel avatar = new Panel
            {
                Size = new Size(36, 36),
                Location = new Point(18, 7),
                BackColor = accentOrange
            };
            AplicarBordesRedondeados(avatar, 18);
            avatar.Controls.Add(new Label
            {
                Text = "👤",
                Font = new Font("Segoe UI", 14f),
                ForeColor = textWhite,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            });
            headerPanel.Controls.Add(avatar);

            headerPanel.Controls.Add(new Label
            {
                Text = "Dashboard",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = textWhite,
                Location = new Point(64, 8),
                AutoSize = true,
                BackColor = Color.Transparent
            });

            headerPanel.Controls.Add(new Label
            {
                Text = "Bienvenido, " + nombreTrabajador + " · " + DateTime.Now.ToString("dddd, dd 'de' MMMM 'de' yyyy"),
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = textMuted,
                Location = new Point(64, 28),
                AutoSize = true,
                BackColor = Color.Transparent
            });

            Panel badge = new Panel
            {
                Size = new Size(90, 24),
                BackColor = Color.FromArgb(20, 60, 40),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            badge.Location = new Point(headerPanel.Width - 110, 13);
            AplicarBordesRedondeados(badge, 12);
            badge.Controls.Add(new Label
            {
                Text = "● En línea",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = accentGreen,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            });
            headerPanel.Controls.Add(badge);
            headerPanel.Resize += (s, e) => badge.Left = headerPanel.Width - badge.Width - 18;

            return headerPanel;
        }

        // ═══════════════════════════════════════════
        // PANEL DE MESES
        // ═══════════════════════════════════════════
        private FlowLayoutPanel CrearPanelMeses()
        {
            FlowLayoutPanel pnl = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 5, 0, 5)
            };

            string[] meses = { "ENE", "FEB", "MAR", "ABR", "MAY", "JUN",
                              "JUL", "AGO", "SEP", "OCT", "NOV", "DIC" };

            for (int i = 0; i < 12; i++)
            {
                int mesIndex = i;
                bool esMesActual = (i == DateTime.Now.Month - 1);

                Button btnMes = new Button
                {
                    Text = meses[i],
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    ForeColor = esMesActual ? textWhite : textMuted,
                    BackColor = esMesActual ? accentOrange : bgRow,
                    FlatStyle = FlatStyle.Flat,
                    Height = 28,
                    Width = 68,
                    Margin = new Padding(2, 0, 2, 0),
                    Cursor = Cursors.Hand,
                    Tag = mesIndex
                };
                btnMes.FlatAppearance.BorderSize = 0;
                btnMes.FlatAppearance.MouseOverBackColor = Color.FromArgb(45, 45, 60);
                AplicarBordesRedondeados(btnMes, 14);

                btnMes.Click += (s, e) =>
                {
                    mesSeleccionado = new DateTime(DateTime.Now.Year, mesIndex + 1, 1);

                    foreach (Control c in pnlMeses.Controls)
                    {
                        if (c is Button b)
                        {
                            int tag = (int)b.Tag;
                            bool activo = tag == mesIndex;
                            b.BackColor = activo ? accentOrange : bgRow;
                            b.ForeColor = activo ? textWhite : textMuted;
                        }
                    }

                    CargarDatosReales();
                    RefrescarGraficos();
                };

                pnl.Controls.Add(btnMes);
            }

            return pnl;
        }

        // ═══════════════════════════════════════════
        // CARGA DE DATOS
        // ═══════════════════════════════════════════
        private void CargarDatosReales()
        {
            Conexion conexionDB = new Conexion();
            try
            {
                NpgsqlConnection con = conexionDB.ObtenerConexion();

                int mes = mesSeleccionado.Month;
                int anio = mesSeleccionado.Year;

                // 1. Salidas del mes
                string sqlSalidasMes = @"
                    SELECT 
                        COUNT(*) AS total,
                        COUNT(CASE WHEN estado = 'Devuelto' THEN 1 END) AS retornadas,
                        COUNT(CASE WHEN estado IS NULL OR estado <> 'Devuelto' THEN 1 END) AS activas
                    FROM salidas
                    WHERE id_trabajador = @idTrab
                      AND EXTRACT(MONTH FROM fecha_salida) = @mes
                      AND EXTRACT(YEAR FROM fecha_salida) = @anio";

                using (NpgsqlCommand cmd = new NpgsqlCommand(sqlSalidasMes, con))
                {
                    cmd.Parameters.AddWithValue("@idTrab", idTrabajador);
                    cmd.Parameters.AddWithValue("@mes", mes);
                    cmd.Parameters.AddWithValue("@anio", anio);

                    using (NpgsqlDataReader rd = cmd.ExecuteReader())
                    {
                        if (rd.Read())
                        {
                            salidasDelMes = Convert.ToInt32(rd["total"]);
                            salidasRetornadas = Convert.ToInt32(rd["retornadas"]);
                            salidasActivas = Convert.ToInt32(rd["activas"]);
                        }
                    }
                }

                // 2. Salidas por mes (gráfico líneas)
                string sqlPorMes = @"
                    SELECT EXTRACT(MONTH FROM fecha_salida)::int AS mes, COUNT(*) AS total
                    FROM salidas
                    WHERE id_trabajador = @idTrab
                      AND EXTRACT(YEAR FROM fecha_salida) = @anio
                    GROUP BY EXTRACT(MONTH FROM fecha_salida)
                    ORDER BY mes";

                for (int i = 0; i < 12; i++) salidasPorMes[i] = 0;

                using (NpgsqlCommand cmd = new NpgsqlCommand(sqlPorMes, con))
                {
                    cmd.Parameters.AddWithValue("@idTrab", idTrabajador);
                    cmd.Parameters.AddWithValue("@anio", anio);

                    using (NpgsqlDataReader rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            int m = Convert.ToInt32(rd["mes"]);
                            salidasPorMes[m - 1] = Convert.ToInt32(rd["total"]);
                        }
                    }
                }

                // 3. Equipos a cargo
                string sqlEquiposCargo = @"
                    SELECT COUNT(DISTINCT se.id_equipo) AS total
                    FROM salida_equipos se
                    INNER JOIN salidas s ON s.id_salida = se.id_salida
                    WHERE s.id_trabajador = @idTrab
                      AND (s.estado IS NULL OR s.estado <> 'Devuelto')";

                using (NpgsqlCommand cmd = new NpgsqlCommand(sqlEquiposCargo, con))
                {
                    cmd.Parameters.AddWithValue("@idTrab", idTrabajador);
                    object r = cmd.ExecuteScalar();
                    equiposEnEvento = r != null ? Convert.ToInt32(r) : 0;
                }

                // 4. Equipos disponibles
                string sqlDisponibles = @"
                    SELECT COALESCE(SUM(stock), 0) FROM equipos 
                    WHERE LOWER(TRIM(estado)) = 'disponible'";
                using (NpgsqlCommand cmd = new NpgsqlCommand(sqlDisponibles, con))
                {
                    object r = cmd.ExecuteScalar();
                    equiposDisponibles = r != null ? Convert.ToInt32(r) : 0;
                }

                equiposAsignadosActivos = equiposEnEvento;

                // 5. Eventos del mes
                string sqlEventosMes = @"
                    SELECT COUNT(DISTINCT e.id)
                    FROM eventos e
                    INNER JOIN salidas s ON s.id_evento = e.id
                    WHERE s.id_trabajador = @idTrab
                      AND EXTRACT(MONTH FROM e.fecha) = @mes
                      AND EXTRACT(YEAR FROM e.fecha) = @anio";

                using (NpgsqlCommand cmd = new NpgsqlCommand(sqlEventosMes, con))
                {
                    cmd.Parameters.AddWithValue("@idTrab", idTrabajador);
                    cmd.Parameters.AddWithValue("@mes", mes);
                    cmd.Parameters.AddWithValue("@anio", anio);
                    object r = cmd.ExecuteScalar();
                    totalEventosDelMes = r != null ? Convert.ToInt32(r) : 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error InfoPanel: " + ex.Message);
            }
            finally
            {
                conexionDB.CerrarConexion();
            }

            ActualizarKPIs();
            CargarPerfil();
            CargarPersonalExterno();
        }

        private void ActualizarKPIs()
        {
            if (lblKpiSalidas != null) lblKpiSalidas.Text = salidasDelMes.ToString();
            if (lblKpiActivas != null) lblKpiActivas.Text = salidasActivas.ToString();
            if (lblKpiRetornadas != null) lblKpiRetornadas.Text = salidasRetornadas.ToString();
            if (lblKpiEquipos != null) lblKpiEquipos.Text = equiposDisponibles.ToString();
        }

        // ═══════════════════════════════════════════════════════════
        // PERFIL DEL TRABAJADOR (nombre, correo, contraseña, celular)
        // ═══════════════════════════════════════════════════════════
        private void CargarPerfil()
        {
            var found = this.Controls.Find("flpPerfil", true);
            if (found.Length == 0) return;
            var flp = found[0] as FlowLayoutPanel;
            if (flp == null) return;

            flp.Controls.Clear();

            Conexion conexionDB = new Conexion();
            try
            {
                NpgsqlConnection con = conexionDB.ObtenerConexion();

                string sql = @"
            SELECT 
                t.nombre,
                COALESCE(t.correo, '') AS correo,
                COALESCE(t.password, '') AS contrasena,
                COALESCE(t.telefono, '') AS celular,
                COALESCE(t.dni, '') AS dni,
                COALESCE(r.nombre_rol, 'Sin rol') AS rol
            FROM trabajadores t
            LEFT JOIN roles r ON r.id_rol = t.id_rol
            WHERE t.id_trabajador = @id
            LIMIT 1";

                using (NpgsqlCommand cmd = new NpgsqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@id", idTrabajador);

                    using (NpgsqlDataReader rd = cmd.ExecuteReader())
                    {
                        if (rd.Read())
                        {
                            string nombre = rd["nombre"].ToString();
                            string correo = rd["correo"].ToString();
                            string contrasena = rd["contrasena"].ToString();
                            string celular = rd["celular"].ToString();
                            string dni = rd["dni"].ToString();
                            string rol = rd["rol"].ToString();

                            // 🎭 Rol
                            flp.Controls.Add(CrearCampoPerfil_Rol(rol));

                            // 👤 Nombre
                            flp.Controls.Add(CrearCampoPerfil("👤", "Nombre completo", nombre, false));

                            // ✉ Correo
                            flp.Controls.Add(CrearCampoPerfil("✉", "Correo electrónico",
                                string.IsNullOrWhiteSpace(correo) ? "Sin correo" : correo, false));

                            // 📱 Celular
                            flp.Controls.Add(CrearCampoPerfil("📱", "Celular",
                                string.IsNullOrWhiteSpace(celular) ? "Sin celular" : celular, false));

                            // 🪪 DNI (solo si existe)
                            if (!string.IsNullOrWhiteSpace(dni))
                            {
                                flp.Controls.Add(CrearCampoPerfil("🪪", "DNI", dni, false));
                            }

                            // 🔒 Contraseña
                            flp.Controls.Add(CrearCampoPerfil_Password(contrasena));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error perfil: " + ex.Message);
            }
            finally
            {
                conexionDB.CerrarConexion();
            }

            // Separador
            flp.Controls.Add(new Panel
            {
                Width = 380,
                Height = 1,
                BackColor = Color.FromArgb(45, 45, 60),
                Margin = new Padding(0, 8, 0, 8)
            });

            // Botón solicitar cambio
            Button btnSolicitar = new Button
            {
                Text = "📩  Solicitar cambio de datos al administrador",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = accentOrange,
                BackColor = Color.FromArgb(45, 25, 20),
                FlatStyle = FlatStyle.Flat,
                Width = 380,
                Height = 40,
                Cursor = Cursors.Hand
            };
            btnSolicitar.FlatAppearance.BorderColor = accentOrange;
            btnSolicitar.FlatAppearance.BorderSize = 1;
            AplicarBordesRedondeados(btnSolicitar, 8);
            btnSolicitar.Click += (s, e) =>
            {
                using (var form = new FormSolicitarCambio(idTrabajador, nombreTrabajador))
                {
                    if (form.ShowDialog() == DialogResult.OK)
                    {
                        CargarPerfil();
                    }
                }
            };
            flp.Controls.Add(btnSolicitar);
        }

        private Panel CrearCampoPerfil(string icono, string etiqueta, string valor, bool esPassword)
        {
            Panel item = new Panel
            {
                Width = 380,
                Height = 60,
                Margin = new Padding(0, 4, 0, 4),
                BackColor = bgRow
            };
            AplicarBordesRedondeados(item, 8);

            // Ícono
            item.Controls.Add(new Label
            {
                Text = icono,
                Font = new Font("Segoe UI", 14f),
                ForeColor = accentCyan,
                Location = new Point(14, 18),
                AutoSize = true,
                BackColor = Color.Transparent
            });

            // Etiqueta
            item.Controls.Add(new Label
            {
                Text = etiqueta.ToUpper(),
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = textMuted,
                Location = new Point(50, 10),
                AutoSize = true,
                BackColor = Color.Transparent
            });

            // Valor
            item.Controls.Add(new Label
            {
                Text = valor,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = textWhite,
                Location = new Point(50, 28),
                AutoSize = true,
                BackColor = Color.Transparent,
                MaximumSize = new Size(310, 0)
            });

            return item;
        }

        private Panel CrearCampoPerfil_Rol(string rol)
        {
            Panel item = new Panel
            {
                Width = 380,
                Height = 44,
                Margin = new Padding(0, 4, 0, 4),
                BackColor = Color.FromArgb(40, 25, 55)
            };
            AplicarBordesRedondeados(item, 8);

            item.Controls.Add(new Label
            {
                Text = "🎭",
                Font = new Font("Segoe UI", 14f),
                ForeColor = accentPurple,
                Location = new Point(14, 10),
                AutoSize = true,
                BackColor = Color.Transparent
            });

            item.Controls.Add(new Label
            {
                Text = "ROL",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = textMuted,
                Location = new Point(50, 6),
                AutoSize = true,
                BackColor = Color.Transparent
            });

            item.Controls.Add(new Label
            {
                Text = rol,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = accentPurple,
                Location = new Point(50, 22),
                AutoSize = true,
                BackColor = Color.Transparent
            });

            return item;
        }

        private Panel CrearCampoPerfil_Password(string contrasena)
        {
            Panel item = new Panel
            {
                Width = 380,
                Height = 60,
                Margin = new Padding(0, 4, 0, 4),
                BackColor = bgRow
            };
            AplicarBordesRedondeados(item, 8);

            item.Controls.Add(new Label
            {
                Text = "🔒",
                Font = new Font("Segoe UI", 14f),
                ForeColor = accentYellow,
                Location = new Point(14, 18),
                AutoSize = true,
                BackColor = Color.Transparent
            });

            item.Controls.Add(new Label
            {
                Text = "CONTRASEÑA",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = textMuted,
                Location = new Point(50, 10),
                AutoSize = true,
                BackColor = Color.Transparent
            });

            // 🔒 Solo asteriscos según la cantidad de caracteres
            int largo = string.IsNullOrEmpty(contrasena) ? 8 : contrasena.Length;
            if (largo > 24) largo = 24;   // tope visual para no desbordar

            string asteriscos = new string('•', largo);

            item.Controls.Add(new Label
            {
                Text = asteriscos,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = textMuted,
                Location = new Point(50, 26),
                AutoSize = true,
                BackColor = Color.Transparent
            });

            // Botón "Cambiar" a la derecha
            Button btnCambiar = new Button
            {
                Text = "🔑  Cambiar",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = textWhite,
                BackColor = Color.FromArgb(50, 50, 70),
                FlatStyle = FlatStyle.Flat,
                Width = 110,
                Height = 30,
                Location = new Point(255, 15),
                Cursor = Cursors.Hand
            };
            btnCambiar.FlatAppearance.BorderSize = 0;
            btnCambiar.FlatAppearance.MouseOverBackColor = Color.FromArgb(70, 70, 90);
            AplicarBordesRedondeados(btnCambiar, 6);
            btnCambiar.Click += (s, e) => MostrarDialogoCambiarPassword();
            item.Controls.Add(btnCambiar);

            return item;
        }

        private void MostrarDialogoCambiarPassword()
        {
            MessageBox.Show(
                "🔑 Cambio de contraseña\n\n" +
                "Por seguridad, debes solicitar el cambio al administrador.\n\n" +
                "El admin verificará tu identidad y actualizará tu contraseña.",
                "Cambiar contraseña",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void MostrarDialogoSolicitud()
        {
            var resp = MessageBox.Show(
                "¿Deseas enviar una solicitud al administrador para cambiar tus datos?\n\n" +
                "El admin revisará tu solicitud y te contactará.",
                "Solicitar cambio de datos",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (resp == DialogResult.Yes)
            {
                MessageBox.Show(
                    "✅ Solicitud enviada correctamente.\n\n" +
                    "El administrador será notificado.",
                    "Solicitud enviada",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        // ═══════════════════════════════════════════
        // 🔥 PERSONAL EXTERNO (nombre + celular)
        // ═══════════════════════════════════════════
        private void CargarPersonalExterno()
        {
            var found = this.Controls.Find("flpPersonalExt", true);
            if (found.Length == 0) return;

            var flp = found[0] as FlowLayoutPanel;
            if (flp == null) return;

            flp.Controls.Clear();

            Conexion conexionDB = new Conexion();
            try
            {
                NpgsqlConnection con = conexionDB.ObtenerConexion();

                // 🔥 CORREGIDO: consulta a personal_externo, no a trabajadores
                string sql = @"
            SELECT nombre, apellido, celular
            FROM personal_externo
            WHERE activo = TRUE
            ORDER BY nombre, apellido";

                using (NpgsqlCommand cmd = new NpgsqlCommand(sql, con))
                using (NpgsqlDataReader rd = cmd.ExecuteReader())
                {
                    bool hay = false;
                    while (rd.Read())
                    {
                        hay = true;
                        string nombre = rd["nombre"]?.ToString() ?? "";
                        string apellido = rd["apellido"]?.ToString() ?? "";
                        string celular = rd["celular"]?.ToString() ?? "";

                        flp.Controls.Add(CrearItemPersonalExterno(nombre, apellido, celular));
                    }

                    if (!hay)
                    {
                        flp.Controls.Add(new Label
                        {
                            Text = "Sin personal externo registrado",
                            Font = new Font("Segoe UI", 9f, FontStyle.Italic),
                            ForeColor = textMuted,
                            AutoSize = true,
                            Margin = new Padding(10, 20, 0, 0)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error personal externo: " + ex.Message);
            }
            finally
            {
                conexionDB.CerrarConexion();
            }
        }

        private Panel CrearItemPersonalExterno(string nombre, string apellido, string celular)
        {
            Panel item = new Panel
            {
                Width = 380,
                Height = 52,
                Margin = new Padding(0, 4, 0, 4),
                BackColor = bgRow
            };
            AplicarBordesRedondeados(item, 8);

            // Barra de acento izquierda
            Panel barra = new Panel
            {
                Width = 4,
                Dock = DockStyle.Left,
                BackColor = accentPurple
            };
            item.Controls.Add(barra);

            // Avatar pequeño
            Panel avatar = new Panel
            {
                Size = new Size(32, 32),
                Location = new Point(14, 10),
                BackColor = accentPurple
            };
            AplicarBordesRedondeados(avatar, 16);
            avatar.Controls.Add(new Label
            {
                Text = "🧑‍🔧",
                Font = new Font("Segoe UI", 12f),
                ForeColor = textWhite,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            });
            item.Controls.Add(avatar);

            // Nombre completo
            Label lblNombre = new Label
            {
                Text = $"{nombre} {apellido}".Trim(),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = textWhite,
                Location = new Point(56, 10),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            item.Controls.Add(lblNombre);

            // Celular
            Label lblCelular = new Label
            {
                Text = "📱  " + (string.IsNullOrWhiteSpace(celular) ? "Sin teléfono" : celular),
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = string.IsNullOrWhiteSpace(celular) ? textMuted : accentGreen,
                Location = new Point(56, 28),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            item.Controls.Add(lblCelular);

            return item;
        }

        // ═══════════════════════════════════════════
        // KPI CARD
        // ═══════════════════════════════════════════
        private Panel CrearKPI(string icono, string titulo, string valor, Color colorAcento, out Label lblValOut)
        {
            Panel card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = bgCard,
                Margin = new Padding(5)
            };
            AplicarBordesRedondeados(card, 10);
            card.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(45, 45, 60), 1f))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (var p = new GraphicsPath())
                    {
                        int r = 10;
                        p.AddArc(0, 0, r, r, 180, 90);
                        p.AddArc(card.Width - r - 1, 0, r, r, 270, 90);
                        p.AddArc(card.Width - r - 1, card.Height - r - 1, r, r, 0, 90);
                        p.AddArc(0, card.Height - r - 1, r, r, 90, 90);
                        p.CloseAllFigures();
                        e.Graphics.DrawPath(pen, p);
                    }
                }
            };

            Panel barra = new Panel
            {
                Width = 4,
                Dock = DockStyle.Left,
                BackColor = colorAcento
            };
            card.Controls.Add(barra);

            card.Controls.Add(new Label
            {
                Text = icono,
                Font = new Font("Segoe UI", 20f),
                ForeColor = colorAcento,
                Location = new Point(16, 14),
                AutoSize = true,
                BackColor = Color.Transparent
            });

            card.Controls.Add(new Label
            {
                Text = titulo.ToUpper(),
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = textMuted,
                Location = new Point(60, 12),
                AutoSize = true,
                BackColor = Color.Transparent
            });

            Label lblVal = new Label
            {
                Text = valor,
                Font = new Font("Segoe UI", 22f, FontStyle.Bold),
                ForeColor = textWhite,
                Location = new Point(58, 28),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            card.Controls.Add(lblVal);

            lblValOut = lblVal;
            return card;
        }

        // ═══════════════════════════════════════════
        // PANEL DE GRÁFICOS
        // ═══════════════════════════════════════════
        private Panel CrearPanelGrafico(string titulo, string icono)
        {
            Panel pnl = new Panel { Dock = DockStyle.Fill, BackColor = bgCard, Margin = new Padding(5) };
            AplicarBordesRedondeados(pnl, 10);

            Label lblTitle = new Label
            {
                Text = $"{icono}  {titulo}",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = textWhite,
                Dock = DockStyle.Top,
                Height = 28,
                Padding = new Padding(12, 8, 0, 0)
            };
            pnl.Controls.Add(lblTitle);
            return pnl;
        }

        private void RefrescarGraficos()
        {
            pnlLineChart?.Invalidate();
            pnlDona1?.Invalidate();
            pnlDona2?.Invalidate();
            pnlBarras?.Invalidate();
            pnlTarjeta?.Invalidate();
        }

        // ═══════════════════════════════════════════
        // DIBUJO DE GRÁFICOS
        // ═══════════════════════════════════════════
        private void DibujarGraficoLineas(Graphics g, Rectangle rect)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int margenIzq = 40;
            int margenDer = 20;
            int margenSup = 35;
            int margenInf = 30;

            int ancho = rect.Width - margenIzq - margenDer;
            int alto = rect.Height - margenSup - margenInf;

            Pen gridPen = new Pen(Color.FromArgb(40, 40, 55), 1);
            for (int i = 0; i <= 4; i++)
            {
                int y = margenSup + (alto / 4) * i;
                g.DrawLine(gridPen, margenIzq, y, margenIzq + ancho, y);
            }

            int maxVal = 1;
            foreach (var v in salidasPorMes) if (v > maxVal) maxVal = v;

            string[] mesesCortos = { "E", "F", "M", "A", "M", "J", "J", "A", "S", "O", "N", "D" };
            int espaciado = ancho / 11;

            Point[] puntos = new Point[12];
            for (int i = 0; i < 12; i++)
            {
                int x = margenIzq + (espaciado * i);
                int y = margenSup + alto - (int)(((float)salidasPorMes[i] / maxVal) * alto * 0.9f);
                puntos[i] = new Point(x, y);

                g.DrawString(mesesCortos[i], new Font("Segoe UI", 7f),
                    new SolidBrush(textMuted), x - 3, margenSup + alto + 8);
            }

            if (puntos.Length > 1)
            {
                var areaPuntos = new Point[puntos.Length + 2];
                Array.Copy(puntos, areaPuntos, puntos.Length);
                areaPuntos[puntos.Length] = new Point(puntos[puntos.Length - 1].X, margenSup + alto);
                areaPuntos[puntos.Length + 1] = new Point(puntos[0].X, margenSup + alto);

                using (var brush = new LinearGradientBrush(
                    new Rectangle(0, margenSup, rect.Width, alto),
                    Color.FromArgb(80, accentOrange),
                    Color.FromArgb(0, accentOrange),
                    LinearGradientMode.Vertical))
                {
                    g.FillPolygon(brush, areaPuntos);
                }

                g.DrawLines(new Pen(accentOrange, 2.5f), puntos);

                foreach (var p in puntos)
                {
                    g.FillEllipse(new SolidBrush(accentOrange), p.X - 4, p.Y - 4, 8, 8);
                    g.FillEllipse(new SolidBrush(bgCard), p.X - 2, p.Y - 2, 4, 4);
                }
            }

            g.DrawString($"Máx: {maxVal}", new Font("Segoe UI", 7f),
                new SolidBrush(textMuted), margenIzq, margenSup - 15);
        }

        private void DibujarDona(Graphics g, Rectangle rect, int valor1, int valor2, Color c1, Color c2)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int grosor = 16;
            int size = Math.Min(rect.Width, rect.Height) - 100;
            if (size < 60) size = 60;

            int x = (rect.Width - size) / 2;
            int y = (rect.Height - size) / 2 + 10;

            int totalReal = valor1 + valor2;
            if (totalReal == 0) totalReal = 1;

            int angulo1 = (int)((valor1 / (float)totalReal) * 360);
            int angulo2 = 360 - angulo1;

            g.DrawArc(new Pen(Color.FromArgb(30, 30, 42), grosor), x, y, size, size, -90, 360);

            if (valor1 > 0)
            {
                using (var p = new Pen(c1, grosor) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                    g.DrawArc(p, x, y, size, size, -90, angulo1);
            }
            if (valor2 > 0)
            {
                using (var p = new Pen(c2, grosor) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                    g.DrawArc(p, x, y, size, size, -90 + angulo1, angulo2);
            }

            string texto = $"{valor1 + valor2}";
            using (var font = new Font("Segoe UI", 18f, FontStyle.Bold))
            {
                var sizeTexto = g.MeasureString(texto, font);
                g.DrawString(texto, font, new SolidBrush(textWhite),
                    x + (size - sizeTexto.Width) / 2,
                    y + (size - sizeTexto.Height) / 2 - 5);
            }

            using (var fontSmall = new Font("Segoe UI", 7.5f))
            {
                string s1 = $"{valor1}";
                string s2 = $"{valor2}";

                g.FillEllipse(new SolidBrush(c1), x + 12, y + size - 20, 8, 8);
                g.DrawString(s1, fontSmall, new SolidBrush(textWhite), x + 24, y + size - 22);

                g.FillEllipse(new SolidBrush(c2), x + size - 50, y + size - 20, 8, 8);
                g.DrawString(s2, fontSmall, new SolidBrush(textWhite), x + size - 38, y + size - 22);
            }
        }

        private void DibujarBarrasCirculares(Graphics g, Rectangle rect, int salidas, int eventos)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int centerX = rect.Width / 2;
            int centerY = rect.Height / 2 + 20;
            int radioBase = Math.Min(rect.Width, rect.Height) / 4;

            int maxVal = Math.Max(salidas, Math.Max(eventos, 1));
            float proporcion = (float)salidas / maxVal;

            g.DrawArc(new Pen(Color.FromArgb(40, 40, 55), 14),
                centerX - radioBase, centerY - radioBase,
                radioBase * 2, radioBase * 2, -90, 360);

            int angulo = (int)(360 * proporcion);
            if (angulo > 0)
            {
                g.DrawArc(new Pen(accentOrange, 14),
                    centerX - radioBase, centerY - radioBase,
                    radioBase * 2, radioBase * 2, -90, angulo);
            }

            string texto = $"{salidas}/{maxVal}";
            Font font = new Font("Segoe UI", 14f, FontStyle.Bold);
            SizeF sizeTexto = g.MeasureString(texto, font);
            g.DrawString(texto, font, new SolidBrush(textWhite),
                centerX - sizeTexto.Width / 2,
                centerY - sizeTexto.Height / 2);

            string sub = $"{salidas} salidas en el mes";
            Font fontSub = new Font("Segoe UI", 8f);
            SizeF stSub = g.MeasureString(sub, fontSub);
            g.DrawString(sub, fontSub, new SolidBrush(textMuted),
                centerX - stSub.Width / 2,
                centerY + 25);
        }

        private void DibujarTarjeta(Graphics g, Rectangle rect)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            g.FillEllipse(new SolidBrush(accentOrange), 20, 20, 40, 40);
            g.DrawString("👤", new Font("Segoe UI", 18f), new SolidBrush(textWhite), 27, 27);

            g.DrawString(nombreTrabajador, new Font("Segoe UI", 13f, FontStyle.Bold),
                new SolidBrush(textWhite), 75, 24);

            g.DrawString("Trabajador", new Font("Segoe UI", 9f),
                new SolidBrush(textMuted), 76, 47);

            g.DrawLine(new Pen(Color.FromArgb(60, 60, 80), 1),
                20, 80, rect.Width - 20, 80);

            string fechaActual = mesSeleccionado.ToString("MMMM 'de' yyyy").ToUpper();
            g.DrawString("MES:", new Font("Segoe UI", 8f, FontStyle.Bold),
                new SolidBrush(textMuted), 20, 95);
            g.DrawString(fechaActual, new Font("Segoe UI", 11f, FontStyle.Bold),
                new SolidBrush(accentOrange), 20, 112);

            g.DrawString("Salidas:", new Font("Segoe UI", 8.5f),
                new SolidBrush(textMuted), 20, 145);
            g.DrawString(salidasDelMes.ToString(), new Font("Segoe UI", 11f, FontStyle.Bold),
                new SolidBrush(textWhite), 100, 143);

            g.DrawString("Activas:", new Font("Segoe UI", 8.5f),
                new SolidBrush(textMuted), 20, 170);
            g.DrawString(salidasActivas.ToString(), new Font("Segoe UI", 11f, FontStyle.Bold),
                new SolidBrush(accentYellow), 100, 168);

            g.DrawString("Retornadas:", new Font("Segoe UI", 8.5f),
                new SolidBrush(textMuted), 20, 195);
            g.DrawString(salidasRetornadas.ToString(), new Font("Segoe UI", 11f, FontStyle.Bold),
                new SolidBrush(accentGreen), 100, 193);
        }

        // ═══════════════════════════════════════════
        // UTILIDADES
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