using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Npgsql;

namespace SUSTENTACION.PanelTrabajador
{
    public class FormRetornoEquipo : Form
    {
        // ==========================================
        // PALETA DE COLORES
        // ==========================================
        private readonly Color fondo = Color.FromArgb(17, 19, 32);
        private readonly Color tarjeta = Color.FromArgb(26, 29, 46);
        private readonly Color campo = Color.FromArgb(37, 41, 62);
        private readonly Color morado = Color.FromArgb(100, 88, 245);
        private readonly Color blanco = Color.FromArgb(245, 246, 255);
        private readonly Color gris = Color.FromArgb(158, 164, 188);
        private readonly Color verde = Color.FromArgb(22, 177, 127);
        private readonly Color naranja = Color.FromArgb(255, 160, 60);
        private readonly Color rojo = Color.FromArgb(230, 80, 80);
        private readonly Color amarillo = Color.FromArgb(245, 200, 70);

        // ==========================================
        // DATOS
        // ==========================================
        private readonly int idTrabajador;
        private readonly string nombreTrabajador;
        private readonly DateTime fechaSeleccionada;

        private int idSalida = 0;
        private List<EquipoRetorno> equipos = new List<EquipoRetorno>();

        // ==========================================
        // COMPONENTES
        // ==========================================
        private Label lblSalidaInfo;
        private FlowLayoutPanel flpEquipos;
        private Label lblContador;
        private Label lblSinEquipos;

        // ==========================================
        // MODELO
        // ==========================================
        private class EquipoRetorno
        {
            public int Id { get; set; }
            public string Codigo { get; set; }
            public string Nombre { get; set; }
            public bool Incluir { get; set; } = false;   // ← Ahora FALSE por defecto
            public string Estado { get; set; } = "Buen estado";
            public string Observaciones { get; set; } = "";

            public CheckBox ChkIncluir { get; set; }
            public RadioButton RbBueno { get; set; }
            public RadioButton RbFalla { get; set; }
            public RadioButton RbFallando { get; set; }
            public TextBox TxtObservaciones { get; set; }
            public Panel Tarjeta { get; set; }
        }

        public FormRetornoEquipo(
            int idTrabajador,
            string nombreTrabajador,
            DateTime fecha)
        {
            this.idTrabajador = idTrabajador;
            this.nombreTrabajador = nombreTrabajador;
            this.fechaSeleccionada = fecha;

            CrearInterfaz();
            this.Load += (s, e) => CargarDatos();
        }

