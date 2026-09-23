using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SUSTENTACION.PanelTrabajador
{
    public class InfoPanel : UserControl
    {
        // Paleta de colores basada en la imagen del Dashboard
        private readonly Color bgMain = Color.FromArgb(15, 15, 20);
        private readonly Color bgCard = Color.FromArgb(25, 25, 35);
        private readonly Color accentOrange = Color.FromArgb(255, 87, 34);
        private readonly Color accentPurple = Color.FromArgb(156, 39, 176);
        private readonly Color accentCyan = Color.FromArgb(0, 188, 212);
        private readonly Color accentYellow = Color.FromArgb(255, 193, 7);
        private readonly Color textWhite = Color.White;
        private readonly Color textMuted = Color.FromArgb(150, 150, 160);

        public InfoPanel()
        {
            InitializeComponentes();
        }

        private void InitializeComponentes()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = bgMain;
            this.Padding = new Padding(15);

            // Layout principal: 1 columna, 3 filas (Header, Filtros, Contenido)
            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.Transparent
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));  // Título
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));  // Meses
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // Contenido

            // ==========================================
            // 1. TÍTULO SUPERIOR
            // ==========================================
            Panel headerPanel = new Panel { Dock = DockStyle.Fill, BackColor = bgCard };
            AplicarBordesRedondeados(headerPanel, 8);

            Label lblTitulo = new Label
            {
                Text = "Dashboard de Información",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = textWhite,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            headerPanel.Controls.Add(lblTitulo);
            mainLayout.Controls.Add(headerPanel, 0, 0);

            // ==========================================
            // 2. BARRA DE MESES
            // ==========================================
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
                    ForeColor = mes == "AGOSTO" ? textWhite : textMuted,
                    BackColor = mes == "AGOSTO" ? accentOrange : Color.Transparent,
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

            // ==========================================
            // 3. CONTENIDO PRINCIPAL (Layout de 2 columnas)
            // ==========================================
            TableLayoutPanel contentLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f)); // Izquierda (Gráficos grandes)
            contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f)); // Derecha (Tarjetas y listas)

            // --- COLUMNA IZQUIERDA ---
            TableLayoutPanel leftColumn = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.Transparent
            };
            leftColumn.RowStyles.Add(new RowStyle(SizeType.Percent, 35f)); // Gráfico de líneas
            leftColumn.RowStyles.Add(new RowStyle(SizeType.Percent, 35f)); // Gráficos de dona
            leftColumn.RowStyles.Add(new RowStyle(SizeType.Percent, 30f)); // Gráfico de barras circular

            // 1. Gráfico de Líneas (Ingresos y Gastos)
            Panel pnlLineChart = CrearPanelGrafico("Ingresos y Gastos", "📈");
            pnlLineChart.Paint += (s, e) => DibujarGraficoLineas(e.Graphics, pnlLineChart.ClientRectangle);
            leftColumn.Controls.Add(pnlLineChart, 0, 0);

            // 2. Gráficos de Dona (Presupuesto e Inversiones)
            TableLayoutPanel pnlDonaLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            pnlDonaLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            pnlDonaLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

            Panel pnlDona1 = CrearPanelGrafico("Plan de Presupuesto", "🍩");
            pnlDona1.Paint += (s, e) => DibujarDona(e.Graphics, pnlDona1.ClientRectangle, 63, accentOrange, accentYellow, accentPurple);

            Panel pnlDona2 = CrearPanelGrafico("Inversiones", "📊");
            pnlDona2.Paint += (s, e) => DibujarDona(e.Graphics, pnlDona2.ClientRectangle, 45, accentPurple, accentCyan, accentOrange);

            pnlDonaLayout.Controls.Add(pnlDona1, 0, 0);
            pnlDonaLayout.Controls.Add(pnlDona2, 1, 0);
            leftColumn.Controls.Add(pnlDonaLayout, 0, 1);

            // 3. Gráfico de Barras Circular (Metas Financieras)
            Panel pnlBarras = CrearPanelGrafico("Metas Financieras", "🎯");
            pnlBarras.Paint += (s, e) => DibujarBarrasCirculares(e.Graphics, pnlBarras.ClientRectangle);
            leftColumn.Controls.Add(pnlBarras, 0, 2);

            contentLayout.Controls.Add(leftColumn, 0, 0);

            // --- COLUMNA DERECHA ---
            TableLayoutPanel rightColumn = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent
            };
            rightColumn.RowStyles.Add(new RowStyle(SizeType.Percent, 40f)); // Tarjeta de crédito
            rightColumn.RowStyles.Add(new RowStyle(SizeType.Percent, 60f)); // Lista de valores

            // 1. Tarjeta de Crédito
            Panel pnlTarjeta = new Panel { Dock = DockStyle.Fill, BackColor = bgCard, Margin = new Padding(5) };
            AplicarBordesRedondeados(pnlTarjeta, 10);
            pnlTarjeta.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // Dibujar chip
                g.FillRectangle(new SolidBrush(Color.FromArgb(255, 193, 7)), 20, 40, 30, 20);

                // Dibujar número de tarjeta
                g.DrawString("1234 .... .... 5678", new Font("Consolas", 12f, FontStyle.Bold), new SolidBrush(textWhite), 20, 80);

                // Dibujar logo de Mastercard
                g.FillEllipse(new SolidBrush(Color.FromArgb(255, 87, 34)), pnlTarjeta.Width - 60, 20, 30, 30);
                g.FillEllipse(new SolidBrush(Color.FromArgb(255, 193, 7)), pnlTarjeta.Width - 40, 20, 30, 30);

                // Texto de balance
                g.DrawString("Balance:", new Font("Segoe UI", 10f), new SolidBrush(textMuted), 20, pnlTarjeta.Height - 40);
                g.DrawString("$7 538,00", new Font("Segoe UI", 14f, FontStyle.Bold), new SolidBrush(textWhite), 80, pnlTarjeta.Height - 45);
            };
            rightColumn.Controls.Add(pnlTarjeta, 0, 0);

            // 2. Lista de Valores Totales
            Panel pnlLista = new Panel { Dock = DockStyle.Fill, BackColor = bgCard, Margin = new Padding(5), Padding = new Padding(10) };
            AplicarBordesRedondeados(pnlLista, 10);

            Label lblListaTitle = new Label
            {
                Text = "Valores Totales",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = textWhite,
                Dock = DockStyle.Top,
                Height = 30
            };
            pnlLista.Controls.Add(lblListaTitle);

            string[,] valores = {
                { "📊", "Presupuesto", "$35 419", "255,87,34" },
                { "📈", "Ingresos", "$35 419", "255,87,34" },
                { "📉", "Gastos", "$22 263", "156,39,176" },
                { "💼", "Inversiones", "$113 000", "0,188,212" },
                { "💳", "Deudas", "$52 990", "156,39,176" },
                { "🎯", "Metas Financieras", "$90 711", "255,193,7" }
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
        // MÉTODOS DE DIBUJO DE GRÁFICOS
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

            // Dibujar fondo de cuadrícula
            Pen gridPen = new Pen(Color.FromArgb(40, 40, 50), 1);
            for (int i = 1; i < 5; i++)
            {
                int y = rect.Height / 5 * i;
                g.DrawLine(gridPen, 10, y + 25, rect.Width - 10, y + 25);
            }

            // Dibujar líneas (simulando ingresos y gastos)
            Point[] puntosIngresos = {
                new Point(20, rect.Height - 30),
                new Point(rect.Width / 4, rect.Height - 80),
                new Point(rect.Width / 2, rect.Height - 50),
                new Point(rect.Width * 3 / 4, rect.Height - 100),
                new Point(rect.Width - 20, rect.Height - 70)
            };

            Point[] puntosGastos = {
                new Point(20, rect.Height - 60),
                new Point(rect.Width / 4, rect.Height - 40),
                new Point(rect.Width / 2, rect.Height - 90),
                new Point(rect.Width * 3 / 4, rect.Height - 60),
                new Point(rect.Width - 20, rect.Height - 90)
            };

            g.DrawCurve(new Pen(accentOrange, 2), puntosIngresos);
            g.DrawCurve(new Pen(accentPurple, 2), puntosGastos);
        }

        private void DibujarDona(Graphics g, Rectangle rect, int porcentaje, Color c1, Color c2, Color c3)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int size = Math.Min(rect.Width, rect.Height) - 60;
            int x = (rect.Width - size) / 2;
            int y = (rect.Height - size) / 2 + 10;

            // Dona exterior
            g.DrawArc(new Pen(c1, 12), x, y, size, size, -90, 120);
            g.DrawArc(new Pen(c2, 12), x, y, size, size, 30, 90);
            g.DrawArc(new Pen(c3, 12), x, y, size, size, 120, 150);

            // Círculo central
            g.FillEllipse(new SolidBrush(bgCard), x + 20, y + 20, size - 40, size - 40);

            // Texto central
            string texto = $"{porcentaje}%";
            Font font = new Font("Segoe UI", 12f, FontStyle.Bold);
            SizeF sizeTexto = g.MeasureString(texto, font);
            g.DrawString(texto, font, new SolidBrush(textWhite), x + (size - sizeTexto.Width) / 2, y + (size - sizeTexto.Height) / 2);
        }

        private void DibujarBarrasCirculares(Graphics g, Rectangle rect)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int centerX = rect.Width / 2;
            int centerY = rect.Height / 2 + 10;
            int radioBase = Math.Min(rect.Width, rect.Height) / 3;

            // Dibujar arcos concéntricos
            for (int i = 0; i < 5; i++)
            {
                int radio = radioBase + (i * 8);
                Color color = i % 2 == 0 ? accentOrange : accentPurple;
                g.DrawArc(new Pen(color, 4), centerX - radio, centerY - radio, radio * 2, radio * 2, -90, 270);
            }

            // Texto central
            g.DrawString("42%", new Font("Segoe UI", 12f, FontStyle.Bold), new SolidBrush(textWhite), centerX - 15, centerY - 10);
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