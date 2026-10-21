using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Npgsql;

namespace SUSTENTACION.PanelReportes
{
    /// <summary>
    /// Panel de Reportes con KPIs, filtro por fechas, gráficos simples y exportación a CSV.
    /// Solo lectura. Basado en las tablas: equipos, salidas, salida_equipos, retornos,
    /// trabajadores y solicitudes_cambio.
    /// </summary>
    public class PanelReportes : UserControl
    {
        // ---------- Paleta ----------
        private readonly Color fondo = Color.FromArgb(243, 246, 250);
        private readonly Color tarjeta = Color.White;
        private readonly Color verde = Color.FromArgb(19, 125, 91);
        private readonly Color texto = Color.FromArgb(26, 39, 54);
        private readonly Color tenue = Color.FromArgb(106, 122, 140);
        private readonly Color linea = Color.FromArgb(226, 232, 239);
        private readonly Color rojo = Color.FromArgb(178, 45, 45);
        private readonly Color ambar = Color.FromArgb(190, 130, 30);
        private readonly Color azul = Color.FromArgb(28, 107, 185);
        private readonly Color morado = Color.FromArgb(119, 79, 178);

        // ---------- Controles ----------
        private Label lblConexion;
        private DateTimePicker dtDesde, dtHasta;
        private Button btnConsultar, btnExportar;

        private Label lblKpiEquipos, lblKpiStock, lblKpiSalidas, lblKpiDevoluciones;
        private Label lblKpiPendientes, lblKpiSolicitudesPend, lblKpiSolicitudesAprob, lblKpiSolicitudesRech;

        private DataGridView gridCategorias;
        private DataGridView gridMovimientos;
        private DataGridView gridTopEquipos;
        private DataGridView gridTrabajadores;

        private bool cargando;

        public PanelReportes()
        {
            Dock = DockStyle.Fill;
            BackColor = fondo;
            Font = new Font("Segoe UI", 9F);
            ConstruirInterfaz();
            Load += (s, e) => CargarTodo(true);
        }

        // =================================================================
        //  INTERFAZ
        // =================================================================
        private void ConstruirInterfaz()
        {
            // Panel raíz con auto-scroll
            var raiz = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = fondo,
                Padding = new Padding(22)
            };
            Controls.Add(raiz);

            int y = 0;

            // ----- Encabezado -----
            var lblMarca = new Label
            {
                Text = "ARTEMUSA TV / ADMINISTRACIÓN",
                AutoSize = true,
                ForeColor = verde,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Location = new Point(0, y)
            };
            raiz.Controls.Add(lblMarca);
            y += 20;

            var lblTitulo = new Label
            {
                Text = "Panel de Reportes",
                AutoSize = true,
                ForeColor = texto,
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                Location = new Point(0, y)
            };
            raiz.Controls.Add(lblTitulo);
            y += 42;

            var lblSub = new Label
            {
                Text = "Resumen visual de equipos, movimientos y solicitudes.",
                AutoSize = true,
                ForeColor = tenue,
                Location = new Point(2, y)
            };
            raiz.Controls.Add(lblSub);
            y += 30;

