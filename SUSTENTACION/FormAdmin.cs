#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using Npgsql;
using SUSTENTACION.PanelAdministrador;
using SUSTENTACION.Temas;

namespace SUSTENTACION
{
    public partial class FormAdmin : Form
    {
        public bool SesionCerrada { get; private set; } = false;
        // =========================================================
        // PANELES PRINCIPALES
        // =========================================================
        private BarraLateral barra = null!;
        private Panel panelRightSidebar = null!;
        private Panel panelRightInner = null!;
        private Panel panelHeader = null!;
        private Panel panelMainContent = null!;

        private Panel cardUser = null!;
        private Button btnCerrarSesion = null!;
        private Button btnResumen = null!;
        private Label lblBreadcrumb = null!;
        private Label lblHeaderTitle = null!;

        private Label? lblDrawerMov;
        private Label? lblDrawerDisp;
        private Label? lblDrawerMant;
        private Label? lblDrawerAsig;

        private PanelResumen? panelResumenRef = null;

        // =========================================================
        // ANIMACIONES
        // =========================================================
        private const int DrawerAncho = 300;
        private const int DuracionSidebarMs = 280;
        private const int DuracionDrawerMs = 260;

        private sealed class Animacion
        {
            public bool Activa;
            public float Desde;
            public float Hasta;
            public long Inicio;
            public int Duracion;
        }

        private readonly Animacion animSidebar = new Animacion();
        private readonly Animacion animDrawer = new Animacion();
        private readonly Stopwatch reloj = Stopwatch.StartNew();
        private System.Windows.Forms.Timer animTimer = null!;
        private bool sidebarExpandido = true;
        private bool drawerAbierto = false;

        private string nombreUsuario;
        private string rolUsuario;

        public FormAdmin(string nombre, string rol)
        {
            InitializeComponent();
            nombreUsuario = string.IsNullOrWhiteSpace(nombre) ? "Juan Pérez" : nombre;
            rolUsuario = string.IsNullOrWhiteSpace(rol) ? "Administrador" : rol;

            string rutaIcono = System.IO.Path.Combine(
                AppContext.BaseDirectory,
                "logo_artemusa.ico"
            );

            if (System.IO.File.Exists(rutaIcono))
            {
                this.Icon = new Icon(rutaIcono);
            }

            this.ShowIcon = true;

            ConstruirInterfaz();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED
                return cp;
            }
        }