        // ==========================================
        // CREAR INTERFAZ
        // ==========================================
        private void CrearInterfaz()
        {
            this.Text = "ARTEMUSA | Registrar retorno de equipos";
            this.Size = new Size(1100, 780);
            this.MinimumSize = new Size(950, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = fondo;
            this.Font = new Font("Segoe UI", 9);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;

            // Cabecera
            Label titulo = new Label
            {
                Text = "REGISTRAR RETORNO DE EQUIPOS",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = blanco,
                Location = new Point(24, 20),
                AutoSize = true
            };
            this.Controls.Add(titulo);

            Label subtitulo = new Label
            {
                Text = "Marca cada equipo con su estado. Puedes retornar los equipos que desees.",
                Font = new Font("Segoe UI", 9),
                ForeColor = gris,
                Location = new Point(26, 55),
                AutoSize = true
            };
            this.Controls.Add(subtitulo);

            // Info salida
            lblSalidaInfo = new Label
            {
                Text = "Cargando información...",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = morado,
                Location = new Point(26, 90),
                Size = new Size(1000, 45)
            };
            this.Controls.Add(lblSalidaInfo);

            // Contador
            lblContador = new Label
            {
                Text = "0 equipos seleccionados",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = blanco,
                Location = new Point(26, 140),
                AutoSize = true
            };
            this.Controls.Add(lblContador);

            // Label "sin equipos"
            lblSinEquipos = new Label
            {
                Text = "✅ Todos los equipos de esta salida ya fueron retornados.",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = verde,
                Location = new Point(26, 250),
                Size = new Size(1000, 30),
                Visible = false
            };
            this.Controls.Add(lblSinEquipos);

            // Flow de equipos
            flpEquipos = new FlowLayoutPanel
            {
                Location = new Point(24, 170),
                Size = new Size(1040, 510),
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = fondo,
                Padding = new Padding(0)
            };
            this.Controls.Add(flpEquipos);

            // Botones
            Button btnRetornar = new Button
            {
                Text = "REGISTRAR RETORNO SELECCIONADOS",
                Width = 300,
                Height = 45,
                Location = new Point(764, 695),
                FlatStyle = FlatStyle.Flat,
                BackColor = naranja,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Name = "btnRetornar"
            };
            btnRetornar.FlatAppearance.BorderSize = 0;
            btnRetornar.Click += (s, e) => RegistrarRetorno();
            this.Controls.Add(btnRetornar);

            Button btnCancelar = new Button
            {
                Text = "CANCELAR",
                Width = 155,
                Height = 45,
                Location = new Point(594, 695),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(65, 68, 88),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCancelar.FlatAppearance.BorderSize = 0;
            btnCancelar.Click += (s, e) =>
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };
            this.Controls.Add(btnCancelar);
        }

        // ==========================================
        // CARGAR DATOS
        // ==========================================
        private void CargarDatos()
        {
            Conexion conexionDB = new Conexion();
            try
            {
                NpgsqlConnection con = conexionDB.ObtenerConexion();

                // 1. Obtener la salida del día
                string querySalida = @"
                    SELECT id_salida, encargado, ubicacion
                    FROM salidas
                    WHERE DATE(fecha_salida) = @fecha
                      AND (estado IS NULL OR estado <> 'Devuelto')
                    LIMIT 1";

                using (NpgsqlCommand cmd = new NpgsqlCommand(querySalida, con))
                {
                    cmd.Parameters.AddWithValue("@fecha", fechaSeleccionada.Date);
                    using (NpgsqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            idSalida = Convert.ToInt32(reader["id_salida"]);
                            string encargado = reader["encargado"].ToString();
                            string ubicacion = reader["ubicacion"].ToString();

                            lblSalidaInfo.Text =
                                $"Salida del {fechaSeleccionada:dd/MM/yyyy}   |   " +
                                $"Encargado: {encargado}   |   Ubicación: {ubicacion}";
                        }
                        else
                        {
                            lblSalidaInfo.Text = "No se encontró una salida activa para este día.";
                            lblSalidaInfo.ForeColor = rojo;
                            return;
                        }
                    }
                }

                // 2. Obtener SOLO los equipos que aún NO han sido retornados
                string queryEquipos = @"
                    SELECT e.id, e.codigo, e.nombre
                    FROM equipos e
                    INNER JOIN salida_equipos se ON se.id_equipo = e.id
                    WHERE se.id_salida = @salida
                      AND e.id NOT IN (
                          SELECT COALESCE(id_equipo, 0) 
                          FROM retornos 
                          WHERE id_salida = @salida
                      )
                    ORDER BY e.nombre";

                using (NpgsqlCommand cmd = new NpgsqlCommand(queryEquipos, con))
                {
                    cmd.Parameters.AddWithValue("@salida", idSalida);
                    using (NpgsqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            equipos.Add(new EquipoRetorno
                            {
                                Id = Convert.ToInt32(reader["id"]),
                                Codigo = reader["codigo"].ToString(),
                                Nombre = reader["nombre"].ToString()
                            });
                        }
                    }
                }

                // 3. Si no quedan equipos pendientes
                if (equipos.Count == 0)
                {
                    lblSinEquipos.Visible = true;
                    lblContador.Visible = false;

                    // Ocultar botón de retornar
                    var btnRet = this.Controls.Find("btnRetornar", true);
                    if (btnRet.Length > 0) btnRet[0].Enabled = false;

                    return;
                }

                // 4. Crear tarjetas
                foreach (var eq in equipos)
                {
                    CrearTarjetaEquipo(eq);
                }

                ActualizarContador();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar datos:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                conexionDB.CerrarConexion();
            }
        }

