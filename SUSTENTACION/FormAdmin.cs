using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using SUSTENTACION.PanelAdministrador;

namespace SUSTENTACION
{
    public partial class FormAdmin : Form
    {
        // =========================================================
        // PALETA VISUAL MODERNA - ARTEMUSA
        // =========================================================
        private readonly Color bgDark = Color.FromArgb(10, 14, 20);
        private readonly Color bgSidebar = Color.FromArgb(15, 20, 28);
        private readonly Color bgHeader = Color.FromArgb(10, 14, 20);
        private readonly Color bgCard = Color.FromArgb(22, 28, 36);
        private readonly Color bgCardHover = Color.FromArgb(28, 35, 44);
        private readonly Color bgInput = Color.FromArgb(18, 24, 32);

        private readonly Color accentGreen = Color.FromArgb(163, 230, 53);
        private readonly Color accentGreenSoft = Color.FromArgb(102, 204, 51);
        private readonly Color accentBlue = Color.FromArgb(56, 189, 248);
        private readonly Color accentOrange = Color.FromArgb(251, 146, 60);
        private readonly Color accentPink = Color.FromArgb(236, 72, 153);
        private readonly Color accentPurple = Color.FromArgb(168, 85, 247);

        private readonly Color textWhite = Color.FromArgb(245, 247, 250);
        private readonly Color textSoft = Color.FromArgb(203, 213, 225);
        private readonly Color textGray = Color.FromArgb(148, 163, 184);
        private readonly Color borderColor = Color.FromArgb(39, 47, 58);

        // =========================================================
        // PALETA DEL MODO CLARO
        // =========================================================
        private readonly Color lightCanvas = Color.FromArgb(244, 248, 250);
        private readonly Color lightSidebar = Color.FromArgb(248, 250, 252);
        private readonly Color lightHeader = Color.White;
        private readonly Color lightCard = Color.White;
        private readonly Color lightCardHover = Color.FromArgb(232, 240, 235);
        private readonly Color lightInput = Color.FromArgb(247, 250, 252);
        private readonly Color lightText = Color.FromArgb(23, 36, 48);
        private readonly Color lightSoft = Color.FromArgb(60, 76, 90);
        private readonly Color lightGray = Color.FromArgb(96, 112, 126);
        private readonly Color lightBorder = Color.FromArgb(214, 225, 231);

        // =========================================================
        // PANELS PRINCIPALES
        // =========================================================
        private Panel panelSidebar = null!;
        private Panel panelRightSidebar = null!;
        private Panel panelHeader = null!;
        private Panel panelMainContent = null!;

        // Referencias visuales del menú lateral
        private Panel brandCardRef = null!;
        private Panel statusCardRef = null!;
        private Label lblMenuPrincipalRef = null!;
        private Label lblLogoRef = null!;
        private Label lblLogoSubRef = null!;
        private Button btnToggleSidebar = null!;

        // Sidebar deslizante
        private const int SidebarAnchoExpandido = 235;
        private const int SidebarAnchoColapsado = 76;
        private const int SidebarPasoAnimacion = 18;
        private bool sidebarExpandido = true;
        private int sidebarAnchoObjetivo = SidebarAnchoExpandido;
        private System.Windows.Forms.Timer timerSidebar = null!;
        private readonly ToolTip tooltipSidebar = new ToolTip();

        // Referencias navegación
        private Button btnOverviewRef = null!;
        private Button btnInventarioRef = null!;
        private Button btnMovimientosRef = null!;
        private Button btnTrabajadoresRef = null!;
        private Button btnReportesRef = null!;
        private Button btnTemaRef = null!;
        private Button? botonActivo = null;

        // El panel inicia en modo oscuro, igual que el diseño original.
        private bool modoOscuro = true;

        private Label lblBreadcrumb = null!;
        private Label lblHeaderTitle = null!;

        // Controles y resultados del panel de equipos audiovisuales.
        private ComboBox? cmbOverviewCategoria;
        private ComboBox? cmbOverviewEstado;
        private ComboBox? cmbOverviewUbicacion;
        private ComboBox? cmbOverviewTipoMovimiento;
        private ComboBox? cmbOverviewResponsable;
        private DateTimePicker? dtOverviewDesde;
        private DateTimePicker? dtOverviewHasta;
        private TextBox? txtOverviewBuscar;
        private NumericUpDown? nudOverviewUmbral;
        private Button? btnOverviewAplicar;
        private Label? lblOverviewConexion;
        private Label? lblOverviewActualizado;
        private Label? lblSidebarMovimientosHoy;
        private Label? lblSidebarCondicionBaja;
        private Label? lblSidebarResponsables;

        private DataGridView? dgvOverviewMovimientos;

        private OverviewBarChart? chartOverviewCategorias;
        private OverviewMonthlyChart? chartOverviewMovimientos;
        private OverviewDonutChart? chartOverviewEstados;
        private readonly Label[] valoresOverview = new Label[5];
        private readonly Label[] detallesOverview = new Label[5];
        private DataTable? tablaOverviewMovimientos;
        private bool cargandoOverview;

        // Datos usuario
        private string nombreUsuario;
        private string rolUsuario;

        public FormAdmin(string nombre, string rol)
        {
            InitializeComponent();
            nombreUsuario = string.IsNullOrWhiteSpace(nombre) ? "Juan Pérez" : nombre;
            rolUsuario = string.IsNullOrWhiteSpace(rol) ? "Administrador" : rol;

            ConstruirInterfaz();
        }

        private void ConstruirInterfaz()
        {
            this.Size = new Size(1450, 860);
            this.MinimumSize = new Size(1280, 760);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Artemusa Inventario - Panel de Administración";
            this.BackColor = bgDark;
            this.DoubleBuffered = true;
            this.Padding = new Padding(0);

            ConfigurarAnimacionSidebar();
            CrearSidebar();
            CrearRightSidebar();
            CrearHeader();
            CrearMainContent();

            MostrarOverview();
        }

        // =========================================================
        // SIDEBAR IZQUIERDO
        // =========================================================
        private void CrearSidebar()
        {
            panelSidebar = new Panel
            {
                Width = SidebarAnchoExpandido,
                MinimumSize = new Size(SidebarAnchoColapsado, 0),
                Dock = DockStyle.Left,
                BackColor = bgSidebar,
                Padding = new Padding(0)
            };
            this.Controls.Add(panelSidebar);

            // Marca
            brandCardRef = new Panel
            {
                Size = new Size(164, 72),
                Location = new Point(10, 15),
                BackColor = bgCard
            };
            AplicarBordesRedondeados(brandCardRef, 16);

            lblLogoRef = new Label
            {
                Text = "ARTEMUSA",
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = accentGreen,
                Location = new Point(14, 10),
                AutoSize = true
            };
            brandCardRef.Controls.Add(lblLogoRef);

            lblLogoSubRef = new Label
            {
                Text = "Gestión de Inventario",
                Font = new Font("Segoe UI", 8.1f, FontStyle.Regular),
                ForeColor = textGray,
                Location = new Point(15, 40),
                AutoSize = true
            };
            brandCardRef.Controls.Add(lblLogoSubRef);
            panelSidebar.Controls.Add(brandCardRef);

            // Botón de abrir/cerrar menú
            btnToggleSidebar = new Button
            {
                Name = "btnToggleSidebar",
                Text = "❮",
                Size = new Size(40, 40),
                Location = new Point(184, 30),
                BackColor = bgCard,
                ForeColor = textSoft,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Symbol", 12f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand,
                TabStop = false
            };
            btnToggleSidebar.FlatAppearance.BorderSize = 1;
            btnToggleSidebar.FlatAppearance.BorderColor = borderColor;
            btnToggleSidebar.FlatAppearance.MouseOverBackColor = bgCardHover;
            AplicarBordesRedondeados(btnToggleSidebar, 12);
            btnToggleSidebar.Click += (s, e) => AlternarSidebar();
            tooltipSidebar.SetToolTip(btnToggleSidebar, "Contraer menú");
            panelSidebar.Controls.Add(btnToggleSidebar);

            lblMenuPrincipalRef = new Label
            {
                Text = "MENÚ PRINCIPAL",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = textGray,
                Location = new Point(14, 104),
                AutoSize = true
            };
            panelSidebar.Controls.Add(lblMenuPrincipalRef);

            btnOverviewRef = CrearBotonSidebar("Panel", 130, "◉");
            btnOverviewRef.Click += (s, e) => MostrarOverview();
            panelSidebar.Controls.Add(btnOverviewRef);

            btnInventarioRef = CrearBotonSidebar("Inventario", 176, "▣");
            btnInventarioRef.Click += (s, e) => MostrarInventario();
            panelSidebar.Controls.Add(btnInventarioRef);

            btnMovimientosRef = CrearBotonSidebar("Movimientos", 222, "⇄");
            btnMovimientosRef.Click += (s, e) => MostrarMovimientos();
            panelSidebar.Controls.Add(btnMovimientosRef);

            btnTrabajadoresRef = CrearBotonSidebar("Trabajadores", 268, "👥");
            btnTrabajadoresRef.Click += (s, e) => MostrarTrabajadores();
            panelSidebar.Controls.Add(btnTrabajadoresRef);

            btnReportesRef = CrearBotonSidebar("Reportes", 314, "▧");
            btnReportesRef.Click += (s, e) => MostrarReportes();
            panelSidebar.Controls.Add(btnReportesRef);

            tooltipSidebar.SetToolTip(btnOverviewRef, "Panel general del inventario audiovisual");
            tooltipSidebar.SetToolTip(btnInventarioRef, "Inventario");
            tooltipSidebar.SetToolTip(btnMovimientosRef, "Movimientos");
            tooltipSidebar.SetToolTip(btnTrabajadoresRef, "Trabajadores");
            tooltipSidebar.SetToolTip(btnReportesRef, "Reportes");

            // Estado del sistema
            statusCardRef = new Panel
            {
                Size = new Size(200, 105),
                Location = new Point(10, 525),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                BackColor = bgCard
            };
            AplicarBordesRedondeados(statusCardRef, 16);

            Label lblStatusTitle = new Label
            {
                Text = "Estado del Sistema",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = textWhite,
                Location = new Point(15, 14),
                AutoSize = true
            };
            statusCardRef.Controls.Add(lblStatusTitle);

            Label lblStatusDot = new Label
            {
                Text = "●",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = accentGreen,
                Location = new Point(14, 40),
                AutoSize = true
            };
            statusCardRef.Controls.Add(lblStatusDot);

            Label lblStatusText = new Label
            {
                Text = "Sistema operativo\nConexión estable",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = textGray,
                Location = new Point(35, 44),
                AutoSize = true
            };
            statusCardRef.Controls.Add(lblStatusText);
            panelSidebar.Controls.Add(statusCardRef);

            btnTemaRef = CrearBotonTema();
            panelSidebar.Controls.Add(btnTemaRef);

            panelSidebar.Resize += (s, e) =>
            {
                ReubicarElementosSidebar();
            };

            ReubicarElementosSidebar();
            AplicarEstadoVisualSidebar();
        }

        // =========================================================
        // SIDEBAR DESLIZANTE
        // =========================================================
        private void ConfigurarAnimacionSidebar()
        {
            timerSidebar = new System.Windows.Forms.Timer
            {
                Interval = 12
            };

            timerSidebar.Tick += (s, e) =>
            {
                int actual = panelSidebar.Width;

                if (actual == sidebarAnchoObjetivo)
                {
                    timerSidebar.Stop();
                    AplicarEstadoVisualSidebar();
                    panelMainContent?.PerformLayout();
                    this.PerformLayout();
                    return;
                }

                if (actual < sidebarAnchoObjetivo)
                {
                    panelSidebar.Width = Math.Min(
                        sidebarAnchoObjetivo,
                        actual + SidebarPasoAnimacion
                    );
                }
                else
                {
                    panelSidebar.Width = Math.Max(
                        sidebarAnchoObjetivo,
                        actual - SidebarPasoAnimacion
                    );
                }

                ReubicarElementosSidebar();

                if (panelSidebar.Width == sidebarAnchoObjetivo)
                {
                    timerSidebar.Stop();
                    AplicarEstadoVisualSidebar();
                    panelMainContent?.PerformLayout();
                    this.PerformLayout();
                }
            };
        }