        private void ConstruirInterfaz()
        {
            this.Size = new Size(1450, 860);
            this.MinimumSize = new Size(1280, 760);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Artemusa Inventario - Panel de Administración";
            this.BackColor = TemaApp.BgDark;
            this.DoubleBuffered = true;
            this.Padding = new Padding(0);
            this.KeyPreview = true;

            ConfigurarAnimaciones();

            CrearMainContent();
            CrearRightSidebar();
            CrearHeader();
            CrearSidebar();

            MostrarOverview();

            this.Shown += (s, e) =>
            {
                if (this.ClientSize.Width < 1420)
                {
                    sidebarExpandido = false;
                    barra.Expandido = false;
                    barra.Width = BarraLateral.AnchoColapsado;
                }
            };
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.B))
            {
                AlternarSidebar();
                return true;
            }

            if (keyData == Keys.Escape && drawerAbierto)
            {
                CerrarDrawer(true);
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        // =========================================================
        // MOTOR DE ANIMACIÓN
        // =========================================================
        private static float EaseInOutCubic(float p) => p < 0.5f ? 4f * p * p * p : 1f - (float)Math.Pow(-2f * p + 2f, 3) / 2f;
        private static float EaseOutCubic(float p) => 1f - (float)Math.Pow(1f - p, 3);

        private void ConfigurarAnimaciones()
        {
            animTimer = new System.Windows.Forms.Timer { Interval = 15 };
            animTimer.Tick += (s, e) => AvanzarAnimaciones();
        }

        private void IniciarAnimacion(Animacion a, float desde, float hasta, int duracion)
        {
            a.Desde = desde;
            a.Hasta = hasta;
            a.Duracion = duracion;
            a.Inicio = reloj.ElapsedMilliseconds;
            a.Activa = true;
            if (!animTimer.Enabled) animTimer.Start();
        }

        private void AvanzarAnimaciones()
        {
            long ahora = reloj.ElapsedMilliseconds;
            bool alguna = false;

            if (animSidebar.Activa)
            {
                float p = Math.Min(1f, (ahora - animSidebar.Inicio) / (float)animSidebar.Duracion);
                float e = EaseInOutCubic(p);
                int w = (int)Math.Round(animSidebar.Desde + (animSidebar.Hasta - animSidebar.Desde) * e);
                if (barra.Width != w) barra.Width = w;

                if (p >= 1f)
                {
                    animSidebar.Activa = false;
                    barra.Width = (int)animSidebar.Hasta;
                }
                else alguna = true;
            }

            if (animDrawer.Activa)
            {
                float p = Math.Min(1f, (ahora - animDrawer.Inicio) / (float)animDrawer.Duracion);
                float e = EaseOutCubic(p);
                int w = (int)Math.Round(animDrawer.Desde + (animDrawer.Hasta - animDrawer.Desde) * e);
                if (panelRightSidebar.Width != w) panelRightSidebar.Width = w;

                if (p >= 1f)
                {
                    animDrawer.Activa = false;
                    panelRightSidebar.Width = (int)animDrawer.Hasta;
                    if (animDrawer.Hasta <= 0f) panelRightSidebar.Visible = false;
                }
                else alguna = true;
            }

            if (!alguna) animTimer.Stop();
        }

        // =========================================================
        // MENÚ LATERAL Y NAVEGACIÓN
        // =========================================================
        private void CrearSidebar()
        {
            barra = new BarraLateral
            {
                Dock = DockStyle.Left,
                Width = BarraLateral.AnchoExpandido
            };

            barra.AgregarItem("Panel", "◉", false);
            barra.AgregarItem("Inventario", "▣", false);
            barra.AgregarItem("Movimientos", "⇄", false);
            barra.AgregarItem("Trabajadores", "👥", true);
            barra.AgregarItem("Reportes", "▧", false);

            barra.Expandido = true;
            barra.Oscuro = TemaApp.EsModoOscuro;

            barra.ToggleClick += (s, e) => AlternarSidebar();
            barra.TemaClick += (s, e) => CambiarTema();
            barra.ItemClick += indice =>
            {
                switch (indice)
                {
                    case 0: MostrarOverview(); break;
                    case 1: MostrarInventario(); break;
                    case 2: MostrarMovimientos(); break;
                    case 3: MostrarTrabajadores(); break;
                    case 4: MostrarReportes(); break;
                }
            };

            this.Controls.Add(barra);
        }

        private void AlternarSidebar()
        {
            sidebarExpandido = !sidebarExpandido;
            barra.Expandido = sidebarExpandido;

            int destino = sidebarExpandido ? BarraLateral.AnchoExpandido : BarraLateral.AnchoColapsado;
            IniciarAnimacion(animSidebar, barra.Width, destino, DuracionSidebarMs);
        }

        // =========================================================
        // CAMBIO DE TEMA
        // =========================================================
        private void CambiarTema()
        {
            // 1. Cambiar el estado global (esto avisa al panel de Trabajadores y demás)
            TemaApp.EsModoOscuro = !TemaApp.EsModoOscuro;
            bool modoOscuro = TemaApp.EsModoOscuro;

            // 2. Actualizar solo el fondo del contenido principal
            // (Se comenta/elimina lo del Header, Sidebar y Barra para que se queden oscuros)
            // this.BackColor = modoOscuro ? TemaApp.BgDark : TemaApp.LightCanvas;
            // panelHeader.BackColor = modoOscuro ? TemaApp.BgHeader : TemaApp.LightHeader;
            // panelRightSidebar.BackColor = modoOscuro ? TemaApp.BgSidebar : TemaApp.LightSidebar;
            // panelRightInner.BackColor = modoOscuro ? TemaApp.BgSidebar : TemaApp.LightSidebar;
            // barra.Oscuro = modoOscuro;

            panelMainContent.BackColor = modoOscuro ? TemaApp.BgDark : TemaApp.LightCanvas;

            // 3. Actualizar otros estilos (grillas del FormAdmin, botón de resumen, etc)
            // Nota: Si AplicarTemaRecursivo(this) te cambia el header o el menú a blanco, 
            // bórralo o pásale solo el panelMainContent.
            AplicarTemaRecursivo(panelMainContent);

            ActualizarEstilosGrids(this);
            ActualizarBotonResumen();

            if (panelResumenRef != null)
            {
                panelResumenRef.BackColor = TemaApp.DashFondo;
                panelResumenRef.Invalidate(true);
            }

            // 4. Refrescar todo el formulario para aplicar los cambios
            panelMainContent.Invalidate(true);
            this.Invalidate(true);
        }

        private void AplicarTemaRecursivo(Control control)
        {
            if (control is BarraLateral || control is PanelResumen)
                return;

            if (control != btnCerrarSesion)
            {
                control.BackColor = MapearColorFondo(control.BackColor);
                control.ForeColor = MapearColorTexto(control.ForeColor);

                if (control is Button button)
                {
                    button.FlatAppearance.MouseOverBackColor = TemaApp.EsModoOscuro ? TemaApp.BgCardHover : TemaApp.LightCardHover;
                }

                if (control is DataGridView grid)
                {
                    AplicarEstiloGrid(grid);
                }
            }

            foreach (Control hijo in control.Controls)
            {
                AplicarTemaRecursivo(hijo);
            }
        }

        private Color MapearColorFondo(Color color)
        {
            int valor = color.ToArgb();
            bool modoOscuro = TemaApp.EsModoOscuro;

            if (modoOscuro)
            {
                if (valor == TemaApp.LightCanvas.ToArgb() || valor == Color.FromArgb(245, 247, 249).ToArgb() || valor == Color.FromArgb(242, 246, 249).ToArgb()) return TemaApp.BgDark;
                if (valor == TemaApp.LightSidebar.ToArgb()) return TemaApp.BgSidebar;
                if (valor == TemaApp.LightCard.ToArgb() || valor == Color.White.ToArgb() || valor == Color.FromArgb(250, 252, 253).ToArgb()) return TemaApp.BgCard;
                if (valor == TemaApp.LightInput.ToArgb() || valor == Color.FromArgb(235, 241, 245).ToArgb()) return TemaApp.BgInput;
                if (valor == TemaApp.LightCardHover.ToArgb()) return TemaApp.BgCardHover;
                if (valor == TemaApp.LightBorder.ToArgb()) return TemaApp.BorderColor;
            }
            else
            {
                if (valor == TemaApp.BgDark.ToArgb()) return TemaApp.LightCanvas;
                if (valor == TemaApp.BgSidebar.ToArgb()) return TemaApp.LightSidebar;
                if (valor == TemaApp.BgHeader.ToArgb() || valor == TemaApp.BgCard.ToArgb()) return TemaApp.LightCard;
                if (valor == TemaApp.BgInput.ToArgb()) return TemaApp.LightInput;
                if (valor == TemaApp.BgCardHover.ToArgb()) return TemaApp.LightCardHover;
                if (valor == TemaApp.BorderColor.ToArgb()) return TemaApp.LightBorder;
                if (valor == Color.FromArgb(25, 31, 39).ToArgb() || valor == Color.FromArgb(32, 42, 53).ToArgb()) return TemaApp.LightInput;
            }

            return color;
        }

        private Color MapearColorTexto(Color color)
        {
            int valor = color.ToArgb();
            bool modoOscuro = TemaApp.EsModoOscuro;

            if (modoOscuro)
            {
                if (valor == TemaApp.LightText.ToArgb()) return TemaApp.TextWhite;
                if (valor == TemaApp.LightSoft.ToArgb()) return TemaApp.TextSoft;
                if (valor == TemaApp.LightGray.ToArgb()) return TemaApp.TextGray;
            }
            else
            {
                if (valor == TemaApp.TextWhite.ToArgb() || valor == Color.White.ToArgb()) return TemaApp.LightText;
                if (valor == TemaApp.TextSoft.ToArgb()) return TemaApp.LightSoft;
                if (valor == TemaApp.TextGray.ToArgb()) return TemaApp.LightGray;
            }

            return color;
        }

        private void ActualizarEstilosGrids(Control raiz)
        {
            if (raiz is PanelResumen) return;

            if (raiz is DataGridView grid)
            {
                AplicarEstiloGrid(grid);
            }

            foreach (Control hijo in raiz.Controls)
            {
                ActualizarEstilosGrids(hijo);
            }
        }

        private void AplicarEstiloGrid(DataGridView grid)
        {
            bool modoOscuro = TemaApp.EsModoOscuro;
            Color fondo = modoOscuro ? TemaApp.BgCard : TemaApp.LightCard;
            Color fondoEntrada = modoOscuro ? TemaApp.BgInput : TemaApp.LightInput;
            Color fondoSeleccion = modoOscuro ? Color.FromArgb(32, 42, 53) : Color.FromArgb(221, 240, 228);
            Color fondoAlterno = modoOscuro ? Color.FromArgb(25, 31, 39) : Color.FromArgb(247, 250, 252);
            Color texto = modoOscuro ? TemaApp.TextWhite : TemaApp.LightText;

            grid.BackgroundColor = fondo;
            grid.GridColor = modoOscuro ? TemaApp.BorderColor : TemaApp.LightBorder;
            grid.ColumnHeadersDefaultCellStyle.BackColor = fondoEntrada;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = TemaApp.AccentGreen;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = fondoEntrada;
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = TemaApp.AccentGreen;
            grid.DefaultCellStyle.BackColor = fondo;
            grid.DefaultCellStyle.ForeColor = texto;
            grid.DefaultCellStyle.SelectionBackColor = fondoSeleccion;
            grid.DefaultCellStyle.SelectionForeColor = modoOscuro ? TemaApp.AccentGreen : Color.FromArgb(22, 101, 52);
            grid.AlternatingRowsDefaultCellStyle.BackColor = fondoAlterno;
            grid.AlternatingRowsDefaultCellStyle.ForeColor = texto;
        }

        // =========================================================
        // HEADER Y PANELES
        // =========================================================
        private void CrearHeader()
        {
            panelHeader = new Panel
            {
                Height = 82,
                Dock = DockStyle.Top,
                BackColor = TemaApp.BgHeader,
                Padding = new Padding(24, 12, 24, 12)
            };
            panelHeader.Paint += (s, e) =>
            {
                using (var pen = new Pen(TemaApp.EsModoOscuro ? TemaApp.BorderColor : TemaApp.LightBorder, 1))
                    e.Graphics.DrawLine(pen, 0, panelHeader.Height - 1, panelHeader.Width, panelHeader.Height - 1);
            };
            this.Controls.Add(panelHeader);

            lblBreadcrumb = new Label
            {
                Text = "Inicio / Panel",
                Font = new Font("Segoe UI", 8.8f, FontStyle.Regular),
                ForeColor = TemaApp.TextGray,
                Location = new Point(24, 12),
                AutoSize = true
            };
            panelHeader.Controls.Add(lblBreadcrumb);

            lblHeaderTitle = new Label
            {
                Text = "Panel General",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = TemaApp.TextWhite,
                Location = new Point(22, 30),
                AutoSize = true
            };
            panelHeader.Controls.Add(lblHeaderTitle);

            btnResumen = new Button
            {
                Text = "▤  Resumen",
                Size = new Size(124, 42),
                BackColor = TemaApp.BgCard,
                ForeColor = TemaApp.TextSoft,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                TabStop = false
            };
            btnResumen.FlatAppearance.BorderSize = 1;
            btnResumen.FlatAppearance.BorderColor = TemaApp.BorderColor;
            btnResumen.FlatAppearance.MouseOverBackColor = TemaApp.BgCardHover;
            AplicarBordesRedondeados(btnResumen, 14);
            btnResumen.Click += (s, e) => AlternarDrawer();
            panelHeader.Controls.Add(btnResumen);

            cardUser = new Panel
            {
                Size = new Size(226, 50),
                BackColor = TemaApp.BgCard
            };
            AplicarBordesRedondeados(cardUser, 16);

            string iniciales = ObtenerIniciales(nombreUsuario);
            cardUser.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                var av = new Rectangle(9, 8, 34, 34);
                using (var br = new LinearGradientBrush(av, TemaApp.AccentGreen, TemaApp.AccentGreenSoft, LinearGradientMode.ForwardDiagonal))
                    g.FillEllipse(br, av);
                using (var f = new Font("Segoe UI", 9f, FontStyle.Bold))
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    g.DrawString(iniciales, f, Brushes.Black, av, sf);
            };

            cardUser.Controls.Add(new Label
            {
                Text = nombreUsuario,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = TemaApp.TextWhite,
                Location = new Point(52, 8),
                Size = new Size(165, 18),
                AutoEllipsis = true,
                BackColor = Color.Transparent
            });
            cardUser.Controls.Add(new Label
            {
                Text = rolUsuario,
                Font = new Font("Segoe UI", 8f),
                ForeColor = TemaApp.TextGray,
                Location = new Point(52, 26),
                Size = new Size(165, 16),
                AutoEllipsis = true,
                BackColor = Color.Transparent
            });
            panelHeader.Controls.Add(cardUser);

            btnCerrarSesion = new Button
            {
                Text = "Cerrar Sesión",
                Size = new Size(120, 42),
                BackColor = Color.FromArgb(239, 68, 68),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCerrarSesion.FlatAppearance.BorderSize = 0;
            AplicarBordesRedondeados(btnCerrarSesion, 14);
            btnCerrarSesion.Click += BtnCerrarSesion_Click;
            btnCerrarSesion.MouseEnter += (s, e) => btnCerrarSesion.BackColor = Color.FromArgb(220, 38, 38);
            btnCerrarSesion.MouseLeave += (s, e) => btnCerrarSesion.BackColor = Color.FromArgb(239, 68, 68);
            panelHeader.Controls.Add(btnCerrarSesion);

            panelHeader.Resize += (s, e) => ReubicarHeader();
            ReubicarHeader();
        }

        private void ReubicarHeader()
        {
            int ancho = panelHeader.ClientSize.Width;
            btnCerrarSesion.Location = new Point(ancho - 24 - btnCerrarSesion.Width, 20);
            cardUser.Location = new Point(btnCerrarSesion.Left - 12 - cardUser.Width, 16);
            btnResumen.Location = new Point(cardUser.Left - 12 - btnResumen.Width, 20);
        }

        private static string ObtenerIniciales(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return "?";
            var partes = nombre.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length == 1) return partes[0].Substring(0, 1).ToUpper();
            return (partes[0].Substring(0, 1) + partes[1].Substring(0, 1)).ToUpper();
        }

        private void CrearRightSidebar()
        {
            panelRightSidebar = new Panel
            {
                Width = 0,
                Dock = DockStyle.Right,
                BackColor = TemaApp.BgSidebar,
                Visible = false
            };
            this.Controls.Add(panelRightSidebar);

            panelRightInner = new Panel
            {
                Width = DrawerAncho,
                Dock = DockStyle.Right,
                BackColor = TemaApp.BgSidebar,
                Padding = new Padding(14)
            };
            panelRightInner.Paint += (s, e) =>
            {
                using (var pen = new Pen(TemaApp.EsModoOscuro ? TemaApp.BorderColor : TemaApp.LightBorder, 1))
                    e.Graphics.DrawLine(pen, 0, 0, 0, panelRightInner.Height);
            };
            panelRightSidebar.Controls.Add(panelRightInner);

            panelRightInner.Controls.Add(new Label
            {
                Text = "Resumen del almacén",
                Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
                ForeColor = TemaApp.TextWhite,
                Location = new Point(18, 20),
                AutoSize = true
            });

            panelRightInner.Controls.Add(new Label
            {
                Text = "Indicadores en tiempo real",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = TemaApp.TextGray,
                Location = new Point(19, 45),
                AutoSize = true
            });

            Button btnCerrarDrawer = new Button
            {
                Text = "✕",
                Size = new Size(32, 32),
                Location = new Point(DrawerAncho - 14 - 32 - 4, 16),
                BackColor = TemaApp.BgCard,
                ForeColor = TemaApp.TextSoft,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Symbol", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                TabStop = false
            };
            btnCerrarDrawer.FlatAppearance.BorderSize = 1;
            btnCerrarDrawer.FlatAppearance.BorderColor = TemaApp.BorderColor;
            btnCerrarDrawer.FlatAppearance.MouseOverBackColor = TemaApp.BgCardHover;
            AplicarBordesRedondeados(btnCerrarDrawer, 12);
            btnCerrarDrawer.Click += (s, e) => CerrarDrawer(true);
            panelRightInner.Controls.Add(btnCerrarDrawer);

            int top = 86;
            Panel tMov = CrearTarjetaNotificacion("↔", "Movimientos de hoy", "Consultando actividad…", TemaApp.AccentBlue, top);
            lblDrawerMov = tMov.Controls["lblDetalle"] as Label;
            panelRightInner.Controls.Add(tMov);

            top += 90;
            Panel tDisp = CrearTarjetaNotificacion("✓", "Equipos disponibles", "Consultando inventario…", TemaApp.AccentGreen, top);
            lblDrawerDisp = tDisp.Controls["lblDetalle"] as Label;
            panelRightInner.Controls.Add(tDisp);

            top += 90;
            Panel tAsig = CrearTarjetaNotificacion("☻", "Asignados y en préstamo", "Consultando inventario…", TemaApp.AccentPurple, top);
            lblDrawerAsig = tAsig.Controls["lblDetalle"] as Label;
            panelRightInner.Controls.Add(tAsig);

            top += 90;
            Panel tMant = CrearTarjetaNotificacion("⚙", "En mantenimiento", "Consultando inventario…", TemaApp.AccentOrange, top);
            lblDrawerMant = tMant.Controls["lblDetalle"] as Label;
            panelRightInner.Controls.Add(tMant);

            Label lblAtajo = new Label
            {
                Text = "Ctrl + B  ·  Contraer / expandir menú\nEsc  ·  Cerrar este panel",
                Font = new Font("Segoe UI", 8f),
                ForeColor = TemaApp.TextGray,
                AutoSize = true,
                Location = new Point(18, 460)
            };
            panelRightInner.Controls.Add(lblAtajo);
            panelRightInner.Resize += (s, e) =>
            {
                lblAtajo.Location = new Point(18, Math.Max(top + 90, panelRightInner.Height - 52));
            };
        }

        private Panel CrearTarjetaNotificacion(string icono, string titulo, string detalle, Color colorAcento, int top)
        {
            Panel card = new Panel
            {
                Size = new Size(DrawerAncho - 32, 78),
                Location = new Point(16, top),
                BackColor = TemaApp.BgCard
            };
            AplicarBordesRedondeados(card, 14);

            Panel accentBar = new Panel
            {
                Size = new Size(4, 52),
                Location = new Point(0, 13),
                BackColor = colorAcento
            };
            AplicarBordesRedondeados(accentBar, 4);
            card.Controls.Add(accentBar);

            Label lblIcon = new Label
            {
                Text = icono,
                Font = new Font("Segoe UI Symbol", 13f, FontStyle.Bold),
                ForeColor = colorAcento,
                Location = new Point(14, 18),
                Size = new Size(30, 30),
                TextAlign = ContentAlignment.MiddleCenter
            };
            card.Controls.Add(lblIcon);

            Label lblTitle = new Label
            {
                Text = titulo,
                Font = new Font("Segoe UI", 8.8f, FontStyle.Bold),
                ForeColor = TemaApp.TextWhite,
                Location = new Point(52, 13),
                Size = new Size(205, 21),
                AutoEllipsis = true
            };
            card.Controls.Add(lblTitle);

            Label lblDetail = new Label
            {
                Name = "lblDetalle",
                Text = detalle,
                Font = new Font("Segoe UI", 8.25f),
                ForeColor = TemaApp.TextGray,
                Location = new Point(52, 37),
                Size = new Size(205, 31)
            };
            card.Controls.Add(lblDetail);

            return card;
        }

        private void AlternarDrawer()
        {
            if (drawerAbierto) CerrarDrawer(true);
            else AbrirDrawer();
        }

        private void AbrirDrawer()
        {
            drawerAbierto = true;
            panelRightSidebar.Visible = true;
            CargarResumenDrawer();
            IniciarAnimacion(animDrawer, panelRightSidebar.Width, DrawerAncho, DuracionDrawerMs);
            ActualizarBotonResumen();
        }

        private void CerrarDrawer(bool animar)
        {
            drawerAbierto = false;
            if (animar) IniciarAnimacion(animDrawer, panelRightSidebar.Width, 0, DuracionDrawerMs);
            else
            {
                animDrawer.Activa = false;
                panelRightSidebar.Width = 0;
                panelRightSidebar.Visible = false;
            }
            ActualizarBotonResumen();
        }

        private void ActualizarBotonResumen()
        {
            if (btnResumen == null) return;
            btnResumen.FlatAppearance.BorderColor = drawerAbierto ? TemaApp.AccentGreen : (TemaApp.EsModoOscuro ? TemaApp.BorderColor : TemaApp.LightBorder);
            btnResumen.ForeColor = drawerAbierto ? TemaApp.AccentGreen : (TemaApp.EsModoOscuro ? TemaApp.TextSoft : TemaApp.LightSoft);
        }

        private void CargarResumenDrawer()
        {
            try
            {
                using (var conexion = new global::SUSTENTACION.Conexion().ObtenerConexion())
                {
                    if (conexion == null) throw new InvalidOperationException("Sin conexión");
                    if (conexion.State != ConnectionState.Open) conexion.Open();

                    int total = 0, disp = 0, asig = 0, prest = 0, mant = 0;
                    using (var cmd = new NpgsqlCommand("SELECT LOWER(TRIM(COALESCE(estado,''))) AS e, COUNT(*)::int AS n FROM equipos GROUP BY 1;", conexion))
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            string estado = Convert.ToString(r["e"]) ?? "";
                            int n = Convert.ToInt32(r["n"]);
                            total += n;
                            if (estado == "disponible") disp += n;
                            else if (estado == "asignado") asig += n;
                            else if (estado == "prestado") prest += n;
                            else if (estado == "mantenimiento") mant += n;
                        }
                    }

                    int hoy = 0, ing = 0, sal = 0;
                    using (var cmd = new NpgsqlCommand(@"SELECT COUNT(*)::int AS total, COUNT(*) FILTER (WHERE UPPER(TRIM(tipo_movimiento)) = 'INGRESO')::int AS ingresos, COUNT(*) FILTER (WHERE UPPER(TRIM(tipo_movimiento)) = 'SALIDA')::int AS salidas FROM movimiento WHERE fecha_movimiento::date = CURRENT_DATE;", conexion))
                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            hoy = Convert.ToInt32(r["total"]);
                            ing = Convert.ToInt32(r["ingresos"]);
                            sal = Convert.ToInt32(r["salidas"]);
                        }
                    }

                    if (lblDrawerMov != null) lblDrawerMov.Text = hoy + " movimientos hoy\n" + ing + " ingresos  ·  " + sal + " salidas";
                    if (lblDrawerDisp != null) lblDrawerDisp.Text = disp + " de " + total + " equipos\nlistos para usar";
                    if (lblDrawerAsig != null) lblDrawerAsig.Text = asig + " asignados  ·  " + prest + " en préstamo";
                    if (lblDrawerMant != null) lblDrawerMant.Text = mant + " equipos\nfuera de servicio";
                }
            }
            catch (Exception)
            {
                string msg = "No se pudo consultar la base de datos";
                if (lblDrawerMov != null) lblDrawerMov.Text = msg;
                if (lblDrawerDisp != null) lblDrawerDisp.Text = msg;
                if (lblDrawerAsig != null) lblDrawerAsig.Text = msg;
                if (lblDrawerMant != null) lblDrawerMant.Text = msg;
            }
        }

        private void CrearMainContent()
        {
            panelMainContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = TemaApp.BgDark,
                AutoScroll = false,
                Padding = new Padding(16, 12, 16, 18)
            };
            this.Controls.Add(panelMainContent);
        }

        private void LimpiarContenidoActual()
        {
            if (panelResumenRef != null)
            {
                panelResumenRef.Dispose();
                panelResumenRef = null;
            }

            while (panelMainContent.Controls.Count > 0)
            {
                Control anterior = panelMainContent.Controls[0];
                panelMainContent.Controls.RemoveAt(0);
                anterior.Dispose();
            }
        }

        private void ConfigurarVista(bool esOverview)
        {
            btnResumen.Visible = esOverview;
            if (!esOverview && drawerAbierto)
                CerrarDrawer(false);
        }

        private void MostrarOverview()
        {
            lblBreadcrumb.Text = "Inicio / Panel";
            lblHeaderTitle.Text = "Panel de control";
            barra.Seleccionar(0);
            ConfigurarVista(true);

            LimpiarContenidoActual();

            panelResumenRef = new PanelResumen
            {
                Dock = DockStyle.Fill,
                BackColor = TemaApp.DashFondo
            };

            panelResumenRef.RegistrarEntradaClick += (s, e) => BeginInvoke(new Action(MostrarMovimientos));
            panelResumenRef.RegistrarSalidaClick += (s, e) => BeginInvoke(new Action(MostrarMovimientos));
            panelResumenRef.VerMovimientosClick += (s, e) => BeginInvoke(new Action(MostrarMovimientos));
            panelResumenRef.GenerarReporteClick += (s, e) => BeginInvoke(new Action(MostrarReportes));
            panelResumenRef.VerNotificacionesClick += (s, e) => BeginInvoke(new Action(AbrirDrawer));

            panelMainContent.Controls.Add(panelResumenRef);
            panelResumenRef.BringToFront();
        }

        private void MostrarInventario()
        {
            lblBreadcrumb.Text = "Inicio / Inventario";
            lblHeaderTitle.Text = "Inventario";
            barra.Seleccionar(1);
            ConfigurarVista(false);

            LimpiarContenidoActual();
            global::SUSTENTACION.InventarioControl ucInventario = new global::SUSTENTACION.InventarioControl();
            ucInventario.Dock = DockStyle.Fill;
            panelMainContent.Controls.Add(ucInventario);
            ucInventario.BringToFront();
        }

        private void MostrarMovimientos()
        {
            lblBreadcrumb.Text = "Inicio / Movimientos";
            lblHeaderTitle.Text = "Movimientos";
            barra.Seleccionar(2);
            ConfigurarVista(false);

            LimpiarContenidoActual();

            // SOLUCIÓN AL ERROR CS0234: Usar la ruta correcta hacia el namespace y la clase.
            var ucMovimientos = new global::SUSTENTACION.PanelSolicitudesEquipo.PanelSolicitudesControl { Dock = DockStyle.Fill };
            panelMainContent.Controls.Add(ucMovimientos);
            ucMovimientos.BringToFront();
        }

        private void MostrarTrabajadores()
        {
            lblBreadcrumb.Text = "Inicio / Trabajadores";
            lblHeaderTitle.Text = "Trabajadores";
            barra.Seleccionar(3);
            ConfigurarVista(false);
            LimpiarContenidoActual();
            var trabajadores = new global::SUSTENTACION.PanelTrabajador.Trabajador { Dock = DockStyle.Fill };
            panelMainContent.Controls.Add(trabajadores);
            trabajadores.BringToFront();
        }

        private void MostrarReportes()
        {
            lblBreadcrumb.Text = "Inicio / Reportes";
            lblHeaderTitle.Text = "Reportes";
            barra.Seleccionar(4);
            ConfigurarVista(false);
            LimpiarContenidoActual();
            var reportes = new global::SUSTENTACION.PanelAdministrador.CotrolReporte { Dock = DockStyle.Fill };
            panelMainContent.Controls.Add(reportes);
            reportes.BringToFront();
        }

        private void BtnCerrarSesion_Click(object? sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
                "¿Estás seguro de que deseas cerrar sesión?",
                "Cerrar sesión",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                SesionCerrada = true;
                this.Close();
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            animTimer?.Stop();
            animTimer?.Dispose();
            base.OnFormClosed(e);
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

                    control.Region?.Dispose();
                    control.Region = new Region(path);
                }
            };

            control.Resize += (s, e) => recalcularRegion();
            recalcularRegion();
        }
    }

    // =============================================================
    //  MENÚ LATERAL (Restaurado para solucionar los errores)
    // =============================================================
    internal sealed class BarraLateral : Control
    {
        public const int AnchoColapsado = 76;
        public const int AnchoExpandido = 240;

        private const int ItemsTop = 122;
        private const int ItemAlto = 46;
        private const int ItemSep = 6;

        private const int ZonaMarca = 100;
        private const int ZonaToggle = 101;
        private const int ZonaTema = 102;

        private sealed class Paleta
        {
            public Color Fondo, Tarjeta, Hover, Borde, Texto, Suave, Gris;
        }

        private static readonly Paleta Oscura = new Paleta
        {
            Fondo = Color.FromArgb(15, 20, 28),
            Tarjeta = Color.FromArgb(22, 28, 36),
            Hover = Color.FromArgb(28, 35, 44),
            Borde = Color.FromArgb(39, 47, 58),
            Texto = Color.FromArgb(245, 247, 250),
            Suave = Color.FromArgb(203, 213, 225),
            Gris = Color.FromArgb(148, 163, 184)
        };

        private static readonly Paleta Clara = new Paleta
        {
            Fondo = Color.FromArgb(248, 250, 252),
            Tarjeta = Color.White,
            Hover = Color.FromArgb(232, 240, 235),
            Borde = Color.FromArgb(214, 225, 231),
            Texto = Color.FromArgb(23, 36, 48),
            Suave = Color.FromArgb(60, 76, 90),
            Gris = Color.FromArgb(96, 112, 126)
        };

        private static readonly Color Verde = Color.FromArgb(163, 230, 53);
        private static readonly Color VerdeSuave = Color.FromArgb(102, 204, 51);

        private static readonly Font FMarca = new Font("Segoe UI", 15f, FontStyle.Bold);
        private static readonly Font FMarcaLetra = new Font("Segoe UI", 17f, FontStyle.Bold);
        private static readonly Font FSub = new Font("Segoe UI", 8f);
        private static readonly Font FSeccion = new Font("Segoe UI", 7.75f, FontStyle.Bold);
        private static readonly Font FItem = new Font("Segoe UI", 9.3f);
        private static readonly Font FItemB = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        private static readonly Font FIcono = new Font("Segoe UI Symbol", 12f, FontStyle.Bold);
        private static readonly Font FEmoji = new Font("Segoe UI Emoji", 12f);
        private static readonly Font FEstadoT = new Font("Segoe UI", 9f, FontStyle.Bold);
        private static readonly Font FEstadoS = new Font("Segoe UI", 8.5f);
        private static readonly Font FTema = new Font("Segoe UI", 9.1f, FontStyle.Bold);
        private static readonly Font FTemaIcono = new Font("Segoe UI Symbol", 11f, FontStyle.Bold);

        private sealed class Item
        {
            public string Texto = "";
            public string Icono = "";
            public bool Emoji;
            public float Hover;
            public float HoverObjetivo;
        }

        private readonly List<Item> items = new List<Item>();
        private readonly System.Windows.Forms.Timer timer;
        private readonly ToolTip tip = new ToolTip();

        private int seleccionado = -1;
        private float hlY;
        private float hlObjetivo;
        private int zonaHover = -1;
        private int zonaTooltip = -2;
        private float hoverTema;
        private float hoverTemaObjetivo;
        private int tick;
        private bool oscuro = true;
        private bool expandido = true;

        public event Action<int>? ItemClick;
        public event EventHandler? ToggleClick;
        public event EventHandler? TemaClick;

        public bool Oscuro
        {
            get { return oscuro; }
            set { oscuro = value; Invalidate(); }
        }

        public bool Expandido
        {
            get { return expandido; }
            set { expandido = value; Invalidate(); }
        }

        private Paleta Pal { get { return oscuro ? Oscura : Clara; } }

        private float Progreso
        {
            get
            {
                float p = (Width - AnchoColapsado) / (float)(AnchoExpandido - AnchoColapsado);
                return Math.Max(0f, Math.Min(1f, p));
            }
        }

        public BarraLateral()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            DoubleBuffered = true;
            TabStop = false;
            BackColor = Oscura.Fondo;

            timer = new System.Windows.Forms.Timer { Interval = 15 };
            timer.Tick += (s, e) => Animar();
        }

        public void AgregarItem(string texto, string icono, bool emoji)
        {
            items.Add(new Item { Texto = texto, Icono = icono, Emoji = emoji });
            Invalidate();
        }

        public void Seleccionar(int indice)
        {
            if (indice < 0 || indice >= items.Count) return;

            bool primera = seleccionado < 0;
            seleccionado = indice;
            hlObjetivo = ItemY(indice);
            if (primera) hlY = hlObjetivo;
            Invalidate();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            timer.Start();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            timer.Stop();
            base.OnHandleDestroyed(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                timer.Stop();
                timer.Dispose();
                tip.Dispose();
            }
            base.Dispose(disposing);
        }

        private static float ItemY(int i)
        {
            return ItemsTop + i * (ItemAlto + ItemSep);
        }

        private RectangleF ItemRect(int i)
        {
            return new RectangleF(12, ItemY(i), Width - 24, ItemAlto);
        }

        private Rectangle MarcaRect { get { return new Rectangle(16, 16, 44, 44); } }
        private Rectangle ToggleRect { get { return new Rectangle(Width - 44, 24, 28, 28); } }
        private Rectangle TemaRect { get { return new Rectangle(12, Height - 14 - 42, Width - 24, 42); } }

        private Rectangle EstadoRect
        {
            get { return new Rectangle(12, TemaRect.Top - 12 - 98, Width - 24, 98); }
        }

        private bool HayEspacioEstado
        {
            get { return EstadoRect.Top > ItemY(items.Count) + 10; }
        }

        private int ZonaEn(Point pt)
        {
            float prog = Progreso;
            if (prog > 0.5f && ToggleRect.Contains(pt)) return ZonaToggle;
            if (prog <= 0.5f && MarcaRect.Contains(pt)) return ZonaMarca;
            if (TemaRect.Contains(pt)) return ZonaTema;
            for (int i = 0; i < items.Count; i++)
                if (ItemRect(i).Contains(pt.X, pt.Y)) return i;
            return -1;
        }

        private void Animar()
        {
            bool cambio = false;

            if (Math.Abs(hlObjetivo - hlY) > 0.4f)
            {
                hlY += (hlObjetivo - hlY) * 0.26f;
                cambio = true;
            }
            else if (hlY != hlObjetivo)
            {
                hlY = hlObjetivo;
                cambio = true;
            }

            foreach (Item it in items)
            {
                if (Math.Abs(it.HoverObjetivo - it.Hover) > 0.01f)
                {
                    it.Hover += (it.HoverObjetivo - it.Hover) * 0.3f;
                    cambio = true;
                }
                else it.Hover = it.HoverObjetivo;
            }

            if (Math.Abs(hoverTemaObjetivo - hoverTema) > 0.01f)
            {
                hoverTema += (hoverTemaObjetivo - hoverTema) * 0.3f;
                cambio = true;
            }
            else hoverTema = hoverTemaObjetivo;

            if (cambio)
            {
                Invalidate();
            }
            else
            {
                tick++;
                if (tick % 3 == 0 && HayEspacioEstado)
                    Invalidate(Rectangle.Inflate(EstadoRect, 4, 30));
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int zona = ZonaEn(e.Location);

            if (zona != zonaHover)
            {
                zonaHover = zona;
                for (int i = 0; i < items.Count; i++)
                    items[i].HoverObjetivo = i == zona ? 1f : 0f;
                hoverTemaObjetivo = zona == ZonaTema ? 1f : 0f;
                Cursor = zona >= 0 ? Cursors.Hand : Cursors.Default;
                ActualizarTooltip(zona);
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            zonaHover = -1;
            foreach (Item it in items) it.HoverObjetivo = 0f;
            hoverTemaObjetivo = 0f;
            Cursor = Cursors.Default;
            ActualizarTooltip(-1);
        }

        private void ActualizarTooltip(int zona)
        {
            if (zona == zonaTooltip) return;
            zonaTooltip = zona;

            if (zona < 0 || Progreso > 0.3f)
            {
                tip.Hide(this);
                return;
            }

            string texto;
            int y;
            if (zona == ZonaMarca)
            {
                texto = "Expandir menú  (Ctrl + B)";
                y = MarcaRect.Top + 12;
            }
            else if (zona == ZonaTema)
            {
                texto = oscuro ? "Cambiar a modo claro" : "Cambiar a modo oscuro";
                y = TemaRect.Top + 12;
            }
            else if (zona >= 0 && zona < items.Count)
            {
                texto = items[zona].Texto;
                y = (int)ItemY(zona) + 12;
            }
            else
            {
                tip.Hide(this);
                return;
            }

            tip.Show(texto, this, Width + 6, y);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            int zona = ZonaEn(e.Location);

            if (zona == ZonaToggle || zona == ZonaMarca)
            {
                var h = ToggleClick;
                if (h != null) h(this, EventArgs.Empty);
            }
            else if (zona == ZonaTema)
            {
                var h = TemaClick;
                if (h != null) h(this, EventArgs.Empty);
            }
            else if (zona >= 0 && zona < items.Count)
            {
                var h = ItemClick;
                if (h != null) h(zona);
            }
        }

        private static GraphicsPath Redondo(RectangleF r, float radio)
        {
            var path = new GraphicsPath();
            if (r.Width <= 1 || r.Height <= 1) return path;
            float d = Math.Max(1f, Math.Min(radio * 2f, Math.Min(r.Width, r.Height)));
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static Color Lerp(Color a, Color b, float t)
        {
            t = Math.Max(0f, Math.Min(1f, t));
            return Color.FromArgb(
                (int)(a.A + (b.A - a.A) * t),
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }

        private static Color Alfa(Color c, float f)
        {
            f = Math.Max(0f, Math.Min(1f, f));
            return Color.FromArgb((int)(c.A * f), c.R, c.G, c.B);
        }

        private static void Texto(Graphics g, string s, Font f, Color c, RectangleF r, StringAlignment h = StringAlignment.Near)
        {
            using (var b = new SolidBrush(c))
            using (var sf = new StringFormat
            {
                Alignment = h,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap
            })
                g.DrawString(s, f, b, r, sf);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            Paleta p = Pal;
            float prog = Progreso;
            g.Clear(p.Fondo);

            using (var pen = new Pen(p.Borde, 1))
                g.DrawLine(pen, Width - 1, 0, Width - 1, Height);

            DibujarMarca(g, p, prog);
            DibujarSeccion(g, p, prog);
            DibujarResaltado(g);
            DibujarItems(g, p, prog);

            if (HayEspacioEstado) DibujarEstado(g, p, prog);
            DibujarTema(g, p, prog);
        }

        private void DibujarMarca(Graphics g, Paleta p, float prog)
        {
            var marca = MarcaRect;
            using (var path = Redondo(marca, 12))
            using (var br = new LinearGradientBrush(marca, Verde, VerdeSuave, LinearGradientMode.ForwardDiagonal))
                g.FillPath(br, path);
            Texto(g, "A", FMarcaLetra, Color.Black, marca, StringAlignment.Center);

            if (prog > 0.03f)
            {
                var estado = g.Save();
                g.SetClip(new Rectangle(68, 0, Math.Max(1, Width - 68 - 52), 76));
                Texto(g, "ARTEMUSA", FMarca, Alfa(Verde, prog), new RectangleF(70, 14, 150, 26));
                Texto(g, "Gestión de Inventario", FSub, Alfa(p.Gris, prog), new RectangleF(71, 40, 160, 16));
                g.Restore(estado);

                var t = ToggleRect;
                bool hover = zonaHover == ZonaToggle;
                using (var b = new SolidBrush(Alfa(hover ? p.Hover : p.Tarjeta, prog)))
                    g.FillEllipse(b, t);
                using (var pen = new Pen(Alfa(p.Borde, prog), 1))
                    g.DrawEllipse(pen, t);
                using (var pen = new Pen(Alfa(p.Suave, prog), 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    float cx = t.X + t.Width / 2f, cy = t.Y + t.Height / 2f;
                    g.DrawLine(pen, cx + 2.5f, cy - 5.5f, cx - 3f, cy);
                    g.DrawLine(pen, cx - 3f, cy, cx + 2.5f, cy + 5.5f);
                }
            }
        }

        private void DibujarSeccion(Graphics g, Paleta p, float prog)
        {
            if (prog > 0.03f)
                Texto(g, "MENÚ PRINCIPAL", FSeccion, Alfa(p.Gris, prog), new RectangleF(22, 90, 160, 16));

            if (prog < 0.97f)
            {
                using (var pen = new Pen(Alfa(p.Borde, 1f - prog), 1))
                    g.DrawLine(pen, 24, 98, Width - 24, 98);
            }
        }

        private void DibujarResaltado(Graphics g)
        {
            if (seleccionado < 0) return;

            var r = new RectangleF(12, hlY, Width - 24, ItemAlto);

            for (int k = 3; k >= 1; k--)
            {
                var rg = RectangleF.Inflate(r, k * 2f, k * 2f);
                using (var path = Redondo(rg, 12 + k))
                using (var b = new SolidBrush(Color.FromArgb(10, Verde)))
                    g.FillPath(b, path);
            }

            using (var path = Redondo(r, 12))
            using (var br = new LinearGradientBrush(
                new RectangleF(r.X, r.Y, Math.Max(1f, r.Width), Math.Max(1f, r.Height)),
                Verde, VerdeSuave, LinearGradientMode.Horizontal))
                g.FillPath(br, path);
        }

        private void DibujarItems(Graphics g, Paleta p, float prog)
        {
            for (int i = 0; i < items.Count; i++)
            {
                Item it = items[i];
                RectangleF r = ItemRect(i);

                if (it.Hover > 0.02f && i != seleccionado)
                {
                    using (var path = Redondo(r, 12))
                    using (var b = new SolidBrush(Alfa(p.Hover, it.Hover)))
                        g.FillPath(b, path);
                }

                float act = seleccionado >= 0
                    ? Math.Max(0f, 1f - Math.Abs(r.Y - hlY) / (ItemAlto + ItemSep))
                    : 0f;

                Color baseColor = Lerp(p.Gris, p.Texto, it.Hover);
                Color color = Lerp(baseColor, Color.Black, act);

                var iconoRect = new RectangleF(12, r.Y, 52, r.Height);
                Texto(g, it.Icono, it.Emoji ? FEmoji : FIcono, color, iconoRect, StringAlignment.Center);

                if (prog > 0.03f)
                {
                    var estado = g.Save();
                    g.SetClip(new RectangleF(66, r.Y, Math.Max(1f, r.Width - 56), r.Height));
                    Texto(g, it.Texto, act > 0.5f ? FItemB : FItem, Alfa(color, prog),
                        new RectangleF(70, r.Y, Math.Max(1f, r.Width - 60), r.Height));
                    g.Restore(estado);
                }
            }
        }

        private void DibujarEstado(Graphics g, Paleta p, float prog)
        {
            float pulso = (float)(0.5 + 0.5 * Math.Sin(Environment.TickCount / 380.0));
            var r = EstadoRect;

            if (prog > 0.03f)
            {
                var estado = g.Save();
                g.SetClip(r);
                using (var path = Redondo(r, 14))
                {
                    using (var b = new SolidBrush(Alfa(p.Tarjeta, prog)))
                        g.FillPath(b, path);
                    using (var pen = new Pen(Alfa(p.Borde, prog), 1))
                        g.DrawPath(pen, path);
                }

                Texto(g, "Estado del Sistema", FEstadoT, Alfa(p.Texto, prog),
                    new RectangleF(r.X + 16, r.Y + 12, r.Width - 24, 20));

                float cy = r.Y + 56;
                using (var b = new SolidBrush(Alfa(Verde, prog * (0.15f + 0.2f * pulso))))
                    g.FillEllipse(b, r.X + 14 - 4 * pulso, cy - 4 * pulso - 2, 12 + 8 * pulso, 12 + 8 * pulso);
                using (var b = new SolidBrush(Alfa(Verde, prog)))
                    g.FillEllipse(b, r.X + 18, cy + 2, 8, 8);

                Texto(g, "Sistema operativo", FEstadoS, Alfa(p.Suave, prog),
                    new RectangleF(r.X + 36, r.Y + 42, r.Width - 44, 18));
                Texto(g, "Conexión estable", FEstadoS, Alfa(p.Gris, prog),
                    new RectangleF(r.X + 36, r.Y + 60, r.Width - 44, 18));
                g.Restore(estado);
            }

            if (prog < 0.97f)
            {
                float a = 1f - prog;
                float cx = 38f, cy = r.Bottom - 20;
                using (var b = new SolidBrush(Alfa(Verde, a * (0.15f + 0.2f * pulso))))
                    g.FillEllipse(b, cx - 8 - 3 * pulso, cy - 8 - 3 * pulso, 16 + 6 * pulso, 16 + 6 * pulso);
                using (var b = new SolidBrush(Alfa(Verde, a)))
                    g.FillEllipse(b, cx - 4, cy - 4, 8, 8);
            }
        }

        private void DibujarTema(Graphics g, Paleta p, float prog)
        {
            var r = TemaRect;
            using (var path = Redondo(r, 12))
            {
                using (var b = new SolidBrush(Lerp(p.Tarjeta, p.Hover, hoverTema)))
                    g.FillPath(b, path);
                using (var pen = new Pen(p.Borde, 1))
                    g.DrawPath(pen, path);
            }

            string glifo = oscuro ? "☀" : "☾";
            Texto(g, glifo, FTemaIcono, p.Suave,
                new RectangleF(r.X, r.Y, 52, r.Height), StringAlignment.Center);

            if (prog > 0.03f)
            {
                var estado = g.Save();
                g.SetClip(new RectangleF(r.X + 50, r.Y, Math.Max(1f, r.Width - 54), r.Height));
                Texto(g, oscuro ? "Modo claro" : "Modo oscuro", FTema, Alfa(p.Suave, prog),
                    new RectangleF(r.X + 56, r.Y, Math.Max(1f, r.Width - 60), r.Height));
                g.Restore(estado);
            }
        }
    }
}