using System.Drawing;
using System.Windows.Forms;
using SUSTENTACION.Temas;
using System.Reflection;

namespace SUSTENTACION
{
    public class TemaUserControl : UserControl
    {
        public TemaUserControl()
        {
            // Se suscribe al evento global para actualizarse cuando cambie el tema
            TemaApp.TemaCambiado += AplicarTema;

            // Se desuscribe al destruirse para evitar fugas de memoria
            this.Disposed += (s, e) => TemaApp.TemaCambiado -= AplicarTema;
        }

        public void AplicarTema()
        {
            if (this.IsDisposed) return;

            this.BackColor = TemaApp.FondoApp;
            this.ForeColor = TemaApp.Texto;

            // 1. Actualizar los controles estándar de Windows Forms
            ActualizarControles(this);

            // 2. Actualizar la clase Tema local (para que tus controles dibujados a mano se pinten bien)
            var tipoTemaLocal = this.GetType().GetNestedType("Tema", BindingFlags.NonPublic | BindingFlags.Public);
            if (tipoTemaLocal != null)
            {
                var propOscuro = tipoTemaLocal.GetField("Oscuro", BindingFlags.Public | BindingFlags.Static);
                if (propOscuro != null) propOscuro.SetValue(null, TemaApp.EsModoOscuro);
            }

            // 3. Refrescar todo el panel
            this.Invalidate(true);
        }

        private void ActualizarControles(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                // 1. Paneles y Contenedores
                if (c is Panel || c is TableLayoutPanel || c is FlowLayoutPanel)
                {
                    // Si es el fondo principal usa FondoApp, si es una tarjeta interna usa Superficie
                    c.BackColor = parent is TemaUserControl ? TemaApp.FondoApp : TemaApp.Superficie;
                }

                // 2. Tablas (DataGridView)
                else if (c is DataGridView grid)
                {
                    // SI LA TABLA YA FUE PINTADA POR SU PANEL (Y TIENE FONDO BLANCO), IGNORARLA
                    if (grid.BackgroundColor == TemaApp.Superficie || grid.BackgroundColor == Color.White)
                        continue;

                    grid.BackgroundColor = TemaApp.Superficie;
                    grid.GridColor = TemaApp.Borde;
                    grid.BorderStyle = BorderStyle.None;
                    grid.EnableHeadersVisualStyles = false;

                    // Encabezados
                    grid.ColumnHeadersDefaultCellStyle.BackColor = TemaApp.SuperficieAlt;
                    grid.ColumnHeadersDefaultCellStyle.ForeColor = TemaApp.EsModoOscuro ? TemaApp.Exito : TemaApp.TextoTenue;
                    grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = TemaApp.SuperficieAlt;
                    grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = grid.ColumnHeadersDefaultCellStyle.ForeColor;

                    // Celdas Normales
                    grid.DefaultCellStyle.BackColor = TemaApp.Superficie;
                    grid.DefaultCellStyle.ForeColor = TemaApp.Texto;
                    grid.DefaultCellStyle.SelectionBackColor = TemaApp.EsModoOscuro ? Color.FromArgb(32, 42, 53) : Color.FromArgb(226, 243, 235);
                    grid.DefaultCellStyle.SelectionForeColor = TemaApp.Texto;

                    // Forzar que las filas alternas sean exactamente iguales a las normales
                    grid.AlternatingRowsDefaultCellStyle.BackColor = TemaApp.Superficie;
                    grid.AlternatingRowsDefaultCellStyle.ForeColor = TemaApp.Texto;
                    grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = grid.DefaultCellStyle.SelectionBackColor;
                    grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = TemaApp.Texto;
                }

                // 3. Cajas de texto, combos y numéricos
                else if (c is TextBox || c is ComboBox || c is NumericUpDown)
                {
                    c.BackColor = TemaApp.SuperficieAlt;
                    c.ForeColor = TemaApp.Texto;
                }

                // 4. Etiquetas (Textos)
                else if (c is Label lbl)
                {
                    // Respetamos las etiquetas transparentes (dibujadas a mano)
                    if (lbl.BackColor != Color.Transparent)
                    {
                        lbl.ForeColor = lbl.Font.Bold ? TemaApp.Texto : TemaApp.TextoTenue;
                    }
                }

                // 5. Botones estándar
                else if (c is Button btn)
                {
                    bool esBotonColor = btn.BackColor == Color.FromArgb(163, 230, 53) || btn.BackColor == Color.FromArgb(239, 68, 68);
                    if (!esBotonColor)
                    {
                        btn.BackColor = TemaApp.Superficie;
                        btn.ForeColor = TemaApp.Texto;
                        btn.FlatAppearance.BorderColor = TemaApp.Borde;
                        btn.FlatAppearance.MouseOverBackColor = TemaApp.SuperficieAlt;
                    }
                }

                // Recursividad
                if (c.HasChildren) ActualizarControles(c);
            }
        }
    }
}