        private void AlternarSidebar()
        {
            if (timerSidebar.Enabled)
                return;

            sidebarExpandido = !sidebarExpandido;
            sidebarAnchoObjetivo = sidebarExpandido
                ? SidebarAnchoExpandido
                : SidebarAnchoColapsado;

            // Al empezar a cerrar se ocultan textos para evitar recortes.
            if (!sidebarExpandido)
                AplicarEstadoVisualSidebar();

            timerSidebar.Start();
        }

        private void AplicarEstadoVisualSidebar()
        {
            if (panelSidebar == null)
                return;

            bool expandido = sidebarExpandido;

            brandCardRef.Visible = expandido;
            lblMenuPrincipalRef.Visible = expandido;
            statusCardRef.Visible = expandido;

            btnToggleSidebar.Text = expandido ? "❮" : "☰";
            tooltipSidebar.SetToolTip(
                btnToggleSidebar,
                expandido ? "Contraer menú" : "Expandir menú"
            );

            ActualizarBotonSidebar(btnOverviewRef, "Panel", "◉", expandido);
            ActualizarBotonSidebar(btnInventarioRef, "Inventario", "▣", expandido);
            ActualizarBotonSidebar(btnMovimientosRef, "Movimientos", "⇄", expandido);
            ActualizarBotonSidebar(btnTrabajadoresRef, "Trabajadores", "👥", expandido);
            ActualizarBotonSidebar(btnReportesRef, "Reportes", "▧", expandido);

            if (expandido)
            {
                btnTemaRef.Text = modoOscuro ? "☀  Modo claro" : "☾  Modo oscuro";
                btnTemaRef.TextAlign = ContentAlignment.MiddleCenter;
                btnTemaRef.Font = new Font("Segoe UI", 9.1f, FontStyle.Bold);
            }
            else
            {
                btnTemaRef.Text = modoOscuro ? "☀" : "☾";
                btnTemaRef.TextAlign = ContentAlignment.MiddleCenter;
                btnTemaRef.Font = new Font("Segoe UI Symbol", 11f, FontStyle.Bold);
            }

            ReubicarElementosSidebar();
        }

        private void ActualizarBotonSidebar(
            Button boton,
            string texto,
            string icono,
            bool expandido)
        {
            if (boton == null)
                return;

            boton.Tag = texto;

            if (expandido)
            {
                boton.Text = $"  {icono}   {texto}";
                boton.TextAlign = ContentAlignment.MiddleLeft;
                boton.Font = boton == botonActivo
                    ? new Font("Segoe UI", 9.5f, FontStyle.Bold)
                    : new Font("Segoe UI", 9.3f, FontStyle.Regular);
            }
            else
            {
                boton.Text = icono;
                boton.TextAlign = ContentAlignment.MiddleCenter;
                boton.Font = new Font(
                    icono == "👥" ? "Segoe UI Emoji" : "Segoe UI Symbol",
                    12f,
                    FontStyle.Bold
                );
            }
        }

        private void ReubicarElementosSidebar()
        {
            if (panelSidebar == null)
                return;

            bool compacto = !sidebarExpandido || panelSidebar.Width <= 110;
            int margen = compacto ? 12 : 10;
            int anchoBoton = compacto
                ? Math.Max(46, panelSidebar.ClientSize.Width - 24)
                : Math.Max(120, panelSidebar.ClientSize.Width - 35);

            btnToggleSidebar.Location = compacto
                ? new Point(
                    Math.Max(8, (panelSidebar.ClientSize.Width - btnToggleSidebar.Width) / 2),
                    24
                )
                : new Point(
                    Math.Max(174, panelSidebar.ClientSize.Width - btnToggleSidebar.Width - 11),
                    30
                );

            if (!compacto)
            {
                brandCardRef.Width = Math.Max(150, panelSidebar.ClientSize.Width - 70);
                brandCardRef.Location = new Point(10, 15);
            }

            Button[] botones =
            {
                btnOverviewRef,
                btnInventarioRef,
                btnMovimientosRef,
                btnTrabajadoresRef,
                btnReportesRef
            };

            foreach (Button boton in botones)
            {
                if (boton == null)
                    continue;

                boton.Width = anchoBoton;
                boton.Left = compacto
                    ? Math.Max(8, (panelSidebar.ClientSize.Width - anchoBoton) / 2)
                    : margen;
            }

            btnTemaRef.Width = compacto
                ? Math.Max(46, panelSidebar.ClientSize.Width - 24)
                : Math.Max(120, panelSidebar.ClientSize.Width - 35);

            btnTemaRef.Left = compacto
                ? Math.Max(8, (panelSidebar.ClientSize.Width - btnTemaRef.Width) / 2)
                : margen;

            int margenInferior = 14;
            int separacion = 12;

            btnTemaRef.Top = Math.Max(
                0,
                panelSidebar.ClientSize.Height - btnTemaRef.Height - margenInferior
            );

            if (!compacto)
            {
                statusCardRef.Width = Math.Max(120, panelSidebar.ClientSize.Width - 35);
                statusCardRef.Left = margen;
                statusCardRef.Top = Math.Max(
                    120,
                    btnTemaRef.Top - statusCardRef.Height - separacion
                );
            }
        }

        private Button CrearBotonTema()
        {
            Button btn = new Button
            {
                Name = "btnTema",
                Text = "☀  Modo claro",
                Size = new Size(200, 38),
                Location = new Point(10, 0),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                BackColor = bgCard,
                ForeColor = textSoft,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.1f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };

            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = borderColor;
            btn.FlatAppearance.MouseOverBackColor = bgCardHover;
            AplicarBordesRedondeados(btn, 12);

            btn.MouseEnter += (s, e) =>
            {
                btn.BackColor = modoOscuro ? bgCardHover : lightCardHover;
            };

            btn.MouseLeave += (s, e) =>
            {
                btn.BackColor = modoOscuro ? bgCard : lightCard;
            };

            btn.Click += (s, e) => CambiarTema();
            return btn;
        }

        private Button CrearBotonSidebar(string texto, int topPos, string icono)
        {
            Button btn = new Button
            {
                Text = $"  {icono}   {texto}",
                Tag = texto,
                Size = new Size(200, 38),
                Location = new Point(10, topPos),
                BackColor = Color.Transparent,
                ForeColor = textGray,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.3f, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand
            };

            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(28, 35, 44);

            btn.MouseEnter += (s, e) =>
            {
                if (btn != botonActivo)
                {
                    btn.BackColor = modoOscuro ? Color.FromArgb(24, 31, 40) : lightCardHover;
                    btn.ForeColor = modoOscuro ? textWhite : lightText;
                }
            };

            btn.MouseLeave += (s, e) =>
            {
                if (btn != botonActivo)
                {
                    btn.BackColor = Color.Transparent;
                    btn.ForeColor = modoOscuro ? textGray : lightGray;
                }
            };

            return btn;
        }

        private void ActivarBoton(Button btn)
        {
            Color colorTextoNavegacion = modoOscuro ? textGray : lightGray;

            if (botonActivo != null)
            {
                botonActivo.BackColor = Color.Transparent;
                botonActivo.ForeColor = colorTextoNavegacion;
                botonActivo.Font = new Font("Segoe UI", 9.3f, FontStyle.Regular);
            }

            botonActivo = btn;
            botonActivo.BackColor = accentGreen;
            botonActivo.ForeColor = Color.Black;
            botonActivo.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            AplicarBordesRedondeados(botonActivo, 12);

            // Conserva el formato compacto cuando el menú está cerrado.
            if (!sidebarExpandido)
            {
                AplicarEstadoVisualSidebar();
            }
        }

        // =========================================================
        // CAMBIO DE TEMA
        // =========================================================
        private void CambiarTema()
        {
            modoOscuro = !modoOscuro;

            // Primero se actualizan los controles existentes, incluidos los
            // controles de la vista que esté abierta en ese momento.
            AplicarTemaRecursivo(this);

            // Los paneles principales se fijan explícitamente para evitar que
            // una vista hija pueda dejar el fondo del marco en un color incorrecto.
            this.BackColor = modoOscuro ? bgDark : lightCanvas;
            panelSidebar.BackColor = modoOscuro ? bgSidebar : lightSidebar;
            panelHeader.BackColor = modoOscuro ? bgHeader : lightHeader;
            panelRightSidebar.BackColor = modoOscuro ? bgSidebar : lightSidebar;
            panelMainContent.BackColor = modoOscuro ? bgDark : lightCanvas;

            btnTemaRef.Text = sidebarExpandido
                ? (modoOscuro ? "☀  Modo claro" : "☾  Modo oscuro")
                : (modoOscuro ? "☀" : "☾");
            btnTemaRef.BackColor = modoOscuro ? bgCard : lightCard;
            btnTemaRef.ForeColor = modoOscuro ? textSoft : lightSoft;
            btnTemaRef.FlatAppearance.BorderColor = modoOscuro ? borderColor : lightBorder;
            btnTemaRef.FlatAppearance.MouseOverBackColor = modoOscuro ? bgCardHover : lightCardHover;

            // El botón activo conserva el color de acento en ambos temas.
            if (botonActivo != null)
            {
                botonActivo.BackColor = accentGreen;
                botonActivo.ForeColor = Color.Black;
            }

            btnToggleSidebar.BackColor = modoOscuro ? bgCard : lightCard;
            btnToggleSidebar.ForeColor = modoOscuro ? textSoft : lightSoft;
            btnToggleSidebar.FlatAppearance.BorderColor = modoOscuro ? borderColor : lightBorder;
            btnToggleSidebar.FlatAppearance.MouseOverBackColor = modoOscuro ? bgCardHover : lightCardHover;

            AplicarEstadoVisualSidebar();
            ActualizarEstilosGrids(this);
            this.Invalidate(true);
        }

