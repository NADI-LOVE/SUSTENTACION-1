#nullable disable
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Npgsql;
using NpgsqlTypes;

namespace SUSTENTACION.PanelAdministrador
{
    /// <summary>
    /// Dashboard oscuro del panel de administración
    /// (Gestión y seguimiento de equipos audiovisuales - Artemusa Perú S.R.L.).
    /// </summary>
    public class PanelResumen : UserControl
    {
        // ═════════════════════ PALETA ═════════════════════
        private static readonly Color CFondo = Color.FromArgb(5, 11, 24);
        private static readonly Color CTarjeta = Color.FromArgb(10, 20, 40);
        private static readonly Color CTarjeta2 = Color.FromArgb(14, 27, 52);
        private static readonly Color CLinea = Color.FromArgb(27, 45, 82);
        private static readonly Color CTexto = Color.FromArgb(232, 240, 255);
        private static readonly Color CTenue = Color.FromArgb(128, 147, 181);
        private static readonly Color CCian = Color.FromArgb(34, 211, 238);
        private static readonly Color CAzul = Color.FromArgb(59, 130, 246);
        private static readonly Color CVerde = Color.FromArgb(34, 197, 94);
        private static readonly Color CNaranja = Color.FromArgb(245, 158, 11);
        private static readonly Color CRojo = Color.FromArgb(239, 68, 68);
        private static readonly Color CMorado = Color.FromArgb(139, 92, 246);
        private static readonly Color CTeal = Color.FromArgb(16, 185, 129);
        private static readonly Color CAmarillo = Color.FromArgb(234, 179, 8);

        // ═════════════════════ FUENTES ═════════════════════
        private static readonly Font F75B = new Font("Segoe UI", 7.75F, FontStyle.Bold);
        private static readonly Font F8 = new Font("Segoe UI", 8F);
        private static readonly Font F85 = new Font("Segoe UI", 8.5F);
        private static readonly Font F85B = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        private static readonly Font F9 = new Font("Segoe UI", 9F);
        private static readonly Font F9B = new Font("Segoe UI", 9F, FontStyle.Bold);
        private static readonly Font F95 = new Font("Segoe UI", 9.5F);
        private static readonly Font F105B = new Font("Segoe UI", 10.5F, FontStyle.Bold);
        private static readonly Font F22B = new Font("Segoe UI", 22F, FontStyle.Bold);
        private static readonly Font Sym9 = new Font("Segoe UI Symbol", 9F);
        private static readonly Font Sym11 = new Font("Segoe UI Symbol", 11F);
        private static readonly Font Sym12 = new Font("Segoe UI Symbol", 12F);
        private static readonly Font Sym16 = new Font("Segoe UI Symbol", 16F, FontStyle.Bold);

        // ═════════════════════ DATOS DE USUARIO / EVENTOS ═════════════════════
        public string NombreUsuario { get; set; } = "Daniel Laura";
        public string RolUsuario { get; set; } = "Administrador";

        public event EventHandler RegistrarEntradaClick;
        public event EventHandler RegistrarSalidaClick;
        public event EventHandler VerMovimientosClick;
        public event EventHandler GenerarReporteClick;
        public event EventHandler VerNotificacionesClick;
        public event EventHandler<string> VerEquipo;
        public event EventHandler<string> EditarEquipo;
        public event EventHandler<string> ExportarEquipo;

        // ═════════════════════ CONTROLES ═════════════════════
        private TarjetaKpi kTotal, kDisp, kAsig, kMant, kDev;
        private GraficoTendencias chartTend;
        private GraficoCategorias chartCat;
        //private PanelAlertas panelAlertas;
        private PanelEstados panelEstados;
        private ListaMovimientos listaMov;
        private DataGridView dgv;
        private DataTable tablaDatos;
        private Button btnActualizar;
        private System.Windows.Forms.Timer timer;
        private bool cargando;