            lblConexion = new Label
            {
                Text = "● Verificando conexión",
                AutoSize = true,
                ForeColor = tenue,
                Location = new Point(raiz.Width - 200, 5),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            raiz.Controls.Add(lblConexion);

            // ----- Filtros de fecha -----
            var filtros = new Panel
            {
                Location = new Point(0, y),
                Size = new Size(raiz.Width - 40, 60),
                BackColor = tarjeta,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Padding = new Padding(12)
            };
            raiz.Controls.Add(filtros);
            y += 70;

            filtros.Controls.Add(new Label
            {
                Text = "DESDE",
                AutoSize = true,
                ForeColor = tenue,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Location = new Point(12, 8)
            });
            dtDesde = new DateTimePicker
            {
                Location = new Point(12, 26),
                Width = 130,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today.AddDays(-30)
            };
            filtros.Controls.Add(dtDesde);

            filtros.Controls.Add(new Label
            {
                Text = "HASTA",
                AutoSize = true,
                ForeColor = tenue,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Location = new Point(158, 8)
            });
            dtHasta = new DateTimePicker
            {
                Location = new Point(158, 26),
                Width = 130,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today
            };
            filtros.Controls.Add(dtHasta);

            btnConsultar = new Button
            {
                Text = "↻  Consultar",
                Width = 130,
                Height = 28,
                Location = new Point(306, 24),
                BackColor = verde,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnConsultar.FlatAppearance.BorderSize = 0;
            btnConsultar.Click += (s, e) => CargarTodo(true);
            filtros.Controls.Add(btnConsultar);

            btnExportar = new Button
            {
                Text = "⬇  Exportar CSV",
                Width = 140,
                Height = 28,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(filtros.Width - 152, 24),
                BackColor = azul,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnExportar.FlatAppearance.BorderSize = 0;
            btnExportar.Click += (s, e) => ExportarCsv();
            filtros.Controls.Add(btnExportar);
            filtros.Resize += (s, e) =>
                btnExportar.Location = new Point(filtros.ClientSize.Width - btnExportar.Width - 12, 24);

            // ----- KPIs (2 filas de 4) -----
            var kpis1 = new TableLayoutPanel
            {
                Location = new Point(0, y),
                Size = new Size(raiz.Width - 40, 110),
                ColumnCount = 4,
                RowCount = 1,
                BackColor = fondo,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            for (int i = 0; i < 4; i++)
                kpis1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            raiz.Controls.Add(kpis1);
            lblKpiEquipos = AgregarKpi(kpis1, 0, "EQUIPOS REGISTRADOS", verde);
            lblKpiStock = AgregarKpi(kpis1, 1, "STOCK TOTAL", azul);
            lblKpiSalidas = AgregarKpi(kpis1, 2, "SALIDAS EN EL RANGO", ambar);
            lblKpiDevoluciones = AgregarKpi(kpis1, 3, "DEVOLUCIONES EN EL RANGO", morado);
            y += 120;

            var kpis2 = new TableLayoutPanel
            {
                Location = new Point(0, y),
                Size = new Size(raiz.Width - 40, 110),
                ColumnCount = 4,
                RowCount = 1,
                BackColor = fondo,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            for (int i = 0; i < 4; i++)
                kpis2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            raiz.Controls.Add(kpis2);
            lblKpiPendientes = AgregarKpi(kpis2, 0, "SALIDAS PENDIENTES", rojo);
            lblKpiSolicitudesPend = AgregarKpi(kpis2, 1, "SOLICITUDES PENDIENTES", ambar);
            lblKpiSolicitudesAprob = AgregarKpi(kpis2, 2, "SOLICITUDES APROBADAS", verde);
            lblKpiSolicitudesRech = AgregarKpi(kpis2, 3, "SOLICITUDES RECHAZADAS", rojo);
            y += 120;

            // ----- Grid: Equipos por categoría -----
            gridCategorias = CrearGrid();
            var bloqueCategorias = CrearBloque("EQUIPOS POR CATEGORÍA", gridCategorias, raiz.Width - 40, 220);
            bloqueCategorias.Location = new Point(0, y);
            bloqueCategorias.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            raiz.Controls.Add(bloqueCategorias);
            y += 240;

            // ----- Grid: Movimientos por tipo -----
            gridMovimientos = CrearGrid();
            var bloqueMovimientos = CrearBloque("MOVIMIENTOS EN EL RANGO", gridMovimientos, raiz.Width - 40, 220);
            bloqueMovimientos.Location = new Point(0, y);
            bloqueMovimientos.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            raiz.Controls.Add(bloqueMovimientos);
            y += 240;

            // ----- Grid: Top equipos -----
            gridTopEquipos = CrearGrid();
            var bloqueTop = CrearBloque("TOP 10 EQUIPOS MÁS MOVIDOS EN EL RANGO", gridTopEquipos, raiz.Width - 40, 280);
            bloqueTop.Location = new Point(0, y);
            bloqueTop.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            raiz.Controls.Add(bloqueTop);
            y += 300;

            // ----- Grid: Trabajadores más activos -----
            gridTrabajadores = CrearGrid();
            var bloqueTrab = CrearBloque("TRABAJADORES MÁS ACTIVOS EN EL RANGO", gridTrabajadores, raiz.Width - 40, 260);
            bloqueTrab.Location = new Point(0, y);
            bloqueTrab.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            raiz.Controls.Add(bloqueTrab);
            y += 280;

            // Ajustar ancho de bloques al redimensionar
            raiz.Resize += (s, e) =>
            {
                int ancho = raiz.ClientSize.Width - 40;
                if (ancho < 400) ancho = 400;

                filtros.Width = ancho;
                kpis1.Width = ancho;
                kpis2.Width = ancho;
                bloqueCategorias.Width = ancho;
                bloqueMovimientos.Width = ancho;
                bloqueTop.Width = ancho;
                bloqueTrab.Width = ancho;
                lblConexion.Left = ancho - 200;

                gridCategorias.Width = ancho - 20;
                gridMovimientos.Width = ancho - 20;
                gridTopEquipos.Width = ancho - 20;
                gridTrabajadores.Width = ancho - 20;
            };
        }

        private Label AgregarKpi(TableLayoutPanel panel, int columna, string titulo, Color color)
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = tarjeta,
                Margin = new Padding(0, 0, columna == 3 ? 0 : 10, 10),
                Padding = new Padding(12)
            };
            card.Controls.Add(new Label
            {
                Text = titulo,
                AutoSize = true,
                ForeColor = tenue,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Location = new Point(12, 10)
            });
            var lbl = new Label
            {
                Text = "—",
                AutoSize = true,
                ForeColor = texto,
                Font = new Font("Segoe UI", 21F, FontStyle.Bold),
                Location = new Point(12, 34)
            };
            card.Controls.Add(lbl);
            card.Paint += (s, e) =>
            {
                using (var pen = new Pen(color, 3))
                    e.Graphics.DrawLine(pen, 12, card.Height - 3, card.Width - 12, card.Height - 3);
            };
            panel.Controls.Add(card, columna, 0);
            return lbl;
        }

        private DataGridView CrearGrid()
        {
            var grid = new DataGridView
            {
                Location = new Point(10, 40),
                Size = new Size(900, 160),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false,
                GridColor = linea
            };
            grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(240, 244, 248),
                ForeColor = tenue,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                SelectionBackColor = Color.FromArgb(240, 244, 248),
                SelectionForeColor = tenue,
                Padding = new Padding(6)
            };
            grid.DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = texto,
                SelectionBackColor = Color.FromArgb(226, 243, 235),
                SelectionForeColor = texto,
                Padding = new Padding(5)
            };
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 251, 253);
            grid.RowTemplate.Height = 32;
            return grid;
        }