        private void AplicarTemaRecursivo(Control control)
        {
            if (control != btnTemaRef)
            {
                control.BackColor = MapearColorFondo(control.BackColor);
                control.ForeColor = MapearColorTexto(control.ForeColor);

                if (control is Button button)
                {
                    button.FlatAppearance.MouseOverBackColor =
                        modoOscuro ? bgCardHover : lightCardHover;
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

            if (modoOscuro)
            {
                if (valor == lightCanvas.ToArgb() ||
                    valor == Color.FromArgb(245, 247, 249).ToArgb() ||
                    valor == Color.FromArgb(242, 246, 249).ToArgb())
                {
                    return bgDark;
                }

                if (valor == lightSidebar.ToArgb())
                {
                    return bgSidebar;
                }

                if (valor == lightCard.ToArgb() ||
                    valor == Color.White.ToArgb() ||
                    valor == Color.FromArgb(250, 252, 253).ToArgb())
                {
                    return bgCard;
                }

                if (valor == lightInput.ToArgb() ||
                    valor == Color.FromArgb(235, 241, 245).ToArgb())
                {
                    return bgInput;
                }

                if (valor == lightCardHover.ToArgb())
                {
                    return bgCardHover;
                }

                if (valor == lightBorder.ToArgb())
                {
                    return borderColor;
                }
            }
            else
            {
                if (valor == bgDark.ToArgb())
                {
                    return lightCanvas;
                }

                if (valor == bgSidebar.ToArgb())
                {
                    return lightSidebar;
                }

                if (valor == bgHeader.ToArgb() ||
                    valor == bgCard.ToArgb())
                {
                    return lightCard;
                }

                if (valor == bgInput.ToArgb())
                {
                    return lightInput;
                }

                if (valor == bgCardHover.ToArgb())
                {
                    return lightCardHover;
                }

                if (valor == borderColor.ToArgb())
                {
                    return lightBorder;
                }

                if (valor == Color.FromArgb(25, 31, 39).ToArgb() ||
                    valor == Color.FromArgb(32, 42, 53).ToArgb())
                {
                    return lightInput;
                }
            }

            return color;
        }

        private Color MapearColorTexto(Color color)
        {
            int valor = color.ToArgb();

            if (modoOscuro)
            {
                if (valor == lightText.ToArgb()) return textWhite;
                if (valor == lightSoft.ToArgb()) return textSoft;
                if (valor == lightGray.ToArgb()) return textGray;
            }
            else
            {
                if (valor == textWhite.ToArgb() || valor == Color.White.ToArgb())
                {
                    return lightText;
                }

                if (valor == textSoft.ToArgb()) return lightSoft;
                if (valor == textGray.ToArgb()) return lightGray;
            }

            return color;
        }

        private void ActualizarEstilosGrids(Control raiz)
        {
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
            Color fondo = modoOscuro ? bgCard : lightCard;
            Color fondoEntrada = modoOscuro ? bgInput : lightInput;
            Color fondoSeleccion = modoOscuro
                ? Color.FromArgb(32, 42, 53)
                : Color.FromArgb(221, 240, 228);
            Color fondoAlterno = modoOscuro
                ? Color.FromArgb(25, 31, 39)
                : Color.FromArgb(247, 250, 252);
            Color texto = modoOscuro ? textWhite : lightText;

            grid.BackgroundColor = fondo;
            grid.GridColor = modoOscuro ? borderColor : lightBorder;
            grid.ColumnHeadersDefaultCellStyle.BackColor = fondoEntrada;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = accentGreen;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = fondoEntrada;
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = accentGreen;
            grid.DefaultCellStyle.BackColor = fondo;
            grid.DefaultCellStyle.ForeColor = texto;
            grid.DefaultCellStyle.SelectionBackColor = fondoSeleccion;
            grid.DefaultCellStyle.SelectionForeColor = modoOscuro ? accentGreen : Color.FromArgb(22, 101, 52);
            grid.AlternatingRowsDefaultCellStyle.BackColor = fondoAlterno;
            grid.AlternatingRowsDefaultCellStyle.ForeColor = texto;
        }

        // =========================================================
        // HEADER SUPERIOR
        // =========================================================
        private void CrearHeader()
        {
            panelHeader = new Panel
            {
                Height = 82,
                Dock = DockStyle.Top,
                BackColor = bgHeader,
                Padding = new Padding(24, 12, 24, 12)
            };
            this.Controls.Add(panelHeader);

            lblBreadcrumb = new Label
            {
                Text = "Inicio / Panel",
                Font = new Font("Segoe UI", 8.8f, FontStyle.Regular),
                ForeColor = textGray,
                Location = new Point(24, 12),
                AutoSize = true
            };
            panelHeader.Controls.Add(lblBreadcrumb);

            lblHeaderTitle = new Label
            {
                Text = "Panel General",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = textWhite,
                Location = new Point(22, 30),
                AutoSize = true
            };
            panelHeader.Controls.Add(lblHeaderTitle);

            Panel cardUser = new Panel
            {
                Size = new Size(210, 48),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = bgCard,
                Location = new Point(panelHeader.Width - 370, 17)
            };
            AplicarBordesRedondeados(cardUser, 14);

            Label lblUser = new Label
            {
                Text = $"👤  {nombreUsuario}\n    Rol: {rolUsuario}",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = textWhite,
                Location = new Point(12, 8),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            cardUser.Controls.Add(lblUser);
            panelHeader.Controls.Add(cardUser);

            Button btnCerrarSesion = new Button
            {
                Text = "Cerrar Sesión",
                Size = new Size(116, 40),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.FromArgb(239, 68, 68),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Location = new Point(panelHeader.Width - 140, 20)
            };
            btnCerrarSesion.FlatAppearance.BorderSize = 0;
            AplicarBordesRedondeados(btnCerrarSesion, 12);
            btnCerrarSesion.Click += BtnCerrarSesion_Click;
            panelHeader.Controls.Add(btnCerrarSesion);

            btnCerrarSesion.MouseEnter += (s, e) => btnCerrarSesion.BackColor = Color.FromArgb(220, 38, 38);
            btnCerrarSesion.MouseLeave += (s, e) => btnCerrarSesion.BackColor = Color.FromArgb(239, 68, 68);

            panelHeader.Resize += (s, e) =>
            {
                btnCerrarSesion.Location = new Point(panelHeader.Width - 140, 20);
                cardUser.Location = new Point(panelHeader.Width - 365, 16);
            };
        }

        // =========================================================
        // SIDEBAR DERECHO - NOTIFICACIONES
        // =========================================================
        private void CrearRightSidebar()
        {
            panelRightSidebar = new Panel
            {
                Width = 300,
                Dock = DockStyle.Right,
                BackColor = bgSidebar,
                Padding = new Padding(14),
                AutoScroll = true
            };
            this.Controls.Add(panelRightSidebar);

            Label lblNotifTitle = new Label
            {
                Text = "Resumen del almacén",
                Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
                ForeColor = textWhite,
                Location = new Point(14, 18),
                AutoSize = true
            };
            panelRightSidebar.Controls.Add(lblNotifTitle);

            Label lblNotifSub = new Label
            {
                Text = "Indicadores conectados a Artemusa TV",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = textGray,
                Location = new Point(15, 43),
                AutoSize = true
            };
            panelRightSidebar.Controls.Add(lblNotifSub);

            int top = 78;

            Panel tarjetaMovimientos = CrearTarjetaNotificacion(
                "↔", "Movimientos de hoy", "Consultando actividad…", accentBlue, top);
            lblSidebarMovimientosHoy = tarjetaMovimientos.Controls.Find("lblDetalle", false).Length > 0
                ? tarjetaMovimientos.Controls.Find("lblDetalle", false)[0] as Label
                : null;
            panelRightSidebar.Controls.Add(tarjetaMovimientos);

            top += 90;

            Panel tarjetaResponsables = CrearTarjetaNotificacion(
                "👥", "Responsables activos", "Consultando directorio…", accentPurple, top);
            lblSidebarResponsables = tarjetaResponsables.Controls.Find("lblDetalle", false).Length > 0
                ? tarjetaResponsables.Controls.Find("lblDetalle", false)[0] as Label
                : null;
            panelRightSidebar.Controls.Add(tarjetaResponsables);

            top += 90;
        }

        private Panel CrearTarjetaNotificacion(
            string icono,
            string titulo,
            string detalle,
            Color colorAcento,
            int top)
        {
            Panel card = new Panel
            {
                Size = new Size(268, 78),
                Location = new Point(14, top),
                BackColor = bgCard
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
                Font = new Font("Segoe UI Emoji", 13f),
                ForeColor = textWhite,
                Location = new Point(14, 18),
                Size = new Size(30, 30),
                TextAlign = ContentAlignment.MiddleCenter
            };
            card.Controls.Add(lblIcon);

            Label lblTitle = new Label
            {
                Text = titulo,
                Font = new Font("Segoe UI", 8.8f, FontStyle.Bold),
                ForeColor = textWhite,
                Location = new Point(50, 13),
                Size = new Size(205, 21),
                AutoEllipsis = true
            };
            card.Controls.Add(lblTitle);

            Label lblDetail = new Label
            {
                Name = "lblDetalle",
                Text = detalle,
                Font = new Font("Segoe UI", 8f),
                ForeColor = textGray,
                Location = new Point(50, 37),
                Size = new Size(205, 31)
            };
            card.Controls.Add(lblDetail);

            return card;
        }

        // =========================================================
        // PANEL CENTRAL
        // =========================================================
        private void CrearMainContent()
        {
            panelMainContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = bgDark,
                AutoScroll = true,
                // Menos margen lateral para aprovechar mejor el área al contraer el menú.
                Padding = new Padding(16, 12, 16, 18)
            };
            this.Controls.Add(panelMainContent);
            panelMainContent.BringToFront();
        }

        // =========================================================
        // NAVEGACIÓN
        // =========================================================
        private void LimpiarContenidoActual()
        {
            // Cada página se vuelve a crear al navegar. Liberar la anterior evita
            // acumular controles y recursos al entrar y salir del directorio.
            if (dgvOverviewMovimientos != null)
                dgvOverviewMovimientos.DataSource = null;

            if (tablaOverviewMovimientos != null)
            {
                tablaOverviewMovimientos.Dispose();
                tablaOverviewMovimientos = null;
            }

            dgvOverviewMovimientos = null;
            chartOverviewCategorias = null;
            chartOverviewMovimientos = null;
            chartOverviewEstados = null;

            while (panelMainContent.Controls.Count > 0)
            {
                Control anterior = panelMainContent.Controls[0];
                panelMainContent.Controls.RemoveAt(0);
                anterior.Dispose();
            }
        }

        private void MostrarOverview()
        {
            lblBreadcrumb.Text = "Inicio / Panel";
            lblHeaderTitle.Text = "Panel de control";
            ActivarBoton(btnOverviewRef);

            if (panelRightSidebar != null) panelRightSidebar.Visible = true;

            LimpiarContenidoActual();
            CargarContenidoOverview();
        }

        private void MostrarInventario()
        {
            lblBreadcrumb.Text = "Inicio / Inventario";
            lblHeaderTitle.Text = "Inventario";
            ActivarBoton(btnInventarioRef);

            if (panelRightSidebar != null) panelRightSidebar.Visible = false;

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
            ActivarBoton(btnMovimientosRef);

            if (panelRightSidebar != null) panelRightSidebar.Visible = false;

            LimpiarContenidoActual();

            var ucMovimientos = new SUSTENTACION.PanelMovimiento.PanelMovimiento
            {
                Dock = DockStyle.Fill
            };
            panelMainContent.Controls.Add(ucMovimientos);
            ucMovimientos.BringToFront();
        }

        private void MostrarTrabajadores()
        {
            lblBreadcrumb.Text = "Inicio / Trabajadores";
            lblHeaderTitle.Text = "Trabajadores";
            ActivarBoton(btnTrabajadoresRef);
            if (panelRightSidebar != null) panelRightSidebar.Visible = false;
            LimpiarContenidoActual();

            var trabajadores = new global::SUSTENTACION.PanelTrabajador.Trabajador
            {
                Dock = DockStyle.Fill
            };
            panelMainContent.Controls.Add(trabajadores);
            trabajadores.BringToFront();
        }

        private void MostrarReportes()
        {
            lblBreadcrumb.Text = "Inicio / Reportes";
            lblHeaderTitle.Text = "Reportes";
            ActivarBoton(btnReportesRef);
            if (panelRightSidebar != null) panelRightSidebar.Visible = false;
            LimpiarContenidoActual();
            var reportes = new global::SUSTENTACION.PanelAdministrador.CotrolReporte
            {
                Dock = DockStyle.Fill
            };
            panelMainContent.Controls.Add(reportes);
            reportes.BringToFront();
        }

        // =========================================================
        // PANEL GENERAL DE INVENTARIO AUDIOVISUAL
        // =========================================================
        private void CargarContenidoOverview()
        {
            FlowLayoutPanel contenido = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(0, 0, 8, 16),
                Margin = new Padding(0),
                BackColor = Color.Transparent
            };
            panelMainContent.Controls.Add(contenido);

            Panel cabecera = CrearCabeceraOverview();
            Panel filtros = CrearFiltrosOverview();
            TableLayoutPanel indicadores = CrearIndicadoresOverview();
            TableLayoutPanel graficos = CrearGraficosOverview();
            Panel graficoCategorias = CrearGraficoCategoriasOverview();
            Panel historial = CrearHistorialOverview();

            contenido.Controls.Add(cabecera);
            contenido.Controls.Add(filtros);
            contenido.Controls.Add(indicadores);
            contenido.Controls.Add(graficos);
            contenido.Controls.Add(graficoCategorias);
            contenido.Controls.Add(historial);

            Action ajustarAnchos = () =>
            {
                int ancho = Math.Max(460, contenido.ClientSize.Width - 24);
                foreach (Control control in contenido.Controls)
                    control.Width = ancho;
            };
            contenido.Resize += (s, e) => ajustarAnchos();
            ajustarAnchos();

            CargarDatosOverview();
        }

        private Panel CrearCabeceraOverview()
        {
            Panel panel = new Panel
            {
                Height = 126,
                BackColor = bgCard,
                Margin = new Padding(0, 0, 0, 12),
                Padding = new Padding(18)
            };
            AplicarBordesRedondeados(panel, 16);

            Label sobrelinea = new Label
            {
                Text = "ARTEMUSA TV  /  INVENTARIO AUDIOVISUAL",
                Location = new Point(20, 12),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.2f, FontStyle.Bold),
                ForeColor = accentGreen,
                BackColor = Color.Transparent
            };
            Label titulo = new Label
            {
                Text = "Panel de control",
                Location = new Point(18, 33),
                AutoSize = true,
                Font = new Font("Segoe UI", 19f, FontStyle.Bold),
                ForeColor = textWhite,
                BackColor = Color.Transparent
            };
            Label subtitulo = new Label
            {
                Text = "Resumen del almacén, existencias y actividad de los equipos.",
                Location = new Point(20, 72),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.7f),
                ForeColor = textGray,
                BackColor = Color.Transparent
            };
            panel.Controls.Add(sobrelinea);
            panel.Controls.Add(titulo);
            panel.Controls.Add(subtitulo);

