using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SUSTENTACION.PanelAdministrador
{
    [System.ComponentModel.DesignerCategory("")]
    public class ControlInventario : UserControl
    {
        private readonly Color bgContainer = Color.FromArgb(20, 30, 38);
        private readonly Color bgCard = Color.FromArgb(28, 41, 51);
        private readonly Color textWhite = Color.FromArgb(240, 245, 250);
        private readonly Color textMuted = Color.FromArgb(130, 150, 165);
        private readonly Color accentTeal = Color.FromArgb(20, 184, 166);

        private TableLayoutPanel mainLayout;
        private DataGridView dgv;

        public ControlInventario()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = bgContainer;
            this.Padding = new Padding(20);
            this.DoubleBuffered = true;

            ConstruirUI();
        }

        private void ConstruirUI()
        {
            // Layout Principal Vertical (Reemplaza al FlowLayoutPanel de ancho fijo)
            mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.Transparent
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));  // Header / Titulo y Botones
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // Cards Gradiente
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // Tabla DataGridView

            this.Controls.Add(mainLayout);

            // 1. HEADER (TÍTULO + BOTONES)
            Panel headerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 10)
            };

            Label lblTitle = new Label
            {
                Text = "Gestión de Inventario",
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = textWhite,
                Location = new Point(0, 5),
                AutoSize = true
            };
            headerPanel.Controls.Add(lblTitle);

            Button btnAddNew = CrearBotonAccion(" +  Nuevo Producto", Color.FromArgb(16, 185, 129), Color.White);
            btnAddNew.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnAddNew.Location = new Point(headerPanel.Width - 140, 5);
            btnAddNew.Click += (s, e) => MessageBox.Show("Abrir formulario de alta de producto");

            Button btnAction = CrearBotonAccion("Acciones 🞃", Color.FromArgb(45, 60, 75), textWhite);
            btnAction.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnAction.Location = new Point(headerPanel.Width - 270, 5);

            headerPanel.Controls.Add(btnAddNew);
            headerPanel.Controls.Add(btnAction);
            headerPanel.Resize += (s, e) =>
            {
                btnAddNew.Location = new Point(headerPanel.Width - 140, 5);
                btnAction.Location = new Point(headerPanel.Width - 270, 5);
            };

            mainLayout.Controls.Add(headerPanel, 0, 0);

            // 2. FILA DE TARJETAS CON GRADIENTE
            FlowLayoutPanel rowCounters = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Margin = new Padding(0, 0, 0, 15)
            };

            rowCounters.Controls.Add(CrearCardGradiente("Total Productos", "1,248", "+2.0% este mes", Color.FromArgb(13, 148, 136), Color.FromArgb(20, 184, 166)));
            rowCounters.Controls.Add(CrearCardGradiente("Marcas Activas", "42", "+1.0% este mes", Color.FromArgb(88, 28, 135), Color.FromArgb(147, 51, 234)));
            rowCounters.Controls.Add(CrearCardGradiente("Alertas Stock", "15", "Revisión urgente", Color.FromArgb(180, 83, 9), Color.FromArgb(245, 158, 11)));
            rowCounters.Controls.Add(CrearCardGradiente("Valor Inventario", "$142.5K", "+12% rendimiento", Color.FromArgb(190, 24, 93), Color.FromArgb(236, 72, 153)));

            mainLayout.Controls.Add(rowCounters, 0, 1);

            // 3. TABLA DE DATOS
            Panel cardTable = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = bgCard,
                Padding = new Padding(10),
                Margin = new Padding(0)
            };
            AplicarBordesRedondeados(cardTable, 16);

            dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = bgCard,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                EnableHeadersVisualStyles = false,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, // Ajuste automático de columnas
                RowTemplate = { Height = 40 }
            };

            // Estilos Header
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(20, 30, 38);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = textMuted;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);

            // Estilos Celdas
            dgv.DefaultCellStyle.BackColor = bgCard;
            dgv.DefaultCellStyle.ForeColor = textWhite;
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(40, 58, 73);
            dgv.DefaultCellStyle.SelectionForeColor = accentTeal;
            dgv.GridColor = Color.FromArgb(40, 55, 68);

            // Columnas
            dgv.Columns.Add("SKU", "SKU");
            dgv.Columns.Add("Titulo", "Producto");
            dgv.Columns.Add("Categoria", "Categoría");
            dgv.Columns.Add("QTY", "Stock");
            dgv.Columns.Add("Ubicacion", "Almacén");
            dgv.Columns.Add("Precio", "Precio");
            dgv.Columns.Add("Estado", "Estado");

            // Proporciones de columnas
            dgv.Columns[0].FillWeight = 80;
            dgv.Columns[1].FillWeight = 200;
            dgv.Columns[2].FillWeight = 110;
            dgv.Columns[3].FillWeight = 70;
            dgv.Columns[4].FillWeight = 120;
            dgv.Columns[5].FillWeight = 90;
            dgv.Columns[6].FillWeight = 90;

            // Datos de prueba
            dgv.Rows.Add("MRP400", "Chaqueta de Cuero Premium", "Ropa", "22", "Almacén Central", "$223.20", "Disponible");
            dgv.Rows.Add("MRP405", "Botas de Montaña ABCD", "Calzado", "35", "Ubicación A-5", "$120.00", "Disponible");
            dgv.Rows.Add("MRP620", "Soporte Ergonómico Monitor", "Accesorios", "8", "Ubicación B-2", "$45.50", "Bajo Stock");
            dgv.Rows.Add("MRP052", "Zapatillas Deportivas XYZ", "Calzado", "12", "Almacén Central", "$89.99", "Disponible");
            dgv.Rows.Add("MRP890", "Teclado Mecánico Inalámbrico", "Electrónica", "0", "Ubicación C-1", "$110.00", "Agotado");

            cardTable.Controls.Add(dgv);
            mainLayout.Controls.Add(cardTable, 0, 2);
        }

        private Panel CrearCardGradiente(string titulo, string valor, string subtexto, Color colorInicio, Color colorFin)
        {
            Panel card = new Panel
            {
                Size = new Size(220, 85),
                Margin = new Padding(0, 0, 15, 10)
            };
            AplicarBordesRedondeados(card, 14);

            card.Paint += (s, e) =>
            {
                using (LinearGradientBrush brush = new LinearGradientBrush(card.ClientRectangle, colorInicio, colorFin, LinearGradientMode.Horizontal))
                {
                    e.Graphics.FillRectangle(brush, card.ClientRectangle);
                }
            };

            Label lblT = new Label
            {
                Text = titulo,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 240, 255),
                Location = new Point(12, 10),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            card.Controls.Add(lblT);

            Label lblV = new Label
            {
                Text = valor,
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(10, 28),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            card.Controls.Add(lblV);

            Label lblSub = new Label
            {
                Text = subtexto,
                Font = new Font("Segoe UI", 7.5f),
                ForeColor = Color.FromArgb(230, 230, 230),
                Location = new Point(12, 60),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            card.Controls.Add(lblSub);

            return card;
        }

        private Button CrearBotonAccion(string texto, Color bg, Color fg)
        {
            Button btn = new Button
            {
                Text = texto,
                Size = new Size(120, 36),
                BackColor = bg,
                ForeColor = fg,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            AplicarBordesRedondeados(btn, 8);
            return btn;
        }

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