        // ==========================================
        // CREAR TARJETA
        // ==========================================
        private void CrearTarjetaEquipo(EquipoRetorno eq)
        {
            Panel card = new Panel
            {
                Width = 1010,
                Height = 130,
                BackColor = tarjeta,
                Margin = new Padding(0, 0, 0, 10),
                Padding = new Padding(15)
            };
            eq.Tarjeta = card;

            // Checkbox DESMARCADO por defecto
            CheckBox chkIncluir = new CheckBox
            {
                Text = "",
                Location = new Point(10, 15),
                Size = new Size(20, 20),
                Checked = false,           // ← AHORA FALSE
                Cursor = Cursors.Hand
            };
            chkIncluir.CheckedChanged += (s, e) =>
            {
                eq.Incluir = chkIncluir.Checked;
                card.BackColor = eq.Incluir
                    ? Color.FromArgb(35, 38, 58)   // Seleccionado: un poco más claro
                    : tarjeta;
                ActualizarContador();
            };
            eq.ChkIncluir = chkIncluir;
            card.Controls.Add(chkIncluir);

            // Código
            Label lblCodigo = new Label
            {
                Text = eq.Codigo,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = morado,
                Location = new Point(40, 12),
                AutoSize = true
            };
            card.Controls.Add(lblCodigo);

            // Nombre
            Label lblNombre = new Label
            {
                Text = eq.Nombre,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = blanco,
                Location = new Point(40, 35),
                AutoSize = true
            };
            card.Controls.Add(lblNombre);

            // Radio Buen estado
            RadioButton rbBueno = new RadioButton
            {
                Text = "✓ Buen estado",
                Font = new Font("Segoe UI", 9),
                ForeColor = verde,
                Location = new Point(40, 75),
                Size = new Size(180, 22),
                Checked = true,
                Cursor = Cursors.Hand
            };
            rbBueno.CheckedChanged += (s, e) =>
            {
                if (rbBueno.Checked)
                {
                    eq.Estado = "Buen estado";
                    eq.TxtObservaciones.Enabled = false;
                    eq.TxtObservaciones.Text = "";
                }
            };
            eq.RbBueno = rbBueno;
            card.Controls.Add(rbBueno);

            // Radio Presenta fallas
            RadioButton rbFalla = new RadioButton
            {
                Text = "⚠ Presenta fallas",
                Font = new Font("Segoe UI", 9),
                ForeColor = amarillo,
                Location = new Point(230, 75),
                Size = new Size(180, 22),
                Cursor = Cursors.Hand
            };
            rbFalla.CheckedChanged += (s, e) =>
            {
                if (rbFalla.Checked)
                {
                    eq.Estado = "Presenta fallas";
                    eq.TxtObservaciones.Enabled = true;
                    eq.TxtObservaciones.Focus();
                }
            };
            eq.RbFalla = rbFalla;
            card.Controls.Add(rbFalla);

            // Radio Está fallando
            RadioButton rbFallando = new RadioButton
            {
                Text = "✗ Está fallando",
                Font = new Font("Segoe UI", 9),
                ForeColor = rojo,
                Location = new Point(420, 75),
                Size = new Size(180, 22),
                Cursor = Cursors.Hand
            };
            rbFallando.CheckedChanged += (s, e) =>
            {
                if (rbFallando.Checked)
                {
                    eq.Estado = "Está fallando";
                    eq.TxtObservaciones.Enabled = true;
                    eq.TxtObservaciones.Focus();
                }
            };
            eq.RbFallando = rbFallando;
            card.Controls.Add(rbFallando);

            // Observaciones
            TextBox txtObs = new TextBox
            {
                Location = new Point(620, 12),
                Size = new Size(370, 28),
                BackColor = campo,
                ForeColor = blanco,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9),
                Enabled = false,
                Text = ""
            };
            eq.TxtObservaciones = txtObs;
            card.Controls.Add(txtObs);

            Label lblObs = new Label
            {
                Text = "Observaciones (obligatorio si hay fallas):",
                Font = new Font("Segoe UI", 7.5f),
                ForeColor = gris,
                Location = new Point(620, 45),
                AutoSize = true
            };
            card.Controls.Add(lblObs);