            Button btnVerInventario = CrearBotonOverview("Ver inventario", accentGreen, Color.Black, 126);
            Button btnActualizar = CrearBotonOverview("Actualizar", bgInput, textWhite, 104);
            btnVerInventario.Click += (s, e) => MostrarInventario();
            btnActualizar.Click += (s, e) => CargarDatosOverview();
            panel.Controls.Add(btnVerInventario);
            panel.Controls.Add(btnActualizar);

            lblOverviewConexion = new Label
            {
                Text = "● Conectando con la base de datos…",
                Location = new Point(20, 99),
                AutoSize = true,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = accentOrange,
                BackColor = Color.Transparent,
                AutoEllipsis = true
            };
            lblOverviewActualizado = new Label
            {
                Text = "",
                AutoSize = true,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Font = new Font("Segoe UI", 8f),
                ForeColor = textGray,
                BackColor = Color.Transparent
            };
            panel.Controls.Add(lblOverviewConexion);
            panel.Controls.Add(lblOverviewActualizado);

            Action reubicar = () =>
            {
                btnVerInventario.Location = new Point(Math.Max(16, panel.ClientSize.Width - btnVerInventario.Width - 18), 32);
                btnActualizar.Location = new Point(Math.Max(16, btnVerInventario.Left - btnActualizar.Width - 8), 32);
                lblOverviewActualizado.Location = new Point(
                    Math.Max(20, panel.ClientSize.Width - lblOverviewActualizado.Width - 18), 100);
                lblOverviewConexion.MaximumSize = new Size(Math.Max(200, panel.ClientSize.Width - 280), 22);
            };
            panel.Resize += (s, e) => reubicar();
            reubicar();
            return panel;
        }

