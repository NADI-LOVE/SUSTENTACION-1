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
        // Paleta
        private readonly Color bgMain = Color.FromArgb(15, 15, 20);
        private readonly Color bgCard = Color.FromArgb(25, 25, 35);
        private readonly Color accentOrange = Color.FromArgb(255, 87, 34);
        private readonly Color accentPurple = Color.FromArgb(156, 39, 176);
        private readonly Color accentCyan = Color.FromArgb(0, 188, 212);
        private readonly Color accentYellow = Color.FromArgb(255, 193, 7);
        private readonly Color accentGreen = Color.FromArgb(46, 204, 148);
        private readonly Color textWhite = Color.White;
        private readonly Color textMuted = Color.FromArgb(150, 150, 160);

        // Datos del trabajador
        private int idTrabajador;
        private string nombreTrabajador;
        private DateTime mesSeleccionado = DateTime.Now;

        // KPIs reales
        private int salidasDelMes = 0;
        private int salidasRetornadas = 0;
        private int salidasActivas = 0;
        private int equiposAsignadosActivos = 0;
        private int equiposDisponibles = 0;
        private int equiposEnEvento = 0;
        private int totalEventosDelMes = 0;
        private int[] salidasPorMes = new int[12];

        // Paneles de gráficos
        private Panel pnlLineChart;
        private Panel pnlDona1;
        private Panel pnlDona2;
        private Panel pnlBarras;
        private Panel pnlTarjeta;
        private FlowLayoutPanel pnlMeses;

        public InfoPanel(int idTrabajador = 1, string nombreTrabajador = "Trabajador")
        {
            this.idTrabajador = idTrabajador;
            this.nombreTrabajador = nombreTrabajador;

            InitializeComponentes();
            CargarDatosReales();
            RefrescarGraficos();
        }

        private void InitializeComponentes()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = bgMain;
            this.Padding = new Padding(15);

            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.Transparent
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            // ═══════════════════════════════════════
            // TÍTULO
            // ═══════════════════════════════════════
            Panel headerPanel = new Panel { Dock = DockStyle.Fill, BackColor = bgCard };
            AplicarBordesRedondeados(headerPanel, 8);

            Label lblTitulo = new Label
            {
                Text = $"📊  Dashboard de {nombreTrabajador}",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = textWhite,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            headerPanel.Controls.Add(lblTitulo);
            mainLayout.Controls.Add(headerPanel, 0, 0);

            // ═══════════════════════════════════════
            // BOTONES DE MESES
            // ═══════════════════════════════════════
            pnlMeses = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 5, 0, 5)
            };

            string[] meses = { "ENERO", "FEBRERO", "MARZO", "ABRIL", "MAYO", "JUNIO",
                              "JULIO", "AGOSTO", "SEPTIEMBRE", "OCTUBRE", "NOVIEMBRE", "DICIEMBRE" };

            for (int i = 0; i < 12; i++)
            {
                int mesIndex = i;
                bool esMesActual = (i == DateTime.Now.Month - 1);

                Button btnMes = new Button
                {
                    Text = meses[i],
                    Font = new Font("Segoe UI", 7f, FontStyle.Bold),
                    ForeColor = esMesActual ? textWhite : textMuted,
                    BackColor = esMesActual ? accentOrange : Color.Transparent,
                    FlatStyle = FlatStyle.Flat,
                    Height = 25,
                    Width = 80,
                    Margin = new Padding(2, 0, 2, 0),
                    Cursor = Cursors.Hand,
                    Tag = mesIndex
                };
                btnMes.FlatAppearance.BorderSize = 0;
                AplicarBordesRedondeados(btnMes, 4);

                btnMes.Click += (s, e) =>
                {
                    mesSeleccionado = new DateTime(DateTime.Now.Year, mesIndex + 1, 1);

                    // Actualizar colores
                    foreach (Control c in pnlMeses.Controls)
                    {
                        if (c is Button b)
                        {
                            int tag = (int)b.Tag;
                            bool activo = tag == mesIndex;
                            b.BackColor = activo ? accentOrange : Color.Transparent;
                            b.ForeColor = activo ? textWhite : textMuted;
                        }
                    }

                    CargarDatosReales();
                    RefrescarGraficos();
                };

                pnlMeses.Controls.Add(btnMes);
            }

            mainLayout.Controls.Add(pnlMeses, 0, 1);

            // ═══════════════════════════════════════
            // CONTENIDO
            // ═══════════════════════════════════════
            TableLayoutPanel contentLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f));
            contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));

            // ─── IZQUIERDA ───
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

            // Gráfico de líneas: salidas por mes
            pnlLineChart = CrearPanelGrafico("Salidas durante el año", "📈");
            pnlLineChart.Paint += (s, e) => DibujarGraficoLineas(e.Graphics, pnlLineChart.ClientRectangle);
            leftColumn.Controls.Add(pnlLineChart, 0, 0);

            // Donas
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

            // Barras circulares: eventos del mes
            pnlBarras = CrearPanelGrafico("Actividad del mes", "🎯");
            pnlBarras.Paint += (s, e) => DibujarBarrasCirculares(
                e.Graphics, pnlBarras.ClientRectangle,
                salidasDelMes, totalEventosDelMes);
            leftColumn.Controls.Add(pnlBarras, 0, 2);

            contentLayout.Controls.Add(leftColumn, 0, 0);

            // ─── DERECHA ───
            TableLayoutPanel rightColumn = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent
            };
            rightColumn.RowStyles.Add(new RowStyle(SizeType.Percent, 40f));
            rightColumn.RowStyles.Add(new RowStyle(SizeType.Percent, 60f));

            // Tarjeta
            pnlTarjeta = new Panel { Dock = DockStyle.Fill, BackColor = bgCard, Margin = new Padding(5) };
            AplicarBordesRedondeados(pnlTarjeta, 10);
            pnlTarjeta.Paint += (s, e) => DibujarTarjeta(e.Graphics, pnlTarjeta.ClientRectangle);
            rightColumn.Controls.Add(pnlTarjeta, 0, 0);

            // Lista de resumen
            Panel pnlLista = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = bgCard,
                Margin = new Padding(5),
                Padding = new Padding(10),
                AutoScroll = true
            };
            AplicarBordesRedondeados(pnlLista, 10);

            Label lblListaTitle = new Label
            {
                Text = "📋  Resumen de Actividad",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = textWhite,
                Dock = DockStyle.Top,
                Height = 35
            };
            pnlLista.Controls.Add(lblListaTitle);

            // Se llenará con datos reales
            FlowLayoutPanel flpResumen = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Color.Transparent,
                Name = "flpResumen"
            };
            pnlLista.Controls.Add(flpResumen);
            lblListaTitle.BringToFront();

            rightColumn.Controls.Add(pnlLista, 0, 1);

            contentLayout.Controls.Add(rightColumn, 1, 0);
            mainLayout.Controls.Add(contentLayout, 0, 2);
            this.Controls.Add(mainLayout);
        }

        // ==========================================
        // CARGA DE DATOS REALES DESDE POSTGRESQL
        // ==========================================
        private void CargarDatosReales()
        {
            Conexion conexionDB = new Conexion();
            try
            {
                NpgsqlConnection con = conexionDB.ObtenerConexion();

                int mes = mesSeleccionado.Month;
                int anio = mesSeleccionado.Year;

                // 1. Salidas del mes (encargadas por este trabajador)
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

                // 2. Salidas por mes (para el gráfico de líneas)
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

                // 3. Equipos a cargo (en salidas activas del trabajador)
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

                // 4. Equipos disponibles (global)
                string sqlDisponibles = @"
                    SELECT COALESCE(SUM(stock), 0) FROM equipos 
                    WHERE LOWER(TRIM(estado)) = 'disponible'";
                using (NpgsqlCommand cmd = new NpgsqlCommand(sqlDisponibles, con))
                {
                    object r = cmd.ExecuteScalar();
                    equiposDisponibles = r != null ? Convert.ToInt32(r) : 0;
                }

                equiposAsignadosActivos = equiposEnEvento;

                // 5. Total eventos del mes (donde trabaja este trabajador)
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
                System.Diagnostics.Debug.WriteLine("Error en InfoPanel: " + ex.Message);
            }
            finally
            {
                conexionDB.CerrarConexion();
            }

            // Actualizar lista de resumen
            ActualizarListaResumen();
        }

        private void ActualizarListaResumen()
        {
            var flp = this.Controls.Find("flpResumen", true);
            if (flp.Length == 0) return;
            FlowLayoutPanel panel = flp[0] as FlowLayoutPanel;
            if (panel == null) return;

            panel.Controls.Clear();

            string[,] valores = {
                { "📤", "Salidas del mes", salidasDelMes.ToString(), "255,87,34" },
                { "🔄", "Salidas activas", salidasActivas.ToString(), "255,193,7" },
                { "✅", "Salidas retornadas", salidasRetornadas.ToString(), "46,204,148" },
                { "🚚", "Equipos a mi cargo", equiposEnEvento.ToString(), "156,39,176" },
                { "📦", "Equipos disponibles", equiposDisponibles.ToString(), "0,188,212" },
                { "📅", "Eventos del mes", totalEventosDelMes.ToString(), "255,193,7" },
                { "👤", "Encargado", nombreTrabajador, "150,150,160" }
            };

            int filas = valores.GetLength(0);

            for (int i = 0; i < filas; i++)
            {
                string icono = valores[i, 0];
                string nombre = valores[i, 1];
                string monto = valores[i, 2];
                string rgbStr = valores[i, 3];

                Panel itemPanel = new Panel
                {
                    Width = 300,
                    Height = 35,
                    Margin = new Padding(0, 3, 0, 3),
                    BackColor = Color.FromArgb(30, 30, 42)
                };
                AplicarBordesRedondeados(itemPanel, 8);

                Label lblIcon = new Label
                {
                    Text = icono,
                    Font = new Font("Segoe UI", 11f),
                    ForeColor = textWhite,
                    Location = new Point(10, 8),
                    AutoSize = true
                };

                Label lblNombre = new Label
                {
                    Text = nombre,
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                    ForeColor = textWhite,
                    Location = new Point(45, 10),
                    AutoSize = true
                };

                string[] rgb = rgbStr.Split(',');
                Color color = Color.FromArgb(
                    int.Parse(rgb[0]),
                    int.Parse(rgb[1]),
                    int.Parse(rgb[2])
                );

                Label lblMonto = new Label
                {
                    Text = monto,
                    Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                    ForeColor = color,
                    Location = new Point(230, 9),
                    AutoSize = true,
                    TextAlign = ContentAlignment.MiddleRight
                };

                itemPanel.Controls.Add(lblIcon);
                itemPanel.Controls.Add(lblNombre);
                itemPanel.Controls.Add(lblMonto);

                panel.Controls.Add(itemPanel);
            }
        }

        private void RefrescarGraficos()
        {
            pnlLineChart?.Invalidate();
            pnlDona1?.Invalidate();
            pnlDona2?.Invalidate();
            pnlBarras?.Invalidate();
            pnlTarjeta?.Invalidate();
        }

        // ==========================================
        // DIBUJO DE GRÁFICOS
        // ==========================================
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

        private void DibujarGraficoLineas(Graphics g, Rectangle rect)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int margenIzq = 40;
            int margenDer = 20;
            int margenSup = 35;
            int margenInf = 30;

            int ancho = rect.Width - margenIzq - margenDer;
            int alto = rect.Height - margenSup - margenInf;

            // Grid
            Pen gridPen = new Pen(Color.FromArgb(40, 40, 55), 1);
            for (int i = 0; i <= 4; i++)
            {
                int y = margenSup + (alto / 4) * i;
                g.DrawLine(gridPen, margenIzq, y, margenIzq + ancho, y);
            }

            // Max para escalar
            int maxVal = 1;
            foreach (var v in salidasPorMes) if (v > maxVal) maxVal = v;

            // Puntos
            string[] mesesCortos = { "E", "F", "M", "A", "M", "J", "J", "A", "S", "O", "N", "D" };
            int espaciado = ancho / 11;

            Point[] puntos = new Point[12];
            for (int i = 0; i < 12; i++)
            {
                int x = margenIzq + (espaciado * i);
                int y = margenSup + alto - (int)(((float)salidasPorMes[i] / maxVal) * alto * 0.9f);
                puntos[i] = new Point(x, y);

                // Etiqueta del mes
                g.DrawString(mesesCortos[i], new Font("Segoe UI", 7f),
                    new SolidBrush(textMuted), x - 3, margenSup + alto + 8);
            }

            // Línea
            if (puntos.Length > 1)
            {
                g.DrawLines(new Pen(accentOrange, 2.5f), puntos);
            }

            // Puntos visibles
            foreach (var p in puntos)
            {
                g.FillEllipse(new SolidBrush(accentOrange), p.X - 3, p.Y - 3, 6, 6);
            }

            // Valor máximo del gráfico
            g.DrawString($"Máx: {maxVal}", new Font("Segoe UI", 7f),
                new SolidBrush(textMuted), margenIzq, margenSup - 15);
        }

        private void DibujarDona(Graphics g, Rectangle rect, int valor1, int valor2, Color c1, Color c2)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int size = Math.Min(rect.Width, rect.Height) - 90;
            if (size < 40) size = 40;

            int x = (rect.Width - size) / 2;
            int y = (rect.Height - size) / 2 + 15;

            int totalReal = valor1 + valor2;
            if (totalReal == 0) totalReal = 1;

            int angulo1 = (int)((valor1 / (float)totalReal) * 360);
            int angulo2 = 360 - angulo1;

            // Fondo del círculo
            g.DrawArc(new Pen(Color.FromArgb(40, 40, 55), 12), x, y, size, size, -90, 360);

            if (valor1 > 0)
                g.DrawArc(new Pen(c1, 12), x, y, size, size, -90, angulo1);
            if (valor2 > 0)
                g.DrawArc(new Pen(c2, 12), x, y, size, size, -90 + angulo1, angulo2);

            // Texto central
            string texto = $"{valor1 + valor2}";
            Font font = new Font("Segoe UI", 14f, FontStyle.Bold);
            SizeF sizeTexto = g.MeasureString(texto, font);
            g.DrawString(texto, font, new SolidBrush(textWhite),
                x + (size - sizeTexto.Width) / 2,
                y + (size - sizeTexto.Height) / 2);

            // Etiquetas debajo
            string labels = $"{c1.Name} : {valor1}    {c2.Name} : {valor2}";
            Font fontSmall = new Font("Segoe UI", 7f);
            SizeF stSmall = g.MeasureString(labels, fontSmall);

            g.FillEllipse(new SolidBrush(c1), x + 5, y + size + 8, 8, 8);
            g.DrawString($"Activos: {valor1}", fontSmall, new SolidBrush(textMuted),
                x + 18, y + size + 6);

            g.FillEllipse(new SolidBrush(c2), x + 90, y + size + 8, 8, 8);
            g.DrawString($"Otros: {valor2}", fontSmall, new SolidBrush(textMuted),
                x + 103, y + size + 6);
        }

        private void DibujarBarrasCirculares(Graphics g, Rectangle rect, int salidas, int eventos)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int centerX = rect.Width / 2;
            int centerY = rect.Height / 2 + 20;
            int radioBase = Math.Min(rect.Width, rect.Height) / 4;

            int maxVal = Math.Max(salidas, Math.Max(eventos, 1));
            float proporcion = (float)salidas / maxVal;

            // Fondo gris
            g.DrawArc(new Pen(Color.FromArgb(40, 40, 55), 14),
                centerX - radioBase, centerY - radioBase,
                radioBase * 2, radioBase * 2, -90, 360);

            // Arco proporcional
            int angulo = (int)(360 * proporcion);
            if (angulo > 0)
            {
                g.DrawArc(new Pen(accentOrange, 14),
                    centerX - radioBase, centerY - radioBase,
                    radioBase * 2, radioBase * 2, -90, angulo);
            }

            // Texto
            string texto = $"{salidas}/{maxVal}";
            Font font = new Font("Segoe UI", 14f, FontStyle.Bold);
            SizeF sizeTexto = g.MeasureString(texto, font);
            g.DrawString(texto, font, new SolidBrush(textWhite),
                centerX - sizeTexto.Width / 2,
                centerY - sizeTexto.Height / 2);

            // Subtítulo
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

            // Ícono usuario
            g.FillEllipse(new SolidBrush(accentOrange), 20, 20, 40, 40);
            g.DrawString("👤", new Font("Segoe UI", 18f), new SolidBrush(textWhite), 27, 27);

            // Nombre
            g.DrawString(nombreTrabajador, new Font("Segoe UI", 13f, FontStyle.Bold),
                new SolidBrush(textWhite), 75, 24);

            // Rol
            g.DrawString("Trabajador", new Font("Segoe UI", 9f),
                new SolidBrush(textMuted), 76, 47);

            // Línea
            g.DrawLine(new Pen(Color.FromArgb(60, 60, 80), 1),
                20, 80, rect.Width - 20, 80);

            // Info
            string fechaActual = mesSeleccionado.ToString("MMMM 'de' yyyy").ToUpper();
            g.DrawString("MES:", new Font("Segoe UI", 8f, FontStyle.Bold),
                new SolidBrush(textMuted), 20, 95);
            g.DrawString(fechaActual, new Font("Segoe UI", 11f, FontStyle.Bold),
                new SolidBrush(accentOrange), 20, 112);

            // Stats
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

        // ==========================================
        // UTILIDADES
        // ==========================================
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