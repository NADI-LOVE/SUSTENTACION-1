    // Sustituye completamente el contenido de Trabajador.cs por este archivo.
    // Usa las tablas del esquema Artemusa: responsable y area.
    // Las cuentas y contraseñas de acceso permanecen en usuario; este módulo no las modifica.
    #nullable disable
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Globalization;
    using System.IO;
    using System.Net.Mail;
    using System.Text;
    using System.Windows.Forms;
    using MySql.Data.MySqlClient;

    namespace SUSTENTACION.PanelTrabajador
    {
        public class Trabajador : UserControl
        {
            private readonly Color background = Color.FromArgb(242, 246, 249);
            private readonly Color ink = Color.FromArgb(24, 42, 55);
            private readonly Color muted = Color.FromArgb(105, 123, 137);
            private readonly Color green = Color.FromArgb(18, 124, 91);
            private readonly Dictionary<string, Font> fonts = new Dictionary<string, Font>();

            private DataTable people;
            private DataTable areaSummary;
            private TextBox search;
            private ComboBox areaFilter;
            private ComboBox statusFilter;
            private DataGridView grid;
            private DataGridView summaryGrid;
            private FlowLayoutPanel profile;
            private Label message;
            private Label count;
            private Label empty;
            private Label[] metrics = new Label[4];
            private Button add;
            private Button edit;
            private Button toggle;
            private TabControl views;
            private bool loading;
            private bool online;
            private bool busy;

            public Trabajador()
            {
                Dock = DockStyle.Fill;
                BackColor = background;
                DoubleBuffered = true;
                Font = UiFont(9);
                BuildInterface();
                Load += delegate { CargarTrabajadores(); };
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    if (people != null) people.Dispose();
                    if (areaSummary != null) areaSummary.Dispose();
                    foreach (Font font in fonts.Values) font.Dispose();
                    fonts.Clear();
                }
                base.Dispose(disposing);
            }

            private Font UiFont(float size, bool bold = false)
            {
                string key = size.ToString(CultureInfo.InvariantCulture) + (bold ? "B" : "R");
                Font font;
                if (!fonts.TryGetValue(key, out font))
                {
                    font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular);
                    fonts.Add(key, font);
                }
                return font;
            }

            private Label Caption(string text, float size, Color color, bool bold = false)
            {
                return new Label
                {
                    Text = text,
                    AutoSize = true,
                    Font = UiFont(size, bold),
                    ForeColor = color,
                    BackColor = Color.Transparent
                };
            }

            private Button ActionButton(string text, EventHandler action, bool primary = false, int width = 130)
            {
                Button button = new Button
                {
                    Text = text,
                    Width = width,
                    Height = 40,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = primary ? green : Color.White,
                    ForeColor = primary ? Color.White : ink,
                    Font = UiFont(9, true),
                    Cursor = Cursors.Hand,
                    Margin = new Padding(0, 0, 10, 0),
                    UseVisualStyleBackColor = false
                };
                button.FlatAppearance.BorderColor = primary ? green : Color.FromArgb(219, 229, 234);
                button.FlatAppearance.MouseOverBackColor = primary ? Color.FromArgb(14, 103, 77) : background;
                button.Click += action;
                return button;
            }

            private ComboBox Choice(params string[] items)
            {
                ComboBox box = new ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.White,
                    ForeColor = ink,
                    Font = UiFont(9),
                    Width = 180
                };
                box.Items.AddRange(items);
                if (items.Length > 0) box.SelectedIndex = 0;
                return box;
            }

            private void BuildInterface()
            {
                SuspendLayout();
                AutoScroll = true;

                Panel page = new Panel
                {
                    BackColor = background,
                    Padding = new Padding(24),
                    Location = Point.Empty
                };
                Controls.Add(page);

                TableLayoutPanel layout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 1,
                    RowCount = 6,
                    Margin = new Padding(0)
                };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 126));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 98));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));

                page.Controls.Add(layout);
                layout.Controls.Add(BuildHeader(), 0, 0);
                layout.Controls.Add(BuildMetrics(), 0, 1);
                layout.Controls.Add(BuildFilters(), 0, 2);
                layout.Controls.Add(BuildActions(), 0, 3);
                layout.Controls.Add(BuildViews(), 0, 4);

                message = Caption("Conectando con la base de datos…", 8.5f, muted);
                message.AutoSize = false;
                message.Dock = DockStyle.Fill;
                message.TextAlign = ContentAlignment.MiddleLeft;
                message.AutoEllipsis = true;
                layout.Controls.Add(message, 0, 5);

                Action resize = delegate
                {
                    page.Size = new Size(
                        Math.Max(1030, ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 2),
                        Math.Max(850, ClientSize.Height - 2));
                };
                Resize += delegate { resize(); };
                resize();

                UpdateActions();
                ShowProfile();
                ResumeLayout(true);
            }

            private Control BuildHeader()
            {
                Panel panel = new Card
                {
                    Dock = DockStyle.Fill,
                    BackColor = ink,
                    Margin = new Padding(0, 0, 0, 16)
                };

                Label brand = Caption("ARTEMUSA TV  /  GESTIÓN DE TRABAJADORES", 8, Color.FromArgb(157, 219, 196), true);
                brand.Location = new Point(24, 14);
                Label title = Caption("Gestión de trabajadores", 23, Color.White, true);
                title.Location = new Point(19, 37);
                Label subtitle = Caption("Registro y control del personal relacionado con los equipos audiovisuales", 9, Color.FromArgb(187, 204, 211));
                subtitle.Location = new Point(24, 80);

                Button refresh = ActionButton("↻  Actualizar", delegate { CargarTrabajadores(); }, false, 125);
                add = ActionButton("+ Nuevo trabajador", delegate { OpenEditor(false); }, true, 172);
                panel.Controls.AddRange(new Control[] { brand, title, subtitle, refresh, add });
                panel.Resize += delegate
                {
                    add.Location = new Point(panel.Width - add.Width - 22, 35);
                    refresh.Location = new Point(add.Left - refresh.Width - 10, 35);
                };
                return panel;
            }

            private Control BuildMetrics()
            {
                TableLayoutPanel row = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 4,
                    RowCount = 1,
                    Margin = new Padding(0)
                };

                string[] titles = { "TRABAJADORES REGISTRADOS", "TRABAJADORES ACTIVOS", "TRABAJADORES INACTIVOS", "ÁREAS REGISTRADAS" };
                string[] notes = { "Personal registrado", "Estado activo", "Historial conservado", "Áreas de Artemusa TV" };
                Color[] fills =
                {
                    Color.FromArgb(231, 242, 251),
                    Color.FromArgb(226, 243, 232),
                    Color.FromArgb(255, 243, 222),
                    Color.FromArgb(240, 234, 251)
                };
                Color[] accents =
                {
                    Color.FromArgb(43, 103, 163),
                    green,
                    Color.FromArgb(165, 99, 27),
                    Color.FromArgb(119, 79, 178)
                };

                for (int i = 0; i < 4; i++)
                {
                    row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
                    Panel card = new Card
                    {
                        Dock = DockStyle.Fill,
                        BackColor = fills[i],
                        Margin = new Padding(0, 0, i == 3 ? 0 : 12, 16)
                    };
                    Label title = Caption(titles[i], 7.5f, accents[i], true);
                    title.Location = new Point(16, 13);
                    metrics[i] = Caption("—", 26, ink, true);
                    metrics[i].Location = new Point(12, 34);
                    Label note = Caption(notes[i], 8, muted);
                    note.Location = new Point(17, 86);
                    card.Controls.AddRange(new Control[] { title, metrics[i], note });
                    row.Controls.Add(card, i, 0);
                }
                return row;
            }

            private Control BuildFilters()
            {
                Panel panel = new Card
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.White,
                    Padding = new Padding(16, 10, 16, 10),
                    Margin = new Padding(0, 0, 0, 14)
                };
                TableLayoutPanel row = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 4,
                    RowCount = 2
                };
                row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
                row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
                row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
                row.RowStyles.Add(new RowStyle(SizeType.Absolute, 23));
                row.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

                row.Controls.Add(Caption("BUSCAR POR NOMBRE, DNI O CONTACTO", 7.5f, muted, true), 0, 0);
                row.Controls.Add(Caption("ÁREA", 7.5f, muted, true), 1, 0);
                row.Controls.Add(Caption("ESTADO", 7.5f, muted, true), 2, 0);

                search = new TextBox
                {
                    Dock = DockStyle.Top,
                    Margin = new Padding(0, 0, 14, 0),
                    Font = UiFont(10),
                    ForeColor = ink
                };
                areaFilter = Choice("Todas las áreas");
                areaFilter.Width = 205;
                statusFilter = Choice("Todos los estados", "Activo", "Inactivo");
                statusFilter.Width = 166;
                Button clear = ActionButton("Limpiar", delegate
                {
                    search.Clear();
                    areaFilter.SelectedIndex = 0;
                    statusFilter.SelectedIndex = 0;
                }, false, 84);
                clear.Height = 29;

                row.Controls.Add(search, 0, 1);
                row.Controls.Add(areaFilter, 1, 1);
                row.Controls.Add(statusFilter, 2, 1);
                row.Controls.Add(clear, 3, 1);
                panel.Controls.Add(row);

                search.TextChanged += delegate { ApplyFilters(); };
                areaFilter.SelectedIndexChanged += delegate { ApplyFilters(); };
                statusFilter.SelectedIndexChanged += delegate { ApplyFilters(); };
                return panel;
            }

            private Control BuildActions()
            {
                FlowLayoutPanel panel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    WrapContents = false,
                    Margin = new Padding(0)
                };
                edit = ActionButton("Editar trabajador", delegate { OpenEditor(true); });
                toggle = ActionButton("Desactivar", delegate { ChangeStatus(); }, false, 136);
                Button export = ActionButton("Exportar CSV", delegate { ExportCsv(); });
                count = Caption("Sin registros", 9, muted);
                count.Margin = new Padding(14, 12, 0, 0);
                panel.Controls.AddRange(new Control[] { edit, toggle, export, count });
                return panel;
            }

            private Control BuildViews()
            {
                views = new TabControl
                {
                    Dock = DockStyle.Fill,
                    Font = UiFont(10, true),
                    Padding = new Point(20, 10),
                    Margin = new Padding(0)
                };
                TabPage directory = new TabPage("Lista de trabajadores") { BackColor = Color.White, Padding = new Padding(10) };
                TabPage summary = new TabPage("Resumen por área") { BackColor = Color.White, Padding = new Padding(14) };

                TableLayoutPanel split = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 2,
                    RowCount = 1,
                    Margin = new Padding(0)
                };
                split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                split.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 276));
                split.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

                Panel list = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 14, 0) };
                grid = BuildGrid();
                grid.SelectionChanged += delegate
                {
                    if (!loading)
                    {
                        ShowProfile();
                        UpdateActions();
                    }
                };
                grid.CellDoubleClick += delegate (object sender, DataGridViewCellEventArgs e)
                {
                    if (e.RowIndex >= 0) OpenEditor(true);
                };
                grid.CellFormatting += FormatState;
                empty = Caption("", 9, muted);
                empty.AutoSize = false;
                empty.Height = 42;
                empty.Dock = DockStyle.Bottom;
                empty.TextAlign = ContentAlignment.MiddleLeft;
                list.Controls.Add(grid);
                list.Controls.Add(empty);

                profile = new FlowLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    FlowDirection = FlowDirection.TopDown,
                    WrapContents = false,
                    AutoScroll = true,
                    BackColor = Color.FromArgb(246, 249, 251),
                    Padding = new Padding(16)
                };
                split.Controls.Add(list, 0, 0);
                split.Controls.Add(profile, 1, 0);
                directory.Controls.Add(split);

                summaryGrid = BuildGrid();
                summary.Controls.Add(summaryGrid);
                Label info = Caption("La distribución se calcula con los responsables que coinciden con los filtros.", 9, muted);
                info.Dock = DockStyle.Top;
                info.AutoSize = false;
                info.Height = 36;
                summary.Controls.Add(info);

                views.TabPages.Add(directory);
                views.TabPages.Add(summary);
                views.SelectedIndexChanged += delegate { UpdateActions(); };
                return views;
            }

            private DataGridView BuildGrid()
            {
                DataGridView result = new BufferedGrid
                {
                    Dock = DockStyle.Fill,
                    BackgroundColor = Color.White,
                    BorderStyle = BorderStyle.None,
                    AllowUserToAddRows = false,
                    AllowUserToDeleteRows = false,
                    AllowUserToResizeRows = false,
                    ReadOnly = true,
                    RowHeadersVisible = false,
                    MultiSelect = false,
                    SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    EnableHeadersVisualStyles = false,
                    ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                    CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                    GridColor = Color.FromArgb(230, 237, 242),
                    ColumnHeadersHeight = 42,
                    ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
                };
                result.RowTemplate.Height = 48;
                result.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = background,
                    ForeColor = muted,
                    Font = UiFont(8.5f, true),
                    Padding = new Padding(10, 0, 0, 0),
                    SelectionBackColor = background,
                    SelectionForeColor = muted
                };
                result.DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.White,
                    ForeColor = ink,
                    Font = UiFont(9),
                    Padding = new Padding(10, 0, 4, 0),
                    SelectionBackColor = Color.FromArgb(222, 242, 232),
                    SelectionForeColor = ink
                };
                result.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 252, 253);
                return result;
            }

            private void FormatState(object sender, DataGridViewCellFormattingEventArgs e)
            {
                DataGridView source = sender as DataGridView;
                if (source == null || e.RowIndex < 0 || e.ColumnIndex < 0) return;
                if (source.Columns[e.ColumnIndex].Name == "estado")
                {
                    bool active = string.Equals(Convert.ToString(e.Value), "Activo", StringComparison.OrdinalIgnoreCase);
                    e.CellStyle.ForeColor = active ? green : Color.FromArgb(160, 89, 32);
                    e.CellStyle.Font = UiFont(9, true);
                }
            }

            private MySqlConnection OpenConnection()
            {
                MySqlConnection connection = new global::SUSTENTACION.Conexion().ObtenerConexion();
                if (connection == null) throw new InvalidOperationException("Conexion.ObtenerConexion() devolvió una conexión vacía.");
                try
                {
                    if (connection.State != ConnectionState.Open) connection.Open();
                    return connection;
                }
                catch
                {
                    connection.Dispose();
                    throw;
                }
            }

            public void CargarTrabajadores()
            {
                if (loading || busy) return;
                loading = true;

                DataRow selected = SelectedRow();
                object selectedId = selected == null ? null : selected["id"];
                try
                {
                    DataTable fresh = new DataTable();
                    int totalAreas;

                    using (MySqlConnection connection = OpenConnection())
                    {
                        const string sql = @"
    SELECT r.id_responsable AS id,
           r.nombre AS nombres,
           r.apellido AS apellidos,
           r.dni AS documento,
           COALESCE(r.telefono, '') AS telefono,
           COALESCE(r.correo, '') AS correo,
           r.estado,
           r.id_area,
           a.nombre_area AS area,
           a.estado AS estado_area
    FROM responsable r
    INNER JOIN area a ON a.id_area = r.id_area
    ORDER BY r.apellido, r.nombre";
                        using (MySqlDataAdapter adapter = new MySqlDataAdapter(sql, connection))
                            adapter.Fill(fresh);

                        using (MySqlCommand command = new MySqlCommand("SELECT COUNT(*) FROM area", connection))
                            totalAreas = Convert.ToInt32(command.ExecuteScalar());
                    }

                    fresh.Columns.Add("nombre_completo", typeof(string), "nombres + ' ' + apellidos");
                    DataTable old = people;
                    people = fresh;
                    grid.DataSource = fresh;
                    if (old != null) old.Dispose();
                    ConfigureColumns();
                    PopulateAreaFilter(fresh);

                    int active = 0;
                    int inactive = 0;
                    foreach (DataRow row in fresh.Rows)
                    {
                        if (string.Equals(Convert.ToString(row["estado"]), "Activo", StringComparison.OrdinalIgnoreCase)) active++;
                        if (string.Equals(Convert.ToString(row["estado"]), "Inactivo", StringComparison.OrdinalIgnoreCase)) inactive++;
                    }

                    metrics[0].Text = fresh.Rows.Count.ToString("N0");
                    metrics[1].Text = active.ToString("N0");
                    metrics[2].Text = inactive.ToString("N0");
                    metrics[3].Text = totalAreas.ToString("N0");

                    online = true;
                    message.Text = "●  Conectado a responsable y area · Actualizado " + DateTime.Now.ToString("HH:mm:ss")
                        + " · Las cuentas de acceso se administran por separado.";
                    message.ForeColor = green;
                }
                catch (Exception ex)
                {
                    online = false;
                    grid.DataSource = null;
                    if (people != null)
                    {
                        people.Dispose();
                        people = null;
                    }
                    if (areaSummary != null)
                    {
                        areaSummary.Dispose();
                        areaSummary = null;
                    }
                    summaryGrid.DataSource = null;
                    foreach (Label metric in metrics) metric.Text = "—";
                    message.Text = "No se pudo cargar el directorio. Revisa la conexión y las tablas responsable y area.";
                    message.ForeColor = Color.Firebrick;
                    Error("Directorio no disponible", ex);
                }
                finally
                {
                    loading = false;
                    ApplyFilters();
                    RestoreSelection(selectedId);
                    ShowProfile();
                    UpdateActions();
                }
            }

            private void PopulateAreaFilter(DataTable source)
            {
                string current = Convert.ToString(areaFilter.SelectedItem);
                SortedSet<string> names = new SortedSet<string>(StringComparer.CurrentCultureIgnoreCase);
                foreach (DataRow row in source.Rows)
                {
                    if (!row.IsNull("area")) names.Add(Convert.ToString(row["area"]));
                }

                areaFilter.Items.Clear();
                areaFilter.Items.Add("Todas las áreas");
                foreach (string name in names) areaFilter.Items.Add(name);
                int index = areaFilter.Items.IndexOf(current);
                areaFilter.SelectedIndex = index >= 0 ? index : 0;
            }

            private void ConfigureColumns()
            {
                if (grid.Columns.Count == 0) return;
                foreach (DataGridViewColumn column in grid.Columns) column.Visible = false;

                string[] names = { "nombre_completo", "documento", "area", "telefono", "correo", "estado" };
                string[] labels = { "Responsable", "Documento", "Área", "Teléfono", "Correo", "Estado" };
                for (int i = 0; i < names.Length; i++)
                {
                    if (!grid.Columns.Contains(names[i])) continue;
                    DataGridViewColumn column = grid.Columns[names[i]];
                    column.Visible = true;
                    column.HeaderText = labels[i];
                    column.DisplayIndex = i;
                    column.MinimumWidth = i == 0 ? 180 : 100;
                    column.FillWeight = i == 0 ? 165 : 100;
                }
                if (grid.Columns.Contains("id")) grid.Columns["id"].Visible = false;
            }

            private DataRow SelectedRow()
            {
                if (grid == null || grid.CurrentRow == null) return null;
                DataRowView row = grid.CurrentRow.DataBoundItem as DataRowView;
                return row == null ? null : row.Row;
            }

            private void RestoreSelection(object id)
            {
                if (id == null || grid.DataSource == null) return;
                foreach (DataGridViewRow row in grid.Rows)
                {
                    DataRowView item = row.DataBoundItem as DataRowView;
                    if (item != null && object.Equals(item["id"], id))
                    {
                        if (grid.Columns.Contains("nombre_completo"))
                        {
                            int indiceColumna = grid.Columns["nombre_completo"].Index;
                            if (indiceColumna >= 0 && indiceColumna < row.Cells.Count)
                                grid.CurrentCell = row.Cells[indiceColumna];
                        }
                        return;
                    }
                }
            }

            private void UpdateActions()
            {
                if (add == null || edit == null || toggle == null) return;
                add.Enabled = online && !busy;
                DataRow row = SelectedRow();
                bool selected = row != null && online && !busy && views != null && views.SelectedIndex == 0;
                edit.Enabled = toggle.Enabled = selected;
                toggle.Text = row != null && string.Equals(Convert.ToString(row["estado"]), "Inactivo", StringComparison.OrdinalIgnoreCase)
                    ? "Reactivar"
                    : "Desactivar";
            }

            private static string EscapeLike(string value)
            {
                StringBuilder text = new StringBuilder();
                foreach (char c in value ?? "")
                {
                    if (c == '\'') text.Append("''");
                    else if (c == '[') text.Append("[[]");
                    else if (c == ']') text.Append("[]]");
                    else if (c == '*') text.Append("[*]");
                    else if (c == '%') text.Append("[%]");
                    else text.Append(c);
                }
                return text.ToString();
            }

            private void ApplyFilters()
            {
                if (loading || grid == null || count == null) return;
                if (people == null)
                {
                    count.Text = "Sin registros";
                    if (empty != null) empty.Text = "No hay datos disponibles. Pulsa Actualizar para reintentar.";
                    summaryGrid.DataSource = null;
                    if (areaSummary != null)
                    {
                        areaSummary.Dispose();
                        areaSummary = null;
                    }
                    ShowProfile();
                    UpdateActions();
                    return;
                }

                List<string> conditions = new List<string>();
                string query = EscapeLike(search.Text.Trim());
                if (query.Length > 0)
                {
                    List<string> terms = new List<string>();
                    foreach (string column in new[] { "nombre_completo", "documento", "telefono", "correo", "area" })
                        terms.Add("[" + column + "] LIKE '%" + query + "%'");
                    conditions.Add("(" + string.Join(" OR ", terms) + ")");
                }

                if (areaFilter.SelectedIndex > 0)
                {
                    conditions.Add("[area] = '" + Convert.ToString(areaFilter.SelectedItem).Replace("'", "''") + "'");
                }
                if (statusFilter.SelectedIndex > 0)
                {
                    conditions.Add("[estado] = '" + Convert.ToString(statusFilter.SelectedItem).Replace("'", "''") + "'");
                }

                try
                {
                    people.DefaultView.RowFilter = string.Join(" AND ", conditions);
                }
                catch (EvaluateException ex)
                {
                    people.DefaultView.RowFilter = "";
                    message.Text = "No se pudo aplicar el filtro: " + ex.Message;
                }

                count.Text = people.DefaultView.Count.ToString("N0") + " responsables visibles";
                if (people.Rows.Count == 0)
                    empty.Text = "El directorio está vacío. Empieza con + Nuevo responsable.";
                else if (people.DefaultView.Count == 0)
                    empty.Text = "No hay coincidencias. Cambia la búsqueda o limpia los filtros.";
                else
                    empty.Text = "Selecciona una fila para ver la ficha. Doble clic para editar.";

                BuildSummary();
                ShowProfile();
                UpdateActions();
            }

            private void BuildSummary()
            {
                DataTable summary = new DataTable();
                summary.Columns.Add("Área", typeof(string));
                summary.Columns.Add("Total", typeof(int));
                summary.Columns.Add("Activos", typeof(int));
                summary.Columns.Add("Inactivos", typeof(int));

                SortedDictionary<string, int[]> areas = new SortedDictionary<string, int[]>(StringComparer.CurrentCultureIgnoreCase);
                foreach (DataRowView row in people.DefaultView)
                {
                    string name = Convert.ToString(row["area"]);
                    if (!areas.ContainsKey(name)) areas.Add(name, new int[3]);
                    areas[name][0]++;
                    if (string.Equals(Convert.ToString(row["estado"]), "Activo", StringComparison.OrdinalIgnoreCase)) areas[name][1]++;
                    if (string.Equals(Convert.ToString(row["estado"]), "Inactivo", StringComparison.OrdinalIgnoreCase)) areas[name][2]++;
                }

                foreach (KeyValuePair<string, int[]> pair in areas)
                    summary.Rows.Add(pair.Key, pair.Value[0], pair.Value[1], pair.Value[2]);

                DataTable old = areaSummary;
                areaSummary = summary;
                summaryGrid.DataSource = summary;
                if (old != null) old.Dispose();
                if (summaryGrid.Columns.Count > 0) summaryGrid.Columns[0].FillWeight = 200;
            }

            private void ProfileLine(string title, string value)
            {
                Label label = Caption(title.ToUpperInvariant(), 7.5f, muted, true);
                label.Margin = new Padding(0, 10, 0, 4);
                Label text = Caption(string.IsNullOrWhiteSpace(value) ? "No registrado" : value, 9, ink);
                text.MaximumSize = new Size(222, 0);
                text.Margin = new Padding(0, 0, 0, 4);
                profile.Controls.Add(label);
                profile.Controls.Add(text);
            }

            private void ShowProfile()
            {
                if (profile == null) return;
                profile.SuspendLayout();
                while (profile.Controls.Count > 0)
                {
                    Control control = profile.Controls[0];
                    profile.Controls.RemoveAt(0);
                    control.Dispose();
                }

                Label heading = Caption("FICHA DEL TRABAJADOR", 8, muted, true);
                heading.Margin = new Padding(0, 0, 0, 18);
                profile.Controls.Add(heading);

                DataRow row = SelectedRow();
                if (row == null)
                {
                    Label hint = Caption("Selecciona un trabajador para consultar sus datos y área asignada..", 11, ink);
                    hint.MaximumSize = new Size(222, 0);
                    profile.Controls.Add(hint);
                    profile.ResumeLayout(true);
                    return;
                }

                string names = Convert.ToString(row["nombres"]);
                string surnames = Convert.ToString(row["apellidos"]);
                string initials = (names.Length > 0 ? names.Substring(0, 1) : "")
                    + (surnames.Length > 0 ? surnames.Substring(0, 1) : "");

                Label avatar = Caption(initials.ToUpperInvariant(), 22, green, true);
                avatar.AutoSize = false;
                avatar.Size = new Size(68, 58);
                avatar.TextAlign = ContentAlignment.MiddleCenter;
                avatar.BackColor = Color.FromArgb(223, 239, 231);
                profile.Controls.Add(avatar);

                Label name = Caption(Convert.ToString(row["nombre_completo"]), 15, ink, true);
                name.MaximumSize = new Size(222, 0);
                name.Margin = new Padding(0, 12, 0, 6);
                profile.Controls.Add(name);

                bool active = string.Equals(Convert.ToString(row["estado"]), "Activo", StringComparison.OrdinalIgnoreCase);
                Label badge = Caption(Convert.ToString(row["estado"]), 9, active ? green : Color.Sienna, true);
                profile.Controls.Add(badge);

                ProfileLine("Código interno", "RP-" + Convert.ToInt32(row["id"]).ToString("D5"));
                ProfileLine("DNI", Convert.ToString(row["documento"]));
                ProfileLine("Área de custodia", Convert.ToString(row["area"]));
                ProfileLine("Teléfono", Convert.ToString(row["telefono"]));
                ProfileLine("Correo", Convert.ToString(row["correo"]));
                if (!string.Equals(Convert.ToString(row["estado_area"]), "Activo", StringComparison.OrdinalIgnoreCase))
                    ProfileLine("Estado del área", Convert.ToString(row["estado_area"]));
                profile.ResumeLayout(true);
            }

            private TextBox Field(Control parent, string label, int x, int y, int width, string value, int maxLength)
            {
                Label title = Caption(label, 9, muted, true);
                title.Location = new Point(x, y);
                parent.Controls.Add(title);
                TextBox field = new TextBox
                {
                    Text = value ?? "",
                    MaxLength = maxLength,
                    Font = UiFont(10),
                    Location = new Point(x, y + 25),
                    Size = new Size(width, 28)
                };
                parent.Controls.Add(field);
                return field;
            }

            private List<AreaOption> LoadAreaOptions(int? currentAreaId)
            {
                List<AreaOption> result = new List<AreaOption>();
                const string activeSql = @"
    SELECT id_area, nombre_area, estado
    FROM area
    WHERE estado='Activo'
    ORDER BY nombre_area";
                const string editingSql = @"
    SELECT id_area, nombre_area, estado
    FROM area
    WHERE estado='Activo' OR id_area=@idArea
    ORDER BY CASE WHEN estado='Activo' THEN 0 ELSE 1 END, nombre_area";

                using (MySqlConnection connection = OpenConnection())
                using (MySqlCommand command = new MySqlCommand(currentAreaId.HasValue ? editingSql : activeSql, connection))
                {
                    if (currentAreaId.HasValue) command.Parameters.AddWithValue("@idArea", currentAreaId.Value);
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Add(new AreaOption(
                                Convert.ToInt32(reader["id_area"]),
                                Convert.ToString(reader["nombre_area"]),
                                Convert.ToString(reader["estado"])));
                        }
                    }
                }
                return result;
            }

            private void OpenEditor(bool editing)
            {
                if (!online || busy) return;
                DataRow selected = editing ? SelectedRow() : null;
                if (editing && selected == null) return;

                List<AreaOption> areaOptions;
                try
                {
                    int? currentAreaId = editing ? (int?)Convert.ToInt32(selected["id_area"]) : null;
                    areaOptions = LoadAreaOptions(currentAreaId);
                }
                catch (Exception ex)
                {
                    Error("No se pudieron consultar las áreas", ex);
                    return;
                }
                if (areaOptions.Count == 0)
                {
                    Inform("No hay áreas activas. Registra o activa un área antes de crear responsables.");
                    return;
                }

                using (Form dialog = new Form
                {
                    Text = editing ? "Editar responsable" : "Nuevo responsable",
                    ClientSize = new Size(626, 500),
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    StartPosition = FormStartPosition.CenterParent,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    ShowInTaskbar = false,
                    BackColor = Color.White,
                    Font = UiFont(9),
                    AutoScaleMode = AutoScaleMode.Font
                })
                {
                    Label title = Caption(editing ? "Actualizar responsable" : "Registrar responsable", 21, ink, true);
                    title.Location = new Point(20, 16);
                    dialog.Controls.Add(title);
                    Label subtitle = Caption("La ficha se vincula con un área de Artemusa TV. No crea ni cambia contraseñas de acceso.", 8.5f, muted);
                    subtitle.Location = new Point(24, 59);
                    dialog.Controls.Add(subtitle);

                    TabControl formTabs = new TabControl
                    {
                        Location = new Point(20, 91),
                        Size = new Size(586, 315),
                        Font = UiFont(9, true)
                    };
                    TabPage work = new TabPage("Datos personales y área") { BackColor = Color.White };
                    TabPage contact = new TabPage("Contacto") { BackColor = Color.White };
                    formTabs.TabPages.Add(work);
                    formTabs.TabPages.Add(contact);
                    dialog.Controls.Add(formTabs);

                    TextBox document = Field(work, "DNI *", 16, 15, 258,
                        editing ? Convert.ToString(selected["documento"]) : "", 20);
                    TextBox names = Field(work, "Nombres *", 296, 15, 258,
                        editing ? Convert.ToString(selected["nombres"]) : "", 100);
                    TextBox surnames = Field(work, "Apellidos *", 16, 87, 258,
                        editing ? Convert.ToString(selected["apellidos"]) : "", 100);

                    Label areaTitle = Caption("Área *", 9, muted, true);
                    areaTitle.Location = new Point(296, 87);
                    work.Controls.Add(areaTitle);
                    ComboBox area = new ComboBox
                    {
                        DropDownStyle = ComboBoxStyle.DropDownList,
                        FlatStyle = FlatStyle.Flat,
                        BackColor = Color.White,
                        ForeColor = ink,
                        Font = UiFont(9),
                        Location = new Point(296, 112),
                        Size = new Size(258, 28),
                        DisplayMember = "VisibleName",
                        ValueMember = "Id"
                    };
                    area.DataSource = areaOptions;
                    work.Controls.Add(area);

                    Label stateTitle = Caption("Estado del responsable *", 9, muted, true);
                    stateTitle.Location = new Point(16, 159);
                    work.Controls.Add(stateTitle);
                    ComboBox state = Choice("Activo", "Inactivo");
                    state.Location = new Point(16, 184);
                    state.Width = 258;
                    if (editing) state.SelectedItem = Convert.ToString(selected["estado"]);
                    work.Controls.Add(state);

                    TextBox phone = Field(contact, "Teléfono (opcional)", 16, 15, 538,
                        editing ? Convert.ToString(selected["telefono"]) : "", 30);
                    TextBox email = Field(contact, "Correo (opcional)", 16, 87, 538,
                        editing ? Convert.ToString(selected["correo"]) : "", 100);

                    Label contactInfo = Caption("El responsable es un contacto de custodia; la cuenta de inicio de sesión se conserva en la tabla usuario.", 9, muted);
                    contactInfo.Location = new Point(18, 151);
                    contactInfo.MaximumSize = new Size(520, 0);
                    contact.Controls.Add(contactInfo);

                    if (editing) area.SelectedValue = Convert.ToInt32(selected["id_area"]);
                    else if (area.Items.Count > 0) area.SelectedIndex = 0;

                    Button cancel = ActionButton("Cancelar", delegate { dialog.Close(); }, false, 112);
                    cancel.Location = new Point(334, 432);
                    Button save = ActionButton("Guardar ficha", delegate { }, true, 150);
                    save.Location = new Point(456, 432);
                    dialog.Controls.Add(cancel);
                    dialog.Controls.Add(save);
                    dialog.CancelButton = cancel;

                    save.Click += delegate
                    {
                        if (document.Text.Trim().Length == 0 || names.Text.Trim().Length == 0
                            || surnames.Text.Trim().Length == 0 || area.SelectedValue == null || state.SelectedIndex < 0)
                        {
                            formTabs.SelectedTab = work;
                            Inform("Completa DNI, nombres, apellidos, área y estado.");
                            return;
                        }

                        if (!string.IsNullOrWhiteSpace(email.Text))
                        {
                            try
                            {
                                MailAddress address = new MailAddress(email.Text.Trim());
                                if (address.Address != email.Text.Trim()) throw new FormatException();
                            }
                            catch (FormatException)
                            {
                                formTabs.SelectedTab = contact;
                                email.Focus();
                                Inform("Escribe un correo válido o deja el campo vacío.");
                                return;
                            }
                        }

                        Dictionary<string, object> values = new Dictionary<string, object>
                        {
                            { "documento", document.Text.Trim() },
                            { "nombres", names.Text.Trim() },
                            { "apellidos", surnames.Text.Trim() },
                            { "id_area", Convert.ToInt32(area.SelectedValue) },
                            { "telefono", phone.Text.Trim() },
                            { "correo", email.Text.Trim() },
                            { "estado", Convert.ToString(state.SelectedItem) }
                        };

                        busy = true;
                        save.Enabled = cancel.Enabled = false;
                        UpdateActions();
                        bool success = false;
                        try
                        {
                            SaveResponsible(values, selected, editing);
                            success = true;
                        }
                        catch (MySqlException ex)
                        {
                            if (ex.Number == 1062)
                                Inform("Ya existe un responsable con ese DNI. Verifica el documento e inténtalo de nuevo.");
                            else
                                Error("No se pudo guardar la ficha", ex);
                        }
                        catch (Exception ex)
                        {
                            Error("No se pudo guardar la ficha", ex);
                        }
                        finally
                        {
                            busy = false;
                            save.Enabled = cancel.Enabled = true;
                            UpdateActions();
                        }

                        if (success)
                        {
                            dialog.DialogResult = DialogResult.OK;
                            dialog.Close();
                        }
                    };

                    if (dialog.ShowDialog(FindForm()) == DialogResult.OK) CargarTrabajadores();
                }
            }

            private void SaveResponsible(Dictionary<string, object> values, DataRow original, bool editing)
            {
                using (MySqlConnection connection = OpenConnection())
                using (MySqlTransaction transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    try
                    {
                        if (editing)
                        {
                            if (original == null) throw new InvalidOperationException("Selecciona un responsable antes de editar.");
                            const string lockSql = @"
    SELECT nombre, apellido, dni, telefono, correo, estado, id_area
    FROM responsable
    WHERE id_responsable=@id
    FOR UPDATE";
                            using (MySqlCommand command = new MySqlCommand(lockSql, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@id", original["id"]);
                                using (MySqlDataReader reader = command.ExecuteReader())
                                {
                                    if (!reader.Read()) throw new InvalidOperationException("El responsable ya no existe. Actualiza el directorio.");
                                    if (!SamePerson(reader, original))
                                        throw new InvalidOperationException("La ficha cambió en otra sesión. Cierra el formulario, actualiza el directorio y vuelve a editar.");
                                }
                            }

                            const string updateSql = @"
    UPDATE responsable
    SET nombre=@nombres,
        apellido=@apellidos,
        dni=@documento,
        telefono=@telefono,
        correo=@correo,
        estado=@estado,
        id_area=@idArea
    WHERE id_responsable=@id";
                            using (MySqlCommand command = new MySqlCommand(updateSql, connection, transaction))
                            {
                                AddResponsibleParameters(command, values);
                                command.Parameters.AddWithValue("@id", original["id"]);
                                // MySQL puede devolver 0 si los valores no cambiaron; verifica el registro en vez de tratarlo como error.
                                command.ExecuteNonQuery();
                            }
                            using (MySqlCommand verify = new MySqlCommand(@"
    SELECT nombre, apellido, dni, telefono, correo, estado, id_area
    FROM responsable
    WHERE id_responsable=@id", connection, transaction))
                            {
                                verify.Parameters.AddWithValue("@id", original["id"]);
                                using (MySqlDataReader reader = verify.ExecuteReader())
                                {
                                    if (!reader.Read() || !ResponsibleMatches(reader, values))
                                        throw new InvalidOperationException("Los datos actualizados no coinciden con el formulario.");
                                }
                            }
                        }
                        else
                        {
                            const string insertSql = @"
    INSERT INTO responsable
    (nombre, apellido, dni, telefono, correo, estado, id_area)
    VALUES
    (@nombres, @apellidos, @documento, @telefono, @correo, @estado, @idArea)";
                            using (MySqlCommand command = new MySqlCommand(insertSql, connection, transaction))
                            {
                                AddResponsibleParameters(command, values);
                                if (command.ExecuteNonQuery() != 1)
                                    throw new InvalidOperationException("No se pudo registrar exactamente una ficha.");
                            }
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        try { transaction.Rollback(); } catch { }
                        throw;
                    }
                }
            }

            private static void AddResponsibleParameters(MySqlCommand command, Dictionary<string, object> values)
            {
                command.Parameters.AddWithValue("@nombres", values["nombres"]);
                command.Parameters.AddWithValue("@apellidos", values["apellidos"]);
                command.Parameters.AddWithValue("@documento", DbNullIfEmpty(Convert.ToString(values["documento"])));
                command.Parameters.AddWithValue("@telefono", DbNullIfEmpty(Convert.ToString(values["telefono"])));
                command.Parameters.AddWithValue("@correo", DbNullIfEmpty(Convert.ToString(values["correo"])));
                command.Parameters.AddWithValue("@estado", values["estado"]);
                command.Parameters.AddWithValue("@idArea", values["id_area"]);
            }

            private static bool SamePerson(MySqlDataReader reader, DataRow original)
            {
                return string.Equals(DbText(reader["nombre"]), DbText(original["nombres"]), StringComparison.Ordinal)
                    && string.Equals(DbText(reader["apellido"]), DbText(original["apellidos"]), StringComparison.Ordinal)
                    && string.Equals(DbText(reader["dni"]), DbText(original["documento"]), StringComparison.Ordinal)
                    && string.Equals(DbText(reader["telefono"]), DbText(original["telefono"]), StringComparison.Ordinal)
                    && string.Equals(DbText(reader["correo"]), DbText(original["correo"]), StringComparison.Ordinal)
                    && string.Equals(DbText(reader["estado"]), DbText(original["estado"]), StringComparison.Ordinal)
                    && Convert.ToInt32(reader["id_area"]) == Convert.ToInt32(original["id_area"]);
            }

            private static bool ResponsibleMatches(MySqlDataReader reader, Dictionary<string, object> values)
            {
                return string.Equals(DbText(reader["nombre"]), DbText(values["nombres"]), StringComparison.Ordinal)
                    && string.Equals(DbText(reader["apellido"]), DbText(values["apellidos"]), StringComparison.Ordinal)
                    && string.Equals(DbText(reader["dni"]), DbText(values["documento"]), StringComparison.Ordinal)
                    && string.Equals(DbText(reader["telefono"]), DbText(values["telefono"]), StringComparison.Ordinal)
                    && string.Equals(DbText(reader["correo"]), DbText(values["correo"]), StringComparison.Ordinal)
                    && string.Equals(DbText(reader["estado"]), DbText(values["estado"]), StringComparison.Ordinal)
                    && Convert.ToInt32(reader["id_area"]) == Convert.ToInt32(values["id_area"]);
            }

            private static string DbText(object value)
            {
                return value == null || value == DBNull.Value ? "" : Convert.ToString(value);
            }

            private static object DbNullIfEmpty(string value)
            {
                return string.IsNullOrWhiteSpace(value) ? (object)DBNull.Value : value.Trim();
            }

            private void ChangeStatus()
            {
                DataRow row = SelectedRow();
                if (!online || busy || row == null) return;

                string current = Convert.ToString(row["estado"]);
                string next = string.Equals(current, "Activo", StringComparison.OrdinalIgnoreCase) ? "Inactivo" : "Activo";
                if (MessageBox.Show(FindForm(),
                    "¿Cambiar el estado de " + row["nombre_completo"] + " a " + next + "?\nLa ficha se conservará y el historial de asignaciones no se borrará.",
                    "Estado del responsable", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;

                busy = true;
                UpdateActions();
                try
                {
                    using (MySqlConnection connection = OpenConnection())
                    using (MySqlTransaction transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted))
                    {
                        try
                        {
                            string currentDatabaseState;
                            using (MySqlCommand find = new MySqlCommand(
                                "SELECT estado FROM responsable WHERE id_responsable=@id FOR UPDATE", connection, transaction))
                            {
                                find.Parameters.AddWithValue("@id", row["id"]);
                                object result = find.ExecuteScalar();
                                if (result == null || result == DBNull.Value)
                                    throw new InvalidOperationException("El responsable ya no existe.");
                                currentDatabaseState = Convert.ToString(result);
                            }
                            if (!string.Equals(currentDatabaseState, current, StringComparison.Ordinal))
                                throw new InvalidOperationException("La ficha cambió en otra sesión. Actualiza el directorio antes de reintentar.");

                            using (MySqlCommand update = new MySqlCommand(
                                "UPDATE responsable SET estado=@estado WHERE id_responsable=@id", connection, transaction))
                            {
                                update.Parameters.AddWithValue("@estado", next);
                                update.Parameters.AddWithValue("@id", row["id"]);
                                if (update.ExecuteNonQuery() != 1)
                                    throw new InvalidOperationException("No se pudo actualizar el estado.");
                            }
                            transaction.Commit();
                        }
                        catch
                        {
                            try { transaction.Rollback(); } catch { }
                            throw;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Error("No se pudo cambiar el estado", ex);
                }
                finally
                {
                    busy = false;
                    CargarTrabajadores();
                }
            }

            private void ExportCsv()
            {
                DataTable table = views.SelectedIndex == 0 ? people : areaSummary;
                if (table == null || table.DefaultView.Count == 0)
                {
                    Inform("No hay registros visibles para exportar.");
                    return;
                }

                string[] columns = views.SelectedIndex == 0
                    ? new[] { "id", "documento", "nombres", "apellidos", "area", "telefono", "correo", "estado" }
                    : new[] { "Área", "Total", "Activos", "Inactivos" };

                using (SaveFileDialog dialog = new SaveFileDialog
                {
                    Filter = "Archivo CSV (*.csv)|*.csv",
                    FileName = "responsables_artemusa_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".csv"
                })
                {
                    if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;
                    try
                    {
                        using (StreamWriter writer = new StreamWriter(dialog.FileName, false, new UTF8Encoding(true)))
                        {
                            List<string> header = new List<string>();
                            foreach (string column in columns) header.Add(Csv(column));
                            writer.WriteLine(string.Join(";", header));

                            foreach (DataRowView row in table.DefaultView)
                            {
                                List<string> fields = new List<string>();
                                foreach (string column in columns)
                                    fields.Add(Csv(Convert.ToString(row[column], CultureInfo.CurrentCulture)));
                                writer.WriteLine(string.Join(";", fields));
                            }
                        }
                        Inform("Exportación completada con los registros visibles.");
                    }
                    catch (Exception ex)
                    {
                        Error("No se pudo exportar el CSV", ex);
                    }
                }
            }

            private static string Csv(string value)
            {
                value = value ?? "";
                string trimmed = value.TrimStart();
                if (trimmed.Length > 0 && "=+-@".IndexOf(trimmed[0]) >= 0)
                    value = "'" + value;
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }

            private void Inform(string text)
            {
                MessageBox.Show(FindForm(), text, "Artemusa · Responsables",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            private void Error(string title, Exception exception)
            {
                MessageBox.Show(FindForm(), title + "\n\n" + exception.Message,
                    "Artemusa · Responsables", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            private sealed class AreaOption
            {
                public int Id { get; private set; }
                public string Name { get; private set; }
                public string State { get; private set; }
                public string VisibleName
                {
                    get { return string.Equals(State, "Activo", StringComparison.OrdinalIgnoreCase) ? Name : Name + " (Inactiva)"; }
                }

                public AreaOption(int id, string name, string state)
                {
                    Id = id;
                    Name = name;
                    State = state;
                }
            }

            private sealed class BufferedGrid : DataGridView
            {
                public BufferedGrid() { DoubleBuffered = true; }
            }

            private sealed class Card : Panel
            {
                public Card()
                {
                    DoubleBuffered = true;
                    ResizeRedraw = true;
                }

                protected override void OnPaint(PaintEventArgs e)
                {
                    base.OnPaint(e);
                    if (Width < 20 || Height < 20) return;

                    using (GraphicsPath path = new GraphicsPath())
                    using (Pen pen = new Pen(Color.FromArgb(220, 229, 235)))
                    {
                        const int diameter = 18;
                        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                        path.AddArc(0, 0, diameter, diameter, 180, 90);
                        path.AddArc(Width - diameter - 1, 0, diameter, diameter, 270, 90);
                        path.AddArc(Width - diameter - 1, Height - diameter - 1, diameter, diameter, 0, 90);
                        path.AddArc(0, Height - diameter - 1, diameter, diameter, 90, 90);
                        path.CloseFigure();
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            }
        }
    }