            flpEquipos.Controls.Add(card);
        }

        // ==========================================
        // CONTADOR
        // ==========================================
        private void ActualizarContador()
        {
            int seleccionados = 0;
            foreach (var eq in equipos)
                if (eq.Incluir) seleccionados++;

            lblContador.Text = $"{seleccionados} de {equipos.Count} equipos seleccionados";
        }

        // ==========================================
        // REGISTRAR RETORNO
        // ==========================================
        private void RegistrarRetorno()
        {
            var equiposSeleccionados = equipos.FindAll(e => e.Incluir);

            if (equiposSeleccionados.Count == 0)
            {
                MessageBox.Show(
                    "Marca al menos un equipo (con el checkbox) para retornar.",
                    "Sin equipos seleccionados",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            // Validar observaciones
            foreach (var eq in equiposSeleccionados)
            {
                bool requiereObs = eq.Estado == "Presenta fallas" || eq.Estado == "Está fallando";
                if (requiereObs && string.IsNullOrWhiteSpace(eq.TxtObservaciones.Text))
                {
                    MessageBox.Show(
                        $"Debes describir la falla del equipo:\n{eq.Codigo} - {eq.Nombre}",
                        "Observación requerida",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    eq.TxtObservaciones.Focus();
                    return;
                }

                eq.Observaciones = eq.TxtObservaciones.Text.Trim();
            }

            // Confirmar
            string resumen = $"¿Confirmar retorno de {equiposSeleccionados.Count} equipo(s)?\n\n";
            foreach (var eq in equiposSeleccionados)
                resumen += $"• {eq.Codigo} - {eq.Estado}\n";

            if (MessageBox.Show(resumen, "Confirmar retorno",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            Conexion conexionDB = new Conexion();
            NpgsqlTransaction transaccion = null;

            try
            {
                NpgsqlConnection con = conexionDB.ObtenerConexion();
                transaccion = con.BeginTransaction();

                foreach (var eq in equiposSeleccionados)
                {
                    // INSERT en retornos
                    string insertRetorno = @"
                        INSERT INTO retornos
                        (id_salida, id_trabajador, id_equipo, fecha_retorno, observaciones, estado, estado_equipo)
                        VALUES (@salida, @trabajador, @equipo, @fecha, @obs, 'Completado', @estadoEquipo)";

                    using (NpgsqlCommand cmdRet = new NpgsqlCommand(insertRetorno, con, transaccion))
                    {
                        cmdRet.Parameters.AddWithValue("@salida", idSalida);
                        cmdRet.Parameters.AddWithValue("@trabajador", idTrabajador);
                        cmdRet.Parameters.AddWithValue("@equipo", eq.Id);
                        cmdRet.Parameters.AddWithValue("@fecha", DateTime.Now);
                        cmdRet.Parameters.AddWithValue("@obs",
                            string.IsNullOrWhiteSpace(eq.Observaciones)
                                ? (object)DBNull.Value
                                : eq.Observaciones);
                        cmdRet.Parameters.AddWithValue("@estadoEquipo", eq.Estado);
                        cmdRet.ExecuteNonQuery();
                    }

                    // UPDATE estado del equipo
                    string nuevoEstado = eq.Estado == "Buen estado"
                        ? "Disponible"
                        : eq.Estado == "Presenta fallas"
                            ? "Con observaciones"
                            : "En mantenimiento";

                    string updateEquipo = @"
                        UPDATE equipos
                        SET estado = @estado
                        WHERE id = @id";

                    using (NpgsqlCommand cmdEq = new NpgsqlCommand(updateEquipo, con, transaccion))
                    {
                        cmdEq.Parameters.AddWithValue("@estado", nuevoEstado);
                        cmdEq.Parameters.AddWithValue("@id", eq.Id);
                        cmdEq.ExecuteNonQuery();
                    }
                }

                // Verificar si quedan equipos pendientes
                string countPendientes = @"
                    SELECT COUNT(*) 
                    FROM salida_equipos se
                    WHERE se.id_salida = @salida
                      AND se.id_equipo NOT IN (
                          SELECT COALESCE(id_equipo, 0) 
                          FROM retornos 
                          WHERE id_salida = @salida
                      )";

                int pendientes = 0;
                using (NpgsqlCommand cmdCount = new NpgsqlCommand(countPendientes, con, transaccion))
                {
                    cmdCount.Parameters.AddWithValue("@salida", idSalida);
                    pendientes = Convert.ToInt32(cmdCount.ExecuteScalar());
                }

                // Si ya no quedan pendientes, marcar la salida como Devuelto
                if (pendientes == 0)
                {
                    string update = @"
                        UPDATE salidas
                        SET estado = 'Devuelto'
                        WHERE id_salida = @id";

                    using (NpgsqlCommand cmdUpd = new NpgsqlCommand(update, con, transaccion))
                    {
                        cmdUpd.Parameters.AddWithValue("@id", idSalida);
                        cmdUpd.ExecuteNonQuery();
                    }
                }

                transaccion.Commit();

                MessageBox.Show(
                    $"{equiposSeleccionados.Count} equipo(s) retornado(s) correctamente.",
                    "Éxito",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                if (transaccion != null)
                {
                    try { transaccion.Rollback(); } catch { }
                }

                MessageBox.Show(
                    "Error al registrar retorno:\n" + ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                conexionDB.CerrarConexion();
            }
        }
    }
}