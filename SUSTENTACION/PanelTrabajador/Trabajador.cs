#nullable disable
using System;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Npgsql;

namespace SUSTENTACION.PanelTrabajador
{
    public class Trabajador : UserControl
    {
        private readonly Color fondo = Color.FromArgb(242, 246, 249);
        private readonly Color oscuro = Color.FromArgb(24, 42, 55);
        private readonly Color verde = Color.FromArgb(18, 124, 91);
        private DataGridView tablaInternos, tablaExternos;
        private TabControl pestanas;
        private TextBox buscar;
        private ComboBox filtroEstado;
        private Label[] indicadores = new Label[4];
        private Label estadoConexion, cantidad;
        private Button nuevo, editar, cambiarEstado, ficha;
        private Button editarInterno;
        private DataTable internos, externos;
        private DataTable departamentosDatos, rolesDatos;
        private bool cargando;
        private int? seleccionExterno;

        public Trabajador()
        {
            Dock = DockStyle.Fill;
            BackColor = fondo;
            Font = new Font("Segoe UI", 9);
            CrearInterfaz();
            Load += (s, e) => ActualizarDatos();
        }

        private Label Etiqueta(string texto, float tamano = 9, bool negrita = false)
        {
            return new Label
            {
                Text = texto,
                AutoSize = true,
                ForeColor = oscuro,
                Font = new Font("Segoe UI", tamano, negrita ? FontStyle.Bold : FontStyle.Regular)
            };
        }

        private Button Boton(string texto, EventHandler evento, bool principal = false, int ancho = 130)
        {
            Button b = new Button
            {
                Text = texto,
                Height = 38,
                Width = ancho,
                Margin = new Padding(0, 0, 9, 0),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                BackColor = principal ? verde : Color.White,
                ForeColor = principal ? Color.White : oscuro,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            b.FlatAppearance.BorderColor = Color.FromArgb(213, 226, 232);
            b.Click += evento;
            return b;
        }

        private void CrearInterfaz()
        {
            AutoScroll = true;
            Panel pagina = new Panel { BackColor = fondo, Padding = new Padding(20), Location = Point.Empty };
            Controls.Add(pagina);
            // Al crearse el control, ClientSize puede ser 0. Se reajusta al mostrarse.
            Action ajustar = () =>
            {
                if (IsDisposed || pagina.IsDisposed) return;
                pagina.Size = new Size(Math.Max(1080, ClientSize.Width - 20),
                                       Math.Max(790, ClientSize.Height - 2));
            };
            Resize += (s, e) => ajustar();
            HandleCreated += (s, e) => ajustar();
            Load += (s, e) => ajustar();
            ajustar();
            TableLayoutPanel diseno = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6 };
            diseno.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            diseno.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
            diseno.RowStyles.Add(new RowStyle(SizeType.Absolute, 125));
            diseno.RowStyles.Add(new RowStyle(SizeType.Absolute, 85));
            diseno.RowStyles.Add(new RowStyle(SizeType.Absolute, 53));
            diseno.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            diseno.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            pagina.Controls.Add(diseno);

            Panel encabezado = new Panel { Dock = DockStyle.Fill, BackColor = oscuro, Margin = new Padding(0, 0, 0, 14) };
            Label marca = Etiqueta("ARTEMUSA TV / GESTIÓN DE PERSONAL", 8, true);
            marca.ForeColor = Color.FromArgb(157, 219, 196); marca.Location = new Point(22, 12);
            Label titulo = Etiqueta("Gestión de trabajadores", 22, true);
            titulo.ForeColor = Color.White; titulo.Location = new Point(20, 32);
            Label subtitulo = Etiqueta("Personal interno y técnicos externos responsables de equipos audiovisuales", 9);
            subtitulo.ForeColor = Color.FromArgb(187, 204, 211); subtitulo.Location = new Point(22, 76);
            Button actualizar = Boton("Actualizar", (s, e) => ActualizarDatos(), false, 125);
            nuevo = Boton("+ Nuevo técnico externo", (s, e) => AbrirEditor(false), true, 185);
            encabezado.Controls.AddRange(new Control[] { marca, titulo, subtitulo, actualizar, nuevo });
            encabezado.Resize += (s, e) => {
                nuevo.Location = new Point(encabezado.Width - nuevo.Width - 20, 34);
                actualizar.Location = new Point(nuevo.Left - actualizar.Width - 12, 34);
            };
            diseno.Controls.Add(encabezado, 0, 0);

            TableLayoutPanel tarjetas = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1 };
            string[] nombres = { "TRABAJADORES INTERNOS", "TÉCNICOS EXTERNOS", "EXTERNOS ACTIVOS", "DEPARTAMENTOS" };
            Color[] colores = { Color.FromArgb(231, 242, 251), Color.FromArgb(226, 243, 232), Color.FromArgb(255, 243, 222), Color.FromArgb(240, 234, 251) };
            for (int i = 0; i < 4; i++)
            {
                tarjetas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
                Panel tarjeta = new Panel { Dock = DockStyle.Fill, BackColor = colores[i], Margin = new Padding(0, 0, 12, 13) };
                Label nombre = Etiqueta(nombres[i], 8, true); nombre.Location = new Point(15, 14);
                indicadores[i] = Etiqueta("—", 26, true); indicadores[i].Location = new Point(13, 38);
                tarjeta.Controls.Add(nombre); tarjeta.Controls.Add(indicadores[i]); tarjetas.Controls.Add(tarjeta, i, 0);
            }
            diseno.Controls.Add(tarjetas, 0, 1);