        public PanelResumen()
        {
            Dock = DockStyle.Fill;
            BackColor = CFondo;
            DoubleBuffered = true;
            Font = new Font("Segoe UI", 9F);
            Padding = new Padding(20, 12, 20, 14);
            AutoScroll = true;
            AutoScrollMinSize = new Size(1180, 960);

            ConstruirInterfaz();

            timer = new System.Windows.Forms.Timer { Interval = 60000 };
            timer.Tick += (s, e) => CargarDatos(false);

            Load += (s, e) =>
            {
                CargarDatos(true);
                timer.Start();
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && timer != null)
            {
                timer.Stop();
                timer.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>Permite al formulario padre refrescar el dashboard.</summary>
        public void Actualizar()
        {
            CargarDatos(false);
        }

        // ═════════════════════════════════════════════════════════
        //                       INTERFAZ
        // ═════════════════════════════════════════════════════════
        private void ConstruirInterfaz()
        {
            var main = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                BackColor = CFondo,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));  // Título
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 128)); // KPIs
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 36));   // Gráficos
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 28));   // Fila Inferior
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 36));   // Tabla
            Controls.Add(main);

            main.Controls.Add(CrearTitulo(), 0, 0);
            main.Controls.Add(CrearFilaKpis(), 0, 1);
            main.Controls.Add(CrearFilaGraficos(), 0, 2);
            main.Controls.Add(CrearFilaInferior(), 0, 3);
            main.Controls.Add(CrearTablaMovimientos(), 0, 4);
        }

        private Control CrearTitulo()
        {
            var fila = new Panel { Dock = DockStyle.Fill, BackColor = CFondo, Margin = Padding.Empty };

            var izquierda = new Panel { Dock = DockStyle.Fill, BackColor = CFondo };
            izquierda.Controls.Add(new Label
            {
                Text = "ARTEMUSA AV   ›   Dashboard",
                AutoSize = true,
                ForeColor = CTenue,
                BackColor = CFondo,
                Font = new Font("Segoe UI", 8F),
                Location = new Point(1, 4)
            });
            izquierda.Controls.Add(new Label
            {
                Text = "Panel de Control",
                AutoSize = true,
                ForeColor = Color.White,
                BackColor = CFondo,
                Font = new Font("Segoe UI", 19F, FontStyle.Bold),
                Location = new Point(0, 18)
            });
            izquierda.Controls.Add(new Label
            {
                Text = "Gestión y seguimiento de equipos audiovisuales",
                AutoSize = true,
                ForeColor = Color.FromArgb(96, 165, 250),
                BackColor = CFondo,
                Font = new Font("Segoe UI", 9F),
                Location = new Point(2, 50)
            });
            fila.Controls.Add(izquierda);

            var derecha = new Panel { Dock = DockStyle.Right, Width = 340, BackColor = CFondo };
            var fecha = new BotonFecha { Size = new Size(210, 36), Location = new Point(130, 14) };
            derecha.Controls.Add(fecha);

            btnActualizar = new Button
            {
                Text = "↻  Actualizar",
                Size = new Size(112, 36),
                Location = new Point(10, 14),
                BackColor = CTarjeta,
                ForeColor = CTexto,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 8.75F, FontStyle.Bold)
            };
            btnActualizar.FlatAppearance.BorderColor = CLinea;
            btnActualizar.FlatAppearance.MouseOverBackColor = CTarjeta2;
            btnActualizar.Click += (s, e) => CargarDatos(true);
            derecha.Controls.Add(btnActualizar);
            fila.Controls.Add(derecha);

            return fila;
        }

        private Control CrearFilaKpis()
        {
            var t = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                RowCount = 1,
                BackColor = CFondo,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            for (int i = 0; i < 5; i++) t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));

            kTotal = new TarjetaKpi("Total de Equipos", "▣", CAzul);
            kDisp = new TarjetaKpi("Equipos Disponibles", "⬢", CVerde);
            kAsig = new TarjetaKpi("Equipos Asignados", "☻", CMorado);
            kMant = new TarjetaKpi("En Mantenimiento", "⚙", CNaranja);
            kDev = new TarjetaKpi("Devoluciones Pendientes", "⟳", CRojo);

            var tarjetas = new[] { kTotal, kDisp, kAsig, kMant, kDev };
            for (int i = 0; i < tarjetas.Length; i++)
            {
                tarjetas[i].Margin = new Padding(i == 0 ? 0 : 6, 0, i == 4 ? 0 : 6, 10);
                t.Controls.Add(tarjetas[i], i, 0);
            }
            return t;
        }

        private Control CrearFilaGraficos()
        {
            var t = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2, // <-- CAMBIO: Antes era 3, ahora es 2
                RowCount = 1,
                BackColor = CFondo,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };

            // CAMBIO: Ambas columnas al 50%
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            // Tendencias (Se queda igual)
            var cTend = new TarjetaOscura("Tendencias de Préstamos y Devoluciones", "↗", CCian) { Margin = new Padding(0, 0, 6, 10) };
            chartTend = new GraficoTendencias { Dock = DockStyle.Fill };
            cTend.Controls.Add(chartTend);
            var selector = new SelectorRango { Size = new Size(132, 28) };
            selector.Cambio += (s, e) => chartTend.Rango = selector.Dias;
            cTend.Controls.Add(selector);
            cTend.Resize += (s, e) => selector.Location = new Point(cTend.Width - selector.Width - 16, 10);
            selector.Location = new Point(Math.Max(0, cTend.Width - selector.Width - 16), 10);
            t.Controls.Add(cTend, 0, 0);

            // Categorías (Se queda igual, pero cambia su margen derecho para que quede bien)
            var cCat = new TarjetaOscura("Distribución por Categoría", "◔", CCian) { Margin = new Padding(6, 0, 0, 10) }; // <-- CAMBIO: Margen derecho a 0
            chartCat = new GraficoCategorias { Dock = DockStyle.Fill };
            cCat.Controls.Add(chartCat);
            t.Controls.Add(cCat, 1, 0);

            // AQUI COMENTAMOS O ELIMINAMOS EL PANEL DE WHATSAPP
            /*
            var cWa = new TarjetaOscura("Gestión de Alertas de WhatsApp", "✆", CVerde) { Margin = new Padding(6, 0, 0, 10) };
            panelAlertas = new PanelAlertas { Dock = DockStyle.Fill };
            panelAlertas.VerTodas += (s, e) =>
            {
                var h = VerNotificacionesClick;
                if (h != null) h(this, EventArgs.Empty);
            };
            cWa.Controls.Add(panelAlertas);
            var swPrincipal = new ToggleSwitch { Checked = true };
            cWa.Controls.Add(swPrincipal);
            cWa.Resize += (s, e) => swPrincipal.Location = new Point(cWa.Width - swPrincipal.Width - 16, 13);
            swPrincipal.Location = new Point(Math.Max(0, cWa.Width - swPrincipal.Width - 16), 13);
            t.Controls.Add(cWa, 2, 0);
            */

            return t;
        }

        private Control CrearFilaInferior()
        {
            var t = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2, // <-- CAMBIO 1: Cambiar de 3 a 2 columnas
                RowCount = 1,
                BackColor = CFondo,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };

            // Ambas columnas al 50%
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            // Estado general (Columna 0)
            var cEst = new TarjetaOscura("Estado General de Equipos", "▣", CCian) { Margin = new Padding(0, 0, 6, 10) };
            panelEstados = new PanelEstados { Dock = DockStyle.Fill };
            cEst.Controls.Add(panelEstados);
            t.Controls.Add(cEst, 0, 0);

            // Acciones rápidas (comentado, ya no se usa)
            // ...

            // Últimos movimientos (Columna 1)
            var cUlt = new TarjetaOscura("Últimos Movimientos", "⇄", CCian) { Margin = new Padding(6, 0, 0, 10) };
            listaMov = new ListaMovimientos { Dock = DockStyle.Fill };
            cUlt.Controls.Add(listaMov);
            var lnk = new Label
            {
                Text = "Ver todos",
                AutoSize = true,
                ForeColor = CCian,
                BackColor = CTarjeta,
                Font = F85,
                Cursor = Cursors.Hand
            };
            lnk.Click += (s, e) => Lanzar(VerMovimientosClick);
            cUlt.Controls.Add(lnk);
            cUlt.Resize += (s, e) => lnk.Location = new Point(cUlt.Width - lnk.Width - 18, 15);
            lnk.Location = new Point(Math.Max(0, cUlt.Width - lnk.Width - 18), 15);

            t.Controls.Add(cUlt, 1, 0); // <-- CAMBIO 2: Agregarlo en la columna 1, no en la 2

            return t;
        }

        private Control CrearTablaMovimientos()
        {
            var card = new TarjetaOscura("Movimientos de Equipos y Estado", "↗", CCian) { Margin = new Padding(0, 0, 0, 0) };
            card.Padding = new Padding(10, 46, 10, 10);

            dgv = new DataGridView
            {
                Name = "dgvMovimientos",
                Dock = DockStyle.Fill,
                BackgroundColor = CTarjeta,
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                MultiSelect = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EnableHeadersVisualStyles = false,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                GridColor = Color.FromArgb(20, 36, 68),
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                EditMode = DataGridViewEditMode.EditProgrammatically
            };
            dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(9, 18, 36),
                ForeColor = CTenue,
                Font = new Font("Segoe UI", 8.25F, FontStyle.Bold),
                Padding = new Padding(8, 0, 0, 0),
                SelectionBackColor = Color.FromArgb(9, 18, 36),
                SelectionForeColor = CTenue,
                Alignment = DataGridViewContentAlignment.MiddleLeft
            };
            dgv.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = CTarjeta,
                ForeColor = CTexto,
                Font = new Font("Segoe UI", 8.75F),
                SelectionBackColor = Color.FromArgb(18, 38, 76),
                SelectionForeColor = Color.White,
                Padding = new Padding(8, 0, 0, 0)
            };
            dgv.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(12, 24, 46),
                ForeColor = CTexto,
                SelectionBackColor = Color.FromArgb(18, 38, 76),
                SelectionForeColor = Color.White
            };
            dgv.ColumnHeadersHeight = 34;
            dgv.RowTemplate.Height = 36;
            dgv.CellPainting += Dgv_CellPainting;
            dgv.CellMouseClick += Dgv_CellMouseClick;
            dgv.DataBindingComplete += (s, e) =>
            {
                dgv.ClearSelection();
                dgv.CurrentCell = null;
            };

            card.Controls.Add(dgv);
            return card;
        }

        private static void Lanzar(EventHandler h)
        {
            if (h != null) h(null, EventArgs.Empty);
        }

        // ═════════════════════════════════════════════════════════
        //                  TABLA: PINTADO / CLICS
        // ═════════════════════════════════════════════════════════
        private static Rectangle ZonaAccion(Rectangle celda, int i)
        {
            int w = 26;
            int x = celda.X + (celda.Width - w * 2) / 2 + i * w; // Cambia w * 3 por w * 2
            int y = celda.Y + (celda.Height - w) / 2;
            return new Rectangle(x, y, w, w);
        }

        private static Color ColorBadge(string columna, string texto)
        {
            string t = (texto ?? "").Trim().ToLowerInvariant();
            if (columna == "Estado")
            {
                if (t == "disponible") return Color.FromArgb(22, 163, 74);
                if (t == "asignado") return Color.FromArgb(37, 99, 235);
                if (t == "prestado") return Color.FromArgb(124, 58, 237);
                if (t.Contains("mantenimiento")) return Color.FromArgb(217, 119, 6);
                return Color.FromArgb(71, 85, 105);
            }
            if (columna == "Condición")
            {
                if (t == "bueno") return Color.FromArgb(22, 163, 74);
                if (t == "regular") return Color.FromArgb(217, 119, 6);
                if (t == "malo") return Color.FromArgb(220, 38, 38);
                return Color.FromArgb(71, 85, 105);
            }
            // Tipo
            if (t == "salida") return Color.FromArgb(37, 99, 235);
            if (t == "devolución" || t == "devolucion" || t == "entrada") return Color.FromArgb(22, 163, 74);
            if (t == "traslado") return Color.FromArgb(124, 58, 237);
            if (t == "mantenimiento") return Color.FromArgb(217, 119, 6);
            return Color.FromArgb(71, 85, 105);
        }

        private void Dgv_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            string col = dgv.Columns[e.ColumnIndex].Name;
            if (col != "Estado" && col != "Condición" && col != "Tipo" && col != "Acciones") return;

            e.Paint(e.CellBounds, DataGridViewPaintParts.Background |
                                  DataGridViewPaintParts.Border |
                                  DataGridViewPaintParts.SelectionBackground);
            var g = e.Graphics;
            Calidad(g);

            if (col == "Acciones")
            {
                //string[] glifos = { "◉", "✎", "⇪" };
                //Color[] colores = { CCian, CTenue, CTenue };
                string[] glifos = { "◉", "⇪" };
                Color[] colores = { CCian, CTenue };
                for (int i = 0; i < 2; i++) // Cambia el 3 por 2
                    DtCentro(g, glifos[i], Sym11, colores[i], ZonaAccion(e.CellBounds, i));
            }   
            else
            {
                string txt = Convert.ToString(e.Value ?? "").Trim();
                if (txt.Length > 0)
                {
                    int w = (int)g.MeasureString(txt, F75B).Width + 18;
                    int h = 20;
                    var pill = new Rectangle(
                        e.CellBounds.X + 6,
                        e.CellBounds.Y + (e.CellBounds.Height - h) / 2,
                        Math.Min(w, e.CellBounds.Width - 12), h);
                    Chip(g, txt, ColorBadge(col, txt), Color.White, pill);
                }
            }
            e.Handled = true;
        }

        private void Dgv_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (dgv.Columns[e.ColumnIndex].Name != "Acciones") return;

            Rectangle celda = dgv.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
            var punto = new Point(celda.X + e.X, celda.Y + e.Y);
            string id = Convert.ToString(dgv.Rows[e.RowIndex].Cells["ID"].Value);

            for (int i = 0; i < 2; i++) // Cambia el 3 por 2
            {
                if (!ZonaAccion(celda, i).Contains(punto)) continue;

                if (i == 0)
                {
                    var h = VerEquipo;
                    if (h != null) h(this, id);
                    else MostrarDetalle(e.RowIndex);
                }
                // Se elimina el "else if (i == 1)" que era Editar
                else // Ahora i == 1 es Exportar
                {
                    var h = ExportarEquipo;
                    if (h != null) h(this, id);
                }
                break;
            }
        }

        private void MostrarDetalle(int fila)
        {
            var r = dgv.Rows[fila];
            var sb = new StringBuilder();
            foreach (DataGridViewColumn c in dgv.Columns)
            {
                if (c.Name == "Acciones") continue;
                sb.AppendLine(c.HeaderText + ": " + Convert.ToString(r.Cells[c.Index].Value));
            }
            MessageBox.Show(sb.ToString(), "Detalle del equipo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ═════════════════════════════════════════════════════════
        //                       CARGA DE DATOS
        // ═════════════════════════════════════════════════════════
        private void CargarDatos(bool manual)
        {
            if (cargando) return;
            cargando = true;
            var errores = new List<string>();

            if (btnActualizar != null)
            {
                btnActualizar.Enabled = false;
                btnActualizar.Text = "Cargando...";
            }

            try
            {
                using (var conexion = new global::SUSTENTACION.Conexion().ObtenerConexion())
                {
                    if (conexion == null)
                        throw new InvalidOperationException("La conexión a la base de datos no está disponible.");
                    if (conexion.State != ConnectionState.Open)
                        conexion.Open();

                    Paso(errores, "indicadores", () => CargarEstados(conexion));
                    Paso(errores, "categorías", () => CargarCategorias(conexion));
                    Paso(errores, "tendencias", () => CargarTendencias(conexion));
                    Paso(errores, "movimientos recientes", () => CargarMovimientos(conexion));
                }
            }
            catch (Exception ex)
            {
                errores.Add("conexión: " + ex.Message);
            }
            finally
            {
                cargando = false;
                if (btnActualizar != null)
                {
                    btnActualizar.Enabled = true;
                    btnActualizar.Text = "↻  Actualizar";
                }
            }

            if (errores.Count > 0 && manual)
            {
                MessageBox.Show(
                    "No se pudo cargar toda la información del panel." + Environment.NewLine + Environment.NewLine +
                    string.Join(Environment.NewLine, errores),
                    "Artemusa Inventario - Base de datos",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private static void Paso(List<string> errores, string etapa, Action accion)
        {
            try { accion(); }
            catch (Exception ex) { errores.Add("Error en " + etapa + ": " + ex.Message); }
        }

        // ── KPI / ESTADOS ──
        private void CargarEstados(NpgsqlConnection con)
        {
            int total = 0, disp = 0, asig = 0, prest = 0, mant = 0;
            using (var cmd = new NpgsqlCommand(
                "SELECT LOWER(TRIM(COALESCE(estado,''))) AS e, COUNT(*)::int AS n FROM public.equipos GROUP BY 1;", con))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    string estado = Convert.ToString(r["e"]);
                    int n = Convert.ToInt32(r["n"]);
                    total += n;
                    if (estado == "disponible") disp += n;
                    else if (estado == "asignado" || estado == "en uso") asig += n;
                    else if (estado == "prestado") prest += n;
                    else if (estado.Contains("mantenimiento")) mant += n;
                }
            }

            kTotal.Valor = total.ToString("N0");
            kDisp.Valor = disp.ToString("N0");
            kAsig.Valor = asig.ToString("N0");
            kMant.Valor = mant.ToString("N0");
            kDev.Valor = prest.ToString("N0");

            // Mini-sparklines estáticos si no hay histórico, para que la tarjeta no se vea vacía
            var serieFicticia = new int[] { total, total, total, total, total, total, total, total, total, total, total, total };
            kTotal.Serie = serieFicticia;

            panelEstados.SetData(total, disp, asig, mant, prest);
            chartCat.Total = total;
        }

        private void CargarCategorias(NpgsqlConnection con)
        {
            var lista = new List<KeyValuePair<string, int>>();
            using (var cmd = new NpgsqlCommand(
                "SELECT COALESCE(NULLIF(TRIM(categoria),''), 'Sin categoría') AS cat, COUNT(*)::int AS n FROM public.equipos GROUP BY 1 ORDER BY 2 DESC;", con))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                    lista.Add(new KeyValuePair<string, int>(Convert.ToString(r["cat"]), Convert.ToInt32(r["n"])));
            }

            // Top 5 + "Otros"
            var final = new List<KeyValuePair<string, int>>();
            for (int i = 0; i < lista.Count; i++)
            {
                if (i < 5) final.Add(lista[i]);
                else if (final.Count == 5) final.Add(new KeyValuePair<string, int>("Otros", lista[i].Value));
                else final[5] = new KeyValuePair<string, int>("Otros", final[5].Value + lista[i].Value);
            }
            chartCat.SetData(final);
        }

        private void CargarTendencias(NpgsqlConnection con)
        {
            DateTime hoy = DateTime.Today;
            DateTime desde = hoy.AddDays(-59);

            var gSalida = new int[60];
            var gDevol = new int[60];
            var gRetr = new int[60];

            // 1. Salidas (desde salidas)
            using (var cmd = new NpgsqlCommand(@"
    SELECT fecha_salida::date AS d, COUNT(*)::int AS n
    FROM public.salidas
    WHERE fecha_salida::date >= @desde
    GROUP BY 1;", con))
            {
                cmd.Parameters.Add("desde", NpgsqlDbType.Date).Value = desde;
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        // --- CAMBIO AQUI ---
                        DateTime d;
                        if (r["d"] is DateOnly dateOnly)
                            d = dateOnly.ToDateTime(TimeOnly.MinValue);
                        else
                            d = Convert.ToDateTime(r["d"]).Date;
                        // -------------------

                        int idx = (d - desde).Days;
                        if (idx >= 0 && idx < 60) gSalida[idx] += Convert.ToInt32(r["n"]);
                    }
                }
            }

            // 2. Devoluciones (desde retornos)
            using (var cmd = new NpgsqlCommand(@"
    SELECT fecha_retorno::date AS d, COUNT(*)::int AS n
    FROM public.retornos
    WHERE fecha_retorno::date >= @desde
    GROUP BY 1;", con))
            {
                cmd.Parameters.Add("desde", NpgsqlDbType.Date).Value = desde;
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        // --- CAMBIO AQUI ---
                        DateTime d;
                        if (r["d"] is DateOnly dateOnly)
                            d = dateOnly.ToDateTime(TimeOnly.MinValue);
                        else
                            d = Convert.ToDateTime(r["d"]).Date;
                        // -------------------

                        int idx = (d - desde).Days;
                        if (idx >= 0 && idx < 60) gDevol[idx] += Convert.ToInt32(r["n"]);
                    }
                }
            }

            // Gráfico de tendencias (30 días)
            var dias = new DateTime[30];
            var prest = new int[30];
            var dev = new int[30];
            var retr = new int[30];
            for (int i = 0; i < 30; i++)
            {
                dias[i] = hoy.AddDays(i - 29);
                prest[i] = gSalida[30 + i];
                dev[i] = gDevol[30 + i];
                retr[i] = gRetr[30 + i]; // Si no tienes tabla de retrasos, queda en 0
            }
            chartTend.SetData(dias, prest, dev, retr);
        }

        private void CargarMovimientos(NpgsqlConnection con)
        {
            // Consulta unificada: Salidas y Retornos reales
            string sql = @"
                SELECT e.codigo AS codigo,
                       e.nombre AS nombre,
                       COALESCE(e.categoria, 'General') AS categoria,
                       COALESCE(e.estado, '') AS estado,
                       '' AS condicion,
                       COALESCE(s.ubicacion, e.ubicacion, '') AS ubicacion,
                       COALESCE(t.nombre, '—') AS responsable,
                       s.fecha_salida AS fecha,
                       'Salida' AS tipo
                FROM public.salida_equipos se
                JOIN public.salidas s ON s.id_salida = se.id_salida
                JOIN public.equipos e ON e.id = se.id_equipo
                LEFT JOIN public.trabajadores t ON t.id_trabajador = s.id_trabajador
                UNION ALL
                SELECT e.codigo,
                       e.nombre,
                       COALESCE(e.categoria, 'General'),
                       COALESCE(e.estado, ''),
                       COALESCE(r.estado_equipo, ''),
                       COALESCE(e.ubicacion, ''),
                       COALESCE(t.nombre, '—'),
                       r.fecha_retorno,
                       'Devolución'
                FROM public.retornos r
                JOIN public.equipos e ON e.id = r.id_equipo
                LEFT JOIN public.trabajadores t ON t.id_trabajador = r.id_trabajador
                ORDER BY fecha DESC
                LIMIT 60;";

            var tabla = new DataTable();
            foreach (string c in new[] { "ID", "Equipo", "Categoría", "Estado", "Condición", "Ubicación", "Responsable", "Fecha Movimiento", "Tipo", "Acciones" })
                tabla.Columns.Add(c, typeof(string));

            var items = new List<ItemMovimiento>();

            using (var cmd = new NpgsqlCommand(sql, con))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    string codigo = Convert.ToString(r["codigo"]);
                    string nombre = Convert.ToString(r["nombre"]);
                    DateTime fecha = r["fecha"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(r["fecha"]);
                    string tipoNorm = NormTipo(Convert.ToString(r["tipo"]));
                    TipoInfo info = InfoTipo(tipoNorm);

                    string estado = Capitalizar(Convert.ToString(r["estado"]));
                    if (estado.Equals("Mantenimiento", StringComparison.OrdinalIgnoreCase))
                        estado = "En mantenimiento";

                    tabla.Rows.Add(
                        codigo,
                        nombre,
                        Convert.ToString(r["categoria"]),
                        estado,
                        Capitalizar(Convert.ToString(r["condicion"])),
                        Convert.ToString(r["ubicacion"]),
                        Convert.ToString(r["responsable"]),
                        fecha == DateTime.MinValue ? "—" : fecha.ToString("dd/MM/yyyy HH:mm"),
                        info.Etiqueta,
                        "");

                    items.Add(new ItemMovimiento { Fecha = fecha, TipoNorm = tipoNorm, Equipo = nombre, Codigo = codigo });
                }
            }

            tablaDatos = tabla;
            dgv.DataSource = tabla.DefaultView;
            ConfigurarColumnas();

            listaMov.SetData(items.Take(5).ToList());
            //panelAlertas.SetData(items.Take(4).ToList());
        }

        private void ConfigurarColumnas()
        {
            var pesos = new Dictionary<string, float>
            {
                { "ID", 55 }, { "Equipo", 130 }, { "Categoría", 85 }, { "Estado", 105 }, { "Condición", 75 },
                { "Ubicación", 110 }, { "Responsable", 100 }, { "Fecha Movimiento", 115 }, { "Tipo", 95 }, { "Acciones",  70 }
            };
            foreach (DataGridViewColumn c in dgv.Columns)
            {
                c.SortMode = DataGridViewColumnSortMode.NotSortable;
                float w;
                if (pesos.TryGetValue(c.Name, out w)) c.FillWeight = w;
            }
            if (dgv.Columns.Contains("Acciones"))
            {
                dgv.Columns["Acciones"].MinimumWidth = 70;
                dgv.Columns["Acciones"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
        }

        // ═════════════════════════════════════════════════════════
        //                       UTILIDADES
        // ═════════════════════════════════════════════════════════
        private sealed class ItemMovimiento
        {
            public DateTime Fecha;
            public string TipoNorm;
            public string Equipo;
            public string Codigo;
        }

        private sealed class TipoInfo
        {
            public string Titulo;
            public string Glyph;
            public string Etiqueta;
            public string Chip;
            public Color Color;
            public Color ChipColor;
        }

        private static string NormTipo(string t)
        {
            t = (t ?? "").Trim().ToUpperInvariant();
            if (t.StartsWith("SAL")) return "SALIDA";
            if (t.StartsWith("ING") || t.StartsWith("ENTR")) return "INGRESO";
            if (t.StartsWith("DEVOL")) return "DEVOLUCION";
            if (t.StartsWith("MANT")) return "MANTENIMIENTO";
            if (t.StartsWith("TRAS")) return "TRASLADO";
            if (t.StartsWith("RETRA")) return "RETRASO";
            return t;
        }

        private static string Capitalizar(string s)
        {
            s = (s ?? "").Trim();
            if (s.Length == 0 || s == "—") return s;
            return char.ToUpper(s[0]) + s.Substring(1).ToLowerInvariant();
        }

        private static TipoInfo InfoTipo(string norm)
        {
            switch (norm)
            {
                case "SALIDA":
                    return new TipoInfo { Titulo = "Salida de equipo", Glyph = "↗", Etiqueta = "Salida", Chip = "Confirmada", Color = CAzul, ChipColor = CVerde };
                case "INGRESO":
                    return new TipoInfo { Titulo = "Entrada de equipo", Glyph = "↙", Etiqueta = "Entrada", Chip = "Nueva", Color = CVerde, ChipColor = CAzul };
                case "DEVOLUCION":
                    return new TipoInfo { Titulo = "Devolución de equipo", Glyph = "↩", Etiqueta = "Devolución", Chip = "Pendiente", Color = CTeal, ChipColor = CAmarillo };
                case "MANTENIMIENTO":
                    return new TipoInfo { Titulo = "Equipo en mantenimiento", Glyph = "⚙", Etiqueta = "Mantenimiento", Chip = "En proceso", Color = CNaranja, ChipColor = CNaranja };
                case "TRASLADO":
                    return new TipoInfo { Titulo = "Traslado de equipo", Glyph = "⇄", Etiqueta = "Traslado", Chip = "Nueva", Color = CMorado, ChipColor = CAzul };
                default:
                    return new TipoInfo { Titulo = "Movimiento de equipo", Glyph = "•", Etiqueta = Capitalizar(norm), Chip = "Nueva", Color = CTenue, ChipColor = CAzul };
            }
        }

        // ── Helpers de dibujo ──
        private static void Calidad(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        }

        private static Color Alpha(Color c, int a)
        {
            return Color.FromArgb(Math.Max(0, Math.Min(255, a)), c.R, c.G, c.B);
        }

        private static Color Aclarar(Color c, int n)
        {
            return Color.FromArgb(c.A, Math.Min(255, c.R + n), Math.Min(255, c.G + n), Math.Min(255, c.B + n));
        }

        private static Color Oscurecer(Color c, int n)
        {
            return Color.FromArgb(c.A, Math.Max(0, c.R - n), Math.Max(0, c.G - n), Math.Max(0, c.B - n));
        }

        private static void Dt(Graphics g, string s, Font f, Color c, float x, float y)
        {
            using (var b = new SolidBrush(c)) g.DrawString(s, f, b, x, y);
        }

        private static void DtCentro(Graphics g, string s, Font f, Color c, RectangleF r)
        {
            using (var b = new SolidBrush(c))
            using (var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap
            })
                g.DrawString(s, f, b, r, sf);
        }

        private static void DtDer(Graphics g, string s, Font f, Color c, RectangleF r)
        {
            using (var b = new SolidBrush(c))
            using (var sf = new StringFormat
            {
                Alignment = StringAlignment.Far,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap
            })
                g.DrawString(s, f, b, r, sf);
        }

        private static void DtIzq(Graphics g, string s, Font f, Color c, RectangleF r)
        {
            using (var b = new SolidBrush(c))
            using (var sf = new StringFormat
            {
                Alignment = StringAlignment.Near,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap
            })
                g.DrawString(s, f, b, r, sf);
        }

        private static void Chip(Graphics g, string texto, Color fondo, Color fg, Rectangle r)
        {
            using (var p = Redondo(r, r.Height / 2))
            using (var b = new SolidBrush(fondo))
                g.FillPath(b, p);
            DtCentro(g, texto, F75B, fg, r);
        }

        private static GraphicsPath Redondo(Rectangle rect, int radio)
        {
            var path = new GraphicsPath();
            if (rect.Width <= 0 || rect.Height <= 0) return path;
            int d = Math.Max(1, Math.Min(radio * 2, Math.Min(rect.Width, rect.Height)));
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static void CurvaConRelleno(Graphics g, PointF[] pts, Color color, float grosor, float baseY, Rectangle zona, int alfa)
        {
            if (pts == null || pts.Length < 2) return;
            using (var path = new GraphicsPath())
            {
                path.AddCurve(pts, 0.35f);
                path.AddLine(pts[pts.Length - 1].X, baseY, pts[0].X, baseY);
                path.CloseFigure();
                if (zona.Width > 0 && zona.Height > 0)
                    using (var br = new LinearGradientBrush(zona, Alpha(color, alfa), Alpha(color, 0), LinearGradientMode.Vertical))
                        g.FillPath(br, path);
            }
            using (var pen = new Pen(color, grosor) { LineJoin = LineJoin.Round })
                g.DrawCurve(pen, pts, 0.35f);
        }

        // ═════════════════════════════════════════════════════════
        //                    CONTROLES PERSONALIZADOS
        // ═════════════════════════════════════════════════════════
        private abstract class Lienzo : Control
        {
            protected Lienzo()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                         ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
                BackColor = CTarjeta;
            }
        }

        private sealed class TarjetaOscura : Lienzo
        {
            private readonly string titulo;
            private readonly string glyph;
            private readonly Color colorGlyph;

            public TarjetaOscura(string titulo, string glyph, Color colorGlyph)
            {
                this.titulo = titulo;
                this.glyph = glyph;
                this.colorGlyph = colorGlyph;
                Dock = DockStyle.Fill;
                BackColor = CFondo;
                Padding = new Padding(14, 48, 14, 12);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                if (Width < 40 || Height < 40) return;
                var g = e.Graphics;
                Calidad(g);

                var r = new Rectangle(0, 0, Width - 1, Height - 1);
                using (var p = Redondo(r, 14))
                {
                    using (var b = new SolidBrush(CTarjeta)) g.FillPath(b, p);
                    using (var pen = new Pen(CLinea, 1)) g.DrawPath(pen, p);
                }

                float x = 16;
                if (!string.IsNullOrEmpty(glyph))
                {
                    Dt(g, glyph, Sym12, colorGlyph, 14, 12);
                    x = 38;
                }
                Dt(g, titulo, F105B, CTexto, x, 14);
            }
        }

        private sealed class BotonFecha : Lienzo
        {
            public BotonFecha()
            {
                BackColor = CFondo;
                Cursor = Cursors.Hand;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                var g = e.Graphics;
                Calidad(g);
                var r = new Rectangle(0, 0, Width - 1, Height - 1);
                using (var p = Redondo(r, 10))
                {
                    using (var b = new SolidBrush(CTarjeta)) g.FillPath(b, p);
                    using (var pen = new Pen(CLinea, 1)) g.DrawPath(pen, p);
                }

                int cx = 14, cy = Height / 2 - 8;
                using (var pen = new Pen(CCian, 1.6f))
                {
                    g.DrawRectangle(pen, cx, cy + 2, 15, 13);
                    g.DrawLine(pen, cx, cy + 6, cx + 15, cy + 6);
                    g.DrawLine(pen, cx + 4, cy, cx + 4, cy + 4);
                    g.DrawLine(pen, cx + 11, cy, cx + 11, cy + 4);
                }

                string hoy = DateTime.Today.ToString("dd/MM/yyyy");
                Dt(g, hoy + " - " + hoy, F9, CTexto, 38, Height / 2 - 9);
            }
        }

        private sealed class TarjetaKpi : Lienzo
        {
            private readonly string titulo;
            private readonly string glyph;
            private readonly Color acento;
            private string valor = "0";
            private double delta;
            private int[] serie = new int[0];

            public string Valor { get { return valor; } set { valor = value; Invalidate(); } }
            public double Delta { get { return delta; } set { delta = value; Invalidate(); } }
            public int[] Serie { get { return serie; } set { serie = value ?? new int[0]; Invalidate(); } }

            public TarjetaKpi(string titulo, string glyph, Color acento)
            {
                this.titulo = titulo;
                this.glyph = glyph;
                this.acento = acento;
                Dock = DockStyle.Fill;
                BackColor = CFondo;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                if (Width < 100 || Height < 80) return;
                var g = e.Graphics;
                Calidad(g);

                var r = new Rectangle(0, 0, Width - 1, Height - 1);
                using (var p = Redondo(r, 14))
                {
                    using (var br = new LinearGradientBrush(r, Aclarar(CTarjeta, 8), CTarjeta, LinearGradientMode.Vertical))
                        g.FillPath(br, p);
                    using (var pen = new Pen(Alpha(acento, 120), 1.3f))
                        g.DrawPath(pen, p);
                }

                var ic = new Rectangle(14, 14, 44, 44);
                using (var p = Redondo(ic, 11))
                using (var br = new LinearGradientBrush(ic, Aclarar(acento, 35), Oscurecer(acento, 45), LinearGradientMode.ForwardDiagonal))
                    g.FillPath(br, p);
                DtCentro(g, glyph, Sym16, Color.White, ic);

                Dt(g, titulo, F85, CTexto, 68, 13);
                Dt(g, valor, F22B, Color.White, 66, 27);

                bool sube = delta >= 0;
                Color cd = sube ? CVerde : CRojo;
                string txt = (sube ? "↑ " : "↓ ") + Math.Abs(delta).ToString("0") + "%";
                Dt(g, txt, F85B, cd, 68, 66);
                float w = g.MeasureString(txt, F85B).Width;
                Dt(g, "vs. mes anterior", F8, CTenue, 68 + w + 4, 67);

                var zona = new Rectangle(14, 88, Width - 29, Math.Max(8, Height - 88 - 10));
                DibujarSpark(g, zona);
            }

            private void DibujarSpark(Graphics g, Rectangle z)
            {
                if (serie == null || serie.Length < 2 || z.Height < 8) return;
                int n = serie.Length;
                int min = serie.Min(), max = serie.Max();
                var pts = new PointF[n];
                for (int i = 0; i < n; i++)
                {
                    float t = max == min ? 0.4f : (serie[i] - min) / (float)(max - min);
                    pts[i] = new PointF(z.Left + i * z.Width / (n - 1f), z.Bottom - t * z.Height);
                }
                var estado = g.Save();
                g.SetClip(new Rectangle(z.Left - 2, z.Top - 2, z.Width + 4, z.Height + 6));
                CurvaConRelleno(g, pts, Aclarar(acento, 15), 2f, z.Bottom + 2, z, 90);
                g.Restore(estado);
            }
        }

        private sealed class SelectorRango : Lienzo
        {
            private readonly ContextMenuStrip menu;
            private int dias = 7;
            public int Dias { get { return dias; } }
            public event EventHandler Cambio;

            public SelectorRango()
            {
                Cursor = Cursors.Hand;
                menu = new ContextMenuStrip
                {
                    BackColor = CTarjeta2,
                    ForeColor = CTexto,
                    ShowImageMargin = false,
                    Font = new Font("Segoe UI", 9F)
                };
                AgregarItem("Últimos 7 días", 7);
                AgregarItem("Últimos 30 días", 30);
            }

            private void AgregarItem(string texto, int d)
            {
                var it = new ToolStripMenuItem(texto) { BackColor = CTarjeta2, ForeColor = CTexto };
                it.Click += (s, e) =>
                {
                    dias = d;
                    Invalidate();
                    var h = Cambio;
                    if (h != null) h(this, EventArgs.Empty);
                };
                menu.Items.Add(it);
            }

            protected override void OnClick(EventArgs e)
            {
                base.OnClick(e);
                menu.Show(this, new Point(0, Height + 2));
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                var g = e.Graphics;
                Calidad(g);
                var r = new Rectangle(0, 0, Width - 1, Height - 1);
                using (var p = Redondo(r, 8))
                {
                    using (var b = new SolidBrush(CTarjeta2)) g.FillPath(b, p);
                    using (var pen = new Pen(CLinea, 1)) g.DrawPath(pen, p);
                }
                DtIzq(g, "Últimos " + dias + " días", F85, CTexto, new RectangleF(12, 0, Width - 30, Height));
                DtCentro(g, "▾", F85, CTenue, new Rectangle(Width - 24, 0, 20, Height));
            }
        }

        private sealed class ToggleSwitch : Lienzo
        {
            public bool Checked { get; set; }

            public ToggleSwitch()
            {
                Size = new Size(42, 22);
                Cursor = Cursors.Hand;
            }

            protected override void OnClick(EventArgs e)
            {
                Checked = !Checked;
                Invalidate();
                base.OnClick(e);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                var g = e.Graphics;
                Calidad(g);
                var r = new Rectangle(0, 0, Width - 1, Height - 1);
                using (var p = Redondo(r, Height / 2))
                using (var b = new SolidBrush(Checked ? CVerde : Color.FromArgb(51, 65, 100)))
                    g.FillPath(b, p);
                int d = Height - 6;
                int x = Checked ? Width - d - 3 : 3;
                g.FillEllipse(Brushes.White, x, 3, d, d);
            }
        }

        private sealed class BotonAccion : Lienzo
        {
            private readonly string texto;
            private readonly string glyph;
            private readonly Color c1;
            private readonly Color c2;
            private bool hover;
            private bool presionado;

            public BotonAccion(string texto, string glyph, Color c1, Color c2)
            {
                this.texto = texto;
                this.glyph = glyph;
                this.c1 = c1;
                this.c2 = c2;
                Dock = DockStyle.Fill;
                Cursor = Cursors.Hand;
            }

            protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
            protected override void OnMouseLeave(EventArgs e) { hover = false; presionado = false; Invalidate(); base.OnMouseLeave(e); }
            protected override void OnMouseDown(MouseEventArgs e) { presionado = true; Invalidate(); base.OnMouseDown(e); }
            protected override void OnMouseUp(MouseEventArgs e) { presionado = false; Invalidate(); base.OnMouseUp(e); }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                if (Width < 60 || Height < 16) return;
                var g = e.Graphics;
                Calidad(g);
                var r = new Rectangle(0, 0, Width - 1, Height - 1);
                int k = presionado ? -12 : (hover ? 14 : 0);
                using (var p = Redondo(r, 9))
                {
                    using (var br = new LinearGradientBrush(r, Aclarar(c1, k), Aclarar(c2, k), LinearGradientMode.Horizontal))
                        g.FillPath(br, p);
                    using (var pen = new Pen(Alpha(Color.White, 28), 1)) g.DrawPath(pen, p);
                }

                int d = Math.Min(22, Height - 8);
                var ic = new Rectangle(10, (Height - d) / 2, d, d);
                using (var b = new SolidBrush(Alpha(Color.White, 45))) g.FillEllipse(b, ic);
                DtCentro(g, glyph, Sym9, Color.White, ic);

                DtIzq(g, texto, F9B, Color.White, new RectangleF(10 + d + 10, 0, Width - d - 30, Height));
            }
        }

        private sealed class GraficoTendencias : Lienzo
        {
            private const int L = 36, R = 14, T = 34, B = 26;
            private DateTime[] dias = new DateTime[0];
            private int[] prest = new int[0];
            private int[] dev = new int[0];
            private int[] retr = new int[0];
            private int rango = 7;
            private int hover = -1;

            public int Rango { get { return rango; } set { rango = value; hover = -1; Invalidate(); } }

            public GraficoTendencias()
            {
                MinimumSize = new Size(220, 140);
            }

            public void SetData(DateTime[] d, int[] p, int[] v, int[] r)
            {
                dias = d ?? new DateTime[0];
                prest = p ?? new int[0];
                dev = v ?? new int[0];
                retr = r ?? new int[0];
                Invalidate();
            }

            private Rectangle Plot
            {
                get { return new Rectangle(L, T, Math.Max(1, Width - L - R), Math.Max(1, Height - T - B)); }
            }

            private int N { get { return Math.Min(rango, dias.Length); } }

            private static int Val(int[] a, int i)
            {
                return i >= 0 && i < a.Length ? Math.Max(0, a[i]) : 0;
            }

            private PointF[] Pts(int[] arr, Rectangle p, int max, int n, int ini)
            {
                var pts = new PointF[n];
                for (int i = 0; i < n; i++)
                    pts[i] = new PointF(p.Left + i * p.Width / (n - 1f), p.Bottom - Val(arr, ini + i) * p.Height / (float)max);
                return pts;
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                base.OnMouseMove(e);
                int n = N;
                var p = Plot;
                int idx = -1;
                if (n >= 2 && e.X >= p.Left - 6 && e.X <= p.Right + 6 && e.Y >= p.Top && e.Y <= p.Bottom)
                {
                    idx = (int)Math.Round((e.X - p.Left) * (n - 1f) / p.Width);
                    idx = Math.Max(0, Math.Min(n - 1, idx));
                }
                if (idx != hover) { hover = idx; Invalidate(); }
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                base.OnMouseLeave(e);
                hover = -1;
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                if (Width < 160 || Height < 110) return;
                var g = e.Graphics;
                Calidad(g);

                int n = N;
                var p = Plot;
                int ini = dias.Length - n;

                int maxDato = 0;
                for (int i = 0; i < n; i++)
                    maxDato = Math.Max(maxDato, Math.Max(Val(prest, ini + i), Math.Max(Val(dev, ini + i), Val(retr, ini + i))));
                int max = maxDato <= 4 ? 4 : (int)(Math.Ceiling(maxDato * 1.15 / 4.0) * 4);

                using (var pen = new Pen(Color.FromArgb(26, 44, 80), 1) { DashStyle = DashStyle.Dash })
                {
                    for (int i = 0; i <= 4; i++)
                    {
                        float y = p.Bottom - i * p.Height / 4f;
                        g.DrawLine(pen, p.Left, y, p.Right, y);
                        string lbl = (max * i / 4).ToString(CultureInfo.InvariantCulture);
                        DtDer(g, lbl, F8, CTenue, new RectangleF(0, y - 8, L - 6, 16));
                    }
                }

                int lx = L;
                Leyenda(g, "Préstamos", CAzul, false, ref lx);
                Leyenda(g, "Devoluciones", CNaranja, false, ref lx);
                Leyenda(g, "Retrasos", CRojo, true, ref lx);

                if (n < 2)
                {
                    DtCentro(g, "Sin datos para mostrar", F9, CTenue, p);
                    return;
                }

                int paso = n <= 7 ? 1 : 5;
                for (int i = 0; i < n; i += paso)
                {
                    float x = p.Left + i * p.Width / (n - 1f);
                    string d = dias[ini + i].ToString("dd/MM");
                    DtCentro(g, d, F8, i == hover ? CTexto : CTenue, new RectangleF(x - 24, p.Bottom + 6, 48, 16));
                }

                var pPrest = Pts(prest, p, max, n, ini);
                var pDev = Pts(dev, p, max, n, ini);
                var pRetr = Pts(retr, p, max, n, ini);

                if (hover >= 0)
                {
                    using (var pen = new Pen(Alpha(CTenue, 120), 1) { DashStyle = DashStyle.Dash })
                        g.DrawLine(pen, pPrest[hover].X, p.Top, pPrest[hover].X, p.Bottom);
                }

                var estado = g.Save();
                g.SetClip(new Rectangle(0, p.Top - 8, Width, p.Height + 16));
                CurvaConRelleno(g, pPrest, CAzul, 2.6f, p.Bottom, p, 110);
                CurvaConRelleno(g, pDev, CNaranja, 2.6f, p.Bottom, p, 90);
                using (var pen = new Pen(CRojo, 2f) { DashStyle = DashStyle.Dash })
                    g.DrawCurve(pen, pRetr, 0.35f);
                g.Restore(estado);

                if (hover >= 0)
                {
                    Marcador(g, pPrest[hover], CAzul);
                    Marcador(g, pDev[hover], CNaranja);
                    Marcador(g, pRetr[hover], CRojo);
                    Tooltip(g, pPrest[hover].X, p, ini + hover);
                }
            }

            private void Leyenda(Graphics g, string txt, Color col, bool punteada, ref int x)
            {
                using (var pen = new Pen(col, 2.2f))
                {
                    if (punteada) pen.DashStyle = DashStyle.Dash;
                    g.DrawLine(pen, x, 14, x + 18, 14);
                }
                using (var b = new SolidBrush(col)) g.FillEllipse(b, x + 5, 10, 8, 8);
                Dt(g, txt, F8, CTexto, x + 22, 7);
                x += 22 + (int)g.MeasureString(txt, F8).Width + 12;
            }

            private static void Marcador(Graphics g, PointF pt, Color col)
            {
                g.FillEllipse(Brushes.White, pt.X - 5, pt.Y - 5, 10, 10);
                using (var pen = new Pen(col, 2.2f)) g.DrawEllipse(pen, pt.X - 5, pt.Y - 5, 10, 10);
            }

            private void Tooltip(Graphics g, float ancla, Rectangle p, int idx)
            {
                int w = 138, h = 78;
                float x = ancla + 14;
                if (x + w > Width - 4) x = ancla - w - 14;
                var box = new Rectangle((int)x, p.Top + 2, w, h);
                using (var path = Redondo(box, 8))
                {
                    using (var b = new SolidBrush(Color.FromArgb(240, 8, 16, 34))) g.FillPath(b, path);
                    using (var pen = new Pen(CLinea, 1)) g.DrawPath(pen, path);
                }
                Dt(g, dias[idx].ToString("dd/MM/yyyy"), F85B, CTexto, box.X + 10, box.Y + 7);
                Fila(g, box, 0, "Préstamos", Val(prest, idx), CAzul);
                Fila(g, box, 1, "Devoluciones", Val(dev, idx), CNaranja);
                Fila(g, box, 2, "Retrasos", Val(retr, idx), CRojo);
            }

            private static void Fila(Graphics g, Rectangle box, int i, string txt, int val, Color col)
            {
                int y = box.Y + 26 + i * 16;
                using (var b = new SolidBrush(col)) g.FillEllipse(b, box.X + 10, y + 4, 8, 8);
                Dt(g, txt + ": " + val, F8, CTexto, box.X + 24, y);
            }
        }

        private sealed class GraficoCategorias : Lienzo
        {
            private static readonly Color[] Paleta =
            {
                Color.FromArgb(59, 130, 246),
                Color.FromArgb(34, 197, 94),
                Color.FromArgb(251, 146, 60),
                Color.FromArgb(168, 85, 247),
                Color.FromArgb(34, 211, 238),
                Color.FromArgb(250, 204, 21)
            };

            private List<KeyValuePair<string, int>> datos = new List<KeyValuePair<string, int>>();
            public int Total { get; set; }

            public GraficoCategorias()
            {
                MinimumSize = new Size(200, 140);
            }

            public void SetData(List<KeyValuePair<string, int>> d)
            {
                datos = d ?? new List<KeyValuePair<string, int>>();
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                if (Width < 150 || Height < 100) return;
                var g = e.Graphics;
                Calidad(g);

                int suma = datos.Sum(k => k.Value);
                int total = suma;

                int size = Math.Min(Height - 8, Math.Min(170, (int)(Width * 0.52)));
                size = Math.Max(70, size);
                int x0 = 2, y0 = (Height - size) / 2;
                int grosor = Math.Max(14, (int)(size / 4.6));
                var rect = new Rectangle(x0 + grosor / 2, y0 + grosor / 2, size - grosor, size - grosor);

                using (var pen = new Pen(Color.FromArgb(18, 32, 62), grosor))
                    g.DrawEllipse(pen, rect);

                if (suma > 0)
                {
                    int seg = datos.Count(k => k.Value > 0);
                    float hueco = seg > 1 ? 2.5f : 0f;
                    float ini = -90f;
                    for (int i = 0; i < datos.Count; i++)
                    {
                        if (datos[i].Value <= 0) continue;
                        float barrido = datos[i].Value * 360f / suma;
                        using (var pen = new Pen(Paleta[i % Paleta.Length], grosor))
                        {
                            pen.StartCap = LineCap.Flat;
                            pen.EndCap = LineCap.Flat;
                            g.DrawArc(pen, rect, ini + hueco / 2f, Math.Max(0.5f, barrido - hueco));
                        }
                        ini += barrido;
                    }
                }

                using (var fNum = new Font("Segoe UI", Math.Max(14F, size / 6F), FontStyle.Bold))
                {
                    DtCentro(g, total.ToString("N0"), fNum, Color.White,
                        new RectangleF(x0, y0 + size * 0.28f, size, size * 0.25f));
                    DtCentro(g, "Total Equipos", F8, CTenue,
                        new RectangleF(x0, y0 + size * 0.52f, size, size * 0.16f));
                }

                int lx = x0 + size + 14;
                int filas = Math.Max(1, datos.Count);
                int altoFila = Math.Min(28, (Height - 8) / filas);
                int ly = (Height - altoFila * filas) / 2;
                for (int i = 0; i < datos.Count; i++)
                {
                    int fy = ly + i * altoFila;
                    using (var b = new SolidBrush(Paleta[i % Paleta.Length]))
                        g.FillEllipse(b, lx, fy + altoFila / 2 - 4, 9, 9);
                    DtIzq(g, datos[i].Key, F85, CTexto, new RectangleF(lx + 15, fy, Width - lx - 62, altoFila));
                    string pct = (suma == 0 ? 0 : (int)Math.Round(datos[i].Value * 100.0 / suma)) + "%";
                    DtDer(g, pct, F85B, CTexto, new RectangleF(Width - 50, fy, 44, altoFila));
                }
            }
        }

        private sealed class PanelAlertas : Lienzo
        {
            private List<ItemMovimiento> items = new List<ItemMovimiento>();
            private readonly ToggleSwitch swRetraso;
            private Rectangle zonaEnlace = Rectangle.Empty;

            public event EventHandler VerTodas;

            public PanelAlertas()
            {
                swRetraso = new ToggleSwitch { Checked = false };
                Controls.Add(swRetraso);
            }

            public void SetData(List<ItemMovimiento> lista)
            {
                items = lista ?? new List<ItemMovimiento>();
                Invalidate();
            }

            protected override void OnLayout(LayoutEventArgs levent)
            {
                base.OnLayout(levent);
                swRetraso.Location = new Point(Width - swRetraso.Width - 4, 5);
            }

            protected override void OnMouseClick(MouseEventArgs e)
            {
                base.OnMouseClick(e);
                if (zonaEnlace.Contains(e.Location))
                {
                    var h = VerTodas;
                    if (h != null) h(this, EventArgs.Empty);
                }
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                base.OnMouseMove(e);
                Cursor = zonaEnlace.Contains(e.Location) ? Cursors.Hand : Cursors.Default;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                if (Width < 150 || Height < 100) return;
                var g = e.Graphics;
                Calidad(g);

                DtIzq(g, "Activar Notificaciones de Retraso", F9, CTexto, new RectangleF(2, 0, Width - 60, 32));

                int top = 38;
                int pie = 24;
                int n = Math.Max(1, Math.Min(4, items.Count));
                int altoFila = Math.Max(30, Math.Min(52, (Height - top - pie) / 4));

                if (items.Count == 0)
                    DtCentro(g, "Sin notificaciones recientes", F85, CTenue, new RectangleF(0, top, Width, 60));

                for (int i = 0; i < items.Count && i < 4; i++)
                {
                    var it = items[i];
                    TipoInfo info = InfoTipo(it.TipoNorm);
                    var fila = new Rectangle(0, top + i * altoFila, Width - 1, altoFila - 5);

                    using (var p = Redondo(fila, 8))
                    {
                        using (var b = new SolidBrush(Color.FromArgb(13, 26, 50))) g.FillPath(b, p);
                        using (var pen = new Pen(Color.FromArgb(22, 40, 74), 1)) g.DrawPath(pen, p);
                    }

                    int d = Math.Min(28, fila.Height - 8);
                    var ic = new Rectangle(fila.X + 8, fila.Y + (fila.Height - d) / 2, d, d);
                    using (var b = new SolidBrush(Color.FromArgb(37, 211, 102))) g.FillEllipse(b, ic);
                    DtCentro(g, "✆", Sym9, Color.White, ic);

                    int tx = ic.Right + 8;
                    int anchoChip = 64;
                    float anchoTxt = Width - tx - anchoChip - 12;

                    DtIzq(g, info.Titulo, F85B, CTexto, new RectangleF(tx, fila.Y + 2, anchoTxt, fila.Height / 2f));
                    DtIzq(g, it.Equipo, F8, CTenue, new RectangleF(tx, fila.Y + fila.Height / 2f - 2, anchoTxt, fila.Height / 2f));

                    string hora = it.Fecha == DateTime.MinValue ? "" : it.Fecha.ToString("HH:mm");
                    DtDer(g, hora, F8, CTenue, new RectangleF(Width - anchoChip - 8, fila.Y + 2, anchoChip, 14));
                    var chip = new Rectangle(Width - anchoChip - 8, fila.Y + fila.Height / 2 + 1, anchoChip, 16);
                    Chip(g, info.Chip, info.ChipColor, info.ChipColor == CAmarillo ? Color.FromArgb(40, 30, 0) : Color.White, chip);
                }

                string enlace = "Ver todas las notificaciones  →";
                zonaEnlace = new Rectangle(0, Height - pie, Width, pie);
                DtCentro(g, enlace, F85, CCian, zonaEnlace);
            }
        }

        private sealed class PanelEstados : Lienzo
        {
            private int total, almacen, asignados, mant, prestamo;

            private static readonly string[] Etiquetas = { "En Almacén", "Asignados", "En Mantenimiento", "En Préstamo" };
            private static readonly string[] Glifos = { "⌂", "☻", "⚙", "⇄" };
            private static readonly Color[] Colores =
            {
                Color.FromArgb(34, 197, 94),
                Color.FromArgb(56, 189, 248),
                Color.FromArgb(245, 158, 11),
                Color.FromArgb(168, 85, 247)
            };

            public PanelEstados()
            {
                MinimumSize = new Size(260, 110);
            }

            public void SetData(int total, int almacen, int asignados, int mant, int prestamo)
            {
                this.total = total;
                this.almacen = almacen;
                this.asignados = asignados;
                this.mant = mant;
                this.prestamo = prestamo;
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                if (Width < 200 || Height < 90) return;
                var g = e.Graphics;
                Calidad(g);

                int[] vals = { almacen, asignados, mant, prestamo };
                int colW = Width / 4;
                int size = Math.Max(44, Math.Min(Math.Min(colW - 20, Height - 62), 92));
                int grosor = Math.Max(7, size / 8);

                for (int i = 0; i < 4; i++)
                {
                    int cx = i * colW + colW / 2;
                    int x0 = cx - size / 2;
                    int y0 = 0;
                    var rect = new Rectangle(x0 + grosor / 2, y0 + grosor / 2, size - grosor, size - grosor);

                    using (var pen = new Pen(Color.FromArgb(24, 40, 72), grosor))
                        g.DrawEllipse(pen, rect);

                    double frac = total <= 0 ? 0 : vals[i] / (double)total;
                    if (frac > 0)
                    {
                        using (var pen = new Pen(Colores[i], grosor))
                        {
                            pen.StartCap = LineCap.Round;
                            pen.EndCap = LineCap.Round;
                            g.DrawArc(pen, rect, -90f, (float)Math.Max(4, frac * 360));
                        }
                    }

                    using (var f = new Font("Segoe UI", Math.Max(9F, size / 6.5F), FontStyle.Bold))
                        DtCentro(g, (int)Math.Round(frac * 100) + "%", f, Color.White,
                            new RectangleF(x0, y0, size, size));

                    float ty = y0 + size + 3;
                    DtCentro(g, Glifos[i], Sym11, Colores[i], new RectangleF(i * colW, ty, colW, 18));
                    DtCentro(g, Etiquetas[i], F85B, CTexto, new RectangleF(i * colW, ty + 18, colW, 16));
                    DtCentro(g, vals[i] + " equipos", F8, CTenue, new RectangleF(i * colW, ty + 33, colW, 15));
                }
            }
        }

        private sealed class ListaMovimientos : Lienzo
        {
            private List<ItemMovimiento> items = new List<ItemMovimiento>();

            public ListaMovimientos()
            {
                MinimumSize = new Size(260, 100);
            }

            public void SetData(List<ItemMovimiento> lista)
            {
                items = lista ?? new List<ItemMovimiento>();
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                if (Width < 200 || Height < 60) return;
                var g = e.Graphics;
                Calidad(g);

                if (items.Count == 0)
                {
                    DtCentro(g, "Todavía no hay movimientos registrados.", F9, CTenue, new RectangleF(0, 0, Width, Height));
                    return;
                }

                int filas = 5;
                int alto = Math.Max(30, Height / filas);
                bool mostrarFecha = Width >= 430;

                for (int i = 0; i < items.Count && i < filas; i++)
                {
                    var it = items[i];
                    TipoInfo info = InfoTipo(it.TipoNorm);
                    int y = i * alto;

                    if (i > 0)
                        using (var pen = new Pen(Color.FromArgb(20, 36, 68), 1))
                            g.DrawLine(pen, 0, y, Width, y);

                    int d = Math.Min(30, alto - 6);
                    var ic = new Rectangle(2, y + (alto - d) / 2, d, d);
                    using (var p = Redondo(ic, 8))
                    using (var b = new SolidBrush(Alpha(info.Color, 45)))
                        g.FillPath(b, p);
                    DtCentro(g, info.Glyph, Sym11, info.Color, ic);

                    int tx = ic.Right + 10;
                    int derecha = 84 + (mostrarFecha ? 112 : 0);
                    float anchoTxt = Width - tx - derecha;

                    DtIzq(g, info.Titulo, F85B, CTexto, new RectangleF(tx, y + 1, anchoTxt, alto / 2f));
                    DtIzq(g, it.Equipo + "  ·  " + it.Codigo, F8, CTenue, new RectangleF(tx, y + alto / 2f - 1, anchoTxt, alto / 2f));

                    if (mostrarFecha && it.Fecha != DateTime.MinValue)
                        DtDer(g, it.Fecha.ToString("dd/MM/yyyy HH:mm"), F8, CTenue, new RectangleF(Width - 84 - 110, y, 106, alto));

                    var chip = new Rectangle(Width - 80, y + (alto - 20) / 2, 78, 20);
                    Chip(g, info.Etiqueta, info.Color, Color.White, chip);
                }
            }
        }
    }
}