        private Panel CrearFiltrosOverview()
        {
            Panel panel = new Panel
            {
                Height = 194,
                BackColor = bgCard,
                Margin = new Padding(0, 0, 0, 12),
                Padding = new Padding(12)
            };
            AplicarBordesRedondeados(panel, 14);
            Label titulo = new Label
            {
                Text = "Filtros del panel",
                Location = new Point(16, 11),
                AutoSize = true,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = textWhite,
                BackColor = Color.Transparent
            };
            Label ayuda = new Label
            {
                Text = "Fechas, tipo y responsable filtran movimientos; categoría, estado y ubicación acotan equipos.",
                Location = new Point(150, 13),
                AutoSize = true,
                Font = new Font("Segoe UI", 8f),
                ForeColor = textGray,
                BackColor = Color.Transparent
            };
            panel.Controls.Add(titulo);
            panel.Controls.Add(ayuda);

            dtOverviewDesde = new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd/MM/yyyy",
                Value = DateTime.Today.AddMonths(-5),
                Font = new Font("Segoe UI", 8.4f),
                Height = 28
            };
            dtOverviewHasta = new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd/MM/yyyy",
                Value = DateTime.Today,
                Font = new Font("Segoe UI", 8.4f),
                Height = 28
            };
            cmbOverviewCategoria = CrearComboOverview();
            cmbOverviewEstado = CrearComboOverview();
            cmbOverviewUbicacion = CrearComboOverview();
            cmbOverviewTipoMovimiento = CrearComboOverview();
            cmbOverviewResponsable = CrearComboOverview();
            cmbOverviewTipoMovimiento.Items.AddRange(new object[]
            {
                "Todos", "Ingreso", "Salida", "Traslado", "Devolución", "Mantenimiento"
            });
            cmbOverviewTipoMovimiento.SelectedIndex = 0;

            txtOverviewBuscar = new TextBox
            {
                Font = new Font("Segoe UI", 8.6f),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = bgInput,
                ForeColor = textWhite,
                Height = 28
            };
            txtOverviewBuscar.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    AplicarFiltrosOverview();
                }
            };
            nudOverviewUmbral = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 999999,
                Value = 2,
                Font = new Font("Segoe UI", 8.6f),
                BackColor = bgInput,
                ForeColor = textWhite,
                Height = 28,
                ThousandsSeparator = true
            };

            TableLayoutPanel rejilla = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 130,
                ColumnCount = 5,
                RowCount = 2,
                Padding = new Padding(8, 3, 8, 5),
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };
            rejilla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16f));
            rejilla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16f));
            rejilla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
            rejilla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 17f));
            rejilla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 31f));
            rejilla.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            rejilla.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            rejilla.Controls.Add(CrearCampoOverview("Desde", dtOverviewDesde), 0, 0);
            rejilla.Controls.Add(CrearCampoOverview("Hasta", dtOverviewHasta), 1, 0);
            rejilla.Controls.Add(CrearCampoOverview("Categoría", cmbOverviewCategoria), 2, 0);
            rejilla.Controls.Add(CrearCampoOverview("Estado", cmbOverviewEstado), 3, 0);
            rejilla.Controls.Add(CrearCampoOverview("Ubicación", cmbOverviewUbicacion), 4, 0);
            rejilla.Controls.Add(CrearCampoOverview("Tipo de movimiento", cmbOverviewTipoMovimiento), 0, 1);
            rejilla.Controls.Add(CrearCampoOverview("Responsable", cmbOverviewResponsable), 1, 1);

            Panel campoBuscar = CrearCampoOverview("Buscar equipo, código o responsable", txtOverviewBuscar);
            rejilla.Controls.Add(campoBuscar, 2, 1);
            rejilla.SetColumnSpan(campoBuscar, 2);

            Panel acciones = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Margin = new Padding(4)
            };
            Label etiquetaUmbral = new Label
            {
                Text = "Condición",
                Location = new Point(2, 1),
                AutoSize = true,
                Font = new Font("Segoe UI", 7.2f, FontStyle.Bold),
                ForeColor = textGray,
                BackColor = Color.Transparent
            };
            nudOverviewUmbral.Width = 57;
            nudOverviewUmbral.Location = new Point(2, 24);
            btnOverviewAplicar = CrearBotonOverview("Aplicar", accentGreen, Color.Black, 78);
            Button btnLimpiar = CrearBotonOverview("Limpiar", bgInput, textWhite, 64);
            btnOverviewAplicar.Click += (s, e) => AplicarFiltrosOverview();
            btnLimpiar.Click += (s, e) => LimpiarFiltrosOverview();
            Action ajustarAcciones = () =>
            {
                int anchoPanel = acciones.ClientSize.Width;
                int anchoLimpiar = Math.Min(64, Math.Max(54, anchoPanel / 4));
                int anchoAplicar = Math.Min(78, Math.Max(62, anchoPanel / 3));
                btnLimpiar.Width = anchoLimpiar;
                btnLimpiar.Left = Math.Max(2, anchoPanel - anchoLimpiar - 2);
                btnOverviewAplicar.Width = anchoAplicar;
                btnOverviewAplicar.Left = Math.Max(55, btnLimpiar.Left - anchoAplicar - 5);
                nudOverviewUmbral.Width = Math.Max(42, btnOverviewAplicar.Left - 10);
                nudOverviewUmbral.Left = 2;
                nudOverviewUmbral.Top = 24;
                btnOverviewAplicar.Top = 21;
                btnLimpiar.Top = 21;
            };
            acciones.Resize += (s, e) => ajustarAcciones();
            acciones.Controls.Add(etiquetaUmbral);
            acciones.Controls.Add(nudOverviewUmbral);
            acciones.Controls.Add(btnOverviewAplicar);
            acciones.Controls.Add(btnLimpiar);
            ajustarAcciones();
            rejilla.Controls.Add(acciones, 4, 1);

            panel.Controls.Add(rejilla);
            return panel;
        }

        private TableLayoutPanel CrearIndicadoresOverview()
        {
            TableLayoutPanel tabla = new TableLayoutPanel
            {
                Height = 118,
                ColumnCount = 5,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 12),
                Padding = new Padding(0)
            };
            for (int i = 0; i < 5; i++)
            {
                tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
                tabla.Controls.Add(CrearTarjetaIndicadorOverview(i), i, 0);
            }
            tabla.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            return tabla;
        }

        private Panel CrearTarjetaIndicadorOverview(int indice)
        {
            Color[] colores =
{
    accentBlue,
    accentGreen,
    accentOrange,
    accentPurple,
    accentPink
};

            string[] titulos =
            {
    "EQUIPOS REGISTRADOS",
    "DISPONIBLES",
    "ASIGNADOS",
    "EN MANTENIMIENTO",
    "MOVIMIENTOS DEL PERÍODO"
};

            string[] detalles =
            {
    "Total de equipos registrados",
    "Equipos disponibles actualmente",
    "Equipos con asignación activa",
    "Equipos actualmente en mantenimiento",
    "Movimientos según fechas y filtros"
};


            Panel tarjeta = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(5, 0, 5, 0),
                BackColor = bgCard,
                Padding = new Padding(12)
            };
            AplicarBordesRedondeados(tarjeta, 13);
            Panel acento = new Panel
            {
                Dock = DockStyle.Top,
                Height = 3,
                BackColor = colores[indice]
            };
            Label titulo = new Label
            {
                Text = titulos[indice],
                Location = new Point(12, 12),
                AutoSize = false,
                Size = new Size(160, 18),
                Font = new Font("Segoe UI", 7.1f, FontStyle.Bold),
                ForeColor = textGray,
                AutoEllipsis = true,
                BackColor = Color.Transparent
            };
            valoresOverview[indice] = new Label
            {
                Text = "—",
                Location = new Point(11, 33),
                AutoSize = false,
                Size = new Size(165, 38),
                Font = new Font("Segoe UI", 21f, FontStyle.Bold),
                ForeColor = textWhite,
                AutoEllipsis = true,
                BackColor = Color.Transparent
            };
            detallesOverview[indice] = new Label
            {
                Text = detalles[indice],
                Location = new Point(12, 77),
                AutoSize = false,
                Size = new Size(165, 20),
                Font = new Font("Segoe UI", 7.5f),
                ForeColor = textGray,
                AutoEllipsis = true,
                BackColor = Color.Transparent
            };
            tarjeta.Resize += (s, e) =>
            {
                int anchoTexto = Math.Max(55, tarjeta.ClientSize.Width - 24);
                titulo.Width = anchoTexto;
                valoresOverview[indice].Width = anchoTexto;
                detallesOverview[indice].Width = anchoTexto;
            };
            tarjeta.Controls.Add(acento);
            tarjeta.Controls.Add(titulo);
            tarjeta.Controls.Add(valoresOverview[indice]);
            tarjeta.Controls.Add(detallesOverview[indice]);
            return tarjeta;
        }

        private TableLayoutPanel CrearGraficosOverview()
        {
            TableLayoutPanel tabla = new TableLayoutPanel
            {
                Height = 254,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 12),
                Padding = new Padding(0)
            };
            tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62f));
            tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38f));
            tabla.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            chartOverviewMovimientos = new OverviewMonthlyChart
            {
                Dock = DockStyle.Fill,
                BackColor = bgCard,
                ForeColor = textSoft,
                AccentColor = accentBlue,
                Margin = new Padding(5, 0, 5, 0)
            };
            chartOverviewEstados = new OverviewDonutChart
            {
                Dock = DockStyle.Fill,
                BackColor = bgCard,
                ForeColor = textSoft,
                Margin = new Padding(5, 0, 5, 0)
            };
            tabla.Controls.Add(CrearTarjetaGraficoOverview("Movimientos registrados por mes y tipo", chartOverviewMovimientos), 0, 0);
            tabla.Controls.Add(CrearTarjetaGraficoOverview("Estado de los equipos", chartOverviewEstados), 1, 0);
            return tabla;
        }

        private Panel CrearGraficoCategoriasOverview()
        {
            Panel tarjeta = new Panel
            {
                Height = 220,
                BackColor = bgCard,
                Margin = new Padding(0, 0, 0, 12),
                Padding = new Padding(12)
            };
            AplicarBordesRedondeados(tarjeta, 13);
            Label titulo = new Label
            {
                Text = "Unidades por categoría audiovisual · principales 6",
                Location = new Point(14, 11),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.6f, FontStyle.Bold),
                ForeColor = textWhite,
                BackColor = Color.Transparent
            };
            chartOverviewCategorias = new OverviewBarChart
            {
                Location = new Point(12, 42),
                Size = new Size(500, 160),
                BackColor = bgCard,
                ForeColor = textSoft,
                BarColor = accentGreen,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Margin = new Padding(0)
            };
            tarjeta.Controls.Add(titulo);
            tarjeta.Controls.Add(chartOverviewCategorias);
            tarjeta.Resize += (s, e) => chartOverviewCategorias.Size = new Size(
                Math.Max(160, tarjeta.ClientSize.Width - 24), Math.Max(110, tarjeta.ClientSize.Height - 52));
            return tarjeta;
        }

        private Panel CrearTarjetaGraficoOverview(string titulo, Control grafico)
        {
            Panel panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = bgCard,
                Margin = new Padding(5, 0, 5, 0),
                Padding = new Padding(12)
            };
            AplicarBordesRedondeados(panel, 13);
            Label etiqueta = new Label
            {
                Text = titulo,
                Location = new Point(14, 11),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.6f, FontStyle.Bold),
                ForeColor = textWhite,
                BackColor = Color.Transparent
            };
            grafico.Location = new Point(10, 40);
            grafico.Size = new Size(300, 195);
            panel.Controls.Add(etiqueta);
            panel.Controls.Add(grafico);
            panel.Resize += (s, e) => grafico.Size = new Size(
                Math.Max(120, panel.ClientSize.Width - 20),
                Math.Max(100, panel.ClientSize.Height - 50));
            return panel;
        }

        private Panel CrearHistorialOverview()
        {
            Panel panel = new Panel
            {
                Height = 356,
                BackColor = bgCard,
                Margin = new Padding(0, 0, 0, 14),
                Padding = new Padding(12)
            };
            AplicarBordesRedondeados(panel, 13);
            Label titulo = new Label
            {
                Text = "Movimientos recientes",
                Location = new Point(16, 11),
                AutoSize = true,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = textWhite,
                BackColor = Color.Transparent
            };
            Label subtitulo = new Label
            {
                Text = "Últimos 100 registros que coinciden con los filtros.",
                Location = new Point(17, 34),
                AutoSize = true,
                Font = new Font("Segoe UI", 8f),
                ForeColor = textGray,
                BackColor = Color.Transparent
            };
            Button verMovimientos = CrearBotonOverview("Abrir movimientos", bgInput, textWhite, 146);
            verMovimientos.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            verMovimientos.Click += (s, e) => MostrarMovimientos();
            panel.Controls.Add(titulo);
            panel.Controls.Add(subtitulo);
            panel.Controls.Add(verMovimientos);

            dgvOverviewMovimientos = new DataGridView
            {
                Name = "dgvOverviewMovimientos",
                Location = new Point(12, 64),
                Size = new Size(500, 276),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoGenerateColumns = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                BorderStyle = BorderStyle.None,
                BackgroundColor = bgCard,
                GridColor = borderColor,
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 37,
                RowTemplate = { Height = 34 },
                Font = new Font("Segoe UI", 8.2f)
            };
            dgvOverviewMovimientos.ColumnHeadersDefaultCellStyle.BackColor = bgInput;
            dgvOverviewMovimientos.ColumnHeadersDefaultCellStyle.ForeColor = accentGreen;
            dgvOverviewMovimientos.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8f, FontStyle.Bold);
            dgvOverviewMovimientos.DefaultCellStyle.BackColor = bgCard;
            dgvOverviewMovimientos.DefaultCellStyle.ForeColor = textWhite;
            dgvOverviewMovimientos.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(25, 31, 39);
            dgvOverviewMovimientos.CellDoubleClick += (s, e) => MostrarMovimientos();
            panel.Controls.Add(dgvOverviewMovimientos);
            panel.Resize += (s, e) =>
            {
                verMovimientos.Location = new Point(Math.Max(12, panel.ClientSize.Width - verMovimientos.Width - 14), 12);
                dgvOverviewMovimientos.Width = Math.Max(180, panel.ClientSize.Width - 24);
                dgvOverviewMovimientos.Height = Math.Max(100, panel.ClientSize.Height - 76);
            };
            AplicarEstiloGrid(dgvOverviewMovimientos);
            return panel;
        }

        private Panel CrearCampoOverview(string titulo, Control control)
        {
            Panel campo = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(4),
                BackColor = Color.Transparent
            };
            Label etiqueta = new Label
            {
                Text = titulo,
                Dock = DockStyle.Top,
                Height = 19,
                Font = new Font("Segoe UI", 7.2f, FontStyle.Bold),
                ForeColor = textGray,
                BackColor = Color.Transparent,
                AutoEllipsis = true
            };
            control.Dock = DockStyle.Bottom;
            control.Height = 29;
            campo.Controls.Add(control);
            campo.Controls.Add(etiqueta);
            return campo;
        }

        private ComboBox CrearComboOverview()
        {
            return new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.4f),
                BackColor = bgInput,
                ForeColor = textWhite,
                IntegralHeight = false,
                MaxDropDownItems = 12
            };
        }

        private Button CrearBotonOverview(string texto, Color fondo, Color textoColor, int ancho)
        {
            Button boton = new Button
            {
                Text = texto,
                Size = new Size(ancho, 34),
                BackColor = fondo,
                ForeColor = textoColor,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.3f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(3)
            };
            boton.FlatAppearance.BorderSize = fondo == bgInput ? 1 : 0;
            boton.FlatAppearance.BorderColor = borderColor;
            boton.FlatAppearance.MouseOverBackColor = fondo == accentGreen ? Color.FromArgb(190, 242, 100) : bgCardHover;
            AplicarBordesRedondeados(boton, 9);
            return boton;
        }

        private void AplicarFiltrosOverview()
        {
            if (dtOverviewDesde != null && dtOverviewHasta != null && dtOverviewDesde.Value.Date > dtOverviewHasta.Value.Date)
            {
                MessageBox.Show("La fecha Desde no puede ser posterior a la fecha Hasta.",
                    "Revisar filtros", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            CargarDatosOverview();
        }

        private void LimpiarFiltrosOverview()
        {
            if (dtOverviewDesde != null) dtOverviewDesde.Value = DateTime.Today.AddMonths(-5);
            if (dtOverviewHasta != null) dtOverviewHasta.Value = DateTime.Today;
            if (cmbOverviewCategoria != null && cmbOverviewCategoria.Items.Count > 0) cmbOverviewCategoria.SelectedIndex = 0;
            if (cmbOverviewEstado != null && cmbOverviewEstado.Items.Count > 0) cmbOverviewEstado.SelectedIndex = 0;
            if (cmbOverviewUbicacion != null && cmbOverviewUbicacion.Items.Count > 0) cmbOverviewUbicacion.SelectedIndex = 0;
            if (cmbOverviewTipoMovimiento != null && cmbOverviewTipoMovimiento.Items.Count > 0) cmbOverviewTipoMovimiento.SelectedIndex = 0;
            if (cmbOverviewResponsable != null && cmbOverviewResponsable.Items.Count > 0) cmbOverviewResponsable.SelectedIndex = 0;
            if (txtOverviewBuscar != null) txtOverviewBuscar.Clear();
            if (nudOverviewUmbral != null) nudOverviewUmbral.Value = 2;
            CargarDatosOverview();
        }

        private void CargarDatosOverview()
        {
            if (cargandoOverview || panelMainContent == null || panelMainContent.IsDisposed)
                return;

            cargandoOverview = true;
            if (btnOverviewAplicar != null) btnOverviewAplicar.Enabled = false;

            DataTable equiposCategoria = new DataTable();
            DataTable estadosEquipo = new DataTable();
            DataTable movimientosMensuales = new DataTable();
            DataTable movimientosRecientes = new DataTable();
            try
            {
                int totalEquipos;
                long totalUnidades;
                int equiposBajoUmbral;
                int totalMovimientos;
                int responsablesActivos;
                int bajoUmbralGlobal;
                int movimientosHoy;
                int responsablesActivosGlobal;

                using (MySqlConnection conexion = new Conexion().ObtenerConexion())
                {
                    if (conexion == null)
                        throw new InvalidOperationException("La conexión configurada no devolvió una conexión válida.");
                    if (conexion.State != ConnectionState.Open)
                        conexion.Open();

                    PoblarComboOverview(conexion, cmbOverviewCategoria,
                        "SELECT DISTINCT COALESCE(NULLIF(TRIM(categoria), ''), 'Sin categoría') FROM equipos ORDER BY 1",
                        "Todas");
                    PoblarComboOverview(conexion, cmbOverviewEstado,
                        "SELECT DISTINCT COALESCE(NULLIF(TRIM(estado), ''), 'Sin estado') FROM equipos ORDER BY 1",
                        "Todos");
                    PoblarComboOverview(conexion, cmbOverviewUbicacion,
                        "SELECT DISTINCT COALESCE(NULLIF(TRIM(ubicacion), ''), 'Sin ubicación') FROM equipos ORDER BY 1",
                        "Todas");
                    PoblarResponsablesOverview(conexion);

                    string whereEquipos = @"
WHERE (@categoria IS NULL OR e.categoria = @categoria)
  AND (@estado IS NULL OR e.estado = @estado)
  AND (@ubicacion IS NULL OR e.ubicacion = @ubicacion)
  AND (@buscar = '' OR CONCAT_WS(' ', e.codigo, e.nombre, e.categoria, e.marca, e.modelo, e.ubicacion) LIKE @buscar)";
                    using (MySqlCommand comando = new MySqlCommand(@"
SELECT COUNT(*) AS total,
       COALESCE(SUM(e.stock), 0) AS unidades,
       COALESCE(SUM(CASE WHEN e.stock <= @umbral THEN 1 ELSE 0 END), 0) AS bajo
FROM equipos e " + whereEquipos, conexion))
                    {
                        AgregarParametrosEquipoOverview(comando);
                        comando.Parameters.AddWithValue("@umbral", ObtenerUmbralOverview());
                        using (MySqlDataReader lector = comando.ExecuteReader())
                        {
                            if (lector.Read())
                            {
                                totalEquipos = Convert.ToInt32(lector["total"]);
                                totalUnidades = Convert.ToInt64(lector["unidades"]);
                                equiposBajoUmbral = Convert.ToInt32(lector["bajo"]);
                            }
                            else
                            {
                                totalEquipos = 0;
                                totalUnidades = 0;
                                equiposBajoUmbral = 0;
                            }
                        }
                    }

                    using (MySqlCommand comando = new MySqlCommand(@"
SELECT COALESCE(NULLIF(TRIM(e.categoria), ''), 'Sin categoría') AS etiqueta,
       COALESCE(SUM(e.stock), 0) AS total
FROM equipos e " + whereEquipos + @"
GROUP BY COALESCE(NULLIF(TRIM(e.categoria), ''), 'Sin categoría')
ORDER BY total DESC, etiqueta
LIMIT 8", conexion))
                    {
                        AgregarParametrosEquipoOverview(comando);
                        using (MySqlDataAdapter adaptador = new MySqlDataAdapter(comando))
                            adaptador.Fill(equiposCategoria);
                    }

                    using (MySqlCommand comando = new MySqlCommand(@"
SELECT COALESCE(NULLIF(TRIM(e.estado), ''), 'Sin estado') AS etiqueta,
       COUNT(*) AS total
FROM equipos e " + whereEquipos + @"
GROUP BY COALESCE(NULLIF(TRIM(e.estado), ''), 'Sin estado')
ORDER BY total DESC, etiqueta", conexion))
                    {
                        AgregarParametrosEquipoOverview(comando);
                        using (MySqlDataAdapter adaptador = new MySqlDataAdapter(comando))
                            adaptador.Fill(estadosEquipo);
                    }

                    string whereMovimientos = @"
WHERE m.fecha_movimiento >= @desde AND m.fecha_movimiento < @hasta
  AND (@categoria IS NULL OR e.categoria = @categoria)
  AND (@estado IS NULL OR e.estado = @estado)
  AND (@ubicacion IS NULL OR e.ubicacion = @ubicacion)
  AND (@tipo IS NULL OR m.tipo_movimiento = @tipo)
  AND (@responsable IS NULL OR m.id_responsable = @responsable)
  AND (@buscar = '' OR CONCAT_WS(' ', e.codigo, e.nombre, e.categoria, e.marca, e.modelo,
       e.ubicacion, r.nombre, r.apellido, u.nombre, u.apellido, m.motivo, m.observacion) LIKE @buscar)";
                    using (MySqlCommand comando = new MySqlCommand(@"
SELECT COUNT(*)
FROM movimiento m
INNER JOIN equipos e ON e.id = m.id_equipo
LEFT JOIN responsable r ON r.id_responsable = m.id_responsable
LEFT JOIN usuario u ON u.id_usuario = COALESCE(m.id_usuario, m.id_trabajador) " + whereMovimientos, conexion))
                    {
                        AgregarParametrosMovimientoOverview(comando);
                        totalMovimientos = Convert.ToInt32(comando.ExecuteScalar());
                    }

                    using (MySqlCommand comando = new MySqlCommand(@"
SELECT DATE_FORMAT(m.fecha_movimiento, '%Y-%m') AS periodo,
       m.tipo_movimiento AS tipo,
       COUNT(*) AS total
FROM movimiento m
INNER JOIN equipos e ON e.id = m.id_equipo
LEFT JOIN responsable r ON r.id_responsable = m.id_responsable
LEFT JOIN usuario u ON u.id_usuario = COALESCE(m.id_usuario, m.id_trabajador) " + whereMovimientos + @"
GROUP BY YEAR(m.fecha_movimiento), MONTH(m.fecha_movimiento), m.tipo_movimiento
ORDER BY YEAR(m.fecha_movimiento), MONTH(m.fecha_movimiento), m.tipo_movimiento", conexion))
                    {
                        AgregarParametrosMovimientoOverview(comando);
                        using (MySqlDataAdapter adaptador = new MySqlDataAdapter(comando))
                            adaptador.Fill(movimientosMensuales);
                    }

                    using (MySqlCommand comando = new MySqlCommand(@"
SELECT DATE_FORMAT(m.fecha_movimiento, '%d/%m/%Y') AS Fecha,
       TIME_FORMAT(m.hora_movimiento, '%H:%i') AS Hora,
       CASE m.tipo_movimiento
         WHEN 'INGRESO' THEN 'Ingreso'
         WHEN 'SALIDA' THEN 'Salida'
         WHEN 'TRASLADO' THEN 'Traslado'
         WHEN 'DEVOLUCION' THEN 'Devolución'
         ELSE 'Mantenimiento'
       END AS Tipo,
       e.codigo AS Codigo,
       e.nombre AS Equipo,
       COALESCE(m.cantidad, 1) AS Cantidad,
       COALESCE(NULLIF(TRIM(CONCAT_WS(' ', NULLIF(r.nombre, ''), NULLIF(r.apellido, ''))), ''),
                NULLIF(TRIM(CONCAT_WS(' ', NULLIF(u.nombre, ''), NULLIF(u.apellido, ''))), ''),
                'Sin responsable') AS Responsable,
       COALESCE(NULLIF(m.ubicacion_destino, ''), NULLIF(m.ubicacion_origen, ''), e.ubicacion, '—') AS Ubicacion,
       COALESCE(NULLIF(m.motivo, ''), NULLIF(m.observacion, ''), '—') AS Motivo
FROM movimiento m
INNER JOIN equipos e ON e.id = m.id_equipo
LEFT JOIN responsable r ON r.id_responsable = m.id_responsable
LEFT JOIN usuario u ON u.id_usuario = COALESCE(m.id_usuario, m.id_trabajador) " + whereMovimientos + @"
ORDER BY m.fecha_movimiento DESC, m.hora_movimiento DESC, m.id_movimiento DESC
LIMIT 100", conexion))
                    {
                        AgregarParametrosMovimientoOverview(comando);
                        using (MySqlDataAdapter adaptador = new MySqlDataAdapter(comando))
                            adaptador.Fill(movimientosRecientes);
                    }

                    using (MySqlCommand comando = new MySqlCommand(
                        "SELECT COUNT(*) FROM responsable WHERE estado = 'Activo'", conexion))
                        responsablesActivos = Convert.ToInt32(comando.ExecuteScalar());

                    using (MySqlCommand comando = new MySqlCommand(
                        "SELECT COUNT(*) FROM equipos WHERE stock <= @umbral", conexion))
                    {
                        comando.Parameters.AddWithValue("@umbral", ObtenerUmbralOverview());
                        bajoUmbralGlobal = Convert.ToInt32(comando.ExecuteScalar());
                    }
                    using (MySqlCommand comando = new MySqlCommand(
                        "SELECT COUNT(*) FROM movimiento WHERE fecha_movimiento = CURDATE()", conexion))
                        movimientosHoy = Convert.ToInt32(comando.ExecuteScalar());
                    responsablesActivosGlobal = responsablesActivos;
                }

                valoresOverview[0].Text = totalEquipos.ToString("N0", CultureInfo.GetCultureInfo("es-MX"));
                valoresOverview[1].Text = totalUnidades.ToString("N0", CultureInfo.GetCultureInfo("es-MX"));
                valoresOverview[2].Text = equiposBajoUmbral.ToString("N0", CultureInfo.GetCultureInfo("es-MX"));
                valoresOverview[3].Text = totalMovimientos.ToString("N0", CultureInfo.GetCultureInfo("es-MX"));
                valoresOverview[4].Text = responsablesActivos.ToString("N0", CultureInfo.GetCultureInfo("es-MX"));
                detallesOverview[2].Text = "Stock ≤ " + ObtenerUmbralOverview().ToString("N0", CultureInfo.GetCultureInfo("es-MX"));
                detallesOverview[3].Text = dtOverviewDesde == null || dtOverviewHasta == null
                    ? "En el periodo seleccionado"
                    : dtOverviewDesde.Value.ToString("dd/MM/yyyy") + " – " + dtOverviewHasta.Value.ToString("dd/MM/yyyy");

                chartOverviewCategorias?.SetBars(CrearBarrasOverview(equiposCategoria), accentGreen);
                chartOverviewEstados?.SetData(CrearBarrasOverview(estadosEquipo));
                chartOverviewMovimientos?.SetData(CrearDatosMensualesOverview(movimientosMensuales));
                if (dgvOverviewMovimientos != null)
                {
                    DataTable? anterior = tablaOverviewMovimientos;
                    tablaOverviewMovimientos = movimientosRecientes;
                    dgvOverviewMovimientos.DataSource = tablaOverviewMovimientos;
                    if (anterior != null && !ReferenceEquals(anterior, movimientosRecientes))
                        anterior.Dispose();
                    AplicarEstiloGrid(dgvOverviewMovimientos);
                    ConfigurarColumnasOverview();
                }
                else
                {
                    movimientosRecientes.Dispose();
                }

                ActualizarResumenSidebar(bajoUmbralGlobal, movimientosHoy, responsablesActivosGlobal);
                if (lblOverviewConexion != null)
                {
                    lblOverviewConexion.Text = "● Conectado a artemusa_inventario";
                    lblOverviewConexion.ForeColor = accentGreen;
                }
                if (lblOverviewActualizado != null)
                    lblOverviewActualizado.Text = "Actualizado " + DateTime.Now.ToString("dd/MM/yyyy HH:mm");

                equiposCategoria.Dispose();
                estadosEquipo.Dispose();
                movimientosMensuales.Dispose();
            }
            catch (Exception ex)
            {
                equiposCategoria.Dispose();
                estadosEquipo.Dispose();
                movimientosMensuales.Dispose();
                if (lblOverviewConexion != null)
                {
                    lblOverviewConexion.Text = "● No se pudo consultar la base de datos: " + ex.Message;
                    lblOverviewConexion.ForeColor = accentOrange;
                }
                if (lblOverviewActualizado != null) lblOverviewActualizado.Text = "Comprueba la conexión y el esquema";
                foreach (Label valor in valoresOverview)
                    if (valor != null) valor.Text = "—";
                chartOverviewCategorias?.SetBars(new List<OverviewBar>(), accentGreen);
                chartOverviewEstados?.SetData(new List<OverviewBar>());
                chartOverviewMovimientos?.SetData(new List<OverviewMonthBucket>());
                if (dgvOverviewMovimientos != null)
                    dgvOverviewMovimientos.DataSource = null;
                bool tablaRecienteAsignada = ReferenceEquals(tablaOverviewMovimientos, movimientosRecientes);
                if (tablaOverviewMovimientos != null)
                {
                    tablaOverviewMovimientos.Dispose();
                    tablaOverviewMovimientos = null;
                }
                if (!tablaRecienteAsignada)
                    movimientosRecientes.Dispose();
            }
            finally
            {
                cargandoOverview = false;
                if (btnOverviewAplicar != null && !btnOverviewAplicar.IsDisposed)
                    btnOverviewAplicar.Enabled = true;
            }
        }

        private void PoblarComboOverview(MySqlConnection conexion, ComboBox? combo, string consulta, string opcionGeneral)
        {
            if (combo == null || combo.IsDisposed) return;
            string seleccionado = combo.SelectedItem == null ? opcionGeneral : Convert.ToString(combo.SelectedItem, CultureInfo.CurrentCulture) ?? opcionGeneral;
            combo.BeginUpdate();
            try
            {
                combo.Items.Clear();
                combo.Items.Add(opcionGeneral);
                using (MySqlCommand comando = new MySqlCommand(consulta, conexion))
                using (MySqlDataReader lector = comando.ExecuteReader())
                {
                    while (lector.Read())
                    {
                        string valor = Convert.ToString(lector[0], CultureInfo.CurrentCulture) ?? "";
                        if (!string.IsNullOrWhiteSpace(valor) && !combo.Items.Contains(valor))
                            combo.Items.Add(valor);
                    }
                }
                int indice = combo.Items.IndexOf(seleccionado);
                combo.SelectedIndex = indice >= 0 ? indice : 0;
            }
            finally
            {
                combo.EndUpdate();
            }
        }

        private void AgregarParametrosEquipoOverview(MySqlCommand comando)
        {
            comando.Parameters.AddWithValue("@categoria", (object?)LeerFiltroComboOverview(cmbOverviewCategoria, 0) ?? DBNull.Value);
            comando.Parameters.AddWithValue("@estado", (object?)LeerFiltroComboOverview(cmbOverviewEstado, 0) ?? DBNull.Value);
            comando.Parameters.AddWithValue("@ubicacion", (object?)LeerFiltroComboOverview(cmbOverviewUbicacion, 0) ?? DBNull.Value);
            string busqueda = txtOverviewBuscar == null ? "" : txtOverviewBuscar.Text.Trim();
            comando.Parameters.AddWithValue("@buscar", busqueda.Length == 0 ? "" : "%" + busqueda + "%");
        }

        private void AgregarParametrosMovimientoOverview(MySqlCommand comando)
        {
            comando.Parameters.AddWithValue("@desde", dtOverviewDesde == null ? DateTime.Today.AddMonths(-5) : dtOverviewDesde.Value.Date);
            comando.Parameters.AddWithValue("@hasta", (dtOverviewHasta == null ? DateTime.Today : dtOverviewHasta.Value.Date).AddDays(1));
            comando.Parameters.AddWithValue("@categoria", (object?)LeerFiltroComboOverview(cmbOverviewCategoria, 0) ?? DBNull.Value);
            comando.Parameters.AddWithValue("@estado", (object?)LeerFiltroComboOverview(cmbOverviewEstado, 0) ?? DBNull.Value);
            comando.Parameters.AddWithValue("@ubicacion", (object?)LeerFiltroComboOverview(cmbOverviewUbicacion, 0) ?? DBNull.Value);
            comando.Parameters.AddWithValue("@tipo", (object?)ObtenerTipoMovimientoFiltroOverview() ?? DBNull.Value);
            int? idResponsable = ObtenerResponsableFiltroOverview();
            comando.Parameters.AddWithValue("@responsable", idResponsable.HasValue ? (object)idResponsable.Value : DBNull.Value);
            string busqueda = txtOverviewBuscar == null ? "" : txtOverviewBuscar.Text.Trim();
            comando.Parameters.AddWithValue("@buscar", busqueda.Length == 0 ? "" : "%" + busqueda + "%");
        }

        private string? LeerFiltroComboOverview(ComboBox? combo, int indiceGeneral)
        {
            if (combo == null || combo.SelectedIndex <= indiceGeneral || combo.SelectedItem == null)
                return null;
            return Convert.ToString(combo.SelectedItem, CultureInfo.CurrentCulture);
        }

        private string? ObtenerTipoMovimientoFiltroOverview()
        {
            if (cmbOverviewTipoMovimiento == null || cmbOverviewTipoMovimiento.SelectedIndex <= 0)
                return null;
            switch (cmbOverviewTipoMovimiento.SelectedIndex)
            {
                case 1: return "INGRESO";
                case 2: return "SALIDA";
                case 3: return "TRASLADO";
                case 4: return "DEVOLUCION";
                case 5: return "MANTENIMIENTO";
                default: return null;
            }
        }

        private void PoblarResponsablesOverview(MySqlConnection conexion)
        {
            if (cmbOverviewResponsable == null || cmbOverviewResponsable.IsDisposed) return;
            int? idSeleccionado = ObtenerResponsableFiltroOverview();
            cmbOverviewResponsable.DisplayMember = nameof(OverviewResponsableOpcion.Nombre);
            cmbOverviewResponsable.BeginUpdate();
            try
            {
                cmbOverviewResponsable.Items.Clear();
                cmbOverviewResponsable.Items.Add(new OverviewResponsableOpcion(0, "Todos"));
                const string consulta = @"
SELECT id_responsable,
       TRIM(CONCAT_WS(' ', NULLIF(nombre, ''), NULLIF(apellido, ''))) AS nombre_completo,
       estado
FROM responsable
ORDER BY CASE WHEN estado = 'Activo' THEN 0 ELSE 1 END, apellido, nombre";
                using (MySqlCommand comando = new MySqlCommand(consulta, conexion))
                using (MySqlDataReader lector = comando.ExecuteReader())
                {
                    while (lector.Read())
                    {
                        int id = Convert.ToInt32(lector["id_responsable"]);
                        string nombre = Convert.ToString(lector["nombre_completo"], CultureInfo.CurrentCulture) ?? "Responsable";
                        string estado = Convert.ToString(lector["estado"], CultureInfo.CurrentCulture) ?? "";
                        if (!string.Equals(estado, "Activo", StringComparison.OrdinalIgnoreCase))
                            nombre += " (Inactivo)";
                        cmbOverviewResponsable.Items.Add(new OverviewResponsableOpcion(id, nombre));
                    }
                }
                int indice = 0;
                for (int i = 1; i < cmbOverviewResponsable.Items.Count; i++)
                {
                    OverviewResponsableOpcion opcion = (OverviewResponsableOpcion)cmbOverviewResponsable.Items[i];
                    if (idSeleccionado.HasValue && opcion.Id == idSeleccionado.Value)
                    {
                        indice = i;
                        break;
                    }
                }
                cmbOverviewResponsable.SelectedIndex = indice;
            }
            finally
            {
                cmbOverviewResponsable.EndUpdate();
            }
        }

        private int? ObtenerResponsableFiltroOverview()
        {
            if (cmbOverviewResponsable?.SelectedItem is OverviewResponsableOpcion opcion && opcion.Id > 0)
                return opcion.Id;
            return null;
        }

        private int ObtenerUmbralOverview()
        {
            return nudOverviewUmbral == null ? 2 : Decimal.ToInt32(nudOverviewUmbral.Value);
        }

        private List<OverviewBar> CrearBarrasOverview(DataTable datos, bool traducirTipo = false)
        {
            List<OverviewBar> barras = new List<OverviewBar>();
            foreach (DataRow fila in datos.Rows)
            {
                string etiqueta = Convert.ToString(fila["etiqueta"], CultureInfo.CurrentCulture) ?? "Sin dato";
                if (traducirTipo) etiqueta = TraducirTipoMovimientoOverview(etiqueta);
                barras.Add(new OverviewBar
                {
                    Label = etiqueta,
                    Value = Convert.ToInt32(fila["total"])
                });
            }
            return barras;
        }

        private List<OverviewMonthBucket> CrearDatosMensualesOverview(DataTable datos)
        {
            DateTime desde = dtOverviewDesde == null ? DateTime.Today.AddMonths(-5) : dtOverviewDesde.Value.Date;
            DateTime hasta = dtOverviewHasta == null ? DateTime.Today : dtOverviewHasta.Value.Date;
            DateTime primerMes = new DateTime(desde.Year, desde.Month, 1);
            DateTime ultimoMes = new DateTime(hasta.Year, hasta.Month, 1);
            DateTime seisMesesAntes = ultimoMes.AddMonths(-5);
            if (primerMes < seisMesesAntes)
                primerMes = seisMesesAntes;

            List<OverviewMonthBucket> meses = new List<OverviewMonthBucket>();
            for (DateTime mes = primerMes; mes <= ultimoMes; mes = mes.AddMonths(1))
            {
                meses.Add(new OverviewMonthBucket(mes));
            }

            Dictionary<string, OverviewMonthBucket> porMes = meses.ToDictionary(
                mes => mes.Month.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                mes => mes);
            foreach (DataRow fila in datos.Rows)
            {
                string periodo = Convert.ToString(fila["periodo"], CultureInfo.InvariantCulture) ?? "";
                if (!DateTime.TryParseExact(periodo, "yyyy-MM", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out DateTime fechaMes))
                    continue;

                string tipo = Convert.ToString(fila["tipo"], CultureInfo.InvariantCulture) ?? "";
                string claveTipo = tipo.Trim().ToUpperInvariant();
                string claveMes = fechaMes.ToString("yyyy-MM", CultureInfo.InvariantCulture);
                if (porMes.TryGetValue(claveMes, out OverviewMonthBucket? mes) &&
                    mes.Valores.ContainsKey(claveTipo))
                {
                    mes.Valores[claveTipo] = Convert.ToInt32(fila["total"], CultureInfo.InvariantCulture);
                }
            }
            return meses;
        }

        private string TraducirTipoMovimientoOverview(string tipo)
        {
            switch ((tipo ?? "").ToUpperInvariant())
            {
                case "INGRESO": return "Ingreso";
                case "SALIDA": return "Salida";
                case "TRASLADO": return "Traslado";
                case "DEVOLUCION": return "Devolución";
                case "MANTENIMIENTO": return "Mantenimiento";
                default: return tipo;
            }
        }

        private void ActualizarResumenSidebar(int bajoUmbral, int movimientosHoy, int responsablesActivos)
        {
            if (lblSidebarCondicionBaja != null)
                lblSidebarCondicionBaja.Text = bajoUmbral.ToString("N0", CultureInfo.GetCultureInfo("es-MX"))
                    + " equipos requieren revisión";

            if (lblSidebarMovimientosHoy != null)
                lblSidebarMovimientosHoy.Text = movimientosHoy.ToString("N0", CultureInfo.GetCultureInfo("es-MX"))
                    + " movimientos registrados hoy";

            if (lblSidebarResponsables != null)
                lblSidebarResponsables.Text = responsablesActivos.ToString("N0", CultureInfo.GetCultureInfo("es-MX"))
                    + " personas disponibles para asignación";
        }

        private void ConfigurarColumnasOverview()
        {
            if (dgvOverviewMovimientos == null || dgvOverviewMovimientos.Columns.Count == 0) return;
            string[] nombres = { "Fecha", "Hora", "Tipo", "Codigo", "Equipo", "Cantidad", "Responsable", "Ubicacion", "Motivo" };
            string[] encabezados = { "Fecha", "Hora", "Tipo", "Código", "Equipo", "Cantidad", "Responsable", "Ubicación", "Motivo" };
            float[] pesos = { 10f, 7f, 10f, 9f, 17f, 8f, 15f, 12f, 12f };
            for (int i = 0; i < encabezados.Length; i++)
            {
                if (dgvOverviewMovimientos.Columns.Contains(nombres[i]))
                {
                    dgvOverviewMovimientos.Columns[nombres[i]].HeaderText = encabezados[i];
                    dgvOverviewMovimientos.Columns[nombres[i]].FillWeight = pesos[i];
                }
            }
            if (dgvOverviewMovimientos.Columns.Contains("Cantidad"))
                dgvOverviewMovimientos.Columns["Cantidad"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        }

        private sealed class OverviewBar
        {
            public string Label { get; set; } = "";
            public int Value { get; set; }
        }

        private sealed class OverviewMonthBucket
        {
            public DateTime Month { get; private set; }
            public Dictionary<string, int> Valores { get; private set; }

            public OverviewMonthBucket(DateTime month)
            {
                Month = month;
                Valores = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                {
                    { "INGRESO", 0 },
                    { "SALIDA", 0 },
                    { "TRASLADO", 0 },
                    { "DEVOLUCION", 0 },
                    { "MANTENIMIENTO", 0 }
                };
            }
        }

        private sealed class OverviewResponsableOpcion
        {
            public int Id { get; private set; }
            public string Nombre { get; private set; }

            public OverviewResponsableOpcion(int id, string nombre)
            {
                Id = id;
                Nombre = nombre;
            }

            public override string ToString()
            {
                return Nombre;
            }
        }

        private sealed class OverviewMonthlyChart : Panel
        {
            private List<OverviewMonthBucket> meses = new List<OverviewMonthBucket>();

            private static readonly string[] TiposMovimiento =
            {
                "INGRESO", "SALIDA", "TRASLADO", "DEVOLUCION", "MANTENIMIENTO"
            };

            private static readonly string[] EtiquetasMovimiento =
            {
                "Ingreso", "Salida", "Traslado", "Devolución", "Mantenimiento"
            };

            private static readonly Color[] ColoresMovimiento =
            {
                Color.FromArgb(16, 145, 105),
                Color.FromArgb(231, 92, 77),
                Color.FromArgb(49, 130, 206),
                Color.FromArgb(139, 92, 246),
                Color.FromArgb(217, 146, 38)
            };

            [System.ComponentModel.DefaultValue(typeof(Color), "SteelBlue")]
            public Color AccentColor { get; set; } = Color.SteelBlue;

            public OverviewMonthlyChart()
            {
                SetStyle(
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.UserPaint |
                    ControlStyles.ResizeRedraw,
                    true);

                TabStop = false;
            }

            public void SetData(List<OverviewMonthBucket> nuevos)
            {
                meses = nuevos ?? new List<OverviewMonthBucket>();
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                using (Font fuente = new Font("Segoe UI", 7.5f))
                using (Font fuenteEje = new Font("Segoe UI", 7f))
                using (Font fuenteLeyenda = new Font("Segoe UI", 7.1f))
                using (SolidBrush texto = new SolidBrush(ForeColor))
                using (Pen rejilla = new Pen(Color.FromArgb(48, ForeColor), 1f))
                {
                    int ancho = ClientSize.Width;
                    int alto = ClientSize.Height;
                    if (ancho < 80 || alto < 70)
                        return;

                    int left = 34;
                    int right = 9;
                    int top = 10;
                    int bottom = 36;
                    int plotWidth = Math.Max(1, ancho - left - right);
                    int plotHeight = Math.Max(1, alto - top - bottom);
                    int leyendaY = alto - 22;

                    long maximo = 0;
                    foreach (OverviewMonthBucket mes in meses)
                        foreach (int valor in mes.Valores.Values)
                            if (valor > maximo) maximo = valor;

                    if (meses.Count == 0 || maximo == 0)
                    {
                        TextRenderer.DrawText(g, "Sin movimientos en el periodo seleccionado", fuente,
                            new Rectangle(0, top, ancho, Math.Max(30, plotHeight)), ForeColor,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                    }
                    else
                    {
                        long escala = Math.Max(3L, ((maximo + 2L) / 3L) * 3L);
                        for (int tick = 0; tick <= 3; tick++)
                        {
                            int y = top + plotHeight - (plotHeight * tick / 3);
                            g.DrawLine(rejilla, left, y, left + plotWidth, y);
                            long valorTick = escala * tick / 3;
                            Rectangle etiquetaTick = new Rectangle(0, y - 8, left - 5, 16);
                            TextRenderer.DrawText(g, valorTick.ToString("N0", CultureInfo.GetCultureInfo("es-MX")),
                                fuenteEje, etiquetaTick, ForeColor,
                                TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                        }

                        int groupWidth = Math.Max(1, plotWidth / Math.Max(1, meses.Count));
                        int barWidth = Math.Max(2, Math.Min(13, (groupWidth - 12) / 7));
                        int gap = Math.Max(1, Math.Min(4, (groupWidth - (barWidth * TiposMovimiento.Length)) /
                            (TiposMovimiento.Length + 1)));
                        CultureInfo cultura = CultureInfo.GetCultureInfo("es-MX");

                        for (int i = 0; i < meses.Count; i++)
                        {
                            OverviewMonthBucket mes = meses[i];
                            int anchoGrupoBarras = TiposMovimiento.Length * barWidth + (TiposMovimiento.Length - 1) * gap;
                            int inicio = left + i * groupWidth + Math.Max(0, (groupWidth - anchoGrupoBarras) / 2);
                            for (int serie = 0; serie < TiposMovimiento.Length; serie++)
                            {
                                int valor = mes.Valores.TryGetValue(TiposMovimiento[serie], out int v) ? v : 0;
                                int altura = maximo == 0 ? 0 : (int)Math.Round(plotHeight * (valor / (double)escala));
                                if (valor > 0) altura = Math.Max(2, altura);
                                using (SolidBrush barra = new SolidBrush(ColoresMovimiento[serie]))
                                {
                                    Rectangle rect = new Rectangle(
                                        inicio + serie * (barWidth + gap),
                                        top + plotHeight - altura,
                                        barWidth,
                                        Math.Max(0, altura));
                                    if (rect.Height > 0) g.FillRectangle(barra, rect);
                                }
                            }

                            string mesCorto = mes.Month.ToString("MMM", cultura);
                            if (mesCorto.Length > 0)
                                mesCorto = char.ToUpper(mesCorto[0], cultura) + mesCorto.Substring(1);
                            Rectangle etiquetaMes = new Rectangle(left + i * groupWidth, top + plotHeight + 4,
                                groupWidth, 17);
                            TextRenderer.DrawText(g, mesCorto, fuente, etiquetaMes, ForeColor,
                                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                        }
                    }

                    int anchoLeyenda = Math.Max(1, ancho / TiposMovimiento.Length);
                    for (int i = 0; i < TiposMovimiento.Length; i++)
                    {
                        int x = i * anchoLeyenda + 2;
                        using (SolidBrush punto = new SolidBrush(ColoresMovimiento[i]))
                            g.FillEllipse(punto, x, leyendaY + 5, 8, 8);
                        Rectangle etiqueta = new Rectangle(x + 12, leyendaY, Math.Max(1, anchoLeyenda - 14), 18);
                        TextRenderer.DrawText(g, EtiquetasMovimiento[i], fuenteLeyenda, etiqueta, ForeColor,
                            TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                    }
                }
            }
        }

        private sealed class OverviewDonutChart : Panel
        {
            private List<OverviewBar> estados = new List<OverviewBar>();

            private static readonly Color[] ColoresEstado =
            {
                Color.FromArgb(16, 145, 105), Color.FromArgb(49, 130, 206),
                Color.FromArgb(217, 146, 38), Color.FromArgb(231, 92, 77),
                Color.FromArgb(139, 92, 246), Color.FromArgb(20, 156, 165),
                Color.FromArgb(184, 93, 163), Color.FromArgb(116, 130, 145)
            };

            public OverviewDonutChart()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
                TabStop = false;
            }

            public void SetData(List<OverviewBar> nuevos)
            {
                List<OverviewBar> ordenados = (nuevos ?? new List<OverviewBar>())
                    .Where(x => x.Value > 0).OrderByDescending(x => x.Value).ToList();
                if (ordenados.Count > 7)
                {
                    long totalOtros = ordenados.Skip(6).Sum(x => (long)x.Value);
                    ordenados = ordenados.Take(6).ToList();
                    ordenados.Add(new OverviewBar
                    {
                        Label = "Otros",
                        Value = (int)Math.Min(int.MaxValue, totalOtros)
                    });
                }
                estados = ordenados;
                Invalidate();
            }

            private static Color ColorParaEstado(string estado)
            {
                string valor = (estado ?? "").Trim().ToLowerInvariant();
                if (valor.Contains("dispon") || valor.Contains("operativ") || valor.Contains("activo"))
                    return ColoresEstado[0];
                if (valor.Contains("asign") || valor.Contains("prest"))
                    return ColoresEstado[1];
                if (valor.Contains("manten") || valor.Contains("revisi"))
                    return ColoresEstado[2];
                if (valor.Contains("baja") || valor.Contains("dañ") || valor.Contains("dan") || valor.Contains("inactiv"))
                    return ColoresEstado[3];
                uint hash = unchecked((uint)StringComparer.OrdinalIgnoreCase.GetHashCode(estado ?? ""));
                return ColoresEstado[4 + (int)(hash % 4u)];
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                using (Font fuente = new Font("Segoe UI", 7.4f))
                using (Font fuenteCentro = new Font("Segoe UI", 10f, FontStyle.Bold))
                using (Font fuenteCentroPequena = new Font("Segoe UI", 6.7f))
                using (SolidBrush texto = new SolidBrush(ForeColor))
                using (SolidBrush fondoCentro = new SolidBrush(BackColor))
                {
                    int ancho = ClientSize.Width;
                    int alto = ClientSize.Height;
                    if (ancho < 100 || alto < 70)
                        return;

                    List<OverviewBar> valores = estados.Where(x => x.Value > 0)
                        .OrderByDescending(x => x.Value).Take(8).ToList();
                    long total = valores.Sum(x => (long)x.Value);
                    if (total <= 0)
                    {
                        TextRenderer.DrawText(g, "Sin equipos para estos filtros", fuente,
                            ClientRectangle, ForeColor,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                        return;
                    }

                    int anchoLeyenda = Math.Max(118, (int)(ancho * 0.43));
                    int areaDonut = Math.Max(70, ancho - anchoLeyenda - 8);
                    int diametro = Math.Max(58, Math.Min(alto - 24, areaDonut - 12));
                    int xDonut = Math.Max(2, (areaDonut - diametro) / 2);
                    int yDonut = Math.Max(2, (alto - diametro) / 2);
                    Rectangle rectDonut = new Rectangle(xDonut, yDonut, diametro, diametro);
                    float inicio = -90f;
                    for (int i = 0; i < valores.Count; i++)
                    {
                        float barrido = i == valores.Count - 1
                            ? 270f - inicio
                            : (float)(360d * valores[i].Value / total);
                        using (SolidBrush segmento = new SolidBrush(ColorParaEstado(valores[i].Label)))
                            g.FillPie(segmento, rectDonut, inicio, Math.Max(0.1f, barrido));
                        inicio += barrido;
                    }

                    int hueco = (int)(diametro * 0.58);
                    Rectangle rectCentro = new Rectangle(
                        rectDonut.X + (diametro - hueco) / 2,
                        rectDonut.Y + (diametro - hueco) / 2,
                        hueco, hueco);
                    g.FillEllipse(fondoCentro, rectCentro);
                    Rectangle textoTotal = new Rectangle(rectCentro.X, rectCentro.Y + rectCentro.Height / 2 - 16,
                        rectCentro.Width, 22);
                    TextRenderer.DrawText(g, total.ToString("N0", CultureInfo.GetCultureInfo("es-MX")),
                        fuenteCentro, textoTotal, ForeColor,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                    Rectangle textoEquipos = new Rectangle(rectCentro.X, textoTotal.Bottom - 1,
                        rectCentro.Width, 15);
                    TextRenderer.DrawText(g, "equipos", fuenteCentroPequena, textoEquipos, ForeColor,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                    int leyendaX = areaDonut + 7;
                    int filas = valores.Count;
                    int altoFila = Math.Max(17, Math.Min(24, (alto - 8) / Math.Max(1, filas)));
                    for (int i = 0; i < filas; i++)
                    {
                        int y = 4 + i * altoFila;
                        using (SolidBrush muestra = new SolidBrush(ColorParaEstado(valores[i].Label)))
                            g.FillEllipse(muestra, leyendaX, y + 5, 8, 8);

                        int anchoValor = 78;
                        Rectangle rectEtiqueta = new Rectangle(leyendaX + 13, y,
                            Math.Max(20, ancho - leyendaX - anchoValor - 16), altoFila);
                        Rectangle rectValor = new Rectangle(ancho - anchoValor - 3, y, anchoValor, altoFila);
                        TextRenderer.DrawText(g, valores[i].Label, fuente, rectEtiqueta, ForeColor,
                            TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                        string proporcion = ((double)valores[i].Value * 100d / total).ToString("0.#", CultureInfo.GetCultureInfo("es-MX")) + "%";
                        string conteoYProporcion = valores[i].Value.ToString("N0", CultureInfo.GetCultureInfo("es-MX")) + " · " + proporcion;
                        TextRenderer.DrawText(g, conteoYProporcion, fuente, rectValor, ForeColor,
                            TextFormatFlags.VerticalCenter | TextFormatFlags.Right | TextFormatFlags.EndEllipsis);
                    }
                }
            }
        }

        private sealed class OverviewBarChart : Panel
        {
            private List<OverviewBar> barras = new List<OverviewBar>();

            [System.ComponentModel.DefaultValue(typeof(Color), "SeaGreen")]
            public Color BarColor { get; set; } = Color.SeaGreen;

            public OverviewBarChart()
            {
                SetStyle(
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.UserPaint |
                    ControlStyles.ResizeRedraw, true);

                TabStop = false;
            }

            public void SetBars(List<OverviewBar> nuevos, Color color)
            {
                barras = nuevos ?? new List<OverviewBar>();
                BarColor = color;
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                Graphics graphics = e.Graphics;
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Color texto = ForeColor;
                Color barraFondo = BackColor == Color.Transparent ? Color.FromArgb(30, 40, 50) : ControlPaint.Dark(BackColor, 0.08f);
                using (Font fuente = new Font("Segoe UI", 8f, FontStyle.Regular))
                using (Font fuenteNegrita = new Font("Segoe UI", 8f, FontStyle.Bold))
                using (SolidBrush brushFondo = new SolidBrush(barraFondo))
                using (SolidBrush brushBarra = new SolidBrush(BarColor))
                {
                    if (barras.Count == 0)
                    {
                        TextRenderer.DrawText(graphics, "Sin registros para estos filtros", fuente,
                            ClientRectangle, texto, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                        return;
                    }

                    List<OverviewBar> visibles = barras.OrderByDescending(b => b.Value).Take(6).ToList();
                    int maximo = Math.Max(1, visibles.Max(b => b.Value));
                    int filaAlto = Math.Max(25, Math.Min(34, (ClientSize.Height - 12) / visibles.Count));
                    int anchoEtiqueta = Math.Min(155, Math.Max(82, ClientSize.Width / 3));
                    int anchoConteo = 42;
                    int xBarra = anchoEtiqueta + 8;
                    int anchoBarra = Math.Max(24, ClientSize.Width - xBarra - anchoConteo - 18);

                    for (int i = 0; i < visibles.Count; i++)
                    {
                        OverviewBar barra = visibles[i];
                        int y = 4 + i * filaAlto;
                        Rectangle rectEtiqueta = new Rectangle(2, y, anchoEtiqueta, filaAlto - 2);
                        TextRenderer.DrawText(graphics, barra.Label, fuente, rectEtiqueta, texto,
                            TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                        Rectangle fondo = new Rectangle(xBarra, y + (filaAlto - 12) / 2, anchoBarra, 12);
                        graphics.FillRectangle(brushFondo, fondo);
                        int anchoRelleno = Math.Max(barra.Value > 0 ? 3 : 0, (int)(anchoBarra * barra.Value / (double)maximo));
                        graphics.FillRectangle(brushBarra, new Rectangle(fondo.X, fondo.Y, Math.Min(anchoBarra, anchoRelleno), fondo.Height));
                        Rectangle rectValor = new Rectangle(xBarra + anchoBarra + 5, y, anchoConteo, filaAlto - 2);
                        TextRenderer.DrawText(graphics, barra.Value.ToString("N0", CultureInfo.CurrentCulture), fuenteNegrita,
                            rectValor, texto, TextFormatFlags.VerticalCenter | TextFormatFlags.Right | TextFormatFlags.EndEllipsis);
                    }
                }
            }
        }

        // =========================================================
        // ACCIONES
        // =========================================================
        private void EjecutarEnvioWhatsApp()
        {
            global::SUSTENTACION.PanelAdministrador.CotrolReporte.AbrirNotificacionWhatsApp(this);
        }

        private void BtnCerrarSesion_Click(object? sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
                "¿Estás seguro de que deseas cerrar sesión?",
                "Cerrar Sesión",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                this.Close();
            }
        }

        // =========================================================
        // UTILIDADES VISUALES
        // =========================================================
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
}
