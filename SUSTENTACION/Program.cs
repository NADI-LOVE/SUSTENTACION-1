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

            // Inicio con formulario de login que redirige según rol
            Application.Run(new LoginForm1());
        }
    }
}