        private Panel CrearBloque(string titulo, DataGridView grid, int ancho, int alto)
        {
            var bloque = new Panel
            {
                Size = new Size(ancho, alto),
                BackColor = tarjeta
            };
            bloque.Controls.Add(new Label
            {
                Text = titulo,
                AutoSize = true,
                ForeColor = verde,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Location = new Point(12, 10)
            });
            grid.Size = new Size(ancho - 20, alto - 50);
            bloque.Controls.Add(grid);
            bloque.Resize += (s, e) =>
                grid.Size = new Size(bloque.Width - 20, bloque.Height - 50);
            return bloque;
        }

        // =================================================================
        //  CONEXIÓN
        // =================================================================
        private NpgsqlConnection AbrirConexion()
        {
            var conexion = new global::SUSTENTACION.Conexion().ObtenerConexion();
            if (conexion == null) throw new InvalidOperationException("La conexión devolvió un valor vacío.");
            if (conexion.State != ConnectionState.Open) conexion.Open();
            return conexion;
        }

        // =================================================================
        //  CARGA PRINCIPAL
        // =================================================================
        private void CargarTodo(bool mostrarErrores)
        {
            if (cargando || IsDisposed) return;
            if (dtDesde.Value.Date > dtHasta.Value.Date)
            {
                MessageBox.Show(this, "La fecha inicial no puede ser posterior a la final.",
                    "Reportes", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            cargando = true;
            btnConsultar.Enabled = false;

            try
            {
                using (var conexion = AbrirConexion())
                {
                    DateTime desde = dtDesde.Value.Date;
                    DateTime hasta = dtHasta.Value.Date.AddDays(1); // incluye el día completo

                    CargarKpis(conexion, desde, hasta);
                    gridCategorias.DataSource = CargarCategorias(conexion);
                    gridMovimientos.DataSource = CargarMovimientos(conexion, desde, hasta);
                    gridTopEquipos.DataSource = CargarTopEquipos(conexion, desde, hasta);
                    gridTrabajadores.DataSource = CargarTrabajadores(conexion, desde, hasta);
                }

                lblConexion.Text = "● Conectado a Supabase · " + DateTime.Now.ToString("HH:mm:ss");
                lblConexion.ForeColor = verde;
            }
            catch (Exception ex)
            {
                lblConexion.Text = "● Error de conexión o consulta";
                lblConexion.ForeColor = Color.Firebrick;
                if (mostrarErrores)
                    MessageBox.Show(this, "No se pudieron cargar los reportes.\r\n\r\n" + ex.Message,
                        "Reportes", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                cargando = false;
                btnConsultar.Enabled = true;
            }
        }

        // =================================================================
        //  KPIs
        // =================================================================
        private void CargarKpis(NpgsqlConnection conexion, DateTime desde, DateTime hasta)
        {
            // --- Equipos y stock ---
            using (var cmd = new NpgsqlCommand(
                "SELECT COUNT(*) AS total, COALESCE(SUM(stock),0) AS stock_total FROM public.equipos",
                conexion))
            using (var reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    lblKpiEquipos.Text = Convert.ToInt64(reader["total"]).ToString("N0");
                    lblKpiStock.Text = Convert.ToInt64(reader["stock_total"]).ToString("N0");
                }
            }

            // --- Salidas / devoluciones en el rango ---
            using (var cmd = new NpgsqlCommand(@"
SELECT
    (SELECT COUNT(*) FROM public.salidas
     WHERE fecha_salida >= @desde AND fecha_salida < @hasta) AS salidas_rango,
    (SELECT COUNT(*) FROM public.retornos
     WHERE fecha_retorno >= @desde AND fecha_retorno < @hasta) AS devoluciones_rango,
    (SELECT COUNT(*) FROM public.salidas s
     WHERE NOT EXISTS (
         SELECT 1 FROM public.retornos r WHERE r.id_salida = s.id_salida)
    ) AS pendientes", conexion))
            {
                cmd.Parameters.AddWithValue("@desde", desde);
                cmd.Parameters.AddWithValue("@hasta", hasta);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        lblKpiSalidas.Text = Convert.ToInt64(reader["salidas_rango"]).ToString("N0");
                        lblKpiDevoluciones.Text = Convert.ToInt64(reader["devoluciones_rango"]).ToString("N0");
                        lblKpiPendientes.Text = Convert.ToInt64(reader["pendientes"]).ToString("N0");
                    }
                }
            }

            // --- Solicitudes de cambio por estado ---
            using (var cmd = new NpgsqlCommand(@"
SELECT
    COUNT(*) FILTER (WHERE COALESCE(estado,'Pendiente') = 'Pendiente') AS pendientes,
    COUNT(*) FILTER (WHERE estado = 'Aprobada')  AS aprobadas,
    COUNT(*) FILTER (WHERE estado = 'Rechazada') AS rechazadas
FROM public.solicitudes_cambio", conexion))
            using (var reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    lblKpiSolicitudesPend.Text = Convert.ToInt64(reader["pendientes"]).ToString("N0");
                    lblKpiSolicitudesAprob.Text = Convert.ToInt64(reader["aprobadas"]).ToString("N0");
                    lblKpiSolicitudesRech.Text = Convert.ToInt64(reader["rechazadas"]).ToString("N0");
                }
            }
        }

        // =================================================================
        //  GRIDS
        // =================================================================
        private DataTable CargarCategorias(NpgsqlConnection conexion)
        {
            var tabla = new DataTable();
            using (var adaptador = new NpgsqlDataAdapter(@"
SELECT
    COALESCE(categoria,'Sin categoría') AS categoria,
    COUNT(*)      AS equipos,
    COALESCE(SUM(stock),0) AS stock_total
FROM public.equipos
GROUP BY COALESCE(categoria,'Sin categoría')
ORDER BY equipos DESC;", conexion))
                adaptador.Fill(tabla);
            ConfigurarColumnasCategorias();
            return tabla;
        }

        private void ConfigurarColumnasCategorias()
        {
            if (gridCategorias.Columns.Count == 0) return;
            if (gridCategorias.Columns.Contains("categoria"))
                gridCategorias.Columns["categoria"].HeaderText = "Categoría";
            if (gridCategorias.Columns.Contains("equipos"))
                gridCategorias.Columns["equipos"].HeaderText = "Cantidad de equipos";
            if (gridCategorias.Columns.Contains("stock_total"))
                gridCategorias.Columns["stock_total"].HeaderText = "Stock total";
        }

        private DataTable CargarMovimientos(NpgsqlConnection conexion, DateTime desde, DateTime hasta)
        {
            var tabla = new DataTable();
            using (var adaptador = new NpgsqlDataAdapter(@"
SELECT
    tipo,
    COUNT(*) AS cantidad
FROM (
    SELECT 'Salida'::text      AS tipo, fecha_salida  AS fecha
    FROM public.salidas
    UNION ALL
    SELECT 'Devolución'::text  AS tipo, fecha_retorno AS fecha
    FROM public.retornos
) t
WHERE fecha >= @desde AND fecha < @hasta
GROUP BY tipo
ORDER BY cantidad DESC;", conexion))
            {
                adaptador.SelectCommand.Parameters.AddWithValue("@desde", desde);
                adaptador.SelectCommand.Parameters.AddWithValue("@hasta", hasta);
                adaptador.Fill(tabla);
            }
            ConfigurarColumnasMovimientos();
            return tabla;
        }

        private void ConfigurarColumnasMovimientos()
        {
            if (gridMovimientos.Columns.Count == 0) return;
            if (gridMovimientos.Columns.Contains("tipo"))
                gridMovimientos.Columns["tipo"].HeaderText = "Tipo de movimiento";
            if (gridMovimientos.Columns.Contains("cantidad"))
                gridMovimientos.Columns["cantidad"].HeaderText = "Cantidad";
        }

        private DataTable CargarTopEquipos(NpgsqlConnection conexion, DateTime desde, DateTime hasta)
        {
            var tabla = new DataTable();
            using (var adaptador = new NpgsqlDataAdapter(@"
SELECT
    e.codigo     AS codigo,
    e.nombre     AS equipo,
    COUNT(*)     AS movimientos
FROM public.salida_equipos se
JOIN public.salidas  s ON s.id_salida = se.id_salida
JOIN public.equipos  e ON e.id = se.id_equipo
WHERE s.fecha_salida >= @desde AND s.fecha_salida < @hasta
GROUP BY e.codigo, e.nombre
ORDER BY movimientos DESC
LIMIT 10;", conexion))
            {
                adaptador.SelectCommand.Parameters.AddWithValue("@desde", desde);
                adaptador.SelectCommand.Parameters.AddWithValue("@hasta", hasta);
                adaptador.Fill(tabla);
            }
            ConfigurarColumnasTop();
            return tabla;
        }

        private void ConfigurarColumnasTop()
        {
            if (gridTopEquipos.Columns.Count == 0) return;
            if (gridTopEquipos.Columns.Contains("codigo"))
                gridTopEquipos.Columns["codigo"].HeaderText = "Código";
            if (gridTopEquipos.Columns.Contains("equipo"))
                gridTopEquipos.Columns["equipo"].HeaderText = "Equipo";
            if (gridTopEquipos.Columns.Contains("movimientos"))
                gridTopEquipos.Columns["movimientos"].HeaderText = "Movimientos";
        }

        private DataTable CargarTrabajadores(NpgsqlConnection conexion, DateTime desde, DateTime hasta)
        {
            var tabla = new DataTable();
            using (var adaptador = new NpgsqlDataAdapter(@"
SELECT
    COALESCE(t.nombre,'(sin nombre)') AS trabajador,
    COALESCE(t.cargo,'')              AS cargo,
    COUNT(*)                          AS salidas
FROM public.salidas s
LEFT JOIN public.trabajadores t ON t.id_trabajador = s.id_trabajador
WHERE s.fecha_salida >= @desde AND s.fecha_salida < @hasta
GROUP BY t.nombre, t.cargo
ORDER BY salidas DESC
LIMIT 10;", conexion))
            {
                adaptador.SelectCommand.Parameters.AddWithValue("@desde", desde);
                adaptador.SelectCommand.Parameters.AddWithValue("@hasta", hasta);
                adaptador.Fill(tabla);
            }
            ConfigurarColumnasTrabajadores();
            return tabla;
        }

        private void ConfigurarColumnasTrabajadores()
        {
            if (gridTrabajadores.Columns.Count == 0) return;
            if (gridTrabajadores.Columns.Contains("trabajador"))
                gridTrabajadores.Columns["trabajador"].HeaderText = "Trabajador";
            if (gridTrabajadores.Columns.Contains("cargo"))
                gridTrabajadores.Columns["cargo"].HeaderText = "Cargo";
            if (gridTrabajadores.Columns.Contains("salidas"))
                gridTrabajadores.Columns["salidas"].HeaderText = "Salidas registradas";
        }

        // =================================================================
        //  EXPORTAR CSV
        // =================================================================
        private void ExportarCsv()
        {
            using (var dlg = new SaveFileDialog
            {
                Title = "Exportar reportes",
                Filter = "Archivo CSV (*.csv)|*.csv",
                FileName = "Reportes_Artemusa_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".csv"
            })
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                try
                {
                    using (var escritor = new StreamWriter(dlg.FileName, false, new UTF8Encoding(true)))
                    {
                        escritor.WriteLine("REPORTE GENERAL - " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
                        escritor.WriteLine("Rango: " + dtDesde.Value.ToString("dd/MM/yyyy")
                            + " a " + dtHasta.Value.ToString("dd/MM/yyyy"));
                        escritor.WriteLine();

                        escritor.WriteLine("INDICADORES");
                        escritor.WriteLine("Equipos registrados;" + lblKpiEquipos.Text);
                        escritor.WriteLine("Stock total;" + lblKpiStock.Text);
                        escritor.WriteLine("Salidas en el rango;" + lblKpiSalidas.Text);
                        escritor.WriteLine("Devoluciones en el rango;" + lblKpiDevoluciones.Text);
                        escritor.WriteLine("Salidas pendientes;" + lblKpiPendientes.Text);
                        escritor.WriteLine("Solicitudes pendientes;" + lblKpiSolicitudesPend.Text);
                        escritor.WriteLine("Solicitudes aprobadas;" + lblKpiSolicitudesAprob.Text);
                        escritor.WriteLine("Solicitudes rechazadas;" + lblKpiSolicitudesRech.Text);
                        escritor.WriteLine();

                        EscribirGrid(escritor, "EQUIPOS POR CATEGORÍA", gridCategorias);
                        EscribirGrid(escritor, "MOVIMIENTOS EN EL RANGO", gridMovimientos);
                        EscribirGrid(escritor, "TOP 10 EQUIPOS MÁS MOVIDOS", gridTopEquipos);
                        EscribirGrid(escritor, "TRABAJADORES MÁS ACTIVOS", gridTrabajadores);
                    }
                    MessageBox.Show(this, "Reporte exportado correctamente.",
                        "Exportar", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "No se pudo exportar.\r\n\r\n" + ex.Message,
                        "Exportar", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void EscribirGrid(StreamWriter escritor, string titulo, DataGridView grid)
        {
            escritor.WriteLine("== " + titulo + " ==");
            for (int c = 0; c < grid.Columns.Count; c++)
            {
                if (c > 0) escritor.Write(";");
                escritor.Write(EscaparCsv(grid.Columns[c].HeaderText));
            }
            escritor.WriteLine();

            foreach (DataGridViewRow fila in grid.Rows)
            {
                if (fila.IsNewRow) continue;
                for (int c = 0; c < grid.Columns.Count; c++)
                {
                    if (c > 0) escritor.Write(";");
                    escritor.Write(EscaparCsv(Convert.ToString(fila.Cells[c].FormattedValue) ?? ""));
                }
                escritor.WriteLine();
            }
            escritor.WriteLine();
        }

        private string EscaparCsv(string valor)
        {
            valor = valor ?? "";
            return "\"" + valor.Replace("\"", "\"\"") + "\"";
        }
    }
}