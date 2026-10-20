using Npgsql;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SUSTENTACION.PanelTrabajador
{
    public class AjustesPanel : UserControl
    {
        // ═══════════════════════════════════════════
        // PALETA
        // ═══════════════════════════════════════════
        private readonly Color bgMain = Color.FromArgb(21, 22, 37);
        private readonly Color bgCard = Color.FromArgb(30, 31, 48);
        private readonly Color bgRow = Color.FromArgb(38, 40, 58);
        private readonly Color accentPurple = Color.FromArgb(92, 84, 241);
        private readonly Color accentGreen = Color.FromArgb(46, 204, 148);
        private readonly Color accentRed = Color.FromArgb(220, 53, 69);
        private readonly Color textWhite = Color.White;
        private readonly Color textMuted = Color.FromArgb(130, 134, 158);

        private int idTrabajador;
        private string nombreTrabajador;

        // Referencias para logout
        public event Action CerrarSesionSolicitado;
        public event Action<bool> TemaCambiado;   // true = oscuro, false = claro

        // Estado del tema
        private bool temaOscuro = true;

        public AjustesPanel(int idTrabajador, string nombreTrabajador)
        {
            this.idTrabajador = idTrabajador;
            this.nombreTrabajador = nombreTrabajador;

            InitializeComponentes();
        }

        private void InitializeComponentes()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = bgMain;
            this.Padding = new Padding(24, 20, 24, 20);
            this.AutoScroll = true;

            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,   // ← era 4, ahora 3
                BackColor = Color.Transparent,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80f));   // Header
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 160f));  // Preferencias
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 260f));  // Info

            mainLayout.Controls.Add(CrearHeader(), 0, 0);
            mainLayout.Controls.Add(CrearSeccionTema(), 0, 1);
            mainLayout.Controls.Add(CrearSeccionInfo(), 0, 2);
            // ❌ Ya no agregamos CrearSeccionLogout()

            this.Controls.Add(mainLayout);
        }

        // ═══════════════════════════════════════════
        // HEADER
        // ═══════════════════════════════════════════
        private Panel CrearHeader()
        {
            Panel header = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = bgCard,
                Padding = new Padding(20)
            };
            AplicarBordesRedondeados(header, 12);

            header.Controls.Add(new Label
            {
                Text = "⚙️  Ajustes",
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = textWhite,
                Location = new Point(20, 14),
                AutoSize = true,
                BackColor = Color.Transparent
            });

            header.Controls.Add(new Label
            {
                Text = "Configuración de apariencia y información del sistema",
                Font = new Font("Segoe UI", 9f),
                ForeColor = textMuted,
                Location = new Point(22, 44),
                AutoSize = true,
                BackColor = Color.Transparent
            });

            return header;
        }

        // ═══════════════════════════════════════════
        // SECCIÓN 1: TEMA OSCURO / CLARO
        // ═══════════════════════════════════════════
        private Panel CrearSeccionTema()
        {
            Panel card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = bgCard,
                Padding = new Padding(20),
                Margin = new Padding(0, 10, 0, 0)
            };
            AplicarBordesRedondeados(card, 12);

            card.Controls.Add(new Label
            {
                Text = "🎨  Apariencia",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = textWhite,
                Location = new Point(18, 14),
                AutoSize = true,
                BackColor = Color.Transparent
            });

            // Fila con el toggle
            Panel fila = new Panel
            {
                Location = new Point(15, 55),
                Size = new Size(card.Width - 30, 70),
                BackColor = bgRow,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            AplicarBordesRedondeados(fila, 10);

            // Ícono
            fila.Controls.Add(new Label
            {
                Text = "🌙",
                Font = new Font("Segoe UI", 18f),
                ForeColor = accentPurple,
                Location = new Point(18, 18),
                AutoSize = true,
                BackColor = Color.Transparent
            });

            // Texto
            fila.Controls.Add(new Label
            {
                Text = "Tema oscuro",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = textWhite,
                Location = new Point(60, 15),
                AutoSize = true,
                BackColor = Color.Transparent
            });

            fila.Controls.Add(new Label
            {
                Text = "Cambia entre modo oscuro y modo claro",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = textMuted,
                Location = new Point(60, 38),
                AutoSize = true,
                BackColor = Color.Transparent
            });

            // Toggle switch
            Panel switchContainer = new Panel
            {
                Size = new Size(60, 32),
                BackColor = accentPurple,
                Location = new Point(fila.Width - 80, 19)
            };
            AplicarBordesRedondeados(switchContainer, 16);
            switchContainer.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            Panel knob = new Panel
            {
                Size = new Size(24, 24),
                BackColor = Color.White,
                Location = new Point(32, 4)
            };
            AplicarBordesRedondeados(knob, 12);
            switchContainer.Controls.Add(knob);

            switchContainer.Cursor = Cursors.Hand;
            switchContainer.Click += (s, e) =>
            {
                temaOscuro = !temaOscuro;
                TemaManager.EsOscuro = temaOscuro;
                GuardarPreferenciaTema(temaOscuro);
                TemaCambiado?.Invoke(temaOscuro);
            };

            fila.Controls.Add(switchContainer);
            card.Controls.Add(fila);

            return card;
        }

        // ═══════════════════════════════════════════
        // SECCIÓN 2: INFORMACIÓN DEL SISTEMA
        // ═══════════════════════════════════════════
        private Panel CrearSeccionInfo()
        {
            Panel card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = bgCard,
                Padding = new Padding(20),
                Margin = new Padding(0, 10, 0, 0)
            };
            AplicarBordesRedondeados(card, 12);

            card.Controls.Add(new Label
            {
                Text = "ℹ️  Información del sistema",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = textWhite,
                Location = new Point(18, 14),
                AutoSize = true,
                BackColor = Color.Transparent
            });

            FlowLayoutPanel items = new FlowLayoutPanel
            {
                Location = new Point(15, 50),
                Size = new Size(card.Width - 30, 200),
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = false,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };

            items.Controls.Add(CrearItemInfo("📦", "Versión", "ARTEMISA v1.0.0"));
            items.Controls.Add(CrearItemInfo("💾", "Base de datos", "Supabase PostgreSQL"));
            items.Controls.Add(CrearItemInfo("🔌", "Estado de conexión", "● Conectado", accentGreen));

            // Botón de política de privacidad
            Button btnPrivacidad = new Button
            {
                Text = "📄  Ver política de privacidad",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = accentPurple,
                BackColor = bgRow,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(300, 38),
                Margin = new Padding(0, 5, 0, 0),
                Cursor = Cursors.Hand
            };
            btnPrivacidad.FlatAppearance.BorderSize = 0;
            AplicarBordesRedondeados(btnPrivacidad, 8);
            btnPrivacidad.Click += (s, e) => AbrirPrivacidad();
            items.Controls.Add(btnPrivacidad);

            card.Controls.Add(items);
            return card;
        }

        //
        //
        //
        private void GuardarPreferenciaTema(bool oscuro)
        {
            Conexion conexionDB = new Conexion();
            try
            {
                NpgsqlConnection con = conexionDB.ObtenerConexion();
                string sql = @"
            INSERT INTO preferencias_usuario (id_trabajador, tema_oscuro)
            VALUES (@id, @oscuro)
            ON CONFLICT (id_trabajador) 
            DO UPDATE SET tema_oscuro = @oscuro";

                using (NpgsqlCommand cmd = new NpgsqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@id", idTrabajador);
                    cmd.Parameters.AddWithValue("@oscuro", oscuro);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error guardar tema: " + ex.Message);
            }
            finally { conexionDB.CerrarConexion(); }
        }

        // ═══════════════════════════════════════════
        // SECCIÓN 3: CERRAR SESIÓN
        // ═══════════════════════════════════════════
        private Panel CrearSeccionLogout()
        {
            Panel card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = bgCard,
                Padding = new Padding(20),
                Margin = new Padding(0, 10, 0, 0)
            };
            AplicarBordesRedondeados(card, 12);

            card.Controls.Add(new Label
            {
                Text = "🚪  Sesión",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = textWhite,
                Location = new Point(18, 14),
                AutoSize = true,
                BackColor = Color.Transparent
            });

            card.Controls.Add(new Label
            {
                Text = "Cierra tu sesión actual. Serás redirigido al login.",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = textMuted,
                Location = new Point(18, 40),
                AutoSize = true,
                BackColor = Color.Transparent
            });

            Button btnLogout = new Button
            {
                Text = "Cerrar sesión",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = accentRed,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(200, 42),
                Location = new Point(18, 62),
                Cursor = Cursors.Hand
            };
            btnLogout.FlatAppearance.BorderSize = 0;
            btnLogout.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 73, 89);
            AplicarBordesRedondeados(btnLogout, 8);
            btnLogout.Click += (s, e) =>
            {
                var resp = MessageBox.Show(
                    "¿Estás seguro de cerrar sesión?",
                    "Cerrar sesión",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (resp == DialogResult.Yes)
                    CerrarSesionSolicitado?.Invoke();
            };
            card.Controls.Add(btnLogout);

            return card;
        }

        // ═══════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════
        private Panel CrearItemInfo(string icono, string titulo, string valor, Color? colorValor = null)
        {
            Panel item = new Panel
            {
                Width = 1100,
                Height = 44,
                Margin = new Padding(0, 3, 0, 3),
                BackColor = bgRow
            };
            AplicarBordesRedondeados(item, 8);

            item.Controls.Add(new Label
            {
                Text = icono,
                Font = new Font("Segoe UI", 12f),
                ForeColor = accentPurple,
                Location = new Point(14, 11),
                AutoSize = true,
                BackColor = Color.Transparent
            });

            item.Controls.Add(new Label
            {
                Text = titulo,
                Font = new Font("Segoe UI", 9f),
                ForeColor = textMuted,
                Location = new Point(48, 12),
                AutoSize = true,
                BackColor = Color.Transparent
            });

            item.Controls.Add(new Label
            {
                Text = valor,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = colorValor ?? textWhite,
                Location = new Point(240, 12),
                AutoSize = true,
                BackColor = Color.Transparent
            });

            return item;
        }

        private void AbrirPrivacidad()
        {
            MessageBox.Show(
                "📄  Política de privacidad — ARTEMISA\n\n" +
                "1. Datos que recopilamos:\n" +
                "   • Nombre, correo, teléfono, DNI (registro)\n" +
                "   • Eventos y equipos asignados\n" +
                "   • Historial de salidas y retornos\n\n" +
                "2. Cómo usamos los datos:\n" +
                "   • Gestión de inventario de equipos\n" +
                "   • Coordinación de eventos\n" +
                "   • Comunicación con administradores\n\n" +
                "3. Almacenamiento:\n" +
                "   • Supabase (PostgreSQL) con cifrado SSL\n" +
                "   • Servidores en AWS (us-east-1)\n\n" +
                "4. Sus derechos:\n" +
                "   • Puede solicitar la corrección de sus datos\n" +
                "   • Puede solicitar la eliminación de su cuenta\n\n" +
                "Contacto: admin@artemusa.com",
                "Política de privacidad",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        // ═══════════════════════════════════════════
        // UTILIDADES
        // ═══════════════════════════════════════════
        private void AplicarBordesRedondeados(Control control, int radio)
        {
            Action recalc = () =>
            {
                if (control.Width <= 0 || control.Height <= 0) return;
                using (var path = new GraphicsPath())
                {
                    path.AddArc(0, 0, radio, radio, 180, 90);
                    path.AddArc(control.Width - radio, 0, radio, radio, 270, 90);
                    path.AddArc(control.Width - radio, control.Height - radio, radio, radio, 0, 90);
                    path.AddArc(0, control.Height - radio, radio, radio, 90, 90);
                    path.CloseAllFigures();
                    control.Region = new Region(path);
                }
            };
            control.Resize += (s, e) => recalc();
            recalc();
        }
    }
}