            Panel filtros = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Margin = new Padding(0, 0, 0, 12), Padding = new Padding(12) };
            TableLayoutPanel f = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 2 };
            f.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            f.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 195));
            f.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            f.RowStyles.Add(new RowStyle(SizeType.Absolute, 23));
            f.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            f.Controls.Add(Etiqueta("BUSCAR POR NOMBRE, DNI O CONTACTO", 8, true), 0, 0);
            f.Controls.Add(Etiqueta("ESTADO", 8, true), 1, 0);
            buscar = new TextBox { Dock = DockStyle.Top, Font = new Font("Segoe UI", 10), Margin = new Padding(0, 0, 12, 0) };
            filtroEstado = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 0, 12, 0) };
            filtroEstado.Items.AddRange(new object[] { "Todos los estados", "Activo", "Inactivo" });
            filtroEstado.SelectedIndex = 0;
            Button limpiar = Boton("Limpiar", (s, e) => { buscar.Clear(); filtroEstado.SelectedIndex = 0; }, false, 100);
            limpiar.Height = 29;
            f.Controls.Add(buscar, 0, 1); f.Controls.Add(filtroEstado, 1, 1); f.Controls.Add(limpiar, 2, 1);
            filtros.Controls.Add(f); diseno.Controls.Add(filtros, 0, 2);
            buscar.TextChanged += (s, e) => Filtrar();
            filtroEstado.SelectedIndexChanged += (s, e) => Filtrar();

            FlowLayoutPanel acciones = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
            editar = Boton("Editar técnico", (s, e) => AbrirEditor(true), false, 125);
            cambiarEstado = Boton("Desactivar", (s, e) => CambiarEstado(), false, 125);
            ficha = Boton("Ver ficha", (s, e) => VerFicha(), false, 115);
            editarInterno = Boton("Editar trabajador", (s, e) => EditarTrabajadorInterno(), false, 145);
            Button exportar = Boton("Exportar CSV", (s, e) => ExportarCsv(), false, 125);
            cantidad = Etiqueta("Sin registros", 9); cantidad.Margin = new Padding(10, 11, 0, 0);
            acciones.Controls.AddRange(new Control[] { editar, cambiarEstado, ficha, editarInterno, exportar, cantidad });
            diseno.Controls.Add(acciones, 0, 3);

            pestanas = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            TabPage tabI = new TabPage("Trabajadores internos") { Padding = new Padding(10), BackColor = Color.White };
            TabPage tabE = new TabPage("Personal externo") { Padding = new Padding(10), BackColor = Color.White };
            tablaInternos = CrearTabla(); tablaExternos = CrearTabla();
            tablaInternos.SelectionChanged += (s, e) => ActualizarBotones();
            tabI.Controls.Add(tablaInternos); tabE.Controls.Add(tablaExternos);
            pestanas.TabPages.Add(tabI); pestanas.TabPages.Add(tabE);
            pestanas.SelectedIndex = 1;
            pestanas.SelectedIndexChanged += (s, e) => { Filtrar(); ActualizarBotones(); };
            tablaExternos.SelectionChanged += (s, e) => { DataRow r = Seleccionado(); if (r != null) seleccionExterno = Convert.ToInt32(r["id_personal"]); ActualizarBotones(); };
            tablaExternos.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && tablaExternos.Columns[e.ColumnIndex].Name != "fecha_registro") VerFicha(); };
            // WhatsApp integrado a la derecha de la fecha, sin agregar otra columna.
            tablaExternos.CellPainting += PintarFechaConWhatsApp;
            tablaExternos.CellMouseClick += (s, e) => {
                if (e.RowIndex < 0 || e.ColumnIndex < 0 ||
                    tablaExternos.Columns[e.ColumnIndex].Name != "fecha_registro") return;
                Rectangle celda = tablaExternos.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
                if (e.X >= celda.Width - 39)
                {
                    DataRowView vista = tablaExternos.Rows[e.RowIndex].DataBoundItem as DataRowView;
                    if (vista != null) AbrirWhatsApp(vista.Row);
                }
            };
            tablaExternos.CellMouseMove += (s, e) => {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
                if (tablaExternos.Columns[e.ColumnIndex].Name == "fecha_registro")
                    tablaExternos.Rows[e.RowIndex].Cells[e.ColumnIndex].ToolTipText =
                        "Haz clic en el icono verde para abrir WhatsApp";
            };
            diseno.Controls.Add(pestanas, 0, 4);
            estadoConexion = Etiqueta("Conectando con Supabase...", 9);
            estadoConexion.Dock = DockStyle.Fill;
            diseno.Controls.Add(estadoConexion, 0, 5);
            ActualizarBotones();
        }

        private DataGridView CrearTabla()
        {
            DataGridView t = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EnableHeadersVisualStyles = false
            };
            t.RowTemplate.Height = 42;
            t.ColumnHeadersHeight = 42;
            t.ColumnHeadersDefaultCellStyle.BackColor = fondo;
            t.ColumnHeadersDefaultCellStyle.ForeColor = oscuro;
            t.DefaultCellStyle.SelectionBackColor = Color.FromArgb(222, 242, 232);
            t.DefaultCellStyle.SelectionForeColor = oscuro;
            t.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 252, 253);
            return t;
        }

        private NpgsqlConnection Conectar() { return new global::SUSTENTACION.Conexion().ObtenerConexion(); }
        public void CargarTrabajadores() { ActualizarDatos(); }

        private void ActualizarDatos()
        {
            if (cargando) return;
            cargando = true;
            try
            {
                DataTable nuevosI = new DataTable(), nuevosE = new DataTable();
                int departamentos;
                DataTable nuevosD = new DataTable(), nuevosR = new DataTable();
                using (NpgsqlConnection c = Conectar())
                {
                    const string qI = @"SELECT t.id_trabajador AS id, t.nombre AS nombre,
                        COALESCE(t.dni,'') AS dni, COALESCE(t.telefono,'') AS telefono,
                        t.correo, t.id_rol, t.id_departamento, COALESCE(r.nombre_rol,'Sin rol') AS rol,
                         COALESCE(d.nombre_departamento,'Sin departamento') AS departamento,
                        CASE WHEN t.activo THEN 'Activo' ELSE 'Inactivo' END AS estado
                        FROM public.trabajadores t
                        LEFT JOIN public.departamentos d ON d.id_departamento=t.id_departamento
                        LEFT JOIN public.roles r ON r.id_rol=t.id_rol
                        ORDER BY t.nombre";
                    using (NpgsqlDataAdapter a = new NpgsqlDataAdapter(qI, c)) a.Fill(nuevosI);
                    const string qE = @"SELECT id_personal, nombre, apellido, dni, celular,
                        direccion, activo, fecha_registro,
                        CASE WHEN activo THEN 'Activo' ELSE 'Inactivo' END AS estado
                        FROM public.personal_externo ORDER BY id_personal DESC";
                    using (NpgsqlDataAdapter a = new NpgsqlDataAdapter(qE, c)) a.Fill(nuevosE);
                    using (NpgsqlDataAdapter a = new NpgsqlDataAdapter(
                        "SELECT id_departamento, nombre_departamento FROM public.departamentos ORDER BY nombre_departamento", c))
                        a.Fill(nuevosD);
                    using (NpgsqlDataAdapter a = new NpgsqlDataAdapter(
                        "SELECT id_rol, nombre_rol FROM public.roles ORDER BY nombre_rol", c))
                        a.Fill(nuevosR);
                    departamentos = nuevosD.Rows.Count;
                }
                nuevosE.Columns.Add("nombre_completo", typeof(string), "nombre + ' ' + apellido");
                DataTable oldI = internos, oldE = externos;
                internos = nuevosI; externos = nuevosE;
                if (departamentosDatos != null) departamentosDatos.Dispose();
                if (rolesDatos != null) rolesDatos.Dispose();
                departamentosDatos = nuevosD; rolesDatos = nuevosR;
                tablaInternos.DataSource = internos;
                tablaExternos.DataSource = externos;
                if (oldI != null) oldI.Dispose(); if (oldE != null) oldE.Dispose();
                ConfigurarColumnas();
                indicadores[0].Text = internos.Rows.Count.ToString();
                indicadores[1].Text = externos.Rows.Count.ToString();
                int activos = 0;
                foreach (DataRow r in externos.Rows) if (Convert.ToBoolean(r["activo"])) activos++;
                indicadores[2].Text = activos.ToString();
                indicadores[3].Text = departamentos.ToString();
                estadoConexion.Text = "● Conectado a Supabase · " + DateTime.Now.ToString("HH:mm:ss");
                estadoConexion.ForeColor = verde;
                if (seleccionExterno.HasValue) RestaurarSeleccion(seleccionExterno.Value);
            }
            catch (Exception ex)
            {
                estadoConexion.Text = "Error al cargar datos: " + ex.Message;
                estadoConexion.ForeColor = Color.Firebrick;
                for (int i = 0; i < indicadores.Length; i++) indicadores[i].Text = "Error";
                MessageBox.Show("Error al consultar Supabase:\n\n" + ex.Message, "Artemusa", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally { cargando = false; Filtrar(); ActualizarBotones(); }
        }

        private void RestaurarSeleccion(int id)
        {
            foreach (DataGridViewRow row in tablaExternos.Rows)
            {
                DataRowView d = row.DataBoundItem as DataRowView;
                if (d != null && Convert.ToInt32(d["id_personal"]) == id)
                {
                    if (tablaExternos.Columns.Contains("nombre_completo"))
                        tablaExternos.CurrentCell = row.Cells["nombre_completo"];
                    break;
                }
            }
        }

        private void ConfigurarColumnas()
        {
            foreach (DataGridViewColumn c in tablaInternos.Columns) c.Visible = false;
            Mostrar(tablaInternos, "nombre", "Trabajador", 0);
            Mostrar(tablaInternos, "dni", "DNI", 1);
            Mostrar(tablaInternos, "telefono", "Teléfono", 2);
            Mostrar(tablaInternos, "correo", "Correo", 3);
            Mostrar(tablaInternos, "departamento", "Departamento", 4);
            Mostrar(tablaInternos, "rol", "Rol", 5);
            Mostrar(tablaInternos, "estado", "Estado", 6);
            foreach (DataGridViewColumn c in tablaExternos.Columns) c.Visible = false;
            Mostrar(tablaExternos, "nombre_completo", "Técnico externo", 0);
            Mostrar(tablaExternos, "dni", "DNI", 1);
            Mostrar(tablaExternos, "celular", "Celular", 2);
            Mostrar(tablaExternos, "direccion", "Dirección", 3);
            Mostrar(tablaExternos, "estado", "Estado", 4);
            Mostrar(tablaExternos, "fecha_registro", "Fecha de registro", 5);
            // La fecha y el icono comparten una sola celda.
            if (tablaExternos.Columns.Contains("fecha_registro"))
            {
                tablaExternos.Columns["fecha_registro"].DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
                tablaExternos.Columns["fecha_registro"].MinimumWidth = 205;
                tablaExternos.Columns["fecha_registro"].FillWeight = 145;
            }
        }

        private void PintarFechaConWhatsApp(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 ||
                tablaExternos.Columns[e.ColumnIndex].Name != "fecha_registro") return;

            e.Handled = true;
            e.Paint(e.CellBounds, DataGridViewPaintParts.Background | DataGridViewPaintParts.Border);
            bool seleccionada = (e.State & DataGridViewElementStates.Selected) != 0;
            Color colorTexto = seleccionada ? tablaExternos.DefaultCellStyle.SelectionForeColor : oscuro;
            string fecha = "";
            if (e.Value != null && e.Value != DBNull.Value)
                fecha = Convert.ToDateTime(e.Value).ToString("dd/MM/yyyy HH:mm");

            Rectangle areaFecha = new Rectangle(e.CellBounds.X + 8, e.CellBounds.Y + 2,
                Math.Max(10, e.CellBounds.Width - 49), e.CellBounds.Height - 4);
            TextRenderer.DrawText(e.Graphics, fecha, e.CellStyle.Font ?? tablaExternos.Font,
                areaFecha, colorTexto, TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);

            int cx = e.CellBounds.Right - 23;
            int cy = e.CellBounds.Top + e.CellBounds.Height / 2;
            Rectangle circulo = new Rectangle(cx - 13, cy - 13, 26, 26);
            using (SolidBrush pincel = new SolidBrush(Color.FromArgb(37, 174, 87)))
                e.Graphics.FillEllipse(pincel, circulo);
            TextRenderer.DrawText(e.Graphics, "☎", new Font("Segoe UI Symbol", 12, FontStyle.Bold),
                circulo, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.NoPadding);
        }

        private static void Mostrar(DataGridView tabla, string columna, string titulo, int orden)
        {
            if (tabla.Columns.Contains(columna))
            {
                tabla.Columns[columna].Visible = true;
                tabla.Columns[columna].HeaderText = titulo;
                tabla.Columns[columna].DisplayIndex = orden;
                tabla.Columns[columna].MinimumWidth = columna == "nombre_completo" || columna == "tecnico" ? 165 : 100;
            }
        }

        private static string EscaparFiltro(string s)
        {
            return (s ?? "").Replace("'", "''").Replace("[", "[[]").Replace("]", "[]]")
                .Replace("%", "[%]").Replace("*", "[*]");
        }

        private void Filtrar()
        {
            if (cargando || buscar == null || cantidad == null || pestanas == null) return;
            DataTable datos = pestanas.SelectedIndex == 1 ? externos : internos;
            if (datos == null) { cantidad.Text = "Sin registros"; return; }
            string texto = EscaparFiltro(buscar.Text.Trim());
            string[] campos = pestanas.SelectedIndex == 1
                ? new[] { "nombre", "apellido", "dni", "celular", "direccion" }
                : new[] { "nombre", "dni", "telefono", "correo", "departamento" };
            string condicion = "";
            if (texto.Length > 0)
            {
                string[] partes = new string[campos.Length];
                for (int i = 0; i < campos.Length; i++) partes[i] = "Convert([" + campos[i] + "], 'System.String') LIKE '%" + texto + "%'";
                condicion = "(" + string.Join(" OR ", partes) + ")";
            }
            if (filtroEstado.SelectedIndex > 0)
            {
                if (condicion.Length > 0) condicion += " AND ";
                condicion += "[estado] = '" + filtroEstado.SelectedItem + "'";
            }
            try { datos.DefaultView.RowFilter = condicion; }
            catch (EvaluateException) { datos.DefaultView.RowFilter = ""; }
            cantidad.Text = datos.DefaultView.Count + (pestanas.SelectedIndex == 1 ? " técnicos visibles" : " trabajadores visibles");
            ActualizarBotones();
        }

        private DataRow Seleccionado()
        {
            if (tablaExternos == null || tablaExternos.CurrentRow == null) return null;
            DataRowView v = tablaExternos.CurrentRow.DataBoundItem as DataRowView;
            return v == null ? null : v.Row;
        }

        private void ActualizarBotones()
        {
            if (nuevo == null || editar == null || cambiarEstado == null || pestanas == null) return;
            bool externo = pestanas.SelectedIndex == 1;
            DataRow r = externo ? Seleccionado() : null;
            nuevo.Enabled = externo && !cargando;
            editarInterno.Visible = !externo;
            editarInterno.Enabled = !externo && tablaInternos.CurrentRow != null && !cargando;
            editar.Visible = externo;
            cambiarEstado.Visible = externo;
            ficha.Visible = externo;
            editar.Enabled = externo && r != null && !cargando;
            cambiarEstado.Enabled = editar.Enabled;
            ficha.Enabled = editar.Enabled;
            cambiarEstado.Text = editar.Enabled && !Convert.ToBoolean(r["activo"]) ? "Reactivar" : "Desactivar";
        }

        private TextBox Campo(TableLayoutPanel panel, string etiqueta, string valor, int maximo)
        {
            Panel contenedor = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5) };
            Label titulo = Etiqueta(etiqueta, 9, true); titulo.Dock = DockStyle.Top;
            TextBox caja = new TextBox { Text = valor ?? "", MaxLength = maximo, Dock = DockStyle.Bottom, Font = new Font("Segoe UI", 10) };
            contenedor.Controls.Add(caja); contenedor.Controls.Add(titulo); panel.Controls.Add(contenedor);
            return caja;
        }

        private void AbrirEditor(bool editarExistente)
        {
            if (pestanas.SelectedIndex != 1) return;
            DataRow registro = editarExistente ? Seleccionado() : null;
            if (editarExistente && registro == null) return;
            using (Form ventana = new Form
            {
                Text = editarExistente ? "Editar técnico externo" : "Nuevo técnico externo",
                StartPosition = FormStartPosition.CenterParent,
                ClientSize = new Size(620, 490),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 9)
            })
            {
                Label titulo = Etiqueta(editarExistente ? "Editar técnico externo" : "Registrar técnico externo", 20, true);
                titulo.Location = new Point(22, 18); ventana.Controls.Add(titulo);
                Label nota = Etiqueta("Datos del técnico que podrá ser responsable de equipos audiovisuales.", 9);
                nota.Location = new Point(25, 62); ventana.Controls.Add(nota);
                TableLayoutPanel formulario = new TableLayoutPanel { Location = new Point(20, 100), Size = new Size(580, 310), ColumnCount = 2, RowCount = 3 };
                formulario.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
                formulario.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
                for (int i = 0; i < 3; i++) formulario.RowStyles.Add(new RowStyle(SizeType.Percent, 33.333f));
                ventana.Controls.Add(formulario);
                Func<string, string> valor = clave => registro == null ? "" : Convert.ToString(registro[clave]);
                TextBox nombre = Campo(formulario, "Nombres *", valor("nombre"), 100);
                TextBox apellido = Campo(formulario, "Apellidos *", valor("apellido"), 100);
                TextBox dni = Campo(formulario, "DNI (8 dígitos) *", valor("dni"), 8);
                TextBox celular = Campo(formulario, "Celular (9 dígitos) *", valor("celular"), 9);
                TextBox direccion = Campo(formulario, "Dirección", valor("direccion"), 150);
                Panel estadoPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5) };
                Label et = Etiqueta("Estado", 9, true); et.Dock = DockStyle.Top;
                CheckBox activo = new CheckBox { Text = "Técnico activo", Checked = registro == null || Convert.ToBoolean(registro["activo"]), Dock = DockStyle.Bottom };
                estadoPanel.Controls.Add(activo); estadoPanel.Controls.Add(et); formulario.Controls.Add(estadoPanel);
                Button cancelar = Boton("Cancelar", (s, e) => ventana.Close()); cancelar.Location = new Point(310, 430);
                Button guardar = Boton("Guardar técnico", (s, e) => { }, true, 145); guardar.Location = new Point(455, 430);
                ventana.Controls.Add(cancelar); ventana.Controls.Add(guardar); ventana.CancelButton = cancelar;
                guardar.Click += (s, e) => {
                    if (string.IsNullOrWhiteSpace(nombre.Text) || string.IsNullOrWhiteSpace(apellido.Text)
                        || !Regex.IsMatch(dni.Text.Trim(), @"^\d{8}$") || !Regex.IsMatch(celular.Text.Trim(), @"^9\d{8}$"))
                    {
                        MessageBox.Show("Completa nombres, apellidos, DNI de 8 dígitos y celular peruano de 9 dígitos que empieza por 9.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    guardar.Enabled = false;
                    try
                    {
                        using (NpgsqlConnection c = Conectar())
                        {
                            string sql = editarExistente
                                ? @"UPDATE public.personal_externo SET nombre=@nombre, apellido=@apellido,
                                    dni=@dni, celular=@celular, direccion=@direccion, activo=@activo WHERE id_personal=@id"
                                : @"INSERT INTO public.personal_externo (nombre, apellido, dni, celular, direccion, activo)
                                    VALUES (@nombre, @apellido, @dni, @celular, @direccion, @activo)";
                            using (NpgsqlCommand cmd = new NpgsqlCommand(sql, c))
                            {
                                cmd.Parameters.AddWithValue("@nombre", nombre.Text.Trim());
                                cmd.Parameters.AddWithValue("@apellido", apellido.Text.Trim());
                                cmd.Parameters.AddWithValue("@dni", dni.Text.Trim());
                                cmd.Parameters.AddWithValue("@celular", celular.Text.Trim());
                                cmd.Parameters.AddWithValue("@direccion", direccion.Text.Trim());
                                cmd.Parameters.AddWithValue("@activo", activo.Checked);
                                if (editarExistente) cmd.Parameters.AddWithValue("@id", Convert.ToInt32(registro["id_personal"]));
                                if (cmd.ExecuteNonQuery() != 1) throw new InvalidOperationException("No se guardó el registro esperado.");
                            }
                        }
                        ventana.DialogResult = DialogResult.OK; ventana.Close();
                    }
                    catch (PostgresException ex) when (ex.SqlState == "23505")
                    {
                        MessageBox.Show("Ya existe un registro con ese DNI.", "Registro duplicado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    catch (Exception ex) { MessageBox.Show("No se pudo guardar:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                    finally { guardar.Enabled = true; }
                };
                if (ventana.ShowDialog(FindForm()) == DialogResult.OK) ActualizarDatos();
            }
        }


        // Administración de datos del personal interno. No cambia credenciales ni contraseñas.
        private void EditarTrabajadorInterno()
        {
            if (pestanas.SelectedIndex != 0 || tablaInternos.CurrentRow == null) return;
            DataRowView vista = tablaInternos.CurrentRow.DataBoundItem as DataRowView;
            if (vista == null) return;
            DataRow registro = vista.Row;
            if (departamentosDatos == null || rolesDatos == null || rolesDatos.Rows.Count == 0)
            {
                MessageBox.Show("No hay roles disponibles en Supabase.", "Artemusa");
                return;
            }
            using (Form ventana = new Form
            {
                Text = "Editar trabajador interno - Artemusa",
                StartPosition = FormStartPosition.CenterParent,
                ClientSize = new Size(610, 520),
                BackColor = Color.White,
                Font = new Font("Segoe UI", 9),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            })
            {
                Label titulo = Etiqueta("Editar trabajador interno", 19, true);
                titulo.Location = new Point(22, 18);
                ventana.Controls.Add(titulo);
                Label nota = Etiqueta("Datos administrativos. La contraseña y el acceso no se modifican aquí.", 9);
                nota.Location = new Point(24, 63);
                ventana.Controls.Add(nota);
                TableLayoutPanel formulario = new TableLayoutPanel
                {
                    Location = new Point(20, 98),
                    Size = new Size(570, 330),
                    ColumnCount = 2,
                    RowCount = 4
                };
                formulario.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
                formulario.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
                for (int i = 0; i < 4; i++) formulario.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
                ventana.Controls.Add(formulario);
                TextBox nombre = Campo(formulario, "Nombre completo *", Convert.ToString(registro["nombre"]), 150);
                TextBox dni = Campo(formulario, "DNI (8 dígitos)", Convert.ToString(registro["dni"]), 8);
                TextBox telefono = Campo(formulario, "Celular (9 dígitos)", Convert.ToString(registro["telefono"]), 9);
                TextBox correo = Campo(formulario, "Correo electrónico *", Convert.ToString(registro["correo"]), 150);
                ComboBox departamento = Selector(formulario, "Departamento (opcional)");
                departamento.Items.Add(new Opcion(-1, "Sin departamento"));
                foreach (DataRow d in departamentosDatos.Rows)
                    departamento.Items.Add(new Opcion(Convert.ToInt32(d["id_departamento"]), Convert.ToString(d["nombre_departamento"])));
                int idDepartamento = registro["id_departamento"] == DBNull.Value ? -1 : Convert.ToInt32(registro["id_departamento"]);
                SeleccionarOpcion(departamento, idDepartamento);
                ComboBox rol = Selector(formulario, "Rol *");
                foreach (DataRow d in rolesDatos.Rows)
                    rol.Items.Add(new Opcion(Convert.ToInt32(d["id_rol"]), Convert.ToString(d["nombre_rol"])));
                SeleccionarOpcion(rol, Convert.ToInt32(registro["id_rol"]));
                Panel estadoPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5) };
                Label etiqueta = Etiqueta("Estado", 9, true); etiqueta.Dock = DockStyle.Top;
                CheckBox activo = new CheckBox { Text = "Trabajador activo", Checked = Convert.ToString(registro["estado"]) == "Activo", Dock = DockStyle.Bottom };
                estadoPanel.Controls.Add(activo); estadoPanel.Controls.Add(etiqueta); formulario.Controls.Add(estadoPanel);
                Button cancelar = Boton("Cancelar", (s, e) => ventana.Close()); cancelar.Location = new Point(305, 450);
                Button guardar = Boton("Guardar cambios", (s, e) => { }, true, 155); guardar.Location = new Point(435, 450);
                ventana.Controls.Add(cancelar); ventana.Controls.Add(guardar); ventana.CancelButton = cancelar;
                guardar.Click += (s, e) =>
                {
                    string doc = dni.Text.Trim();
                    string tel = telefono.Text.Trim();
                    if (string.IsNullOrWhiteSpace(nombre.Text) ||
                        !Regex.IsMatch(correo.Text.Trim(), @"^[^\s@]+@[^\s@]+\.[^\s@]+$") ||
                        (doc.Length > 0 && !Regex.IsMatch(doc, @"^\d{8}$")) ||
                        (tel.Length > 0 && !Regex.IsMatch(tel, @"^9\d{8}$")) || rol.SelectedItem == null)
                    {
                        MessageBox.Show("Verifica nombre, correo, DNI de 8 dígitos, celular peruano y rol.", "Validación");
                        return;
                    }
                    guardar.Enabled = false;
                    try
                    {
                        using (NpgsqlConnection c = Conectar())
                        using (NpgsqlCommand cmd = new NpgsqlCommand(@"
                            UPDATE public.trabajadores
                            SET nombre=@nombre, dni=@dni, telefono=@telefono, correo=@correo,
                                id_departamento=@departamento, id_rol=@rol, activo=@activo,
                                fecha_actualizacion=NOW()
                            WHERE id_trabajador=@id", c))
                        {
                            cmd.Parameters.AddWithValue("@nombre", nombre.Text.Trim());
                            cmd.Parameters.Add("@dni", NpgsqlTypes.NpgsqlDbType.Varchar).Value = doc.Length == 0 ? (object)DBNull.Value : doc;
                            cmd.Parameters.Add("@telefono", NpgsqlTypes.NpgsqlDbType.Varchar).Value = tel.Length == 0 ? (object)DBNull.Value : tel;
                            cmd.Parameters.AddWithValue("@correo", correo.Text.Trim());
                            Opcion dep = departamento.SelectedItem as Opcion;
                            cmd.Parameters.Add("@departamento", NpgsqlTypes.NpgsqlDbType.Integer).Value = dep == null || dep.Id < 0 ? (object)DBNull.Value : dep.Id;
                            cmd.Parameters.AddWithValue("@rol", ((Opcion)rol.SelectedItem).Id);
                            cmd.Parameters.AddWithValue("@activo", activo.Checked);
                            cmd.Parameters.AddWithValue("@id", Convert.ToInt32(registro["id"]));
                            if (cmd.ExecuteNonQuery() != 1) throw new InvalidOperationException("No se actualizó el trabajador.");
                        }
                        ventana.DialogResult = DialogResult.OK;
                        ventana.Close();
                    }
                    catch (PostgresException ex) when (ex.SqlState == "23505")
                    {
                        MessageBox.Show("El DNI o correo ya está registrado.", "Duplicado");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("No se pudieron guardar los cambios: " + ex.Message, "Error");
                    }
                    finally { guardar.Enabled = true; }
                };
                if (ventana.ShowDialog(FindForm()) == DialogResult.OK) ActualizarDatos();
            }
        }

        private sealed class Opcion
        {
            public int Id { get; private set; }
            public string Nombre { get; private set; }
            public Opcion(int id, string nombre) { Id = id; Nombre = nombre; }
            public override string ToString() { return Nombre; }
        }

        private ComboBox Selector(TableLayoutPanel panel, string etiqueta)
        {
            Panel contenedor = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5) };
            Label titulo = Etiqueta(etiqueta, 9, true); titulo.Dock = DockStyle.Top;
            ComboBox lista = new ComboBox { Dock = DockStyle.Bottom, DropDownStyle = ComboBoxStyle.DropDownList };
            contenedor.Controls.Add(lista); contenedor.Controls.Add(titulo); panel.Controls.Add(contenedor);
            return lista;
        }

        private static void SeleccionarOpcion(ComboBox lista, int id)
        {
            for (int i = 0; i < lista.Items.Count; i++)
            {
                Opcion opcion = lista.Items[i] as Opcion;
                if (opcion != null && opcion.Id == id) { lista.SelectedIndex = i; return; }
            }
            if (lista.Items.Count > 0 && id == -1) lista.SelectedIndex = 0;
        }

        private void CambiarEstado()
        {
            if (pestanas.SelectedIndex != 1) return;
            DataRow r = Seleccionado(); if (r == null) return;
            bool nuevoEstado = !Convert.ToBoolean(r["activo"]);
            if (MessageBox.Show("¿" + (nuevoEstado ? "Reactivar" : "Desactivar") + " a " + r["nombre_completo"] + "?\nEl historial se conservará.",
                "Confirmar estado", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            try
            {
                using (NpgsqlConnection c = Conectar())
                using (NpgsqlCommand cmd = new NpgsqlCommand("UPDATE public.personal_externo SET activo=@activo WHERE id_personal=@id AND activo=@anterior", c))
                {
                    cmd.Parameters.AddWithValue("@activo", nuevoEstado);
                    cmd.Parameters.AddWithValue("@anterior", !nuevoEstado);
                    cmd.Parameters.AddWithValue("@id", Convert.ToInt32(r["id_personal"]));
                    if (cmd.ExecuteNonQuery() != 1) throw new InvalidOperationException("El registro cambió en otra sesión. Actualiza la lista.");
                }
                ActualizarDatos();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "No se pudo cambiar el estado", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }

        private void VerFicha()
        {
            DataRow r = Seleccionado(); if (r == null || pestanas.SelectedIndex != 1) return;
            string texto = "TÉCNICO EXTERNO\n\n" + r["nombre_completo"] + "\nDNI: " + r["dni"] +
                "\nCelular: " + r["celular"] + "\nDirección: " + r["direccion"] +
                "\nEstado: " + r["estado"] + "\nRegistrado: " + Convert.ToDateTime(r["fecha_registro"]).ToString("dd/MM/yyyy HH:mm") +
                "\n\nEQUIPOS RETIRADOS Y DEVOLUCIONES\nSe consultarán desde el módulo Movimientos cuando esté integrado.";
            using (Form ventana = new Form
            {
                Text = "Ficha del técnico - Artemusa",
                StartPosition = FormStartPosition.CenterParent,
                ClientSize = new Size(620, 530),
                BackColor = Color.White,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false
            })
            {
                TextBox info = new TextBox
                {
                    Multiline = true,
                    ReadOnly = true,
                    ScrollBars = ScrollBars.Vertical,
                    Dock = DockStyle.Fill,
                    Text = texto,
                    Font = new Font("Segoe UI", 11),
                    BackColor = Color.White,
                    BorderStyle = BorderStyle.None
                };
                ventana.Padding = new Padding(22); ventana.Controls.Add(info);
                ventana.ShowDialog(FindForm());
            }
        }

        private void AbrirWhatsApp(DataRow tecnico)
        {
            string celular = Regex.Replace(Convert.ToString(tecnico["celular"]) ?? "", @"\D", "");
            // Perú: 9 dígitos nacionales; wa.me requiere prefijo internacional 51.
            if (Regex.IsMatch(celular, @"^9\d{8}$")) celular = "51" + celular;
            if (!Regex.IsMatch(celular, @"^519[0-9]{8}$"))
            {
                MessageBox.Show("El técnico no tiene un celular peruano válido de 9 dígitos.",
                    "WhatsApp", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string nombre = Convert.ToString(tecnico["nombre_completo"]);
            string mensaje = "Hola " + nombre + ", le escribimos de Artemusa TV para coordinar sobre los equipos audiovisuales. Saludos.";
            string url = "https://wa.me/" + celular + "?text=" + Uri.EscapeDataString(mensaje);
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo abrir WhatsApp: " + ex.Message,
                    "Artemusa", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ExportarCsv()
        {
            DataTable datos = pestanas.SelectedIndex == 1 ? externos : internos;
            if (datos == null || datos.DefaultView.Count == 0) { MessageBox.Show("No hay datos visibles para exportar."); return; }
            string[] campos = pestanas.SelectedIndex == 1
                ? new[] { "id_personal", "nombre", "apellido", "dni", "celular", "direccion", "estado", "fecha_registro" }
                : new[] { "id", "nombre", "dni", "telefono", "correo", "departamento", "estado" };
            using (SaveFileDialog dlg = new SaveFileDialog
            {
                Filter = "CSV (*.csv)|*.csv",
                FileName = "artemusa_personal_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".csv"
            })
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                try
                {
                    using (StreamWriter w = new StreamWriter(dlg.FileName, false, new UTF8Encoding(true)))
                    {
                        w.WriteLine(string.Join(";", campos));
                        foreach (DataRowView r in datos.DefaultView)
                        {
                            string[] valores = new string[campos.Length];
                            for (int i = 0; i < campos.Length; i++) valores[i] = EscaparCsv(Convert.ToString(r[campos[i]], CultureInfo.CurrentCulture));
                            w.WriteLine(string.Join(";", valores));
                        }
                    }
                    MessageBox.Show("CSV exportado correctamente.", "Artemusa");
                }
                catch (Exception ex) { MessageBox.Show(ex.Message, "Error al exportar", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
        }

        private static string EscaparCsv(string valor)
        {
            valor = valor ?? "";
            string v = valor.TrimStart();
            if (v.Length > 0 && "=+-@".IndexOf(v[0]) >= 0) valor = "'" + valor;
            return "\"" + valor.Replace("\"", "\"\"") + "\"";
        }
    }
}
