using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace SUSTENTACION.PanelTrabajador
{
    public class InfoPanel : UserControl
    {
        // Paleta de colores
        private readonly Color bgMain = Color.FromArgb(15, 15, 20);
        private readonly Color bgCard = Color.FromArgb(25, 25, 35);
        private readonly Color accentOrange = Color.FromArgb(255, 87, 34);
        private readonly Color accentPurple = Color.FromArgb(156, 39, 176);
        private readonly Color accentCyan = Color.FromArgb(0, 188, 212);
        private readonly Color accentYellow = Color.FromArgb(255, 193, 7);
        private readonly Color textWhite = Color.White;
        private readonly Color textMuted = Color.FromArgb(150, 150, 160);

        // Datos reales del trabajador
        private int idTrabajador;
        private string nombreTrabajador;
        private int totalEquiposAsignados;
        private int totalEventos;
        private int equiposDisponibles;
        private int equiposEnEvento;
        private decimal ingresosTotales;
        private decimal gastosTotales;

        // Paneles de gráficos (para repintarlos)
        private Panel pnlLineChart;
        private Panel pnlDona1;
        private Panel pnlDona2;
        private Panel pnlBarras;
        private Panel pnlTarjeta;

        public InfoPanel(int idTrabajador = 1, string nombreTrabajador = "Trabajador")
        {
            this.idTrabajador = idTrabajador;
            this.nombreTrabajador = nombreTrabajador;

            InitializeComponentes();
            CargarDatosReales();
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

            // TÍTULO
            Panel headerPanel = new Panel { Dock = DockStyle.Fill, BackColor = bgCard };
            AplicarBordesRedondeados(headerPanel, 8);
            Label lblTitulo = new Label
            {
                Text = $"Dashboard de {nombreTrabajador}",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = textWhite,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            headerPanel.Controls.Add(lblTitulo);
            mainLayout.Controls.Add(headerPanel, 0, 0);

            // MESES
            FlowLayoutPanel pnlMeses = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 5, 0, 5)
            };
            string[] meses = { "ENERO", "FEBRERO", "MARZO", "ABRIL", "MAYO", "JUNIO", "JULIO", "AGOSTO", "SEPTIEMBRE", "OCTUBRE", "NOVIEMBRE", "DICIEMBRE" };
            foreach (var mes in meses)
            {
                Button btnMes = new Button
                {
                    Text = mes,
                    Font = new Font("Segoe UI", 7f, FontStyle.Bold),
                    ForeColor = mes == DateTime.Now.ToString("MMMM").ToUpper() ? textWhite : textMuted,
                    BackColor = mes == DateTime.Now.ToString("MMMM").ToUpper() ? accentOrange : Color.Transparent,
                    FlatStyle = FlatStyle.Flat,
                    Height = 25,
                    Width = 80,
                    Margin = new Padding(2, 0, 2, 0),
                    Cursor = Cursors.Hand
                };
                btnMes.FlatAppearance.BorderSize = 0;
                AplicarBordesRedondeados(btnMes, 4);
                pnlMeses.Controls.Add(btnMes);
            }
            mainLayout.Controls.Add(pnlMeses, 0, 1);

            // CONTENIDO
            TableLayoutPanel contentLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f));
            contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));

            // IZQUIERDA
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

            pnlLineChart = CrearPanelGrafico("Ingresos y Gastos", "📈");
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

            pnlDona1 = CrearPanelGrafico("Equipos por Estado", "🍩");
            pnlDona1.Paint += (s, e) => DibujarDona(e.Graphics, pnlDona1.ClientRectangle, equiposDisponibles, equiposEnEvento, totalEquiposAsignados, accentOrange, accentYellow, accentPurple);

            pnlDona2 = CrearPanelGrafico("Eventos Asignados", "📊");
            pnlDona2.Paint += (s, e) => DibujarDona(e.Graphics, pnlDona2.ClientRectangle, totalEventos, 100 - totalEventos, 0, accentPurple, accentCyan, accentOrange);

            pnlDonaLayout.Controls.Add(pnlDona1, 0, 0);
            pnlDonaLayout.Controls.Add(pnlDona2, 1, 0);
            leftColumn.Controls.Add(pnlDonaLayout, 0, 1);

            pnlBarras = CrearPanelGrafico("Metas Financieras", "🎯");
            pnlBarras.Paint += (s, e) => DibujarBarrasCirculares(e.Graphics, pnlBarras.ClientRectangle, ingresosTotales, gastosTotales);
            leftColumn.Controls.Add(pnlBarras, 0, 2);

            contentLayout.Controls.Add(leftColumn, 0, 0);

            // DERECHA
            TableLayoutPanel rightColumn = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent
            };
            rightColumn.RowStyles.Add(new RowStyle(SizeType.Percent, 40f));
            rightColumn.RowStyles.Add(new RowStyle(SizeType.Percent, 60f));

            pnlTarjeta = new Panel { Dock = DockStyle.Fill, BackColor = bgCard, Margin = new Padding(5) };
            AplicarBordesRedondeados(pnlTarjeta, 10);
            pnlTarjeta.Paint += (s, e) => DibujarTarjeta(e.Graphics, pnlTarjeta.ClientRectangle);
            rightColumn.Controls.Add(pnlTarjeta, 0, 0);

            Panel pnlLista = new Panel { Dock = DockStyle.Fill, BackColor = bgCard, Margin = new Padding(5), Padding = new Padding(10) };
            AplicarBordesRedondeados(pnlLista, 10);

            Label lblListaTitle = new Label
            {
                Text = "Resumen de Actividad",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = textWhite,
                Dock = DockStyle.Top,
                Height = 30
            };
            pnlLista.Controls.Add(lblListaTitle);

            string[,] valores = {
                { "📦", "Equipos Asignados", totalEquiposAsignados.ToString(), "255,87,34" },
                { "✅", "Equipos Disponibles", equiposDisponibles.ToString(), "0,188,212" },
                { "🚚", "Equipos en Evento", equiposEnEvento.ToString(), "156,39,176" },
                { "📅", "Eventos Totales", totalEventos.ToString(), "255,193,7" },
                { "💰", "Ingresos", $"${ingresosTotales:N0}", "0,188,212" },
                { "💸", "Gastos", $"${gastosTotales:N0}", "156,39,176" }
            };

            int yPos = 35;
            for (int i = 0; i < valores.GetLength(0); i++)
            {
                Panel itemPanel = new Panel
                {
                    Location = new Point(5, yPos),
                    Size = new Size(pnlLista.Width - 15, 30),
                    BackColor = Color.Transparent
                };

                Label lblIcon = new Label { Text = valores[i, 0], Font = new Font("Segoe UI", 10f), ForeColor = Color.White, Location = new Point(0, 5), AutoSize = true };
                Label lblNombre = new Label { Text = valores[i, 1], Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = textWhite, Location = new Point(30, 7), AutoSize = true };

                string[] rgb = valores[i, 3].Split(',');
                Color itemColor = Color.FromArgb(int.Parse(rgb[0]), int.Parse(rgb[1]), int.Parse(rgb[2]));
                Label lblMonto = new Label { Text = valores[i, 2], Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = itemColor, Location = new Point(150, 7), AutoSize = true };

                itemPanel.Controls.Add(lblIcon);
                itemPanel.Controls.Add(lblNombre);
                itemPanel.Controls.Add(lblMonto);
                pnlLista.Controls.Add(itemPanel);
                yPos += 35;
            }
            rightColumn.Controls.Add(pnlLista, 0, 1);

            contentLayout.Controls.Add(rightColumn, 1, 0);
            mainLayout.Controls.Add(contentLayout, 0, 2);
            this.Controls.Add(mainLayout);
        }

        // ==========================================
        // CARGA DE DATOS REALES DESDE MYSQL
        // ==========================================
        private void CargarDatosReales()
        {
            Conexion conexionDB = new Conexion();
            try
            {
                MySqlConnection con = conexionDB.ObtenerConexion();

                // 1. Equipos asignados a este trabajador (asumiendo que hay una tabla 'asignaciones' o similar)
                string sqlEquipos = @"SELECT 
                    COUNT(*) AS total,
                    IFNULL(SUM(CASE WHEN LOWER(TRIM(estado)) = 'disponible' THEN 1 ELSE 0 END), 0) AS disponibles,
                    IFNULL(SUM(CASE WHEN LOWER(TRIM(estado)) = 'en evento' THEN 1 ELSE 0 END), 0) AS en_evento
                    FROM equipos WHERE id_trabajador = @idTrabajador";

                MySqlCommand cmdEq = new MySqlCommand(sqlEquipos, con);
                cmdEq.Parameters.AddWithValue("@idTrabajador", idTrabajador);
                using (MySqlDataReader rd = cmdEq.ExecuteReader())
                {
                    if (rd.Read())
                    {
                        totalEquiposAsignados = Convert.ToInt32(rd["total"]);
                        equiposDisponibles = Convert.ToInt32(rd["disponibles"]);
                        equiposEnEvento = Convert.ToInt32(rd["en_evento"]);
                    }
                }

                // 2. Eventos asignados a este trabajador
                string sqlEventos = "SELECT COUNT(*) FROM eventos WHERE id_trabajador = @idTrabajador";
                MySqlCommand cmdEv = new MySqlCommand(sqlEventos, con);
                cmdEv.Parameters.AddWithValue("@idTrabajador", idTrabajador);
                object resEv = cmdEv.ExecuteScalar();
                totalEventos = resEv != null ? Convert.ToInt32(resEv) : 0;

                // 3. Ingresos y Gastos (ejemplo: sumando campos de la tabla eventos)
                string sqlFinanzas = @"SELECT 
                    IFNULL(SUM(ingreso), 0) AS total_ingresos,
                    IFNULL(SUM(gasto), 0) AS total_gastos
                    FROM eventos WHERE id_trabajador = @idTrabajador";
                MySqlCommand cmdFin = new MySqlCommand(sqlFinanzas, con);
                cmdFin.Parameters.AddWithValue("@idTrabajador", idTrabajador);
                using (MySqlDataReader rd = cmdFin.ExecuteReader())
                {
                    if (rd.Read())
                    {
                        ingresosTotales = Convert.ToDecimal(rd["total_ingresos"]);
                        gastosTotales = Convert.ToDecimal(rd["total_gastos"]);
                    }
                }
            }
            catch (Exception ex)
            {
                // Si falla, dejamos valores en 0 para que los gráficos no se rompan
                Console.WriteLine("Error al cargar datos: " + ex.Message);
            }
            finally
            {
                conexionDB.CerrarConexion();
            }
        }

        // ==========================================
        // MÉTODOS DE DIBUJO (AHORA CON DATOS REALES)
        // ==========================================
        private Panel CrearPanelGrafico(string titulo, string icono)
        {
            Panel pnl = new Panel { Dock = DockStyle.Fill, BackColor = bgCard, Margin = new Padding(5) };
            AplicarBordesRedondeados(pnl, 10);
            Label lblTitle = new Label
            {
                Text = $"{icono}  {titulo}",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = textWhite,
                Dock = DockStyle.Top,
                Height = 25,
                Padding = new Padding(10, 5, 0, 0)
            };
            pnl.Controls.Add(lblTitle);
            return pnl;
        }

        private void DibujarGraficoLineas(Graphics g, Rectangle rect)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Pen gridPen = new Pen(Color.FromArgb(40, 40, 50), 1);
            for (int i = 1; i < 5; i++)
            {
                int y = rect.Height / 5 * i;
                g.DrawLine(gridPen, 10, y + 25, rect.Width - 10, y + 25);
            }

            // Simulamos puntos basados en ingresos y gastos reales
            int maxVal = Math.Max((int)ingresosTotales, (int)gastosTotales);
            if (maxVal == 0) maxVal = 1; // evitar división por cero

            Point[] puntosIngresos = {
                new Point(20, rect.Height - 30),
                new Point(rect.Width / 4, rect.Height - (int)((ingresosTotales / maxVal) * (rect.Height - 50))),
                new Point(rect.Width / 2, rect.Height - (int)((ingresosTotales / maxVal) * (rect.Height - 50))),
                new Point(rect.Width * 3 / 4, rect.Height - (int)((ingresosTotales / maxVal) * (rect.Height - 50))),
                new Point(rect.Width - 20, rect.Height - 30)
            };

            Point[] puntosGastos = {
                new Point(20, rect.Height - 60),
                new Point(rect.Width / 4, rect.Height - (int)((gastosTotales / maxVal) * (rect.Height - 50))),
                new Point(rect.Width / 2, rect.Height - (int)((gastosTotales / maxVal) * (rect.Height - 50))),
                new Point(rect.Width * 3 / 4, rect.Height - (int)((gastosTotales / maxVal) * (rect.Height - 50))),
                new Point(rect.Width - 20, rect.Height - 60)
            };

            g.DrawCurve(new Pen(accentOrange, 2), puntosIngresos);
            g.DrawCurve(new Pen(accentPurple, 2), puntosGastos);
        }

        private void DibujarDona(Graphics g, Rectangle rect, int valor1, int valor2, int total, Color c1, Color c2, Color c3)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int size = Math.Min(rect.Width, rect.Height) - 60;
            int x = (rect.Width - size) / 2;
            int y = (rect.Height - size) / 2 + 10;

            // Calcular ángulos basados en datos reales
            int totalReal = Math.Max(valor1 + valor2 + (total - valor1 - valor2), 1);
            int angulo1 = (int)((valor1 / (float)totalReal) * 360);
            int angulo2 = (int)((valor2 / (float)totalReal) * 360);
            int angulo3 = 360 - angulo1 - angulo2;

            g.DrawArc(new Pen(c1, 12), x, y, size, size, -90, angulo1);
            g.DrawArc(new Pen(c2, 12), x, y, size, size, -90 + angulo1, angulo2);
            g.DrawArc(new Pen(c3, 12), x, y, size, size, -90 + angulo1 + angulo2, angulo3);

            g.FillEllipse(new SolidBrush(bgCard), x + 20, y + 20, size - 40, size - 40);

            // Texto central con el total real
            string texto = $"{valor1 + valor2}";
            Font font = new Font("Segoe UI", 12f, FontStyle.Bold);
            SizeF sizeTexto = g.MeasureString(texto, font);
            g.DrawString(texto, font, new SolidBrush(textWhite), x + (size - sizeTexto.Width) / 2, y + (size - sizeTexto.Height) / 2);
        }

        private void DibujarBarrasCirculares(Graphics g, Rectangle rect, decimal ingresos, decimal gastos)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int centerX = rect.Width / 2;
            int centerY = rect.Height / 2 + 10;
            int radioBase = Math.Min(rect.Width, rect.Height) / 3;

            // La cantidad de arcos depende de los ingresos totales
            int numArcos = Math.Min((int)(ingresos / 10000), 5);
            if (numArcos == 0) numArcos = 1;

            for (int i = 0; i < numArcos; i++)
            {
                int radio = radioBase + (i * 8);
                Color color = i % 2 == 0 ? accentOrange : accentPurple;
                g.DrawArc(new Pen(color, 4), centerX - radio, centerY - radio, radio * 2, radio * 2, -90, 270);
            }

            // Texto central con el porcentaje de ingresos vs gastos
            string texto = ingresos > 0 ? $"{((ingresos - gastos) / ingresos * 100):0}%" : "0%";
            g.DrawString(texto, new Font("Segoe UI", 12f, FontStyle.Bold), new SolidBrush(textWhite), centerX - 15, centerY - 10);
        }

        private void DibujarTarjeta(Graphics g, Rectangle rect)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.FillRectangle(new SolidBrush(Color.FromArgb(255, 193, 7)), 20, 40, 30, 20);
            g.DrawString("**** **** **** 5678", new Font("Consolas", 12f, FontStyle.Bold), new SolidBrush(textWhite), 20, 80);
            g.FillEllipse(new SolidBrush(Color.FromArgb(255, 87, 34)), rect.Width - 60, 20, 30, 30);
            g.FillEllipse(new SolidBrush(Color.FromArgb(255, 193, 7)), rect.Width - 40, 20, 30, 30);
            g.DrawString("Balance:", new Font("Segoe UI", 10f), new SolidBrush(textMuted), 20, rect.Height - 40);
            g.DrawString($"${ingresosTotales - gastosTotales:N0}", new Font("Segoe UI", 14f, FontStyle.Bold), new SolidBrush(textWhite), 80, rect.Height - 45);
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