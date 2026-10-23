
using System;
using System.Windows.Forms;

namespace SUSTENTACION
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Iniciar con el login de la rama trabajador
            Application.Run(new Form1());
        }
    }
}
